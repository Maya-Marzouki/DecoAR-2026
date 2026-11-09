# DecoAR : modules M3 et M4 (travail de Maya Marzouki)

Ce document décrit ce qui a été réalisé pour **M3 (bibliothèque de produits)** et **M4 (réalité augmentée)** : les fonctionnalités, les scripts et les fonctions principales, et leur emplacement dans le projet Unity.

---

## 1. Vue d'ensemble du parcours

```
Boutique (Interactable « etagere_… ») ──► M3 : rayon / catalogue ──► « Voir en AR » ──► M4 : placement AR
                                              │                                          │
                                              └──── « Ajouter au panier » ──► GameManager ◄┘ ──► M5 (caisse)
```

- **GameManager** (socle commun) transporte le produit choisi entre les scènes : prefab, dimensions, type de pose et variante. Il garde aussi le panier.
- **Scènes :** `Assets/DecoAR/M3_ProductCatalog/Scenes/M3.unity` et `Assets/DecoAR/M4_AR/Scenes/M4_AR.unity`.

---

## 2. Socle commun (Core) utilisé par M3 et M4

| Fichier | Rôle | Éléments principaux |
|---|---|---|
| `Assets/DecoAR/Core/Scripts/GameManager.cs` | État partagé entre les scènes, persistant (`DontDestroyOnLoad`) | `SelectProduct(id, prefab, sizeCm, placement, variante)`, `SetSelectedVariant()`, `SelectedProduct*`, `CurrentCategory`, `AddToCart(CartItem)`, `TakeCartItems()` (lu par M5), `CartCount`, événements `ProductSelected` et `CartItemAdded` |
| `Assets/DecoAR/Core/Scripts/ShopTypes.cs` | Types partagés | `enum ProductPlacement { Sol, Mur, Surface }`, `SceneNames` (noms des scènes : Shop, Catalog, AR, Checkout), `CartItem` (id, nom, variante, couleur, prix, quantité) |
| `Assets/DecoAR/Core/Scripts/HapticManager.cs` | Service unique de vibration (cahier 9.1) | `Vibrate(HapticIntensity)` : intensités Légère, Moyenne et Forte sur iOS, via le plugin natif ; respecte l'interrupteur des paramètres |
| `Assets/Plugins/iOS/DecoARM4Haptics.mm` | Plugin natif iOS des vibrations | `_DecoARM4_Impact(style)` (UIImpactFeedbackGenerator) |

---

## 3. Module M3 : bibliothèque de produits

Dossier : `Assets/DecoAR/M3_ProductCatalog/`

### 3.1 Fonctionnalités réalisées (cahier des charges)

