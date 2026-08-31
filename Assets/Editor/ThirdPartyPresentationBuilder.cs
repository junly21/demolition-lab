using System;
using System.Collections.Generic;
using System.IO;
using DemolitionLab;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ThirdPartyPresentationBuilder
{
    private static bool assetsReady;

    private const string MaterialFolder = "Assets/Art/Materials";
    private const string PrefabFolder = "Assets/Art/Prefabs/Industrial";
    private const string IntegratedEnvironmentName = "Integrated Industrial Assets";
    private const string LauncherDetailName = "Integrated Launcher Housing";

    private const string CityRoot = "Assets/Art/ThirdParty/kenney_city-kit-industrial_1.0";
    private const string FactoryRoot = "Assets/Art/ThirdParty/kenney_factory-kit_3.0";
    private const string AsphaltRoot = "Assets/Art/ThirdParty/asphalt_01_4k.blend/textures";
    private const string ConcreteRoot = "Assets/Art/ThirdParty/concrete_4k.blend/textures";

    private static readonly string[] StageScenePaths =
    {
        "Assets/Scenes/VerticalSlice_Stage01.unity",
        "Assets/Scenes/VerticalSlice_Stage02.unity",
        "Assets/Scenes/VerticalSlice_Stage03.unity",
        "Assets/Scenes/VerticalSlice_Stage04.unity",
        "Assets/Scenes/VerticalSlice_Stage05.unity"
    };

    private static readonly ModelDefinition[] ModelDefinitions =
    {
        new("IndustrialWarehouse", $"{CityRoot}/Models/FBX format/building-f.fbx", MaterialKind.City),
        new("IndustrialPlant", $"{CityRoot}/Models/FBX format/building-l.fbx", MaterialKind.City),
        new("CoolingChimney", $"{CityRoot}/Models/FBX format/chimney-large.fbx", MaterialKind.City),
        new("StorageTank", $"{CityRoot}/Models/FBX format/detail-tank.fbx", MaterialKind.City),
        new("CargoCrate", $"{FactoryRoot}/Models/FBX format/box-large.fbx", MaterialKind.Factory),
        new("FactoryMachine", $"{FactoryRoot}/Models/FBX format/machine-fortified.fbx", MaterialKind.Factory),
        new("IndustrialPipe", $"{FactoryRoot}/Models/FBX format/pipe-large-long.fbx", MaterialKind.Factory),
        new("YardCrane", $"{FactoryRoot}/Models/FBX format/crane.fbx", MaterialKind.Factory)
    };

    [MenuItem("DemolitionLab/Presentation/Apply Imported Industrial Assets")]
    public static void ApplyToAllStages()
    {
        RebuildPresentationAssets();
        foreach (string scenePath in StageScenePaths)
        {
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            ApplyEnvironmentToOpenScene();
            ApplyStructuralVisualsToOpenScene();
            EditorSceneManager.SaveOpenScenes();
        }

        AssetDatabase.SaveAssets();
        Debug.Log("Applied imported industrial presentation assets to Stage01-05.");
    }

    [MenuItem("DemolitionLab/Presentation/Validate Imported Industrial Assets")]
    public static void ValidateAllStages()
    {
        EnsurePresentationAssets();
        ValidateProjectPrefabs();
        foreach (string scenePath in StageScenePaths)
        {
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            ValidateOpenScene(scenePath);
        }

        Debug.Log("Imported industrial asset validation passed for Stage01-05.");
    }

    internal static void ApplyEnvironmentToOpenScene()
    {
        EnsurePresentationAssets();
        GameObject presentationEnvironment = GameObject.Find("Presentation Environment");
        if (presentationEnvironment == null)
        {
            throw new UnityException("The presentation environment must exist before imported assets are applied.");
        }

        RemoveChild(presentationEnvironment.transform, IntegratedEnvironmentName);
        RemovePrototypeBackground(presentationEnvironment.transform);

        Transform root = new GameObject(IntegratedEnvironmentName).transform;
        root.SetParent(presentationEnvironment.transform, false);
        AddStageEnvironment(root, SceneManager.GetActiveScene().name);

        Renderer groundRenderer = GameObject.Find("Ground")?.GetComponent<Renderer>();
        if (groundRenderer == null)
        {
            throw new UnityException("The vertical-slice scene is missing its Ground renderer.");
        }

        groundRenderer.sharedMaterial = LoadMaterial("DL_Asphalt");
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    internal static void ApplyStructuralVisualsToOpenScene()
    {
        EnsurePresentationAssets();
        foreach (PresentationVisual visual in UnityEngine.Object.FindObjectsByType<PresentationVisual>())
        {
            MeshRenderer renderer = visual.GetComponent<MeshRenderer>();
            if (renderer == null)
            {
                continue;
            }

            renderer.sharedMaterial = visual.Category switch
            {
                PresentationCategory.Concrete => LoadMaterial("DL_Concrete"),
                PresentationCategory.Steel => LoadMaterial("DL_Steel"),
                PresentationCategory.Weak => LoadMaterial("DL_WeakSteel"),
                PresentationCategory.Reinforced => LoadMaterial("DL_ReinforcedSteel"),
                PresentationCategory.Foundation => LoadMaterial("DL_FoundationConcrete"),
                _ => LoadMaterial("DL_Steel")
            };
        }

        LauncherController launcher = UnityEngine.Object.FindAnyObjectByType<LauncherController>();
        if (launcher == null)
        {
            throw new UnityException("The vertical-slice scene is missing its launcher.");
        }

        foreach (MeshRenderer renderer in launcher.GetComponentsInChildren<MeshRenderer>(true))
        {
            renderer.sharedMaterial = LoadMaterial("DL_LauncherMetal");
        }

        RemoveChild(launcher.transform, LauncherDetailName);
        GameObject housing = InstantiateProjectPrefab(
            "FactoryMachine",
            launcher.transform,
            LauncherDetailName,
            new Vector3(-0.72f, 0.48f, 0.35f),
            Quaternion.Euler(0f, 180f, 0f),
            Vector3.one * 0.42f);
        housing.transform.SetAsLastSibling();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    internal static void ValidateOpenScene(string scenePath)
    {
        Transform environment = GameObject.Find("Presentation Environment")?.transform
            .Find(IntegratedEnvironmentName);
        if (environment == null || environment.childCount < 6)
        {
            throw new UnityException($"{scenePath} is missing its integrated industrial environment.");
        }

        ValidateVisualOnlyHierarchy(environment, $"{scenePath} environment");

        Renderer groundRenderer = GameObject.Find("Ground")?.GetComponent<Renderer>();
        if (groundRenderer == null || groundRenderer.sharedMaterial != LoadMaterial("DL_Asphalt"))
        {
            throw new UnityException($"{scenePath} does not use the project asphalt material.");
        }

        foreach (PresentationVisual visual in UnityEngine.Object.FindObjectsByType<PresentationVisual>())
        {
            MeshRenderer renderer = visual.GetComponent<MeshRenderer>();
            if (renderer == null || renderer.sharedMaterial == null
                || !renderer.sharedMaterial.name.StartsWith("DL_", StringComparison.Ordinal))
            {
                throw new UnityException($"{scenePath} VisualRoot '{visual.transform.parent?.name}' has no integrated material.");
            }
        }

        LauncherController launcher = UnityEngine.Object.FindAnyObjectByType<LauncherController>();
        Transform launcherDetail = launcher != null ? launcher.transform.Find(LauncherDetailName) : null;
        if (launcherDetail == null)
        {
            throw new UnityException($"{scenePath} is missing the imported launcher housing.");
        }

        ValidateVisualOnlyHierarchy(launcherDetail, $"{scenePath} launcher housing");
        PhysicsPresentationSeparationBuilder.ValidateOpenScene(scenePath);
    }

    private static void EnsurePresentationAssets()
    {
        if (assetsReady)
        {
            return;
        }

        if (AssetDatabase.LoadAssetAtPath<Material>($"{MaterialFolder}/DL_Asphalt.mat") != null
            && AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/IndustrialWarehouse.prefab") != null)
        {
            assetsReady = true;
            return;
        }

        RebuildPresentationAssets();
    }

    private static void RebuildPresentationAssets()
    {
        assetsReady = false;

        EnsureFolder("Assets/Art", "Materials");
        EnsureFolder("Assets/Art", "Prefabs");
        EnsureFolder("Assets/Art/Prefabs", "Industrial");

        ConfigureTexture($"{AsphaltRoot}/asphalt_01_diff_4k.jpg", false, true);
        ConfigureTexture($"{AsphaltRoot}/asphalt_01_nor_gl_4k.exr", true, false);
        ConfigureTexture($"{AsphaltRoot}/asphalt_01_rough_4k.exr", false, false);
        ConfigureTexture($"{ConcreteRoot}/concrete_diff_4k.jpg", false, true);
        ConfigureTexture($"{ConcreteRoot}/concrete_nor_gl_4k.exr", true, false);
        ConfigureTexture($"{ConcreteRoot}/concrete_rough_4k.jpg", false, false);

        string asphaltMask = $"{MaterialFolder}/Asphalt_MetallicSmoothness.png";
        string concreteMask = $"{MaterialFolder}/Concrete_MetallicSmoothness.png";
        CreateSmoothnessMask($"{AsphaltRoot}/asphalt_01_rough_4k.exr", asphaltMask);
        CreateSmoothnessMask($"{ConcreteRoot}/concrete_rough_4k.jpg", concreteMask);

        CreateSurfaceMaterial(
            "DL_Asphalt",
            $"{AsphaltRoot}/asphalt_01_diff_4k.jpg",
            $"{AsphaltRoot}/asphalt_01_nor_gl_4k.exr",
            asphaltMask,
            new Color(0.48f, 0.5f, 0.5f),
            new Vector2(4f, 4f));
        CreateSurfaceMaterial(
            "DL_Concrete",
            $"{ConcreteRoot}/concrete_diff_4k.jpg",
            $"{ConcreteRoot}/concrete_nor_gl_4k.exr",
            concreteMask,
            new Color(0.86f, 0.88f, 0.88f),
            new Vector2(1.35f, 1.35f));
        CreateSurfaceMaterial(
            "DL_FoundationConcrete",
            $"{ConcreteRoot}/concrete_diff_4k.jpg",
            $"{ConcreteRoot}/concrete_nor_gl_4k.exr",
            concreteMask,
            new Color(0.62f, 0.65f, 0.65f),
            Vector2.one);

        CreateSimpleMaterial("DL_Steel", new Color(0.16f, 0.19f, 0.2f), 0.78f, 0.34f);
        CreateSimpleMaterial("DL_WeakSteel", new Color(0.27f, 0.18f, 0.11f), 0.62f, 0.27f);
        CreateSimpleMaterial("DL_ReinforcedSteel", new Color(0.055f, 0.13f, 0.18f), 0.86f, 0.4f);
        CreateSimpleMaterial("DL_LauncherMetal", new Color(0.055f, 0.07f, 0.075f), 0.9f, 0.42f);
        CreateColorMapMaterial(
            "DL_KenneyCity",
            $"{CityRoot}/Models/FBX format/Textures/colormap.png",
            new Color(0.58f, 0.62f, 0.68f));
        CreateColorMapMaterial(
            "DL_KenneyFactory",
            $"{FactoryRoot}/Models/FBX format/Textures/colormap.png",
            new Color(0.5f, 0.54f, 0.62f));

        foreach (ModelDefinition definition in ModelDefinitions)
        {
            CreateProjectPrefab(definition);
        }

        AssetDatabase.SaveAssets();
        assetsReady = true;
    }

    private static void AddStageEnvironment(Transform root, string sceneName)
    {
        switch (sceneName)
        {
            case "VerticalSlice_Stage01":
                AddPrefab(root, "IndustrialWarehouse", new Vector3(-10.8f, 0f, 17.2f), 2.35f, 18f);
                AddPrefab(root, "IndustrialPlant", new Vector3(9.2f, 0f, 18.4f), 2.15f, -12f);
                AddPrefab(root, "CoolingChimney", new Vector3(12.6f, 0f, 16.1f), 1.65f, 0f);
                AddPrefab(root, "StorageTank", new Vector3(-7.8f, 0f, 13.9f), 1.3f, 12f);
                AddYardProps(root, -1f);
                break;
            case "VerticalSlice_Stage02":
                AddPrefab(root, "IndustrialPlant", new Vector3(-11.5f, 0f, 18.2f), 2.25f, 10f);
                AddPrefab(root, "IndustrialWarehouse", new Vector3(9.5f, 0f, 18.8f), 2.4f, -18f);
                AddPrefab(root, "CoolingChimney", new Vector3(-13.2f, 0f, 15.5f), 1.8f, 0f);
                AddYardProps(root, 1f);
                break;
            case "VerticalSlice_Stage03":
                AddPrefab(root, "IndustrialWarehouse", new Vector3(-11.8f, 0f, 18.3f), 2.45f, 8f);
                AddPrefab(root, "IndustrialPlant", new Vector3(10.5f, 0f, 19f), 2.3f, -14f);
                AddPrefab(root, "StorageTank", new Vector3(12.8f, 0f, 14.8f), 1.45f, -12f);
                AddYardProps(root, -1f);
                break;
            case "VerticalSlice_Stage04":
                AddPrefab(root, "IndustrialPlant", new Vector3(-12.5f, 0f, 19.5f), 2.45f, 14f);
                AddPrefab(root, "IndustrialWarehouse", new Vector3(10.8f, 0f, 20f), 2.55f, -12f);
                AddPrefab(root, "CoolingChimney", new Vector3(-9f, 0f, 15.4f), 1.65f, 0f);
                AddPrefab(root, "CoolingChimney", new Vector3(13.8f, 0f, 17f), 1.35f, 0f);
                AddYardProps(root, 1f);
                break;
            case "VerticalSlice_Stage05":
                AddPrefab(root, "IndustrialWarehouse", new Vector3(-12.8f, 0f, 20f), 2.65f, 10f);
                AddPrefab(root, "IndustrialPlant", new Vector3(10.5f, 0f, 21f), 2.45f, -16f);
                AddPrefab(root, "YardCrane", new Vector3(12.6f, 0f, 14.8f), 1.65f, -24f);
                AddPrefab(root, "StorageTank", new Vector3(-10f, 0f, 14.4f), 1.4f, 18f);
                AddYardProps(root, -1f);
                break;
            default:
                throw new UnityException($"Unsupported vertical-slice scene '{sceneName}'.");
        }
    }

    private static void AddYardProps(Transform root, float side)
    {
        AddPrefab(root, "CargoCrate", new Vector3(10.8f * side, 0f, 9.8f), 1.15f, 18f * side);
        AddPrefab(root, "CargoCrate", new Vector3(12.1f * side, 0f, 10.1f), 0.9f, -12f * side);
        AddPrefab(root, "FactoryMachine", new Vector3(-11.8f * side, 0f, 11.2f), 1.05f, 15f * side);
        AddPrefab(root, "IndustrialPipe", new Vector3(-10.4f * side, 0.55f, 9.6f), 1.1f, 88f);
    }

    private static void AddPrefab(
        Transform parent,
        string prefabName,
        Vector3 position,
        float scale,
        float yaw)
    {
        InstantiateProjectPrefab(
            prefabName,
            parent,
            prefabName,
            position,
            Quaternion.Euler(0f, yaw, 0f),
            Vector3.one * scale);
    }

    private static GameObject InstantiateProjectPrefab(
        string prefabName,
        Transform parent,
        string instanceName,
        Vector3 position,
        Quaternion rotation,
        Vector3 scale)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/{prefabName}.prefab");
        if (prefab == null)
        {
            throw new UnityException($"Missing project-owned presentation prefab '{prefabName}'.");
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        instance.name = instanceName;
        instance.transform.localPosition = position;
        instance.transform.localRotation = rotation;
        instance.transform.localScale = scale;
        return instance;
    }

    private static void CreateProjectPrefab(ModelDefinition definition)
    {
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(definition.SourcePath);
        if (model == null)
        {
            throw new UnityException($"Missing imported model '{definition.SourcePath}'.");
        }

        GameObject root = new(definition.PrefabName);
        GameObject modelInstance = (GameObject)PrefabUtility.InstantiatePrefab(model);
        PrefabUtility.UnpackPrefabInstance(
            modelInstance,
            PrefabUnpackMode.Completely,
            InteractionMode.AutomatedAction);
        modelInstance.name = "Visual";
        modelInstance.transform.SetParent(root.transform, false);

        StripNonVisualComponents(root);
        Material material = definition.MaterialKind == MaterialKind.City
            ? LoadMaterial("DL_KenneyCity")
            : LoadMaterial("DL_KenneyFactory");
        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            renderer.sharedMaterial = material;
        }

        PrefabUtility.SaveAsPrefabAsset(root, $"{PrefabFolder}/{definition.PrefabName}.prefab");
        UnityEngine.Object.DestroyImmediate(root);
    }

    private static void StripNonVisualComponents(GameObject root)
    {
        foreach (Component component in root.GetComponentsInChildren<Component>(true))
        {
            if (component is Transform or MeshFilter or MeshRenderer)
            {
                continue;
            }

            UnityEngine.Object.DestroyImmediate(component);
        }
    }

    private static void ValidateProjectPrefabs()
    {
        foreach (ModelDefinition definition in ModelDefinitions)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                $"{PrefabFolder}/{definition.PrefabName}.prefab");
            if (prefab == null)
            {
                throw new UnityException($"Missing project-owned prefab '{definition.PrefabName}'.");
            }

            ValidateVisualOnlyHierarchy(prefab.transform, $"Prefab {definition.PrefabName}");
        }
    }

    private static void ValidateVisualOnlyHierarchy(Transform root, string context)
    {
        if (root.GetComponentInChildren<Rigidbody>(true) != null
            || root.GetComponentInChildren<Collider>(true) != null
            || root.GetComponentInChildren<Joint>(true) != null
            || root.GetComponentInChildren<MonoBehaviour>(true) != null)
        {
            throw new UnityException($"{context} contains a physics or gameplay component.");
        }

        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer.sharedMaterial == null
                || renderer.sharedMaterial.shader == null
                || !renderer.sharedMaterial.shader.name.StartsWith("Universal Render Pipeline/", StringComparison.Ordinal))
            {
                throw new UnityException($"{context} contains an incompatible renderer material.");
            }
        }
    }

    private static void ConfigureTexture(string path, bool isNormalMap, bool sRgb)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            throw new UnityException($"Missing imported texture '{path}'.");
        }

        importer.textureType = isNormalMap ? TextureImporterType.NormalMap : TextureImporterType.Default;
        importer.sRGBTexture = sRgb;
        importer.maxTextureSize = 2048;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        importer.SaveAndReimport();
    }

    private static void CreateSmoothnessMask(string roughnessPath, string outputPath)
    {
        TextureImporter importer = AssetImporter.GetAtPath(roughnessPath) as TextureImporter;
        if (importer == null)
        {
            throw new UnityException($"Missing roughness texture '{roughnessPath}'.");
        }

        importer.isReadable = true;
        importer.sRGBTexture = false;
        importer.maxTextureSize = 2048;
        importer.SaveAndReimport();

        Texture2D roughness = AssetDatabase.LoadAssetAtPath<Texture2D>(roughnessPath);
        Color[] sourcePixels = roughness.GetPixels();
        var mask = new Texture2D(roughness.width, roughness.height, TextureFormat.RGBA32, false, true);
        var maskPixels = new Color[sourcePixels.Length];
        for (int index = 0; index < sourcePixels.Length; index++)
        {
            float smoothness = 1f - sourcePixels[index].r;
            maskPixels[index] = new Color(0f, 0f, 0f, smoothness);
        }

        mask.SetPixels(maskPixels);
        mask.Apply();
        File.WriteAllBytes(outputPath, mask.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(mask);
        AssetDatabase.ImportAsset(outputPath, ImportAssetOptions.ForceUpdate);

        TextureImporter maskImporter = AssetImporter.GetAtPath(outputPath) as TextureImporter;
        maskImporter.sRGBTexture = false;
        maskImporter.maxTextureSize = 2048;
        maskImporter.textureCompression = TextureImporterCompression.CompressedHQ;
        maskImporter.SaveAndReimport();

        importer.isReadable = false;
        importer.SaveAndReimport();
    }

    private static void CreateSurfaceMaterial(
        string name,
        string baseMapPath,
        string normalMapPath,
        string maskMapPath,
        Color tint,
        Vector2 tiling)
    {
        Material material = GetOrCreateMaterial(name);
        material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(baseMapPath));
        material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(normalMapPath));
        material.SetTexture("_MetallicGlossMap", AssetDatabase.LoadAssetAtPath<Texture2D>(maskMapPath));
        material.SetTextureScale("_BaseMap", tiling);
        material.SetTextureScale("_BumpMap", tiling);
        material.SetTextureScale("_MetallicGlossMap", tiling);
        material.SetColor("_BaseColor", tint);
        material.SetFloat("_Metallic", 0f);
        material.SetFloat("_Smoothness", 1f);
        material.SetFloat("_BumpScale", 0.65f);
        material.EnableKeyword("_NORMALMAP");
        material.EnableKeyword("_METALLICSPECGLOSSMAP");
        EditorUtility.SetDirty(material);
    }

    private static void CreateSimpleMaterial(
        string name,
        Color color,
        float metallic,
        float smoothness)
    {
        Material material = GetOrCreateMaterial(name);
        material.SetTexture("_BaseMap", null);
        material.SetTexture("_BumpMap", null);
        material.SetTexture("_MetallicGlossMap", null);
        material.SetColor("_BaseColor", color);
        material.SetFloat("_Metallic", metallic);
        material.SetFloat("_Smoothness", smoothness);
        material.DisableKeyword("_NORMALMAP");
        material.DisableKeyword("_METALLICSPECGLOSSMAP");
        EditorUtility.SetDirty(material);
    }

    private static void CreateColorMapMaterial(string name, string colorMapPath, Color tint)
    {
        ConfigureTexture(colorMapPath, false, true);
        Material material = GetOrCreateMaterial(name);
        material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(colorMapPath));
        material.SetColor("_BaseColor", tint);
        material.SetFloat("_Metallic", 0.08f);
        material.SetFloat("_Smoothness", 0.28f);
        EditorUtility.SetDirty(material);
    }

    private static Material GetOrCreateMaterial(string name)
    {
        string path = $"{MaterialFolder}/{name}.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null)
        {
            return material;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            throw new UnityException("URP Lit shader is unavailable.");
        }

        material = new Material(shader) { name = name };
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static Material LoadMaterial(string name)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialFolder}/{name}.mat");
        if (material == null)
        {
            throw new UnityException($"Missing project presentation material '{name}'.");
        }

        return material;
    }

    private static void RemovePrototypeBackground(Transform environment)
    {
        string[] names =
        {
            "Warehouse West",
            "Warehouse East",
            "Warehouse Roof West",
            "Warehouse Roof East",
            "Distant Stack A",
            "Distant Stack B",
            "Distant Stack C"
        };

        foreach (string objectName in names)
        {
            RemoveChild(environment, objectName);
        }
    }

    private static void RemoveChild(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);
        if (child != null)
        {
            UnityEngine.Object.DestroyImmediate(child.gameObject);
        }
    }

    private static void EnsureFolder(string parent, string folderName)
    {
        string path = $"{parent}/{folderName}";
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, folderName);
        }
    }

    private readonly struct ModelDefinition
    {
        public ModelDefinition(string prefabName, string sourcePath, MaterialKind materialKind)
        {
            PrefabName = prefabName;
            SourcePath = sourcePath;
            MaterialKind = materialKind;
        }

        public string PrefabName { get; }
        public string SourcePath { get; }
        public MaterialKind MaterialKind { get; }
    }

    private enum MaterialKind
    {
        City,
        Factory
    }
}
