# DecoAR : guide de révision pour la validation finale

Ce document résume tout le projet pour répondre aux questions du jury : l'idée, les modules, les fonctionnalités, les technologies, le matériel, les dossiers et les fichiers importants. Il se termine par des questions probables avec leurs réponses.

Pour l'architecture détaillée et les diagrammes UML, voir [SYSTEM_DESIGN.md](SYSTEM_DESIGN.md). Pour l'état de l'intégration, voir [RECAP_INTEGRATION.md](RECAP_INTEGRATION.md).

---

## 1. Le projet en une minute

**DecoAR** est une application mobile (Android et iOS) de **boutique de décoration en 3D avec la réalité augmentée**. L'utilisateur :

1. crée un compte et se connecte, avec un mot de passe ou avec son visage ;
2. crée son personnage : choix de l'avatar, couleurs, et scan de son propre visage ;
3. se promène dans une boutique 3D ;
4. ouvre un rayon et inspecte un produit en 3D (rotation, zoom, variantes) ;
5. place le produit **chez lui, en taille réelle**, grâce à la réalité augmentée ;
6. ajoute le produit au panier et paie à la caisse (Stripe), avec livraison à l'adresse GPS.

Un espace d'administration (M6) permet de **créer un modèle 3D d'un produit à partir d'une simple photo**, grâce à une IA.

**Équipe et modules**

| Module | Rôle | Membre |
| --- | --- | --- |
| Auth | Compte, connexion, Face ID, profil | Salsabil Hafsi |
| M1 | Personnage (avatar, personnalisation, scan du visage) | Melek Mouelhi |
| M3 | Catalogue produits (rayons, inspection 3D, recherche) | Maya Marzouki |
| M4 | Réalité augmentée (placement en taille réelle) | Maya Marzouki |
| M5 | Boutique 3D, panier, caisse, paiement | Mohamed Karim Ouertatani |
| M6 | Génération 3D d'un produit à partir d'une photo (administration) | Ali Belkadhi |

---

## 2. Parcours de l'application

```
Auth (connexion / inscription / Face ID)
   │
   ▼
M1_Personnalisation (choix de l'avatar, couleurs) ──► M1_ScanVisage (scan 3D du visage)
   │ "Entrer dans la boutique"
   ▼
caisse (boutique 3D M5, première personne)
   │ télé « Voir le catalogue » / étagères
   ▼
M3 (rayon, fiche produit) ──► « Voir en AR » ──► M4_AR (placement chez soi)
   │                                                │ « Ajouter au panier »
   ▼                                                ▼
Panier ──► Caisse : adresse GPS ──► paiement Stripe ──► animation colis et billets

Bouton profil (en haut à gauche, partout)
   ▼
Profile ──► Espace administration (3D) ──► AdminScene ──► M6_Generator
```

Scènes du build, dans l'ordre : `Auth`, `Profile`, `M1_Personnalisation`, `M1_ScanVisage`, `caisse`, `M3`, `M4_AR`, `AdminScene`, `M6_Generator`.

Les noms des scènes sont centralisés dans `Assets/DecoAR/Core/Scripts/ShopTypes.cs` (classe `SceneNames`).

---

## 3. Fonctionnalités par module

### Auth : compte et sécurité

- Inscription : nom, prénom, e-mail, mot de passe et **enregistrement du visage**.
- Vérification de l'e-mail par un code envoyé par mail.
- Connexion par mot de passe **ou par le visage**.
- **Détection du vivant** : le serveur tire au sort des mouvements (« tourne la tête à gauche », etc.), ce qui empêche de tricher avec une photo.
- Mot de passe oublié, avec un code par e-mail.
- Profil : photo, informations, changement du mot de passe, visage, déconnexion.
- **Verrouillage automatique** : si l'application reste en arrière-plan trop longtemps, il faut se reconnaître par le visage pour revenir.
- Bouton rond « profil » présent sur tous les écrans (`AuthOverlay`).

### M1 : personnage (ma partie)

