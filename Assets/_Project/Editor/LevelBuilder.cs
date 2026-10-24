using System.Collections.Generic;
using System.IO;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using YazLab.CameraSystem;
using YazLab.Characters;
using YazLab.Companion;
using YazLab.Companion.Movement;
using YazLab.Companion.Sensors;
using YazLab.Core;
using YazLab.Interactables;
using YazLab.Npc;
using YazLab.Player;
using YazLab.Quests;
using YazLab.Testing;
using YazLab.UI;
using static YazLab.EditorTools.EditorBuildUtils;
using static YazLab.EditorTools.ProjectSetup;

namespace YazLab.EditorTools
{
    /// <summary>
    /// Ana oyun sahnesini koddan, tekrarlanabilir biçimde kurar.
    ///
    /// Yerleşim (y = 0 zemin):
    ///   Başlangıç alanı   X[-12,10]  Z[0,20]   oyuncu + Robi
    ///   Bakım alanı       X[10,30]   Z[0,20]   alçak tünelle girilen labirent, piston kapı, şalter
    ///   Uçurum            Z[20,28]             asma köprü (menteşe Z=28)
    ///   Karşı alan        X[-12,12]  Z[28,72]  ara bölüm (altınlar + geçit Z=47), NPC bekçi + kilitli kapı (Z=60) + hedef
    /// </summary>
    public static class LevelBuilder
    {
        public const string ScenePath = "Assets/_Project/Scenes/Main.unity";
        private const string NavMeshAssetPath = "Assets/_Project/Scenes/Main_NavMesh.asset";

        private const float WallHeight = 2.2f;
        private const float WallThickness = 0.4f;
        private const float TunnelClearance = 1.2f;
        private const float PlayerHeight = 1.8f;
        private const float PlayerRadius = 0.35f;

        private static readonly Vector3 PlayerSpawn = new(-4f, 0f, 6f);
        private static readonly Vector3 CompanionSpawn = new(-2f, 0f, 4.5f);
        private static readonly Vector3 LeverPosition = new(27.3f, 0f, 17.3f);
        private static readonly Vector3 LeverInteractionPoint = new(27.3f, 0f, 16.1f);

        private const float QuestGateZ = 47f;
        private const float QuestGateHalfWidth = 1.8f;

        private static readonly Vector3[] CoinPositions =
        {
            new(-8.5f, 0f, 36f),
            new(8.5f, 0f, 40f),
            new(-6f, 0f, 44.5f)
        };

        private static readonly Vector3[] TestSpawnPoints =
        {
            new(-9f, 0f, 3f),
            new(-8f, 0f, 17f),
            new(0f, 0f, 10f),
            new(6f, 0f, 2f),
            new(7f, 0f, 17f)
        };

        private static Material _wall, _wood, _rope, _hazard, _abyss, _metal;

        [MenuItem("YazLab/2 - Seviyeyi Oluştur")]
        public static void Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateMaterials();

            var environment = Group("Environment").transform;
            var gameplay = Group("Gameplay").transform;

            BuildLighting();
            BuildTerrain(environment);
            BuildBoundaries(environment);
            BuildMaintenanceArea(environment);
            var bridge = BuildBridge(gameplay);
            var piston = BuildPistonGate(gameplay);
            var lever = BuildLever(gameplay, bridge);
            var door = BuildGate(environment, gameplay);
            var quest = BuildMiddleArea(environment, gameplay, out var questGate, out var coins);
            BuildDecoration(environment);

            var player = BuildPlayer(out var playerInput);
            var companion = BuildCompanion(player.transform);
            Set(player.GetComponent<PlayerCommander>(), "companionBehaviour", companion);

            var camera = BuildCamera(player.transform, playerInput);
            Set(player.GetComponent<PlayerMotor>(), "cameraTransform", camera.transform);

            BuildTriggers(gameplay);
            BuildGuard(gameplay, door);
            BuildUi();
            BuildMenus(playerInput);
            var surface = BuildSystems();
            BuildTestRunner(companion, lever, bridge, piston);
            BuildQuestTestRunner(quest, questGate, coins, player.GetComponent<PlayerMotor>(), companion);

            BakeNavMesh(surface);
            ValidateLayout(surface);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log($"[YazLab] Sahne oluşturuldu: {ScenePath} (kapı: {door.name})");
        }

        private static void CreateMaterials()
        {
            _wall = GetMaterial("M_Wall", new Color(0.56f, 0.58f, 0.63f));
            _wood = GetMaterial("M_Wood", new Color(0.58f, 0.38f, 0.22f));
            _rope = GetMaterial("M_Rope", new Color(0.42f, 0.28f, 0.16f));
            _hazard = GetMaterial("M_Hazard", new Color(0.95f, 0.65f, 0.1f));
            _abyss = GetMaterial("M_Abyss", new Color(0.04f, 0.05f, 0.08f), 0f);
            _metal = GetMaterial("M_Metal", new Color(0.35f, 0.38f, 0.42f), 0.6f);
        }

