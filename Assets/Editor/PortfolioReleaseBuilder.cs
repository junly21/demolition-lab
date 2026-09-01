using DemolitionLab;
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PortfolioReleaseBuilder
{
    private const string ProductName = "Demolition Lab";
    private const string Version = "1.0.0-portfolio";
    private const string TitleScene = "VerticalSlice_Title";
    private const string MacBuildPath = "Builds/Release/DemolitionLab-macOS/Demolition Lab.app";
    private const string WindowsBuildPath = "Builds/Release/DemolitionLab-Windows/Demolition Lab.exe";

    private static readonly string[] ReleaseScenes =
    {
        "Assets/Scenes/VerticalSlice_Title.unity",
        "Assets/Scenes/VerticalSlice_Stage01.unity",
        "Assets/Scenes/VerticalSlice_Stage02.unity",
        "Assets/Scenes/VerticalSlice_Stage03.unity",
        "Assets/Scenes/VerticalSlice_Stage04.unity",
        "Assets/Scenes/VerticalSlice_Stage05.unity"
    };

    private static readonly string[] NextScenes =
    {
        "VerticalSlice_Stage02",
        "VerticalSlice_Stage03",
        "VerticalSlice_Stage04",
        "VerticalSlice_Stage05",
        string.Empty
    };

    [MenuItem("DemolitionLab/Release/Apply Release Candidate Settings")]
    public static void ApplyReleaseCandidateSettings()
    {
        EditorBuildSettings.scenes = Array.ConvertAll(
            ReleaseScenes,
            path => new EditorBuildSettingsScene(path, true));

        PlayerSettings.productName = ProductName;
        PlayerSettings.bundleVersion = Version;
        PlayerSettings.defaultScreenWidth = 1920;
        PlayerSettings.defaultScreenHeight = 1080;
        PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;

        for (int index = 0; index < NextScenes.Length; index++)
        {
            string scenePath = ReleaseScenes[index + 1];
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            VerticalSliceStageController controller =
                UnityEngine.Object.FindAnyObjectByType<VerticalSliceStageController>();
            if (controller == null)
            {
                throw new UnityException($"{scenePath} has no stage controller.");
            }

            var serializedController = new SerializedObject(controller);
            serializedController.FindProperty("logImpactDiagnostics").boolValue = false;
            serializedController.FindProperty("completionSceneName").stringValue =
                index == NextScenes.Length - 1 ? TitleScene : string.Empty;
            serializedController.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
        }

        AssetDatabase.SaveAssets();
        Debug.Log("Applied portfolio release-candidate settings.");
    }

    [MenuItem("DemolitionLab/Release/Validate Release Candidate")]
    public static void ValidateReleaseCandidate()
    {
        ValidateBuildSettings();
        ValidatePlayerSettings();
        ValidateStageFlowConfiguration();
        FinalVisualPolishBuilder.ValidateFinalPortfolioBuild();
        Debug.Log("Portfolio release-candidate validation passed.");
    }

    [MenuItem("DemolitionLab/Release/Build macOS Release")]
    public static void BuildMacOSRelease()
    {
        ValidateReleaseCandidate();
        Build(BuildTarget.StandaloneOSX, MacBuildPath);
    }

    [MenuItem("DemolitionLab/Release/Build Windows Release")]
    public static void BuildWindowsRelease()
    {
        if (!IsWindowsBuildSupportAvailable())
        {
            throw new UnityException("Windows standalone build support is not installed.");
        }

        ValidateReleaseCandidate();
        Build(BuildTarget.StandaloneWindows64, WindowsBuildPath);
    }

    public static bool IsWindowsBuildSupportAvailable()
    {
        return BuildPipeline.IsBuildTargetSupported(
            BuildTargetGroup.Standalone,
            BuildTarget.StandaloneWindows64);
    }

    private static void ValidateBuildSettings()
    {
        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
        if (scenes.Length != ReleaseScenes.Length)
        {
            throw new UnityException("Release build must contain exactly Title and Stage01-05.");
        }

        for (int index = 0; index < ReleaseScenes.Length; index++)
        {
            if (!scenes[index].enabled || scenes[index].path != ReleaseScenes[index])
            {
                throw new UnityException($"Release scene order is invalid at index {index}.");
            }
        }
    }

    private static void ValidatePlayerSettings()
    {
        if (PlayerSettings.productName != ProductName
            || PlayerSettings.bundleVersion != Version
            || PlayerSettings.defaultScreenWidth != 1920
            || PlayerSettings.defaultScreenHeight != 1080)
        {
            throw new UnityException("Release player metadata or 16:9 defaults are incomplete.");
        }
    }

    private static void ValidateStageFlowConfiguration()
    {
        for (int index = 0; index < NextScenes.Length; index++)
        {
            string scenePath = ReleaseScenes[index + 1];
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            VerticalSliceStageController controller =
                UnityEngine.Object.FindAnyObjectByType<VerticalSliceStageController>();
            if (controller == null)
            {
                throw new UnityException($"{scenePath} has no stage controller.");
            }

            var serializedController = new SerializedObject(controller);
            string nextScene = serializedController.FindProperty("nextSceneName").stringValue;
            string completionScene = serializedController.FindProperty("completionSceneName").stringValue;
            bool diagnostics = serializedController.FindProperty("logImpactDiagnostics").boolValue;
            string expectedCompletion = index == NextScenes.Length - 1 ? TitleScene : string.Empty;
            if (nextScene != NextScenes[index]
                || completionScene != expectedCompletion
                || diagnostics)
            {
                throw new UnityException($"{scenePath} release flow configuration is invalid.");
            }
        }
    }

    private static void Build(BuildTarget target, string locationPathName)
    {
        string outputPath = Path.GetFullPath(Path.Combine(
            Application.dataPath,
            "..",
            locationPathName));
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
        var options = new BuildPlayerOptions
        {
            scenes = ReleaseScenes,
            locationPathName = outputPath,
            target = target,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
        {
            throw new UnityException(
                $"{target} release build failed: {report.summary.result}, "
                + $"{report.summary.totalErrors} errors.");
        }

        Debug.Log(
            $"Built {target} release at {outputPath} "
            + $"({report.summary.totalSize} bytes).");
    }
}
