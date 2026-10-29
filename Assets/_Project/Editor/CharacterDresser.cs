using System.Linq;
using UnityEditor;
using UnityEngine;
using static YazLab.EditorTools.EditorBuildUtils;

namespace YazLab.EditorTools
{
    /// <summary>
    /// Kale temasındaki karakterlerin görünüşü: prenses savaşçı (oyuncu), şövalye bekçi (Oozi) ve mor Robi.
    /// KayKit şövalyesinde gelen fazla silahlar gizlenir.
    /// </summary>
    public static class CharacterDresser
    {
        public const string KayKitModels = "Assets/ThirdParty/KayKit_Adventurers/Models";
        public const string PrincessModel = "Assets/ThirdParty/Quaternius_ModularWomen/Models/Medieval.fbx";
        public const string KnightModel = KayKitModels + "/Knight.fbx";

        /// <summary>Kılıçlı kadın savaşçı modeli zaten hazır, üstüne yalnızca taç eklenir.</summary>
        public static void DressPrincess(GameObject model)
        {
            AddCrown(model, "Head", "Medieval_Head");
        }

        public static void DressKnight(GameObject model)
        {
            KeepOnly(model, "Knight_", "1H_Sword", "Badge_Shield");
            // Offhand kılıcı da "1H_Sword" ile başlıyor, onu ayrıca gizle.
            var offhand = model.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "1H_Sword_Offhand");
            if (offhand != null) offhand.gameObject.SetActive(false);
        }

        /// <summary>Robi'yi kale temasına uysun diye mor tona boyar.</summary>
        public static void TintRobi(GameObject model)
        {
            foreach (var renderer in model.GetComponentsInChildren<Renderer>())
            {
                var source = renderer.sharedMaterial;
                if (source == null) continue;

                string path = "Assets/_Project/Materials/Robi.mat";
                var tinted = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (tinted == null)
                {
                    tinted = new Material(source);
                    AssetDatabase.CreateAsset(tinted, path);
                }
                else
                {
                    tinted.CopyPropertiesFromMaterial(source);
                }

                tinted.SetColor("_BaseColor", new Color(0.82f, 0.7f, 1f));
                EditorUtility.SetDirty(tinted);
                renderer.sharedMaterial = tinted;
            }
        }

        private static void KeepOnly(GameObject model, params string[] prefixes)
        {
            foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                bool keep = prefixes.Any(p => renderer.name.StartsWith(p));
                if (!keep) renderer.gameObject.SetActive(false);
            }
        }

        private static Transform FindBone(Transform root, string name)
        {
            return root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name);
        }

        /// <summary>Başın tepesine basit altın taç (halka + sivri uçlar), kafa kemiğine bağlı.</summary>
        private static void AddCrown(GameObject model, string boneName, string headMeshName)
        {
            var head = FindBone(model.transform, boneName);
            var headMesh = model.GetComponentsInChildren<Renderer>().FirstOrDefault(r => r.name == headMeshName);
            if (head == null || headMesh == null) return;

            var bounds = headMesh.bounds;
            float width = bounds.size.x * 0.55f;
            var gold = GetMaterial("Gold", new Color(1f, 0.78f, 0.2f), 0.7f);

            var crown = new GameObject("Crown") { layer = model.layer };
            crown.transform.position = new Vector3(bounds.center.x, bounds.max.y - width * 0.12f, bounds.center.z);

            var ring = Primitive(PrimitiveType.Cylinder, "Ring", crown.transform, gold);
            ring.transform.localScale = new Vector3(width, width * 0.1f, width);

            const int spikes = 5;
            for (int i = 0; i < spikes; i++)
            {
                float angle = i * Mathf.PI * 2f / spikes;
                var spike = Primitive(PrimitiveType.Cube, $"Spike_{i}", crown.transform, gold);
                spike.transform.localPosition = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * width * 0.42f
                                                + Vector3.up * width * 0.17f;
                spike.transform.localRotation = Quaternion.Euler(0f, -angle * Mathf.Rad2Deg, 45f);
                spike.transform.localScale = Vector3.one * width * 0.17f;
            }

            crown.transform.SetParent(head, true);
            SetLayerRecursive(crown, model.layer);
        }

        private static GameObject Primitive(PrimitiveType type, string name, Transform parent, Material material)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }
    }
}
