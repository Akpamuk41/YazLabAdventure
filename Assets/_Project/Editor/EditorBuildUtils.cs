using UnityEditor;
using UnityEngine;

namespace YazLab.EditorTools
{
    /// <summary>Seviye kurulumu sırasında kullanılan sahne/model/serileştirme yardımcıları.</summary>
    public static class EditorBuildUtils
    {
        public const string ModelsFolder = "Assets/ThirdParty/Kenney_PlatformerKit/Models";
        private const string MaterialsFolder = "Assets/_Project/Materials";

        public static Material GetMaterial(string name, Color color, float smoothness = 0.2f)
        {
            string path = $"{MaterialsFolder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }

            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(material);
            return material;
        }

        public static GameObject Group(string name, Transform parent = null)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            return go;
        }

        public static GameObject Box(string name, Vector3 center, Vector3 size, Material material, int layer, Transform parent)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = material;
            go.layer = layer;
            return go;
        }

        /// <summary>Görünmez, yalnızca çarpışma için kutu.</summary>
        public static GameObject InvisibleBox(string name, Vector3 center, Vector3 size, int layer, Transform parent, bool isTrigger = false)
        {
            var go = new GameObject(name) { layer = layer };
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            var box = go.AddComponent<BoxCollider>();
            box.size = size;
            box.isTrigger = isTrigger;
            return go;
        }

        public static GameObject InstantiateModel(string modelName, Transform parent)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelsFolder}/{modelName}.fbx");
            if (prefab == null)
            {
                Debug.LogError($"[YazLab] Model bulunamadı: {modelName}");
                return new GameObject($"MISSING_{modelName}");
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            instance.transform.localScale = Vector3.one;
            if (parent != null) instance.transform.SetParent(parent, true);
            return instance;
        }

        /// <summary>Nesne kimlik dönüşümündeyken renderer sınırları = yerel sınırlar.</summary>
        public static Bounds LocalBounds(GameObject go)
        {
            var t = go.transform;
            var parent = t.parent;
            var position = t.position;
            var rotation = t.rotation;
            var scale = t.localScale;

            t.SetParent(null, false);
            t.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            t.localScale = Vector3.one;

            var renderers = go.GetComponentsInChildren<Renderer>();
            var bounds = renderers.Length > 0 ? renderers[0].bounds : new Bounds(Vector3.zero, Vector3.one);
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);

            t.SetParent(parent, false);
            t.SetPositionAndRotation(position, rotation);
            t.localScale = scale;
            return bounds;
        }

        /// <summary>Modeli verilen dünya kutusuna (eksen hizalı) gererek yerleştirir.</summary>
        public static GameObject FitModelToBox(string modelName, string name, Vector3 center, Vector3 size, int layer,
            Transform parent, bool addCollider)
        {
            var go = InstantiateModel(modelName, parent);
            go.name = name;
            var b = LocalBounds(go);
            var scale = new Vector3(SafeDiv(size.x, b.size.x), SafeDiv(size.y, b.size.y), SafeDiv(size.z, b.size.z));
            go.transform.localScale = scale;
            go.transform.position = center - Vector3.Scale(b.center, scale);

            if (addCollider)
            {
                var box = go.AddComponent<BoxCollider>();
                box.center = b.center;
                box.size = b.size;
            }

            SetLayerRecursive(go, layer);
            return go;
        }

        /// <summary>Modeli yüksekliğe göre orantılı ölçekler ve tabanını verilen noktaya oturtur.</summary>
        public static GameObject PlaceModel(string modelName, string name, Vector3 groundPosition, float yaw, float height,
            Transform parent, int layer = 0, bool addCollider = false)
        {
            var go = InstantiateModel(modelName, parent);
            go.name = name;
            var b = LocalBounds(go);
            float s = SafeDiv(height, b.size.y);
            var rotation = Quaternion.Euler(0f, yaw, 0f);
            var pivotOffset = new Vector3(b.center.x, b.min.y, b.center.z) * s;

            go.transform.localScale = Vector3.one * s;
            go.transform.SetPositionAndRotation(groundPosition - rotation * pivotOffset, rotation);

            if (addCollider)
            {
                var box = go.AddComponent<BoxCollider>();
                box.center = b.center;
                box.size = b.size;
            }

            SetLayerRecursive(go, layer);
            return go;
        }

        public static Bounds WorldBounds(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            var bounds = renderers.Length > 0 ? renderers[0].bounds : new Bounds(go.transform.position, Vector3.zero);
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);
            return bounds;
        }

        public static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name) return root;
            foreach (Transform child in root)
            {
                var found = FindDeep(child, name);
                if (found != null) return found;
            }

            return null;
        }

        public static void SetLayerRecursive(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform) SetLayerRecursive(child.gameObject, layer);
        }

        private static float SafeDiv(float a, float b) => Mathf.Abs(b) < 0.0001f ? 1f : a / b;

        // --- Serileştirilmiş (private [SerializeField]) alanlara yazma ---

        public static void Set(Object target, string property, Object value) =>
            Modify(target, property, p => p.objectReferenceValue = value);

        public static void Set(Object target, string property, float value) =>
            Modify(target, property, p => p.floatValue = value);

        public static void Set(Object target, string property, int value) =>
            Modify(target, property, p => p.intValue = value);

        public static void Set(Object target, string property, bool value) =>
            Modify(target, property, p => p.boolValue = value);

        public static void Set(Object target, string property, string value) =>
            Modify(target, property, p => p.stringValue = value);

        public static void Set(Object target, string property, Object[] values) =>
            Modify(target, property, p =>
            {
                p.arraySize = values.Length;
                for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            });

        private static void Modify(Object target, string property, System.Action<SerializedProperty> apply)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(property);
            if (prop == null)
            {
                Debug.LogError($"[YazLab] {target.GetType().Name}.{property} bulunamadı.");
                return;
            }

            apply(prop);
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