| Code | Fonctionnalité |
| --- | --- |
| F1.1 / F1.2 | Personnalisation de la peau, des cheveux, du t-shirt ou de la tenue, du pantalon et des chaussures |
| — | **Choix entre 3 avatars** (Remy et 2 avatars MetaPerson) |
| — | **Scan du visage** à la caméra frontale. Le visage est calculé **sur le téléphone** (plan B choisi) et collé sur la tête de l'avatar |
| — | Scan simple : mesure de la couleur de la peau et des cheveux, pour proposer les teintes les plus proches |
| F1.3 | Animations (repos, marche), importées depuis Mixamo |
| F1.4 | Déplacement avec un joystick tactile |
| F1.5 | Caméra à la troisième personne qui ne traverse pas les murs |
| F1.6 | Caméra orientable au gyroscope |
| F1.7 | HUD : panier, mini-carte, paramètres |
| F1.8 | Vibration lors d'une collision |
| F1.9 | Bouton d'interaction contextuel près d'une étagère ou du comptoir |
| — | Écran adapté au portrait et au paysage |

**Comment marche le scan 3D** (`FaceScan3D.cs`) :

1. AR Foundation (ARCore sur Android, ARKit sur iOS), avec la caméra frontale, fournit le **maillage réel du visage** (environ 470 points).
2. Le maillage est figé, puis l'image de la caméra prise au même instant est collée dessus comme texture.
3. Le résultat est enregistré dans le stockage privé de l'application (`FaceMaskStore`). `CharacterAppearance` le pose ensuite sur la tête du personnage.

Une **option serveur** existe aussi (`M1_FaceServer`). Le téléphone envoie 3 photos (face, gauche, droite) à un PC, qui calcule une tête 3D complète avec le **modèle FLAME** (MediaPipe et un ajustement). Par défaut, l'application utilise la solution calculée sur le téléphone, qui fonctionne sans PC.

### M3 : catalogue produits

| Code | Fonctionnalité |
| --- | --- |
| F3.1 | Étagère interactive par zone (salon, chambre, cuisine, bain, tableaux, luminaires, déco) |
| F3.2 | Le produit sort de l'étagère et vient devant la caméra |
| F3.3 | Rotation à 360° au doigt et zoom par pincement |
| F3.4 | Variantes de couleur ou de matière |
| F3.5 | Fiche produit : nom, prix, dimensions, description |
| F3.6 | Boutons « Voir en AR » et « Ajouter au panier » |
| F3.7 | **Recherche vocale** (framework Speech sur iOS) |
| F3.8 | **Secouer le téléphone** repose le produit (accéléromètre, avec un seuil et un délai anti-rebond) |
| F3.9 | Recherche texte |
| F3.10 | Vibrations légères |

Les produits sont en taille réelle, en mètres, avec le pivot au sol. Les pièces nommées « Variant… » prennent la couleur de la variante choisie.

### M4 : réalité augmentée

- Détection des **plans** du sol et des murs (ARCore / ARKit).
- Placement du produit sélectionné dans M3, **en taille réelle** (dimensions en cm, `M4Sizing`).
- Bouton « Taille réelle », déplacement et rotation du produit.
- **Estimation de la lumière** : le produit virtuel prend l'intensité et la couleur de la lumière de la pièce (`M4LightMatcher`).
- Changement de variante sans quitter l'AR (F4.6).
- Prise de **photo** de la pièce avec le produit (`M4Photo`).
- « Ajouter au panier » (envoie le produit et sa variante à M5) et « Retour au rayon » (F4.8).
- Vibration moyenne quand le produit est posé (F4.9).

### M5 : boutique 3D et caisse

