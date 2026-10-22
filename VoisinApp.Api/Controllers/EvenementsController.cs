using System.Globalization;

namespace VoisinApp.Api.Controllers;

[Route("api/evenements")]
public class EvenementsController(
    AppDbContext db, ServiceNotifications notifications, ServiceFichiers fichiers) : ControleurDeBase
{
    // Page "Événements" : événements d'un jour (date au format AAAA-MM-JJ, aujourd'hui par défaut)
    [HttpGet]
    public async Task<ActionResult<List<EvenementResumeDto>>> ListerParJour(string? date)
    {
        DateOnly jour = Temps.AujourdhuiLocal();
        if (!string.IsNullOrWhiteSpace(date) &&
            !DateOnly.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out jour))
            return Erreur(400, "Date invalide (format attendu : AAAA-MM-JJ).");

        Utilisateur moi = await db.Utilisateurs.FirstAsync(u => u.Id == UtilisateurId);
        if (moi.QuartierId is not long quartierId)
            return Ok(new List<EvenementResumeDto>());

        var (debut, fin) = Temps.BornesDuJour(jour);
        List<Evenement> evenements = await db.Evenements
            .Where(e => e.QuartierId == quartierId && e.DebutLe >= debut && e.DebutLe < fin)
            .OrderBy(e => e.DebutLe)
            .ToListAsync();

        return Ok(await ConstruireResumesAsync(evenements));
    }

    // Section "À venir" : à partir de demain
    [HttpGet("a-venir")]
    public async Task<ActionResult<List<EvenementResumeDto>>> AVenir(int limite = 10)
    {
        Utilisateur moi = await db.Utilisateurs.FirstAsync(u => u.Id == UtilisateurId);
        if (moi.QuartierId is not long quartierId)
            return Ok(new List<EvenementResumeDto>());

        DateTime demain = Temps.BornesDuJour(Temps.AujourdhuiLocal()).FinUtc;
        List<Evenement> evenements = await db.Evenements
            .Where(e => e.QuartierId == quartierId && e.DebutLe >= demain)
            .OrderBy(e => e.DebutLe)
            .Take(Math.Clamp(limite, 1, 50))
            .ToListAsync();

        return Ok(await ConstruireResumesAsync(evenements));
    }

    // Page "Détail événement"
    [HttpGet("{id:long}")]
    public async Task<ActionResult<EvenementDetailDto>> Detail(long id, double? latitude, double? longitude)
    {
        Evenement? e = await db.Evenements.FirstOrDefaultAsync(x => x.Id == id);
        if (e is null)
            return Erreur(404, "Événement introuvable.");

        Utilisateur moi = await db.Utilisateurs.Include(u => u.Quartier).FirstAsync(u => u.Id == UtilisateurId);

        var participants = await db.ParticipantsEvenement
            .Where(p => p.EvenementId == id)
            .OrderBy(p => p.RejointLe)
            .Select(p => new { p.UtilisateurId, p.Utilisateur.Prenom, p.Utilisateur.Nom })
            .ToListAsync();

        // "dont 6 que vous connaissez" : participants avec qui j'ai déjà une conversation
        IQueryable<long> mesConversations = db.MembresConversation
            .Where(m => m.UtilisateurId == moi.Id)
            .Select(m => m.ConversationId);
        List<long> connusListe = await db.MembresConversation
            .Where(m => mesConversations.Contains(m.ConversationId) && m.UtilisateurId != moi.Id)
            .Select(m => m.UtilisateurId)
            .Distinct()
            .ToListAsync();
        HashSet<long> connus = connusListe.ToHashSet();
        int nbConnus = participants.Count(p => p.UtilisateurId != moi.Id && connus.Contains(p.UtilisateurId));

        // "À 8 min à pied"
        string? trajet = null;
        double? posLat = latitude ?? moi.Quartier?.Latitude;
        double? posLon = longitude ?? moi.Quartier?.Longitude;
        if (e.Latitude.HasValue && e.Longitude.HasValue && posLat.HasValue && posLon.HasValue)
        {
            double metres = Geo.Metres(posLat.Value, posLon.Value, e.Latitude.Value, e.Longitude.Value);
            trajet = $"À {Geo.MinutesAPied(metres)} min à pied";
        }

        return Ok(new EvenementDetailDto(
            e.Id, e.Titre, e.Categorie, e.Description, e.NomLieu, e.DebutLe, e.FinLe,
            Temps.DateLongue(e.DebutLe), Temps.Horaires(e.DebutLe, e.FinLe), trajet,
            participants.Count, nbConnus, nbConnus > 0 ? $"dont {nbConnus} que vous connaissez" : null,
            participants.Take(3).Select(p => Textes.Initiales(p.Prenom, p.Nom)).ToList(),
            UrlAbsolue(e.ImageCouvertureUrl), participants.Any(p => p.UtilisateurId == moi.Id),
            e.OrganisateurId == moi.Id));
    }

    // Bouton "+" : créer un événement
    [HttpPost]
    public async Task<ActionResult<EvenementDetailDto>> Creer(CreerEvenementDto dto)
    {
        string? titre = dto.Titre?.Trim();
        string? lieu = dto.NomLieu?.Trim();
        if (string.IsNullOrWhiteSpace(titre))
            return Erreur(400, "Donnez un titre à l'événement.");
        if (string.IsNullOrWhiteSpace(lieu))
            return Erreur(400, "Indiquez le lieu de l'événement.");
        if (dto.DebutLe is null)
            return Erreur(400, "Indiquez la date et l'heure de début.");
        if (dto.DebutLe.Value.ToUniversalTime() < DateTime.UtcNow.AddMinutes(-5))
            return Erreur(400, "La date de l'événement est déjà passée.");
        if (dto.FinLe is not null && dto.FinLe <= dto.DebutLe)
            return Erreur(400, "L'heure de fin doit être après le début.");

        Utilisateur moi = await db.Utilisateurs.FirstAsync(u => u.Id == UtilisateurId);
        if (moi.QuartierId is not long quartierId)
            return Erreur(400, "Complétez votre quartier avant de créer un événement.");

        var evenement = new Evenement
        {
            OrganisateurId = moi.Id,
            QuartierId = quartierId,
            Titre = titre,
            Categorie = dto.Categorie?.Trim(),
            Description = dto.Description?.Trim(),
            NomLieu = lieu,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            DebutLe = dto.DebutLe.Value,
            FinLe = dto.FinLe
        };
        evenement.Participants.Add(new ParticipantEvenement { UtilisateurId = moi.Id }); // l'organisateur participe
        db.Evenements.Add(evenement);
        await db.SaveChangesAsync();

        List<long> voisins = await db.Utilisateurs
            .Where(u => u.QuartierId == quartierId && u.Id != moi.Id && u.Actif)
            .Select(u => u.Id)
            .Take(500)
            .ToListAsync();
        await notifications.AjouterPourPlusieursAsync(voisins, "evenement",
            $"Nouvel événement : {titre}", $"{Temps.DateLongue(evenement.DebutLe)} · {lieu}", "evenement", evenement.Id);
        await db.SaveChangesAsync();

        return await Detail(evenement.Id, null, null);
    }

    // Image de couverture (organisateur uniquement). Champ de formulaire : "fichier"
    [HttpPost("{id:long}/image")]
    [RequestSizeLimit(20_000_000)]
    public async Task<ActionResult<ReponseSimpleDto>> ChangerImage(long id, [FromForm(Name = "fichier")] IFormFile fichierEnvoye)
    {
        Evenement? evenement = await db.Evenements.FirstOrDefaultAsync(e => e.Id == id);
        if (evenement is null)
            return Erreur(404, "Événement introuvable.");
        if (evenement.OrganisateurId != UtilisateurId)
            return Erreur(403, "Seul l'organisateur peut modifier l'image.");

        var (url, erreur) = await fichiers.EnregistrerImageAsync(fichierEnvoye, "evenements");
        if (url is null)
            return Erreur(400, erreur ?? "Image refusée.");

        fichiers.Supprimer(evenement.ImageCouvertureUrl);
        evenement.ImageCouvertureUrl = url;
        await db.SaveChangesAsync();
        return Ok(new ReponseSimpleDto(UrlAbsolue(url)!));
    }

    // Bouton "Je participe" : s'inscrire / se désinscrire
    [HttpPost("{id:long}/participer")]
    public async Task<ActionResult<ParticipationDto>> Participer(long id)
    {
        if (!await db.Evenements.AnyAsync(e => e.Id == id))
            return Erreur(404, "Événement introuvable.");

        ParticipantEvenement? existant = await db.ParticipantsEvenement
            .FirstOrDefaultAsync(p => p.EvenementId == id && p.UtilisateurId == UtilisateurId);

        bool participe;
        if (existant is null)
        {
            db.ParticipantsEvenement.Add(new ParticipantEvenement { EvenementId = id, UtilisateurId = UtilisateurId });
            participe = true;
        }
        else
        {
            db.ParticipantsEvenement.Remove(existant);
            participe = false;
        }
        await db.SaveChangesAsync();

        int total = await db.ParticipantsEvenement.CountAsync(p => p.EvenementId == id);
        return Ok(new ParticipationDto(participe, total));
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Supprimer(long id)
    {
        Evenement? evenement = await db.Evenements.FirstOrDefaultAsync(e => e.Id == id);
        if (evenement is null)
            return Erreur(404, "Événement introuvable.");
        if (evenement.OrganisateurId != UtilisateurId)
            return Erreur(403, "Seul l'organisateur peut supprimer l'événement.");

        fichiers.Supprimer(evenement.ImageCouvertureUrl);
        db.Commentaires.RemoveRange(db.Commentaires.Where(c => c.TypeCible == "evenement" && c.CibleId == id));
        db.Jaimes.RemoveRange(db.Jaimes.Where(j => j.TypeCible == "evenement" && j.CibleId == id));
        db.Evenements.Remove(evenement);
        await db.SaveChangesAsync();
        return NoContent();
    }

    private async Task<List<EvenementResumeDto>> ConstruireResumesAsync(List<Evenement> evenements)
    {
        List<long> ids = evenements.Select(e => e.Id).ToList();
        if (ids.Count == 0)
            return new List<EvenementResumeDto>();

        var participants = await db.ParticipantsEvenement
            .Where(p => ids.Contains(p.EvenementId))
            .OrderBy(p => p.RejointLe)
            .Select(p => new { p.EvenementId, p.UtilisateurId, p.Utilisateur.Prenom, p.Utilisateur.Nom })
            .ToListAsync();

        return evenements.Select(e =>
        {
            var deLEvenement = participants.Where(p => p.EvenementId == e.Id).ToList();
            return new EvenementResumeDto(
                e.Id, e.Titre, e.Categorie, e.NomLieu, e.DebutLe, e.FinLe,
                Temps.JourHeure(e.DebutLe), Temps.Jour(e.DebutLe), Temps.MoisCourt(e.DebutLe), Temps.Heure(e.DebutLe),
                deLEvenement.Count,
                deLEvenement.Take(3).Select(p => Textes.Initiales(p.Prenom, p.Nom)).ToList(),
                UrlAbsolue(e.ImageCouvertureUrl),
                deLEvenement.Any(p => p.UtilisateurId == UtilisateurId));
        }).ToList();
    }
}
