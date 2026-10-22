namespace VoisinApp.Api.Controllers;

[Route("api/annonces")]
public class AnnoncesController(AppDbContext db, ServiceConversations conversations, ServiceFichiers fichiers) : ControleurDeBase
{
    private sealed record LigneAnnonce(
        long Id, string Titre, string Type, long? Prix, string? UnitePrix,
        double? Latitude, double? Longitude, DateTime CreeLe, string? Photo);

    // Page "Petites annonces" : liste, filtres (type) et recherche
    // Le téléphone peut envoyer sa position (latitude, longitude) pour des distances précises.
    [HttpGet]
    public async Task<ActionResult<List<AnnonceCarteDto>>> Lister(
        string? type, string? recherche, double? latitude, double? longitude, int page = 1, int taille = 20)
    {
        Utilisateur moi = await db.Utilisateurs.Include(u => u.Quartier).FirstAsync(u => u.Id == UtilisateurId);
        if (moi.QuartierId is not long quartierId)
            return Ok(new List<AnnonceCarteDto>());

        if (!string.IsNullOrWhiteSpace(type) && !Valeurs.TypesAnnonce.Contains(type))
            return Erreur(400, "Type d'annonce invalide.");

        page = Math.Max(1, page);
        taille = Math.Clamp(taille, 1, 50);

        IQueryable<Annonce> requete = db.Annonces.Where(a => a.QuartierId == quartierId && a.Statut == "active");
        if (!string.IsNullOrWhiteSpace(type))
            requete = requete.Where(a => a.Type == type);
        if (!string.IsNullOrWhiteSpace(recherche))
        {
            string motif = $"%{recherche.Trim()}%";
            requete = requete.Where(a => EF.Functions.Like(a.Titre, motif));
        }

        List<LigneAnnonce> lignes = await requete
            .OrderByDescending(a => a.CreeLe)
            .Skip((page - 1) * taille)
            .Take(taille)
            .Select(a => new LigneAnnonce(
                a.Id, a.Titre, a.Type, a.Prix, a.UnitePrix, a.Latitude, a.Longitude, a.CreeLe,
                a.Photos.OrderBy(p => p.Position).Select(p => p.Url).FirstOrDefault()))
            .ToListAsync();

        return Ok(VersCartes(lignes, latitude ?? moi.Quartier?.Latitude, longitude ?? moi.Quartier?.Longitude));
    }

