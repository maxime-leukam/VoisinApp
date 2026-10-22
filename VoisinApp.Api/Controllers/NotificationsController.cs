namespace VoisinApp.Api.Controllers;

[Route("api/notifications")]
public class NotificationsController(AppDbContext db) : ControleurDeBase
{
    // Icône cloche de l'accueil
    [HttpGet]
    public async Task<ActionResult<List<NotificationDto>>> Lister(bool nonLuesSeulement = false, int page = 1, int taille = 30)
    {
        page = Math.Max(1, page);
        taille = Math.Clamp(taille, 1, 100);

        IQueryable<Notification> requete = db.Notifications.Where(n => n.UtilisateurId == UtilisateurId);
        if (nonLuesSeulement)
            requete = requete.Where(n => !n.Lue);

        List<Notification> liste = await requete
            .OrderByDescending(n => n.CreeLe)
            .Skip((page - 1) * taille)
            .Take(taille)
            .ToListAsync();

        return Ok(liste.Select(n => new NotificationDto(
            n.Id, n.Type, n.Titre, n.Contenu, n.TypeCible, n.CibleId, n.Lue,
            n.CreeLe, Temps.DateRelative(n.CreeLe))).ToList());
    }

    // Pastille rouge
    [HttpGet("non-lues")]
    public async Task<ActionResult<NonLusDto>> NonLues() =>
        Ok(new NonLusDto(await db.Notifications.CountAsync(n => n.UtilisateurId == UtilisateurId && !n.Lue)));

    [HttpPost("{id:long}/lue")]
    public async Task<IActionResult> MarquerLue(long id)
    {
        Notification? n = await db.Notifications.FirstOrDefaultAsync(x => x.Id == id && x.UtilisateurId == UtilisateurId);
        if (n is null)
            return Erreur(404, "Notification introuvable.");

        n.Lue = true;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("tout-lire")]
    public async Task<IActionResult> ToutMarquerLu()
    {
        await db.Notifications
            .Where(n => n.UtilisateurId == UtilisateurId && !n.Lue)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.Lue, true));
        return NoContent();
    }
}
