namespace VoisinApp.Api.Controllers;

[Route("api/demandes-aide")]
public class DemandesAideController(
    AppDbContext db, ServiceConversations conversations, ServiceNotifications notifications) : ControleurDeBase
{
    // Page "Demander de l'aide" : bouton "Publier ma demande"
    [HttpPost]
    public async Task<ActionResult<DemandeAideDto>> Creer(CreerDemandeAideDto dto)
    {
        string? titre = dto.Titre?.Trim();
        if (dto.Categorie is null || !Valeurs.CategoriesDemande.Contains(dto.Categorie))
            return Erreur(400, "Choisissez une catégorie.");
        if (string.IsNullOrWhiteSpace(titre))
            return Erreur(400, "Donnez un titre à votre demande.");
        if (titre.Length > 120)
            return Erreur(400, "Le titre est trop long (120 caractères maximum).");

        Utilisateur moi = await db.Utilisateurs.FirstAsync(u => u.Id == UtilisateurId);
        if (moi.QuartierId is not long quartierId)
            return Erreur(400, "Complétez votre quartier avant de publier.");

        var demande = new DemandeAide
        {
            UtilisateurId = moi.Id,
            QuartierId = quartierId,
            Categorie = dto.Categorie,
            Titre = titre,
            Description = dto.Description?.Trim(),
            Urgente = dto.Urgente
        };
        db.DemandesAide.Add(demande);
        await db.SaveChangesAsync();

        // "Demande urgente : notifier les voisins disponibles"
        if (dto.Urgente)
        {
            List<long> voisins = await db.Utilisateurs
                .Where(u => u.QuartierId == quartierId && u.Id != moi.Id && u.Actif)
                .Select(u => u.Id)
                .Take(500)
                .ToListAsync();

            await notifications.AjouterPourPlusieursAsync(voisins, "demande_aide",
                $"Demande urgente : {titre}", $"{moi.Prenom} a besoin d'aide près de chez vous.", "demande_aide", demande.Id);
            await db.SaveChangesAsync();
        }

        List<DemandeAideDto> resultat = await ConstruireAsync(db.DemandesAide.Where(d => d.Id == demande.Id));
        return Ok(resultat[0]);
    }

    // Demandes ouvertes du quartier (urgentes en premier)
    [HttpGet]
    public async Task<ActionResult<List<DemandeAideDto>>> Lister(string? categorie, int page = 1, int taille = 20)
    {
        Utilisateur moi = await db.Utilisateurs.FirstAsync(u => u.Id == UtilisateurId);
        if (moi.QuartierId is not long quartierId)
            return Ok(new List<DemandeAideDto>());
        if (!string.IsNullOrWhiteSpace(categorie) && !Valeurs.CategoriesDemande.Contains(categorie))
            return Erreur(400, "Catégorie invalide.");

        page = Math.Max(1, page);
        taille = Math.Clamp(taille, 1, 50);

        IQueryable<DemandeAide> requete = db.DemandesAide
            .Where(d => d.QuartierId == quartierId && (d.Statut == "ouverte" || d.Statut == "en_cours"));
        if (!string.IsNullOrWhiteSpace(categorie))
            requete = requete.Where(d => d.Categorie == categorie);

        requete = requete
            .OrderByDescending(d => d.Urgente)
            .ThenByDescending(d => d.CreeLe)
            .Skip((page - 1) * taille)
            .Take(taille);

        return Ok(await ConstruireAsync(requete));
    }

    [HttpGet("mes-demandes")]
    public async Task<ActionResult<List<DemandeAideDto>>> MesDemandes()
    {
        IQueryable<DemandeAide> requete = db.DemandesAide
            .Where(d => d.UtilisateurId == UtilisateurId)
            .OrderByDescending(d => d.CreeLe);
        return Ok(await ConstruireAsync(requete));
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<DemandeAideDto>> Detail(long id)
    {
        List<DemandeAideDto> resultat = await ConstruireAsync(db.DemandesAide.Where(d => d.Id == id));
        if (resultat.Count == 0)
            return Erreur(404, "Demande introuvable.");
        return Ok(resultat[0]);
    }

    // Un voisin propose son aide : ouvre une conversation avec l'auteur de la demande
    [HttpPost("{id:long}/proposer-aide")]
    public async Task<ActionResult<ConversationRefDto>> ProposerAide(long id)
    {
        DemandeAide? demande = await db.DemandesAide.Include(d => d.Utilisateur).FirstOrDefaultAsync(d => d.Id == id);
        if (demande is null)
            return Erreur(404, "Demande introuvable.");
        if (demande.UtilisateurId == UtilisateurId)
            return Erreur(400, "C'est votre propre demande.");
        if (demande.Statut != "ouverte" && demande.Statut != "en_cours")
            return Erreur(400, "Cette demande n'est plus ouverte.");

        Utilisateur moi = await db.Utilisateurs.FirstAsync(u => u.Id == UtilisateurId);

        bool dejaPropose = await db.ReponsesAide.AnyAsync(r => r.DemandeAideId == id && r.AidantId == moi.Id);
        Conversation conversation = await conversations.ObtenirOuCreerPriveeAsync(moi.Id, demande.UtilisateurId, null, demande.Id);

        if (!dejaPropose)
        {
            db.ReponsesAide.Add(new ReponseAide { DemandeAideId = id, AidantId = moi.Id });
            conversation.Messages.Add(new Message
            {
                ConversationId = conversation.Id,
                ExpediteurId = moi.Id,
                Contenu = $"Bonjour {demande.Utilisateur.Prenom}, j'ai vu votre demande « {demande.Titre} »."
            });

            await notifications.AjouterAsync(demande.UtilisateurId, "demande_aide",
                $"{moi.Prenom} propose de vous aider",
                $"Pour votre demande « {demande.Titre} ».", "demande_aide", id);

            await db.SaveChangesAsync();
        }

        return Ok(new ConversationRefDto(conversation.Id));
    }

    // L'auteur marque sa demande comme terminée (et, au choix, indique qui l'a aidé : +1 "coup de main")
    [HttpPost("{id:long}/terminer")]
    public async Task<IActionResult> Terminer(long id, TerminerDemandeDto dto)
    {
        DemandeAide? demande = await db.DemandesAide.FirstOrDefaultAsync(d => d.Id == id);
        if (demande is null)
            return Erreur(404, "Demande introuvable.");
        if (demande.UtilisateurId != UtilisateurId)
            return Erreur(403, "Seul l'auteur peut clôturer sa demande.");
        if (demande.Statut == "terminee" || demande.Statut == "annulee")
            return Erreur(400, "Cette demande est déjà clôturée.");

        demande.Statut = "terminee";

        if (dto.AidantId is long aidantId)
        {
            ReponseAide? reponse = await db.ReponsesAide
                .FirstOrDefaultAsync(r => r.DemandeAideId == id && r.AidantId == aidantId);
            if (reponse is null)
                return Erreur(400, "Cette personne n'a pas proposé son aide.");

            reponse.Statut = "termine";
            reponse.TermineLe = DateTime.UtcNow;

            await notifications.AjouterAsync(aidantId, "demande_aide", "Merci pour votre aide !",
                $"La demande « {demande.Titre} » a été marquée comme terminée.", "demande_aide", id);
        }

        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id:long}/annuler")]
    public async Task<IActionResult> Annuler(long id)
    {
        DemandeAide? demande = await db.DemandesAide.FirstOrDefaultAsync(d => d.Id == id);
        if (demande is null)
            return Erreur(404, "Demande introuvable.");
        if (demande.UtilisateurId != UtilisateurId)
            return Erreur(403, "Seul l'auteur peut annuler sa demande.");
        if (demande.Statut == "terminee" || demande.Statut == "annulee")
            return Erreur(400, "Cette demande est déjà clôturée.");

        demande.Statut = "annulee";
        await db.SaveChangesAsync();
        return NoContent();
    }

    private async Task<List<DemandeAideDto>> ConstruireAsync(IQueryable<DemandeAide> requete)
    {
        List<DemandeAide> demandes = await requete.Include(d => d.Utilisateur).ToListAsync();
        List<long> ids = demandes.Select(d => d.Id).ToList();

        Dictionary<long, int> nbReponses = await db.ReponsesAide
            .Where(r => ids.Contains(r.DemandeAideId))
            .GroupBy(r => r.DemandeAideId)
            .Select(g => new { Id = g.Key, N = g.Count() })
            .ToDictionaryAsync(x => x.Id, x => x.N);

        List<long> dejaProposeListe = await db.ReponsesAide
            .Where(r => r.AidantId == UtilisateurId && ids.Contains(r.DemandeAideId))
            .Select(r => r.DemandeAideId)
            .ToListAsync();
        HashSet<long> dejaPropose = dejaProposeListe.ToHashSet();

        return demandes.Select(d => new DemandeAideDto(
            d.Id, d.Categorie, Libelles.CategorieDemande(d.Categorie), d.Titre, d.Description, d.Urgente,
            d.Statut, Libelles.StatutDemande(d.Statut),
            VersAuteur(d.Utilisateur.Id, d.Utilisateur.Prenom, d.Utilisateur.Nom, d.Utilisateur.AvatarUrl),
            d.CreeLe, Temps.DateRelative(d.CreeLe),
            nbReponses.GetValueOrDefault(d.Id), d.UtilisateurId == UtilisateurId, dejaPropose.Contains(d.Id))).ToList();
    }
}
