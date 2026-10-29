using UnityEngine;
using static YazLab.EditorTools.EditorBuildUtils;
using static YazLab.EditorTools.ProjectSetup;

namespace YazLab.EditorTools
{
    /// <summary>
    /// Gri kutu duvarları kale duvarı modelleriyle giydirir, kapının yanlarına ve köşelere kule koyar.
    /// Çarpışma kutuları aynen kalır (NavMesh ve testler değişmesin), sadece görünüşleri gizlenir.
    /// </summary>
    public static class CastleDresser
    {
        private const string Models = "Assets/ThirdParty/Kenney_CastleKit/Models/";
        private const float TileLength = 2.2f;
        private const float VisualThickness = 0.7f;
        private const float ExtraHeight = 0.5f;

        private static readonly string[] WallPrefixes = { "Wall_", "GateWall_", "QuestWall_", "Maze_W" };

        private static readonly Vector3[] CornerTowers =
        {
            new(-12.2f, 0f, -0.2f),
            new(30.2f, 0f, -0.2f),
            new(30.2f, 0f, 20.2f),
            new(-12.2f, 0f, 72.2f),
            new(12.2f, 0f, 72.2f)
        };

        public static void Dress(Transform environment, Transform door)
        {
            var castle = Group("Castle", environment).transform;

            foreach (var t in environment.GetComponentsInChildren<Transform>())
            {
                if (!IsWall(t.name)) continue;
                DressWall(t, castle);
            }

            foreach (var corner in CornerTowers)
            {
                Tower("CornerTower", corner, 2.2f, 4.2f, castle);
            }

            if (door != null)
            {
                var bounds = DoorBounds(door);
                float x = bounds.extents.x + 1.1f;
                Tower("GateTower_L", new Vector3(bounds.center.x - x, 0f, door.position.z), 2.2f, 5.5f, castle);
                Tower("GateTower_R", new Vector3(bounds.center.x + x, 0f, door.position.z), 2.2f, 5.5f, castle);
            }
        }

        private static bool IsWall(string name)
        {
            foreach (var prefix in WallPrefixes)
            {
                if (name.StartsWith(prefix)) return true;
            }

            return false;
        }

        private static void DressWall(Transform box, Transform parent)
        {
            var renderer = box.GetComponent<MeshRenderer>();
            if (renderer != null) renderer.enabled = false;

            var size = box.localScale;
            bool alongX = size.x >= size.z;
            float length = alongX ? size.x : size.z;
            float height = size.y + ExtraHeight;
            int count = Mathf.Max(1, Mathf.RoundToInt(length / TileLength));
            float tile = length / count;

            var axis = alongX ? Vector3.right : Vector3.forward;
            var start = box.position - axis * (length / 2f);
            for (int i = 0; i < count; i++)
            {
                var center = start + axis * (tile * (i + 0.5f));
                center.y = 0f;
                Segment($"{box.name}_{i}", center, alongX ? 0f : 90f, tile, height, parent);
            }
        }

        /// <summary>Duvar modelini kendi ekseninde (uzunluk x yükseklik x kalınlık) kutuya oturtur.</summary>
        private static void Segment(string name, Vector3 groundCenter, float yaw, float length, float height, Transform parent)
        {
            var holder = new GameObject(name);
            holder.transform.SetParent(parent, false);
            holder.transform.SetPositionAndRotation(groundCenter, Quaternion.Euler(0f, yaw, 0f));

            var model = InstantiateModel(Models + "wall.fbx", null);
            var b = LocalBounds(model);
            // Model z ekseninde uzunsa x'e çevir.
            bool modelAlongZ = b.size.z > b.size.x;
            float modelLength = modelAlongZ ? b.size.z : b.size.x;
            float modelDepth = modelAlongZ ? b.size.x : b.size.z;

            model.transform.SetParent(holder.transform, false);
            model.transform.localRotation = Quaternion.Euler(0f, modelAlongZ ? 90f : 0f, 0f);
            var scale = new Vector3(length / modelLength, height / b.size.y, VisualThickness / modelDepth);
            model.transform.localScale = modelAlongZ ? new Vector3(scale.z, scale.y, scale.x) : scale;

            var offset = new Vector3(b.center.x, b.min.y, b.center.z);
            model.transform.localPosition = -(model.transform.localRotation * Vector3.Scale(offset, model.transform.localScale));
        }

        private static void Tower(string name, Vector3 groundPosition, float width, float height, Transform parent)
        {
            var tower = PlaceModel(Models + "tower-square.fbx", name, groundPosition, 0f, height, parent);
            var b = LocalBounds(tower);
            float s = width / Mathf.Max(b.size.x, b.size.z);
            tower.transform.localScale = new Vector3(s, tower.transform.localScale.y, s);

            InvisibleBox(name + "_Collider", groundPosition + Vector3.up * (height / 2f),
                new Vector3(width, height, width), ObstacleLayer, tower.transform.parent);
        }

        private static Bounds DoorBounds(Transform door)
        {
            var renderers = door.GetComponentsInChildren<Renderer>();
            var bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);
            return bounds;
        }
    }
}
