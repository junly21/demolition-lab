using System.Collections.Generic;
using DemolitionLab;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class VerticalSliceStage02Builder
{
    private const string SourceScenePath = "Assets/Scenes/VerticalSlice_Stage01.unity";
    private const string ScenePath = "Assets/Scenes/VerticalSlice_Stage02.unity";
    private const int ExpectedPieceCount = 24;
    private const int ExpectedTargetCount = 12;
    private const int ExpectedJointCount = 4;
    private const float LowerBreakForce = 240f;
    private const float LowerBreakTorque = 95f;
    private const float UpperBreakForce = 300f;
    private const float UpperBreakTorque = 120f;

    [MenuItem("DemolitionLab/Build Vertical Slice Stage 02")]
    public static void BuildScene()
    {
        Scene scene = EditorSceneManager.OpenScene(SourceScenePath, OpenSceneMode.Single);
        if (!EditorSceneManager.SaveScene(scene, ScenePath))
        {
            throw new UnityException($"Failed to create Stage02 at {ScenePath}.");
        }

        GameObject oldStructure = GameObject.Find("Stage 01 Structure");
        if (oldStructure == null)
        {
            throw new UnityException("Stage02 source is missing the Stage01 structure root.");
        }

        Object.DestroyImmediate(oldStructure);
        StageMaterials materials = LoadMaterials();
        var pieces = new List<GameObject>();
        var targets = new List<StageTargetPiece>();
        var structure = new Dictionary<string, GameObject>();
        GameObject root = new("Stage 02 Dual Support Structure");
        BuildStructure(root.transform, materials, structure, pieces, targets);

        VerticalSliceStageController controller = Object.FindAnyObjectByType<VerticalSliceStageController>();
        if (controller == null)
        {
            throw new UnityException("Stage02 source is missing the reusable stage controller.");
        }

        ConfigureController(controller, targets);
        AddJoints(structure, controller, materials.Broken);
        EditorSceneManager.SaveScene(scene);
        AddSceneToBuildSettings();
        AssetDatabase.SaveAssets();
        Debug.Log($"Built vertical-slice Stage02 at {ScenePath}");
    }

    [MenuItem("DemolitionLab/Validate Vertical Slice Stage 02")]
    public static void ValidateScene()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject[] pieces = GameObject.FindGameObjectsWithTag("Destructible");
        StageTargetPiece[] targets = Object.FindObjectsByType<StageTargetPiece>();
        CriticalSupportJoint[] joints = Object.FindObjectsByType<CriticalSupportJoint>();
        VerticalSliceStageController[] controllers = Object.FindObjectsByType<VerticalSliceStageController>();

        if (pieces.Length != ExpectedPieceCount
            || targets.Length != ExpectedTargetCount
            || joints.Length != ExpectedJointCount
            || controllers.Length != 1)
        {
            throw new UnityException(
                $"Stage02 expected {ExpectedPieceCount} pieces, {ExpectedTargetCount} targets, "
                + $"{ExpectedJointCount} joints, and one controller.");
        }

        ValidateController(controllers[0]);
        ValidateJoints(joints);
        ValidateNoInitialPenetration(pieces);

        int leftTargets = 0;
        int rightTargets = 0;
        foreach (StageTargetPiece target in targets)
        {
            if (target.name.StartsWith("Left_"))
            {
                leftTargets++;
            }
            else if (target.name.StartsWith("Right_"))
            {
                rightTargets++;
            }
        }

        if (leftTargets != 6 || rightTargets != 6)
        {
            throw new UnityException(
                $"Stage02 must split its objective evenly, found {leftTargets}/{rightTargets}.");
        }

        Debug.Log(
            "Stage02 validation passed: 24 pieces, two six-target halves, four critical joints, "
            + "three shots, Stage03 progression, references, and collider separation verified.");
    }

    private static void BuildStructure(
        Transform root,
        StageMaterials materials,
        IDictionary<string, GameObject> structure,
        ICollection<GameObject> pieces,
        ICollection<StageTargetPiece> targets)
    {
        AddPiece(structure, pieces, targets, root, "Left_Foundation", new Vector3(-2.4f, 0.5f, 4f), new Vector3(1.5f, 1f, 1.5f), 8f, true, false, materials.Concrete);
        AddPiece(structure, pieces, targets, root, "Right_Foundation", new Vector3(2.4f, 0.5f, 4f), new Vector3(1.5f, 1f, 1.5f), 8f, true, false, materials.Concrete);
        AddPiece(structure, pieces, targets, root, "Left_Weak_Lower", new Vector3(-2.4f, 1.5f, 4f), Vector3.one, 2f, false, false, materials.Weak);
        AddPiece(structure, pieces, targets, root, "Left_Weak_Upper", new Vector3(-2.4f, 2.5f, 4f), Vector3.one, 2f, false, false, materials.Weak);
        AddPiece(structure, pieces, targets, root, "Right_Weak_Lower", new Vector3(2.4f, 1.5f, 4f), Vector3.one, 2f, false, false, materials.Weak);
        AddPiece(structure, pieces, targets, root, "Right_Weak_Upper", new Vector3(2.4f, 2.5f, 4f), Vector3.one, 2f, false, false, materials.Weak);

        AddSide(root, materials, structure, pieces, targets, "Left", -1.75f, new[] { -3f, -2.25f, -1.5f, -0.75f });
        AddSide(root, materials, structure, pieces, targets, "Right", 1.75f, new[] { 0.75f, 1.5f, 2.25f, 3f });

        AddPiece(structure, pieces, targets, root, "Left_Ground_Decor", new Vector3(-4.6f, 0.5f, 4f), Vector3.one, 1.5f, false, false, materials.Decor);
        AddPiece(structure, pieces, targets, root, "Right_Ground_Decor", new Vector3(4.6f, 0.5f, 4f), Vector3.one, 1.5f, false, false, materials.Decor);
    }

    private static void AddSide(
        Transform root,
        StageMaterials materials,
        IDictionary<string, GameObject> structure,
        ICollection<GameObject> pieces,
        ICollection<StageTargetPiece> targets,
        string side,
        float centerX,
        IReadOnlyList<float> wallPositions)
    {
        AddPiece(structure, pieces, targets, root, $"{side}_Platform", new Vector3(centerX, 3.25f, 4f), new Vector3(3.5f, 0.5f, 1.5f), 5f, false, true, materials.Concrete);
        foreach (float wallX in wallPositions)
        {
            AddPiece(structure, pieces, targets, root, $"{side}_Wall_{wallX:0.00}", new Vector3(wallX, 4.25f, 4f), new Vector3(0.6f, 1.5f, 1f), 1f, false, true, materials.Concrete);
        }

        AddPiece(structure, pieces, targets, root, $"{side}_Roof", new Vector3(centerX, 5.25f, 4f), new Vector3(3.5f, 0.5f, 1.5f), 4f, false, true, materials.Concrete);
        AddPiece(structure, pieces, targets, root, $"{side}_Roof_Decor_A", new Vector3(centerX - 0.7f, 5.8f, 4f), Vector3.one * 0.6f, 0.8f, false, false, materials.Decor);
        AddPiece(structure, pieces, targets, root, $"{side}_Roof_Decor_B", new Vector3(centerX + 0.7f, 5.8f, 4f), Vector3.one * 0.6f, 0.8f, false, false, materials.Decor);
    }

    private static void AddPiece(
        IDictionary<string, GameObject> structure,
        ICollection<GameObject> pieces,
        ICollection<StageTargetPiece> targets,
        Transform root,
        string name,
        Vector3 position,
        Vector3 scale,
        float mass,
        bool isKinematic,
        bool isTarget,
        Material material)
    {
        GameObject piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
        piece.name = name;
        piece.tag = "Destructible";
        piece.transform.SetParent(root, false);
        piece.transform.position = position;
        piece.transform.localScale = scale;
        piece.GetComponent<Renderer>().sharedMaterial = material;

        Rigidbody body = piece.AddComponent<Rigidbody>();
        body.mass = mass;
        body.isKinematic = isKinematic;
        body.linearDamping = 0.08f;
        body.angularDamping = 0.18f;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        body.solverIterations = 10;
        body.solverVelocityIterations = 4;

        if (isTarget)
        {
            targets.Add(piece.AddComponent<StageTargetPiece>());
        }

        structure.Add(name, piece);
        pieces.Add(piece);
    }

    private static void AddJoints(
        IReadOnlyDictionary<string, GameObject> structure,
        VerticalSliceStageController controller,
        Material brokenMaterial)
    {
        CreateJoint(structure["Left_Weak_Lower"], structure["Left_Foundation"], controller, LowerBreakForce, LowerBreakTorque, brokenMaterial);
        CreateJoint(structure["Left_Weak_Upper"], structure["Left_Weak_Lower"], controller, UpperBreakForce, UpperBreakTorque, brokenMaterial);
        CreateJoint(structure["Right_Weak_Lower"], structure["Right_Foundation"], controller, LowerBreakForce, LowerBreakTorque, brokenMaterial);
        CreateJoint(structure["Right_Weak_Upper"], structure["Right_Weak_Lower"], controller, UpperBreakForce, UpperBreakTorque, brokenMaterial);
    }

    private static void CreateJoint(
        GameObject owner,
        GameObject connected,
        VerticalSliceStageController controller,
        float breakForce,
        float breakTorque,
        Material brokenMaterial)
    {
        FixedJoint joint = owner.AddComponent<FixedJoint>();
        joint.connectedBody = connected.GetComponent<Rigidbody>();
        joint.breakForce = breakForce;
        joint.breakTorque = breakTorque;
        joint.enableCollision = false;

        CriticalSupportJoint supportJoint = owner.AddComponent<CriticalSupportJoint>();
        var serializedJoint = new SerializedObject(supportJoint);
        serializedJoint.FindProperty("physicalJoint").objectReferenceValue = joint;
        serializedJoint.FindProperty("stageController").objectReferenceValue = controller;
        serializedJoint.FindProperty("weakConnection").boolValue = true;
        serializedJoint.FindProperty("indicatorRenderer").objectReferenceValue = owner.GetComponent<Renderer>();
        serializedJoint.FindProperty("brokenMaterial").objectReferenceValue = brokenMaterial;
        serializedJoint.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureController(
        VerticalSliceStageController controller,
        IReadOnlyList<StageTargetPiece> targets)
    {
        var serializedController = new SerializedObject(controller);
        serializedController.FindProperty("stageLabel").stringValue = "STAGE 02";
        serializedController.FindProperty("startingShots").intValue = 3;
        serializedController.FindProperty("clearThreshold").floatValue = 0.7f;
        serializedController.FindProperty("collapseSettleDelay").floatValue = 2.75f;
        serializedController.FindProperty("nextSceneName").stringValue = "VerticalSlice_Stage03";
        AssignObjectList(serializedController.FindProperty("targetPieces"), targets);
        Text label = (Text)serializedController.FindProperty("stageLabelText").objectReferenceValue;
        label.text = "STAGE 02";
        serializedController.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ValidateController(VerticalSliceStageController controller)
    {
        var serializedController = new SerializedObject(controller);
        if (serializedController.FindProperty("stageLabel").stringValue != "STAGE 02"
            || serializedController.FindProperty("startingShots").intValue != 3
            || !Mathf.Approximately(serializedController.FindProperty("clearThreshold").floatValue, 0.7f)
            || serializedController.FindProperty("nextSceneName").stringValue != "VerticalSlice_Stage03"
            || serializedController.FindProperty("targetPieces").arraySize != ExpectedTargetCount)
        {
            throw new UnityException("Stage02 controller configuration is invalid.");
        }
    }

    private static void ValidateJoints(IReadOnlyList<CriticalSupportJoint> joints)
    {
        int lowerJoints = 0;
        int upperJoints = 0;
        foreach (CriticalSupportJoint joint in joints)
        {
            if (!joint.IsWeak)
            {
                throw new UnityException("Stage02 critical joints must use the visible Weak supports.");
            }

            if (Mathf.Approximately(joint.BreakForce, LowerBreakForce)
                && Mathf.Approximately(joint.BreakTorque, LowerBreakTorque))
            {
                lowerJoints++;
            }
            else if (Mathf.Approximately(joint.BreakForce, UpperBreakForce)
                && Mathf.Approximately(joint.BreakTorque, UpperBreakTorque))
            {
                upperJoints++;
            }
        }

        if (lowerJoints != 2 || upperJoints != 2)
        {
            throw new UnityException("Stage02 joint calibration mismatch.");
        }
    }

    private static void ValidateNoInitialPenetration(IReadOnlyList<GameObject> pieces)
    {
        Physics.SyncTransforms();
        for (int firstIndex = 0; firstIndex < pieces.Count; firstIndex++)
        {
            Collider first = pieces[firstIndex].GetComponent<Collider>();
            for (int secondIndex = firstIndex + 1; secondIndex < pieces.Count; secondIndex++)
            {
                Collider second = pieces[secondIndex].GetComponent<Collider>();
                if (Physics.ComputePenetration(
                        first, first.transform.position, first.transform.rotation,
                        second, second.transform.position, second.transform.rotation,
                        out _, out float distance)
                    && distance > 0.0001f)
                {
                    throw new UnityException(
                        $"Stage02 pieces '{pieces[firstIndex].name}' and '{pieces[secondIndex].name}' "
                        + $"overlap by {distance:0.0000} m.");
                }
            }
        }
    }

    private static StageMaterials LoadMaterials()
    {
        return new StageMaterials(
            LoadMaterial("Concrete"),
            LoadMaterial("Weak"),
            LoadMaterial("Decor"),
            LoadMaterial("Broken"));
    }

    private static Material LoadMaterial(string name)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>($"Assets/Materials/VerticalSlice/{name}.mat");
        if (material == null)
        {
            throw new UnityException($"Missing vertical-slice material '{name}'.");
        }

        return material;
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
        int stageOneIndex = scenes.FindIndex(scene => scene.path == SourceScenePath);
        scenes.Insert(stageOneIndex + 1, new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private readonly struct StageMaterials
    {
        public StageMaterials(Material concrete, Material weak, Material decor, Material broken)
        {
            Concrete = concrete;
            Weak = weak;
            Decor = decor;
            Broken = broken;
        }

        public Material Concrete { get; }
        public Material Weak { get; }
        public Material Decor { get; }
        public Material Broken { get; }
    }
}
