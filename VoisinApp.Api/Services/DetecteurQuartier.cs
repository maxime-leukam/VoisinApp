namespace VoisinApp.Api.Services;

// Retrouve un quartier existant à partir d'une adresse saisie (utilisé quand on modifie son adresse dans le profil).
public static class DetecteurQuartier
{
    public static async Task<Quartier?> TrouverAsync(AppDbContext db, string? adresse, string? ville)
    {
        List<Quartier> quartiers = await db.Quartiers.ToListAsync();
        string a = Textes.Normaliser(adresse);
        string v = Textes.Normaliser(ville);

        // 1) Le nom du quartier apparaît dans l'adresse (ou dans la ville saisie)
        List<Quartier> parNom = quartiers
            .Where(q =>
            {
                string nom = Textes.Normaliser(q.Nom);
                return (a.Length > 0 && a.Contains(nom)) || (v.Length > 0 && v.Contains(nom));
            })
            .ToList();

        if (parNom.Count == 1)
            return parNom[0];
        if (parNom.Count > 1 && v.Length > 0)
            return parNom.FirstOrDefault(q => Textes.Normaliser(q.Ville) == v) ?? parNom[0];
        if (parNom.Count > 1)
            return parNom[0];

        // 2) La ville ou l'arrondissement saisi ne correspond qu'à un seul quartier connu
        if (v.Length > 0)
        {
            List<Quartier> memeZone = quartiers
                .Where(q => Textes.Normaliser(q.Ville) == v || Textes.Normaliser(q.Arrondissement) == v)
                .ToList();
            if (memeZone.Count == 1)
                return memeZone[0];
        }

        return null;
    }
}