- Boutique en **première personne**. Sur PC : touches ZQSD et souris. Sur mobile : joystick à gauche, rotation à droite et **gyroscope**.
- **Télévision** qui s'allume quand on s'approche (effet cathodique) et ouvre le catalogue.
- **Vendeur PNJ** animé : accueil, attente, parole, confirmation.
- **Panier 3D** sur le comptoir : les articles y tombent sous forme de petites boîtes.
- HUD mobile construit par code. Les icônes sont dessinées par des champs de distance signée, sans fichier image.
- Caisse :
  - adresse de livraison par **GPS** et géocodage inverse avec OpenStreetMap (Nominatim) ;
  - **paiement Stripe** dans une page web intégrée à l'application ;
  - vérification du paiement auprès du serveur.
- Animations après le paiement : colis qui se ferme puis s'envole, billets et pièces, applaudissements.
- Musique et sons, avec une option pour couper la musique.
- Rendu propre à la caisse : un pipeline URP dédié, avec plus de lumières et le HDR.

### M6 : génération 3D (administration)

- On choisit une **photo** d'un produit, dans la galerie ou avec l'appareil photo (NativeGallery, NativeCamera).
- L'application l'envoie au serveur Flask (`/generate`). Le serveur appelle **TRELLIS**, une IA image vers 3D hébergée sur Hugging Face.
- L'application suit la tâche (`/status/<id>`), puis télécharge le **modèle GLB** (`/model/<id>`).
- Le modèle s'affiche avec **glTFast**. On peut le tourner et zoomer (`M6ModelViewer`).

### Core : le socle partagé

- `GameManager` (singleton qui ne disparaît pas entre les scènes) garde :
  - l'apparence du personnage ;
  - le produit sélectionné ;
  - le panier ;
  - la position du joueur dans la boutique ;
  - les options (vibrations, musique).
- `HapticManager` : un seul point d'entrée pour les vibrations, avec 3 intensités (tableau 9.1 du cahier des charges).
- `ShopTypes` : noms des scènes, type de placement AR et article du panier.

---

## 4. Technologies

### Application mobile

| Technologie | Utilisation |
| --- | --- |
| **Unity 6** (6000.6.6f1) et **C#** | Moteur du jeu et toute la logique de l'application |
| **URP** (Universal Render Pipeline) 17.6 | Rendu optimisé pour le mobile |
| **AR Foundation 6.6** et **ARCore** / **ARKit** | Réalité augmentée : plans, lumière, maillage du visage |
| **XR Interaction Toolkit** 3.6 | Placement et manipulation des objets en AR (template Mobile AR) |
| **Input System** 1.20 | Tactile, clavier, gyroscope, accéléromètre |
| **uGUI** et **TextMeshPro** | Interface |
| **glTFast** 6.20 | Chargement des modèles GLB générés par M6 |
| **NativeGallery** et **NativeCamera** | Galerie et appareil photo du téléphone (M6) |
| **Mixamo** | Personnage Remy et animations |
| **MetaPerson** (avatarsdk.com) | 2 avatars supplémentaires (FBX) |
| Plugins natifs iOS (`.mm`) et Android (`.java`) | Recherche vocale, vibrations précises, page de paiement intégrée |

### Serveurs

| Service | Technologie | Rôle |
| --- | --- | --- |
| `Backend/` | **Node.js** 20+, **Express** 5, **MongoDB** (Mongoose) | API des comptes |
| | **JWT** | Session de l'utilisateur |
| | **bcrypt** | Hachage des mots de passe |
| | **Nodemailer** / Brevo | E-mails |
| | express-rate-limit | Limite d'essais contre la force brute |
| `face-service/` | **Python**, **Flask**, **OpenCV** | Reconnaissance faciale : **YuNet** détecte le visage, **SFace** en fait un vecteur de 128 nombres |
| `M1_FaceServer/` | **Python**, **FastAPI**, **MediaPipe**, **FLAME**, SciPy | Tête 3D à partir de 3 photos (option de M1) |
| `Assets/DecoAR/Backend_M6/` | **Python**, **Flask**, **Gradio client**, **TRELLIS** (Hugging Face) | Photo transformée en modèle 3D (GLB) |
| `supabase/functions/stripe-checkout` | **Supabase Edge Function** (Deno / TypeScript), **Stripe** | Paiement. La clé secrète Stripe reste sur le serveur |
| `render.yaml` | **Render** | Déploiement en ligne de l'API et du service visage |

