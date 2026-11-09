using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DecoAR.Core;
using GLTFast;
using GLTFast.Materials;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DecoAR.M3
{
    /// Produits générés en 3D par l'administration (M6) dans le catalogue de la boutique :
    /// chaque modèle ajouté au catalogue 3D (M6Catalog) devient un produit de son rayon, avec son prix,
    /// sa taille réelle et son type de pose. Les modèles GLB sont chargés une fois (glTFast) et gardés
    /// pour toute la session : l'étagère, l'AR (M4) et le panier de la caisse (M5) les utilisent comme
    /// n'importe quel produit.
    public static class M3GeneratedCatalog
    {
        const string IdPrefix = "GEN-";

        static readonly Dictionary<string, ProductOption> products = new Dictionary<string, ProductOption>();
        static readonly List<GltfImport> importers = new List<GltfImport>();
        static Transform root;
        static Host host;
        static bool loading;

        public static IEnumerable<ProductOption> Products => products.Values;

        /// Produit généré par son id (« GEN-… »), null si inconnu.
        public static ProductOption Get(string id) => id != null && products.TryGetValue(id, out var p) ? p : null;

        /// Met à jour la liste (nouveaux produits chargés, produits supprimés retirés), puis appelle done(nouveaux).
        public static void Refresh(Action<List<ProductOption>> done)
        {
            if (host == null)
            {
                var go = new GameObject("M3_ProduitsGeneres");
                UnityEngine.Object.DontDestroyOnLoad(go);
                host = go.AddComponent<Host>();
                var holder = new GameObject("Modeles");
                holder.transform.SetParent(go.transform, false);
                holder.SetActive(false); // modèles de référence, jamais affichés : on les copie
                root = holder.transform;
            }
            host.StartCoroutine(RefreshRoutine(done));
        }

        static IEnumerator RefreshRoutine(Action<List<ProductOption>> done)
        {
            while (loading) yield return null;
            loading = true;
            var added = new List<ProductOption>();
            var items = DecoAR.M6.M6Catalog.All();
            var alive = new HashSet<string>();
            foreach (var item in items)
            {
                string id = IdPrefix + item.id;
                alive.Add(id);
                if (products.ContainsKey(id)) continue;
                var task = Build(item, id);
                while (!task.IsCompleted) yield return null;
                if (task.Result != null) { products[id] = task.Result; added.Add(task.Result); }
            }
            // Produits supprimés dans l'administration
            var gone = new List<string>();
            foreach (var id in products.Keys) if (!alive.Contains(id)) gone.Add(id);
            foreach (var id in gone) { if (products[id].prefab) UnityEngine.Object.Destroy(products[id].prefab); products.Remove(id); }
            loading = false;
            done?.Invoke(added);
        }

        static async Task<ProductOption> Build(DecoAR.M6.M6Catalog.Item item, string id)
        {
            if (!File.Exists(item.Path)) return null;
            var pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            var importer = pipeline != null ? new GltfImport(materialGenerator: new UniversalRPMaterialGenerator(pipeline)) : new GltfImport();

            // Chargé loin de la scène, actif le temps de mesurer le modèle, puis rangé avec les modèles de référence
            var model = new GameObject("Genere_" + item.id);
            model.transform.position = new Vector3(0f, -500f, 0f);
            try
            {
                if (!await importer.Load(new Uri(Path.GetFullPath(item.Path)).AbsoluteUri)) throw new Exception("import GLB impossible");
                if (!await importer.InstantiateMainSceneAsync(model.transform)) throw new Exception("instanciation impossible");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[M3] Produit généré « {item.name} » ignoré : {e.Message}");
                UnityEngine.Object.Destroy(model);
                importer.Dispose();
                return null;
            }
            importers.Add(importer); // les textures du modèle vivent avec l'importeur : on le garde

            var renderers = model.GetComponentsInChildren<Renderer>();
            var b = renderers.Length > 0 ? renderers[0].bounds : new Bounds(model.transform.position, Vector3.one * 0.3f);
            foreach (var r in renderers) b.Encapsulate(r.bounds);
            foreach (var c in model.GetComponentsInChildren<Collider>()) UnityEngine.Object.Destroy(c);
            model.transform.SetParent(root, false);
            model.transform.localPosition = Vector3.zero;

            string category = Normalize(item.category);
            float heightCm = item.heightCm > 0 ? item.heightCm : DecoAR.M6.M6Catalog.DefaultHeight(category);
            float k = b.size.y > 0.0001f ? heightCm / (b.size.y * 100f) : 1f;
            var size = b.size * 100f * k;

            return new ProductOption
            {
                id = id,
                displayName = item.name,
                category = category,
                price = item.price > 0 ? item.price : DefaultPrice(category),
                sizeCm = new Vector3(Mathf.Max(1f, Mathf.Round(size.x)), Mathf.Max(1f, Mathf.Round(size.y)), Mathf.Max(1f, Mathf.Round(size.z))),
                prefab = model,
                description = "Modèle 3D généré à partir d'une photo, ajouté par l'administration DecoAR.",
                placement = Placement(category),
                variants = new List<ProductVariant> { new ProductVariant { name = "Original", color = Color.white } },
                keywords = "nouveau 3d genere " + item.name,
            };
        }

        /// Anciennes catégories de l'administration → rayons de la boutique.
        static string Normalize(string category)
        {
            if (Array.IndexOf(M3Categories.All, category) >= 0) return category;
            if (category == "Décoration") return M3Categories.ObjetsDeco;
            return M3Categories.ObjetsDeco;
        }

        static ProductPlacement Placement(string category)
        {
            if (category == M3Categories.Tableaux) return ProductPlacement.Mur;
            if (category == M3Categories.Luminaires || category == M3Categories.ObjetsDeco || category == M3Categories.Multimedia) return ProductPlacement.Surface;
            return ProductPlacement.Sol;
        }

        static float DefaultPrice(string category)
        {
            switch (category)
            {
                case M3Categories.Salon: case M3Categories.Chambre: case M3Categories.Cuisine: return 490f;
                case M3Categories.Luminaires: case M3Categories.Tableaux: return 120f;
                default: return 59f;
            }
        }

        class Host : MonoBehaviour { }
    }
}
