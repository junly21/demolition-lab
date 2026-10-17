using System.Collections.Generic;
using System.Reflection;
using DemolitionLab;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class M5SceneBuilder
{
    private const string SourceScenePath = "Assets/Scenes/M4_BreakableConnections.unity";
    private const string ScenePath = "Assets/Scenes/M5_PhysicalConnections.unity";
    private const float WeakGravityCapacity = 120f;
    private const float WeakImpactCapacity = 28f;
    private const float ReinforcedGravityCapacity = 240f;
    private const float ReinforcedImpactCapacity = 60f;
    private const float WeakBreakForce = 450f;
    private const float WeakBreakTorque = 180f;
    private const float ReinforcedBreakForce = 1200f;
    private const float ReinforcedBreakTorque = 500f;
    private const float AngularLimit = 0.5f;
    private const float RepresentativeGameplayImpulse = 15f;
    private const float MediocreGameplayImpulse = 8f;
    private const int ExpectedBlockCount = 45;
    private const int ExpectedConnectionCount = 50;

    [MenuItem("DemolitionLab/Build M5 Scene")]
    public static void BuildScene()
    {
        string previousScenePath = SceneManager.GetActiveScene().path;
        Scene scene = EditorSceneManager.OpenScene(SourceScenePath, OpenSceneMode.Single);
        if (!EditorSceneManager.SaveScene(scene, ScenePath))
        {
            throw new UnityException($"Failed to create M5 scene at {ScenePath}.");
        }

        StructuralBlock materialSource = Object.FindAnyObjectByType<StructuralBlock>();
        if (materialSource == null)
        {
            throw new UnityException("M5 source scene has no StructuralBlock material source.");
        }

        BlockMaterials materials = ReadBlockMaterials(materialSource);
        foreach (StructureManager manager in Object.FindObjectsByType<StructureManager>())
        {
            Object.DestroyImmediate(manager.gameObject);
        }

        GameObject root = new("M5 Physical Frame");
        StructureManager structureManager = root.AddComponent<StructureManager>();
        StructuralStabilityEvaluator stabilityEvaluator = root.AddComponent<StructuralStabilityEvaluator>();
        LocalSupportEvaluator localSupportEvaluator = root.AddComponent<LocalSupportEvaluator>();
        StructuralLoadEvaluator loadEvaluator = root.AddComponent<StructuralLoadEvaluator>();

        Dictionary<string, StructuralBlock> blocks = BuildFrame(root.transform, materials);
        List<StructuralConnection> connections = BuildConnections(blocks);
        ConfigureManager(
            structureManager,
            stabilityEvaluator,
            localSupportEvaluator,
            loadEvaluator,
            blocks,
            connections);
        ConfigureCameraAndLauncher();

        EditorSceneManager.SaveScene(scene);
        AddSceneToBuildSettings();
        AssetDatabase.SaveAssets();

        if (!string.IsNullOrEmpty(previousScenePath) && previousScenePath != ScenePath)
        {
            EditorSceneManager.OpenScene(previousScenePath, OpenSceneMode.Single);
        }

        Debug.Log($"Built Milestone 5 scene at {ScenePath}");
    }

    [MenuItem("DemolitionLab/Validate M5 Scene")]
    public static void ValidateScene()
    {
        ValidateAuthoredConfiguration();
        ValidateRuntimeInitialization();
        ValidateIdlePhysics();
        ValidateMediocreWeakImpactSurvival();
        ValidateWeakImpactResponse();
        ValidateReinforcedImpactResponse();
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Debug.Log(
            "M5 validation passed: configuration, idle stability, local force propagation, "
            + "weak physical failure, reinforced survival, and logical synchronization verified.");
    }

    private static Dictionary<string, StructuralBlock> BuildFrame(
        Transform root,
        BlockMaterials materials)
    {
        var blocks = new Dictionary<string, StructuralBlock>();
        float[] columnX = { -6f, -2f, 2f, 6f };
        for (int column = 0; column < columnX.Length; column++)
        {
            for (int level = 0; level < 5; level++)
            {
                AddBlock(
                    blocks,
                    root,
                    $"Column_{column}_Level_{level}",
                    new Vector3(columnX[column], 0.5f + level, 3f),
                    level == 0,
                    materials);
            }
        }

        AddBeamRow(blocks, root, materials, "Top", 4.5f, -5f, -3f);
        AddBeamRow(blocks, root, materials, "Top", 4.5f, -1f, 1f);
        AddBeamRow(blocks, root, materials, "Top", 4.5f, 3f, 5f);
        AddBeamRow(blocks, root, materials, "Mid", 2.5f, -5f, -3f);
        AddBeamRow(blocks, root, materials, "Mid", 2.5f, 3f, 5f);

        AddBraceBlocks(blocks, root, materials, "Weak", -5f, -4f, -3f);
        AddBraceBlocks(blocks, root, materials, "Reinforced", 3f, 4f, 5f);
        return blocks;
    }

    private static void AddBeamRow(
        IDictionary<string, StructuralBlock> blocks,
        Transform root,
        BlockMaterials materials,
        string row,
        float y,
        float minX,
        float maxX)
    {
        for (float x = minX; x <= maxX; x++)
        {
            AddBlock(
                blocks,
                root,
                $"{row}_Beam_{x:0}",
                new Vector3(x, y, 3f),
                false,
                materials);
        }
    }

    private static void AddBraceBlocks(
        IDictionary<string, StructuralBlock> blocks,
        Transform root,
        BlockMaterials materials,
        string prefix,
        float outerX,
        float centerX,
        float innerX)
    {
        AddBlock(blocks, root, $"{prefix}_Brace_Outer_Low", new Vector3(outerX, 0.5f, 3f), false, materials);
        AddBlock(blocks, root, $"{prefix}_Brace_Outer_High", new Vector3(outerX, 1.5f, 3f), false, materials);
        AddBlock(blocks, root, $"{prefix}_Brace_Center", new Vector3(centerX, 1.5f, 3f), false, materials);
        AddBlock(blocks, root, $"{prefix}_Brace_Inner_High", new Vector3(innerX, 1.5f, 3f), false, materials);
        AddBlock(blocks, root, $"{prefix}_Brace_Inner_Low", new Vector3(innerX, 0.5f, 3f), false, materials);
    }

    private static void AddBlock(
        IDictionary<string, StructuralBlock> blocks,
        Transform root,
        string nodeId,
        Vector3 position,
        bool isGroundAnchor,
        BlockMaterials materials)
    {
        GameObject blockObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        blockObject.name = nodeId;
        blockObject.tag = "Destructible";
        blockObject.transform.SetParent(root, false);
        blockObject.transform.position = position;

        Rigidbody body = blockObject.AddComponent<Rigidbody>();
        body.mass = isGroundAnchor ? 4f : 1f;
        body.useGravity = true;
        body.isKinematic = isGroundAnchor;
        body.linearDamping = 0.12f;
        body.angularDamping = 0.35f;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        body.solverIterations = 12;
        body.solverVelocityIterations = 4;
        body.maxAngularVelocity = 20f;

        StructuralBlock block = blockObject.AddComponent<StructuralBlock>();
        var serializedBlock = new SerializedObject(block);
        serializedBlock.FindProperty("nodeId").stringValue = nodeId;
        serializedBlock.FindProperty("isGroundAnchor").boolValue = isGroundAnchor;
        serializedBlock.FindProperty("neighbors").arraySize = 0;
        serializedBlock.FindProperty("supportedMaterial").objectReferenceValue = materials.Supported;
        serializedBlock.FindProperty("unsupportedMaterial").objectReferenceValue = materials.Unsupported;
        serializedBlock.FindProperty("disconnectedMaterial").objectReferenceValue = materials.Disconnected;
        serializedBlock.FindProperty("destroyedMaterial").objectReferenceValue = materials.Destroyed;
        serializedBlock.ApplyModifiedPropertiesWithoutUndo();
        blocks.Add(nodeId, block);
    }

    private static List<StructuralConnection> BuildConnections(
        IReadOnlyDictionary<string, StructuralBlock> blocks)
    {
        var connections = new List<StructuralConnection>();
        for (int column = 0; column < 4; column++)
        {
            for (int level = 1; level < 5; level++)
            {
                AddConnection(
                    connections,
                    blocks[$"Column_{column}_Level_{level}"],
                    blocks[$"Column_{column}_Level_{level - 1}"]);
            }
        }

        ConnectHorizontalChain(connections, blocks, new[]
        {
            "Column_0_Level_4", "Top_Beam_-5", "Top_Beam_-4", "Top_Beam_-3", "Column_1_Level_4"
        });
        ConnectHorizontalChain(connections, blocks, new[]
        {
            "Column_1_Level_4", "Top_Beam_-1", "Top_Beam_0", "Top_Beam_1", "Column_2_Level_4"
        });
        ConnectHorizontalChain(connections, blocks, new[]
        {
            "Column_2_Level_4", "Top_Beam_3", "Top_Beam_4", "Top_Beam_5", "Column_3_Level_4"
        });
        ConnectHorizontalChain(connections, blocks, new[]
        {
            "Column_0_Level_2", "Mid_Beam_-5", "Mid_Beam_-4", "Mid_Beam_-3", "Column_1_Level_2"
        });
        ConnectHorizontalChain(connections, blocks, new[]
        {
            "Column_2_Level_2", "Mid_Beam_3", "Mid_Beam_4", "Mid_Beam_5", "Column_3_Level_2"
        });

        ConnectBrace(
            connections,
            blocks,
            "Weak",
            "Column_0_Level_0",
            "Column_1_Level_0",
            "Mid_Beam_-4");
        ConnectBrace(
            connections,
            blocks,
            "Reinforced",
            "Column_2_Level_0",
            "Column_3_Level_0",
            "Mid_Beam_4");
        return connections;
    }

    private static void ConnectHorizontalChain(
        ICollection<StructuralConnection> connections,
        IReadOnlyDictionary<string, StructuralBlock> blocks,
        IReadOnlyList<string> nodeIds)
    {
        for (int index = 1; index < nodeIds.Count; index++)
        {
            AddConnection(connections, blocks[nodeIds[index]], blocks[nodeIds[index - 1]]);
        }
    }

    private static void ConnectBrace(
        ICollection<StructuralConnection> connections,
        IReadOnlyDictionary<string, StructuralBlock> blocks,
        string prefix,
        string outerAnchor,
        string innerAnchor,
        string centerSupport)
    {
        ConnectHorizontalChain(connections, blocks, new[]
        {
            outerAnchor,
            $"{prefix}_Brace_Outer_Low",
            $"{prefix}_Brace_Outer_High",
            $"{prefix}_Brace_Center",
            centerSupport
        });
        ConnectHorizontalChain(connections, blocks, new[]
        {
            innerAnchor,
            $"{prefix}_Brace_Inner_Low",
            $"{prefix}_Brace_Inner_High",
            $"{prefix}_Brace_Center"
        });
    }

    private static void AddConnection(
        ICollection<StructuralConnection> connections,
        StructuralBlock owner,
        StructuralBlock connected)
    {
        float midpointX = (owner.transform.position.x + connected.transform.position.x) * 0.5f;
        StructuralConnectionType type = midpointX < 0f
            ? StructuralConnectionType.Weak
            : StructuralConnectionType.Reinforced;
        float gravityCapacity = type == StructuralConnectionType.Weak
            ? WeakGravityCapacity
            : ReinforcedGravityCapacity;
        float impactCapacity = type == StructuralConnectionType.Weak
            ? WeakImpactCapacity
            : ReinforcedImpactCapacity;
        float breakForce = type == StructuralConnectionType.Weak
            ? WeakBreakForce
            : ReinforcedBreakForce;
        float breakTorque = type == StructuralConnectionType.Weak
            ? WeakBreakTorque
            : ReinforcedBreakTorque;

        ConfigurableJoint joint = owner.gameObject.AddComponent<ConfigurableJoint>();
        joint.connectedBody = connected.GetComponent<Rigidbody>();
        joint.autoConfigureConnectedAnchor = true;
        joint.xMotion = ConfigurableJointMotion.Locked;
        joint.yMotion = ConfigurableJointMotion.Locked;
        joint.zMotion = ConfigurableJointMotion.Locked;
        joint.angularXMotion = ConfigurableJointMotion.Limited;
        joint.angularYMotion = ConfigurableJointMotion.Limited;
        joint.angularZMotion = ConfigurableJointMotion.Limited;
        joint.lowAngularXLimit = new SoftJointLimit { limit = -AngularLimit };
        joint.highAngularXLimit = new SoftJointLimit { limit = AngularLimit };
        joint.angularYLimit = new SoftJointLimit { limit = AngularLimit };
        joint.angularZLimit = new SoftJointLimit { limit = AngularLimit };
        joint.angularXLimitSpring = new SoftJointLimitSpring { spring = 5000f, damper = 300f };
        joint.angularYZLimitSpring = new SoftJointLimitSpring { spring = 5000f, damper = 300f };
        joint.rotationDriveMode = RotationDriveMode.Slerp;
        joint.slerpDrive = new JointDrive
        {
            positionSpring = 4000f,
            positionDamper = 250f,
            maximumForce = 10000f
        };
        joint.projectionMode = JointProjectionMode.PositionAndRotation;
        joint.projectionDistance = 0.01f;
        joint.projectionAngle = 0.5f;
        joint.enableCollision = false;
        joint.enablePreprocessing = true;
        joint.breakForce = breakForce;
        joint.breakTorque = breakTorque;

        StructuralConnection connection = owner.gameObject.AddComponent<StructuralConnection>();
        var serializedConnection = new SerializedObject(connection);
        serializedConnection.FindProperty("blockA").objectReferenceValue = owner;
        serializedConnection.FindProperty("blockB").objectReferenceValue = connected;
        serializedConnection.FindProperty("gravityCapacity").floatValue = gravityCapacity;
        serializedConnection.FindProperty("impactCapacity").floatValue = impactCapacity;
        serializedConnection.FindProperty("isActive").boolValue = true;
        serializedConnection.FindProperty("connectionType").enumValueIndex = (int)type;
        serializedConnection.FindProperty("physicalJoint").objectReferenceValue = joint;
        serializedConnection.FindProperty("physicalBreakForce").floatValue = breakForce;
        serializedConnection.FindProperty("physicalBreakTorque").floatValue = breakTorque;
        serializedConnection.ApplyModifiedPropertiesWithoutUndo();
        connections.Add(connection);
    }

    private static void ConfigureManager(
        StructureManager manager,
        StructuralStabilityEvaluator stabilityEvaluator,
        LocalSupportEvaluator localSupportEvaluator,
        StructuralLoadEvaluator loadEvaluator,
        IReadOnlyDictionary<string, StructuralBlock> blocks,
        IReadOnlyList<StructuralConnection> connections)
    {
        var serializedManager = new SerializedObject(manager);
        AssignObjectList(serializedManager.FindProperty("blocks"), new List<StructuralBlock>(blocks.Values));
        AssignObjectList(serializedManager.FindProperty("connections"), connections);
        serializedManager.FindProperty("stabilityEvaluator").objectReferenceValue = stabilityEvaluator;
        serializedManager.FindProperty("localSupportEvaluator").objectReferenceValue = localSupportEvaluator;
        serializedManager.FindProperty("loadEvaluator").objectReferenceValue = loadEvaluator;
        serializedManager.FindProperty("keepDestroyedBlocksAsDebris").boolValue = true;
        serializedManager.FindProperty("destroyImpactedBlocks").boolValue = false;
        serializedManager.FindProperty("usePhysicalJoints").boolValue = true;
        serializedManager.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureCameraAndLauncher()
    {
        Camera camera = Camera.main;
        if (camera != null)
        {
            camera.transform.position = new Vector3(0f, 8f, -23f);
            camera.transform.LookAt(new Vector3(0f, 2.5f, 3f));
            camera.fieldOfView = 50f;
        }

        LauncherController launcher = Object.FindAnyObjectByType<LauncherController>();
        if (launcher != null)
        {
            var serializedLauncher = new SerializedObject(launcher);
            serializedLauncher.FindProperty("yawLimits").vector2Value = new Vector2(-35f, 35f);
            serializedLauncher.FindProperty("pitchLimits").vector2Value = new Vector2(-5f, 24f);
            serializedLauncher.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void ValidateAuthoredConfiguration()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        StructuralBlock[] blocks = Object.FindObjectsByType<StructuralBlock>();
        StructuralConnection[] connections = Object.FindObjectsByType<StructuralConnection>();
        StructureManager manager = Object.FindAnyObjectByType<StructureManager>();
        if (manager == null
            || blocks.Length != ExpectedBlockCount
            || connections.Length != ExpectedConnectionCount)
        {
            throw new UnityException(
                $"M5 expected {ExpectedBlockCount} blocks and {ExpectedConnectionCount} connections.");
        }

        int anchors = 0;
        int weakConnections = 0;
        int reinforcedConnections = 0;
        foreach (StructuralBlock block in blocks)
        {
            if (block.IsGroundAnchor)
            {
                anchors++;
            }

            Rigidbody body = block.GetComponent<Rigidbody>();
            if (body == null || body.mass <= 0f)
            {
                throw new UnityException($"M5 block '{block.NodeId}' has invalid Rigidbody configuration.");
            }
        }

        foreach (StructuralConnection connection in connections)
        {
            if (!connection.IsActive || !connection.HasPhysicalJoint)
            {
                throw new UnityException("M5 active logical connection is missing its physical joint.");
            }

            if (connection.ConnectionType == StructuralConnectionType.Weak)
            {
                weakConnections++;
                AssertConnectionProfile(
                    connection,
                    WeakGravityCapacity,
                    WeakImpactCapacity,
                    WeakBreakForce,
                    WeakBreakTorque);
            }
            else
            {
                reinforcedConnections++;
                AssertConnectionProfile(
                    connection,
                    ReinforcedGravityCapacity,
                    ReinforcedImpactCapacity,
                    ReinforcedBreakForce,
                    ReinforcedBreakTorque);
            }
        }

        if (anchors != 4 || weakConnections == 0 || reinforcedConnections == 0)
        {
            throw new UnityException(
                $"M5 expected 4 anchors and both joint profiles, found anchors={anchors}, "
                + $"weak={weakConnections}, reinforced={reinforcedConnections}.");
        }

        ValidateNoInitialPenetration(blocks);
    }

    private static void ValidateRuntimeInitialization()
    {
        RuntimeContext context = LoadAndInitializeRuntimeScene();

        foreach (StructuralBlock block in context.Blocks.Values)
        {
            bool expectedKinematic = block.IsGroundAnchor;
            if (block.GetComponent<Rigidbody>().isKinematic != expectedKinematic)
            {
                throw new UnityException(
                    $"M5 block '{block.NodeId}' kinematic state does not match its anchor role.");
            }

            if (!block.ParticipatesInStructure || !block.IsSupported)
            {
                throw new UnityException($"M5 block '{block.NodeId}' failed initial logical support.");
            }
        }
    }

    private static void ValidateIdlePhysics()
    {
        RuntimeContext context = LoadAndInitializeRuntimeScene();
        var initialPositions = new Dictionary<StructuralBlock, Vector3>();
        foreach (StructuralBlock block in context.Blocks.Values)
        {
            initialPositions.Add(block, block.transform.position);
        }

        SimulatePhysics(context.Connections, 500);

        float maxSpeed = 0f;
        float maxDrift = 0f;
        foreach (StructuralBlock block in context.Blocks.Values)
        {
            Rigidbody body = block.GetComponent<Rigidbody>();
            maxSpeed = Mathf.Max(maxSpeed, body.linearVelocity.magnitude);
            maxDrift = Mathf.Max(maxDrift, Vector3.Distance(initialPositions[block], body.position));
        }

        if (maxSpeed > 0.35f || maxDrift > 0.12f)
        {
            throw new UnityException(
                $"M5 idle frame is unstable: maxSpeed={maxSpeed:0.000} m/s, "
                + $"maxDrift={maxDrift:0.000} m.");
        }

        AssertNoBrokenConnections(context.Connections, "M5 idle simulation broke a joint.");
    }

    private static void ValidateMediocreWeakImpactSurvival()
    {
        RuntimeContext context = LoadAndInitializeRuntimeScene();
        StructuralBlock target = context.Blocks["Column_0_Level_1"];

        target.GetComponent<Rigidbody>().AddForceAtPosition(
            Vector3.forward * MediocreGameplayImpulse,
            target.transform.position,
            ForceMode.Impulse);
        SimulatePhysics(context.Connections, 30);

        if (CountBrokenConnections(context.Connections, StructuralConnectionType.Weak) > 0)
        {
            throw new UnityException("M5.1 mediocre centered Weak impact unexpectedly broke a joint.");
        }
    }

    private static void ValidateWeakImpactResponse()
    {
        RuntimeContext context = LoadAndInitializeRuntimeScene();
        StructuralBlock target = context.Blocks["Column_0_Level_1"];
        StructuralBlock neighbor = context.Blocks["Column_0_Level_2"];
        Vector3 neighborStart = neighbor.transform.position;

        target.GetComponent<Rigidbody>().AddForceAtPosition(
            Vector3.forward * RepresentativeGameplayImpulse,
            target.transform.position + Vector3.up * 0.4f,
            ForceMode.Impulse);
        SimulatePhysics(context.Connections, 30);

        if (Vector3.Distance(neighborStart, neighbor.transform.position) < 0.002f)
        {
            throw new UnityException("M5 weak impact did not propagate visible motion to a connected neighbor.");
        }

        int brokenWeak = CountBrokenConnections(
            context.Connections,
            StructuralConnectionType.Weak);
        if (brokenWeak == 0)
        {
            throw new UnityException("M5 representative 15 N·s weak impact did not break a weak physical joint.");
        }

        foreach (StructuralConnection connection in context.Connections)
        {
            if (!connection.HasPhysicalJoint && connection.IsActive)
            {
                throw new UnityException("A broken M5 physical joint remained logically active.");
            }
        }
    }

    private static void ValidateReinforcedImpactResponse()
    {
        RuntimeContext context = LoadAndInitializeRuntimeScene();
        StructuralBlock target = context.Blocks["Column_3_Level_1"];
        StructuralBlock neighbor = context.Blocks["Column_3_Level_2"];
        Vector3 neighborStart = neighbor.transform.position;

        target.GetComponent<Rigidbody>().AddForceAtPosition(
            Vector3.forward * RepresentativeGameplayImpulse,
            target.transform.position + Vector3.up * 0.4f,
            ForceMode.Impulse);
        SimulatePhysics(context.Connections, 30);

        if (Vector3.Distance(neighborStart, neighbor.transform.position) < 0.002f)
        {
            throw new UnityException("M5 reinforced impact did not propagate motion through physical joints.");
        }

        if (CountBrokenConnections(context.Connections, StructuralConnectionType.Reinforced) > 0)
        {
            throw new UnityException("M5 reinforced joint failed under the representative 15 N·s hit.");
        }
    }

    private static RuntimeContext LoadAndInitializeRuntimeScene()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        StructureManager manager = Object.FindAnyObjectByType<StructureManager>();
        MethodInfo awake = typeof(StructureManager).GetMethod(
            "Awake",
            BindingFlags.Instance | BindingFlags.NonPublic);
        awake?.Invoke(manager, null);

        var blocks = new Dictionary<string, StructuralBlock>();
        foreach (StructuralBlock block in Object.FindObjectsByType<StructuralBlock>())
        {
            blocks.Add(block.NodeId, block);
        }

        return new RuntimeContext(
            blocks,
            new List<StructuralConnection>(Object.FindObjectsByType<StructuralConnection>()));
    }

    private static void SimulatePhysics(
        IReadOnlyList<StructuralConnection> connections,
        int steps)
    {
        SimulationMode previousMode = Physics.simulationMode;
        Physics.simulationMode = SimulationMode.Script;
        Physics.SyncTransforms();
        MethodInfo resolveBreak = typeof(StructuralConnection).GetMethod(
            "ResolvePhysicalJointBreak",
            BindingFlags.Instance | BindingFlags.NonPublic);
        try
        {
            for (int step = 0; step < steps; step++)
            {
                Physics.Simulate(0.02f);
                foreach (StructuralConnection connection in connections)
                {
                    resolveBreak?.Invoke(connection, null);
                }
            }
        }
        finally
        {
            Physics.simulationMode = previousMode;
        }
    }

    private static int CountBrokenConnections(
        IReadOnlyList<StructuralConnection> connections,
        StructuralConnectionType type)
    {
        int count = 0;
        foreach (StructuralConnection connection in connections)
        {
            if (connection.ConnectionType == type && !connection.HasPhysicalJoint)
            {
                count++;
            }
        }

        return count;
    }

    private static void AssertNoBrokenConnections(
        IReadOnlyList<StructuralConnection> connections,
        string message)
    {
        foreach (StructuralConnection connection in connections)
        {
            if (!connection.HasPhysicalJoint)
            {
                throw new UnityException(message);
            }
        }
    }

    private static void AssertConnectionProfile(
        StructuralConnection connection,
        float gravityCapacity,
        float impactCapacity,
        float breakForce,
        float breakTorque)
    {
        if (!Mathf.Approximately(connection.GravityCapacity, gravityCapacity)
            || !Mathf.Approximately(connection.ImpactCapacity, impactCapacity)
            || !Mathf.Approximately(connection.PhysicalBreakForce, breakForce)
            || !Mathf.Approximately(connection.PhysicalBreakTorque, breakTorque))
        {
            throw new UnityException(
                $"M5 connection '{connection.BlockA.NodeId}' <-> "
                + $"'{connection.BlockB.NodeId}' has the wrong profile values.");
        }
    }

    private static void ValidateNoInitialPenetration(IReadOnlyList<StructuralBlock> blocks)
    {
        Physics.SyncTransforms();
        for (int firstIndex = 0; firstIndex < blocks.Count; firstIndex++)
        {
            Collider first = blocks[firstIndex].GetComponent<Collider>();
            for (int secondIndex = firstIndex + 1; secondIndex < blocks.Count; secondIndex++)
            {
                Collider second = blocks[secondIndex].GetComponent<Collider>();
                if (Physics.ComputePenetration(
                        first,
                        first.transform.position,
                        first.transform.rotation,
                        second,
                        second.transform.position,
                        second.transform.rotation,
                        out _,
                        out float distance)
                    && distance > 0.0001f)
                {
                    throw new UnityException(
                        $"M5 blocks '{blocks[firstIndex].NodeId}' and "
                        + $"'{blocks[secondIndex].NodeId}' overlap by {distance:0.0000} m.");
                }
            }
        }
    }

    private static BlockMaterials ReadBlockMaterials(StructuralBlock source)
    {
        var serializedSource = new SerializedObject(source);
        return new BlockMaterials(
            serializedSource.FindProperty("supportedMaterial").objectReferenceValue as Material,
            serializedSource.FindProperty("unsupportedMaterial").objectReferenceValue as Material,
            serializedSource.FindProperty("disconnectedMaterial").objectReferenceValue as Material,
            serializedSource.FindProperty("destroyedMaterial").objectReferenceValue as Material);
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

    private static void AddSceneToBuildSettings()
    {
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        scenes.RemoveAll(scene => scene.path == ScenePath);
        scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private readonly struct BlockMaterials
    {
        public BlockMaterials(
            Material supported,
            Material unsupported,
            Material disconnected,
            Material destroyed)
        {
            Supported = supported;
            Unsupported = unsupported;
            Disconnected = disconnected;
            Destroyed = destroyed;
        }

        public Material Supported { get; }
        public Material Unsupported { get; }
        public Material Disconnected { get; }
        public Material Destroyed { get; }
    }

    private readonly struct RuntimeContext
    {
        public RuntimeContext(
            Dictionary<string, StructuralBlock> blocks,
            List<StructuralConnection> connections)
        {
            Blocks = blocks;
            Connections = connections;
        }

        public Dictionary<string, StructuralBlock> Blocks { get; }
        public List<StructuralConnection> Connections { get; }
    }
}
