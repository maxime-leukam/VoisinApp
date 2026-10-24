namespace VoisinApp.Api.Services;

// Enregistre les images envoyées (photos d'annonces, avatars, couvertures d'événements)
// dans wwwroot/uploads. La base ne garde que l'adresse relative, ex. "/uploads/annonces/abc.jpg".
public class ServiceFichiers(IWebHostEnvironment env)
{
    private static readonly string[] Extensions = { ".jpg", ".jpeg", ".png", ".webp" };
    private const long TailleMax = 12 * 1024 * 1024; // 12 Mo

    private string RacineUploads =>
        Path.Combine(env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot"), "uploads");

    public async Task<(string? Url, string? Erreur)> EnregistrerImageAsync(IFormFile fichier, string dossier)
    {
        if (fichier.Length == 0)
            return (null, "Le fichier est vide.");
        if (fichier.Length > TailleMax)
            return (null, "Image trop lourde (12 Mo maximum).");

        string extension = Path.GetExtension(fichier.FileName).ToLowerInvariant();
        if (!Extensions.Contains(extension))
            return (null, "Format non pris en charge (JPG, PNG ou WEBP).");

        // On vérifie le contenu réel du fichier, pas seulement son extension.
        byte[] entete = new byte[12];
        try
        {
            await using Stream lecture = fichier.OpenReadStream();
            await lecture.ReadExactlyAsync(entete, 0, entete.Length);
        }
        catch (EndOfStreamException)
        {
            return (null, "Fichier image invalide.");
        }

        if (!SignatureValide(entete))
            return (null, "Le fichier n'est pas une image valide.");

        string dossierCible = Path.Combine(RacineUploads, dossier);
        Directory.CreateDirectory(dossierCible);

        string nom = $"{Guid.NewGuid():N}{extension}";
        await using (FileStream flux = File.Create(Path.Combine(dossierCible, nom)))
        {
            await fichier.CopyToAsync(flux);
        }

        return ($"/uploads/{dossier}/{nom}", null);
    }

    public void Supprimer(string? urlRelative)
    {
        if (string.IsNullOrWhiteSpace(urlRelative) || !urlRelative.StartsWith("/uploads/"))
            return;

        string racine = Path.GetFullPath(RacineUploads);
        string chemin = Path.GetFullPath(Path.Combine(racine, urlRelative["/uploads/".Length..]));

        // Sécurité : on ne supprime jamais en dehors du dossier uploads.
        if (chemin.StartsWith(racine, StringComparison.Ordinal) && File.Exists(chemin))
            File.Delete(chemin);
    }

    private static bool SignatureValide(byte[] e)
    {
        bool jpeg = e[0] == 0xFF && e[1] == 0xD8 && e[2] == 0xFF;
        bool png = e[0] == 0x89 && e[1] == 0x50 && e[2] == 0x4E && e[3] == 0x47;
        bool webp = e[0] == 'R' && e[1] == 'I' && e[2] == 'F' && e[3] == 'F'
                    && e[8] == 'W' && e[9] == 'E' && e[10] == 'B' && e[11] == 'P';
        return jpeg || png || webp;
    }
}
