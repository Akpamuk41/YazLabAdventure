using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using static YazLab.EditorTools.EditorBuildUtils;

namespace YazLab.EditorTools
{
    /// <summary>
    /// Görsel kalite ayarları: post-processing (ton eşleme, renk, bloom, vinyet), gökyüzü + sis,
    /// kenar yumuşatma, ortam gölgesi (SSAO), net palet dokuları ve zemine çim dokusu.
    /// </summary>
    public static class VisualSetup
    {
        private const string SettingsFolder = "Assets/_Project/Settings";
        private const string ProfilePath = SettingsFolder + "/MainVolumeProfile.asset";
        private const string PipelinePath = SettingsFolder + "/PC_RPAsset.asset";
        private const string RendererPath = SettingsFolder + "/PC_Renderer.asset";
        private const string TexturesFolder = "Assets/_Project/Textures";
        private const string GrassTexturePath = TexturesFolder + "/T_Grass.png";
        private const string SkyboxPath = "Assets/_Project/Materials/M_Sky.mat";

        private static readonly string[] PaletteTextures =
        {
            "Assets/ThirdParty/Kenney_PlatformerKit/Models/Textures/colormap.png",
            "Assets/ThirdParty/Kenney_CastleKit/Models/Textures/colormap.png",
            "Assets/ThirdParty/KayKit_Adventurers/Models/knight_texture.png"
        };

        private static readonly Color HorizonColor = new(0.74f, 0.83f, 0.93f);

        /// <summary>
        /// Asset tarafı (doku içe aktarma, pipeline, profil). Sahne kurulmadan önce çağrılır: yarı kurulmuş sahnede
        /// yeniden içe aktarma olursa NavMesh boş çıkıyor.
        /// </summary>
        public static void PrepareAssets()
        {
            SharpenPaletteTextures();
            ConfigurePipeline();
            GrassTexture();
            SkyMaterial();
            VolumeProfileAsset();
            AssetDatabase.SaveAssets();
        }

        /// <summary>Sahne tarafı: gökyüzü, sis, global volume, kamera ve çim katmanı.</summary>
        public static void Apply(Camera camera, Transform environment, Light sun)
        {
            ApplySky(sun);
            var volume = new GameObject("Global Volume").AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
            SetupCamera(camera);
            AddGrass(environment);
        }

        /// <summary>Kamera bileşenine post-processing ve kenar yumuşatma açar (ekran görüntüsü kamerası da kullanır).</summary>
        public static void SetupCamera(Camera camera)
        {
            var data = camera.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            data.antialiasingQuality = AntialiasingQuality.High;
            camera.allowMSAA = true;
        }

        /// <summary>
        /// Kenney/KayKit dokuları küçük renk paletleri: sıkıştırma ve yumuşak filtre renkleri karıştırıp soldurur.
        /// </summary>
        private static void SharpenPaletteTextures()
        {
            foreach (var path in PaletteTextures)
            {
                if (!(AssetImporter.GetAtPath(path) is TextureImporter importer)) continue;
                if (importer.filterMode == FilterMode.Point && !importer.mipmapEnabled &&
                    importer.textureCompression == TextureImporterCompression.Uncompressed) continue;

                importer.filterMode = FilterMode.Point;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
        }

        private static void ConfigurePipeline()
        {
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (pipeline != null)
            {
                pipeline.msaaSampleCount = 4;
                pipeline.supportsHDR = true;
                pipeline.shadowDistance = 70f;
                EditorUtility.SetDirty(pipeline);
            }

            // SSAO: köşelerde ve nesnelerin dibinde hafif gölge, sahneye derinlik verir.
            var rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if (rendererData == null) return;
            foreach (var feature in rendererData.rendererFeatures)
            {
                if (feature == null || feature.GetType().Name != "ScreenSpaceAmbientOcclusion") continue;
                var so = new SerializedObject(feature);
                so.FindProperty("m_Settings.Intensity").floatValue = 1.1f;
                so.FindProperty("m_Settings.Radius").floatValue = 0.45f;
                so.FindProperty("m_Settings.DirectLightingStrength").floatValue = 0.3f;
                so.ApplyModifiedPropertiesWithoutUndo();
                feature.SetActive(true);
            }

            EditorUtility.SetDirty(rendererData);
        }

        private static Material SkyMaterial()
        {
            var sky = AssetDatabase.LoadAssetAtPath<Material>(SkyboxPath);
            if (sky == null)
            {
                sky = new Material(Shader.Find("Skybox/Procedural"));
                AssetDatabase.CreateAsset(sky, SkyboxPath);
            }

            sky.SetFloat("_SunSize", 0.035f);
            sky.SetFloat("_AtmosphereThickness", 0.85f);
            sky.SetColor("_SkyTint", new Color(0.45f, 0.6f, 0.85f));
            sky.SetColor("_GroundColor", new Color(0.42f, 0.47f, 0.4f));
            sky.SetFloat("_Exposure", 1.25f);
            EditorUtility.SetDirty(sky);
            return sky;
        }

        private static void ApplySky(Light sun)
        {
            RenderSettings.skybox = AssetDatabase.LoadAssetAtPath<Material>(SkyboxPath);
            RenderSettings.sun = sun;

            // Uzaktaki duvarlar hafif pusa girsin: düz bir zemin yerine derinlik hissi.
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = HorizonColor;
            RenderSettings.fogStartDistance = 35f;
            RenderSettings.fogEndDistance = 150f;
        }

        private static void VolumeProfileAsset()
        {
            AssetDatabase.DeleteAsset(ProfilePath);
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, ProfilePath);

            T Add<T>() where T : VolumeComponent
            {
                var component = profile.Add<T>(true);
                component.name = typeof(T).Name;
                AssetDatabase.AddObjectToAsset(component, profile);
                return component;
            }

            Add<Tonemapping>().mode.value = TonemappingMode.ACES;

            var color = Add<ColorAdjustments>();
            color.postExposure.value = 0.35f;
            color.contrast.value = 12f;
            color.saturation.value = 12f;

            var white = Add<WhiteBalance>();
            white.temperature.value = 6f;

            var bloom = Add<Bloom>();
            bloom.threshold.value = 1f;
            bloom.intensity.value = 0.35f;
            bloom.scatter.value = 0.6f;

            var vignette = Add<Vignette>();
            vignette.intensity.value = 0.22f;
            vignette.smoothness.value = 0.45f;

            EditorUtility.SetDirty(profile);
        }

