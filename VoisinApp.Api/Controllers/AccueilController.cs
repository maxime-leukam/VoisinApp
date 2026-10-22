namespace VoisinApp.Api.Controllers;

[Route("api")]
public class AccueilController(AppDbContext db) : ControleurDeBase
{
    // Page d'accueil : "Bonjour Mireille", quartier, pastille de notification, fil "Près de chez vous"
    [HttpGet("accueil")]
    public async Task<ActionResult<AccueilDto>> Accueil()
    {
        Utilisateur moi = await db.Utilisateurs.Include(u => u.Quartier).FirstAsync(u => u.Id == UtilisateurId);
        int nonLues = await db.Notifications.CountAsync(n => n.UtilisateurId == moi.Id && !n.Lue);

        List<PublicationDto> fil = moi.QuartierId is long quartierId
            ? await ConstruireFilAsync(moi.Id, quartierId, moi.Quartier?.Nom, 1, 10)
            : new List<PublicationDto>();

        return Ok(new AccueilDto(moi.Prenom, moi.Quartier?.Nom, nonLues, fil));
    }

    // "Tout voir" : fil complet, paginé
    [HttpGet("fil")]
    public async Task<ActionResult<List<PublicationDto>>> Fil(int page = 1, int taille = 10)
    {
        Utilisateur moi = await db.Utilisateurs.Include(u => u.Quartier).FirstAsync(u => u.Id == UtilisateurId);
        if (moi.QuartierId is not long quartierId)
            return Ok(new List<PublicationDto>());

        page = Math.Max(1, page);
        taille = Math.Clamp(taille, 1, 30);
        return Ok(await ConstruireFilAsync(moi.Id, quartierId, moi.Quartier?.Nom, page, taille));
    }

    private async Task<List<PublicationDto>> ConstruireFilAsync(long moiId, long quartierId, string? quartierNom, int page, int taille)
    {
        int limite = page * taille;

        var annonces = await db.Annonces
            .Where(a => a.QuartierId == quartierId && a.Statut == "active")
            .OrderByDescending(a => a.CreeLe)
            .Take(limite)
            .Select(a => new
            {
                a.Id, a.Titre, a.Description, a.Type, a.CreeLe,
                AuteurId = a.UtilisateurId,
                a.Utilisateur.Prenom, a.Utilisateur.Nom, a.Utilisateur.AvatarUrl,
                Photo = a.Photos.OrderBy(p => p.Position).Select(p => p.Url).FirstOrDefault()
            })
            .ToListAsync();

        var demandes = await db.DemandesAide
            .Where(d => d.QuartierId == quartierId && (d.Statut == "ouverte" || d.Statut == "en_cours"))
            .OrderByDescending(d => d.CreeLe)
            .Take(limite)
            .Select(d => new
            {
                d.Id, d.Titre, d.Description, d.Urgente, d.CreeLe,
                AuteurId = d.UtilisateurId,
                d.Utilisateur.Prenom, d.Utilisateur.Nom, d.Utilisateur.AvatarUrl
            })
            .ToListAsync();

        CompteursInteractions cAnnonces = await OutilsInteractions.ChargerAsync(db, "annonce", annonces.Select(a => a.Id).ToList(), moiId);
        CompteursInteractions cDemandes = await OutilsInteractions.ChargerAsync(db, "demande_aide", demandes.Select(d => d.Id).ToList(), moiId);

        List<long> idsDemandes = demandes.Select(d => d.Id).ToList();
        Dictionary<long, int> propositions = await db.ReponsesAide
            .Where(r => idsDemandes.Contains(r.DemandeAideId))
            .GroupBy(r => r.DemandeAideId)
            .Select(g => new { Id = g.Key, N = g.Count() })
            .ToDictionaryAsync(x => x.Id, x => x.N);

        var publications = new List<PublicationDto>();

        foreach (var a in annonces)
        {
            publications.Add(new PublicationDto(
                "annonce", a.Id, VersAuteur(a.AuteurId, a.Prenom, a.Nom, a.AvatarUrl), quartierNom,
                a.CreeLe, Temps.DateRelative(a.CreeLe), a.Titre, a.Description, UrlAbsolue(a.Photo),
                Libelles.EtiquetteAnnonce(a.Type),
                cAnnonces.Jaimes.GetValueOrDefault(a.Id), cAnnonces.Commentaires.GetValueOrDefault(a.Id),
                cAnnonces.AJaime.Contains(a.Id)));
        }

        foreach (var d in demandes)
        {
            publications.Add(new PublicationDto(
                "demande_aide", d.Id, VersAuteur(d.AuteurId, d.Prenom, d.Nom, d.AvatarUrl), quartierNom,
                d.CreeLe, Temps.DateRelative(d.CreeLe), d.Titre, d.Description, null,
                d.Urgente ? "URGENT" : "DEMANDE D'AIDE",
                cDemandes.Jaimes.GetValueOrDefault(d.Id),
                cDemandes.Commentaires.GetValueOrDefault(d.Id) + propositions.GetValueOrDefault(d.Id),
                cDemandes.AJaime.Contains(d.Id)));
        }

        return publications
            .OrderByDescending(p => p.CreeLe)
            .Skip((page - 1) * taille)
            .Take(taille)
            .ToList();
    }
}
