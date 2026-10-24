namespace VoisinApp.Api.Entites;

// Les noms de propriétés (PascalCase) sont convertis en noms de colonnes
// en snake_case dans AppDbContext (ex. MotDePasseHache -> mot_de_passe_hache).

public class Quartier
{
    public long Id { get; set; }
    public string Nom { get; set; } = "";
    public string Ville { get; set; } = "";
    public string? Arrondissement { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public DateTime CreeLe { get; set; } = DateTime.UtcNow;
}

public class Utilisateur
{
    public long Id { get; set; }
    public string Prenom { get; set; } = "";
    public string Nom { get; set; } = "";
    public string Email { get; set; } = "";
    public string? Telephone { get; set; }
    public string MotDePasseHache { get; set; } = "";
    public string? Adresse { get; set; }
    public long? QuartierId { get; set; }
    public Quartier? Quartier { get; set; }
    public double RayonKm { get; set; } = 2.0;
    public string? AvatarUrl { get; set; }
    public DateTime? CharteAccepteeLe { get; set; }
    public bool EmailVerifie { get; set; }
    public bool AdresseVerifiee { get; set; }
    public bool Actif { get; set; } = true;
    public DateTime CreeLe { get; set; } = DateTime.UtcNow;
    public DateTime ModifieLe { get; set; } = DateTime.UtcNow;
    public DateTime? DerniereActiviteLe { get; set; }
}

public class ParametresUtilisateur
{
    public long UtilisateurId { get; set; }
    public bool NotifierDemandes { get; set; } = true;
    public bool NotifierMessages { get; set; } = true;
    public bool NotifierEvenements { get; set; } = true;
    public bool ProfilVisible { get; set; } = true;
}

public class JetonReinitialisation
{
    public long Id { get; set; }
    public long UtilisateurId { get; set; }
    public string JetonHache { get; set; } = "";
    public DateTime ExpireLe { get; set; }
    public DateTime? UtiliseLe { get; set; }
    public DateTime CreeLe { get; set; } = DateTime.UtcNow;
}

public class DemandeAide
{
    public long Id { get; set; }
    public long UtilisateurId { get; set; }
    public Utilisateur Utilisateur { get; set; } = null!;
    public long QuartierId { get; set; }
    public string Categorie { get; set; } = "autre";
    public string Titre { get; set; } = "";
    public string? Description { get; set; }
    public bool Urgente { get; set; }
    public string Statut { get; set; } = "ouverte";
    public DateTime CreeLe { get; set; } = DateTime.UtcNow;
}

public class ReponseAide
{
    public long Id { get; set; }
    public long DemandeAideId { get; set; }
    public DemandeAide DemandeAide { get; set; } = null!;
    public long AidantId { get; set; }
    public Utilisateur Aidant { get; set; } = null!;
    public string Statut { get; set; } = "propose";
    public DateTime CreeLe { get; set; } = DateTime.UtcNow;
    public DateTime? TermineLe { get; set; }
}

public class Annonce
{
    public long Id { get; set; }
    public long UtilisateurId { get; set; }
    public Utilisateur Utilisateur { get; set; } = null!;
    public long QuartierId { get; set; }
    public string Type { get; set; } = "a_vendre";
    public string Titre { get; set; } = "";
    public string? Description { get; set; }
    public long? Prix { get; set; }
    public string? UnitePrix { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string Statut { get; set; } = "active";
    public DateTime CreeLe { get; set; } = DateTime.UtcNow;
    public List<PhotoAnnonce> Photos { get; set; } = new();
}

public class PhotoAnnonce
{
    public long Id { get; set; }
    public long AnnonceId { get; set; }
    public Annonce Annonce { get; set; } = null!;
    public string Url { get; set; } = "";
    public short Position { get; set; }
}

public class Evenement
{
    public long Id { get; set; }
    public long OrganisateurId { get; set; }
    public Utilisateur Organisateur { get; set; } = null!;
    public long QuartierId { get; set; }
    public string Titre { get; set; } = "";
    public string? Categorie { get; set; }
    public string? Description { get; set; }
    public string NomLieu { get; set; } = "";
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? ImageCouvertureUrl { get; set; }
    public DateTime DebutLe { get; set; }
    public DateTime? FinLe { get; set; }
    public DateTime CreeLe { get; set; } = DateTime.UtcNow;
    public List<ParticipantEvenement> Participants { get; set; } = new();
}

public class ParticipantEvenement
{
    public long EvenementId { get; set; }
    public Evenement Evenement { get; set; } = null!;
    public long UtilisateurId { get; set; }
    public Utilisateur Utilisateur { get; set; } = null!;
    public DateTime RejointLe { get; set; } = DateTime.UtcNow;
}

public class Commentaire
{
    public long Id { get; set; }
    public long UtilisateurId { get; set; }
    public Utilisateur Utilisateur { get; set; } = null!;
    public string TypeCible { get; set; } = "";
    public long CibleId { get; set; }
    public string Contenu { get; set; } = "";
    public DateTime CreeLe { get; set; } = DateTime.UtcNow;
}

public class Jaime
{
    public long UtilisateurId { get; set; }
    public string TypeCible { get; set; } = "";
    public long CibleId { get; set; }
    public DateTime CreeLe { get; set; } = DateTime.UtcNow;
}

public class Signalement
{
    public long Id { get; set; }
    public long UtilisateurId { get; set; }
    public long? QuartierId { get; set; }
    public string Categorie { get; set; } = "";
    public string? Localisation { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string Description { get; set; } = "";
    public string Statut { get; set; } = "recu";
    public DateTime CreeLe { get; set; } = DateTime.UtcNow;
    public DateTime ModifieLe { get; set; } = DateTime.UtcNow;
}

public class Conversation
{
    public long Id { get; set; }
    public string Type { get; set; } = "privee";
    public string? Titre { get; set; }
    public long? AnnonceId { get; set; }
    public long? DemandeAideId { get; set; }
    public long? CreePar { get; set; }
    public DateTime CreeLe { get; set; } = DateTime.UtcNow;
    public List<MembreConversation> Membres { get; set; } = new();
    public List<Message> Messages { get; set; } = new();
}

public class MembreConversation
{
    public long ConversationId { get; set; }
    public Conversation Conversation { get; set; } = null!;
    public long UtilisateurId { get; set; }
    public Utilisateur Utilisateur { get; set; } = null!;
    public DateTime RejointLe { get; set; } = DateTime.UtcNow;
    public DateTime? DerniereLectureLe { get; set; }
}

public class Message
{
    public long Id { get; set; }
    public long ConversationId { get; set; }
    public Conversation Conversation { get; set; } = null!;
    public long? ExpediteurId { get; set; }
    public Utilisateur? Expediteur { get; set; }
    public string Contenu { get; set; } = "";
    public DateTime EnvoyeLe { get; set; } = DateTime.UtcNow;
}

public class Notification
{
    public long Id { get; set; }
    public long UtilisateurId { get; set; }
    public string Type { get; set; } = "";
    public string Titre { get; set; } = "";
    public string? Contenu { get; set; }
    public string? TypeCible { get; set; }
    public long? CibleId { get; set; }
    public bool Lue { get; set; }
    public DateTime CreeLe { get; set; } = DateTime.UtcNow;
}
