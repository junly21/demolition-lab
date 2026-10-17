using DemolitionLab;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

internal static class StageHudFactory
{
    internal static UiReferences CreateUi(VerticalSliceStageController controller)
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        GameObject canvasObject = new("Vertical Slice HUD");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasObject.AddComponent<GraphicRaycaster>();

        Text stage = CreateText(canvas.transform, "Stage Label", font, "STAGE 01", 30, TextAnchor.UpperLeft);
        SetRect(stage.rectTransform, new Vector2(24f, -24f), new Vector2(260f, 50f), new Vector2(0f, 1f));
        Text shots = CreateText(canvas.transform, "Shots Label", font, "Shots: 3", 30, TextAnchor.UpperRight);
        SetRect(shots.rectTransform, new Vector2(-24f, -24f), new Vector2(260f, 50f), new Vector2(1f, 1f));
        Text destruction = CreateText(canvas.transform, "Destruction Label", font, "Destruction: 0%", 28, TextAnchor.LowerCenter);
        SetRect(destruction.rectTransform, new Vector2(0f, 28f), new Vector2(420f, 50f), new Vector2(0.5f, 0f));

        GameObject panel = new("End State Panel");
        panel.transform.SetParent(canvas.transform, false);
        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.04f, 0.05f, 0.07f, 0.94f);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        SetRect(panelRect, Vector2.zero, new Vector2(520f, 340f), new Vector2(0.5f, 0.5f));

        Text resultTitle = CreateText(panel.transform, "Result Title", font, "STAGE CLEAR", 42, TextAnchor.MiddleCenter);
        SetRect(resultTitle.rectTransform, new Vector2(0f, 105f), new Vector2(460f, 70f), new Vector2(0.5f, 0.5f));
        Text resultDetails = CreateText(panel.transform, "Result Details", font, "Shots Used: 0\nDestruction: 0%", 28, TextAnchor.MiddleCenter);
        SetRect(resultDetails.rectTransform, new Vector2(0f, 25f), new Vector2(460f, 100f), new Vector2(0.5f, 0.5f));
        Button retry = CreateButton(panel.transform, "Retry Button", font, "RETRY", new Vector2(-110f, -100f));
        Button next = CreateButton(panel.transform, "Next Button", font, "NEXT", new Vector2(110f, -100f));

        if (Object.FindAnyObjectByType<EventSystem>() == null)
        {
            GameObject eventSystemObject = new("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<InputSystemUIInputModule>();
        }

        return new UiReferences(stage, shots, destruction, panel, resultTitle, resultDetails, retry, next);
    }

    internal static Text CreateText(
        Transform parent,
        string name,
        Font font,
        string content,
        int fontSize,
        TextAnchor alignment)
    {
        GameObject textObject = new(name);
        textObject.transform.SetParent(parent, false);
        Text text = textObject.AddComponent<Text>();
        text.font = font;
        text.text = content;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }

    internal static Button CreateButton(
        Transform parent,
        string name,
        Font font,
        string label,
        Vector2 position)
    {
        GameObject buttonObject = new(name);
        buttonObject.transform.SetParent(parent, false);
        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.18f, 0.48f, 0.78f, 1f);
        Button button = buttonObject.AddComponent<Button>();
        SetRect(buttonObject.GetComponent<RectTransform>(), position, new Vector2(180f, 58f), new Vector2(0.5f, 0.5f));
        Text text = CreateText(buttonObject.transform, "Label", font, label, 24, TextAnchor.MiddleCenter);
        RectTransform textRect = text.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        return button;
    }

    internal static void SetRect(
        RectTransform rect,
        Vector2 anchoredPosition,
        Vector2 size,
        Vector2 anchor)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
    }

    internal readonly struct UiReferences
    {
        public UiReferences(
            Text stageLabel,
            Text shots,
            Text destruction,
            GameObject endPanel,
            Text resultTitle,
            Text resultDetails,
            Button retryButton,
            Button nextButton)
        {
            StageLabel = stageLabel;
            Shots = shots;
            Destruction = destruction;
            EndPanel = endPanel;
            ResultTitle = resultTitle;
            ResultDetails = resultDetails;
            RetryButton = retryButton;
            NextButton = nextButton;
        }

        public Text StageLabel { get; }
        public Text Shots { get; }
        public Text Destruction { get; }
        public GameObject EndPanel { get; }
        public Text ResultTitle { get; }
        public Text ResultDetails { get; }
        public Button RetryButton { get; }
        public Button NextButton { get; }
    }
}
