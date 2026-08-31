using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class VerticalSlicePresentationBuilder
{
    private const string MaterialFolder = "Assets/Materials/Presentation";
    private const string EnvironmentRootName = "Presentation Environment";

    private static readonly string[] StageScenePaths =
    {
        "Assets/Scenes/VerticalSlice_Stage01.unity",
        "Assets/Scenes/VerticalSlice_Stage02.unity",
        "Assets/Scenes/VerticalSlice_Stage03.unity",
        "Assets/Scenes/VerticalSlice_Stage04.unity",
        "Assets/Scenes/VerticalSlice_Stage05.unity"
    };

    [MenuItem("DemolitionLab/Presentation/Apply Industrial Environment")]
    public static void ApplyIndustrialEnvironment()
    {
        foreach (string scenePath in StageScenePaths)
        {
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            ApplyEnvironmentToOpenScene();
            EditorSceneManager.SaveOpenScenes();
        }

        AssetDatabase.SaveAssets();
        Debug.Log("Applied the industrial demolition environment to Stage01-05.");
    }

    [MenuItem("DemolitionLab/Presentation/Validate Industrial Environment")]
    public static void ValidateIndustrialEnvironment()
    {
        foreach (string scenePath in StageScenePaths)
        {
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            ValidateEnvironmentInOpenScene(scenePath);
        }

        Debug.Log("Industrial demolition environment validation passed for Stage01-05.");
    }

    internal static void ApplyEnvironmentToOpenScene()
    {
        PresentationMaterials materials = CreateEnvironmentMaterials();
        RemoveExistingRoot(EnvironmentRootName);

        GameObject root = new(EnvironmentRootName);
        AddEnvironmentGeometry(root.transform, materials);
        ConfigureGround(materials.YardConcrete);
        ConfigureLighting(materials.Skybox);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    private static void AddEnvironmentGeometry(
        Transform root,
        PresentationMaterials materials)
    {
        AddVisualCube(root, "Rear Perimeter Wall", new Vector3(0f, 1.15f, 13.5f), new Vector3(30f, 2.3f, 0.55f), materials.BarrierSteel);
        AddVisualCube(root, "Left Perimeter Barrier", new Vector3(-15f, 0.5f, 3f), new Vector3(0.55f, 1f, 21f), materials.BarrierSteel);
        AddVisualCube(root, "Right Perimeter Barrier", new Vector3(15f, 0.5f, 3f), new Vector3(0.55f, 1f, 21f), materials.BarrierSteel);

        AddVisualCube(root, "Warehouse West", new Vector3(-10.5f, 3.1f, 18f), new Vector3(8f, 6.2f, 6f), materials.Warehouse);
        AddVisualCube(root, "Warehouse East", new Vector3(8.5f, 2.6f, 19.5f), new Vector3(7f, 5.2f, 7f), materials.Warehouse);
        AddVisualCube(root, "Warehouse Roof West", new Vector3(-10.5f, 6.35f, 18f), new Vector3(8.6f, 0.3f, 6.6f), materials.BarrierSteel);
        AddVisualCube(root, "Warehouse Roof East", new Vector3(8.5f, 5.35f, 19.5f), new Vector3(7.6f, 0.3f, 7.6f), materials.BarrierSteel);

        AddVisualCube(root, "Container West", new Vector3(-11.8f, 1.25f, 9f), new Vector3(2.6f, 2.5f, 5.4f), materials.ContainerRust);
        AddVisualCube(root, "Container West Inner", new Vector3(-8.8f, 1.25f, 10.8f), new Vector3(2.6f, 2.5f, 5.4f), materials.ContainerBlue);
        AddVisualCube(root, "Container West Upper", new Vector3(-11.8f, 3.8f, 9f), new Vector3(2.6f, 2.5f, 5.4f), materials.ContainerRust);

        AddVisualCube(root, "Left Lane Marking", new Vector3(-5.2f, 0.015f, 1f), new Vector3(0.12f, 0.03f, 18f), materials.LinePaint);
        AddVisualCube(root, "Right Lane Marking", new Vector3(5.2f, 0.015f, 1f), new Vector3(0.12f, 0.03f, 18f), materials.LinePaint);
        AddVisualCube(root, "Firing Line", new Vector3(0f, 0.02f, -5.8f), new Vector3(10.5f, 0.04f, 0.16f), materials.SafetyYellow);

        for (int index = -6; index <= 6; index++)
        {
            Material stripeMaterial = index % 2 == 0 ? materials.SafetyYellow : materials.BarrierSteel;
            AddVisualCube(
                root,
                $"Rear Hazard Stripe {index + 6:00}",
                new Vector3(index * 1.6f, 1.05f, 13.19f),
                new Vector3(1.6f, 0.3f, 0.04f),
                stripeMaterial);
        }

        AddVisualCube(root, "Distant Stack A", new Vector3(-18f, 4f, 28f), new Vector3(7f, 8f, 8f), materials.Warehouse);
        AddVisualCube(root, "Distant Stack B", new Vector3(0f, 3f, 30f), new Vector3(10f, 6f, 7f), materials.Warehouse);
        AddVisualCube(root, "Distant Stack C", new Vector3(17f, 5f, 29f), new Vector3(8f, 10f, 8f), materials.Warehouse);
    }

    private static GameObject AddVisualCube(
        Transform parent,
        string name,
        Vector3 position,
        Vector3 scale,
        Material material)
    {
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visual.name = name;
        visual.transform.SetParent(parent, false);
        visual.transform.position = position;
        visual.transform.localScale = scale;
        visual.GetComponent<Renderer>().sharedMaterial = material;
        Object.DestroyImmediate(visual.GetComponent<Collider>());
        return visual;
    }

    private static void ConfigureGround(Material groundMaterial)
    {
        GameObject ground = GameObject.Find("Ground");
        Renderer renderer = ground != null ? ground.GetComponent<Renderer>() : null;
        if (renderer == null)
        {
            throw new UnityException("Vertical-slice scene is missing the Ground renderer.");
        }

        renderer.sharedMaterial = groundMaterial;
    }

    private static void ConfigureLighting(Material skybox)
    {
        Light keyLight = Object.FindAnyObjectByType<Light>();
        if (keyLight == null || keyLight.type != LightType.Directional)
        {
            throw new UnityException("Vertical-slice scene is missing its directional key light.");
        }

        keyLight.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
        keyLight.color = new Color(1f, 0.93f, 0.82f);
        keyLight.intensity = 1.35f;
        keyLight.shadows = LightShadows.Soft;
        keyLight.shadowStrength = 0.82f;
        keyLight.shadowBias = 0.035f;
        keyLight.shadowNormalBias = 0.25f;

        RenderSettings.sun = keyLight;
        RenderSettings.skybox = skybox;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.48f, 0.56f, 0.66f);
        RenderSettings.ambientEquatorColor = new Color(0.29f, 0.33f, 0.36f);
        RenderSettings.ambientGroundColor = new Color(0.12f, 0.13f, 0.14f);
        RenderSettings.ambientIntensity = 0.9f;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.52f, 0.59f, 0.64f);
        RenderSettings.fogStartDistance = 24f;
        RenderSettings.fogEndDistance = 58f;
    }

    private static PresentationMaterials CreateEnvironmentMaterials()
    {
        EnsureMaterialFolder();
        return new PresentationMaterials(
            CreateLitMaterial("YardConcrete", new Color(0.24f, 0.26f, 0.27f), 0f, 0.18f),
            CreateLitMaterial("BarrierSteel", new Color(0.11f, 0.15f, 0.17f), 0.72f, 0.34f),
            CreateLitMaterial("Warehouse", new Color(0.31f, 0.34f, 0.35f), 0.18f, 0.22f),
            CreateLitMaterial("ContainerRust", new Color(0.46f, 0.2f, 0.08f), 0.42f, 0.24f),
            CreateLitMaterial("ContainerBlue", new Color(0.12f, 0.28f, 0.36f), 0.48f, 0.28f),
            CreateLitMaterial("SafetyYellow", new Color(0.95f, 0.58f, 0.035f), 0.12f, 0.28f),
            CreateLitMaterial("LinePaint", new Color(0.74f, 0.72f, 0.58f), 0f, 0.14f),
            CreateSkyboxMaterial());
    }

    private static Material CreateLitMaterial(
        string name,
        Color color,
        float metallic,
        float smoothness)
    {
        string path = $"{MaterialFolder}/{name}.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                throw new UnityException("URP Lit shader is unavailable.");
            }

            material = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(material, path);
        }

        material.SetColor("_BaseColor", color);
        material.SetFloat("_Metallic", metallic);
        material.SetFloat("_Smoothness", smoothness);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material CreateSkyboxMaterial()
    {
        string path = $"{MaterialFolder}/IndustrialSky.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Skybox/Procedural");
            if (shader == null)
            {
                throw new UnityException("Procedural skybox shader is unavailable.");
            }

            material = new Material(shader) { name = "IndustrialSky" };
            AssetDatabase.CreateAsset(material, path);
        }

        material.SetColor("_SkyTint", new Color(0.38f, 0.53f, 0.66f));
        material.SetColor("_GroundColor", new Color(0.23f, 0.25f, 0.26f));
        material.SetFloat("_AtmosphereThickness", 0.72f);
        material.SetFloat("_SunSize", 0.03f);
        material.SetFloat("_Exposure", 1.05f);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void EnsureMaterialFolder()
    {
        if (!AssetDatabase.IsValidFolder(MaterialFolder))
        {
            AssetDatabase.CreateFolder("Assets/Materials", "Presentation");
        }
    }

    private static void ValidateEnvironmentInOpenScene(string scenePath)
    {
        GameObject root = GameObject.Find(EnvironmentRootName);
        if (root == null || root.GetComponentsInChildren<Renderer>().Length < 20)
        {
            throw new UnityException($"{scenePath} is missing the presentation environment.");
        }

        if (root.GetComponentInChildren<Collider>() != null)
        {
            throw new UnityException($"{scenePath} presentation geometry must remain non-colliding.");
        }

        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>())
        {
            if (renderer.sharedMaterial == null)
            {
                throw new UnityException($"{scenePath} has a presentation renderer without a material.");
            }
        }

        Renderer ground = GameObject.Find("Ground")?.GetComponent<Renderer>();
        Light keyLight = Object.FindAnyObjectByType<Light>();
        if (ground == null
            || ground.sharedMaterial == null
            || keyLight == null
            || keyLight.shadows != LightShadows.Soft
            || !RenderSettings.fog
            || RenderSettings.skybox == null)
        {
            throw new UnityException($"{scenePath} environment lighting or ground setup is incomplete.");
        }
    }

    private static void RemoveExistingRoot(string rootName)
    {
        GameObject existing = GameObject.Find(rootName);
        if (existing != null)
        {
            Object.DestroyImmediate(existing);
        }
    }

    private readonly struct PresentationMaterials
    {
        public PresentationMaterials(
            Material yardConcrete,
            Material barrierSteel,
            Material warehouse,
            Material containerRust,
            Material containerBlue,
            Material safetyYellow,
            Material linePaint,
            Material skybox)
        {
            YardConcrete = yardConcrete;
            BarrierSteel = barrierSteel;
            Warehouse = warehouse;
            ContainerRust = containerRust;
            ContainerBlue = containerBlue;
            SafetyYellow = safetyYellow;
            LinePaint = linePaint;
            Skybox = skybox;
        }

        public Material YardConcrete { get; }
        public Material BarrierSteel { get; }
        public Material Warehouse { get; }
        public Material ContainerRust { get; }
        public Material ContainerBlue { get; }
        public Material SafetyYellow { get; }
        public Material LinePaint { get; }
        public Material Skybox { get; }
    }
}
