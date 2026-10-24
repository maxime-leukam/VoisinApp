namespace VoisinApp.Api.Services;

public record VilleInfo(string Nom, double Latitude, double Longitude);

// Villes proposées à l'inscription (coordonnées approximatives du centre-ville).
// Pour ajouter une ville : ajoutez une ligne dans "Toutes".
public static class VillesCameroun
{
    public static readonly IReadOnlyList<VilleInfo> Toutes = new List<VilleInfo>
    {
        new("Douala", 4.0511, 9.7679),
        new("Yaoundé", 3.8480, 11.5021),
        new("Bafoussam", 5.4737, 10.4179),
        new("Bamenda", 5.9597, 10.1460),
        new("Garoua", 9.3017, 13.3921),
        new("Maroua", 10.5910, 14.3157),
        new("Ngaoundéré", 7.3167, 13.5833),
        new("Bertoua", 4.5772, 13.6847),
        new("Ebolowa", 2.9000, 11.1500),
        new("Kribi", 2.9400, 9.9100),
        new("Limbé", 4.0240, 9.2020),
        new("Buea", 4.1534, 9.2423),
        new("Edéa", 3.8000, 10.1333),
        new("Nkongsamba", 4.9547, 9.9404),
        new("Dschang", 5.4440, 10.0530),
        new("Foumban", 5.7267, 10.9000),
        new("Kumba", 4.6363, 9.4469),
        new("Bafang", 5.1500, 10.1833),
        new("Mbouda", 5.6280, 10.2540)
    };

    // Quartiers proposés dès le départ. Les autres sont créés automatiquement
    // à la première inscription d'un habitant (ou ajoutez-les ici).
    public static readonly IReadOnlyDictionary<string, string[]> QuartiersDeDepart = new Dictionary<string, string[]>
    {
        ["Douala"] = new[]
        {
            "Akwa", "Bali", "Bepanda", "Bonaberi", "Bonamoussadi", "Bonanjo", "Bonapriso", "Cité des Palmiers",
            "Deido", "Japoma", "Kotto", "Logbaba", "Logpom", "Makepe", "New Bell", "Ndokoti", "Nyalla", "Yassa"
        },
        ["Yaoundé"] = new[]
        {
            "Bastos", "Biyem-Assi", "Briqueterie", "Ekounou", "Elig-Edzoa", "Emana", "Essos", "Etoudi", "Melen",
            "Mendong", "Messa", "Mfandena", "Mimboman", "Mokolo", "Mvan", "Mvog-Ada", "Mvog-Mbi", "Ngoa-Ekelle",
            "Nkolbisson", "Nlongkak", "Nsimeyong", "Obili", "Odza", "Omnisports", "Tsinga"
        }
    };

    public static VilleInfo? Trouver(string? nom)
    {
        string cle = Textes.Normaliser(nom);
        if (cle.Length == 0)
            return null;

        return Toutes.FirstOrDefault(v => Textes.Normaliser(v.Nom) == cle);
    }
}
