using System.Collections.Generic;
using DemolitionLab;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static FinalPresentationSettings;

internal static class FinalTitleSceneBuilder
{
    private const string TitleScenePath = "Assets/Scenes/VerticalSlice_Title.unity";
    private const string StageOneSceneName = "VerticalSlice_Stage01";

    internal static void CreateTitleScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "VerticalSlice_Title";

        GameObject cameraObject = new("Main Camera");
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.Skybox;
        camera.fieldOfView = 46f;
        camera.transform.position = new Vector3(-1.5f, 4.4f, -13.5f);
        camera.transform.LookAt(new Vector3(2.8f, 2.4f, 6.5f));
        cameraObject.AddComponent<AudioListener>();

        GameObject keyObject = new("Directional Light");
        Light key = keyObject.AddComponent<Light>();
        key.type = LightType.Directional;
        key.color = new Color(1f, 0.91f, 0.78f);
        key.intensity = 1.5f;
        key.shadows = LightShadows.Soft;
        key.transform.rotation = Quaternion.Euler(52f, -38f, 0f);

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.45f, 0.5f, 0.53f);
        RenderSettings.fogStartDistance = 22f;
        RenderSettings.fogEndDistance = 55f;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.47f, 0.56f, 0.65f);
        RenderSettings.ambientEquatorColor = new Color(0.34f, 0.4f, 0.45f);
        RenderSettings.ambientGroundColor = new Color(0.22f, 0.25f, 0.28f);
        RenderSettings.ambientIntensity = 1.02f;
        RenderSettings.skybox = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Presentation/IndustrialSky.mat");
        FinalLightingBuilder.ApplyLightingAndPostToOpenScene();

        Material concrete = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Presentation/YardConcrete.mat");
        Material steel = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Presentation/LauncherMetal.mat");
        Material accent = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Presentation/SafetyYellow.mat");
        CreateTitlePrimitive(PrimitiveType.Cube, "Test Yard", new Vector3(1f, -0.35f, 7f), Quaternion.identity, new Vector3(30f, 0.5f, 32f), concrete);
        CreateTitlePrimitive(PrimitiveType.Cube, "Frame Foundation", new Vector3(5.5f, 0.15f, 7f), Quaternion.identity, new Vector3(8.5f, 0.3f, 3.6f), concrete);
        CreateTitlePrimitive(PrimitiveType.Cube, "Frame Column Left", new Vector3(3.1f, 2.7f, 7f), Quaternion.identity, new Vector3(0.48f, 5.2f, 0.55f), steel);
        CreateTitlePrimitive(PrimitiveType.Cube, "Frame Column Right", new Vector3(7.9f, 2.7f, 7f), Quaternion.identity, new Vector3(0.48f, 5.2f, 0.55f), steel);
        CreateTitlePrimitive(PrimitiveType.Cube, "Frame Top Beam", new Vector3(5.5f, 5.15f, 7f), Quaternion.identity, new Vector3(5.4f, 0.5f, 0.62f), steel);
        CreateTitlePrimitive(PrimitiveType.Cube, "Frame Mid Beam", new Vector3(5.5f, 2.65f, 7f), Quaternion.identity, new Vector3(5.1f, 0.34f, 0.5f), steel);
        CreateTitlePrimitive(PrimitiveType.Cube, "Frame Brace A", new Vector3(4.25f, 2.65f, 6.65f), Quaternion.Euler(0f, 0f, 34f), new Vector3(0.24f, 4.4f, 0.28f), steel);
        CreateTitlePrimitive(PrimitiveType.Cube, "Frame Brace B", new Vector3(6.75f, 2.65f, 6.65f), Quaternion.Euler(0f, 0f, -34f), new Vector3(0.24f, 4.4f, 0.28f), steel);
        CreateTitlePrimitive(PrimitiveType.Cube, "Hazard Rail", new Vector3(5.5f, 0.48f, 5.2f), Quaternion.identity, new Vector3(7.2f, 0.16f, 0.18f), accent);

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        GameObject canvasObject = new("Title Screen", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        Image overlay = CreateTitleImage(canvas.transform, "Title Overlay", Vector2.zero, new Vector2(1920f, 1080f));
        overlay.color = new Color(0.015f, 0.028f, 0.035f, 0.32f);
        Text demolition = CreateTitleText(canvas.transform, font, "DEMOLITION", 70, new Vector2(-570f, 190f), new Vector2(700f, 90f));
        demolition.alignment = TextAnchor.MiddleLeft;
        demolition.fontStyle = FontStyle.Bold;
        Text lab = CreateTitleText(canvas.transform, font, "LAB", 96, new Vector2(-570f, 105f), new Vector2(700f, 110f));
        lab.alignment = TextAnchor.MiddleLeft;
        lab.color = new Color(0.96f, 0.58f, 0.1f);
        lab.fontStyle = FontStyle.Bold;
        Image titleAccent = CreateTitleImage(canvas.transform, "Title Accent", new Vector2(-565f, 35f), new Vector2(190f, 4f));
        titleAccent.color = new Color(0.96f, 0.49f, 0.08f, 0.95f);
        Text subtitle = CreateTitleText(canvas.transform, font, "STRUCTURAL DEMOLITION TEST", 25, new Vector2(-570f, -12f), new Vector2(700f, 50f));
        subtitle.alignment = TextAnchor.MiddleLeft;
        subtitle.color = new Color(0.78f, 0.85f, 0.87f);
        Button start = CreateTitleButton(canvas.transform, font, new Vector2(-570f, -115f));
        Text controls = CreateTitleText(canvas.transform, font, "WASD / ARROWS  —  AIM     SPACE  —  FIRE", 19, new Vector2(-570f, -205f), new Vector2(700f, 42f));
        controls.alignment = TextAnchor.MiddleLeft;
        controls.color = new Color(0.66f, 0.73f, 0.75f);

        GameObject menuObject = new("Presentation Menu Controller");
        PresentationMenuController menu = menuObject.AddComponent<PresentationMenuController>();
        PresentationAudioHooks audio = menuObject.AddComponent<PresentationAudioHooks>();
        AudioSource source = menuObject.GetComponent<AudioSource>();
        source.playOnAwake = false;
        PresentationEditorUtilities.AddPersistentClick(start, menu, menu.StartTest);
        PresentationEditorUtilities.AddPersistentClick(start, audio, audio.PlayUiClick);

        GameObject eventSystem = new("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<InputSystemUIInputModule>();
        EditorSceneManager.SaveScene(scene, TitleScenePath);
    }

    internal static void ValidateTitleScene()
    {
        EditorSceneManager.OpenScene(TitleScenePath, OpenSceneMode.Single);
        PresentationMenuController menu = Object.FindAnyObjectByType<PresentationMenuController>();
        Button start = GameObject.Find("Start Button")?.GetComponent<Button>();
        if (menu == null
            || menu.StartSceneName != StageOneSceneName
            || start == null
            || start.onClick.GetPersistentEventCount() < 1
            || Camera.main == null
            || GameObject.Find("Title Backing") != null
            || GameObject.Find("Title Overlay") == null
            || GameObject.Find("Frame Brace A") == null
            || Object.FindAnyObjectByType<Rigidbody>() != null
            || Object.FindAnyObjectByType<Collider>() != null)
        {
            throw new UnityException("Title scene presentation or START flow is incomplete.");
        }

        bool titleBeforeStageOne = false;
        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
        for (int index = 0; index < scenes.Length - 1; index++)
        {
            if (scenes[index].path == TitleScenePath
                && scenes[index + 1].path == StageScenePaths[0]
                && scenes[index].enabled)
            {
                titleBeforeStageOne = true;
                break;
            }
        }

        if (!titleBeforeStageOne)
        {
            throw new UnityException("Title scene is not enabled immediately before Stage01 in build settings.");
        }
    }

    internal static void ConfigureBuildSettings()
    {
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        scenes.RemoveAll(scene => scene.path == TitleScenePath);
        int stageOneIndex = scenes.FindIndex(scene => scene.path == StageScenePaths[0]);
        if (stageOneIndex < 0)
        {
            throw new UnityException("Stage01 must exist in build settings before adding the title scene.");
        }

        scenes.Insert(stageOneIndex, new EditorBuildSettingsScene(TitleScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    internal static void CreateTitlePrimitive(
        PrimitiveType type,
        string name,
        Vector3 position,
        Quaternion rotation,
        Vector3 scale,
        Material material)
    {
        GameObject visual = GameObject.CreatePrimitive(type);
        visual.name = name;
        visual.transform.position = position;
        visual.transform.rotation = rotation;
        visual.transform.localScale = scale;
        Object.DestroyImmediate(visual.GetComponent<Collider>());
        visual.GetComponent<Renderer>().sharedMaterial = material;
    }

    internal static Image CreateTitleImage(Transform parent, string name, Vector2 position, Vector2 size)
    {
        GameObject imageObject = new(name, typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        PresentationEditorUtilities.SetRect(imageObject.GetComponent<RectTransform>(), position, size, new Vector2(0.5f, 0.5f));
        Image image = imageObject.GetComponent<Image>();
        image.raycastTarget = false;
        return image;
    }

    internal static Text CreateTitleText(
        Transform parent,
        Font font,
        string content,
        int fontSize,
        Vector2 position,
        Vector2 size)
    {
        GameObject textObject = new(content, typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(parent, false);
        PresentationEditorUtilities.SetRect(textObject.GetComponent<RectTransform>(), position, size, new Vector2(0.5f, 0.5f));
        Text text = textObject.GetComponent<Text>();
        text.font = font;
        text.text = content;
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = new Color(0.9f, 0.93f, 0.92f);
        text.raycastTarget = false;
        return text;
    }

    internal static Button CreateTitleButton(Transform parent, Font font, Vector2 position)
    {
        GameObject buttonObject = new("Start Button", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        PresentationEditorUtilities.SetRect(buttonObject.GetComponent<RectTransform>(), position, new Vector2(250f, 62f), new Vector2(0.5f, 0.5f));
        Button button = buttonObject.GetComponent<Button>();
        FinalStageHudBuilder.StyleResultButton(button);
        Text label = CreateTitleText(buttonObject.transform, font, "START TEST", 26, Vector2.zero, new Vector2(250f, 62f));
        RectTransform labelRect = label.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        return button;
    }
}