### Outils

- **Git** et **GitHub**, avec une branche par membre, puis la branche `integration-modules`.
- **Git LFS** pour les gros fichiers (FBX, PNG).
- Visual Studio / VS Code.
- Android Logcat pour les journaux du téléphone.
- Tests du backend : `node --test`, supertest et une base MongoDB en mémoire (**11/11 réussis**).

---

## 5. Matériel

| Matériel | Utilisation |
| --- | --- |
| **Samsung Galaxy A57** (SM-A576B) | Téléphone de test principal, **compatible ARCore** : AR, scan du visage, gyroscope |
| Samsung Galaxy A16 (SM-A165F) | **Non compatible ARCore** : l'AR et le scan 3D ne marchent pas. Le scan photo simple prend le relais |
| PC Windows | Unity, build Android, serveurs locaux (Auth, M6, FLAME) |
| Câble USB | Installation de l'APK, et `adb reverse` pour que le téléphone joigne le serveur du PC (`AndroidUsbReverse.cs`) |
| Réseau Wi-Fi commun | Le téléphone envoie ses requêtes au PC (M6 et M1_FaceServer) |

**Capteurs du téléphone utilisés**

| Capteur | Utilisation |
| --- | --- |
| Caméra arrière | Réalité augmentée (M4) |
| Caméra frontale | Scan du visage (M1), Face ID (Auth) |
| Gyroscope | Regarder autour de soi (M1, M5) |
| Accéléromètre | Secouer pour reposer un produit (M3) |
| GPS | Adresse de livraison (M5) |
| Microphone | Recherche vocale (M3) |
| Vibreur | Retours haptiques (tous les modules) |
| Capteur de lumière, par la caméra AR | Éclairage réaliste du produit (M4) |

---

## 6. Les dossiers et leur contenu

### Racine du dépôt

| Dossier ou fichier | Contenu |
| --- | --- |
| `Assets/` | Tout le contenu Unity : scènes, scripts, modèles, matériaux |
| `Packages/` | Liste des paquets Unity (`manifest.json`) et versions figées (`packages-lock.json`) |
| `ProjectSettings/` | Réglages du projet : liste des scènes, icône, plateforme, qualité |
| `Backend/` | API Node.js des comptes (Auth) |
| `face-service/` | Service Python de reconnaissance faciale |
| `M1_FaceServer/` | Serveur Python de tête 3D FLAME (option de M1) |
| `supabase/` | Fonction de paiement Stripe |
| `docs/` | System design, logo, récapitulatifs, ce guide |
| `render.yaml` | Déploiement des serveurs sur Render |
| `README.md` | Présentation et organisation du projet |
| `Library/`, `Logs/`, `UserSettings/` | Générés par Unity, ne sont pas dans Git |

### Dans `Assets/`

| Dossier | Contenu |
| --- | --- |
| `DecoAR/Core/` | Socle commun : `GameManager`, `HapticManager`, `ShopTypes`, `AvatarLook`, icône de l'application, menu de build complet |
| `DecoAR/Auth/` | Écrans de connexion et de profil, appels à l'API, caméra du visage, défi de mouvements, réglages (`Resources/DecoAR_AuthSettings.asset`) |
| `DecoAR/M1_Player/` | Personnage : scripts, scènes `M1_Personnalisation` et `M1_ScanVisage`, avatars (`Mixamo/Avatars`), animations, matériaux, shaders, UI |
| `DecoAR/M3_ProductCatalog/` | Catalogue : données des produits (`Data`), étagères, inspection, recherche, scène `M3` |
| `DecoAR/M4_AR/` | Réalité augmentée : placement, lumière, photo, taille réelle, scène `M4_AR` |
| `DecoAR/m6_3D/` | Génération 3D : `M6Controller`, `M6ModelViewer`, `M6Navigation`, scènes `AdminScene` et `M6_Generator` |
| `DecoAR/Backend_M6/` | Serveur Flask de M6 (`app.py`, `trellis_service.py`) et exemples de modèles générés (`generated_models/*.glb`) |
| `Checkout/` | Module M5 : boutique, caisse, paiement, capteurs, sons, animations, modèles (comptoir, télé, carton), plugins de la page web, scène `caisse` |
| `MobileARTemplateAssets/`, `XR/`, `XRI/` | Template Mobile AR de Unity et réglages XR (ARCore, ARKit) |
| `Plugins/iOS/` | Code natif iOS : reconnaissance vocale (M3), vibrations (M4) |
| `Settings/` | Profils de rendu URP |
| `TextMesh Pro/`, `Samples/` | Ressources fournies par Unity |

