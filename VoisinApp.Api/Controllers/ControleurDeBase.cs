namespace VoisinApp.Api.Controllers;

[ApiController]
[Authorize]
public abstract class ControleurDeBase : ControllerBase
{
    // Identifiant de l'utilisateur connecté (lu dans le jeton JWT)
    protected long UtilisateurId => long.Parse(User.FindFirst("uid")?.Value ?? "0");

    protected ObjectResult Erreur(int statut, string message) =>
        StatusCode(statut, new ErreurDto(message));

    // Transforme "/uploads/x.jpg" en "http://adresse-de-l-api/uploads/x.jpg"
    protected string? UrlAbsolue(string? relatif)
    {
        if (string.IsNullOrWhiteSpace(relatif)) return null;
        if (relatif.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return relatif;
        return $"{Request.Scheme}://{Request.Host}{relatif}";
    }

    protected AuteurDto VersAuteur(long id, string prenom, string nom, string? avatar) =>
        new(id, Textes.NomComplet(prenom, nom), Textes.Initiales(prenom, nom), UrlAbsolue(avatar));
}
