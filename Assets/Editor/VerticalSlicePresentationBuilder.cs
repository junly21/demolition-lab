using System.Collections.Generic;
using DemolitionLab;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class VerticalSlicePresentationBuilder
{
    private const string MaterialFolder = "Assets/Materials/Presentation";
    private const string EnvironmentRootName = "Presentation Environment";
    private const string ConnectionDetailName = "Presentation Connection Detail";
    private const string LauncherPresentationName = "Launcher Presentation";
    private const string BarrelPresentationName = "Barrel Presentation";

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

    [MenuItem("DemolitionLab/Presentation/Apply Structural Presentation")]
    public static void ApplyStructuralPresentation()
    {
        foreach (string scenePath in StageScenePaths)
        {
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            ApplyStructuralPresentationToOpenScene();
            EditorSceneManager.SaveOpenScenes();
        }

        AssetDatabase.SaveAssets();
        Debug.Log("Applied structural, launcher, and HUD presentation to Stage01-05.");
    }

    [MenuItem("DemolitionLab/Presentation/Validate Structural Presentation")]
    public static void ValidateStructuralPresentation()
    {
        foreach (string scenePath in StageScenePaths)
        {
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            ValidateStructuralPresentationInOpenScene(scenePath);
        }

        Debug.Log("Structural presentation validation passed for Stage01-05.");
    }

    [MenuItem("DemolitionLab/Presentation/Apply Camera Framing")]
    public static void ApplyCameraFraming()
    {
        foreach (string scenePath in StageScenePaths)
        {
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            ApplyCameraFramingToOpenScene();
            EditorSceneManager.SaveOpenScenes();
        }

        Debug.Log("Applied presentation camera framing to Stage01-05.");
    }

    [MenuItem("DemolitionLab/Presentation/Validate Camera Framing")]
    public static void ValidateCameraFraming()
    {
        foreach (string scenePath in StageScenePaths)
        {
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            ValidateCameraFramingInOpenScene(scenePath);
        }

        Debug.Log("Presentation camera framing validation passed for Stage01-05.");
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

    internal static void ApplyStructuralPresentationToOpenScene()
    {
        StructuralPresentationMaterials materials = CreateStructuralMaterials();
        RemapStructuralMaterials(materials);
        AddConnectionDetails(materials);
        AddLauncherPresentation(materials);
        ConfigureHudPresentation();
        PhysicsPresentationSeparationBuilder.ApplyToOpenScene();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    internal static void ApplyCameraFramingToOpenScene()
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            throw new UnityException("Vertical-slice scene is missing its Main Camera.");
        }

        CameraFraming framing = GetCameraFraming(SceneManager.GetActiveScene().name);
        camera.transform.position = framing.Position;
        camera.transform.LookAt(framing.LookAt);
        camera.fieldOfView = framing.FieldOfView;
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    private static void RemapStructuralMaterials(StructuralPresentationMaterials materials)
    {
        foreach (GameObject piece in GameObject.FindGameObjectsWithTag("Destructible"))
        {
            foreach (Renderer renderer in piece.GetComponentsInChildren<Renderer>(true))
            {
                Material source = renderer.sharedMaterial;
                if (source == null)
                {
                    continue;
                }

                renderer.sharedMaterial = source.name switch
                {
                    "Concrete" => materials.Concrete,
                    "Weak" => materials.WeakSteel,
                    "Reinforced" => materials.ReinforcedSteel,
                    "Decor" => materials.UtilitySteel,
                    "Broken" => materials.BrokenSteel,
                    _ => source
                };
            }
        }
    }

    private static void AddConnectionDetails(StructuralPresentationMaterials materials)
    {
        foreach (CriticalSupportJoint joint in Object.FindObjectsByType<CriticalSupportJoint>())
        {
            Transform existing = PhysicsPresentationSeparationBuilder.FindDescendant(
                joint.transform,
                ConnectionDetailName);
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            Transform root = new GameObject(ConnectionDetailName).transform;
            root.SetParent(joint.transform, false);
            if (joint.IsWeak)
            {
                AddLocalPrimitive(root, PrimitiveType.Cube, "Exposed Connector Plate", new Vector3(0f, 0.08f, -0.56f), Quaternion.identity, new Vector3(1.18f, 0.16f, 0.08f), materials.WeakSteel);
                AddLocalPrimitive(root, PrimitiveType.Cube, "Hazard Strap Left", new Vector3(-0.3f, 0.08f, -0.61f), Quaternion.identity, new Vector3(0.13f, 0.62f, 0.035f), materials.SafetyYellow);
                AddLocalPrimitive(root, PrimitiveType.Cube, "Hazard Strap Right", new Vector3(0.3f, 0.08f, -0.61f), Quaternion.identity, new Vector3(0.13f, 0.62f, 0.035f), materials.SafetyYellow);
                AddLocalPrimitive(root, PrimitiveType.Sphere, "Bolt Left", new Vector3(-0.32f, 0.08f, -0.65f), Quaternion.identity, Vector3.one * 0.11f, materials.BarrierSteel);
                AddLocalPrimitive(root, PrimitiveType.Sphere, "Bolt Right", new Vector3(0.32f, 0.08f, -0.65f), Quaternion.identity, Vector3.one * 0.11f, materials.BarrierSteel);
            }
            else
            {
                AddLocalPrimitive(root, PrimitiveType.Cube, "Upper Reinforcement Collar", new Vector3(0f, 0.42f, 0f), Quaternion.identity, new Vector3(1.14f, 0.13f, 1.14f), materials.ReinforcedSteel);
                AddLocalPrimitive(root, PrimitiveType.Cube, "Lower Reinforcement Collar", new Vector3(0f, -0.42f, 0f), Quaternion.identity, new Vector3(1.14f, 0.13f, 1.14f), materials.ReinforcedSteel);
                AddLocalPrimitive(root, PrimitiveType.Cube, "Front Brace A", new Vector3(0f, 0f, -0.57f), Quaternion.Euler(0f, 0f, 35f), new Vector3(0.14f, 0.92f, 0.07f), materials.BarrierSteel);
                AddLocalPrimitive(root, PrimitiveType.Cube, "Front Brace B", new Vector3(0f, 0f, -0.58f), Quaternion.Euler(0f, 0f, -35f), new Vector3(0.14f, 0.92f, 0.07f), materials.BarrierSteel);
            }
        }
    }

    private static void AddLauncherPresentation(StructuralPresentationMaterials materials)
    {
        LauncherController launcher = Object.FindAnyObjectByType<LauncherController>();
        if (launcher == null)
        {
            throw new UnityException("Vertical-slice scene is missing its launcher.");
        }

        Transform existing = launcher.transform.Find(LauncherPresentationName);
        if (existing != null)
        {
            Object.DestroyImmediate(existing.gameObject);
        }

        foreach (MeshRenderer renderer in launcher.GetComponentsInChildren<MeshRenderer>(true))
        {
            renderer.sharedMaterial = materials.LauncherMetal;
        }

        Transform root = new GameObject(LauncherPresentationName).transform;
        root.SetParent(launcher.transform, false);
        AddLocalPrimitive(root, PrimitiveType.Cube, "Base Housing", new Vector3(0f, 0.42f, 0f), Quaternion.identity, new Vector3(1.75f, 0.42f, 1.35f), materials.LauncherMetal);
        AddLocalPrimitive(root, PrimitiveType.Cube, "Turntable Deck", new Vector3(0f, 0.72f, 0f), Quaternion.identity, new Vector3(1.25f, 0.16f, 1.05f), materials.LauncherAccent);
        AddLocalPrimitive(root, PrimitiveType.Cylinder, "Yaw Ring", new Vector3(0f, 0.82f, 0f), Quaternion.identity, new Vector3(0.72f, 0.12f, 0.72f), materials.BarrierSteel);
        AddLocalPrimitive(root, PrimitiveType.Cube, "Support Leg FL", new Vector3(-0.78f, 0.18f, -0.58f), Quaternion.Euler(0f, 0f, -12f), new Vector3(0.22f, 0.55f, 0.25f), materials.BarrierSteel);
        AddLocalPrimitive(root, PrimitiveType.Cube, "Support Leg FR", new Vector3(0.78f, 0.18f, -0.58f), Quaternion.Euler(0f, 0f, 12f), new Vector3(0.22f, 0.55f, 0.25f), materials.BarrierSteel);
        AddLocalPrimitive(root, PrimitiveType.Cube, "Support Leg RL", new Vector3(-0.78f, 0.18f, 0.58f), Quaternion.Euler(0f, 0f, -12f), new Vector3(0.22f, 0.55f, 0.25f), materials.BarrierSteel);
        AddLocalPrimitive(root, PrimitiveType.Cube, "Support Leg RR", new Vector3(0.78f, 0.18f, 0.58f), Quaternion.Euler(0f, 0f, 12f), new Vector3(0.22f, 0.55f, 0.25f), materials.BarrierSteel);

        var serializedLauncher = new SerializedObject(launcher);
        Transform pitchPivot = (Transform)serializedLauncher.FindProperty("pitchPivot").objectReferenceValue;
        if (pitchPivot == null)
        {
            throw new UnityException("Launcher presentation requires the pitch pivot reference.");
        }

        Transform existingBarrel = pitchPivot.Find(BarrelPresentationName);
        if (existingBarrel != null)
        {
            Object.DestroyImmediate(existingBarrel.gameObject);
        }

        Transform barrelRoot = new GameObject(BarrelPresentationName).transform;
        barrelRoot.SetParent(pitchPivot, false);
        AddLocalPrimitive(barrelRoot, PrimitiveType.Cylinder, "Breech Sleeve", new Vector3(0f, 0f, 0.42f), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.46f, 0.42f, 0.46f), materials.LauncherMetal);
        AddLocalPrimitive(barrelRoot, PrimitiveType.Cylinder, "Muzzle Collar", new Vector3(0f, 0f, 2.05f), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.4f, 0.18f, 0.4f), materials.LauncherAccent);
        AddLocalPrimitive(barrelRoot, PrimitiveType.Cube, "Pivot Bracket Left", new Vector3(-0.48f, -0.08f, 0.15f), Quaternion.identity, new Vector3(0.18f, 0.72f, 0.75f), materials.BarrierSteel);
        AddLocalPrimitive(barrelRoot, PrimitiveType.Cube, "Pivot Bracket Right", new Vector3(0.48f, -0.08f, 0.15f), Quaternion.identity, new Vector3(0.18f, 0.72f, 0.75f), materials.BarrierSteel);
    }

    private static void ConfigureHudPresentation()
    {
        VerticalSliceStageController controller = Object.FindAnyObjectByType<VerticalSliceStageController>();
        if (controller == null)
        {
            throw new UnityException("Vertical-slice scene is missing its stage controller.");
        }

        var serializedController = new SerializedObject(controller);
        Text stage = (Text)serializedController.FindProperty("stageLabelText").objectReferenceValue;
        Text shots = (Text)serializedController.FindProperty("shotsText").objectReferenceValue;
        Text destruction = (Text)serializedController.FindProperty("destructionText").objectReferenceValue;
        GameObject endPanel = (GameObject)serializedController.FindProperty("endPanel").objectReferenceValue;
        Button next = (Button)serializedController.FindProperty("nextButton").objectReferenceValue;

        ConfigureHudText(stage, 32, new Vector2(32f, -28f), new Vector2(320f, 54f), new Vector2(0f, 1f));
        ConfigureHudText(shots, 32, new Vector2(-32f, -28f), new Vector2(320f, 54f), new Vector2(1f, 1f));
        ConfigureHudText(destruction, 30, new Vector2(0f, 30f), new Vector2(460f, 54f), new Vector2(0.5f, 0f));

        Image panelImage = endPanel != null ? endPanel.GetComponent<Image>() : null;
        if (panelImage != null)
        {
            panelImage.color = new Color(0.035f, 0.05f, 0.06f, 0.96f);
        }

        ConfigureButton(next);
        Button retry = endPanel != null ? endPanel.transform.Find("Retry Button")?.GetComponent<Button>() : null;
        ConfigureButton(retry);
    }

    private static void ConfigureHudText(
        Text text,
        int fontSize,
        Vector2 position,
        Vector2 size,
        Vector2 anchor)
    {
        if (text == null)
        {
            throw new UnityException("Vertical-slice HUD has a missing Text reference.");
        }

        text.fontSize = fontSize;
        text.color = new Color(0.94f, 0.95f, 0.92f);
        RectTransform rect = text.rectTransform;
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Outline outline = text.GetComponent<Outline>();
        if (outline == null)
        {
            outline = text.gameObject.AddComponent<Outline>();
        }

        outline.effectColor = new Color(0.02f, 0.03f, 0.035f, 0.9f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);
    }

    private static void ConfigureButton(Button button)
    {
        if (button == null)
        {
            return;
        }

        Image image = button.GetComponent<Image>();
        if (image != null)
        {
            image.color = new Color(0.11f, 0.2f, 0.24f, 1f);
        }

        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.11f, 0.2f, 0.24f, 1f);
        colors.highlightedColor = new Color(0.18f, 0.35f, 0.4f, 1f);
        colors.pressedColor = new Color(0.9f, 0.52f, 0.08f, 1f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;
    }

    private static GameObject AddLocalPrimitive(
        Transform parent,
        PrimitiveType primitiveType,
        string name,
        Vector3 localPosition,
        Quaternion localRotation,
        Vector3 localScale,
        Material material)
    {
        GameObject visual = GameObject.CreatePrimitive(primitiveType);
        visual.name = name;
        visual.transform.SetParent(parent, false);
        visual.transform.localPosition = localPosition;
        visual.transform.localRotation = localRotation;
        visual.transform.localScale = localScale;
        visual.GetComponent<Renderer>().sharedMaterial = material;
        Object.DestroyImmediate(visual.GetComponent<Collider>());
        return visual;
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

    private static StructuralPresentationMaterials CreateStructuralMaterials()
    {
        EnsureMaterialFolder();
        PresentationMaterials environment = CreateEnvironmentMaterials();
        return new StructuralPresentationMaterials(
            CreateLitMaterial("StructuralConcrete", new Color(0.46f, 0.49f, 0.5f), 0f, 0.16f),
            CreateLitMaterial("WeakSteel", new Color(0.86f, 0.34f, 0.045f), 0.46f, 0.25f),
            CreateLitMaterial("ReinforcedSteel", new Color(0.075f, 0.22f, 0.31f), 0.72f, 0.32f),
            CreateLitMaterial("UtilitySteel", new Color(0.32f, 0.24f, 0.15f), 0.38f, 0.22f),
            CreateLitMaterial("BrokenSteel", new Color(0.19f, 0.065f, 0.04f), 0.32f, 0.16f),
            CreateLitMaterial("LauncherMetal", new Color(0.075f, 0.095f, 0.105f), 0.82f, 0.38f),
            CreateLitMaterial("LauncherAccent", new Color(0.82f, 0.38f, 0.045f), 0.58f, 0.3f),
            environment.BarrierSteel,
            environment.SafetyYellow);
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

    private static void ValidateStructuralPresentationInOpenScene(string scenePath)
    {
        CriticalSupportJoint[] joints = Object.FindObjectsByType<CriticalSupportJoint>();
        if (joints.Length == 0)
        {
            throw new UnityException($"{scenePath} has no critical support joints to present.");
        }

        foreach (CriticalSupportJoint joint in joints)
        {
            Transform detail = PhysicsPresentationSeparationBuilder.FindDescendant(
                joint.transform,
                ConnectionDetailName);
            int expectedRenderers = joint.IsWeak ? 5 : 4;
            if (detail == null
                || detail.GetComponentsInChildren<Renderer>().Length != expectedRenderers
                || detail.GetComponentInChildren<Collider>() != null
                || detail.GetComponentInChildren<Rigidbody>() != null)
            {
                throw new UnityException($"{scenePath} joint '{joint.name}' has invalid visual detailing.");
            }

            foreach (Renderer renderer in detail.GetComponentsInChildren<Renderer>())
            {
                if (renderer.sharedMaterial == null)
                {
                    throw new UnityException($"{scenePath} joint '{joint.name}' has a missing visual material.");
                }
            }
        }

        LauncherController launcher = Object.FindAnyObjectByType<LauncherController>();
        Transform launcherPresentation = launcher != null
            ? launcher.transform.Find(LauncherPresentationName)
            : null;
        Transform pitchPivot = launcher != null
            ? (Transform)new SerializedObject(launcher).FindProperty("pitchPivot").objectReferenceValue
            : null;
        Transform barrelPresentation = pitchPivot != null
            ? pitchPivot.Find(BarrelPresentationName)
            : null;
        if (launcherPresentation == null
            || launcherPresentation.GetComponentsInChildren<Renderer>().Length < 7
            || launcherPresentation.GetComponentInChildren<Collider>() != null
            || launcherPresentation.GetComponentInChildren<Rigidbody>() != null)
        {
            throw new UnityException($"{scenePath} launcher presentation is incomplete or colliding.");
        }

        if (barrelPresentation == null
            || barrelPresentation.GetComponentsInChildren<Renderer>().Length != 4
            || barrelPresentation.GetComponentInChildren<Collider>() != null)
        {
            throw new UnityException($"{scenePath} barrel presentation is incomplete or colliding.");
        }

        VerticalSliceStageController controller = Object.FindAnyObjectByType<VerticalSliceStageController>();
        var serializedController = new SerializedObject(controller);
        string[] hudProperties = { "stageLabelText", "shotsText", "destructionText" };
        foreach (string propertyName in hudProperties)
        {
            Text text = (Text)serializedController.FindProperty(propertyName).objectReferenceValue;
            if (text == null || text.GetComponent<Outline>() == null)
            {
                throw new UnityException($"{scenePath} HUD presentation is incomplete.");
            }
        }

        PhysicsPresentationSeparationBuilder.ValidateOpenScene(scenePath);
    }

    private static void ValidateCameraFramingInOpenScene(string scenePath)
    {
        Camera camera = Camera.main;
        CameraFraming framing = GetCameraFraming(SceneManager.GetActiveScene().name);
        Vector3 expectedDirection = (framing.LookAt - framing.Position).normalized;
        if (camera == null
            || Vector3.Distance(camera.transform.position, framing.Position) > 0.001f
            || Vector3.Angle(camera.transform.forward, expectedDirection) > 0.05f
            || !Mathf.Approximately(camera.fieldOfView, framing.FieldOfView))
        {
            throw new UnityException($"{scenePath} presentation camera framing is incorrect.");
        }
    }

    private static CameraFraming GetCameraFraming(string sceneName)
    {
        return sceneName switch
        {
            "VerticalSlice_Stage01" => new CameraFraming(new Vector3(8.2f, 5.6f, -12.8f), new Vector3(0f, 2.1f, 4f), 49f),
            "VerticalSlice_Stage02" => new CameraFraming(new Vector3(9.2f, 5f, -13.8f), new Vector3(0f, 2.05f, 4f), 50f),
            "VerticalSlice_Stage03" => new CameraFraming(new Vector3(9.2f, 5.3f, -14f), new Vector3(0f, 2.55f, 4f), 49f),
            "VerticalSlice_Stage04" => new CameraFraming(new Vector3(10.8f, 7.2f, -17.2f), new Vector3(0f, 3.9f, 4f), 49f),
            "VerticalSlice_Stage05" => new CameraFraming(new Vector3(12f, 7f, -18.5f), new Vector3(1f, 3.65f, 4f), 51f),
            _ => throw new UnityException($"No presentation camera framing is configured for '{sceneName}'.")
        };
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

    private readonly struct StructuralPresentationMaterials
    {
        public StructuralPresentationMaterials(
            Material concrete,
            Material weakSteel,
            Material reinforcedSteel,
            Material utilitySteel,
            Material brokenSteel,
            Material launcherMetal,
            Material launcherAccent,
            Material barrierSteel,
            Material safetyYellow)
        {
            Concrete = concrete;
            WeakSteel = weakSteel;
            ReinforcedSteel = reinforcedSteel;
            UtilitySteel = utilitySteel;
            BrokenSteel = brokenSteel;
            LauncherMetal = launcherMetal;
            LauncherAccent = launcherAccent;
            BarrierSteel = barrierSteel;
            SafetyYellow = safetyYellow;
        }

        public Material Concrete { get; }
        public Material WeakSteel { get; }
        public Material ReinforcedSteel { get; }
        public Material UtilitySteel { get; }
        public Material BrokenSteel { get; }
        public Material LauncherMetal { get; }
        public Material LauncherAccent { get; }
        public Material BarrierSteel { get; }
        public Material SafetyYellow { get; }
    }

    private readonly struct CameraFraming
    {
        public CameraFraming(Vector3 position, Vector3 lookAt, float fieldOfView)
        {
            Position = position;
            LookAt = lookAt;
            FieldOfView = fieldOfView;
        }

        public Vector3 Position { get; }
        public Vector3 LookAt { get; }
        public float FieldOfView { get; }
    }
}