### Organisation d'un module (même logique partout)

| Sous-dossier | Contenu |
| --- | --- |
| `Scripts/` | Code C# utilisé pendant le jeu |
| `Editor/` | Outils de l'éditeur Unity (menus `DecoAR > …`) qui **génèrent les scènes et les prefabs par code**. Ils ne sont pas inclus dans l'application |
| `Scenes/` | Scènes `.unity` |
| `Prefabs/` | Objets réutilisables |
| `Materials/`, `Shaders/` | Apparence des objets |
| `Data/` | ScriptableObjects : profil du personnage, catalogue |
| `UI/` | Sprites et éléments d'interface |

---

## 7. Fichiers importants à connaître

### M1 (ma partie)

| Fichier | Rôle |
| --- | --- |
| `CharacterProfile.cs` | Les avatars disponibles et les couleurs proposées pour chaque partie |
| `CharacterCustomizer.cs` | Écran de personnalisation : onglets, pastilles de couleur, choix de l'avatar |
| `CharacterAppearance.cs` | Applique le look (couleurs et visage) aux matériaux de l'avatar |
| `FaceScan3D.cs` | Scan 3D du visage avec ARCore / ARKit, calculé sur le téléphone |
| `FaceScanner.cs` / `SkinTone.cs` | Scan simple : mesure la couleur de la peau et des cheveux |
| `FaceMaskStore.cs`, `FaceStore.cs`, `HeadStore.cs` | Enregistrent le visage dans le stockage privé du téléphone |
| `PlayerController.cs` | Déplacement, animations, vibration de collision |
| `ThirdPersonCamera.cs` | Caméra qui ne traverse pas les murs, gyroscope |
| `Interactable.cs` / `PlayerInteractor.cs` | Objets avec lesquels on peut interagir, et bouton contextuel |
| `HUDController.cs`, `VirtualJoystick.cs` | HUD et joystick |
| `Editor/M1SceneBuilder.cs` | Génère les scènes M1 par code |
| `Editor/M1MetaPersonSetup.cs` | Importe les avatars MetaPerson et crée leurs matériaux |
| `M1_FaceServer/server.py` | Serveur de la tête 3D FLAME (option) |

### Les autres modules

| Fichier | Rôle |
| --- | --- |
| `Core/Scripts/GameManager.cs` | État partagé entre les scènes |
| `Auth/Scripts/AuthScreen.cs`, `AuthApi.cs`, `FaceChallenge.cs` | Connexion, appels à l'API, défi de mouvements |
| `Auth/Scripts/AuthOverlay.cs` | Bouton profil et verrouillage automatique |
| `M3_ProductCatalog/Scripts/M3CatalogSelector.cs` | Écran du rayon |
| `M3_ProductCatalog/Scripts/M3Inspector.cs` | Rotation et zoom du produit |
| `M3_ProductCatalog/Scripts/M3ProductCatalog.cs` | Format du catalogue, partagé par M3, M4 et M5 |
| `M4_AR/Scripts/M4PlacementGate.cs` | Placement en AR, choix du sol, variantes |
| `M4_AR/Scripts/M4LightMatcher.cs` | Estimation de la lumière |
| `Checkout/Scripts/UI/M5CheckoutUI.cs` | HUD et caisse |
| `Checkout/Scripts/Payment/M5StripePayment.cs` | Paiement Stripe |
| `Checkout/Scripts/Sensors/M5LocationService.cs` | GPS et adresse |
| `m6_3D/Scripts/M6Controller.cs` | Envoi de la photo et suivi de la génération |
| `m6_3D/Scripts/M6Navigation.cs` | Liens Profil, Administration et M6 |
| `Backend/src/routes/auth.js`, `users.js` | Routes de l'API |
| `Backend/src/models/User.js` | Modèle de l'utilisateur dans MongoDB |
| `face-service/app.py` | Calcul du vecteur du visage (`/embed`) |

