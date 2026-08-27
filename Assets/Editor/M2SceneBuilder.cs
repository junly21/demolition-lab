using System.Collections.Generic;
using System.Reflection;
using DemolitionLab;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class M2SceneBuilder
{
    private const string ScenePath = "Assets/Scenes/M2_StructuralStability.unity";
    private const string ProjectilePrefabPath = "Assets/Prefabs/Projectile.prefab";

    [MenuItem("DemolitionLab/Build M2 Scene")]
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
        Material stableMaterial = CreateMaterial(
            "Assets/Materials/M2_Stable.mat",
            new Color(0.16f, 0.62f, 0.43f));
        Material unstableMaterial = CreateMaterial(
            "Assets/Materials/M2_Unstable.mat",
            new Color(0.95f, 0.64f, 0.12f));
        Material disconnectedMaterial = CreateMaterial(
            "Assets/Materials/M2_Disconnected.mat",
            new Color(0.82f, 0.16f, 0.13f));
        Material destroyedMaterial = CreateMaterial(
            "Assets/Materials/M2_Destroyed.mat",
            new Color(0.35f, 0.37f, 0.4f));

        Projectile projectilePrefab = LoadRequiredAsset<GameObject>(ProjectilePrefabPath)
            .GetComponent<Projectile>();
        if (projectilePrefab == null)
        {
            throw new UnityException($"Projectile component is missing from {ProjectilePrefabPath}.");
        }

        CreateLighting();
        CreateGround(groundMaterial);
        CreateStructure(stableMaterial, unstableMaterial, disconnectedMaterial, destroyedMaterial);
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
        Debug.Log($"Built Milestone 2 scene at {ScenePath}");
    }

    [MenuItem("DemolitionLab/Validate M2 Scene")]
    public static void ValidateScene()
    {
        ValidateStabilityMath();
        ValidateNonCriticalUpperRemoval();
        ValidateLeftSupportRemoval();
        ValidateRightSupportRemoval();
        ValidateDisconnectedComponent();
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Debug.Log("M2 validation passed: stability math and component release scenarios verified.");
    }

    private static void ValidateStabilityMath()
    {
        if (!StructuralStabilityEvaluator.IsWithinSupportInterval(0f, -1f, 1f, 0f))
        {
            throw new UnityException("Inside-interval stability check failed.");
        }

        if (StructuralStabilityEvaluator.IsWithinSupportInterval(1.2f, -1f, 1f, 0f))
        {
            throw new UnityException("Outside-interval stability check failed.");
        }

        if (!StructuralStabilityEvaluator.IsWithinSupportInterval(1.04f, -1f, 1f, 0.05f)
            || StructuralStabilityEvaluator.IsWithinSupportInterval(1.06f, -1f, 1f, 0.05f))
        {
            throw new UnityException("Support tolerance boundary check failed.");
        }

        Vector3 centerOfMass = StructuralStabilityEvaluator.CalculateCenterOfMass(
            new[] { Vector3.zero, new Vector3(10f, 0f, 0f) },
            new[] { 1f, 3f },
            out float totalMass);
        if (!Mathf.Approximately(centerOfMass.x, 7.5f) || !Mathf.Approximately(totalMass, 4f))
        {
            throw new UnityException("Mass-weighted center-of-mass check failed.");
        }
    }

    private static void ValidateLeftSupportRemoval()
    {
        ValidationContext context = LoadAndInitializeScene();
        StructuralBlock leftBase = context.Blocks["Left_Base"];
        leftBase.HandleProjectileImpact(new Vector3(12f, 0f, 0f), leftBase.transform.position);

        if (leftBase.State != StructuralBlockState.Destroyed
            || leftBase.GetComponent<Rigidbody>().isKinematic
            || !leftBase.GetComponent<Renderer>().enabled
            || !leftBase.GetComponent<Collider>().enabled)
        {
            throw new UnityException("Destroyed M2 support did not become visible dynamic debris.");
        }

        foreach (StructuralBlock block in context.Blocks.Values)
        {
            if (block.IsActive
                && (block.State != StructuralBlockState.Unstable
                    || block.GetComponent<Rigidbody>().isKinematic
                    || block.ParticipatesInStructure))
            {
                throw new UnityException("The connected overhanging component was not released together.");
            }
        }

        AssertNoStaticComponentsRemain(context.Manager);
    }

    private static void ValidateRightSupportRemoval()
    {
        ValidationContext context = LoadAndInitializeScene();
        context.Blocks["Right_Base"].HandleProjectileImpact();

        foreach (StructuralBlock block in context.Blocks.Values)
        {
            if (block.IsActive
                && (!block.IsSupported || !block.GetComponent<Rigidbody>().isKinematic))
            {
                throw new UnityException("The heavy left-supported component should remain stable.");
            }
        }
    }

    private static void ValidateNonCriticalUpperRemoval()
    {
        ValidationContext context = LoadAndInitializeScene();
        StructuralBlock roofRight = context.Blocks["Roof_Right"];
        roofRight.HandleProjectileImpact();

        int staticBlockCount = 0;
        foreach (StructuralBlock block in context.Blocks.Values)
        {
            if (block.ParticipatesInStructure
                && (!block.IsSupported || !block.GetComponent<Rigidbody>().isKinematic))
            {
                throw new UnityException("Removing Roof_Right should leave the static structure stable.");
            }

            if (block.ParticipatesInStructure)
            {
                staticBlockCount++;
            }
        }

        if (roofRight.State != StructuralBlockState.Destroyed || staticBlockCount != 10)
        {
            throw new UnityException("The non-critical upper-block scenario did not preserve 10 static blocks.");
        }
    }

    private static void ValidateDisconnectedComponent()
    {
        ValidationContext context = LoadAndInitializeScene();
        context.Blocks["Right_Base"].HandleProjectileImpact();
        context.Blocks["Left_Base"].HandleProjectileImpact();

        foreach (StructuralBlock block in context.Blocks.Values)
        {
            if (block.IsActive
                && (block.State != StructuralBlockState.Disconnected
                    || block.GetComponent<Rigidbody>().isKinematic
                    || block.ParticipatesInStructure))
            {
                throw new UnityException("A component without ground connectivity was not marked disconnected.");
            }
        }

        AssertNoStaticComponentsRemain(context.Manager);
    }

    private static ValidationContext LoadAndInitializeScene()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        StructuralBlock[] blocks = Object.FindObjectsByType<StructuralBlock>(FindObjectsSortMode.None);
        StructureManager manager = Object.FindFirstObjectByType<StructureManager>();
        StructuralStabilityEvaluator evaluator = Object.FindFirstObjectByType<StructuralStabilityEvaluator>();

        if (manager == null || evaluator == null || blocks.Length != 11)
        {
            throw new UnityException("M2 scene must contain 11 blocks, a manager, and a stability evaluator.");
        }

        var blocksById = new Dictionary<string, StructuralBlock>();
        int anchorCount = 0;
        foreach (StructuralBlock block in blocks)
        {
            if (!blocksById.TryAdd(block.NodeId, block))
            {
                throw new UnityException($"Duplicate M2 node ID: {block.NodeId}");
            }

            if (block.IsGroundAnchor)
            {
                anchorCount++;
            }
        }

        if (anchorCount != 2)
        {
            throw new UnityException($"M2 scene expected 2 anchors but found {anchorCount}.");
        }

        MethodInfo awake = typeof(StructureManager).GetMethod(
            "Awake",
            BindingFlags.Instance | BindingFlags.NonPublic);
        awake?.Invoke(manager, null);
        Physics.SyncTransforms();
        ValidateInitialGeometry(blocks);
        ValidateGroundContact(blocks);
        return new ValidationContext(blocksById, manager);
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
        ground.transform.position = new Vector3(0f, -0.5f, 1f);
        ground.transform.localScale = new Vector3(18f, 1f, 16f);
        ground.GetComponent<MeshRenderer>().sharedMaterial = material;
    }

    private static void CreateStructure(
        Material stableMaterial,
        Material unstableMaterial,
        Material disconnectedMaterial,
        Material destroyedMaterial)
    {
        GameObject structureRoot = new("M2 Structural Stability");
        const float structureZ = 2f;
        var blocks = new List<StructuralBlock>
        {
            CreateBlock("Left_Base", new Vector3(-2f, 0.5f, structureZ), new Vector3(3f, 1f, 1f), 3f, structureRoot.transform),
            CreateBlock("Left_Mid", new Vector3(-2f, 1.5f, structureZ), Vector3.one, 1f, structureRoot.transform),
            CreateBlock("Left_Top", new Vector3(-2f, 2.5f, structureZ), Vector3.one, 1f, structureRoot.transform),
            CreateBlock("Beam_Left", new Vector3(-1f, 2.5f, structureZ), Vector3.one, 1f, structureRoot.transform),
            CreateBlock("Beam_Center", new Vector3(0f, 2.5f, structureZ), Vector3.one, 1f, structureRoot.transform),
            CreateBlock("Beam_Right", new Vector3(1f, 2.5f, structureZ), Vector3.one, 1f, structureRoot.transform),
            CreateBlock("Right_Top", new Vector3(2f, 2.5f, structureZ), Vector3.one, 1f, structureRoot.transform),
            CreateBlock("Right_Mid", new Vector3(2f, 1.5f, structureZ), Vector3.one, 1f, structureRoot.transform),
            CreateBlock("Right_Base", new Vector3(2f, 0.5f, structureZ), new Vector3(3f, 1f, 1f), 3f, structureRoot.transform),
            CreateBlock("Roof_Left", new Vector3(-2f, 3.5f, structureZ), Vector3.one, 1f, structureRoot.transform),
            CreateBlock("Roof_Right", new Vector3(-1f, 3.5f, structureZ), Vector3.one, 1f, structureRoot.transform)
        };

        var connections = new Dictionary<StructuralBlock, List<StructuralBlock>>();
        foreach (StructuralBlock block in blocks)
        {
            connections.Add(block, new List<StructuralBlock>());
        }

        for (int index = 0; index < 8; index++)
        {
            Connect(blocks[index], blocks[index + 1], connections);
        }
        Connect(blocks[2], blocks[9], connections);
        Connect(blocks[9], blocks[10], connections);
        Connect(blocks[10], blocks[3], connections);

        foreach (StructuralBlock block in blocks)
        {
            var serializedBlock = new SerializedObject(block);
            serializedBlock.FindProperty("nodeId").stringValue = block.name;
            serializedBlock.FindProperty("isGroundAnchor").boolValue = block == blocks[0] || block == blocks[8];
            serializedBlock.FindProperty("supportedMaterial").objectReferenceValue = stableMaterial;
            serializedBlock.FindProperty("unsupportedMaterial").objectReferenceValue = unstableMaterial;
            serializedBlock.FindProperty("disconnectedMaterial").objectReferenceValue = disconnectedMaterial;
            serializedBlock.FindProperty("destroyedMaterial").objectReferenceValue = destroyedMaterial;

            SerializedProperty neighbors = serializedBlock.FindProperty("neighbors");
            neighbors.arraySize = connections[block].Count;
            for (int index = 0; index < connections[block].Count; index++)
            {
                neighbors.GetArrayElementAtIndex(index).objectReferenceValue = connections[block][index];
            }
            serializedBlock.ApplyModifiedPropertiesWithoutUndo();
        }

        StructuralStabilityEvaluator evaluator = structureRoot.AddComponent<StructuralStabilityEvaluator>();
        StructureManager manager = structureRoot.AddComponent<StructureManager>();
        var serializedManager = new SerializedObject(manager);
        SerializedProperty serializedBlocks = serializedManager.FindProperty("blocks");
        serializedBlocks.arraySize = blocks.Count;
        for (int index = 0; index < blocks.Count; index++)
        {
            serializedBlocks.GetArrayElementAtIndex(index).objectReferenceValue = blocks[index];
        }
        serializedManager.FindProperty("stabilityEvaluator").objectReferenceValue = evaluator;
        serializedManager.FindProperty("keepDestroyedBlocksAsDebris").boolValue = true;
        serializedManager.ApplyModifiedPropertiesWithoutUndo();
    }

    private static StructuralBlock CreateBlock(
        string nodeId,
        Vector3 position,
        Vector3 scale,
        float mass,
        Transform parent)
    {
        GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
        block.name = nodeId;
        block.tag = "Destructible";
        block.transform.SetParent(parent);
        block.transform.position = position;
        block.transform.localScale = scale;

        Rigidbody rigidbody = block.AddComponent<Rigidbody>();
        rigidbody.mass = mass;
        rigidbody.isKinematic = true;
        rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        return block.AddComponent<StructuralBlock>();
    }

    private static void ValidateInitialGeometry(IReadOnlyList<StructuralBlock> blocks)
    {
        for (int firstIndex = 0; firstIndex < blocks.Count; firstIndex++)
        {
            StructuralBlock first = blocks[firstIndex];
            Collider firstCollider = first.GetComponent<Collider>();

            for (int secondIndex = firstIndex + 1; secondIndex < blocks.Count; secondIndex++)
            {
                StructuralBlock second = blocks[secondIndex];
                Collider secondCollider = second.GetComponent<Collider>();
                if (Physics.ComputePenetration(
                        firstCollider,
                        first.transform.position,
                        first.transform.rotation,
                        secondCollider,
                        second.transform.position,
                        second.transform.rotation,
                        out _,
                        out float distance)
                    && distance > 0.0001f)
                {
                    throw new UnityException(
                        $"M2 initial penetration: '{first.NodeId}' and '{second.NodeId}' overlap by {distance:0.0000} m.");
                }
            }

            foreach (StructuralBlock neighbor in first.Neighbors)
            {
                float separation = CalculateBoundsSeparation(
                    firstCollider.bounds,
                    neighbor.GetComponent<Collider>().bounds);
                if (separation > 0.001f)
                {
                    throw new UnityException(
                        $"M2 graph adjacency gap: '{first.NodeId}' and '{neighbor.NodeId}' are {separation:0.0000} m apart.");
                }
            }
        }
    }

    private static float CalculateBoundsSeparation(Bounds first, Bounds second)
    {
        float x = Mathf.Max(0f, Mathf.Max(first.min.x - second.max.x, second.min.x - first.max.x));
        float y = Mathf.Max(0f, Mathf.Max(first.min.y - second.max.y, second.min.y - first.max.y));
        float z = Mathf.Max(0f, Mathf.Max(first.min.z - second.max.z, second.min.z - first.max.z));
        return new Vector3(x, y, z).magnitude;
    }

    private static void ValidateGroundContact(IReadOnlyList<StructuralBlock> blocks)
    {
        GameObject ground = GameObject.Find("Ground");
        Collider groundCollider = ground != null ? ground.GetComponent<Collider>() : null;
        if (groundCollider == null)
        {
            throw new UnityException("M2 validation could not find the Ground collider.");
        }

        foreach (StructuralBlock block in blocks)
        {
            if (!block.IsGroundAnchor)
            {
                continue;
            }

            Collider blockCollider = block.GetComponent<Collider>();
            if (Physics.ComputePenetration(
                    blockCollider,
                    block.transform.position,
                    block.transform.rotation,
                    groundCollider,
                    ground.transform.position,
                    ground.transform.rotation,
                    out _,
                    out float penetration)
                && penetration > 0.0001f)
            {
                throw new UnityException(
                    $"M2 ground penetration: '{block.NodeId}' overlaps Ground by {penetration:0.0000} m.");
            }

            float separation = CalculateBoundsSeparation(blockCollider.bounds, groundCollider.bounds);
            if (separation > 0.001f)
            {
                throw new UnityException(
                    $"M2 ground gap: '{block.NodeId}' is {separation:0.0000} m above Ground.");
            }
        }
    }

    private static void AssertNoStaticComponentsRemain(StructureManager manager)
    {
        MethodInfo findComponents = typeof(StructureManager).GetMethod(
            "FindActiveComponents",
            BindingFlags.Instance | BindingFlags.NonPublic);
        var components = findComponents?.Invoke(manager, null) as List<List<StructuralBlock>>;
        if (components == null || components.Count != 0)
        {
            throw new UnityException("Released blocks still participate in static structural evaluation.");
        }
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
        launcher.transform.position = new Vector3(0f, 0f, -8f);

        GameObject baseObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        baseObject.name = "Base";
        baseObject.transform.SetParent(launcher.transform, false);
        baseObject.transform.localPosition = new Vector3(0f, 0.35f, 0f);
        baseObject.transform.localScale = new Vector3(1.4f, 0.35f, 1.4f);
        baseObject.GetComponent<MeshRenderer>().sharedMaterial = launcherMaterial;

        GameObject yawPivot = new("Yaw Pivot");
        yawPivot.transform.SetParent(launcher.transform, false);
        yawPivot.transform.localPosition = new Vector3(0f, 0.75f, 0f);

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
            throw new UnityException("Failed to assign the projectile prefab to the M2 launcher.");
        }
    }

    private static void CreateCamera()
    {
        GameObject cameraObject = new("Main Camera");
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.fieldOfView = 50f;
        cameraObject.AddComponent<AudioListener>();
        cameraObject.transform.position = new Vector3(0f, 6.5f, -16f);
        cameraObject.transform.LookAt(new Vector3(-0.5f, 1.8f, 0f));
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
            throw new UnityException($"Required M2 asset is missing: {path}");
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

    private readonly struct ValidationContext
    {
        public ValidationContext(Dictionary<string, StructuralBlock> blocks, StructureManager manager)
        {
            Blocks = blocks;
            Manager = manager;
        }

        public Dictionary<string, StructuralBlock> Blocks { get; }
        public StructureManager Manager { get; }
    }
}
