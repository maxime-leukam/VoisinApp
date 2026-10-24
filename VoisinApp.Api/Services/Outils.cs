using System.Globalization;
using System.Net.Mail;
using System.Text;

namespace VoisinApp.Api.Services;

// Valeurs autorisées (codes stockés en base)
public static class Valeurs
{
    public static readonly HashSet<string> TypesAnnonce = new() { "a_vendre", "a_donner", "service" };
    public static readonly HashSet<string> StatutsAnnonce = new() { "active", "vendue", "donnee", "cloturee" };
    public static readonly HashSet<string> CategoriesDemande = new() { "bricolage", "transport", "courses", "autre" };
    public static readonly HashSet<string> CategoriesSignalement = new() { "voirie", "proprete", "eclairage", "securite" };
    public static readonly HashSet<string> TypesCible = new() { "annonce", "demande_aide", "evenement" };
}

// Libellés français affichés dans l'application
public static class Libelles
{
    public static string TypeAnnonce(string t) => t switch
    {
        "a_vendre" => "À vendre",
        "a_donner" => "À donner",
        "service" => "Services",
        _ => t
    };

    public static string EtiquetteAnnonce(string t) => t switch
    {
        "a_vendre" => "À VENDRE",
        "a_donner" => "À DONNER",
        "service" => "SERVICES",
        _ => t.ToUpperInvariant()
    };

    public static string CategorieDemande(string c) => c switch
    {
        "bricolage" => "Bricolage",
        "transport" => "Transport",
        "courses" => "Courses",
        "autre" => "Autre",
        _ => c
    };

    public static string StatutDemande(string s) => s switch
    {
        "ouverte" => "Ouverte",
        "en_cours" => "En cours",
        "terminee" => "Terminée",
        "annulee" => "Annulée",
        _ => s
    };

    public static string CategorieSignalement(string c) => c switch
    {
        "voirie" => "Voirie",
        "proprete" => "Propreté",
        "eclairage" => "Éclairage",
        "securite" => "Sécurité",
        _ => c
    };

    public static string StatutSignalement(string s) => s switch
    {
        "recu" => "Reçu",
        "en_cours_examen" => "En cours d'examen",
        "resolu" => "Résolu",
        "rejete" => "Rejeté",
        _ => s
    };
}

// Dates et heures en français, à l'heure du Cameroun (UTC+1, sans heure d'été)
public static class Temps
{
    public static readonly TimeSpan Decalage = TimeSpan.FromHours(1);
    private static readonly CultureInfo Fr = new("fr-FR");

    public static DateTime EnLocal(DateTime utc) =>
        DateTime.SpecifyKind(utc.Add(Decalage), DateTimeKind.Unspecified);

    public static DateOnly AujourdhuiLocal() => DateOnly.FromDateTime(EnLocal(DateTime.UtcNow));

    // Début et fin (en UTC) d'un jour local
    public static (DateTime DebutUtc, DateTime FinUtc) BornesDuJour(DateOnly jour)
    {
        DateTime minuitLocal = jour.ToDateTime(TimeOnly.MinValue);
        DateTime debut = DateTime.SpecifyKind(minuitLocal - Decalage, DateTimeKind.Utc);
        return (debut, debut.AddDays(1));
    }

    private static string Majuscule(string texte) =>
        texte.Length == 0 ? texte : char.ToUpper(texte[0], Fr) + texte[1..];

    public static string Heure(DateTime utc) =>
        EnLocal(utc).ToString("HH:mm", CultureInfo.InvariantCulture);

    // "il y a 18 min", "il y a 2 h", "Hier", "Lundi", "17 mai"
    public static string DateRelative(DateTime utc)
    {
        DateTime maintenant = DateTime.UtcNow;
        TimeSpan ecart = maintenant - utc;

        if (ecart < TimeSpan.FromMinutes(1)) return "à l'instant";
        if (ecart < TimeSpan.FromHours(1)) return $"il y a {(int)ecart.TotalMinutes} min";

        DateTime local = EnLocal(utc);
        int jours = (EnLocal(maintenant).Date - local.Date).Days;

        if (jours <= 0) return $"il y a {(int)ecart.TotalHours} h";
        if (jours == 1) return "Hier";
        if (jours < 7) return Majuscule(Fr.DateTimeFormat.GetDayName(local.DayOfWeek));

        return local.Year == EnLocal(maintenant).Year
            ? local.ToString("d MMMM", Fr)
            : local.ToString("d MMMM yyyy", Fr);
    }

    // Liste des conversations : "10:24", "Hier", "Lun.", "18 mai"
    public static string HeureConversation(DateTime utc)
    {
        DateTime local = EnLocal(utc);
        int jours = (EnLocal(DateTime.UtcNow).Date - local.Date).Days;

        if (jours <= 0) return local.ToString("HH:mm", CultureInfo.InvariantCulture);
        if (jours == 1) return "Hier";
        if (jours < 7) return Majuscule(Fr.DateTimeFormat.GetAbbreviatedDayName(local.DayOfWeek));
        return local.ToString("d MMMM", Fr);
    }

    // En-tête de groupe de messages : "AUJOURD'HUI", "HIER", "18 MAI"
    public static string JourLibelle(DateTime utc)
    {
        DateTime local = EnLocal(utc);
        int jours = (EnLocal(DateTime.UtcNow).Date - local.Date).Days;

        if (jours <= 0) return "AUJOURD'HUI";
        if (jours == 1) return "HIER";
        return local.ToString("d MMMM", Fr).ToUpper(Fr);
    }

