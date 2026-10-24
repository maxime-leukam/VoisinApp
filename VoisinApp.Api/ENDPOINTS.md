# API VoisinApp : liste des endpoints

- Adresse de base : `http://<adresse-du-pc>:5080` (voir « Lancer l'API »).
- Format : JSON en **camelCase** (`motDePasse`, `creeLe`). Dates en UTC (ISO 8601, avec « Z »).
- **Authentification** : après `connexion` ou `inscription`, envoyer l'en-tête `Authorization: Bearer <jeton>` sur tous les appels marqués 🔒.
- **Erreurs** : toujours `{ "message": "texte en français" }` avec le code HTTP (400, 401, 403, 404, 409, 429, 500).
- Images : les URL renvoyées sont déjà complètes (`http://.../uploads/...`).

## Lancer l'API
1. Ouvrir la solution, définir `VoisinApp.Api` comme projet de démarrage, profil **http**, F5.
2. La base `voisinapp.db` est créée toute seule au premier lancement (aucun script SQL à exécuter).
3. Test : ouvrir `http://localhost:5080/` → « VoisinApp API en ligne ».

---

## Authentification : pages Connexion, Inscription
| Méthode | Route | 🔒 | Corps → réponse |
|---|---|---|---|
| POST | `/api/auth/inscription` | non | `{prenom, nom, email, motDePasse, telephone?, adresse, arrondissement, quartierId?, charteAcceptee}` → `{jeton, expireLe, utilisateur}` |
| POST | `/api/auth/connexion` | non | `{email, motDePasse}` → `{jeton, expireLe, utilisateur}` |
| POST | `/api/auth/mot-de-passe-oublie` | non | `{email}` → `{message, codeDeveloppement?}` (le code apparaît dans la réponse et la console seulement en développement) |
| POST | `/api/auth/reinitialiser-mot-de-passe` | non | `{code, nouveauMotDePasse}` |
| GET | `/api/auth/verifier-email?jeton=` | non | lien reçu par e-mail |
| POST | `/api/auth/renvoyer-verification-email` | 🔒 | : |

## Quartiers : Inscription (étape 2)
| GET | `/api/quartiers` | non | liste des quartiers |
|---|---|---|---|
| GET | `/api/quartiers/detecter?adresse=&ville=` | non | → `{quartierId, nomAffiche: "Bonamoussadi · Douala V", nombreVoisinsTexte: "1 284 voisins inscrits"}` ou 404 |

## Accueil
| GET | `/api/accueil` | 🔒 | → `{prenom, quartierNom, notificationsNonLues, fil[]}` |
|---|---|---|---|
| GET | `/api/fil?page=&taille=` | 🔒 | « Tout voir » → `publication[]` |

## Annonces : pages Annonces, Nouvelle annonce
| GET | `/api/annonces?type=&recherche=&latitude=&longitude=&page=` | 🔒 | `type` = `a_vendre` / `a_donner` / `service` → cartes (`prixTexte`, `infoTexte` = « À 350 m · Hier ») |
|---|---|---|---|
| GET | `/api/annonces/mes-annonces` | 🔒 | : |
| GET | `/api/annonces/{id}` | 🔒 | détail |
| POST | `/api/annonces` | 🔒 | `{type, titre, description, prix?, unitePrix?, latitude?, longitude?}` |
| POST | `/api/annonces/{id}/photos` | 🔒 | formulaire multipart, champ `fichiers` (jusqu'à 5 images) |
| PUT | `/api/annonces/{id}/statut` | 🔒 | `{statut}` : `active`, `vendue`, `donnee`, `cloturee` |
| DELETE | `/api/annonces/{id}` | 🔒 | : |
| POST | `/api/annonces/{id}/contacter` | 🔒 | → `{conversationId}` |

## Demandes d'aide : page Demander de l'aide
| POST | `/api/demandes-aide` | 🔒 | `{categorie, titre, description, urgente}` : `bricolage`, `transport`, `courses`, `autre` |
|---|---|---|---|
| GET | `/api/demandes-aide?categorie=&page=` | 🔒 | demandes ouvertes du quartier |
| GET | `/api/demandes-aide/mes-demandes` | 🔒 | : |
| GET | `/api/demandes-aide/{id}` | 🔒 | : |
| POST | `/api/demandes-aide/{id}/proposer-aide` | 🔒 | → `{conversationId}` (ouvre la discussion) |
| POST | `/api/demandes-aide/{id}/terminer` | 🔒 | `{aidantId?}` (compte un « coup de main » à l'aidant) |
| POST | `/api/demandes-aide/{id}/annuler` | 🔒 | : |

## J'aime et réponses (fil de l'accueil)
| POST | `/api/jaimes` | 🔒 | `{typeCible, cibleId}` → `{aJaime, nbJaime}` (`typeCible` : `annonce`, `demande_aide`, `evenement`) |
|---|---|---|---|
| GET | `/api/commentaires?typeCible=&cibleId=` | 🔒 | : |
| POST | `/api/commentaires` | 🔒 | `{typeCible, cibleId, contenu}` |
| DELETE | `/api/commentaires/{id}` | 🔒 | : |

## Événements : pages Événements, Détail événement
| GET | `/api/evenements?date=AAAA-MM-JJ` | 🔒 | événements du jour (aujourd'hui par défaut) |
|---|---|---|---|
| GET | `/api/evenements/a-venir?limite=` | 🔒 | à partir de demain |
| GET | `/api/evenements/{id}?latitude=&longitude=` | 🔒 | détail (`trajetTexte`: « À 8 min à pied », `nbConnusTexte`) |
| POST | `/api/evenements` | 🔒 | `{titre, categorie, description, nomLieu, latitude?, longitude?, debutLe, finLe?}` |
| POST | `/api/evenements/{id}/image` | 🔒 | multipart, champ `fichier` |
| POST | `/api/evenements/{id}/participer` | 🔒 | bascule « Je participe » → `{participe, nbParticipants}` |
| DELETE | `/api/evenements/{id}` | 🔒 | organisateur seulement |

## Messagerie : pages Messages, Conversation
| GET | `/api/conversations` | 🔒 | liste (`nonLus`, `heureTexte`, `dernierMessage`) |
|---|---|---|---|
| GET | `/api/conversations/non-lus` | 🔒 | → `{total}` |
| POST | `/api/conversations` | 🔒 | `{destinataireId, message?}` → `{conversationId}` |
| GET | `/api/conversations/{id}` | 🔒 | en-tête : `{titre, statutTexte: "En ligne il y a 5 min", lectureSeule}` |
| GET | `/api/conversations/{id}/messages?apresId=` | 🔒 | messages (marque comme lu) ; `apresId` = rafraîchissement |
| POST | `/api/conversations/{id}/messages` | 🔒 | `{contenu}` |
| GET | `/api/utilisateurs/recherche?q=` | 🔒 | trouver un voisin à qui écrire |

## Signalements : page Faire un signalement
| POST | `/api/signalements` | 🔒 | `{categorie, localisation, latitude?, longitude?, description}` : `voirie`, `proprete`, `eclairage`, `securite` |
|---|---|---|---|
| GET | `/api/signalements/mes-signalements` | 🔒 | : |
| GET | `/api/signalements/{id}` | 🔒 | auteur seulement |

## Profil : page Mon profil
| GET | `/api/profil` | 🔒 | profil + statistiques (`nbCoupsDeMain`, `nbAnnonces`, `nbEvenements`, `profilVerifie`, `zoneTexte`) |
|---|---|---|---|
| PUT | `/api/profil` | 🔒 | `{prenom?, nom?, telephone?, adresse?, rayonKm?, quartierId?}` |
| POST | `/api/profil/avatar` | 🔒 | multipart, champ `fichier` |
| GET / PUT | `/api/profil/parametres` | 🔒 | `{notifierDemandes, notifierMessages, notifierEvenements, profilVisible}` |
| PUT | `/api/profil/mot-de-passe` | 🔒 | `{ancienMotDePasse, nouveauMotDePasse}` |
| DELETE | `/api/profil` | 🔒 | désactive le compte |

## Notifications : cloche de l'accueil
| GET | `/api/notifications?nonLuesSeulement=` | 🔒 | : |
|---|---|---|---|
| GET | `/api/notifications/non-lues` | 🔒 | → `{total}` |
| POST | `/api/notifications/{id}/lue` | 🔒 | : |
| POST | `/api/notifications/tout-lire` | 🔒 | : |

## Déconnexion
Il n'y a pas d'appel serveur : l'application supprime simplement le jeton enregistré sur le téléphone.
