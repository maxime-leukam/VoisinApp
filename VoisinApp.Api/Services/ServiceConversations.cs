namespace VoisinApp.Api.Services;

public class ServiceConversations(AppDbContext db)
{
    public const string TitreEquipe = "Équipe EntreNous";

    // Retrouve la conversation privée entre deux personnes (pour une annonce ou une demande donnée),
    // ou la crée si elle n'existe pas encore.
    public async Task<Conversation> ObtenirOuCreerPriveeAsync(long a, long b, long? annonceId, long? demandeAideId)
    {
        Conversation? existante = await db.Conversations.FirstOrDefaultAsync(c =>
            c.Type == "privee"
            && c.AnnonceId == annonceId
            && c.DemandeAideId == demandeAideId
            && c.Membres.Any(m => m.UtilisateurId == a)
            && c.Membres.Any(m => m.UtilisateurId == b));

        if (existante is not null)
            return existante;

        var conversation = new Conversation
        {
            Type = "privee",
            AnnonceId = annonceId,
            DemandeAideId = demandeAideId,
            CreePar = a
        };
        conversation.Membres.Add(new MembreConversation { UtilisateurId = a, DerniereLectureLe = DateTime.UtcNow });
        conversation.Membres.Add(new MembreConversation { UtilisateurId = b });

        db.Conversations.Add(conversation);
        await db.SaveChangesAsync();
        return conversation;
    }

    // Ajoute un message de "Équipe EntreNous" dans la conversation système de l'utilisateur
    // (créée au besoin). Enregistre immédiatement.
    public async Task AjouterMessageSystemeAsync(long utilisateurId, string texte)
    {
        Conversation? conversation = await db.Conversations.FirstOrDefaultAsync(c =>
            c.Type == "systeme" && c.Membres.Any(m => m.UtilisateurId == utilisateurId));

        if (conversation is null)
        {
            conversation = new Conversation { Type = "systeme", Titre = TitreEquipe };
            conversation.Membres.Add(new MembreConversation { UtilisateurId = utilisateurId });
            db.Conversations.Add(conversation);
        }

        conversation.Messages.Add(new Message { ExpediteurId = null, Contenu = texte });
        await db.SaveChangesAsync();
    }
}