    // Mes annonces (profil)
    [HttpGet("mes-annonces")]
    public async Task<ActionResult<List<AnnonceCarteDto>>> MesAnnonces()
    {
        List<LigneAnnonce> lignes = await db.Annonces
            .Where(a => a.UtilisateurId == UtilisateurId)
            .OrderByDescending(a => a.CreeLe)
            .Select(a => new LigneAnnonce(
                a.Id, a.Titre, a.Type, a.Prix, a.UnitePrix, a.Latitude, a.Longitude, a.CreeLe,
                a.Photos.OrderBy(p => p.Position).Select(p => p.Url).FirstOrDefault()))
            .ToListAsync();

        return Ok(VersCartes(lignes, null, null));
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<AnnonceDetailDto>> Detail(long id, double? latitude, double? longitude)
    {
        Annonce? annonce = await db.Annonces
            .Include(a => a.Photos)
            .Include(a => a.Utilisateur)
            .FirstOrDefaultAsync(a => a.Id == id);
        if (annonce is null)
            return Erreur(404, "Annonce introuvable.");

        return Ok(await ConstruireDetailAsync(annonce, latitude, longitude));
    }

    // Page "Nouvelle annonce" : crée l'annonce, puis envoyer les photos avec POST {id}/photos
    [HttpPost]
    public async Task<ActionResult<AnnonceDetailDto>> Creer(CreerAnnonceDto dto)
    {
        string? titre = dto.Titre?.Trim();
        if (dto.Type is null || !Valeurs.TypesAnnonce.Contains(dto.Type))
            return Erreur(400, "Choisissez un type d'annonce valide (à vendre, à donner ou service).");
        if (string.IsNullOrWhiteSpace(titre))
            return Erreur(400, "Donnez un titre à votre annonce.");
        if (titre.Length > 120)
            return Erreur(400, "Le titre est trop long (120 caractères maximum).");
        if (dto.Type == "a_vendre" && (dto.Prix is null || dto.Prix < 0))
            return Erreur(400, "Indiquez un prix pour un objet à vendre.");
        if (dto.Prix < 0)
            return Erreur(400, "Le prix ne peut pas être négatif.");

        Utilisateur moi = await db.Utilisateurs.FirstAsync(u => u.Id == UtilisateurId);
        if (moi.QuartierId is not long quartierId)
            return Erreur(400, "Complétez votre quartier avant de publier.");

        var annonce = new Annonce
        {
            UtilisateurId = moi.Id,
            QuartierId = quartierId,
            Type = dto.Type,
            Titre = titre,
            Description = dto.Description?.Trim(),
            Prix = dto.Type == "a_donner" ? null : dto.Prix,
            UnitePrix = dto.Type == "service" ? dto.UnitePrix?.Trim() : null,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude
        };
        db.Annonces.Add(annonce);
        await db.SaveChangesAsync();

        annonce.Utilisateur = moi;
        return Ok(await ConstruireDetailAsync(annonce, null, null));
    }

    // Jusqu'à 5 photos par annonce. Champ de formulaire : "fichiers" (une ou plusieurs images)
    [HttpPost("{id:long}/photos")]
    [RequestSizeLimit(70_000_000)]
    public async Task<ActionResult<PhotosAjouteesDto>> AjouterPhotos(long id, [FromForm(Name = "fichiers")] List<IFormFile> fichiersEnvoyes)
    {
        Annonce? annonce = await db.Annonces.Include(a => a.Photos).FirstOrDefaultAsync(a => a.Id == id);
        if (annonce is null)
            return Erreur(404, "Annonce introuvable.");
        if (annonce.UtilisateurId != UtilisateurId)
            return Erreur(403, "Vous ne pouvez modifier que vos propres annonces.");
        if (fichiersEnvoyes.Count == 0)
            return Erreur(400, "Aucune photo reçue.");
        if (annonce.Photos.Count + fichiersEnvoyes.Count > 5)
            return Erreur(400, "Une annonce peut avoir 5 photos au maximum.");

        var urls = new List<string>();
        foreach (IFormFile fichier in fichiersEnvoyes)
        {
            var (url, erreur) = await fichiers.EnregistrerImageAsync(fichier, "annonces");
            if (url is null)
                return Erreur(400, erreur ?? "Photo refusée.");

            short position = 1;
            while (annonce.Photos.Any(p => p.Position == position)) position++;

            var photo = new PhotoAnnonce { AnnonceId = annonce.Id, Url = url, Position = position };
            annonce.Photos.Add(photo);
            urls.Add(url);
        }

        await db.SaveChangesAsync();
        return Ok(new PhotosAjouteesDto(urls.Select(u => UrlAbsolue(u)!).ToList()));
    }

    // active / vendue / donnee / cloturee
    [HttpPut("{id:long}/statut")]
    public async Task<IActionResult> ChangerStatut(long id, ChangerStatutDto dto)
    {
        if (dto.Statut is null || !Valeurs.StatutsAnnonce.Contains(dto.Statut))
            return Erreur(400, "Statut invalide.");

        Annonce? annonce = await db.Annonces.FirstOrDefaultAsync(a => a.Id == id);
        if (annonce is null)
            return Erreur(404, "Annonce introuvable.");
        if (annonce.UtilisateurId != UtilisateurId)
            return Erreur(403, "Vous ne pouvez modifier que vos propres annonces.");

        annonce.Statut = dto.Statut;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Supprimer(long id)
    {
        Annonce? annonce = await db.Annonces.Include(a => a.Photos).FirstOrDefaultAsync(a => a.Id == id);
        if (annonce is null)
            return Erreur(404, "Annonce introuvable.");
        if (annonce.UtilisateurId != UtilisateurId)
            return Erreur(403, "Vous ne pouvez supprimer que vos propres annonces.");

        foreach (PhotoAnnonce photo in annonce.Photos)
            fichiers.Supprimer(photo.Url);

        db.Commentaires.RemoveRange(db.Commentaires.Where(c => c.TypeCible == "annonce" && c.CibleId == id));
        db.Jaimes.RemoveRange(db.Jaimes.Where(j => j.TypeCible == "annonce" && j.CibleId == id));
        db.Annonces.Remove(annonce);
        await db.SaveChangesAsync();
        return NoContent();
    }

    // Bouton "Contacter" : ouvre (ou retrouve) la conversation avec le vendeur
    [HttpPost("{id:long}/contacter")]
    public async Task<ActionResult<ConversationRefDto>> Contacter(long id)
    {
        Annonce? annonce = await db.Annonces.FirstOrDefaultAsync(a => a.Id == id);
        if (annonce is null)
            return Erreur(404, "Annonce introuvable.");
        if (annonce.UtilisateurId == UtilisateurId)
            return Erreur(400, "C'est votre propre annonce.");

        Conversation conversation = await conversations.ObtenirOuCreerPriveeAsync(UtilisateurId, annonce.UtilisateurId, annonce.Id, null);
        return Ok(new ConversationRefDto(conversation.Id));
    }

    // ---------- Méthodes privées ----------

    private List<AnnonceCarteDto> VersCartes(List<LigneAnnonce> lignes, double? posLat, double? posLon)
    {
        var resultat = new List<AnnonceCarteDto>();
        foreach (LigneAnnonce a in lignes)
        {
            double? distance = null;
            if (a.Latitude.HasValue && a.Longitude.HasValue && posLat.HasValue && posLon.HasValue)
                distance = Geo.Metres(posLat.Value, posLon.Value, a.Latitude.Value, a.Longitude.Value);

            resultat.Add(new AnnonceCarteDto(
                a.Id, a.Titre, a.Type, Libelles.TypeAnnonce(a.Type), a.Prix,
                Textes.PrixTexte(a.Type, a.Prix, a.UnitePrix), UrlAbsolue(a.Photo),
                distance, InfoTexte(distance, a.CreeLe), a.CreeLe));
        }
        return resultat;
    }

    private static string InfoTexte(double? distance, DateTime creeLe) =>
        distance.HasValue
            ? $"À {Geo.Texte(distance.Value)} · {Temps.DateRelative(creeLe)}"
            : Temps.DateRelative(creeLe);

    private async Task<AnnonceDetailDto> ConstruireDetailAsync(Annonce a, double? latitude, double? longitude)
    {
        Utilisateur moi = await db.Utilisateurs.Include(u => u.Quartier).FirstAsync(u => u.Id == UtilisateurId);

        double? posLat = latitude ?? moi.Quartier?.Latitude;
        double? posLon = longitude ?? moi.Quartier?.Longitude;
        double? distance = null;
        if (a.Latitude.HasValue && a.Longitude.HasValue && posLat.HasValue && posLon.HasValue)
            distance = Geo.Metres(posLat.Value, posLon.Value, a.Latitude.Value, a.Longitude.Value);

        CompteursInteractions c = await OutilsInteractions.ChargerAsync(db, "annonce", new List<long> { a.Id }, UtilisateurId);

        List<string> photos = a.Photos
            .OrderBy(p => p.Position)
            .Select(p => UrlAbsolue(p.Url)!)
            .ToList();

        return new AnnonceDetailDto(
            a.Id, a.Titre, a.Type, Libelles.TypeAnnonce(a.Type), a.Description, a.Prix, a.UnitePrix,
            Textes.PrixTexte(a.Type, a.Prix, a.UnitePrix), a.Statut, photos,
            VersAuteur(a.Utilisateur.Id, a.Utilisateur.Prenom, a.Utilisateur.Nom, a.Utilisateur.AvatarUrl),
            distance, InfoTexte(distance, a.CreeLe), a.CreeLe, a.UtilisateurId == UtilisateurId,
            c.Jaimes.GetValueOrDefault(a.Id), c.Commentaires.GetValueOrDefault(a.Id), c.AJaime.Contains(a.Id));
    }
}
