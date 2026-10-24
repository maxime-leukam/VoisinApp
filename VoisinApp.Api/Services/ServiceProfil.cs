namespace VoisinApp.Api.Services;

public class ServiceProfil(AppDbContext db)
{
    // Construit le profil complet d'un utilisateur, avec ses statistiques.
    // urlAbsolue transforme "/uploads/..." en adresse complète utilisable par le téléphone.
    public async Task<ProfilDto> ConstruireAsync(long utilisateurId, Func<string?, string?> urlAbsolue)
    {
        Utilisateur u = await db.Utilisateurs
            .Include(x => x.Quartier)
            .FirstAsync(x => x.Id == utilisateurId);

        int coupsDeMain = await db.ReponsesAide.CountAsync(r => r.AidantId == utilisateurId && r.Statut == "termine");
        int annonces = await db.Annonces.CountAsync(a => a.UtilisateurId == utilisateurId);
        int evenements = await db.ParticipantsEvenement.CountAsync(p => p.UtilisateurId == utilisateurId);
        int nonLues = await db.Notifications.CountAsync(n => n.UtilisateurId == utilisateurId && !n.Lue);

        int annee = Temps.EnLocal(u.CreeLe).Year;
        string zone = u.Quartier is null
            ? "Zone non définie"
            : $"{u.Quartier.Nom} · Rayon de {Textes.Km(u.RayonKm)}";

        return new ProfilDto(
            u.Id, u.Prenom, u.Nom, Textes.NomComplet(u.Prenom, u.Nom), Textes.Initiales(u.Prenom, u.Nom),
            u.Email, u.Telephone, u.Adresse, urlAbsolue(u.AvatarUrl),
            u.QuartierId, u.Quartier?.Nom, u.Quartier?.Arrondissement,
            u.RayonKm, zone, annee, $"Membre depuis {annee}",
            coupsDeMain, annonces, evenements,
            u.EmailVerifie, u.AdresseVerifiee, u.EmailVerifie && u.AdresseVerifiee, nonLues);
    }
}
