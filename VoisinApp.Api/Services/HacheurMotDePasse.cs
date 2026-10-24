using System.Security.Cryptography;

namespace VoisinApp.Api.Services;

// Hachage PBKDF2 (intégré à .NET) : le mot de passe n'est jamais stocké en clair.
// Format stocké : v1.<itérations>.<sel>.<empreinte>
public static class HacheurMotDePasse
{
    private const int TailleSel = 16;
    private const int TailleEmpreinte = 32;
    private const int Iterations = 210_000;

    public static string Hacher(string motDePasse)
    {
        byte[] sel = RandomNumberGenerator.GetBytes(TailleSel);
        byte[] empreinte = Rfc2898DeriveBytes.Pbkdf2(motDePasse, sel, Iterations, HashAlgorithmName.SHA256, TailleEmpreinte);
        return $"v1.{Iterations}.{Convert.ToBase64String(sel)}.{Convert.ToBase64String(empreinte)}";
    }

    public static bool Verifier(string motDePasse, string hacheStocke)
    {
        string[] parties = hacheStocke.Split('.');
        if (parties.Length != 4 || parties[0] != "v1" || !int.TryParse(parties[1], out int iterations))
            return false;

        try
        {
            byte[] sel = Convert.FromBase64String(parties[2]);
            byte[] attendu = Convert.FromBase64String(parties[3]);
            byte[] calcule = Rfc2898DeriveBytes.Pbkdf2(motDePasse, sel, iterations, HashAlgorithmName.SHA256, attendu.Length);
            return CryptographicOperations.FixedTimeEquals(calcule, attendu);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
