namespace VoisinApp.Api.Controllers;

[Route("api")]
public class InteractionsController(AppDbContext db, ServiceNotifications notifications) : ControleurDeBase
{
    // Bouton ♡ : ajoute ou retire le j'aime
    [HttpPost("jaimes")]
    public async Task<ActionResult<JaimeEtatDto>> BasculerJaime(BasculerJaimeDto dto)
    {
        if (dto.TypeCible is null || !Valeurs.TypesCible.Contains(dto.TypeCible))
            return Erreur(400, "Type d'élément invalide.");
        if (!(await ChercherCibleAsync(dto.TypeCible, dto.CibleId)).Existe)
            return Erreur(404, "Élément introuvable.");

        Jaime? existant = await db.Jaimes.FirstOrDefaultAsync(j =>
            j.UtilisateurId == UtilisateurId && j.TypeCible == dto.TypeCible && j.CibleId == dto.CibleId);

        bool aJaime;
        if (existant is null)
        {
            db.Jaimes.Add(new Jaime { UtilisateurId = UtilisateurId, TypeCible = dto.TypeCible, CibleId = dto.CibleId });
            aJaime = true;
        }
        else
        {
            db.Jaimes.Remove(existant);
            aJaime = false;
        }
        await db.SaveChangesAsync();

        int total = await db.Jaimes.CountAsync(j => j.TypeCible == dto.TypeCible && j.CibleId == dto.CibleId);
        return Ok(new JaimeEtatDto(aJaime, total));
    }

    // Réponses / commentaires d'une publication
    [HttpGet("commentaires")]
    public async Task<ActionResult<List<CommentaireDto>>> ListerCommentaires(string? typeCible, long cibleId)
    {
        if (typeCible is null || !Valeurs.TypesCible.Contains(typeCible))
            return Erreur(400, "Type d'élément invalide.");

        var lignes = await db.Commentaires
            .Where(c => c.TypeCible == typeCible && c.CibleId == cibleId)
            .OrderBy(c => c.CreeLe)
            .Select(c => new
            {
                c.Id, c.Contenu, c.CreeLe, c.UtilisateurId,
                c.Utilisateur.Prenom, c.Utilisateur.Nom, c.Utilisateur.AvatarUrl
            })
            .ToListAsync();

        return Ok(lignes.Select(c => new CommentaireDto(
            c.Id, VersAuteur(c.UtilisateurId, c.Prenom, c.Nom, c.AvatarUrl), c.Contenu,
            c.CreeLe, Temps.DateRelative(c.CreeLe), c.UtilisateurId == UtilisateurId)).ToList());
    }

    [HttpPost("commentaires")]
    public async Task<ActionResult<CommentaireDto>> Commenter(CreerCommentaireDto dto)
    {
        string? contenu = dto.Contenu?.Trim();
        if (dto.TypeCible is null || !Valeurs.TypesCible.Contains(dto.TypeCible))
            return Erreur(400, "Type d'élément invalide.");
        if (string.IsNullOrWhiteSpace(contenu))
            return Erreur(400, "Écrivez votre message.");
        if (contenu.Length > 1000)
            return Erreur(400, "Message trop long (1000 caractères maximum).");

        var cible = await ChercherCibleAsync(dto.TypeCible, dto.CibleId);
        if (!cible.Existe)
            return Erreur(404, "Élément introuvable.");

        Utilisateur moi = await db.Utilisateurs.FirstAsync(u => u.Id == UtilisateurId);
        var commentaire = new Commentaire
        {
            UtilisateurId = moi.Id,
            TypeCible = dto.TypeCible,
            CibleId = dto.CibleId,
            Contenu = contenu
        };
        db.Commentaires.Add(commentaire);

        if (cible.ProprietaireId is long proprietaireId && proprietaireId != moi.Id)
        {
            await notifications.AjouterAsync(proprietaireId, "commentaire",
                $"{moi.Prenom} a répondu", Textes.Tronquer(contenu, 100), dto.TypeCible, dto.CibleId);
        }

        await db.SaveChangesAsync();

        return Ok(new CommentaireDto(
            commentaire.Id, VersAuteur(moi.Id, moi.Prenom, moi.Nom, moi.AvatarUrl), contenu,
            commentaire.CreeLe, Temps.DateRelative(commentaire.CreeLe), true));
    }

    [HttpDelete("commentaires/{id:long}")]
    public async Task<IActionResult> SupprimerCommentaire(long id)
    {
        Commentaire? commentaire = await db.Commentaires.FirstOrDefaultAsync(c => c.Id == id);
        if (commentaire is null)
            return Erreur(404, "Commentaire introuvable.");
        if (commentaire.UtilisateurId != UtilisateurId)
            return Erreur(403, "Vous ne pouvez supprimer que vos propres commentaires.");

        db.Commentaires.Remove(commentaire);
        await db.SaveChangesAsync();
        return NoContent();
    }

    private async Task<(bool Existe, long? ProprietaireId)> ChercherCibleAsync(string typeCible, long id)
    {
        switch (typeCible)
        {
            case "annonce":
                long? vendeur = await db.Annonces.Where(a => a.Id == id).Select(a => (long?)a.UtilisateurId).FirstOrDefaultAsync();
                return (vendeur.HasValue, vendeur);
            case "demande_aide":
                long? auteur = await db.DemandesAide.Where(d => d.Id == id).Select(d => (long?)d.UtilisateurId).FirstOrDefaultAsync();
                return (auteur.HasValue, auteur);
            case "evenement":
                long? organisateur = await db.Evenements.Where(e => e.Id == id).Select(e => (long?)e.OrganisateurId).FirstOrDefaultAsync();
                return (organisateur.HasValue, organisateur);
            default:
                return (false, null);
        }
    }
}
