namespace VoisinApp.Api.Controllers;

[Route("api/conversations")]
public class ConversationsController(
    AppDbContext db, ServiceConversations conversations, ServiceNotifications notifications) : ControleurDeBase
{
    // Page "Messages" : liste des conversations
    [HttpGet]
    public async Task<ActionResult<List<ConversationResumeDto>>> Lister()
    {
        long moi = UtilisateurId;

        List<long> ids = await db.MembresConversation
            .Where(m => m.UtilisateurId == moi)
            .Select(m => m.ConversationId)
            .ToListAsync();
        if (ids.Count == 0)
            return Ok(new List<ConversationResumeDto>());

        List<Conversation> liste = await db.Conversations.Where(c => ids.Contains(c.Id)).ToListAsync();

        var autres = await db.MembresConversation
            .Where(m => ids.Contains(m.ConversationId) && m.UtilisateurId != moi)
            .Select(m => new { m.ConversationId, m.UtilisateurId, m.Utilisateur.Prenom, m.Utilisateur.Nom })
            .ToListAsync();

        List<long> derniersIds = await db.Messages
            .Where(m => ids.Contains(m.ConversationId))
            .GroupBy(m => m.ConversationId)
            .Select(g => g.Max(m => m.Id))
            .ToListAsync();

        var derniers = await db.Messages
            .Where(m => derniersIds.Contains(m.Id))
            .Select(m => new
            {
                m.ConversationId, m.Contenu, m.EnvoyeLe,
                Prenom = m.Expediteur != null ? m.Expediteur.Prenom : null
            })
            .ToListAsync();

        Dictionary<long, int> nonLus = await NonLusParConversationAsync(moi);

        var resultat = new List<(DateTime Tri, ConversationResumeDto Dto)>();
        foreach (Conversation c in liste)
        {
            var dernier = derniers.FirstOrDefault(d => d.ConversationId == c.Id);
            var autre = autres.FirstOrDefault(a => a.ConversationId == c.Id);

            string titre;
            long? autreId = null;
            if (c.Type == "privee")
            {
                titre = autre is null ? "Utilisateur" : Textes.NomComplet(autre.Prenom, autre.Nom);
                autreId = autre?.UtilisateurId;
            }
            else
            {
                titre = c.Titre ?? (c.Type == "systeme" ? ServiceConversations.TitreEquipe : "Groupe");
            }

            string? apercu = dernier?.Contenu;
            if (apercu is not null && c.Type == "groupe" && dernier?.Prenom is not null)
                apercu = $"{dernier.Prenom} : {apercu}";

            DateTime dateTri = dernier?.EnvoyeLe ?? c.CreeLe;
            resultat.Add((dateTri, new ConversationResumeDto(
                c.Id, c.Type, titre, Textes.InitialesDe(titre), Textes.CouleurAvatar(c.Id),
                apercu, dernier?.EnvoyeLe, dernier is null ? "" : Temps.HeureConversation(dernier.EnvoyeLe),
                nonLus.GetValueOrDefault(c.Id), autreId)));
        }

        return Ok(resultat.OrderByDescending(r => r.Tri).Select(r => r.Dto).ToList());
    }

    // Pastille du nombre total de messages non lus
    [HttpGet("non-lus")]
    public async Task<ActionResult<NonLusDto>> NonLus()
    {
        Dictionary<long, int> nonLus = await NonLusParConversationAsync(UtilisateurId);
        return Ok(new NonLusDto(nonLus.Values.Sum()));
    }

    // Nouveau message ("+") : ouvre ou retrouve la conversation avec un voisin
    [HttpPost]
    public async Task<ActionResult<ConversationRefDto>> Creer(CreerConversationDto dto)
    {
        if (dto.DestinataireId == UtilisateurId)
            return Erreur(400, "Vous ne pouvez pas vous écrire à vous-même.");
        if (!await db.Utilisateurs.AnyAsync(u => u.Id == dto.DestinataireId && u.Actif))
            return Erreur(404, "Utilisateur introuvable.");

        Conversation conversation = await conversations.ObtenirOuCreerPriveeAsync(UtilisateurId, dto.DestinataireId, null, null);

        string? texte = dto.Message?.Trim();
        if (!string.IsNullOrWhiteSpace(texte))
        {
            if (texte.Length > 2000)
                return Erreur(400, "Message trop long (2000 caractères maximum).");
            await AjouterMessageAsync(conversation.Id, texte);
        }

        return Ok(new ConversationRefDto(conversation.Id));
    }

    // En-tête de la conversation : nom + "En ligne il y a 5 min"
    [HttpGet("{id:long}")]
    public async Task<ActionResult<ConversationEnteteDto>> Entete(long id)
    {
        if (!await db.MembresConversation.AnyAsync(m => m.ConversationId == id && m.UtilisateurId == UtilisateurId))
            return Erreur(404, "Conversation introuvable.");

        Conversation c = await db.Conversations.FirstAsync(x => x.Id == id);

        if (c.Type == "privee")
        {
            var autre = await db.MembresConversation
                .Where(m => m.ConversationId == id && m.UtilisateurId != UtilisateurId)
                .Select(m => new { m.Utilisateur.Prenom, m.Utilisateur.Nom, m.Utilisateur.DerniereActiviteLe })
                .FirstOrDefaultAsync();

            return Ok(new ConversationEnteteDto(
                id, c.Type, autre is null ? "Utilisateur" : Textes.NomComplet(autre.Prenom, autre.Nom),
                autre is null ? "" : Temps.TexteActivite(autre.DerniereActiviteLe), false));
        }

        int membres = await db.MembresConversation.CountAsync(m => m.ConversationId == id);
        string titre = c.Titre ?? (c.Type == "systeme" ? ServiceConversations.TitreEquipe : "Groupe");
        return Ok(new ConversationEnteteDto(id, c.Type, titre, c.Type == "groupe" ? $"{membres} membres" : "", c.Type == "systeme"));
    }

    // Messages de la conversation (marque la conversation comme lue).
    // apresId : ne renvoie que les messages plus récents (pour rafraîchir régulièrement)
    [HttpGet("{id:long}/messages")]
    public async Task<ActionResult<List<MessageChatDto>>> Messages(long id, long? apresId, int limite = 100)
    {
        long moi = UtilisateurId;
        MembreConversation? membre = await db.MembresConversation
            .FirstOrDefaultAsync(m => m.ConversationId == id && m.UtilisateurId == moi);
        if (membre is null)
            return Erreur(404, "Conversation introuvable.");

        IQueryable<Message> requete = db.Messages.Where(m => m.ConversationId == id);
        if (apresId.HasValue)
            requete = requete.Where(m => m.Id > apresId.Value);

        var lignes = await requete
            .OrderByDescending(m => m.Id)
            .Take(Math.Clamp(limite, 1, 200))
            .Select(m => new { m.Id, m.Contenu, m.EnvoyeLe, m.ExpediteurId, Prenom = m.Expediteur != null ? m.Expediteur.Prenom : null })
            .ToListAsync();
        lignes.Reverse();

        List<DateTime?> lectures = await db.MembresConversation
            .Where(m => m.ConversationId == id && m.UtilisateurId != moi)
            .Select(m => m.DerniereLectureLe)
            .ToListAsync();

        membre.DerniereLectureLe = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Ok(lignes.Select(m => new MessageChatDto(
            m.Id, m.Contenu, m.EnvoyeLe, Temps.Heure(m.EnvoyeLe), Temps.JourLibelle(m.EnvoyeLe),
            m.ExpediteurId == moi,
            m.ExpediteurId == moi && lectures.Any(l => l.HasValue && l.Value >= m.EnvoyeLe),
            m.ExpediteurId == moi ? null : m.Prenom)).ToList());
    }

    // Bouton d'envoi ➤
    [HttpPost("{id:long}/messages")]
    public async Task<ActionResult<MessageChatDto>> Envoyer(long id, EnvoyerMessageDto dto)
    {
        string? texte = dto.Contenu?.Trim();
        if (string.IsNullOrWhiteSpace(texte))
            return Erreur(400, "Écrivez un message.");
        if (texte.Length > 2000)
            return Erreur(400, "Message trop long (2000 caractères maximum).");

        if (!await db.MembresConversation.AnyAsync(m => m.ConversationId == id && m.UtilisateurId == UtilisateurId))
            return Erreur(404, "Conversation introuvable.");

        Conversation c = await db.Conversations.FirstAsync(x => x.Id == id);
        if (c.Type == "systeme")
            return Erreur(403, "Vous ne pouvez pas répondre à cette conversation.");

        Message message = await AjouterMessageAsync(id, texte);
        return Ok(new MessageChatDto(
            message.Id, message.Contenu, message.EnvoyeLe, Temps.Heure(message.EnvoyeLe),
            Temps.JourLibelle(message.EnvoyeLe), true, false, null));
    }

    // ---------- Méthodes privées ----------

    private async Task<Message> AjouterMessageAsync(long conversationId, string texte)
    {
        Utilisateur moi = await db.Utilisateurs.FirstAsync(u => u.Id == UtilisateurId);

        var message = new Message { ConversationId = conversationId, ExpediteurId = moi.Id, Contenu = texte };
        db.Messages.Add(message);

        MembreConversation monAppartenance = await db.MembresConversation
            .FirstAsync(m => m.ConversationId == conversationId && m.UtilisateurId == moi.Id);
        monAppartenance.DerniereLectureLe = DateTime.UtcNow;

        List<long> destinataires = await db.MembresConversation
            .Where(m => m.ConversationId == conversationId && m.UtilisateurId != moi.Id)
            .Select(m => m.UtilisateurId)
            .ToListAsync();
        await notifications.AjouterPourPlusieursAsync(destinataires, "message",
            Textes.NomComplet(moi.Prenom, moi.Nom), Textes.Tronquer(texte, 100), "conversation", conversationId);

        await db.SaveChangesAsync();
        return message;
    }

    private async Task<Dictionary<long, int>> NonLusParConversationAsync(long moi)
    {
        return await (
            from m in db.Messages
            join membre in db.MembresConversation on m.ConversationId equals membre.ConversationId
            where membre.UtilisateurId == moi
                  && m.ExpediteurId != moi
                  && (membre.DerniereLectureLe == null || m.EnvoyeLe > membre.DerniereLectureLe)
            group m by m.ConversationId into g
            select new { Id = g.Key, N = g.Count() })
            .ToDictionaryAsync(x => x.Id, x => x.N);
    }
}
