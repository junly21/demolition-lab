using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using static FinalPresentationSettings;

public static class FinalVisualPolishBuilder
{
    [MenuItem("DemolitionLab/Presentation/Apply Final Materials And Lighting")]
    public static void ApplyMaterialsAndLighting()
    {
        ThirdPartyPresentationBuilder.ApplyToAllStages();
        foreach (string scenePath in StageScenePaths)
        {
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            VerticalSlicePresentationBuilder.ApplyLightingToOpenScene();
            ApplyLightingAndPostToOpenScene();
            EditorSceneManager.SaveOpenScenes();
        }

        AssetDatabase.SaveAssets();
        Debug.Log("Applied final industrial materials, lighting, and post processing to Stage01-05.");
    }

    [MenuItem("DemolitionLab/Presentation/Validate Final Materials And Lighting")]
    public static void ValidateMaterialsAndLighting()
    {
        foreach (string scenePath in StageScenePaths)
        {
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            ValidateLightingAndPostInOpenScene(scenePath);
        }

        Debug.Log("Final materials and lighting validation passed for Stage01-05.");
    }

    [MenuItem("DemolitionLab/Presentation/Apply Demolition Impact Feedback")]
    public static void ApplyImpactFeedback()
    {
        foreach (string scenePath in StageScenePaths)
        {
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            ApplyFeedbackToOpenScene();
            EditorSceneManager.SaveOpenScenes();
        }

        AssetDatabase.SaveAssets();
        Debug.Log("Applied demolition impact feedback to Stage01-05.");
    }

    [MenuItem("DemolitionLab/Presentation/Validate Demolition Impact Feedback")]
    public static void ValidateImpactFeedback()
    {
        foreach (string scenePath in StageScenePaths)
        {
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            ValidateFeedbackInOpenScene(scenePath);
        }

        Debug.Log("Demolition impact feedback validation passed for Stage01-05.");
    }

    [MenuItem("DemolitionLab/Presentation/Apply Final HUD And Flow")]
    public static void ApplyHudAndFlow()
    {
        foreach (string scenePath in StageScenePaths)
        {
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            ApplyHudToOpenScene();
            EditorSceneManager.SaveOpenScenes();
        }

        FinalTitleSceneBuilder.CreateTitleScene();
        FinalTitleSceneBuilder.ConfigureBuildSettings();
        AssetDatabase.SaveAssets();
        Debug.Log("Applied final HUD, title, result, and screenshot presentation flow.");
    }

    [MenuItem("DemolitionLab/Presentation/Validate Final HUD And Flow")]
    public static void ValidateHudAndFlow()
    {
        foreach (string scenePath in StageScenePaths)
        {
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            ValidateHudInOpenScene(scenePath);
        }

        FinalTitleSceneBuilder.ValidateTitleScene();
        Debug.Log("Final HUD and presentation flow validation passed.");
    }

    [MenuItem("DemolitionLab/Presentation/Validate Final Portfolio Build")]
    public static void ValidateFinalPortfolioBuild()
    {
        M1SceneBuilder.ValidateScene();
        M2SceneBuilder.ValidateScene();
        M3SceneBuilder.ValidateScene();
        M4SceneBuilder.ValidateScene();
        M5SceneBuilder.ValidateScene();
        VerticalSliceStage05Builder.ValidateAllStages();
        PhysicsPresentationSeparationBuilder.ValidateAllStages();
        ThirdPartyPresentationBuilder.ValidateAllStages();
        VerticalSlicePresentationBuilder.ValidateIndustrialEnvironment();
        VerticalSlicePresentationBuilder.ValidateStructuralPresentation();
        VerticalSlicePresentationBuilder.ValidateCameraFraming();
        ValidateMaterialsAndLighting();
        ValidateImpactFeedback();
        ValidateHudAndFlow();
        Debug.Log("Final portfolio build validation passed.");
    }

    internal static void ApplyLightingAndPostToOpenScene()
    {
        FinalLightingBuilder.ApplyLightingAndPostToOpenScene();
    }

    internal static void ApplyFeedbackToOpenScene()
    {
        FinalImpactFeedbackBuilder.ApplyFeedbackToOpenScene();
    }

    internal static void ApplyHudToOpenScene()
    {
        FinalStageHudBuilder.ApplyHudToOpenScene();
    }

    internal static void ValidateLightingAndPostInOpenScene(string scenePath)
    {
        FinalLightingBuilder.ValidateLightingAndPostInOpenScene(scenePath);
    }

    internal static void ValidateFeedbackInOpenScene(string scenePath)
    {
        FinalImpactFeedbackBuilder.ValidateFeedbackInOpenScene(scenePath);
    }

    internal static void ValidateHudInOpenScene(string scenePath)
    {
        FinalStageHudBuilder.ValidateHudInOpenScene(scenePath);
    }
}
