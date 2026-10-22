namespace VoisinApp.Api.Controllers;

[Route("api/profil")]
public class ProfilController(AppDbContext db, ServiceProfil profils, ServiceFichiers fichiers) : ControleurDeBase
{
    // Page "Mon profil"
    [HttpGet]
    public async Task<ActionResult<ProfilDto>> Obtenir() =>
        Ok(await profils.ConstruireAsync(UtilisateurId, UrlAbsolue));

    // "Modifier"
    [HttpPut]
    public async Task<ActionResult<ProfilDto>> Modifier(ModifierProfilDto dto)
    {
        Utilisateur u = await db.Utilisateurs.FirstAsync(x => x.Id == UtilisateurId);

        if (dto.Prenom is not null)
        {
            if (string.IsNullOrWhiteSpace(dto.Prenom)) return Erreur(400, "Le prénom ne peut pas être vide.");
            u.Prenom = dto.Prenom.Trim();
        }
        if (dto.Nom is not null)
        {
            if (string.IsNullOrWhiteSpace(dto.Nom)) return Erreur(400, "Le nom ne peut pas être vide.");
            u.Nom = dto.Nom.Trim();
        }
        if (dto.Telephone is not null)
            u.Telephone = dto.Telephone.Trim();

        if (dto.RayonKm is double rayon)
        {
            if (rayon < 0.5 || rayon > 50)
                return Erreur(400, "Le rayon doit être compris entre 0,5 et 50 km.");
            u.RayonKm = rayon;
        }

        if (dto.QuartierId is long quartierId)
        {
            if (!await db.Quartiers.AnyAsync(q => q.Id == quartierId))
                return Erreur(400, "Quartier inconnu.");
            u.QuartierId = quartierId;
        }

        if (dto.Adresse is not null)
        {
            u.Adresse = dto.Adresse.Trim();
            Quartier? detecte = await DetecteurQuartier.TrouverAsync(db, u.Adresse, null);
            if (detecte is not null)
            {
                u.QuartierId = detecte.Id;
                u.AdresseVerifiee = true;
            }
            else
            {
                u.AdresseVerifiee = false;
            }
        }

        u.ModifieLe = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Ok(await profils.ConstruireAsync(u.Id, UrlAbsolue));
    }

    // Photo de profil. Champ de formulaire : "fichier"
    [HttpPost("avatar")]
    [RequestSizeLimit(20_000_000)]
    public async Task<ActionResult<ProfilDto>> ChangerAvatar([FromForm(Name = "fichier")] IFormFile fichierEnvoye)
    {
        var (url, erreur) = await fichiers.EnregistrerImageAsync(fichierEnvoye, "avatars");
        if (url is null)
            return Erreur(400, erreur ?? "Image refusée.");

        Utilisateur u = await db.Utilisateurs.FirstAsync(x => x.Id == UtilisateurId);
        fichiers.Supprimer(u.AvatarUrl);
        u.AvatarUrl = url;
        u.ModifieLe = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Ok(await profils.ConstruireAsync(u.Id, UrlAbsolue));
    }

    // "Notifications" et "Confidentialité et sécurité"
    [HttpGet("parametres")]
    public async Task<ActionResult<ParametresDto>> ObtenirParametres()
    {
        ParametresUtilisateur p = await ObtenirOuCreerParametresAsync();
        return Ok(new ParametresDto(p.NotifierDemandes, p.NotifierMessages, p.NotifierEvenements, p.ProfilVisible));
    }

    [HttpPut("parametres")]
    public async Task<ActionResult<ParametresDto>> ModifierParametres(ParametresDto dto)
    {
        ParametresUtilisateur p = await ObtenirOuCreerParametresAsync();
        p.NotifierDemandes = dto.NotifierDemandes;
        p.NotifierMessages = dto.NotifierMessages;
        p.NotifierEvenements = dto.NotifierEvenements;
        p.ProfilVisible = dto.ProfilVisible;
        await db.SaveChangesAsync();
        return Ok(dto);
    }

    [HttpPut("mot-de-passe")]
    public async Task<ActionResult<ReponseSimpleDto>> ChangerMotDePasse(ChangerMotDePasseDto dto)
    {
        if (string.IsNullOrEmpty(dto.NouveauMotDePasse) || dto.NouveauMotDePasse.Length < 8)
            return Erreur(400, "Le nouveau mot de passe doit contenir au moins 8 caractères.");

        Utilisateur u = await db.Utilisateurs.FirstAsync(x => x.Id == UtilisateurId);
        if (string.IsNullOrEmpty(dto.AncienMotDePasse) || !HacheurMotDePasse.Verifier(dto.AncienMotDePasse, u.MotDePasseHache))
            return Erreur(400, "L'ancien mot de passe est incorrect.");

        u.MotDePasseHache = HacheurMotDePasse.Hacher(dto.NouveauMotDePasse);
        u.ModifieLe = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Ok(new ReponseSimpleDto("Mot de passe modifié."));
    }

    // Désactive le compte (les données ne sont plus accessibles ; la connexion est refusée)
    [HttpDelete]
    public async Task<IActionResult> DesactiverCompte()
    {
        Utilisateur u = await db.Utilisateurs.FirstAsync(x => x.Id == UtilisateurId);
        u.Actif = false;
        u.ModifieLe = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return NoContent();
    }

    private async Task<ParametresUtilisateur> ObtenirOuCreerParametresAsync()
    {
        ParametresUtilisateur? p = await db.ParametresUtilisateurs.FirstOrDefaultAsync(x => x.UtilisateurId == UtilisateurId);
        if (p is null)
        {
            p = new ParametresUtilisateur { UtilisateurId = UtilisateurId };
            db.ParametresUtilisateurs.Add(p);
            await db.SaveChangesAsync();
        }
        return p;
    }
}
