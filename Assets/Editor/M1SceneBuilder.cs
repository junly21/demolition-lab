using System.Collections.Generic;
using System.Reflection;
using DemolitionLab;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class M1SceneBuilder
{
    private const string ScenePath = "Assets/Scenes/M1_StructuralGraph.unity";
    private const string ProjectilePrefabPath = "Assets/Prefabs/Projectile.prefab";

    [MenuItem("DemolitionLab/Build M1 Scene")]
    public static void BuildScene()
    {
        Scene previousScene = SceneManager.GetActiveScene();
        bool preservePreviousScene = previousScene.IsValid() && !string.IsNullOrEmpty(previousScene.path);
        NewSceneMode sceneMode = preservePreviousScene ? NewSceneMode.Additive : NewSceneMode.Single;
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, sceneMode);
        SceneManager.SetActiveScene(scene);

        Material groundMaterial = LoadRequiredAsset<Material>("Assets/Materials/Ground.mat");
        Material launcherMaterial = LoadRequiredAsset<Material>("Assets/Materials/Launcher.mat");
        Material aimMaterial = LoadRequiredAsset<Material>("Assets/Materials/AimLine.mat");
        Material supportedMaterial = CreateMaterial(
            "Assets/Materials/M1_Supported.mat",
            new Color(0.2f, 0.62f, 0.48f));
        Material unsupportedMaterial = CreateMaterial(
            "Assets/Materials/M1_Unsupported.mat",
            new Color(0.8f, 0.22f, 0.16f));
        Projectile projectilePrefab = LoadRequiredAsset<GameObject>(ProjectilePrefabPath)
            .GetComponent<Projectile>();
        if (projectilePrefab == null)
        {
            throw new UnityException($"Projectile component is missing from {ProjectilePrefabPath}.");
        }

        CreateLighting();
        CreateGround(groundMaterial);
        CreateStructure(supportedMaterial, unsupportedMaterial);
        CreateLauncher(projectilePrefab, launcherMaterial, aimMaterial);
        CreateCamera();
        new GameObject("Game Manager").AddComponent<ResetController>();

        EditorSceneManager.SaveScene(scene, ScenePath);
        AddSceneToBuildSettings();
        AssetDatabase.SaveAssets();

        if (preservePreviousScene && previousScene.isLoaded)
        {
            SceneManager.SetActiveScene(previousScene);
            EditorSceneManager.CloseScene(scene, true);
        }
        Debug.Log($"Built Milestone 1 scene at {ScenePath}");
    }

    [MenuItem("DemolitionLab/Validate M1 Scene")]
    public static void ValidateScene()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        StructuralBlock[] blocks = Object.FindObjectsByType<StructuralBlock>(FindObjectsSortMode.None);
        StructureManager manager = Object.FindFirstObjectByType<StructureManager>();

        if (manager == null || blocks.Length != 10)
        {
            throw new UnityException("M1 scene must contain one StructureManager and 10 structural blocks.");
        }

        var blocksById = new Dictionary<string, StructuralBlock>();
        int anchorCount = 0;
        foreach (StructuralBlock block in blocks)
        {
            if (string.IsNullOrWhiteSpace(block.NodeId) || !blocksById.TryAdd(block.NodeId, block))
            {
                throw new UnityException("Every M1 structural block must have a unique node ID.");
            }

            if (block.IsGroundAnchor)
            {
                anchorCount++;
            }
        }

        if (anchorCount != 2)
        {
            throw new UnityException($"M1 scene expected 2 ground anchors but found {anchorCount}.");
        }

        MethodInfo awake = typeof(StructureManager).GetMethod(
            "Awake",
            BindingFlags.Instance | BindingFlags.NonPublic);
        awake?.Invoke(manager, null);

        foreach (StructuralBlock block in blocks)
        {
            if (!block.IsSupported || !block.GetComponent<Rigidbody>().isKinematic)
            {
                throw new UnityException($"Node '{block.NodeId}' was not supported after graph initialization.");
            }
        }

        blocksById["Bridge_Center"].HandleProjectileImpact();
        StructuralBlock roofCap = blocksById["Roof_Cap"];
        if (!roofCap.IsActive || roofCap.IsSupported || roofCap.GetComponent<Rigidbody>().isKinematic)
        {
            throw new UnityException("Roof_Cap did not become dynamic after Bridge_Center was removed.");
        }

        if (!blocksById["Left_Top"].IsSupported || !blocksById["Right_Top"].IsSupported)
        {
            throw new UnityException("Ground-connected components lost support during the split test.");
        }

        Debug.Log("M1 validation passed: 10 nodes, 2 anchors, and split-component physics transition verified.");
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
    }

    private static void CreateLighting()
    {
        GameObject lightObject = new("Directional Light");
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.2f;
        light.shadows = LightShadows.Soft;
        lightObject.transform.rotation = Quaternion.Euler(50f, -35f, 0f);

        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.42f, 0.48f, 0.58f);
        RenderSettings.ambientEquatorColor = new Color(0.24f, 0.27f, 0.3f);
        RenderSettings.ambientGroundColor = new Color(0.1f, 0.1f, 0.1f);
    }

    private static void CreateGround(Material material)
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "Ground";
        ground.transform.position = new Vector3(0f, -0.5f, 0f);
        ground.transform.localScale = new Vector3(24f, 1f, 14f);
        ground.GetComponent<MeshRenderer>().sharedMaterial = material;
    }

    private static void CreateStructure(Material supportedMaterial, Material unsupportedMaterial)
    {
        GameObject structureRoot = new("M1 Structural Graph");
        var blocks = new List<StructuralBlock>
        {
            CreateBlock("Left_Base", new Vector3(5.5f, 0.5f, -2f), structureRoot.transform),
            CreateBlock("Left_Mid", new Vector3(5.5f, 1.5f, -2f), structureRoot.transform),
            CreateBlock("Left_Top", new Vector3(5.5f, 2.5f, -2f), structureRoot.transform),
            CreateBlock("Bridge_Left", new Vector3(5.5f, 2.5f, -1f), structureRoot.transform),
            CreateBlock("Bridge_Center", new Vector3(5.5f, 2.5f, 0f), structureRoot.transform),
            CreateBlock("Bridge_Right", new Vector3(5.5f, 2.5f, 1f), structureRoot.transform),
            CreateBlock("Right_Top", new Vector3(5.5f, 2.5f, 2f), structureRoot.transform),
            CreateBlock("Right_Mid", new Vector3(5.5f, 1.5f, 2f), structureRoot.transform),
            CreateBlock("Right_Base", new Vector3(5.5f, 0.5f, 2f), structureRoot.transform),
            CreateBlock("Roof_Cap", new Vector3(5.5f, 3.5f, 0f), structureRoot.transform)
        };

        var connections = new Dictionary<StructuralBlock, List<StructuralBlock>>();
        foreach (StructuralBlock block in blocks)
        {
            connections.Add(block, new List<StructuralBlock>());
        }

        Connect(blocks[0], blocks[1], connections);
        Connect(blocks[1], blocks[2], connections);
        Connect(blocks[2], blocks[3], connections);
        Connect(blocks[3], blocks[4], connections);
        Connect(blocks[4], blocks[5], connections);
        Connect(blocks[5], blocks[6], connections);
        Connect(blocks[6], blocks[7], connections);
        Connect(blocks[7], blocks[8], connections);
        Connect(blocks[4], blocks[9], connections);

        foreach (StructuralBlock block in blocks)
        {
            var serializedBlock = new SerializedObject(block);
            serializedBlock.FindProperty("nodeId").stringValue = block.name;
            serializedBlock.FindProperty("isGroundAnchor").boolValue = block == blocks[0] || block == blocks[8];
            serializedBlock.FindProperty("supportedMaterial").objectReferenceValue = supportedMaterial;
            serializedBlock.FindProperty("unsupportedMaterial").objectReferenceValue = unsupportedMaterial;

            SerializedProperty neighbors = serializedBlock.FindProperty("neighbors");
            neighbors.arraySize = connections[block].Count;
            for (int index = 0; index < connections[block].Count; index++)
            {
                neighbors.GetArrayElementAtIndex(index).objectReferenceValue = connections[block][index];
            }

            serializedBlock.ApplyModifiedPropertiesWithoutUndo();
        }

        StructureManager manager = structureRoot.AddComponent<StructureManager>();
        var serializedManager = new SerializedObject(manager);
        SerializedProperty serializedBlocks = serializedManager.FindProperty("blocks");
        serializedBlocks.arraySize = blocks.Count;
        for (int index = 0; index < blocks.Count; index++)
        {
            serializedBlocks.GetArrayElementAtIndex(index).objectReferenceValue = blocks[index];
        }
        serializedManager.ApplyModifiedPropertiesWithoutUndo();
    }

    private static StructuralBlock CreateBlock(
        string nodeId,
        Vector3 position,
        Transform parent)
    {
        GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
        block.name = nodeId;
        block.tag = "Destructible";
        block.transform.SetParent(parent);
        block.transform.position = position;
        block.transform.localScale = new Vector3(1.35f, 0.9f, 0.9f);

        Rigidbody rigidbody = block.AddComponent<Rigidbody>();
        rigidbody.mass = 2f;
        rigidbody.isKinematic = true;
        rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        return block.AddComponent<StructuralBlock>();
    }

    private static void Connect(
        StructuralBlock first,
        StructuralBlock second,
        Dictionary<StructuralBlock, List<StructuralBlock>> connections)
    {
        connections[first].Add(second);
        connections[second].Add(first);
    }

    private static void CreateLauncher(
        Projectile projectilePrefab,
        Material launcherMaterial,
        Material aimMaterial)
    {
        GameObject launcher = new("Launcher");
        launcher.transform.position = new Vector3(-8f, 0f, 0f);

        GameObject baseObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        baseObject.name = "Base";
        baseObject.transform.SetParent(launcher.transform, false);
        baseObject.transform.localPosition = new Vector3(0f, 0.35f, 0f);
        baseObject.transform.localScale = new Vector3(1.4f, 0.35f, 1.4f);
        baseObject.GetComponent<MeshRenderer>().sharedMaterial = launcherMaterial;

        GameObject yawPivot = new("Yaw Pivot");
        yawPivot.transform.SetParent(launcher.transform, false);
        yawPivot.transform.localPosition = new Vector3(0f, 0.75f, 0f);
        yawPivot.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);

        GameObject pitchPivot = new("Pitch Pivot");
        pitchPivot.transform.SetParent(yawPivot.transform, false);

        GameObject barrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        barrel.name = "Barrel";
        barrel.transform.SetParent(pitchPivot.transform, false);
        barrel.transform.localPosition = new Vector3(0f, 0f, 1.15f);
        barrel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        barrel.transform.localScale = new Vector3(0.32f, 1.15f, 0.32f);
        barrel.GetComponent<MeshRenderer>().sharedMaterial = launcherMaterial;

        GameObject muzzle = new("Muzzle");
        muzzle.transform.SetParent(pitchPivot.transform, false);
        muzzle.transform.localPosition = new Vector3(0f, 0f, 2.4f);

        LineRenderer aimLine = launcher.AddComponent<LineRenderer>();
        aimLine.useWorldSpace = true;
        aimLine.startWidth = 0.055f;
        aimLine.endWidth = 0.025f;
        aimLine.sharedMaterial = aimMaterial;
        aimLine.shadowCastingMode = ShadowCastingMode.Off;
        aimLine.receiveShadows = false;

        LauncherController controller = launcher.AddComponent<LauncherController>();
        var serializedController = new SerializedObject(controller);
        serializedController.FindProperty("yawPivot").objectReferenceValue = yawPivot.transform;
        serializedController.FindProperty("pitchPivot").objectReferenceValue = pitchPivot.transform;
        serializedController.FindProperty("muzzle").objectReferenceValue = muzzle.transform;
        serializedController.FindProperty("projectilePrefab").objectReferenceValue = projectilePrefab;
        serializedController.FindProperty("aimLine").objectReferenceValue = aimLine;
        serializedController.ApplyModifiedPropertiesWithoutUndo();

        if (serializedController.FindProperty("projectilePrefab").objectReferenceValue == null)
        {
            throw new UnityException("Failed to assign the projectile prefab to the M1 launcher.");
        }
    }

    private static void CreateCamera()
    {
        GameObject cameraObject = new("Main Camera");
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.fieldOfView = 52f;
        cameraObject.AddComponent<AudioListener>();
        cameraObject.transform.position = new Vector3(-1f, 7.2f, -16f);
        cameraObject.transform.LookAt(new Vector3(0f, 1.7f, 0f));
    }

    private static Material CreateMaterial(string path, Color color)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(material, path);
        }

        material.color = color;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static T LoadRequiredAsset<T>(string path) where T : Object
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null)
        {
            throw new UnityException($"Required M1 asset is missing: {path}");
        }

        return asset;
    }

    private static void AddSceneToBuildSettings()
    {
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        scenes.RemoveAll(scene => scene.path == ScenePath);
        scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
