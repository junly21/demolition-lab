using System.Collections.Generic;
using System.Reflection;
using DemolitionLab;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class M4SceneBuilder
{
    private const string SourceScenePath = "Assets/Scenes/M3_LocalSupport.unity";
    private const string ScenePath = "Assets/Scenes/M4_BreakableConnections.unity";
    private const float WeakGravityCapacity = 120f;
    private const float WeakImpactCapacity = 28f;
    private const float ReinforcedGravityCapacity = 240f;
    private const float ReinforcedImpactCapacity = 60f;

    private static readonly (string First, string Second)[] BaseEdges =
    {
        ("Left_Base", "Left_Mid"),
        ("Left_Mid", "Left_Top"),
        ("Left_Top", "Beam_Left"),
        ("Beam_Left", "Beam_Center"),
        ("Beam_Center", "Beam_Right"),
        ("Beam_Right", "Right_Top"),
        ("Right_Top", "Right_Mid"),
        ("Right_Mid", "Right_Base"),
        ("Left_Top", "Roof_Left"),
        ("Roof_Left", "Roof_Right"),
        ("Roof_Right", "Beam_Left")
    };

    [MenuItem("DemolitionLab/Build M4 Scene")]
    public static void BuildScene()
    {
        string previousScenePath = SceneManager.GetActiveScene().path;
        Scene scene = EditorSceneManager.OpenScene(SourceScenePath, OpenSceneMode.Single);
        if (!EditorSceneManager.SaveScene(scene, ScenePath))
        {
            throw new UnityException($"Failed to create M4 scene at {ScenePath}.");
        }

        StructureManager sourceManager = Object.FindAnyObjectByType<StructureManager>();
        if (sourceManager == null)
        {
            throw new UnityException("M4 source scene is missing StructureManager.");
        }

        GameObject weakRoot = sourceManager.gameObject;
        GameObject reinforcedRoot = Object.Instantiate(weakRoot);
        weakRoot.transform.position = new Vector3(-5f, 0f, 0f);
        reinforcedRoot.transform.position = new Vector3(5f, 0f, 0f);

        ConfigureStructure(
            weakRoot,
            "Weak",
            WeakGravityCapacity,
            WeakImpactCapacity,
            false);
        ConfigureStructure(
            reinforcedRoot,
            "Reinforced",
            ReinforcedGravityCapacity,
            ReinforcedImpactCapacity,
            true);
        ConfigureCameraAndLauncher();

        EditorSceneManager.SaveScene(scene);
        AddSceneToBuildSettings();
        AssetDatabase.SaveAssets();

        if (!string.IsNullOrEmpty(previousScenePath) && previousScenePath != ScenePath)
        {
            EditorSceneManager.OpenScene(previousScenePath, OpenSceneMode.Single);
        }

        Debug.Log($"Built Milestone 4 scene at {ScenePath}");
    }

    [MenuItem("DemolitionLab/Validate M4 Scene")]
    public static void ValidateScene()
    {
        ValidateLoadFormula();
        ValidateInitialStructures();
        ValidateWeakNormalImpactBreak();
        ValidateStrongWeakImpactBreak();
        ValidateDistantImpactIsolation();
        ValidateReinforcedNormalImpactSurvival();
        ValidateGravityContribution();
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Debug.Log(
            "M4.1 validation passed: realistic impact calibration, normalized gravity contribution, "
            + "local break isolation, graph reevaluation, and reinforcement verified.");
    }

    private static void ValidateLoadFormula()
    {
        float shortLightLoad = StructuralLoadEvaluator.CalculateLoad(2f, 1f, 9.81f, 0.5f);
        float longHeavyLoad = StructuralLoadEvaluator.CalculateLoad(5f, 3f, 9.81f, 0.5f);
        if (longHeavyLoad <= shortLightLoad)
        {
            throw new UnityException("Long/heavy cantilever load must exceed short/light cantilever load.");
        }
    }

    private static void ValidateInitialStructures()
    {
        ValidationContext context = LoadAndInitializeScene();
        foreach (StructureContext structure in new[] { context.Weak, context.Reinforced })
        {
            bool isWeak = structure.Manager.name.StartsWith("Weak");
            float expectedGravityCapacity = isWeak ? WeakGravityCapacity : ReinforcedGravityCapacity;
            float expectedImpactCapacity = isWeak ? WeakImpactCapacity : ReinforcedImpactCapacity;
            foreach (StructuralBlock block in structure.Blocks.Values)
            {
                if (!block.ParticipatesInStructure || !block.GetComponent<Rigidbody>().isKinematic)
                {
                    throw new UnityException($"Initial M4 block '{block.NodeId}' is not statically supported.");
                }
            }

            foreach (StructuralConnection connection in structure.Connections)
            {
                if (!connection.IsActive)
                {
                    throw new UnityException("The symmetric M4 structure broke a connection during initialization.");
                }

                if (!Mathf.Approximately(connection.GravityCapacity, expectedGravityCapacity)
                    || !Mathf.Approximately(connection.ImpactCapacity, expectedImpactCapacity))
                {
                    throw new UnityException(
                        $"M4 connection capacity mismatch on '{structure.Manager.name}'.");
                }
            }
        }
    }

    private static void ValidateWeakNormalImpactBreak()
    {
        ValidationContext context = LoadAndInitializeScene();
        StructureContext weak = context.Weak;
        StructuralConnection critical = FindConnection(weak, "Right_Mid", "Right_Base");
        StructuralConnection unrelated = FindConnection(weak, "Roof_Left", "Roof_Right");

        weak.Blocks["Right_Base"].HandleProjectileImpact(new Vector3(30f, 0f, 0f), Vector3.zero);

        if (critical.IsActive)
        {
            throw new UnityException("Weak direct connection did not break under a representative 30 N·s impact.");
        }

        if (!unrelated.IsActive)
        {
            throw new UnityException("A distant unrelated connection broke from local impact distribution.");
        }

        if (!weak.Blocks["Right_Mid"].IsReleased)
        {
            throw new UnityException("Breaking the weak support connection did not trigger structural reevaluation.");
        }

        AssertReleasedBlocksExcluded(weak.Manager);
        foreach (StructuralConnection connection in context.Reinforced.Connections)
        {
            if (!connection.IsActive)
            {
                throw new UnityException("Impact on the weak structure affected an unrelated reinforced connection.");
            }
        }
    }

    private static void ValidateStrongWeakImpactBreak()
    {
        ValidationContext context = LoadAndInitializeScene();
        StructureContext weak = context.Weak;
        StructuralConnection localRoofConnection = FindConnection(weak, "Roof_Left", "Roof_Right");

        weak.Blocks["Roof_Right"].HandleProjectileImpact(new Vector3(35f, 0f, 0f), Vector3.zero);

        if (localRoofConnection.IsActive)
        {
            throw new UnityException("Weak direct connection did not break under a strong 35 N·s gameplay impact.");
        }
    }

    private static void ValidateReinforcedNormalImpactSurvival()
    {
        ValidationContext context = LoadAndInitializeScene();
        StructureContext reinforced = context.Reinforced;
        StructuralConnection primary = FindConnection(reinforced, "Right_Mid", "Right_Base");
        StructuralConnection strongBackup = FindConnection(reinforced, "Right_Base", "Right_Brace");

        reinforced.Blocks["Right_Base"].HandleProjectileImpact(new Vector3(30f, 0f, 0f), Vector3.zero);

        if (!primary.IsActive || !strongBackup.IsActive)
        {
            throw new UnityException("A reinforced connection did not survive the representative 30 N·s impact.");
        }

        foreach (StructuralBlock block in reinforced.Blocks.Values)
        {
            if (!block.ParticipatesInStructure || !block.GetComponent<Rigidbody>().isKinematic)
            {
                throw new UnityException(
                    $"Reinforced alternate support path failed to preserve '{block.NodeId}'.");
            }
        }
    }

    private static void ValidateGravityContribution()
    {
        ValidationContext context = LoadAndInitializeScene();
        StructuralConnection weak = FindConnection(context.Weak, "Beam_Left", "Beam_Center");
        StructuralConnection reinforced = FindConnection(context.Reinforced, "Beam_Left", "Beam_Center");
        const float representativeGravityLoad = 73.575f;
        const float moderateImpact = 12f;

        if (weak.CalculateFailureScore(0f, moderateImpact) > 1f
            || weak.CalculateFailureScore(representativeGravityLoad, moderateImpact) <= 1f)
        {
            throw new UnityException("Weak gravity utilization did not reduce the remaining impact margin.");
        }

        if (reinforced.CalculateFailureScore(representativeGravityLoad, moderateImpact) > 1f)
        {
            throw new UnityException("Reinforced connection failed under moderate combined utilization.");
        }

        const float significantGravityLoad = 161.865f;
        const float strongImpact = 20f;
        if (reinforced.CalculateFailureScore(significantGravityLoad, strongImpact) <= 1f)
        {
            throw new UnityException("Reinforced connection did not fail under significant combined utilization.");
        }
    }

    private static void ValidateDistantImpactIsolation()
    {
        ValidationContext context = LoadAndInitializeScene();
        StructureContext weak = context.Weak;
        StructuralConnection critical = FindConnection(weak, "Right_Mid", "Right_Base");
        StructuralConnection localRoofConnection = FindConnection(weak, "Roof_Left", "Roof_Right");

        weak.Blocks["Roof_Right"].HandleProjectileImpact(new Vector3(35f, 0f, 0f), Vector3.zero);

        if (localRoofConnection.IsActive)
        {
            throw new UnityException("A strong direct hit did not break the local weak roof connection.");
        }

        if (!critical.IsActive)
        {
            throw new UnityException("A distant roof impact incorrectly broke the critical base connection.");
        }
    }

    private static void ConfigureStructure(
        GameObject root,
        string prefix,
        float gravityCapacity,
        float impactCapacity,
        bool addReinforcement)
    {
        root.name = $"{prefix} Structure";
        StructureManager manager = root.GetComponent<StructureManager>();
        if (manager == null)
        {
            throw new UnityException($"{prefix} structure is missing StructureManager.");
        }

        var blocks = new Dictionary<string, StructuralBlock>();
        foreach (StructuralBlock block in root.GetComponentsInChildren<StructuralBlock>())
        {
            string originalId = block.NodeId;
            blocks.Add(originalId, block);
            SetBlockId(block, $"{prefix}_{originalId}");
        }

        if (addReinforcement)
        {
            StructuralBlock brace = CreateBrace(blocks["Right_Mid"], root.transform, prefix);
            blocks.Add("Right_Brace", brace);
        }

        var connections = new List<StructuralConnection>();
        foreach ((string first, string second) in BaseEdges)
        {
            connections.Add(CreateConnection(
                root.transform,
                blocks[first],
                blocks[second],
                gravityCapacity,
                impactCapacity));
        }

        if (addReinforcement)
        {
            connections.Add(CreateConnection(
                root.transform,
                blocks["Right_Base"],
                blocks["Right_Brace"],
                ReinforcedGravityCapacity,
                ReinforcedImpactCapacity));
            connections.Add(CreateConnection(
                root.transform,
                blocks["Right_Brace"],
                blocks["Beam_Right"],
                ReinforcedGravityCapacity,
                ReinforcedImpactCapacity));
            connections.Add(CreateConnection(
                root.transform,
                blocks["Right_Brace"],
                blocks["Right_Mid"],
                ReinforcedGravityCapacity,
                ReinforcedImpactCapacity));
        }

        StructuralLoadEvaluator loadEvaluator = root.GetComponent<StructuralLoadEvaluator>();
        if (loadEvaluator == null)
        {
            loadEvaluator = root.AddComponent<StructuralLoadEvaluator>();
        }

        var serializedManager = new SerializedObject(manager);
        AssignObjectList(serializedManager.FindProperty("blocks"), new List<StructuralBlock>(blocks.Values));
        AssignObjectList(serializedManager.FindProperty("connections"), connections);
        serializedManager.FindProperty("loadEvaluator").objectReferenceValue = loadEvaluator;
        serializedManager.FindProperty("destroyImpactedBlocks").boolValue = false;
        serializedManager.ApplyModifiedPropertiesWithoutUndo();
    }

    private static StructuralBlock CreateBrace(StructuralBlock source, Transform parent, string prefix)
    {
        GameObject braceObject = Object.Instantiate(source.gameObject, parent);
        braceObject.name = $"{prefix}_Right_Brace";
        braceObject.transform.localPosition = new Vector3(1f, 1.5f, 2f);
        StructuralBlock brace = braceObject.GetComponent<StructuralBlock>();
        var serializedBrace = new SerializedObject(brace);
        serializedBrace.FindProperty("nodeId").stringValue = $"{prefix}_Right_Brace";
        serializedBrace.FindProperty("isGroundAnchor").boolValue = false;
        serializedBrace.FindProperty("neighbors").arraySize = 0;
        serializedBrace.ApplyModifiedPropertiesWithoutUndo();
        return brace;
    }

    private static StructuralConnection CreateConnection(
        Transform parent,
        StructuralBlock first,
        StructuralBlock second,
        float gravityCapacity,
        float impactCapacity)
    {
        GameObject connectionObject = new($"Connection_{first.NodeId}_{second.NodeId}");
        connectionObject.transform.SetParent(parent, false);
        StructuralConnection connection = connectionObject.AddComponent<StructuralConnection>();
        var serializedConnection = new SerializedObject(connection);
        serializedConnection.FindProperty("blockA").objectReferenceValue = first;
        serializedConnection.FindProperty("blockB").objectReferenceValue = second;
        serializedConnection.FindProperty("gravityCapacity").floatValue = gravityCapacity;
        serializedConnection.FindProperty("impactCapacity").floatValue = impactCapacity;
        serializedConnection.FindProperty("isActive").boolValue = true;
        serializedConnection.ApplyModifiedPropertiesWithoutUndo();
        return connection;
    }

    private static void SetBlockId(StructuralBlock block, string nodeId)
    {
        block.name = nodeId;
        var serializedBlock = new SerializedObject(block);
        serializedBlock.FindProperty("nodeId").stringValue = nodeId;
        serializedBlock.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AssignObjectList<T>(SerializedProperty property, IReadOnlyList<T> values)
        where T : Object
    {
        property.arraySize = values.Count;
        for (int index = 0; index < values.Count; index++)
        {
            property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
        }
    }

    private static void ConfigureCameraAndLauncher()
    {
        Camera camera = Camera.main;
        if (camera != null)
        {
            camera.transform.position = new Vector3(0f, 7f, -22f);
            camera.transform.LookAt(new Vector3(0f, 2f, 2f));
            camera.fieldOfView = 52f;
        }

        LauncherController launcher = Object.FindAnyObjectByType<LauncherController>();
        if (launcher != null)
        {
            var serializedLauncher = new SerializedObject(launcher);
            serializedLauncher.FindProperty("yawLimits").vector2Value = new Vector2(-40f, 40f);
            serializedLauncher.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static ValidationContext LoadAndInitializeScene()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        StructureManager[] managers = Object.FindObjectsByType<StructureManager>();
        if (managers.Length != 2)
        {
            throw new UnityException($"M4 scene expected 2 structures but found {managers.Length}.");
        }

        MethodInfo awake = typeof(StructureManager).GetMethod(
            "Awake",
            BindingFlags.Instance | BindingFlags.NonPublic);
        StructureContext weak = default;
        StructureContext reinforced = default;
        foreach (StructureManager manager in managers)
        {
            awake?.Invoke(manager, null);
            StructureContext context = CreateContext(manager);
            if (manager.name.StartsWith("Weak"))
            {
                weak = context;
            }
            else if (manager.name.StartsWith("Reinforced"))
            {
                reinforced = context;
            }
        }

        if (weak.Manager == null || reinforced.Manager == null
            || weak.Blocks.Count != 11 || reinforced.Blocks.Count != 12)
        {
            throw new UnityException("M4 scene must contain 11 weak and 12 reinforced blocks.");
        }

        return new ValidationContext(weak, reinforced);
    }

    private static StructureContext CreateContext(StructureManager manager)
    {
        string prefix = manager.name.StartsWith("Weak") ? "Weak_" : "Reinforced_";
        var blocks = new Dictionary<string, StructuralBlock>();
        foreach (StructuralBlock block in manager.GetComponentsInChildren<StructuralBlock>())
        {
            blocks.Add(block.NodeId.Substring(prefix.Length), block);
        }

        var connections = new List<StructuralConnection>(
            manager.GetComponentsInChildren<StructuralConnection>());
        return new StructureContext(manager, blocks, connections);
    }

    private static StructuralConnection FindConnection(
        StructureContext context,
        string firstId,
        string secondId)
    {
        StructuralBlock first = context.Blocks[firstId];
        StructuralBlock second = context.Blocks[secondId];
        foreach (StructuralConnection connection in context.Connections)
        {
            if (connection.Connects(first) && connection.Connects(second))
            {
                return connection;
            }
        }

        throw new UnityException($"Missing M4 connection: {firstId} <-> {secondId}");
    }

    private static void AssertReleasedBlocksExcluded(StructureManager manager)
    {
        MethodInfo findComponents = typeof(StructureManager).GetMethod(
            "FindActiveComponents",
            BindingFlags.Instance | BindingFlags.NonPublic);
        var components = findComponents?.Invoke(manager, null) as List<List<StructuralBlock>>;
        if (components == null)
        {
            throw new UnityException("Could not inspect M4 static components.");
        }

        foreach (List<StructuralBlock> component in components)
        {
            foreach (StructuralBlock block in component)
            {
                if (block.IsReleased)
                {
                    throw new UnityException($"Released node '{block.NodeId}' remained in static evaluation.");
                }
            }
        }
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
        public ValidationContext(StructureContext weak, StructureContext reinforced)
        {
            Weak = weak;
            Reinforced = reinforced;
        }

        public StructureContext Weak { get; }
        public StructureContext Reinforced { get; }
    }

    private readonly struct StructureContext
    {
        public StructureContext(
            StructureManager manager,
            Dictionary<string, StructuralBlock> blocks,
            List<StructuralConnection> connections)
        {
            Manager = manager;
            Blocks = blocks;
            Connections = connections;
        }

        public StructureManager Manager { get; }
        public Dictionary<string, StructuralBlock> Blocks { get; }
        public List<StructuralConnection> Connections { get; }
    }
}
