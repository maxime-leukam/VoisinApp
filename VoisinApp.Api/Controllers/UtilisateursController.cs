namespace VoisinApp.Api.Controllers;

[Route("api/utilisateurs")]
public class UtilisateursController(AppDbContext db) : ControleurDeBase
{
    // Messagerie, bouton "+" : trouver un voisin à qui écrire (même quartier, profil visible)
    [HttpGet("recherche")]
    public async Task<ActionResult<List<UtilisateurResumeDto>>> Rechercher(string? q)
    {
        string? terme = q?.Trim();
        if (string.IsNullOrWhiteSpace(terme) || terme.Length < 2)
            return Ok(new List<UtilisateurResumeDto>());

        Utilisateur moi = await db.Utilisateurs.FirstAsync(u => u.Id == UtilisateurId);
        if (moi.QuartierId is null)
            return Ok(new List<UtilisateurResumeDto>());

        string motif = $"%{terme}%";
        var lignes = await db.Utilisateurs
            .Where(u => u.Actif
                        && u.Id != moi.Id
                        && u.QuartierId == moi.QuartierId
                        && (EF.Functions.Like(u.Prenom, motif) || EF.Functions.Like(u.Nom, motif))
                        && !db.ParametresUtilisateurs.Any(p => p.UtilisateurId == u.Id && !p.ProfilVisible))
            .OrderBy(u => u.Prenom)
            .Take(20)
            .Select(u => new { u.Id, u.Prenom, u.Nom, u.AvatarUrl, QuartierNom = u.Quartier != null ? u.Quartier.Nom : null })
            .ToListAsync();

        return Ok(lignes.Select(u => new UtilisateurResumeDto(
            u.Id, Textes.NomComplet(u.Prenom, u.Nom), Textes.Initiales(u.Prenom, u.Nom),
            UrlAbsolue(u.AvatarUrl), u.QuartierNom)).ToList());
    }
}
