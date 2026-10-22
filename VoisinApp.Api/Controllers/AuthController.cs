using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.RateLimiting;

namespace VoisinApp.Api.Controllers;

[Route("api/auth")]
[EnableRateLimiting("auth")]
public class AuthController(
    AppDbContext db,
    ServiceJeton jetons,
    IServiceEmail email,
    ServiceConversations conversations,
    ServiceProfil profils,
    IWebHostEnvironment env) : ControleurDeBase
{
    // Page "Créer mon compte"
    [AllowAnonymous]
    [HttpPost("inscription")]
    public async Task<ActionResult<ReponseAuthDto>> Inscription(InscriptionDto dto)
    {
        string? prenom = dto.Prenom?.Trim();
        string? nom = dto.Nom?.Trim();
        string? adresseEmail = dto.Email?.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(prenom) || string.IsNullOrWhiteSpace(nom))
            return Erreur(400, "Le prénom et le nom sont obligatoires.");
        if (!Textes.EmailValide(adresseEmail))
            return Erreur(400, "Adresse e-mail invalide.");
        if (string.IsNullOrEmpty(dto.MotDePasse) || dto.MotDePasse.Length < 8)
            return Erreur(400, "Le mot de passe doit contenir au moins 8 caractères.");
        if (!dto.CharteAcceptee)
            return Erreur(400, "Vous devez certifier résider dans la zone et accepter la charte de bon voisinage.");
        if (await db.Utilisateurs.AnyAsync(u => u.Email == adresseEmail))
            return Erreur(409, "Un compte existe déjà avec cette adresse e-mail.");

        VilleInfo? ville = VillesCameroun.Trouver(dto.Ville);
        string? nomQuartier = dto.Quartier?.Trim();
        string? adresse = dto.Adresse?.Trim();

        if (ville is null)
            return Erreur(400, "Choisissez votre ville dans la liste.");
        if (string.IsNullOrWhiteSpace(nomQuartier) || nomQuartier.Length < 2)
            return Erreur(400, "Indiquez le nom de votre quartier.");
        if (nomQuartier.Length > 60)
            return Erreur(400, "Le nom du quartier est trop long (60 caractères maximum).");
        if (string.IsNullOrWhiteSpace(adresse))
            return Erreur(400, "Indiquez votre adresse ou lieu-dit.");

        Quartier quartier = await ObtenirOuCreerQuartierAsync(ville, nomQuartier, dto.Arrondissement?.Trim());

        var utilisateur = new Utilisateur
        {
            Prenom = prenom,
            Nom = nom,
            Email = adresseEmail!,
            Telephone = dto.Telephone?.Trim(),
            MotDePasseHache = HacheurMotDePasse.Hacher(dto.MotDePasse),
            Adresse = $"{quartier.Nom}, {adresse}", // forme "quartier, adresse"
            QuartierId = quartier.Id,
            CharteAccepteeLe = DateTime.UtcNow,
            AdresseVerifiee = true, // quartier reconnu à partir de l'adresse
            DerniereActiviteLe = DateTime.UtcNow
        };
        db.Utilisateurs.Add(utilisateur);
        await db.SaveChangesAsync();

        db.ParametresUtilisateurs.Add(new ParametresUtilisateur { UtilisateurId = utilisateur.Id });
        await db.SaveChangesAsync();

        await conversations.AjouterMessageSystemeAsync(utilisateur.Id,
            $"Bienvenue sur EntreNous, {prenom} ! Retrouvez ici les messages de l'équipe.");

        await EnvoyerVerificationEmailAsync(utilisateur);

        var (jeton, expire) = jetons.CreerJetonAcces(utilisateur);
        return Ok(new ReponseAuthDto(jeton, expire, await profils.ConstruireAsync(utilisateur.Id, UrlAbsolue)));
    }

    // Page "Connexion"
    [AllowAnonymous]
    [HttpPost("connexion")]
    public async Task<ActionResult<ReponseAuthDto>> Connexion(ConnexionDto dto)
    {
        string? adresseEmail = dto.Email?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(adresseEmail) || string.IsNullOrEmpty(dto.MotDePasse))
            return Erreur(400, "Saisissez votre e-mail et votre mot de passe.");

        Utilisateur? utilisateur = await db.Utilisateurs.FirstOrDefaultAsync(u => u.Email == adresseEmail);
        if (utilisateur is null || !utilisateur.Actif || !HacheurMotDePasse.Verifier(dto.MotDePasse, utilisateur.MotDePasseHache))
            return Erreur(401, "E-mail ou mot de passe incorrect.");

        utilisateur.DerniereActiviteLe = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var (jeton, expire) = jetons.CreerJetonAcces(utilisateur);
        return Ok(new ReponseAuthDto(jeton, expire, await profils.ConstruireAsync(utilisateur.Id, UrlAbsolue)));
    }

    // "Mot de passe oublié ?" : génère un code de 8 caractères (affiché dans la console de l'API en développement)
    [AllowAnonymous]
    [HttpPost("mot-de-passe-oublie")]
    public async Task<ActionResult<ReponseSimpleDto>> MotDePasseOublie(MotDePasseOublieDto dto)
    {
        const string message = "Si un compte existe avec cette adresse, un code de réinitialisation vient d'être envoyé.";
        string? adresseEmail = dto.Email?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(adresseEmail))
            return Erreur(400, "Saisissez votre adresse e-mail.");

        Utilisateur? utilisateur = await db.Utilisateurs.FirstOrDefaultAsync(u => u.Email == adresseEmail && u.Actif);
        if (utilisateur is null)
            return Ok(new ReponseSimpleDto(message)); // même réponse : on ne révèle pas si le compte existe

        string code = GenererCode();
        db.JetonsReinitialisation.Add(new JetonReinitialisation
        {
            UtilisateurId = utilisateur.Id,
            JetonHache = HacherCode(code),
            ExpireLe = DateTime.UtcNow.AddMinutes(30)
        });
        await db.SaveChangesAsync();

        await email.EnvoyerAsync(utilisateur.Email, "Réinitialisation de votre mot de passe",
            $"Bonjour {utilisateur.Prenom},\n\nVotre code de réinitialisation : {code}\nIl est valable 30 minutes.");

        return Ok(new ReponseSimpleDto(message, env.IsDevelopment() ? code : null));
    }

    [AllowAnonymous]
    [HttpPost("reinitialiser-mot-de-passe")]
    public async Task<ActionResult<ReponseSimpleDto>> Reinitialiser(ReinitialisationDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Code))
            return Erreur(400, "Saisissez le code reçu par e-mail.");
        if (string.IsNullOrEmpty(dto.NouveauMotDePasse) || dto.NouveauMotDePasse.Length < 8)
            return Erreur(400, "Le mot de passe doit contenir au moins 8 caractères.");

        string code = dto.Code.Trim().Replace(" ", "").Replace("-", "").ToUpperInvariant();
        string empreinte = HacherCode(code);
        DateTime maintenant = DateTime.UtcNow;

        JetonReinitialisation? jeton = await db.JetonsReinitialisation
            .FirstOrDefaultAsync(j => j.JetonHache == empreinte && j.UtiliseLe == null && j.ExpireLe > maintenant);
        if (jeton is null)
            return Erreur(400, "Code invalide ou expiré.");

        Utilisateur utilisateur = await db.Utilisateurs.FirstAsync(u => u.Id == jeton.UtilisateurId);
        utilisateur.MotDePasseHache = HacheurMotDePasse.Hacher(dto.NouveauMotDePasse);
        utilisateur.ModifieLe = maintenant;
        jeton.UtiliseLe = maintenant;
        await db.SaveChangesAsync();

        return Ok(new ReponseSimpleDto("Mot de passe modifié. Vous pouvez vous connecter."));
    }

    // Lien reçu par e-mail
    [AllowAnonymous]
    [HttpGet("verifier-email")]
    public async Task<IActionResult> VerifierEmail(string? jeton)
    {
        long? id = string.IsNullOrWhiteSpace(jeton) ? null : await jetons.LireJetonVerificationEmailAsync(jeton);
        if (id is null)
            return Content("<html><body style='font-family:sans-serif;text-align:center;margin-top:60px'><h2>Lien invalide ou expiré</h2></body></html>", "text/html; charset=utf-8");

        Utilisateur? utilisateur = await db.Utilisateurs.FirstOrDefaultAsync(u => u.Id == id.Value);
        if (utilisateur is not null)
        {
            utilisateur.EmailVerifie = true;
            await db.SaveChangesAsync();
        }

        return Content("<html><body style='font-family:sans-serif;text-align:center;margin-top:60px'><h2>✓ Adresse e-mail confirmée</h2><p>Vous pouvez retourner dans l'application EntreNous.</p></body></html>", "text/html; charset=utf-8");
    }

    [HttpPost("renvoyer-verification-email")]
    public async Task<ActionResult<ReponseSimpleDto>> RenvoyerVerification()
    {
        Utilisateur utilisateur = await db.Utilisateurs.FirstAsync(u => u.Id == UtilisateurId);
        if (utilisateur.EmailVerifie)
            return Ok(new ReponseSimpleDto("Votre adresse e-mail est déjà confirmée."));

        await EnvoyerVerificationEmailAsync(utilisateur);
        return Ok(new ReponseSimpleDto("E-mail de confirmation envoyé."));
    }

    // Retrouve le quartier de cette ville (sans tenir compte des accents ni des majuscules) ou le crée.
    private async Task<Quartier> ObtenirOuCreerQuartierAsync(VilleInfo ville, string nom, string? arrondissement)
    {
        string cle = Textes.Normaliser(nom);
        List<Quartier> dansLaVille = await db.Quartiers.Where(q => q.Ville == ville.Nom).ToListAsync();

        Quartier? existant = dansLaVille.FirstOrDefault(q => Textes.Normaliser(q.Nom) == cle);
        if (existant is not null)
            return existant;

        var nouveau = new Quartier
        {
            Nom = Textes.NomPropre(nom),
            Ville = ville.Nom,
            Arrondissement = string.IsNullOrWhiteSpace(arrondissement) ? null : arrondissement,
            Latitude = ville.Latitude,   // position approximative : le centre de la ville
            Longitude = ville.Longitude
        };
        db.Quartiers.Add(nouveau);
        await db.SaveChangesAsync();
        return nouveau;
    }

    private async Task EnvoyerVerificationEmailAsync(Utilisateur utilisateur)
    {
        string jeton = jetons.CreerJetonVerificationEmail(utilisateur.Id);
        string lien = $"{Request.Scheme}://{Request.Host}/api/auth/verifier-email?jeton={Uri.EscapeDataString(jeton)}";
        await email.EnvoyerAsync(utilisateur.Email, "Confirmez votre adresse e-mail",
            $"Bonjour {utilisateur.Prenom},\n\nConfirmez votre adresse e-mail en ouvrant ce lien :\n{lien}");
    }

    private static string GenererCode()
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // sans 0, O, 1, I
        var caracteres = new char[8];
        for (int i = 0; i < caracteres.Length; i++)
            caracteres[i] = alphabet[RandomNumberGenerator.GetInt32(alphabet.Length)];
        return new string(caracteres);
    }

    private static string HacherCode(string code) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(code)));
}
