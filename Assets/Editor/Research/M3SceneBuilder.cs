using System.Collections.Generic;
using System.Reflection;
using DemolitionLab;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class M3SceneBuilder
{
    private const string SourceScenePath = "Assets/Scenes/M2_StructuralStability.unity";
    private const string ScenePath = "Assets/Scenes/M3_LocalSupport.unity";

    [MenuItem("DemolitionLab/Build M3 Scene")]
    public static void BuildScene()
    {
        string previousScenePath = SceneManager.GetActiveScene().path;
        Scene scene = EditorSceneManager.OpenScene(SourceScenePath, OpenSceneMode.Single);
        if (!EditorSceneManager.SaveScene(scene, ScenePath))
        {
            throw new UnityException($"Failed to create M3 scene at {ScenePath}.");
        }

        StructureManager manager = Object.FindFirstObjectByType<StructureManager>();
        if (manager == null)
        {
            throw new UnityException("M3 source scene is missing StructureManager.");
        }

        manager.gameObject.name = "M3 Local Support";
        LocalSupportEvaluator evaluator = manager.GetComponent<LocalSupportEvaluator>();
        if (evaluator == null)
        {
            evaluator = manager.gameObject.AddComponent<LocalSupportEvaluator>();
        }

        var serializedManager = new SerializedObject(manager);
        serializedManager.FindProperty("localSupportEvaluator").objectReferenceValue = evaluator;
        serializedManager.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene);
        AddSceneToBuildSettings();
        AssetDatabase.SaveAssets();

        if (!string.IsNullOrEmpty(previousScenePath) && previousScenePath != ScenePath)
        {
            EditorSceneManager.OpenScene(previousScenePath, OpenSceneMode.Single);
        }

        Debug.Log($"Built Milestone 3 scene at {ScenePath}");
    }

    [MenuItem("DemolitionLab/Validate M3 Scene")]
    public static void ValidateScene()
    {
        ValidateInitialStructure();
        ValidateGlobalFailure();
        ValidateRightSupportRemoval();
        ValidateMiddleSupportRemoval();
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Debug.Log("M3 validation passed: local vertical support, cantilever limits, propagation, and regressions verified.");
    }

    private static void ValidateInitialStructure()
    {
        ValidationContext context = LoadAndInitializeScene();
        foreach (StructuralBlock block in context.Blocks.Values)
        {
            AssertSupported(block, "The intact M3 structure must remain locally supported.");
        }
    }

    private static void ValidateGlobalFailure()
    {
        ValidationContext context = LoadAndInitializeScene();
        context.Blocks["Left_Base"].HandleProjectileImpact();

        foreach (StructuralBlock block in context.Blocks.Values)
        {
            if (block.IsActive
                && (block.ParticipatesInStructure
                    || block.GetComponent<Rigidbody>().isKinematic
                    || block.State != StructuralBlockState.Unstable))
            {
                throw new UnityException("A globally unstable component was not released before local support evaluation.");
            }
        }
    }

    private static void ValidateRightSupportRemoval()
    {
        ValidationContext context = LoadAndInitializeScene();
        context.Blocks["Right_Base"].HandleProjectileImpact();

        AssertSupported(context.Blocks["Left_Base"], "The remaining anchor should stay supported.");
        AssertSupported(context.Blocks["Left_Mid"], "Direct vertical support should remain valid.");
        AssertSupported(context.Blocks["Beam_Left"], "A one-block cantilever should remain supported.");
        AssertSupported(context.Blocks["Beam_Center"], "A two-block cantilever should remain supported.");
        AssertReleased(context.Blocks["Beam_Right"], "A three-block cantilever should be released.");
        AssertReleased(context.Blocks["Right_Top"], "The unsupported right top should be released.");
        AssertReleased(context.Blocks["Right_Mid"], "The block above the removed right base should be released.");
        AssertReleasedBlocksExcluded(context.Manager);
    }

    private static void ValidateMiddleSupportRemoval()
    {
        ValidationContext context = LoadAndInitializeScene();
        context.Blocks["Right_Mid"].HandleProjectileImpact();

        AssertReleased(context.Blocks["Right_Top"], "A block above a destroyed middle support should be released.");
        AssertReleased(context.Blocks["Beam_Right"], "Unsupported propagation should release dependent overhang blocks.");
        AssertSupported(context.Blocks["Right_Base"], "An isolated ground anchor should remain supported.");
        AssertSupported(context.Blocks["Beam_Center"], "The valid short cantilever should remain static.");
    }

    private static ValidationContext LoadAndInitializeScene()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        StructuralBlock[] blocks = Object.FindObjectsByType<StructuralBlock>(FindObjectsSortMode.None);
        StructureManager manager = Object.FindFirstObjectByType<StructureManager>();
        LocalSupportEvaluator evaluator = Object.FindFirstObjectByType<LocalSupportEvaluator>();
        if (manager == null || evaluator == null || blocks.Length != 11)
        {
            throw new UnityException("M3 scene must contain 11 blocks, StructureManager, and LocalSupportEvaluator.");
        }

        var blocksById = new Dictionary<string, StructuralBlock>();
        foreach (StructuralBlock block in blocks)
        {
            if (!blocksById.TryAdd(block.NodeId, block))
            {
                throw new UnityException($"Duplicate M3 node ID: {block.NodeId}");
            }
        }

        MethodInfo awake = typeof(StructureManager).GetMethod(
            "Awake",
            BindingFlags.Instance | BindingFlags.NonPublic);
        awake?.Invoke(manager, null);
        return new ValidationContext(blocksById, manager);
    }

    private static void AssertSupported(StructuralBlock block, string message)
    {
        if (!block.ParticipatesInStructure
            || !block.IsSupported
            || !block.GetComponent<Rigidbody>().isKinematic)
        {
            throw new UnityException($"{message} Node: {block.NodeId}");
        }
    }

    private static void AssertReleased(StructuralBlock block, string message)
    {
        if (!block.IsReleased
            || block.ParticipatesInStructure
            || block.GetComponent<Rigidbody>().isKinematic)
        {
            throw new UnityException($"{message} Node: {block.NodeId}");
        }
    }

    private static void AssertReleasedBlocksExcluded(StructureManager manager)
    {
        MethodInfo findComponents = typeof(StructureManager).GetMethod(
            "FindActiveComponents",
            BindingFlags.Instance | BindingFlags.NonPublic);
        var components = findComponents?.Invoke(manager, null) as List<List<StructuralBlock>>;
        if (components == null)
        {
            throw new UnityException("Could not inspect M3 static components.");
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
        public ValidationContext(Dictionary<string, StructuralBlock> blocks, StructureManager manager)
        {
            Blocks = blocks;
            Manager = manager;
        }

        public Dictionary<string, StructuralBlock> Blocks { get; }
        public StructureManager Manager { get; }
    }
}