    // "SAMEDI · 14:00"
    public static string JourHeure(DateTime utc)
    {
        DateTime local = EnLocal(utc);
        string jour = Fr.DateTimeFormat.GetDayName(local.DayOfWeek).ToUpper(Fr);
        return $"{jour} · {local.ToString("HH:mm", CultureInfo.InvariantCulture)}";
    }

    // "Samedi 17 mai"
    public static string DateLongue(DateTime utc)
    {
        DateTime local = EnLocal(utc);
        return $"{Majuscule(Fr.DateTimeFormat.GetDayName(local.DayOfWeek))} {local.ToString("d MMMM", Fr)}";
    }

    // "De 14:00 à 21:00"
    public static string Horaires(DateTime debut, DateTime? fin) =>
        fin.HasValue ? $"De {Heure(debut)} à {Heure(fin.Value)}" : $"À partir de {Heure(debut)}";

    public static string MoisCourt(DateTime utc) =>
        EnLocal(utc).ToString("MMM", Fr).TrimEnd('.').ToUpper(Fr);

    public static int Jour(DateTime utc) => EnLocal(utc).Day;

    // "En ligne", "En ligne il y a 5 min", "Vu hier"
    public static string TexteActivite(DateTime? derniere)
    {
        if (derniere is null) return "";

        TimeSpan ecart = DateTime.UtcNow - derniere.Value;
        if (ecart < TimeSpan.FromMinutes(2)) return "En ligne";
        if (ecart < TimeSpan.FromHours(1)) return $"En ligne il y a {(int)ecart.TotalMinutes} min";
        if (ecart < TimeSpan.FromHours(24)) return $"En ligne il y a {(int)ecart.TotalHours} h";
        return $"Vu {DateRelative(derniere.Value).ToLowerInvariant()}";
    }
}

// Distances GPS
public static class Geo
{
    private static readonly CultureInfo Fr = new("fr-FR");

    public static double Metres(double lat1, double lon1, double lat2, double lon2)
    {
        const double rayonTerre = 6371000;
        double dLat = Radians(lat2 - lat1);
        double dLon = Radians(lon2 - lon1);
        double a = Math.Pow(Math.Sin(dLat / 2), 2) +
                   Math.Cos(Radians(lat1)) * Math.Cos(Radians(lat2)) * Math.Pow(Math.Sin(dLon / 2), 2);
        return 2 * rayonTerre * Math.Asin(Math.Min(1, Math.Sqrt(a)));
    }

    private static double Radians(double degres) => degres * Math.PI / 180;

    // "350 m", "1 km", "1,2 km"
    public static string Texte(double metres)
    {
        if (metres < 1000)
        {
            double arrondi = Math.Max(10, Math.Round(metres / 10) * 10);
            return $"{arrondi:0} m";
        }
        return (metres / 1000).ToString("0.#", Fr) + " km";
    }

    public static int MinutesAPied(double metres) => Math.Max(1, (int)Math.Round(metres / 80));
}

// Textes, noms, prix, validations
public static class Textes
{
    private static readonly CultureInfo Fr = new("fr-FR");
    private static readonly string[] CouleursAvatar = { "#FBE0D8", "#DCEBE2", "#DCEAF0", "#F7E9C6" };

    public static string NomComplet(string prenom, string nom) => $"{prenom} {nom}".Trim();

    public static string Initiales(string prenom, string nom)
    {
        string a = prenom.Length > 0 ? prenom[..1] : "";
        string b = nom.Length > 0 ? nom[..1] : "";
        string resultat = (a + b).ToUpperInvariant();
        return resultat.Length > 0 ? resultat : "?";
    }

    // Initiales d'un titre : "Équipe EntreNous" -> "ÉE"
    public static string InitialesDe(string texte)
    {
        var lettres = texte
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(mot => char.IsLetter(mot[0]))
            .Take(2)
            .Select(mot => char.ToUpperInvariant(mot[0]));
        string resultat = string.Concat(lettres);
        return resultat.Length > 0 ? resultat : "?";
    }

    public static string CouleurAvatar(long id) =>
        CouleursAvatar[(int)(Math.Abs(id) % CouleursAvatar.Length)];

    // Minuscules sans accents, pour comparer des textes
    public static string Normaliser(string? texte)
    {
        if (string.IsNullOrWhiteSpace(texte)) return "";

        string decompose = texte.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (char c in decompose)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    public static string Km(double km) => km.ToString("0.#", Fr) + " km";

    // "  mvog   ADA " -> "Mvog Ada"
    public static string NomPropre(string texte)
    {
        string compact = System.Text.RegularExpressions.Regex.Replace(texte.Trim(), @"\s+", " ");
        return Fr.TextInfo.ToTitleCase(compact.ToLower(Fr));
    }

    // 35000 -> "35 000"
    public static string Montant(long montant) =>
        montant.ToString("N0", Fr).Replace('\u202f', ' ').Replace('\u00a0', ' ');

    // "35 000 FCFA", "Gratuit", "3 000 FCFA / h"
    public static string PrixTexte(string type, long? prix, string? unite)
    {
        if (type == "a_donner") return "Gratuit";
        if (prix is null) return "Prix à discuter";
        if (prix == 0) return "Gratuit";

        string texte = $"{Montant(prix.Value)} FCFA";
        return string.IsNullOrWhiteSpace(unite) ? texte : $"{texte} / {unite}";
    }

    public static bool EmailValide(string? email)
    {
        if (string.IsNullOrWhiteSpace(email) || email.Length > 254) return false;
        try
        {
            return new MailAddress(email).Address == email;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public static string Tronquer(string texte, int max) =>
        texte.Length <= max ? texte : texte[..(max - 1)] + "…";
}
