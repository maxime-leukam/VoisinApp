namespace VoisinApp.Api.Controllers;

[Route("api/signalements")]
public class SignalementsController(
    AppDbContext db, ServiceConversations conversations, ServiceNotifications notifications) : ControleurDeBase
{
    // Page "Faire un signalement" : bouton "Envoyer le signalement"
    [HttpPost]
    public async Task<ActionResult<SignalementDto>> Creer(CreerSignalementDto dto)
    {
        string? description = dto.Description?.Trim();
        if (dto.Categorie is null || !Valeurs.CategoriesSignalement.Contains(dto.Categorie))
            return Erreur(400, "Choisissez ce que vous souhaitez signaler.");
        if (string.IsNullOrWhiteSpace(description))
            return Erreur(400, "Décrivez le problème observé.");
        if (description.Length > 2000)
            return Erreur(400, "Description trop longue (2000 caractères maximum).");
        if (string.IsNullOrWhiteSpace(dto.Localisation) && (dto.Latitude is null || dto.Longitude is null))
            return Erreur(400, "Indiquez l'adresse ou placez un repère sur la carte.");

        Utilisateur moi = await db.Utilisateurs.FirstAsync(u => u.Id == UtilisateurId);

        var signalement = new Signalement
        {
            UtilisateurId = moi.Id,
            QuartierId = moi.QuartierId,
            Categorie = dto.Categorie,
            Localisation = dto.Localisation?.Trim(),
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            Description = description
        };
        db.Signalements.Add(signalement);

        await notifications.AjouterAsync(moi.Id, "signalement", "Signalement reçu",
            $"Votre signalement « {Libelles.CategorieSignalement(dto.Categorie)} » a été enregistré.", "signalement", null);
        await db.SaveChangesAsync();

        // Message de l'équipe dans la messagerie ("Votre signalement a été pris en compte.")
        await conversations.AjouterMessageSystemeAsync(moi.Id, "Votre signalement a été pris en compte.");

        return Ok(VersDto(signalement));
    }

    [HttpGet("mes-signalements")]
    public async Task<ActionResult<List<SignalementDto>>> MesSignalements()
    {
        List<Signalement> liste = await db.Signalements
            .Where(s => s.UtilisateurId == UtilisateurId)
            .OrderByDescending(s => s.CreeLe)
            .ToListAsync();
        return Ok(liste.Select(VersDto).ToList());
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<SignalementDto>> Detail(long id)
    {
        // Un signalement n'est visible que par son auteur (anonymat vis-à-vis des autres voisins)
        Signalement? s = await db.Signalements.FirstOrDefaultAsync(x => x.Id == id && x.UtilisateurId == UtilisateurId);
        if (s is null)
            return Erreur(404, "Signalement introuvable.");
        return Ok(VersDto(s));
    }

    private static SignalementDto VersDto(Signalement s) => new(
        s.Id, s.Categorie, Libelles.CategorieSignalement(s.Categorie), s.Localisation, s.Description,
        s.Statut, Libelles.StatutSignalement(s.Statut), s.CreeLe, Temps.DateRelative(s.CreeLe));
}
