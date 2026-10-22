namespace VoisinApp.Api.Controllers;

[Route("api/quartiers")]
public class QuartiersController(AppDbContext db) : ControleurDeBase
{
    // Liste des villes proposées (liste déroulante de l'inscription)
    [AllowAnonymous]
    [HttpGet("villes")]
    public ActionResult<List<VilleDto>> Villes() =>
        Ok(VillesCameroun.Toutes
            .OrderBy(v => v.Nom)
            .Select(v => new VilleDto(v.Nom, v.Latitude, v.Longitude))
            .ToList());

    // Quartiers connus d'une ville (suggestions pendant la saisie) : /api/quartiers?ville=Douala&recherche=bon
    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<List<QuartierDto>>> Lister(string? ville, string? recherche)
    {
        IQueryable<Quartier> requete = db.Quartiers;

        if (!string.IsNullOrWhiteSpace(ville))
        {
            VilleInfo? v = VillesCameroun.Trouver(ville);
            if (v is null)
                return Ok(new List<QuartierDto>());
            string nomVille = v.Nom;
            requete = requete.Where(q => q.Ville == nomVille);
        }

        if (!string.IsNullOrWhiteSpace(recherche))
        {
            string motif = $"%{recherche.Trim()}%";
            requete = requete.Where(q => EF.Functions.Like(q.Nom, motif));
        }

        List<Quartier> liste = await requete.OrderBy(q => q.Nom).Take(30).ToListAsync();
        return Ok(liste.Select(q => new QuartierDto(
            q.Id, q.Nom, q.Ville, q.Arrondissement, $"{q.Nom} · {q.Ville}", q.Latitude, q.Longitude)).ToList());
    }

    // Inscription, étape 2 : "Zone : Bonamoussadi · Douala, 1 284 voisins inscrits"
    // (ou "Nouveau quartier" s'il n'existe pas encore : il sera créé à l'inscription)
    [AllowAnonymous]
    [HttpGet("zone")]
    public async Task<ActionResult<ZoneDetecteeDto>> Zone(string? ville, string? quartier)
    {
        VilleInfo? v = VillesCameroun.Trouver(ville);
        if (v is null)
            return Erreur(400, "Choisissez votre ville dans la liste.");

        string nom = (quartier ?? "").Trim();
        if (nom.Length < 2)
            return Erreur(400, "Indiquez le nom de votre quartier.");

        string cle = Textes.Normaliser(nom);
        string nomVille = v.Nom;
        List<Quartier> dansLaVille = await db.Quartiers.Where(q => q.Ville == nomVille).ToListAsync();
        Quartier? existant = dansLaVille.FirstOrDefault(q => Textes.Normaliser(q.Nom) == cle);

        if (existant is null)
        {
            string nomPropre = Textes.NomPropre(nom);
            return Ok(new ZoneDetecteeDto(0, nomPropre, v.Nom, $"{nomPropre} · {v.Nom}", 0,
                "Nouveau quartier : vous serez le premier voisin !", true));
        }

        int voisins = await db.Utilisateurs.CountAsync(u => u.QuartierId == existant.Id && u.Actif);
        string texte = voisins == 0 ? "Soyez le premier voisin inscrit !"
            : voisins == 1 ? "1 voisin inscrit"
            : $"{Textes.Montant(voisins)} voisins inscrits";

        return Ok(new ZoneDetecteeDto(existant.Id, existant.Nom, existant.Ville,
            $"{existant.Nom} · {existant.Ville}", voisins, texte, false));
    }
}
