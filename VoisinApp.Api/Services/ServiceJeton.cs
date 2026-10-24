using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace VoisinApp.Api.Services;

public class OptionsJwt
{
    public string Cle { get; set; } = "";
    public string Emetteur { get; set; } = "";
    public string Audience { get; set; } = "";
    public int DureeHeures { get; set; } = 720;
}

// Création et validation des jetons JWT (connexion et vérification de l'e-mail).
public class ServiceJeton
{
    private readonly OptionsJwt _options;

    public ServiceJeton(IConfiguration configuration)
    {
        _options = configuration.GetSection("Jwt").Get<OptionsJwt>()
                   ?? throw new InvalidOperationException("La section \"Jwt\" est absente de appsettings.json.");

        if (Encoding.UTF8.GetByteCount(_options.Cle) < 32)
            throw new InvalidOperationException("La clé Jwt:Cle doit contenir au moins 32 caractères.");
    }

    private SymmetricSecurityKey CleSignature => new(Encoding.UTF8.GetBytes(_options.Cle));

    private string AudienceVerification => _options.Audience + ".verification";

    public TokenValidationParameters ParametresValidation(bool verificationEmail = false) => new()
    {
        ValidateIssuer = true,
        ValidIssuer = _options.Emetteur,
        ValidateAudience = true,
        ValidAudience = verificationEmail ? AudienceVerification : _options.Audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = CleSignature,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(1)
    };

    public (string Jeton, DateTime ExpireLe) CreerJetonAcces(Utilisateur utilisateur)
    {
        DateTime expire = DateTime.UtcNow.AddHours(_options.DureeHeures);
        var descripteur = new SecurityTokenDescriptor
        {
            Issuer = _options.Emetteur,
            Audience = _options.Audience,
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("uid", utilisateur.Id.ToString()),
                new Claim("email", utilisateur.Email)
            }),
            Expires = expire,
            SigningCredentials = new SigningCredentials(CleSignature, SecurityAlgorithms.HmacSha256)
        };
        return (new JsonWebTokenHandler().CreateToken(descripteur), expire);
    }

    public string CreerJetonVerificationEmail(long utilisateurId)
    {
        var descripteur = new SecurityTokenDescriptor
        {
            Issuer = _options.Emetteur,
            Audience = AudienceVerification,
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("uid", utilisateurId.ToString()),
                new Claim("usage", "verification_email")
            }),
            Expires = DateTime.UtcNow.AddDays(2),
            SigningCredentials = new SigningCredentials(CleSignature, SecurityAlgorithms.HmacSha256)
        };
        return new JsonWebTokenHandler().CreateToken(descripteur);
    }

    // Retourne l'identifiant de l'utilisateur si le jeton de vérification est valide.
    public async Task<long?> LireJetonVerificationEmailAsync(string jeton)
    {
        var resultat = await new JsonWebTokenHandler().ValidateTokenAsync(jeton, ParametresValidation(verificationEmail: true));
        if (!resultat.IsValid)
            return null;

        string? usage = resultat.ClaimsIdentity.FindFirst("usage")?.Value;
        string? uid = resultat.ClaimsIdentity.FindFirst("uid")?.Value;
        if (usage != "verification_email" || !long.TryParse(uid, out long id))
            return null;

        return id;
    }
}
