using System.Collections.Generic;
using DemolitionLab;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class VerticalSliceStage03Builder
{
    private const string SourceScenePath = "Assets/Scenes/VerticalSlice_Stage01.unity";
    private const string Stage02ScenePath = "Assets/Scenes/VerticalSlice_Stage02.unity";
    private const string ScenePath = "Assets/Scenes/VerticalSlice_Stage03.unity";
    private const int ExpectedPieceCount = 31;
    private const int ExpectedTargetCount = 16;
    private const int ExpectedJointCount = 6;
    private const float WeakLowerForce = 240f;
    private const float WeakLowerTorque = 95f;
    private const float WeakUpperForce = 300f;
    private const float WeakUpperTorque = 120f;
    private const float ReinforcedLowerForce = 1200f;
    private const float ReinforcedLowerTorque = 500f;
    private const float ReinforcedUpperForce = 1400f;
    private const float ReinforcedUpperTorque = 600f;

    [MenuItem("DemolitionLab/Build Vertical Slice Stage 03")]
    public static void BuildScene()
    {
        Scene scene = EditorSceneManager.OpenScene(SourceScenePath, OpenSceneMode.Single);
        if (!EditorSceneManager.SaveScene(scene, ScenePath))
        {
            throw new UnityException($"Failed to create Stage03 at {ScenePath}.");
        }

        GameObject oldStructure = GameObject.Find("Stage 01 Structure");
        if (oldStructure == null)
        {
            throw new UnityException("Stage03 source is missing the Stage01 structure root.");
        }

        Object.DestroyImmediate(oldStructure);
        StageMaterials materials = LoadMaterials();
        var pieces = new List<GameObject>();
        var targets = new List<StageTargetPiece>();
        var structure = new Dictionary<string, GameObject>();
        GameObject root = new("Stage 03 Reinforced Center Structure");
        BuildStructure(root.transform, materials, structure, pieces, targets);

        VerticalSliceStageController controller = Object.FindAnyObjectByType<VerticalSliceStageController>();
        if (controller == null)
        {
            throw new UnityException("Stage03 source is missing the reusable stage controller.");
        }

        ConfigureController(controller, targets);
        AddJoints(structure, controller, materials.Broken);
        EditorSceneManager.SaveScene(scene);
        AddSceneToBuildSettings();
        AssetDatabase.SaveAssets();
        Debug.Log($"Built vertical-slice Stage03 at {ScenePath}");
    }

    [MenuItem("DemolitionLab/Validate Vertical Slice Stage 03")]
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
                $"Stage03 expected {ExpectedPieceCount} pieces, {ExpectedTargetCount} targets, "
                + $"{ExpectedJointCount} joints, and one controller.");
        }

        ValidateController(controllers[0]);
        ValidateJoints(joints);
        ValidateNoInitialPenetration(pieces);
        ValidateNoResearchSystems();

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

        if (leftTargets != 8 || rightTargets != 8)
        {
            throw new UnityException(
                $"Stage03 must split its objective evenly, found {leftTargets}/{rightTargets}.");
        }

        Debug.Log(
            "Stage03 validation passed: 31 pieces, two eight-target halves, four Weak and two "
            + "Reinforced joints, four shots, no next scene, references, and collider separation verified.");
    }

    [MenuItem("DemolitionLab/Validate All Vertical Slice Stages")]
    public static void ValidateAllStages()
    {
        VerticalSliceSceneBuilder.ValidateScene();
        VerticalSliceStage02Builder.ValidateScene();
        ValidateScene();

        var enabledScenes = new List<string>();
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (scene.enabled)
            {
                enabledScenes.Add(scene.path);
            }
        }

        int stageOneIndex = enabledScenes.IndexOf(SourceScenePath);
        int stageTwoIndex = enabledScenes.IndexOf(Stage02ScenePath);
        int stageThreeIndex = enabledScenes.IndexOf(ScenePath);
        if (stageOneIndex < 0
            || stageTwoIndex != stageOneIndex + 1
            || stageThreeIndex != stageTwoIndex + 1)
        {
            throw new UnityException("Vertical-slice build order must be Stage01 -> Stage02 -> Stage03.");
        }

        Debug.Log("All vertical-slice stages and Stage01 -> Stage02 -> Stage03 progression validated.");
    }

    private static void BuildStructure(
        Transform root,
        StageMaterials materials,
        IDictionary<string, GameObject> structure,
        ICollection<GameObject> pieces,
        ICollection<StageTargetPiece> targets)
    {
        AddPiece(structure, pieces, targets, root, "Left_Foundation", new Vector3(-3f, 0.5f, 4f), new Vector3(1.5f, 1f, 1.5f), 8f, true, false, materials.Concrete);
        AddPiece(structure, pieces, targets, root, "Center_Foundation", new Vector3(0f, 0.5f, 4f), new Vector3(1.8f, 1f, 1.6f), 10f, true, false, materials.Reinforced);
        AddPiece(structure, pieces, targets, root, "Right_Foundation", new Vector3(3f, 0.5f, 4f), new Vector3(1.5f, 1f, 1.5f), 8f, true, false, materials.Concrete);

        AddPiece(structure, pieces, targets, root, "Left_Weak_Lower", new Vector3(-3f, 1.5f, 4f), Vector3.one, 2f, false, false, materials.Weak);
        AddPiece(structure, pieces, targets, root, "Left_Weak_Upper", new Vector3(-3f, 2.5f, 4f), Vector3.one, 2f, false, false, materials.Weak);
        AddPiece(structure, pieces, targets, root, "Right_Weak_Lower", new Vector3(3f, 1.5f, 4f), Vector3.one, 2f, false, false, materials.Weak);
        AddPiece(structure, pieces, targets, root, "Right_Weak_Upper", new Vector3(3f, 2.5f, 4f), Vector3.one, 2f, false, false, materials.Weak);
        AddPiece(structure, pieces, targets, root, "Center_Reinforced_Lower", new Vector3(0f, 1.5f, 4f), new Vector3(1.2f, 1f, 1.3f), 3f, false, false, materials.Reinforced);
        AddPiece(structure, pieces, targets, root, "Center_Reinforced_Upper", new Vector3(0f, 2.5f, 4f), new Vector3(1.2f, 1f, 1.3f), 3f, false, false, materials.Reinforced);
        AddReinforcementBand(structure["Center_Reinforced_Lower"].transform, materials.Reinforced, "Lower_Band");
        AddReinforcementBand(structure["Center_Reinforced_Upper"].transform, materials.Reinforced, "Upper_Band");

        AddSide(root, materials, structure, pieces, targets, "Left", -1.75f, new[] { -3.15f, -2.58f, -2.01f, -1.44f, -0.87f, -0.3f });
        AddSide(root, materials, structure, pieces, targets, "Right", 1.75f, new[] { 0.3f, 0.87f, 1.44f, 2.01f, 2.58f, 3.15f });

        AddPiece(structure, pieces, targets, root, "Left_Ground_Decor", new Vector3(-4.8f, 0.5f, 4f), Vector3.one, 1.5f, false, false, materials.Decor);
        AddPiece(structure, pieces, targets, root, "Right_Ground_Decor", new Vector3(4.8f, 0.5f, 4f), Vector3.one, 1.5f, false, false, materials.Decor);
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
        AddPiece(structure, pieces, targets, root, $"{side}_Platform", new Vector3(centerX, 3.25f, 4f), new Vector3(3.5f, 0.5f, 1.5f), 6f, false, true, materials.Concrete);
        foreach (float wallX in wallPositions)
        {
            AddPiece(structure, pieces, targets, root, $"{side}_Wall_{wallX:0.00}", new Vector3(wallX, 4.25f, 4f), new Vector3(0.45f, 1.5f, 1f), 0.9f, false, true, materials.Concrete);
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

    private static void AddReinforcementBand(Transform parent, Material material, string name)
    {
        GameObject band = GameObject.CreatePrimitive(PrimitiveType.Cube);
        band.name = name;
        band.transform.SetParent(parent, false);
        band.transform.localPosition = new Vector3(0f, 0.28f, 0f);
        band.transform.localScale = new Vector3(1.12f, 0.16f, 1.12f);
        band.GetComponent<Renderer>().sharedMaterial = material;
        Object.DestroyImmediate(band.GetComponent<Collider>());
    }

    private static void AddJoints(
        IReadOnlyDictionary<string, GameObject> structure,
        VerticalSliceStageController controller,
        Material brokenMaterial)
    {
        CreateJoint(structure["Left_Weak_Lower"], structure["Left_Foundation"], controller, true, WeakLowerForce, WeakLowerTorque, brokenMaterial);
        CreateJoint(structure["Left_Weak_Upper"], structure["Left_Weak_Lower"], controller, true, WeakUpperForce, WeakUpperTorque, brokenMaterial);
        CreateJoint(structure["Right_Weak_Lower"], structure["Right_Foundation"], controller, true, WeakLowerForce, WeakLowerTorque, brokenMaterial);
        CreateJoint(structure["Right_Weak_Upper"], structure["Right_Weak_Lower"], controller, true, WeakUpperForce, WeakUpperTorque, brokenMaterial);
        CreateJoint(structure["Center_Reinforced_Lower"], structure["Center_Foundation"], controller, false, ReinforcedLowerForce, ReinforcedLowerTorque, brokenMaterial);
        CreateJoint(structure["Center_Reinforced_Upper"], structure["Center_Reinforced_Lower"], controller, false, ReinforcedUpperForce, ReinforcedUpperTorque, brokenMaterial);
    }

    private static void CreateJoint(
        GameObject owner,
        GameObject connected,
        VerticalSliceStageController controller,
        bool isWeak,
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
        serializedJoint.FindProperty("weakConnection").boolValue = isWeak;
        serializedJoint.FindProperty("indicatorRenderer").objectReferenceValue = owner.GetComponent<Renderer>();
        serializedJoint.FindProperty("brokenMaterial").objectReferenceValue = brokenMaterial;
        serializedJoint.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureController(
        VerticalSliceStageController controller,
        IReadOnlyList<StageTargetPiece> targets)
    {
        var serializedController = new SerializedObject(controller);
        serializedController.FindProperty("stageLabel").stringValue = "STAGE 03";
        serializedController.FindProperty("startingShots").intValue = 4;
        serializedController.FindProperty("clearThreshold").floatValue = 0.7f;
        serializedController.FindProperty("collapseSettleDelay").floatValue = 2.75f;
        serializedController.FindProperty("nextSceneName").stringValue = string.Empty;
        AssignObjectList(serializedController.FindProperty("targetPieces"), targets);
        Text label = (Text)serializedController.FindProperty("stageLabelText").objectReferenceValue;
        label.text = "STAGE 03";
        serializedController.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ValidateController(VerticalSliceStageController controller)
    {
        var serializedController = new SerializedObject(controller);
        if (serializedController.FindProperty("stageLabel").stringValue != "STAGE 03"
            || serializedController.FindProperty("startingShots").intValue != 4
            || !Mathf.Approximately(serializedController.FindProperty("clearThreshold").floatValue, 0.7f)
            || !string.IsNullOrEmpty(serializedController.FindProperty("nextSceneName").stringValue)
            || serializedController.FindProperty("targetPieces").arraySize != ExpectedTargetCount
            || controller.HasNextScene)
        {
            throw new UnityException("Stage03 controller configuration is invalid.");
        }

        ValidateControllerReferences(serializedController);
    }

    private static void ValidateControllerReferences(SerializedObject controller)
    {
        string[] references =
        {
            "launcher", "stageLabelText", "shotsText", "destructionText", "endPanel",
            "resultTitleText", "resultDetailsText", "nextButton", "cameraShake", "impactDust"
        };

        foreach (string propertyName in references)
        {
            if (controller.FindProperty(propertyName).objectReferenceValue == null)
            {
                throw new UnityException($"Stage03 controller is missing '{propertyName}'.");
            }
        }

        SerializedProperty targets = controller.FindProperty("targetPieces");
        for (int index = 0; index < targets.arraySize; index++)
        {
            if (targets.GetArrayElementAtIndex(index).objectReferenceValue == null)
            {
                throw new UnityException($"Stage03 target reference {index} is missing.");
            }
        }
    }

    private static void ValidateNoResearchSystems()
    {
        if (Object.FindAnyObjectByType<StructureManager>() != null
            || Object.FindAnyObjectByType<StructuralBlock>() != null
            || Object.FindAnyObjectByType<StructuralConnection>() != null)
        {
            throw new UnityException("Stage03 must not use the M1-M5 structural simulation stack.");
        }
    }

    private static void ValidateJoints(IReadOnlyList<CriticalSupportJoint> joints)
    {
        int weakJoints = 0;
        int reinforcedJoints = 0;
        foreach (CriticalSupportJoint joint in joints)
        {
            if (joint.IsWeak)
            {
                weakJoints++;
                bool validWeak = Mathf.Approximately(joint.BreakForce, WeakLowerForce)
                    && Mathf.Approximately(joint.BreakTorque, WeakLowerTorque)
                    || Mathf.Approximately(joint.BreakForce, WeakUpperForce)
                    && Mathf.Approximately(joint.BreakTorque, WeakUpperTorque);
                if (!validWeak)
                {
                    throw new UnityException("Stage03 Weak joint calibration mismatch.");
                }
            }
            else
            {
                reinforcedJoints++;
                bool validReinforced = Mathf.Approximately(joint.BreakForce, ReinforcedLowerForce)
                    && Mathf.Approximately(joint.BreakTorque, ReinforcedLowerTorque)
                    || Mathf.Approximately(joint.BreakForce, ReinforcedUpperForce)
                    && Mathf.Approximately(joint.BreakTorque, ReinforcedUpperTorque);
                if (!validReinforced)
                {
                    throw new UnityException("Stage03 Reinforced joint calibration mismatch.");
                }
            }
        }

        if (weakJoints != 4 || reinforcedJoints != 2)
        {
            throw new UnityException(
                $"Stage03 expected 4 Weak and 2 Reinforced joints, found {weakJoints}/{reinforcedJoints}.");
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
                        $"Stage03 pieces '{pieces[firstIndex].name}' and '{pieces[secondIndex].name}' "
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
            LoadMaterial("Reinforced"),
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
        int stageTwoIndex = scenes.FindIndex(scene => scene.path == Stage02ScenePath);
        scenes.Insert(stageTwoIndex + 1, new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private readonly struct StageMaterials
    {
        public StageMaterials(
            Material concrete,
            Material weak,
            Material reinforced,
            Material decor,
            Material broken)
        {
            Concrete = concrete;
            Weak = weak;
            Reinforced = reinforced;
            Decor = decor;
            Broken = broken;
        }

        public Material Concrete { get; }
        public Material Weak { get; }
        public Material Reinforced { get; }
        public Material Decor { get; }
        public Material Broken { get; }
    }
}
