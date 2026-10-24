namespace VoisinApp.Api.Dtos;

// Toutes les propriétés sont envoyées en camelCase dans le JSON
// (ex. MotDePasse -> "motDePasse"). Les textes de saisie sont nullables
// pour que l'API réponde avec un message en français plutôt qu'une erreur technique.

public record ErreurDto(string Message);
public record ReponseSimpleDto(string Message, string? CodeDeveloppement = null);
public record ConversationRefDto(long ConversationId);

// ---------- Authentification ----------
public record InscriptionDto(
    string? Prenom, string? Nom, string? Email, string? MotDePasse, string? Telephone,
    string? Ville, string? Quartier, string? Arrondissement, string? Adresse, bool CharteAcceptee);
public record ConnexionDto(string? Email, string? MotDePasse);
public record ReponseAuthDto(string Jeton, DateTime ExpireLe, ProfilDto Utilisateur);
public record MotDePasseOublieDto(string? Email);
public record ReinitialisationDto(string? Code, string? NouveauMotDePasse);
public record ChangerMotDePasseDto(string? AncienMotDePasse, string? NouveauMotDePasse);

// ---------- Quartiers ----------
public record VilleDto(string Nom, double Latitude, double Longitude);
public record QuartierDto(long Id, string Nom, string Ville, string? Arrondissement, string NomAffiche, double Latitude, double Longitude);
public record ZoneDetecteeDto(long QuartierId, string Nom, string Ville, string NomAffiche, int NombreVoisins, string NombreVoisinsTexte, bool Nouveau);

// ---------- Profil ----------
public record ProfilDto(
    long Id, string Prenom, string Nom, string NomComplet, string Initiales, string Email, string? Telephone,
    string? Adresse, string? AvatarUrl, long? QuartierId, string? QuartierNom, string? Arrondissement,
    double RayonKm, string ZoneTexte, int MembreDepuis, string MembreDepuisTexte,
    int NbCoupsDeMain, int NbAnnonces, int NbEvenements,
    bool EmailVerifie, bool AdresseVerifiee, bool ProfilVerifie, int NotificationsNonLues);
public record ModifierProfilDto(string? Prenom, string? Nom, string? Telephone, string? Adresse, double? RayonKm, long? QuartierId);
public record ParametresDto(bool NotifierDemandes, bool NotifierMessages, bool NotifierEvenements, bool ProfilVisible);
public record UtilisateurResumeDto(long Id, string NomComplet, string Initiales, string? AvatarUrl, string? QuartierNom);

// ---------- Accueil / fil ----------
public record AuteurDto(long Id, string NomComplet, string Initiales, string? AvatarUrl);
public record PublicationDto(
    string Type, long Id, AuteurDto Auteur, string? Lieu, DateTime CreeLe, string DateTexte,
    string Titre, string? Texte, string? ImageUrl, string? Etiquette,
    int NbJaime, int NbReponses, bool AJaime);
public record AccueilDto(string Prenom, string? QuartierNom, int NotificationsNonLues, IReadOnlyList<PublicationDto> Fil);

// ---------- Annonces ----------
public record AnnonceCarteDto(
    long Id, string Titre, string Type, string TypeLibelle, long? Prix, string PrixTexte,
    string? ImageUrl, double? DistanceMetres, string InfoTexte, DateTime CreeLe);
public record AnnonceDetailDto(
    long Id, string Titre, string Type, string TypeLibelle, string? Description, long? Prix, string? UnitePrix,
    string PrixTexte, string Statut, IReadOnlyList<string> Photos, AuteurDto Vendeur, double? DistanceMetres,
    string InfoTexte, DateTime CreeLe, bool EstMienne, int NbJaime, int NbCommentaires, bool AJaime);
public record CreerAnnonceDto(string? Type, string? Titre, string? Description, long? Prix, string? UnitePrix, double? Latitude, double? Longitude);
public record ChangerStatutDto(string? Statut);
public record PhotosAjouteesDto(IReadOnlyList<string> Photos);

// ---------- Demandes d'aide ----------
public record CreerDemandeAideDto(string? Categorie, string? Titre, string? Description, bool Urgente);
public record DemandeAideDto(
    long Id, string Categorie, string CategorieLibelle, string Titre, string? Description, bool Urgente,
    string Statut, string StatutLibelle, AuteurDto Auteur, DateTime CreeLe, string DateTexte,
    int NbReponses, bool EstMienne, bool DejaPropose);
public record TerminerDemandeDto(long? AidantId);

// ---------- J'aime et commentaires ----------
public record BasculerJaimeDto(string? TypeCible, long CibleId);
public record JaimeEtatDto(bool AJaime, int NbJaime);
public record CreerCommentaireDto(string? TypeCible, long CibleId, string? Contenu);
public record CommentaireDto(long Id, AuteurDto Auteur, string Contenu, DateTime CreeLe, string DateTexte, bool EstMien);

// ---------- Événements ----------
public record EvenementResumeDto(
    long Id, string Titre, string? Categorie, string NomLieu, DateTime DebutLe, DateTime? FinLe,
    string JourHeureTexte, int JourNumero, string MoisCourt, string HeureTexte,
    int NbParticipants, IReadOnlyList<string> ApercuParticipants, string? ImageUrl, bool Participe);
public record EvenementDetailDto(
    long Id, string Titre, string? Categorie, string? Description, string NomLieu, DateTime DebutLe, DateTime? FinLe,
    string DateTexte, string HorairesTexte, string? TrajetTexte, int NbParticipants, int NbConnus, string? NbConnusTexte,
    IReadOnlyList<string> ApercuParticipants, string? ImageUrl, bool Participe, bool EstOrganisateur);
public record CreerEvenementDto(string? Titre, string? Categorie, string? Description, string? NomLieu, double? Latitude, double? Longitude, DateTime? DebutLe, DateTime? FinLe);
public record ParticipationDto(bool Participe, int NbParticipants);

// ---------- Messagerie ----------
public record ConversationResumeDto(
    long Id, string Type, string Titre, string Initiales, string CouleurAvatar, string? DernierMessage,
    DateTime? DernierMessageLe, string HeureTexte, int NonLus, long? AutreUtilisateurId);
public record ConversationEnteteDto(long Id, string Type, string Titre, string StatutTexte, bool LectureSeule);
public record MessageChatDto(long Id, string Contenu, DateTime EnvoyeLe, string HeureTexte, string JourTexte, bool EstMoi, bool Lu, string? ExpediteurNom);
public record EnvoyerMessageDto(string? Contenu);
public record CreerConversationDto(long DestinataireId, string? Message);
public record NonLusDto(int Total);

// ---------- Signalements ----------
public record CreerSignalementDto(string? Categorie, string? Localisation, double? Latitude, double? Longitude, string? Description);
public record SignalementDto(
    long Id, string Categorie, string CategorieLibelle, string? Localisation, string Description,
    string Statut, string StatutLibelle, DateTime CreeLe, string DateTexte);

// ---------- Notifications ----------
public record NotificationDto(long Id, string Type, string Titre, string? Contenu, string? TypeCible, long? CibleId, bool Lue, DateTime CreeLe, string DateTexte);