---

## 8. Échanges entre l'application et les serveurs

| Appel | Serveur | Rôle |
| --- | --- | --- |
| `POST /api/auth/register`, `/login` | Backend Node | Inscription et connexion |
| `POST /api/auth/verify-email`, `/forgot-password`, `/reset-password` | Backend Node | Codes par e-mail |
| `POST /api/auth/face/challenge`, `/face/pose`, `/face/login` | Backend Node, qui appelle face-service | Défi de mouvements et connexion par le visage |
| `GET/PATCH /api/users/me`, `PUT /me/photo`, `PUT /me/face` | Backend Node | Profil |
| `POST /embed` | face-service | Photo transformée en vecteur de 128 nombres |
| `POST /head` | M1_FaceServer | 3 photos transformées en tête 3D FLAME (option) |
| `POST /generate`, `GET /status/<id>`, `GET /model/<id>` | Backend M6 | Photo transformée en modèle GLB |
| `POST` et `GET /stripe-checkout` | Supabase | Création et vérification du paiement |

**Données stockées**

- **MongoDB** : utilisateurs, mots de passe hachés (bcrypt), vecteurs du visage, photos de profil.
- **Sur le téléphone** (`PlayerPrefs` et stockage privé) : jeton de session, look du personnage, visage scanné. Les photos du visage de M1 ne quittent pas le téléphone avec le plan B.
- **Pas de base pour le catalogue** : il est dans un ScriptableObject de Unity.

---

## 9. Sécurité et vie privée

- Les mots de passe sont **hachés avec bcrypt** et ne sont jamais stockés en clair.
- La session utilise un **jeton JWT** qui expire au bout de 7 jours.
- Le nombre d'essais est **limité** (protection contre la force brute).
- La **détection du vivant** par des mouvements au hasard bloque l'utilisation d'une photo.
- Le visage est stocké sous forme de **vecteur**, pas comme une photo. La comparaison se fait côté serveur.
- La **clé secrète Stripe** n'est jamais dans l'application : elle reste sur Supabase.
- Les secrets (`.env`, `MONGODB_URI`, `JWT_SECRET`) restent hors de Git.
- Le scan M1 est calculé **sur le téléphone** : aucune photo n'est envoyée.
- Les textes d'autorisation (caméra, micro, GPS) sont en français.

---

## 10. Questions probables du jury et réponses

**Pourquoi Unity ?**
C'est un moteur 3D multiplateforme (Android et iOS avec le même code) et il dispose d'AR Foundation, qui couvre ARCore et ARKit avec une seule API.

**Quelle différence entre ARCore et AR Foundation ?**
ARCore est la technologie AR de Google pour Android. ARKit est celle d'Apple. AR Foundation est la couche Unity qui utilise l'une ou l'autre selon le téléphone.

**Comment marche le placement en taille réelle ?**
AR Foundation détecte les plans du sol. Le produit est modélisé en mètres, avec ses dimensions réelles en cm dans le catalogue. Il est posé sur le plan horizontal le plus bas (le sol) à l'échelle 1:1.

**Comment le produit s'intègre-t-il à la lumière de la pièce ?**
ARCore estime l'intensité et la couleur de la lumière ambiante, et `M4LightMatcher` applique ces valeurs à la lumière de la scène.

