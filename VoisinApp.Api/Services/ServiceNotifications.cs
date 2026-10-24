namespace VoisinApp.Api.Services;

// Ajoute des notifications en respectant les préférences de chaque utilisateur.
// ATTENTION : ces méthodes n'appellent pas SaveChanges, c'est à l'appelant de le faire.
public class ServiceNotifications(AppDbContext db)
{
    public async Task AjouterPourPlusieursAsync(
        IEnumerable<long> utilisateurIds, string type, string titre, string? contenu, string? typeCible, long? cibleId)
    {
        List<long> ids = utilisateurIds.Distinct().ToList();
        if (ids.Count == 0) return;

        var parametres = await db.ParametresUtilisateurs
            .Where(p => ids.Contains(p.UtilisateurId))
            .ToDictionaryAsync(p => p.UtilisateurId);

        foreach (long id in ids)
        {
            if (parametres.TryGetValue(id, out var p) && !Autorise(p, type))
                continue;

            db.Notifications.Add(new Notification
            {
                UtilisateurId = id,
                Type = type,
                Titre = titre,
                Contenu = contenu,
                TypeCible = typeCible,
                CibleId = cibleId
            });
        }
    }

    public Task AjouterAsync(long utilisateurId, string type, string titre, string? contenu, string? typeCible, long? cibleId) =>
        AjouterPourPlusieursAsync(new[] { utilisateurId }, type, titre, contenu, typeCible, cibleId);

    private static bool Autorise(ParametresUtilisateur p, string type) => type switch
    {
        "message" => p.NotifierMessages,
        "demande_aide" => p.NotifierDemandes,
        "evenement" => p.NotifierEvenements,
        _ => true
    };
}
