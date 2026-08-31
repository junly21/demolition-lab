using System.Collections.Generic;
using DemolitionLab;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PhysicsPresentationSeparationBuilder
{
    internal const string VisualRootName = "VisualRoot";

    private static readonly string[] StageScenePaths =
    {
        "Assets/Scenes/VerticalSlice_Stage01.unity",
        "Assets/Scenes/VerticalSlice_Stage02.unity",
        "Assets/Scenes/VerticalSlice_Stage03.unity",
        "Assets/Scenes/VerticalSlice_Stage04.unity",
        "Assets/Scenes/VerticalSlice_Stage05.unity"
    };

    [MenuItem("DemolitionLab/Presentation/Separate Physics And Visuals")]
    public static void ApplyToAllStages()
    {
        foreach (string scenePath in StageScenePaths)
        {
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            ApplyToOpenScene();
            EditorSceneManager.SaveOpenScenes();
        }

        AssetDatabase.SaveAssets();
        Debug.Log("Separated physics and presentation geometry in Stage01-05.");
    }

    [MenuItem("DemolitionLab/Presentation/Validate Physics And Visuals")]
    public static void ValidateAllStages()
    {
        foreach (string scenePath in StageScenePaths)
        {
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            ValidateOpenScene(scenePath);
        }

        Debug.Log("Physics and presentation separation validation passed for Stage01-05.");
    }

    internal static void ApplyToOpenScene()
    {
        foreach (GameObject physicalRoot in GameObject.FindGameObjectsWithTag("Destructible"))
        {
            if (physicalRoot.GetComponent<Rigidbody>() == null || physicalRoot.GetComponent<Collider>() == null)
            {
                continue;
            }

            Transform visualRoot = physicalRoot.transform.Find(VisualRootName);
            if (visualRoot == null)
            {
                visualRoot = new GameObject(VisualRootName).transform;
                visualRoot.SetParent(physicalRoot.transform, false);
            }

            MoveRootGeometry(physicalRoot, visualRoot.gameObject);
            MovePresentationChildren(physicalRoot.transform, visualRoot);

            PresentationVisual metadata = visualRoot.GetComponent<PresentationVisual>();
            if (metadata == null)
            {
                metadata = visualRoot.gameObject.AddComponent<PresentationVisual>();
            }

            metadata.Configure(DetermineCategory(physicalRoot, visualRoot));
            RewireIndicatorRenderer(physicalRoot, visualRoot);
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    internal static void ValidateOpenScene(string scenePath)
    {
        GameObject[] physicalRoots = GameObject.FindGameObjectsWithTag("Destructible");
        if (physicalRoots.Length == 0)
        {
            throw new UnityException($"{scenePath} has no destructible physical roots.");
        }

        foreach (GameObject physicalRoot in physicalRoots)
        {
            if (physicalRoot.GetComponent<Rigidbody>() == null || physicalRoot.GetComponent<Collider>() == null)
            {
                throw new UnityException($"{scenePath} physical root '{physicalRoot.name}' is missing physics components.");
            }

            if (physicalRoot.GetComponent<MeshRenderer>() != null || physicalRoot.GetComponent<MeshFilter>() != null)
            {
                throw new UnityException($"{scenePath} physical root '{physicalRoot.name}' still owns render geometry.");
            }

            Transform visualRoot = physicalRoot.transform.Find(VisualRootName);
            if (visualRoot == null
                || visualRoot.GetComponent<PresentationVisual>() == null
                || visualRoot.GetComponentInChildren<MeshRenderer>(true) == null
                || visualRoot.GetComponentInChildren<MeshFilter>(true) == null)
            {
                throw new UnityException($"{scenePath} physical root '{physicalRoot.name}' has no valid VisualRoot.");
            }

            if (visualRoot.GetComponentInChildren<Rigidbody>(true) != null
                || visualRoot.GetComponentInChildren<Collider>(true) != null
                || visualRoot.GetComponentInChildren<FixedJoint>(true) != null
                || visualRoot.GetComponentInChildren<StageTargetPiece>(true) != null
                || visualRoot.GetComponentInChildren<CriticalSupportJoint>(true) != null)
            {
                throw new UnityException($"{scenePath} VisualRoot under '{physicalRoot.name}' contains gameplay or physics components.");
            }

            CriticalSupportJoint supportJoint = physicalRoot.GetComponent<CriticalSupportJoint>();
            if (supportJoint != null)
            {
                Renderer expectedRenderer = visualRoot.GetComponent<MeshRenderer>();
                Renderer assignedRenderer = (Renderer)new SerializedObject(supportJoint)
                    .FindProperty("indicatorRenderer").objectReferenceValue;
                if (expectedRenderer == null || assignedRenderer != expectedRenderer)
                {
                    throw new UnityException($"{scenePath} joint '{physicalRoot.name}' has an invalid indicator renderer reference.");
                }
            }
        }
    }

    internal static Transform FindDescendant(Transform root, string objectName)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child != root && child.name == objectName)
            {
                return child;
            }
        }

        return null;
    }

    private static void MoveRootGeometry(GameObject physicalRoot, GameObject visualRoot)
    {
        MeshFilter rootFilter = physicalRoot.GetComponent<MeshFilter>();
        MeshRenderer rootRenderer = physicalRoot.GetComponent<MeshRenderer>();
        if (rootFilter == null && rootRenderer == null)
        {
            return;
        }

        if (rootFilter == null || rootRenderer == null)
        {
            throw new UnityException($"Physical root '{physicalRoot.name}' has incomplete render geometry.");
        }

        MeshFilter visualFilter = visualRoot.GetComponent<MeshFilter>();
        if (visualFilter == null)
        {
            visualFilter = visualRoot.AddComponent<MeshFilter>();
        }

        MeshRenderer visualRenderer = visualRoot.GetComponent<MeshRenderer>();
        if (visualRenderer == null)
        {
            visualRenderer = visualRoot.AddComponent<MeshRenderer>();
        }

        EditorUtility.CopySerialized(rootFilter, visualFilter);
        EditorUtility.CopySerialized(rootRenderer, visualRenderer);
        Object.DestroyImmediate(rootRenderer);
        Object.DestroyImmediate(rootFilter);
    }

    private static void MovePresentationChildren(Transform physicalRoot, Transform visualRoot)
    {
        var children = new List<Transform>();
        foreach (Transform child in physicalRoot)
        {
            if (child != visualRoot)
            {
                children.Add(child);
            }
        }

        foreach (Transform child in children)
        {
            if (ContainsPhysicsOrGameplay(child))
            {
                continue;
            }

            child.SetParent(visualRoot, true);
        }
    }

    private static bool ContainsPhysicsOrGameplay(Transform root)
    {
        return root.GetComponentInChildren<Rigidbody>(true) != null
            || root.GetComponentInChildren<Collider>(true) != null
            || root.GetComponentInChildren<Joint>(true) != null
            || root.GetComponentInChildren<StageTargetPiece>(true) != null
            || root.GetComponentInChildren<CriticalSupportJoint>(true) != null;
    }

    private static PresentationCategory DetermineCategory(GameObject physicalRoot, Transform visualRoot)
    {
        if (physicalRoot.name.Contains("Foundation"))
        {
            return PresentationCategory.Foundation;
        }

        CriticalSupportJoint supportJoint = physicalRoot.GetComponent<CriticalSupportJoint>();
        if (supportJoint != null)
        {
            return supportJoint.IsWeak ? PresentationCategory.Weak : PresentationCategory.Reinforced;
        }

        Renderer renderer = visualRoot.GetComponentInChildren<Renderer>(true);
        string materialName = renderer != null && renderer.sharedMaterial != null
            ? renderer.sharedMaterial.name
            : string.Empty;
        if (materialName.Contains("Weak"))
        {
            return PresentationCategory.Weak;
        }

        if (materialName.Contains("Reinforced"))
        {
            return PresentationCategory.Reinforced;
        }

        if (materialName.Contains("Steel") || physicalRoot.name.Contains("Beam"))
        {
            return PresentationCategory.Steel;
        }

        return PresentationCategory.Concrete;
    }

    private static void RewireIndicatorRenderer(GameObject physicalRoot, Transform visualRoot)
    {
        CriticalSupportJoint supportJoint = physicalRoot.GetComponent<CriticalSupportJoint>();
        if (supportJoint == null)
        {
            return;
        }

        Renderer renderer = visualRoot.GetComponent<MeshRenderer>();
        if (renderer == null)
        {
            throw new UnityException($"Joint '{physicalRoot.name}' has no primary visual renderer.");
        }

        var serializedJoint = new SerializedObject(supportJoint);
        serializedJoint.FindProperty("indicatorRenderer").objectReferenceValue = renderer;
        serializedJoint.ApplyModifiedPropertiesWithoutUndo();
    }
}
