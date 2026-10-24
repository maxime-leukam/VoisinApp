using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace VoisinApp.Api.Donnees;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Quartier> Quartiers => Set<Quartier>();
    public DbSet<Utilisateur> Utilisateurs => Set<Utilisateur>();
    public DbSet<ParametresUtilisateur> ParametresUtilisateurs => Set<ParametresUtilisateur>();
    public DbSet<JetonReinitialisation> JetonsReinitialisation => Set<JetonReinitialisation>();
    public DbSet<DemandeAide> DemandesAide => Set<DemandeAide>();
    public DbSet<ReponseAide> ReponsesAide => Set<ReponseAide>();
    public DbSet<Annonce> Annonces => Set<Annonce>();
    public DbSet<PhotoAnnonce> PhotosAnnonce => Set<PhotoAnnonce>();
    public DbSet<Evenement> Evenements => Set<Evenement>();
    public DbSet<ParticipantEvenement> ParticipantsEvenement => Set<ParticipantEvenement>();
    public DbSet<Commentaire> Commentaires => Set<Commentaire>();
    public DbSet<Jaime> Jaimes => Set<Jaime>();
    public DbSet<Signalement> Signalements => Set<Signalement>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<MembreConversation> MembresConversation => Set<MembreConversation>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        // ---- Noms des tables (identiques au script SQL) ----
        mb.Entity<Quartier>().ToTable("quartiers");
        mb.Entity<Utilisateur>().ToTable("utilisateurs");
        mb.Entity<ParametresUtilisateur>().ToTable("parametres_utilisateur");
        mb.Entity<JetonReinitialisation>().ToTable("jetons_reinitialisation");
        mb.Entity<DemandeAide>().ToTable("demandes_aide");
        mb.Entity<ReponseAide>().ToTable("reponses_aide");
        mb.Entity<Annonce>().ToTable("annonces");
        mb.Entity<PhotoAnnonce>().ToTable("photos_annonce");
        mb.Entity<Evenement>().ToTable("evenements");
        mb.Entity<ParticipantEvenement>().ToTable("participants_evenement");
        mb.Entity<Commentaire>().ToTable("commentaires");
        mb.Entity<Jaime>().ToTable("jaimes");
        mb.Entity<Signalement>().ToTable("signalements");
        mb.Entity<Conversation>().ToTable("conversations");
        mb.Entity<MembreConversation>().ToTable("membres_conversation");
        mb.Entity<Message>().ToTable("messages");
        mb.Entity<Notification>().ToTable("notifications");

        // ---- Clés composites ----
        mb.Entity<ParametresUtilisateur>().HasKey(p => p.UtilisateurId);
        mb.Entity<ParticipantEvenement>().HasKey(p => new { p.EvenementId, p.UtilisateurId });
        mb.Entity<Jaime>().HasKey(j => new { j.UtilisateurId, j.TypeCible, j.CibleId });
        mb.Entity<MembreConversation>().HasKey(m => new { m.ConversationId, m.UtilisateurId });

        // ---- Clés étrangères sans propriété de navigation ----
        mb.Entity<ParametresUtilisateur>().HasOne<Utilisateur>().WithOne().HasForeignKey<ParametresUtilisateur>(p => p.UtilisateurId);
        mb.Entity<JetonReinitialisation>().HasOne<Utilisateur>().WithMany().HasForeignKey(j => j.UtilisateurId);
        mb.Entity<DemandeAide>().HasOne<Quartier>().WithMany().HasForeignKey(d => d.QuartierId);
        mb.Entity<Annonce>().HasOne<Quartier>().WithMany().HasForeignKey(a => a.QuartierId);
        mb.Entity<Evenement>().HasOne<Quartier>().WithMany().HasForeignKey(e => e.QuartierId);
        mb.Entity<Jaime>().HasOne<Utilisateur>().WithMany().HasForeignKey(j => j.UtilisateurId);
        mb.Entity<Signalement>().HasOne<Utilisateur>().WithMany().HasForeignKey(s => s.UtilisateurId);
        mb.Entity<Signalement>().HasOne<Quartier>().WithMany().HasForeignKey(s => s.QuartierId).OnDelete(DeleteBehavior.SetNull);
        mb.Entity<Notification>().HasOne<Utilisateur>().WithMany().HasForeignKey(n => n.UtilisateurId);
        mb.Entity<Conversation>().HasOne<Annonce>().WithMany().HasForeignKey(c => c.AnnonceId).OnDelete(DeleteBehavior.SetNull);
        mb.Entity<Conversation>().HasOne<DemandeAide>().WithMany().HasForeignKey(c => c.DemandeAideId).OnDelete(DeleteBehavior.SetNull);
        mb.Entity<Conversation>().HasOne<Utilisateur>().WithMany().HasForeignKey(c => c.CreePar).OnDelete(DeleteBehavior.SetNull);
        mb.Entity<Message>().HasOne(m => m.Expediteur).WithMany().HasForeignKey(m => m.ExpediteurId).OnDelete(DeleteBehavior.SetNull);

        // ---- Index et contraintes d'unicité ----
        mb.Entity<Utilisateur>().HasIndex(u => u.Email).IsUnique();
        mb.Entity<Utilisateur>().HasIndex(u => u.QuartierId);
        mb.Entity<Quartier>().HasIndex(q => new { q.Nom, q.Ville }).IsUnique();
        mb.Entity<ReponseAide>().HasIndex(r => new { r.DemandeAideId, r.AidantId }).IsUnique();
        mb.Entity<PhotoAnnonce>().HasIndex(p => new { p.AnnonceId, p.Position }).IsUnique();
        mb.Entity<JetonReinitialisation>().HasIndex(j => j.JetonHache);
        mb.Entity<DemandeAide>().HasIndex(d => new { d.QuartierId, d.Statut, d.CreeLe });
        mb.Entity<Annonce>().HasIndex(a => new { a.QuartierId, a.Statut, a.CreeLe });
        mb.Entity<Evenement>().HasIndex(e => new { e.QuartierId, e.DebutLe });
        mb.Entity<Commentaire>().HasIndex(c => new { c.TypeCible, c.CibleId, c.CreeLe });
        mb.Entity<Jaime>().HasIndex(j => new { j.TypeCible, j.CibleId });
        mb.Entity<Message>().HasIndex(m => new { m.ConversationId, m.EnvoyeLe });
        mb.Entity<Notification>().HasIndex(n => new { n.UtilisateurId, n.Lue, n.CreeLe });

        // ---- Colonnes en snake_case + dates toujours lues en UTC ----
        var versUtc = new ValueConverter<DateTime, DateTime>(
            v => v.Kind == DateTimeKind.Local ? v.ToUniversalTime() : DateTime.SpecifyKind(v, DateTimeKind.Utc),
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

        var versUtcNullable = new ValueConverter<DateTime?, DateTime?>(
            v => v.HasValue
                ? (v.Value.Kind == DateTimeKind.Local ? v.Value.ToUniversalTime() : DateTime.SpecifyKind(v.Value, DateTimeKind.Utc))
                : v,
            v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);

        foreach (var entite in mb.Model.GetEntityTypes())
        {
            foreach (var propriete in entite.GetProperties())
            {
                propriete.SetColumnName(VersSnakeCase(propriete.Name));

                if (propriete.ClrType == typeof(DateTime))
                    propriete.SetValueConverter(versUtc);
                else if (propriete.ClrType == typeof(DateTime?))
                    propriete.SetValueConverter(versUtcNullable);
            }
        }
    }

    private static string VersSnakeCase(string nom)
    {
        var resultat = new System.Text.StringBuilder();
        for (int i = 0; i < nom.Length; i++)
        {
            char c = nom[i];
            if (char.IsUpper(c))
            {
                if (i > 0) resultat.Append('_');
                resultat.Append(char.ToLowerInvariant(c));
            }
            else
            {
                resultat.Append(c);
            }
        }
        return resultat.ToString();
    }
}
