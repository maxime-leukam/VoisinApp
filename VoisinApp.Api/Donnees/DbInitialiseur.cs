namespace VoisinApp.Api.Donnees;

public static class DbInitialiseur
{
    // Crée le fichier SQLite et les tables au premier lancement, puis ajoute les quartiers de départ
    // (les quartiers manquants sont ajoutés à chaque démarrage, sans toucher aux existants).
    public static void Initialiser(AppDbContext db)
    {
        db.Database.EnsureCreated();

        foreach (var (nomVille, quartiers) in VillesCameroun.QuartiersDeDepart)
        {
            VilleInfo? ville = VillesCameroun.Trouver(nomVille);
            if (ville is null)
                continue;

            foreach (string nom in quartiers)
            {
                if (!db.Quartiers.Any(q => q.Nom == nom && q.Ville == ville.Nom))
                {
                    // Position approximative : le centre de la ville
                    db.Quartiers.Add(new Quartier
                    {
                        Nom = nom,
                        Ville = ville.Nom,
                        Latitude = ville.Latitude,
                        Longitude = ville.Longitude
                    });
                }
            }
        }
        db.SaveChanges();

        // Quartiers de la maquette : arrondissement et position plus précise
        AffinerQuartier(db, "Bonamoussadi", "Douala", "Douala V", 4.0870, 9.7340);
        AffinerQuartier(db, "Makepe", "Douala", "Douala V", 4.0720, 9.7420);
        db.SaveChanges();
    }

    private static void AffinerQuartier(AppDbContext db, string nom, string ville, string arrondissement, double latitude, double longitude)
    {
        Quartier? quartier = db.Quartiers.FirstOrDefault(q => q.Nom == nom && q.Ville == ville);
        if (quartier is not null && quartier.Arrondissement is null)
        {
            quartier.Arrondissement = arrondissement;
            quartier.Latitude = latitude;
            quartier.Longitude = longitude;
        }
    }
}
