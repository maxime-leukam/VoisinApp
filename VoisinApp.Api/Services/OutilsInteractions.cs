namespace VoisinApp.Api.Services;

public record CompteursInteractions(
    Dictionary<long, int> Jaimes,
    Dictionary<long, int> Commentaires,
    HashSet<long> AJaime);

// Compte les j'aime et les commentaires d'une liste d'éléments (annonces, demandes, événements).
public static class OutilsInteractions
{
    public static async Task<CompteursInteractions> ChargerAsync(
        AppDbContext db, string typeCible, List<long> ids, long utilisateurId)
    {
        if (ids.Count == 0)
            return new CompteursInteractions(new Dictionary<long, int>(), new Dictionary<long, int>(), new HashSet<long>());

        Dictionary<long, int> jaimes = await db.Jaimes
            .Where(j => j.TypeCible == typeCible && ids.Contains(j.CibleId))
            .GroupBy(j => j.CibleId)
            .Select(g => new { Id = g.Key, N = g.Count() })
            .ToDictionaryAsync(x => x.Id, x => x.N);

        Dictionary<long, int> commentaires = await db.Commentaires
            .Where(c => c.TypeCible == typeCible && ids.Contains(c.CibleId))
            .GroupBy(c => c.CibleId)
            .Select(g => new { Id = g.Key, N = g.Count() })
            .ToDictionaryAsync(x => x.Id, x => x.N);

        List<long> aJaimeListe = await db.Jaimes
            .Where(j => j.TypeCible == typeCible && j.UtilisateurId == utilisateurId && ids.Contains(j.CibleId))
            .Select(j => j.CibleId)
            .ToListAsync();

        return new CompteursInteractions(jaimes, commentaires, aJaimeListe.ToHashSet());
    }
}