| Exigence | Réalisation |
|---|---|
| F3.1 Étagère par zone remplie depuis le catalogue | `M3Shelf.Fill()` instancie les produits de la catégorie sur les emplacements de l'étagère 3D |
| F3.2 Sortie animée du produit face à la caméra | `M3Inspector.Inspect()` : animation en arc vers le point d'inspection |
| F3.3 Inspection 360° et zoom par pincement | `M3Inspector.Update()` : glisser pour tourner, pincer pour zoomer (souris et molette dans l'éditeur) |
| F3.4 Variantes appliquées en direct | `M3VariantApplier.Apply()` teinte les pièces nommées `Variant…` du modèle |
| F3.5 Fiche produit | Nom, prix en TND, dimensions, type de pose, description et boutons de variantes (`M3CatalogSelector.FillSheet()`) |
| F3.6 « Voir en AR » et « Ajouter au panier » | `M3CatalogSelector.ViewInAR()` charge M4 ; `AddToCart()` appelle `GameManager.AddToCart()` |
| F3.7 Recherche vocale | `M3VoiceSearch` + plugin iOS `DecoARM3Speech.mm` (framework Speech, en français) ; affiche ce qui a été compris |
| F3.8 Secouer pour reposer | `M3ShakeDetector` : seuil d'accélération + délai anti-rebond ; touche **R** dans l'éditeur |
| F3.9 Recherche texte de secours | Champ de recherche + `M3ProductCatalog.Search()`, sans tenir compte des accents ni de la casse |
| F3.10 Vibration légère | À la sélection et à l'ajout au panier (`HapticManager.Vibrate(Legere)`) |
| Critère : au moins 5 produits par zone, au moins 2 variantes | Catalogue de plus de 100 produits en 10 zones (voir 3.3) |

### 3.2 Scripts et fonctions

| Fichier (`Scripts/`) | Rôle | Fonctions principales |
|---|---|---|
| `M3ProductCatalog.cs` | Format du catalogue (ScriptableObject, cahier 9.3) | Classes `ProductOption` (id, nom, catégorie, prix, `sizeCm`, prefab, description, `placement`, `variants`, `keywords`) et `ProductVariant` (nom, couleur, material, vignette) ; `M3Categories` (liste des zones) ; `Get(id)`, `GetByCategory()`, `Search()`, `Normalize()` |
| `M3CatalogSelector.cs` | Contrôleur de l'écran du rayon | `ShowZone()`, `BuildCatalog()` (liste de boutons), `SelectProduct()`, `FillSheet()`, `ApplyVariant()`, `StoreSelection()`, `ViewInAR()`, `AddToCart()`, `PutBack()`, `Search()`, `ToggleVoice()`, `BackToShop()` |
| `M3Shelf.cs` | Étagère 3D | `Fill(produits)`, `Clear()`, `Find(produit)`, `Highlight(résultats)` (met en évidence les produits trouvés par la recherche) |
| `M3ShelfItem.cs` | Produit posé sur l'étagère | Mémorise sa place (`Home…`), sa taille et son centre ; `SetHighlighted()` (petit rebond) |
| `M3Inspector.cs` | Sélection et inspection 3D | `Inspect(item)`, `ReturnCurrent()`, `ApplyVariant()`, `Release()` ; gestion du toucher (tap, rotation, pincement) ; événements `ItemTapped`, `Inspected`, `Returned` |
| `M3VariantApplier.cs` | Changement de couleur ou de matière (utilisé par M3 et M4) | `Apply(modèle, variante)` |
| `M3ShakeDetector.cs` | Accéléromètre (Input System) | Événement `Shaken` |
| `M3VoiceSearch.cs` | Recherche vocale | `StartListening()`, `StopListening()`, `IsAvailable` ; événements `Partial`, `Result`, `Failed` ; rappels natifs `OnNativeSpeech…` |
| `M3ShelfEntrance.cs` | Lien boutique → M3 | Une interaction avec un `Interactable` dont l'id commence par `etagere` (`etagere_salon`, `etagere_tableaux`…) ouvre M3 sur la bonne zone et mémorise la position de l'avatar |
| `M3ProductCatalogBridge.cs` | Utilitaire | `ApplySelectedProduct(id)` : envoie un produit à M4 sans passer par l'interface |
| `M3ResponsiveLayout.cs` *(si présent)* | Disposition portrait ou paysage automatique | Repositionne l'interface, la caméra et le point d'inspection selon l'orientation |

| Fichier (`Editor/`) | Rôle |
|---|---|
| `M3CatalogSetup.cs` | Outil éditeur de génération : produits, catalogue et scène (menu **DecoAR > M3**) |
| `M3IOSPostBuild.cs` | Après le build iOS : ajoute `Speech.framework` et les textes d'autorisation du micro et de la reconnaissance vocale dans `Info.plist` |

`Assets/Plugins/iOS/DecoARM3Speech.mm` : plugin natif de reconnaissance vocale (`_DecoARM3_StartSpeech`, `_DecoARM3_StopSpeech`).

### 3.3 Données et assets

| Emplacement | Contenu |
|---|---|
| `Data/M3_ProductCatalog.asset` | Le catalogue partagé, lu par M3, M4 et M5 |
| `Prefabs/Products/P_<ID>.prefab` | Un modèle 3D par produit (formes de base Unity), à la taille réelle, pivot au sol, face avant vers +Z. Les pièces `Variant…` changent de couleur |
| `Materials/M3_<couleur>.mat` | Materials URP/Lit partagés |
| `Prefabs/M3_ProductButton_Prefab`, `M3_VariantButton_Prefab` | Boutons de la liste de produits et des variantes |

**Zones (catégories) :** Tableaux, Luminaires, Objets déco, Salon, Chambre, Cuisine, Salle de bain, Plantes, Multimédia, Bureau & loisirs.

**Types de pose :** `Mur` (tableaux, miroirs, appliques…), `Sol` (canapés, lits, tables, armoires…), `Surface` (sol ou table : vases, lampes à poser…).

### 3.4 Comportements de l'écran M3
- Barre du haut : « ‹ Boutique », onglets des zones (rangée qu'on fait défiler), compteur du panier.
- Recherche texte + bouton Micro. Une explication s'affiche avant la première demande d'autorisation.
- Liste des produits en bas ; fiche produit avec variantes, « Voir en AR », « Ajouter au panier » et « Reposer (ou secouez) ».
- Au retour depuis l'AR, le produit et la variante choisis sont réaffichés.

---

## 4. Module M4 : réalité augmentée

Dossier : `Assets/DecoAR/M4_AR/`

### 4.1 Fonctionnalités réalisées (cahier des charges)

| Exigence | Réalisation |
|---|---|
| F4.1 Détection des plans horizontaux et verticaux | AR Foundation (`ARPlaneManager`, ARKit). Les surfaces détectées affichent le **logo DecoAR** (shader `M4LogoPlane`) à la place des points du template |
| F4.2 Placement selon le type | `M4PlacementGate.OnSpawnTriggered()` : tableau → mur seulement ; produit `Sol` → sol seulement (une table est refusée) ; `Surface` → sol ou table |
| F4.3 Taille réelle + ombre | `M4Sizing.FitToRealSize()` d'après les dimensions du catalogue ; ombre sur le *shadow catcher* |
| F4.4 Light estimation | `M4LightMatcher` : intensité et température de couleur de la lumière adaptées à la pièce |
| F4.5 Placer, déplacer, tourner, redimensionner | Porte-produit XRI (gestes) + bouton « Taille réelle » (`ResetToRealSize()`) |
| F4.6 Variante sans quitter l'AR | Bouton « Variante » → `M4PlacementGate.NextVariant()` |
| F4.7 Photo vers la galerie | `M4Photo.TakePhoto()` masque l'interface, capture l'écran et l'enregistre via Native Gallery |
| F4.8 « Ajouter au panier » / « Retour » | `M4ShopActions.AddToCart()`, `BackToCatalog()`, `BackToShop()` (l'avatar reprend sa position) |
| F4.9 Vibration à la pose | `HapticManager.Vibrate(Moyenne)` |
| F4.10 Indications à l'écran | « Balayez lentement le sol / le mur », « Touchez la surface… », messages de refus (`Update()` et `ShowTemporary()`) |

### 4.2 Scripts et fonctions

| Fichier (`Scripts/`) | Rôle | Fonctions principales |
|---|---|---|
| `M4PlacementGate.cs` | Cœur du placement AR | `Awake()` (lit le produit dans le GameManager, ou le produit de test `TAB-01` si la scène est lancée seule), `OnSpawnTriggered(position, normale)`, `BuildProduct()`, `AddShadowCatcher()`, `TryGetFloorHeight()`, `SetProduct()`, `NextVariant()`, `ResetToRealSize()`, `ShowTemporary()` |
| `M4Sizing.cs` | Mise à l'échelle réelle | `GetBounds()`, `FitToRealSize(modèle, sizeCm)` |
| `M4LightMatcher.cs` | Light estimation | `OnFrame()` : lit lumens et température ; `Update()` : transition douce |
| `M4Photo.cs` | Capture photo | `TakePhoto()`, `CaptureRoutine()` |
| `M4ShopActions.cs` | Boutons panier et retour | `AddToCart()`, `BackToCatalog()`, `BackToShop()` |
| `M4Haptics.cs` | Raccourci vers le HapticManager | `Play()`, `Medium()` |
| `M4Hud.cs` *(ajouté ensuite)* | Interface AR complémentaire | — |

| Fichier | Rôle |
|---|---|
| `Editor/M4SceneSetup.cs` | Menu **DecoAR > M4 > Reparer et completer la scene AR** : crée ou répare `M4_PlacementGate`, `M4_LightMatcher`, `M4_Photo`, l'interface `M4_Canvas` et ses boutons, branche l'événement XRI « Object Spawn Triggered » sur `OnSpawnTriggered` et installe le plan logo |
| `Shaders/M4LogoPlane.shader` | Logo DecoAR répété sur les surfaces détectées (bords adoucis, fondu d'apparition) |
| `Materials/M4_LogoPlane.mat` | Réglages : espacement, taille du logo, arrondi des coins, opacité, voile |
| `Prefabs/M4_LogoPlane.prefab` | Plan AR utilisé par l'`ARPlaneManager` |
| `Prefabs/M4_Holder_Floor` / `M4_Holder_Wall` | Porte-produits manipulables au sol et au mur |
| `Shaders/ShadowCatcher.shader`, `Materials/ShadowCatcher_Mat` | Ombre portée sous le produit |

### 4.3 Interface de la scène AR (`M4_Canvas`)
- Bandeau de messages en haut et nom du produit avec sa variante et son prix.
- Colonne de boutons : ‹ Rayon, Boutique, Variante, Taille réelle, Photo, Ajouter au panier.

---

## 5. Autorisations et build iOS
- **Caméra :** texte d'explication dans Player Settings (Camera Usage Description).
- **Galerie :** `ProjectSettings/NativeGallery.json`, en français.
- **Micro et reconnaissance vocale :** ajoutés automatiquement par `M3IOSPostBuild.cs`.
- **Build :** File > Build Profiles > iOS > Build, puis ouvrir le projet dans Xcode, choisir la Team et lancer sur l'iPhone.

## 6. Tests réalisés sur iPhone
Catalogue et zones, sélection, inspection, variantes, recherche texte et vocale, secousse, « Voir en AR », pose au mur et au sol avec refus des surfaces incompatibles, light estimation, photo dans la galerie, ajout au panier, vibrations, retour vers M3, logo sur les surfaces détectées.

## 7. Pour l'intégration avec l'équipe
- Étagères de la boutique : utiliser un `Interactable` avec l'id `etagere_<zone>`.
- Panier : M5 récupère les articles avec `GameManager.Instance.TakeCartItems()`.
- Noms des scènes : `Core/Scripts/ShopTypes.cs` → `SceneNames`.
- Scene List finale : écran d'entrée de M1 en n° 0, toutes les scènes cochées.
