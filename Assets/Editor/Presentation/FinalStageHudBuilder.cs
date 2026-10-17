using DemolitionLab;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

internal static class FinalStageHudBuilder
{
    private const string HudPresentationName = "Final HUD Presentation";

    internal static void ApplyHudToOpenScene()
    {
        VerticalSliceStageController controller = Object.FindAnyObjectByType<VerticalSliceStageController>();
        LauncherController launcher = Object.FindAnyObjectByType<LauncherController>();
        Camera camera = Camera.main;
        if (controller == null || launcher == null || camera == null)
        {
            throw new UnityException("Final HUD requires the stage controller, launcher, and Main Camera.");
        }

        var serializedController = new SerializedObject(controller);
        var serializedHud = new SerializedObject(controller.GetComponent<StageHudView>());
        var serializedFeedback = new SerializedObject(controller.GetComponent<StageFeedbackController>());
        Text stage = (Text)serializedHud.FindProperty("stageLabelText").objectReferenceValue;
        Text shots = (Text)serializedHud.FindProperty("shotsText").objectReferenceValue;
        Text destruction = (Text)serializedHud.FindProperty("destructionText").objectReferenceValue;
        GameObject endPanel = (GameObject)serializedHud.FindProperty("endPanel").objectReferenceValue;
        Text resultTitle = (Text)serializedHud.FindProperty("resultTitleText").objectReferenceValue;
        Text resultDetails = (Text)serializedHud.FindProperty("resultDetailsText").objectReferenceValue;
        Button next = (Button)serializedHud.FindProperty("nextButton").objectReferenceValue;
        PresentationAudioHooks audio = (PresentationAudioHooks)serializedFeedback.FindProperty("audioHooks").objectReferenceValue;
        Canvas canvas = stage != null ? stage.GetComponentInParent<Canvas>() : null;
        if (stage == null || shots == null || destruction == null || endPanel == null
            || resultTitle == null || resultDetails == null || next == null || audio == null || canvas == null)
        {
            throw new UnityException("Final HUD requires all existing HUD and audio references.");
        }

        Transform existing = canvas.transform.Find(HudPresentationName);
        if (existing != null)
        {
            Object.DestroyImmediate(existing.gameObject);
        }

        GameObject root = new(HudPresentationName, typeof(RectTransform));
        root.transform.SetParent(canvas.transform, false);
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;
        root.transform.SetAsFirstSibling();

        ConfigureCompactHudText(stage, 30, new Vector2(30f, -22f), new Vector2(300f, 44f), new Vector2(0f, 1f), TextAnchor.UpperLeft);
        ConfigureCompactHudText(shots, 28, new Vector2(-30f, -22f), new Vector2(300f, 44f), new Vector2(1f, 1f), TextAnchor.UpperRight);
        ConfigureCompactHudText(destruction, 24, new Vector2(0f, 43f), new Vector2(380f, 36f), new Vector2(0.5f, 0f), TextAnchor.LowerCenter);
        CreateHudAccentLine(root.transform, "Stage Accent", new Vector2(30f, -72f), new Vector2(118f, 3f), new Vector2(0f, 1f));
        CreateHudAccentLine(root.transform, "Shots Accent", new Vector2(-30f, -72f), new Vector2(118f, 3f), new Vector2(1f, 1f));
        Image destructionFill = CreateProgressBar(root.transform);

        Image endPanelImage = endPanel.GetComponent<Image>();
        endPanelImage.color = new Color(0.025f, 0.04f, 0.05f, 0.9f);
        endPanel.GetComponent<RectTransform>().sizeDelta = new Vector2(500f, 320f);
        Outline panelOutline = endPanel.GetComponent<Outline>() ?? endPanel.AddComponent<Outline>();
        panelOutline.effectColor = new Color(0.95f, 0.5f, 0.08f, 0.62f);
        panelOutline.effectDistance = new Vector2(1.5f, -1.5f);
        resultTitle.color = new Color(1f, 0.66f, 0.18f);
        resultTitle.fontSize = 40;
        resultDetails.color = new Color(0.82f, 0.88f, 0.9f);

        Button retry = endPanel.transform.Find("Retry Button")?.GetComponent<Button>();
        StyleResultButton(retry);
        StyleResultButton(next);
        PresentationEditorUtilities.AddPersistentClick(retry, audio, audio.PlayUiClick);
        PresentationEditorUtilities.AddPersistentClick(next, audio, audio.PlayUiClick);
        if (SceneManager.GetActiveScene().name == "VerticalSlice_Stage05")
        {
            Text retryLabel = retry != null ? retry.GetComponentInChildren<Text>() : null;
            if (retryLabel != null)
            {
                retryLabel.text = "RESTART TEST";
            }
        }

        serializedHud.FindProperty("destructionFill").objectReferenceValue = destructionFill;
        serializedController.ApplyModifiedPropertiesWithoutUndo();
        serializedHud.ApplyModifiedPropertiesWithoutUndo();
        serializedFeedback.ApplyModifiedPropertiesWithoutUndo();

        var serializedLauncher = new SerializedObject(launcher);
        LineRenderer trajectory = (LineRenderer)serializedLauncher.FindProperty("aimLine").objectReferenceValue;
        PresentationCaptureMode captureMode = camera.GetComponent<PresentationCaptureMode>();
        if (captureMode == null)
        {
            captureMode = camera.gameObject.AddComponent<PresentationCaptureMode>();
        }

        var serializedCapture = new SerializedObject(captureMode);
        serializedCapture.FindProperty("trajectory").objectReferenceValue = trajectory;
        serializedCapture.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    internal static void ValidateHudInOpenScene(string scenePath)
    {
        VerticalSliceStageController controller = Object.FindAnyObjectByType<VerticalSliceStageController>();
        PresentationCaptureMode capture = Camera.main != null
            ? Camera.main.GetComponent<PresentationCaptureMode>()
            : null;
        if (controller == null || capture == null)
        {
            throw new UnityException($"{scenePath} final HUD or screenshot mode is missing.");
        }

        var serializedController = new SerializedObject(controller);
        var serializedHud = new SerializedObject(controller.GetComponent<StageHudView>());
        var serializedFeedback = new SerializedObject(controller.GetComponent<StageFeedbackController>());
        Image fill = (Image)serializedHud.FindProperty("destructionFill").objectReferenceValue;
        GameObject panel = (GameObject)serializedHud.FindProperty("endPanel").objectReferenceValue;
        Text stage = (Text)serializedHud.FindProperty("stageLabelText").objectReferenceValue;
        Canvas canvas = stage != null ? stage.GetComponentInParent<Canvas>() : null;
        Transform hudRoot = canvas != null ? canvas.transform.Find(HudPresentationName) : null;
        if (fill == null
            || fill.type != Image.Type.Filled
            || fill.fillMethod != Image.FillMethod.Horizontal
            || panel == null
            || panel.GetComponent<Outline>() == null
            || hudRoot == null
            || hudRoot.Find("Stage Panel") != null
            || hudRoot.Find("Shots Panel") != null
            || hudRoot.Find("Destruction Panel") != null
            || hudRoot.Find("Stage Accent") == null
            || hudRoot.Find("Shots Accent") == null)
        {
            throw new UnityException($"{scenePath} final HUD presentation is incomplete.");
        }

        var serializedCapture = new SerializedObject(capture);
        if (serializedCapture.FindProperty("trajectory").objectReferenceValue == null)
        {
            throw new UnityException($"{scenePath} screenshot mode is missing its trajectory reference.");
        }
    }

    internal static void ConfigureCompactHudText(
        Text text,
        int fontSize,
        Vector2 position,
        Vector2 size,
        Vector2 anchor,
        TextAnchor alignment)
    {
        text.fontSize = fontSize;
        text.fontStyle = FontStyle.Bold;
        text.alignment = alignment;
        text.color = new Color(0.92f, 0.95f, 0.95f);
        PresentationEditorUtilities.SetRect(text.rectTransform, position, size, anchor);
        Outline outline = text.GetComponent<Outline>();
        if (outline != null)
        {
            outline.effectColor = new Color(0.015f, 0.025f, 0.03f, 0.78f);
            outline.effectDistance = new Vector2(1f, -1f);
        }
    }

    internal static Image CreateHudAccentLine(
        Transform parent,
        string name,
        Vector2 position,
        Vector2 size,
        Vector2 anchor)
    {
        GameObject lineObject = new(name, typeof(RectTransform), typeof(Image));
        lineObject.transform.SetParent(parent, false);
        PresentationEditorUtilities.SetRect(lineObject.GetComponent<RectTransform>(), position, size, anchor);
        Image image = lineObject.GetComponent<Image>();
        image.color = new Color(0.95f, 0.49f, 0.08f, 0.9f);
        image.raycastTarget = false;
        return image;
    }

    internal static Image CreateProgressBar(Transform parent)
    {
        GameObject background = new("Destruction Progress", typeof(RectTransform), typeof(Image));
        background.transform.SetParent(parent, false);
        PresentationEditorUtilities.SetRect(background.GetComponent<RectTransform>(), new Vector2(0f, 28f), new Vector2(380f, 6f), new Vector2(0.5f, 0f));
        Image backgroundImage = background.GetComponent<Image>();
        backgroundImage.color = new Color(0.04f, 0.07f, 0.08f, 0.72f);
        backgroundImage.raycastTarget = false;

        GameObject fillObject = new("Fill", typeof(RectTransform), typeof(Image));
        fillObject.transform.SetParent(background.transform, false);
        RectTransform fillRect = fillObject.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(1f, 1f);
        fillRect.offsetMax = new Vector2(-1f, -1f);
        Image fill = fillObject.GetComponent<Image>();
        fill.color = new Color(0.95f, 0.49f, 0.08f, 1f);
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        fill.fillAmount = 0f;
        fill.raycastTarget = false;
        return fill;
    }

    internal static void StyleResultButton(Button button)
    {
        if (button == null)
        {
            return;
        }

        Image image = button.GetComponent<Image>();
        image.color = new Color(0.08f, 0.15f, 0.17f, 0.88f);
        ColorBlock colors = button.colors;
        colors.normalColor = image.color;
        colors.highlightedColor = new Color(0.16f, 0.32f, 0.36f, 1f);
        colors.pressedColor = new Color(0.95f, 0.49f, 0.08f, 1f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;
        Outline outline = button.GetComponent<Outline>() ?? button.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.95f, 0.49f, 0.08f, 0.7f);
        outline.effectDistance = new Vector2(1f, -1f);
        Text label = button.GetComponentInChildren<Text>();
        if (label != null)
        {
            label.color = new Color(0.94f, 0.96f, 0.94f);
            label.fontStyle = FontStyle.Bold;
        }
    }
}