**Comment passez-vous les données d'une scène à l'autre ?**
Avec le `GameManager`, un singleton marqué `DontDestroyOnLoad`. Il garde le look du personnage, le produit choisi, le panier et la position du joueur.

**Comment marche le scan du visage ? Pourquoi le plan B ?**
ARCore / ARKit fournit le maillage 3D du visage avec la caméra frontale. On le fige, on y colle l'image de la caméra, et on le pose sur la tête de l'avatar.
Le plan A (serveur FLAME) demande un PC allumé sur le même Wi-Fi. Le plan B marche partout, plus vite, et sans envoyer de photo : c'est meilleur pour la vie privée. Le plan A reste disponible en option.

**Et si le téléphone ne supporte pas ARCore ?**
Le scan photo simple prend le relais : il mesure la couleur de la peau et des cheveux, et le personnage les applique.

**Comment marche Face ID ?**
1. Le serveur demande des mouvements tirés au hasard, pour vérifier que c'est une vraie personne.
2. Les photos sont envoyées au service OpenCV : YuNet détecte le visage, SFace le transforme en vecteur de 128 nombres.
3. Le backend compare ce vecteur à celui enregistré (similarité cosinus).

**Comment le paiement est-il sécurisé ?**
L'application n'a jamais la clé Stripe. Elle demande une session de paiement à une fonction Supabase, ouvre la page Stripe dans l'application, puis vérifie le statut du paiement auprès du serveur avant de valider.

**Comment marche la génération 3D (M6) ?**
La photo est envoyée au serveur Flask, qui appelle le modèle d'IA TRELLIS sur Hugging Face. Le résultat est un fichier GLB, chargé dans Unity avec glTFast.

**Pourquoi les scènes sont-elles générées par code (dossiers `Editor/`) ?**
Les scènes deviennent reproductibles. Il y a moins de conflits Git entre les membres, et on peut tout reconstruire avec un menu.

**Comment avez-vous travaillé en équipe ?**
Chaque membre a sa branche Git et son dossier de module. Les modules communiquent par le `Core` (`GameManager`, `SceneNames`, format commun du catalogue). Toutes les branches ont été fusionnées dans `integration-modules`.

**Quels capteurs utilisez-vous ?**
Caméras avant et arrière, gyroscope, accéléromètre, GPS, micro et vibreur (voir la partie 5).

**Quelles sont les limites ?**
- L'AR et le scan 3D demandent un téléphone compatible ARCore ou ARKit.
- M6 et M1_FaceServer doivent tourner sur un PC du même réseau.
- La génération 3D dépend de Hugging Face (quota, temps d'attente).
- Dans la boutique M5, le joueur est en première personne : l'avatar M1 sert surtout à la personnalisation.

**Améliorations possibles ?**
- Héberger M6 en ligne.
- Mettre le catalogue dans une base de données.
- Ajouter le multijoueur.
- Afficher l'avatar dans la boutique en troisième personne.
- Ajouter l'historique des commandes dans le profil.

---

## 11. Démonstration conseillée

1. Lancer le serveur Auth (`npm run dev` dans `Backend`), ou utiliser la version en ligne sur Render.
2. Sur le **Samsung A57** :
   1. se connecter, si possible par le visage ;
   2. choisir un avatar, changer les couleurs, puis faire le scan du visage ;
   3. entrer dans la boutique, regarder autour de soi avec le gyroscope, puis s'approcher de la télé ;
   4. ouvrir un produit, le tourner, zoomer, changer de variante, secouer le téléphone ;
   5. « Voir en AR » : poser le produit au sol, utiliser « Taille réelle », prendre une photo ;
   6. ajouter au panier, aller à la caisse, utiliser le GPS, payer avec la carte de test Stripe ;
   7. aller dans Profil, puis Espace administration (3D), puis montrer un modèle déjà généré.
3. Avoir des captures ou une vidéo en secours, au cas où le Wi-Fi ou le serveur ne fonctionnerait pas.