        /// <summary>Düz yeşil zeminin üstüne ince, tekrarlı bir çim dokusu serer (çarpışma yok, NavMesh değişmez).</summary>
        private static void AddGrass(Transform environment)
        {
            var grass = GrassTexture();
            foreach (var t in environment.GetComponentsInChildren<Transform>())
            {
                if (!t.name.StartsWith("Ground_")) continue;

                var renderers = t.GetComponentsInChildren<Renderer>();
                if (renderers.Length == 0) continue;
                var bounds = renderers[0].bounds;
                foreach (var r in renderers) bounds.Encapsulate(r.bounds);

                var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                quad.name = t.name + "_Grass";
                Object.DestroyImmediate(quad.GetComponent<Collider>());
                quad.transform.SetParent(t.parent, false);
                quad.transform.SetPositionAndRotation(new Vector3(bounds.center.x, bounds.max.y + 0.01f, bounds.center.z),
                    Quaternion.Euler(90f, 0f, 0f));
                quad.transform.localScale = new Vector3(bounds.size.x, bounds.size.z, 1f);

                // Her blok kendi tekrar sayısıyla: doku yaklaşık 4 m'de bir tekrar eder.
                var material = GetMaterial("M_" + quad.name, Color.white, 0.05f);
                material.SetTexture("_BaseMap", grass);
                material.SetTextureScale("_BaseMap", new Vector2(bounds.size.x / 4f, bounds.size.z / 4f));
                EditorUtility.SetDirty(material);

                var renderer = quad.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
            }
        }

        /// <summary>Perlin gürültüsüyle iki tonlu çim dokusu üretir (dış kaynak gerekmez).</summary>
        private static Texture2D GrassTexture()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(GrassTexturePath);
            if (existing != null) return existing;

            const int size = 256;
            var dark = new Color(0.24f, 0.43f, 0.17f);
            var light = new Color(0.4f, 0.6f, 0.25f);
            var texture = new Texture2D(size, size, TextureFormat.RGB24, false);
            var random = new System.Random(7);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float n = Tileable(x, y, size, 4f) * 0.5f + Tileable(x, y, size, 10f) * 0.3f + Tileable(x, y, size, 32f) * 0.2f;
                    float speck = (float)random.NextDouble() * 0.16f - 0.08f;
                    texture.SetPixel(x, y, Color.Lerp(dark, light, Mathf.Clamp01((n - 0.5f) * 1.6f + 0.5f + speck)));
                }
            }

            Directory.CreateDirectory(TexturesFolder);
            File.WriteAllBytes(GrassTexturePath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(GrassTexturePath);

            var importer = (TextureImporter)AssetImporter.GetAtPath(GrassTexturePath);
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Trilinear;
            importer.anisoLevel = 8;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(GrassTexturePath);
        }

        /// <summary>Dikişsiz tekrar eden Perlin: dört kaydırılmış örnek kenarlara göre harmanlanır.</summary>
        private static float Tileable(int x, int y, int size, float cells)
        {
            float u = x / (float)size, v = y / (float)size;
            float Sample(float a, float b) => Mathf.PerlinNoise(a * cells + 13.7f, b * cells + 41.3f);
            return Sample(u, v) * (1 - u) * (1 - v) + Sample(u - 1, v) * u * (1 - v)
                 + Sample(u, v - 1) * (1 - u) * v + Sample(u - 1, v - 1) * u * v;
        }
    }
}
