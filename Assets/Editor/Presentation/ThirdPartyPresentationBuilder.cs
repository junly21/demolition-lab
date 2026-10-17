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
    private const string IntegratedEnvironmentName = "Integrated Industrial Assets";
    private const string LauncherDetailName = "Integrated Launcher Housing";

    private static readonly string[] StageScenePaths =
    {
        "Assets/Scenes/VerticalSlice_Stage01.unity",
        "Assets/Scenes/VerticalSlice_Stage02.unity",
        "Assets/Scenes/VerticalSlice_Stage03.unity",
        "Assets/Scenes/VerticalSlice_Stage04.unity",
        "Assets/Scenes/VerticalSlice_Stage05.unity"
    };

    [MenuItem("DemolitionLab/Presentation/Apply Imported Industrial Assets")]
    public static void ApplyToAllStages()
    {
        IndustrialAssetFactory.RebuildPresentationAssets();
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
        IndustrialAssetFactory.EnsurePresentationAssets();
        IndustrialAssetFactory.ValidateProjectPrefabs();
        IndustrialAssetFactory.ValidateStructuralMaterials();
        foreach (string scenePath in StageScenePaths)
        {
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            ValidateOpenScene(scenePath);
        }

        Debug.Log("Imported industrial asset validation passed for Stage01-05.");
    }

    internal static void ApplyEnvironmentToOpenScene()
    {
        IndustrialAssetFactory.EnsurePresentationAssets();
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

        groundRenderer.sharedMaterial = IndustrialAssetFactory.LoadMaterial("DL_Asphalt");
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    internal static void ApplyStructuralVisualsToOpenScene()
    {
        IndustrialAssetFactory.EnsurePresentationAssets();
        foreach (PresentationVisual visual in UnityEngine.Object.FindObjectsByType<PresentationVisual>())
        {
            MeshRenderer renderer = visual.GetComponent<MeshRenderer>();
            if (renderer == null)
            {
                continue;
            }

            renderer.sharedMaterial = visual.Category switch
            {
                PresentationCategory.Concrete => SelectConcreteMaterial(visual.transform.parent?.name),
                PresentationCategory.Steel => IndustrialAssetFactory.LoadMaterial("DL_Steel"),
                PresentationCategory.Weak => IndustrialAssetFactory.LoadMaterial("DL_WeakSteel"),
                PresentationCategory.Reinforced => IndustrialAssetFactory.LoadMaterial("DL_ReinforcedSteel"),
                PresentationCategory.Foundation => IndustrialAssetFactory.LoadMaterial("DL_FoundationConcrete"),
                _ => IndustrialAssetFactory.LoadMaterial("DL_Steel")
            };
        }

        LauncherController launcher = UnityEngine.Object.FindAnyObjectByType<LauncherController>();
        if (launcher == null)
        {
            throw new UnityException("The vertical-slice scene is missing its launcher.");
        }

        foreach (MeshRenderer renderer in launcher.GetComponentsInChildren<MeshRenderer>(true))
        {
            renderer.sharedMaterial = IndustrialAssetFactory.LoadMaterial("DL_LauncherMetal");
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

        IndustrialAssetFactory.ValidateVisualOnlyHierarchy(environment, $"{scenePath} environment");

        Renderer groundRenderer = GameObject.Find("Ground")?.GetComponent<Renderer>();
        if (groundRenderer == null || groundRenderer.sharedMaterial != IndustrialAssetFactory.LoadMaterial("DL_Asphalt"))
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

        IndustrialAssetFactory.ValidateVisualOnlyHierarchy(launcherDetail, $"{scenePath} launcher housing");
        PhysicsPresentationSeparationBuilder.ValidateOpenScene(scenePath);
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
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{IndustrialAssetFactory.PrefabFolder}/{prefabName}.prefab");
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

    private static Material SelectConcreteMaterial(string objectName)
    {
        int stableValue = 0;
        if (!string.IsNullOrEmpty(objectName))
        {
            foreach (char character in objectName)
            {
                stableValue += character;
            }
        }

        return (stableValue % 3) switch
        {
            1 => IndustrialAssetFactory.LoadMaterial("DL_ConcreteDark"),
            2 => IndustrialAssetFactory.LoadMaterial("DL_ConcreteWorn"),
            _ => IndustrialAssetFactory.LoadMaterial("DL_Concrete")
        };
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
}