        private static void BuildLighting()
        {
            var light = new GameObject("Directional Light").AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.3f;
            light.color = new Color(1f, 0.96f, 0.88f);
            light.shadows = LightShadows.Soft;
            light.transform.rotation = Quaternion.Euler(50f, -35f, 0f);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.62f, 0.72f, 0.85f);
            RenderSettings.ambientEquatorColor = new Color(0.5f, 0.55f, 0.55f);
            RenderSettings.ambientGroundColor = new Color(0.25f, 0.22f, 0.2f);
        }

        private static void BuildTerrain(Transform parent)
        {
            var terrain = Group("Terrain", parent).transform;

            // Başlangıç + bakım alanı tek blok, karşı alan ayrı blok; aradaki boşluk uçurum.
            FitModelToBox("block-grass-large", "Ground_Start", new Vector3(9f, -1.5f, 10f), new Vector3(42f, 3f, 20f), GroundLayer, terrain, true);
            FitModelToBox("block-grass-large", "Ground_Far", new Vector3(0f, -1.5f, 50f), new Vector3(24f, 3f, 44f), GroundLayer, terrain, true);

            Box("Abyss", new Vector3(5f, -25f, 30f), new Vector3(120f, 1f, 120f), _abyss, 0, terrain);
        }

        private static void BuildBoundaries(Transform parent)
        {
            var walls = Group("Boundaries", parent).transform;
            float h = WallHeight / 2f;

            Box("Wall_South", new Vector3(9f, h, -0.2f), new Vector3(42.8f, WallHeight, WallThickness), _wall, ObstacleLayer, walls);
            Box("Wall_West_Start", new Vector3(-12.2f, h, 10f), new Vector3(WallThickness, WallHeight, 20f), _wall, ObstacleLayer, walls);
            Box("Wall_East_Maint", new Vector3(30.2f, h, 10f), new Vector3(WallThickness, WallHeight, 20f), _wall, ObstacleLayer, walls);
            Box("Wall_North_Maint", new Vector3(20f, h, 20.2f), new Vector3(20.8f, WallHeight, WallThickness), _wall, ObstacleLayer, walls);

            Box("Wall_West_Far", new Vector3(-12.2f, h, 50f), new Vector3(WallThickness, WallHeight, 44f), _wall, ObstacleLayer, walls);
            Box("Wall_East_Far", new Vector3(12.2f, h, 50f), new Vector3(WallThickness, WallHeight, 44f), _wall, ObstacleLayer, walls);
            Box("Wall_North_Far", new Vector3(0f, h, 72.2f), new Vector3(24.8f, WallHeight, WallThickness), _wall, ObstacleLayer, walls);

            // Uçurum kenarı çitleri: köprü ağzı (X[-2.2, 2.2]) açık kalır.
            FenceLine(walls, new Vector3(-12f, 0f, 19.6f), new Vector3(-2.2f, 0f, 19.6f));
            FenceLine(walls, new Vector3(2.2f, 0f, 19.6f), new Vector3(10f, 0f, 19.6f));
            FenceLine(walls, new Vector3(-12f, 0f, 28.4f), new Vector3(-2.2f, 0f, 28.4f));
            FenceLine(walls, new Vector3(2.2f, 0f, 28.4f), new Vector3(12f, 0f, 28.4f));
        }

        private static void FenceLine(Transform parent, Vector3 start, Vector3 end)
        {
            var line = Group($"Fence_{start.x:0}_{start.z:0}", parent).transform;
            var direction = end - start;
            float length = direction.magnitude;
            float yaw = Quaternion.LookRotation(direction).eulerAngles.y - 90f;

            var probe = PlaceModel("fence-straight", "probe", Vector3.zero, 0f, 1f, line);
            float segment = Mathf.Max(0.5f, WorldBounds(probe).size.x);
            Object.DestroyImmediate(probe);

            int count = Mathf.Max(1, Mathf.RoundToInt(length / segment));
            float step = length / count;
            for (int i = 0; i < count; i++)
            {
                var position = start + direction.normalized * (step * (i + 0.5f));
                PlaceModel("fence-straight", $"Fence_{i}", position, yaw, 1f, line, ObstacleLayer);
            }

            var center = (start + end) / 2f + Vector3.up * 0.6f;
            var collider = InvisibleBox("FenceCollider", center, new Vector3(length, 1.2f, 0.3f), ObstacleLayer, line);
            collider.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        }

        /// <summary>
        /// Bakım alanı: batı duvarında yalnızca Robi'nin sığdığı alçak tünel, iki rotalı labirent.
        /// Rota A: güney koridoru (piston kapıdan geçer, kısa). Rota B: kuzeybatı odası (uzun ama hep açık).
        /// </summary>
        private static void BuildMaintenanceArea(Transform parent)
        {
            var area = Group("MaintenanceArea", parent).transform;
            float h = WallHeight / 2f;

            // Batı duvarı ve tünel ağzı (Z[4,6]).
            Box("Wall_West_Maint_S", new Vector3(10f, h, 2f), new Vector3(WallThickness, WallHeight, 4f), _wall, ObstacleLayer, area);
            Box("Wall_West_Maint_N", new Vector3(10f, h, 13f), new Vector3(WallThickness, WallHeight, 14f), _wall, ObstacleLayer, area);
            float lintelHeight = WallHeight - TunnelClearance;
            Box("Tunnel_Lintel", new Vector3(10f, TunnelClearance + lintelHeight / 2f, 5f), new Vector3(WallThickness, lintelHeight, 2f), _hazard, ObstacleLayer, area);

            // Tünelin başlangıç alanına taşan kısa bölümü (oyuncuya "buraya sığmıyorum" hissi verir).
            Box("Tunnel_Roof", new Vector3(9.05f, TunnelClearance + 0.15f, 5f), new Vector3(1.5f, 0.3f, 2.4f), _hazard, ObstacleLayer, area);
            Box("Tunnel_Side_S", new Vector3(9.05f, TunnelClearance / 2f, 3.9f), new Vector3(1.5f, TunnelClearance, 0.2f), _metal, ObstacleLayer, area);
            Box("Tunnel_Side_N", new Vector3(9.05f, TunnelClearance / 2f, 6.1f), new Vector3(1.5f, TunnelClearance, 0.2f), _metal, ObstacleLayer, area);

            // İç duvarlar.
            Box("Maze_W1", new Vector3(19f, h, 10f), new Vector3(10f, WallHeight, WallThickness), _wall, ObstacleLayer, area);
            Box("Maze_W2", new Vector3(20f, h, 13.5f), new Vector3(WallThickness, WallHeight, 7f), _wall, ObstacleLayer, area);

            // Statik engeller (sensörün algıladığı sandıklar).
            PlaceModel("crate-strong", "Crate_A", new Vector3(15.5f, 0f, 7f), 15f, 1.1f, area, ObstacleLayer, true);
            PlaceModel("crate", "Crate_B", new Vector3(13f, 0f, 15.5f), 40f, 1f, area, ObstacleLayer, true);
            PlaceModel("crate", "Crate_C", new Vector3(25f, 0f, 12.5f), -10f, 1f, area, ObstacleLayer, true);
            PlaceModel("crate-strong", "Crate_D", new Vector3(27.5f, 0f, 4f), 5f, 1.1f, area, ObstacleLayer, true);
            PlaceModel("barrel", "Barrel_A", new Vector3(12f, 0f, 1.5f), 0f, 1f, area, ObstacleLayer, true);
        }

        private static DrawBridge BuildBridge(Transform parent)
        {
            var hinge = Group("DrawBridge", parent);
            hinge.transform.position = new Vector3(0f, 0f, 28f);
            var body = hinge.AddComponent<Rigidbody>();
            body.isKinematic = true;

            var plank = Box("Bridge_Deck", Vector3.zero, new Vector3(3.6f, 0.3f, 8.3f), _wood, GroundLayer, hinge.transform);
            plank.transform.localPosition = new Vector3(0f, -0.15f, -4.1f);
            for (int side = -1; side <= 1; side += 2)
            {
                var rail = Box(side < 0 ? "Rail_L" : "Rail_R", Vector3.zero, new Vector3(0.12f, 0.7f, 8.3f), _rope, ObstacleLayer, hinge.transform);
                rail.transform.localPosition = new Vector3(side * 1.74f, 0.35f, -4.1f);
            }

            var bridge = hinge.AddComponent<DrawBridge>();
            Set(bridge, "hinge", hinge.transform);
            hinge.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            return bridge;
        }

        private static PistonGate BuildPistonGate(Transform parent)
        {
            var gate = FitModelToBox("block-moving-large", "PistonGate", new Vector3(19f, 1.2f, 5f), new Vector3(0.8f, 2.4f, 9.8f),
                DynamicObstacleLayer, parent, true);
            var body = gate.AddComponent<Rigidbody>();
            body.isKinematic = true;

            var collider = gate.GetComponent<BoxCollider>();
            var obstacle = gate.AddComponent<NavMeshObstacle>();
            obstacle.shape = NavMeshObstacleShape.Box;
            obstacle.center = collider.center;
            obstacle.size = collider.size;
            obstacle.carving = true;
            obstacle.carveOnlyStationary = false;

            return gate.AddComponent<PistonGate>();
        }

        private static Lever BuildLever(Transform parent, DrawBridge bridge)
        {
            var leverGo = PlaceModel("lever", "BridgeLever", LeverPosition, 180f, 1.1f, parent, InteractableLayer, true);
            var interactionPoint = Group("InteractionPoint", leverGo.transform);
            interactionPoint.transform.position = LeverInteractionPoint;

            var lever = leverGo.AddComponent<Lever>();
            Set(lever, "handle", FindDeep(leverGo.transform, "handle"));
            Set(lever, "interactionPoint", interactionPoint.transform);
            Set(lever, "allowedActors", 1 << CompanionLayer);
            Set(lever, "targets", new Object[] { bridge });

            var marker = leverGo.AddComponent<CompanionTaskMarker>();
            Set(marker, "commandLabel", "Şalteri indir");

            var leverBase = Box("Lever_Base", LeverPosition + new Vector3(0f, 0.05f, 0f), new Vector3(1.6f, 0.1f, 1.6f), _hazard, 0, parent);
            leverBase.GetComponent<Collider>().enabled = false;
            leverBase.transform.SetParent(leverGo.transform, true);
            return lever;
        }

        private static RotatingDoor BuildGate(Transform environment, Transform gameplay)
        {
            var door = PlaceModel("door-rotate-large", "GuardedDoor", new Vector3(0f, 0f, 60f), 0f, 3f, gameplay, ObstacleLayer);
            var bounds = WorldBounds(door);
            float halfGap = Mathf.Clamp(bounds.extents.x, 1f, 3f);

            var leaf = FindDeep(door.transform, "door");
            var leafMesh = leaf != null ? leaf.GetComponentInChildren<MeshFilter>() : door.GetComponentInChildren<MeshFilter>();
            if (leafMesh != null) leafMesh.gameObject.AddComponent<BoxCollider>();

            var rotating = door.AddComponent<RotatingDoor>();
            Set(rotating, "doorLeaf", leaf != null ? leaf : door.transform);

            var walls = Group("GateWall", environment).transform;
            const float gateHeight = 3f;
            float leftLength = 12f - halfGap;
            Box("GateWall_L", new Vector3(-12f + leftLength / 2f, gateHeight / 2f, 60f), new Vector3(leftLength, gateHeight, WallThickness), _wall, ObstacleLayer, walls);
            Box("GateWall_R", new Vector3(12f - leftLength / 2f, gateHeight / 2f, 60f), new Vector3(leftLength, gateHeight, WallThickness), _wall, ObstacleLayer, walls);
            return rotating;
        }

        /// <summary>
        /// Ara bölüm: köprüden sonra bekçi avlusunu kapatan geçit. Oyuncu karşı alana dağılmış
        /// altınları toplayınca geçit zemine gömülür ve yol (NavMesh dahil) açılır.
        /// </summary>
        private static CollectQuest BuildMiddleArea(Transform environment, Transform gameplay, out SlidingGate gate, out Collectible[] coins)
        {
            var area = Group("MiddleArea", gameplay).transform;
            var walls = Group("QuestWall", environment).transform;
            float h = WallHeight / 2f;
            float sideLength = 12f - QuestGateHalfWidth;

            Box("QuestWall_L", new Vector3(-12f + sideLength / 2f, h, QuestGateZ), new Vector3(sideLength, WallHeight, WallThickness), _wall, ObstacleLayer, walls);
            Box("QuestWall_R", new Vector3(12f - sideLength / 2f, h, QuestGateZ), new Vector3(sideLength, WallHeight, WallThickness), _wall, ObstacleLayer, walls);

            var gateGo = FitModelToBox("block-moving-blue", "QuestGate", new Vector3(0f, 1.2f, QuestGateZ),
                new Vector3(QuestGateHalfWidth * 2f, 2.4f, 0.6f), ObstacleLayer, area, true);
            gate = gateGo.AddComponent<SlidingGate>();

            PlaceModel("sign", "QuestSign", new Vector3(2.8f, 0f, QuestGateZ - 0.8f), 180f, 1.3f, area);

            var questGo = Group("GoldQuest", area);
            var quest = questGo.AddComponent<CollectQuest>();

            coins = new Collectible[CoinPositions.Length];
            for (int i = 0; i < CoinPositions.Length; i++) coins[i] = BuildCoin(area, i, CoinPositions[i]);

            Set(quest, "required", CoinPositions.Length);
            Set(quest, "targets", new Object[] { gate });
            Set(quest, "collectibles", coins);
            return quest;
        }

        private static Collectible BuildCoin(Transform parent, int index, Vector3 position)
        {
            var root = Group($"Coin_{index + 1}", parent);
            root.layer = 2;
            root.transform.position = position;

            var trigger = root.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, 0.9f, 0f);
            trigger.radius = 0.8f;

            var visual = PlaceModel("coin-gold", "Visual", position + Vector3.up * 0.5f, 0f, 0.8f, root.transform);
            var coin = root.AddComponent<Collectible>();
            Set(coin, "visual", visual.transform);
            return coin;
        }

        private static void BuildDecoration(Transform parent)
        {
            var decor = Group("Decoration", parent).transform;
            void Prop(string model, float x, float z, float yaw, float height) =>
                PlaceModel(model, model, new Vector3(x, 0f, z), yaw, height, decor);

            Prop("tree", -10.5f, 1.5f, 0f, 3.5f);
            Prop("tree-pine", -10.8f, 12f, 30f, 4f);
            Prop("tree", 3.5f, 1.2f, 90f, 3f);
            Prop("rocks", -7f, 18.4f, 0f, 0.6f);
            Prop("flowers", -6f, 9f, 0f, 0.4f);
            Prop("flowers-tall", 2f, 7f, 45f, 0.6f);
            Prop("grass", -2f, 14f, 0f, 0.4f);
            Prop("sign", 2.6f, 18.5f, 180f, 1.3f);

            Prop("tree-pine", -10.5f, 32f, 0f, 4.2f);
            Prop("tree", 10.5f, 35f, 0f, 3.6f);
            Prop("tree", -10.5f, 50f, 60f, 3.4f);
            Prop("tree-pine", 10.5f, 52f, 0f, 4f);
            Prop("tree-pine", -10f, 69f, 0f, 4f);
            Prop("tree", 10f, 70f, 0f, 3.5f);
            Prop("crate", 8.5f, 44f, 20f, 1f);
            Prop("crate", 9.3f, 45.3f, 0f, 1f);
            Prop("barrel", -8.5f, 44f, 0f, 1f);
            Prop("stones", -5f, 40f, 0f, 0.4f);
            Prop("flowers", 4f, 38f, 0f, 0.4f);
            Prop("sign", 3f, 30.5f, 180f, 1.3f);
            Prop("flag", 0f, 68f, 0f, 2.5f);
        }

        private static GameObject BuildPlayer(out KeyboardMouseInput input)
        {
            var player = new GameObject("Player") { layer = PlayerLayer };
            var controller = player.AddComponent<CharacterController>();
            controller.height = PlayerHeight;
            controller.radius = PlayerRadius;
            controller.center = new Vector3(0f, PlayerHeight / 2f + 0.05f, 0f);
            controller.stepOffset = 0.3f;
            controller.slopeLimit = 45f;

            var model = PlaceModel("character-oobi", "Model", Vector3.zero, 0f, 1.7f, player.transform, PlayerLayer);
            SetupAnimator(model);

            input = player.AddComponent<KeyboardMouseInput>();
            player.AddComponent<PlayerMotor>();
            player.AddComponent<PlayerCommander>();
            var interactor = player.AddComponent<PlayerInteractor>();
            Set(interactor, "interactableMask", (1 << InteractableLayer) | (1 << NpcLayer));
            player.AddComponent<LocomotionAnimator>();

            player.transform.position = PlayerSpawn;
            return player;
        }

        private static CompanionController BuildCompanion(Transform player)
        {
            var root = new GameObject("Companion_Robi") { layer = CompanionLayer };
            var model = PlaceModel("character-oodi", "Model", Vector3.zero, 0f, 0.85f, root.transform, CompanionLayer);
            SetupAnimator(model);

            var agent = root.AddComponent<NavMeshAgent>();
            agent.agentTypeID = 0;
            agent.radius = 0.3f;
            agent.height = 0.9f;
            agent.speed = 4.8f;
            agent.angularSpeed = 540f;
            agent.acceleration = 20f;
            agent.stoppingDistance = 0.15f;
            agent.autoBraking = true;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;

            root.AddComponent<NavMeshMovement>();
            var sensor = root.AddComponent<RaycastSensor>();
            Set(sensor, "obstacleMask", (1 << ObstacleLayer) | (1 << DynamicObstacleLayer) | (1 << InteractableLayer) | (1 << GroundLayer));
            Set(sensor, "dynamicObstacleMask", 1 << DynamicObstacleLayer);
            root.AddComponent<LocomotionAnimator>();

            var companion = root.AddComponent<CompanionController>();
            Set(companion, "followTarget", player);

            root.transform.position = CompanionSpawn;
            return companion;
        }

        private static void SetupAnimator(GameObject model)
        {
            var animator = model.GetComponent<Animator>();
            if (animator == null) animator = model.AddComponent<Animator>();
            animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<AnimatorController>(CharacterAnimatorBuilder.ControllerPath);
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
        }

        private static Camera BuildCamera(Transform player, KeyboardMouseInput input)
        {
            var cameraGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = cameraGo.AddComponent<Camera>();
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 300f;
            camera.fieldOfView = 55f;
            cameraGo.AddComponent<AudioListener>();

            var follow = cameraGo.AddComponent<ThirdPersonCamera>();
            Set(follow, "target", player);
            Set(follow, "inputBehaviour", input);
            Set(follow, "collisionMask", (1 << GroundLayer) | (1 << ObstacleLayer));

            cameraGo.transform.SetPositionAndRotation(PlayerSpawn + new Vector3(0f, 4.5f, -6f), Quaternion.Euler(25f, 0f, 0f));
            return camera;
        }

        private static void BuildTriggers(Transform parent)
        {
            var triggers = Group("Triggers", parent).transform;

            var startRespawn = Group("Respawn_Start", triggers).transform;
            startRespawn.position = PlayerSpawn;
            var farRespawn = Group("Respawn_Far", triggers).transform;
            farRespawn.position = new Vector3(0f, 0f, 31f);

            var killZone = InvisibleBox("KillZone", new Vector3(5f, -12f, 36f), new Vector3(200f, 2f, 200f), 2, triggers, true)
                .AddComponent<KillZone>();
            Set(killZone, "respawnPoint", startRespawn);

            var checkpoint = InvisibleBox("Checkpoint_Far", new Vector3(0f, 1.5f, 31f), new Vector3(8f, 3f, 4f), 2, triggers, true)
                .AddComponent<CheckpointTrigger>();
            Set(checkpoint, "respawnPoint", farRespawn);
            Set(checkpoint, "killZones", new Object[] { killZone });

            var hint = InvisibleBox("HintZone_Chasm", new Vector3(-1f, 1.5f, 15.5f), new Vector3(22f, 3f, 9f), 2, triggers, true)
                .AddComponent<HintZone>();
            Set(hint, "hint", "Köprü kalkık! Şalter, yalnızca Robi'nin sığabildiği tünelin ardında.\n[F] Robi'ye şalteri indirme emri ver   ·   [G] Takip et / Bekle");

            InvisibleBox("Goal", new Vector3(0f, 1.5f, 67f), new Vector3(10f, 3f, 8f), 2, triggers, true).AddComponent<GoalTrigger>();
        }

        /// <summary>Kilitli kapıyı koruyan LLM bekçisi (Ollama).</summary>
        private static void BuildGuard(Transform parent, RotatingDoor door)
        {
            var npc = new GameObject("NPC_Guard") { layer = NpcLayer };
            npc.transform.SetParent(parent, false);
            var model = PlaceModel("character-oozi", "Model", Vector3.zero, 0f, 1.9f, npc.transform, NpcLayer);
            SetupAnimator(model);
            var capsule = npc.AddComponent<CapsuleCollider>();
            capsule.center = new Vector3(0f, 0.95f, 0f);
            capsule.height = 1.9f;
            capsule.radius = 0.4f;
            npc.transform.SetPositionAndRotation(new Vector3(2.8f, 0f, 58.3f), Quaternion.Euler(0f, 180f, 0f));

            var guard = npc.AddComponent<GuardNpc>();
            Set(guard, "door", door);
        }

        private static void BuildUi()
        {
            var canvasGo = new GameObject("HUD");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            var objective = CreateText(canvasGo.transform, "ObjectiveText", new Vector2(0f, 1f), new Vector2(30f, -30f), new Vector2(900f, 60f), 30, TextAnchor.UpperLeft, FontStyle.Bold);
            var state = CreateText(canvasGo.transform, "CompanionStateText", new Vector2(1f, 1f), new Vector2(-30f, -30f), new Vector2(600f, 50f), 26, TextAnchor.UpperRight, FontStyle.Bold);
            var hint = CreateText(canvasGo.transform, "HintText", new Vector2(0.5f, 0f), new Vector2(0f, 120f), new Vector2(1400f, 100f), 28, TextAnchor.LowerCenter, FontStyle.Bold);
            var message = CreateText(canvasGo.transform, "MessageText", new Vector2(0.5f, 1f), new Vector2(0f, -140f), new Vector2(1400f, 60f), 30, TextAnchor.MiddleCenter, FontStyle.Bold);
            var help = CreateText(canvasGo.transform, "ControlsText", new Vector2(0f, 0f), new Vector2(30f, 25f), new Vector2(1500f, 40f), 20, TextAnchor.LowerLeft, FontStyle.Normal);
            help.text = "WASD: Hareket   Shift: Koş   Fare: Kamera   F: Robi'ye görev ver   G: Takip/Bekle   E: Etkileşim   F9: Yoldaş testleri   Esc: Duraklat";
            help.color = new Color(1f, 1f, 1f, 0.8f);

            var hud = canvasGo.AddComponent<HudView>();
            Set(hud, "objectiveText", objective);
            Set(hud, "companionStateText", state);
            Set(hud, "hintText", hint);
            Set(hud, "messageText", message);

            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>();
        }

        private static void BuildMenus(KeyboardMouseInput input)
        {
            var canvasGo = new GameObject("Menus");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            var start = CreatePanel(canvasGo.transform, "StartPanel", new Color(0.05f, 0.07f, 0.1f, 0.92f),
                "YazLab Macera", "Robi ile birlikte köprüyü indir, altınları topla ve bekçiyi geçip hedefe ulaş.");
            var startButton = CreateButton(start.transform, "StartButton", "Başla", 0);
            var startQuit = CreateButton(start.transform, "QuitButton", "Çıkış", 1);

            var pause = CreatePanel(canvasGo.transform, "PausePanel", new Color(0f, 0f, 0f, 0.65f), "Duraklatıldı", "Devam etmek için Esc'ye bas.");
            var resume = CreateButton(pause.transform, "ResumeButton", "Devam Et", 0);
            var pauseRestart = CreateButton(pause.transform, "RestartButton", "Yeniden Başla", 1);
            var pauseMenu = CreateButton(pause.transform, "MenuButton", "Ana Menü", 2);

            var end = CreatePanel(canvasGo.transform, "EndPanel", new Color(0.05f, 0.07f, 0.1f, 0.85f), "Tebrikler!", string.Empty);
            var summary = end.transform.Find("Subtitle").GetComponent<Text>();
            var endRestart = CreateButton(end.transform, "RestartButton", "Tekrar Oyna", 0);
            var endMenu = CreateButton(end.transform, "MenuButton", "Ana Menü", 1);

            var flow = canvasGo.AddComponent<GameFlowController>();
            Set(flow, "inputBehaviour", input);
            Set(flow, "startPanel", start);
            Set(flow, "pausePanel", pause);
            Set(flow, "endPanel", end);
            Set(flow, "endSummaryText", summary);
            Set(flow, "startButton", startButton);
            Set(flow, "startQuitButton", startQuit);
            Set(flow, "resumeButton", resume);
            Set(flow, "pauseRestartButton", pauseRestart);
            Set(flow, "pauseMenuButton", pauseMenu);
            Set(flow, "endRestartButton", endRestart);
            Set(flow, "endMenuButton", endMenu);

            pause.SetActive(false);
            end.SetActive(false);
        }

        /// <summary>Ekranı kaplayan yarı saydam panel; başlık ve alt başlık ortada üstte.</summary>
        private static GameObject CreatePanel(Transform parent, string name, Color background, string title, string subtitle)
        {
            var panel = new GameObject(name, typeof(RectTransform));
            panel.transform.SetParent(parent, false);
            var rect = (RectTransform)panel.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            panel.AddComponent<Image>().color = background;

            var titleText = CreateText(panel.transform, "Title", new Vector2(0.5f, 0.5f), new Vector2(0f, 230f), new Vector2(1400f, 120f), 84, TextAnchor.MiddleCenter, FontStyle.Bold);
            titleText.text = title;
            var subtitleText = CreateText(panel.transform, "Subtitle", new Vector2(0.5f, 0.5f), new Vector2(0f, 130f), new Vector2(1400f, 60f), 30, TextAnchor.MiddleCenter, FontStyle.Normal);
            subtitleText.text = subtitle;
            return panel;
        }

        private static Button CreateButton(Transform parent, string name, string label, int index)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, 10f - index * 100f);
            rect.sizeDelta = new Vector2(420f, 80f);

            go.AddComponent<Image>();
            var button = go.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = new Color(0.16f, 0.45f, 0.75f);
            colors.highlightedColor = colors.selectedColor = new Color(0.26f, 0.58f, 0.9f);
            colors.pressedColor = new Color(0.1f, 0.3f, 0.55f);
            button.colors = colors;

            var text = CreateText(go.transform, "Label", new Vector2(0.5f, 0.5f), Vector2.zero, rect.sizeDelta, 34, TextAnchor.MiddleCenter, FontStyle.Bold);
            text.text = label;
            return button;
        }

        private static Text CreateText(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size, int fontSize,
            TextAnchor alignment, FontStyle style)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            var text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            text.color = Color.white;

            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            outline.effectDistance = new Vector2(2f, -2f);
            return text;
        }

        private static NavMeshSurface BuildSystems()
        {
            var systems = new GameObject("GameSystems");
            var surface = systems.AddComponent<NavMeshSurface>();
            surface.agentTypeID = 0;
            surface.collectObjects = CollectObjects.All;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.layerMask = (1 << GroundLayer) | (1 << ObstacleLayer);
            systems.AddComponent<NavMeshRebuilder>();
            systems.AddComponent<ObjectiveTracker>();
            return surface;
        }

        private static void BuildTestRunner(CompanionController companion, Lever lever, DrawBridge bridge, PistonGate piston)
        {
            var runnerGo = new GameObject("CompanionTestRunner");
            var spawns = new List<Object>();
            for (int i = 0; i < TestSpawnPoints.Length; i++)
            {
                var spawn = Group($"TestSpawn_{i + 1}", runnerGo.transform).transform;
                spawn.position = TestSpawnPoints[i];
                spawn.rotation = Quaternion.Euler(0f, i * 72f, 0f);
                spawns.Add(spawn);
            }

            var runner = runnerGo.AddComponent<CompanionTestRunner>();
            Set(runner, "companionBehaviour", companion);
            Set(runner, "task", lever.GetComponent<CompanionTaskMarker>());
            Set(runner, "resettables", new Object[] { lever, bridge, piston });
            Set(runner, "spawnPoints", spawns.ToArray());
        }

        private static void BuildQuestTestRunner(CollectQuest quest, SlidingGate gate, Collectible[] coins, PlayerMotor player,
            CompanionController companion)
        {
            var runnerGo = new GameObject("QuestFlowTestRunner");
            var before = Group("BeforeGate", runnerGo.transform).transform;
            before.position = new Vector3(0f, 0f, QuestGateZ - 6f);
            var courtyard = Group("Courtyard", runnerGo.transform).transform;
            courtyard.position = new Vector3(0f, 0f, QuestGateZ + 6f);

            var runner = runnerGo.AddComponent<QuestFlowTestRunner>();
            Set(runner, "quest", quest);
            Set(runner, "gate", gate);
            Set(runner, "coins", coins);
            Set(runner, "playerBehaviour", player);
            Set(runner, "companionBehaviour", companion);
            Set(runner, "beforeGatePoint", before);
            Set(runner, "courtyardPoint", courtyard);
        }

        private static void BakeNavMesh(NavMeshSurface surface)
        {
            Physics.SyncTransforms();
            surface.BuildNavMesh();

            AssetDatabase.DeleteAsset(NavMeshAssetPath);
            AssetDatabase.CreateAsset(surface.navMeshData, NavMeshAssetPath);
            Set(surface, "m_NavMeshData", surface.navMeshData);
        }

        /// <summary>Tasarım varsayımlarını doğrular ve konsola yazar (batch modda da çalışır).</summary>
        private static void ValidateLayout(NavMeshSurface surface)
        {
            surface.RemoveData();
            surface.AddData();

            var path = new NavMeshPath();
            foreach (var spawn in TestSpawnPoints)
            {
                NavMesh.SamplePosition(spawn, out var from, 2f, NavMesh.AllAreas);
                NavMesh.SamplePosition(LeverInteractionPoint, out var to, 2f, NavMesh.AllAreas);
                NavMesh.CalculatePath(from.position, to.position, NavMesh.AllAreas, path);
                Debug.Log($"[YazLab][Doğrulama] {spawn} → şalter: {path.status}, uzunluk {PathLength(path):0.0} m");
            }

            NavMesh.SamplePosition(PlayerSpawn, out var start, 2f, NavMesh.AllAreas);
            NavMesh.SamplePosition(new Vector3(0f, 0f, 40f), out var far, 2f, NavMesh.AllAreas);
            NavMesh.CalculatePath(start.position, far.position, NavMesh.AllAreas, path);
            Debug.Log($"[YazLab][Doğrulama] Köprü kalkıkken karşı alana yol: {path.status} (Partial/Invalid olmalı)");

            NavMesh.SamplePosition(new Vector3(0f, 0f, QuestGateZ + 5f), out var courtyard, 2f, NavMesh.AllAreas);
            NavMesh.CalculatePath(far.position, courtyard.position, NavMesh.AllAreas, path);
            Debug.Log($"[YazLab][Doğrulama] Geçit kapalıyken bekçi avlusuna yol: {path.status} (Partial/Invalid olmalı)");

            // Oyuncu kapsülü tünele sığmamalı.
            var tunnel = new Vector3(10f, 0f, 5f);
            bool blocked = Physics.CheckCapsule(tunnel + Vector3.up * (PlayerRadius + 0.05f),
                tunnel + Vector3.up * (PlayerHeight - PlayerRadius + 0.05f), PlayerRadius, 1 << ObstacleLayer);
            Debug.Log($"[YazLab][Doğrulama] Oyuncu tünelden geçemez: {blocked}");
        }

        private static float PathLength(NavMeshPath path)
        {
            float length = 0f;
            for (int i = 1; i < path.corners.Length; i++) length += Vector3.Distance(path.corners[i - 1], path.corners[i]);
            return length;
        }

        // --- Ekran görüntüleri (rapor ve kurulum doğrulaması için) ---

        [MenuItem("YazLab/Ekran Görüntülerini Al")]
        public static void CaptureFromMenu() => CaptureScreenshots(Path.GetFullPath("Screenshots"));

        public static void CaptureScreenshots(string directory)
        {
            Directory.CreateDirectory(directory);

            // İlk render shader'ları ısıtır ve bozuk çıkabilir; atılır.
            Shot(directory, "_warmup", new Vector3(9f, 42f, -14f), new Vector3(9f, 0f, 30f));
            File.Delete(Path.Combine(directory, "_warmup.png"));

            Shot(directory, "01_overview", new Vector3(9f, 42f, -14f), new Vector3(9f, 0f, 30f));
            Shot(directory, "02_maintenance_topdown", new Vector3(19f, 30f, 9.9f), new Vector3(19f, 0f, 10f));
            Shot(directory, "03_player_view", PlayerSpawn + new Vector3(-1f, 3.5f, -6f), new Vector3(3f, 1f, 16f));
            Shot(directory, "04_gate_npc", new Vector3(-3f, 4f, 50f), new Vector3(1f, 1.5f, 60f));
            Shot(directory, "05_tunnel", new Vector3(4f, 2.5f, 2f), new Vector3(10f, 0.8f, 5f));
            Shot(directory, "06_middle_area", new Vector3(0f, 9f, 29f), new Vector3(0f, 0f, 43f));
        }

        private static void Shot(string directory, string name, Vector3 position, Vector3 lookAt)
        {
            var go = new GameObject("ScreenshotCamera");
            var camera = go.AddComponent<Camera>();
            camera.fieldOfView = 55f;
            camera.farClipPlane = 300f;
            go.transform.SetPositionAndRotation(position, Quaternion.LookRotation(lookAt - position));

            var rt = new RenderTexture(1600, 900, 24);
            camera.targetTexture = rt;
            camera.Render();

            RenderTexture.active = rt;
            var texture = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            texture.Apply();
            RenderTexture.active = null;

            File.WriteAllBytes(Path.Combine(directory, name + ".png"), texture.EncodeToPNG());
            camera.targetTexture = null;
            Object.DestroyImmediate(texture);
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(go);
        }
    }
}
