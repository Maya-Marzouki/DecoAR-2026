using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace DecoAR.M6
{
    // Catalogue de l'administration : les modeles 3D generes et valides par l'admin.
    // Stocke sur l'appareil (persistentDataPath/m6_catalogue) : un GLB par produit + catalogue.json.
    // Le catalogue de la boutique (M3, M3GeneratedCatalog) lit cette liste : chaque produit apparait dans son rayon.
    public static class M6Catalog
    {
        [Serializable]
        public class Item
        {
            public string id;
            public string name;
            public string category;
            public string glb;   // nom du fichier GLB dans le dossier du catalogue
            public long createdAt; // secondes Unix
            public float price;    // TND (0 = prix par defaut du rayon)
            public float heightCm; // hauteur reelle (0 = hauteur par defaut du rayon)
            public string Path => System.IO.Path.Combine(Folder, glb);
            public DateTime Created => DateTimeOffset.FromUnixTimeSeconds(createdAt).LocalDateTime;
        }

        [Serializable] class Store { public List<Item> items = new List<Item>(); }

        /// Les rayons de la boutique (M3) : un produit genere apparait dans le rayon choisi.
        public static string[] Categories => DecoAR.M3.M3Categories.All;

        /// Hauteur reelle par defaut selon le rayon (cm), pour la taille en AR.
        public static float DefaultHeight(string category)
        {
            switch (category)
            {
                case "Salon": return 85f;
                case "Chambre": return 90f;
                case "Cuisine": return 85f;
                case "Salle de bain": return 80f;
                case "Luminaires": return 55f;
                case "Tableaux": return 60f;
                case "Plantes": return 70f;
                case "Multimédia": return 50f;
                case "Bureau & loisirs": return 75f;
                default: return 30f; // Objets deco
            }
        }

        static string Folder => System.IO.Path.Combine(Application.persistentDataPath, "m6_catalogue");
        static string File_ => System.IO.Path.Combine(Folder, "catalogue.json");

        public static List<Item> All()
        {
            try
            {
                if (!File.Exists(File_)) return new List<Item>();
                var s = JsonUtility.FromJson<Store>(File.ReadAllText(File_));
                var list = s?.items ?? new List<Item>();
                list.RemoveAll(i => !File.Exists(i.Path)); // fichier supprime entre-temps
                list.Sort((a, b) => b.createdAt.CompareTo(a.createdAt));
                return list;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[M6] Catalogue illisible : " + e.Message);
                return new List<Item>();
            }
        }

        /// Copie le GLB dans le catalogue et l'enregistre. Retourne l'element ajoute.
        public static Item Add(string sourceGlb, string name, string category, float price = 0f, float heightCm = 0f)
        {
            Directory.CreateDirectory(Folder);
            var all = All();
            var item = new Item
            {
                id = "P" + (all.Count + 1).ToString("000") + "-" + DateTime.Now.ToString("HHmmss"),
                name = string.IsNullOrWhiteSpace(name) ? "Produit sans nom" : name.Trim(),
                category = category,
                createdAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                price = Mathf.Max(0f, price),
                heightCm = Mathf.Max(0f, heightCm),
            };
            item.glb = item.id + ".glb";
            File.Copy(sourceGlb, item.Path, true);
            all.Add(item);
            File.WriteAllText(File_, JsonUtility.ToJson(new Store { items = all }, true));
            return item;
        }

        public static void Remove(Item item)
        {
            var all = All();
            all.RemoveAll(i => i.id == item.id);
            try { if (File.Exists(item.Path)) File.Delete(item.Path); } catch (Exception) { }
            File.WriteAllText(File_, JsonUtility.ToJson(new Store { items = all }, true));
        }
    }
}
