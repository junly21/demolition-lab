using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;

internal static class PresentationEditorUtilities
{
    internal static void SetRect(
        RectTransform rect,
        Vector2 position,
        Vector2 size,
        Vector2 anchor)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    internal static void AddPersistentClick(Button button, Object target, UnityEngine.Events.UnityAction action)
    {
        if (button == null || target == null)
        {
            return;
        }

        for (int index = 0; index < button.onClick.GetPersistentEventCount(); index++)
        {
            if (button.onClick.GetPersistentTarget(index) == target
                && button.onClick.GetPersistentMethodName(index) == action.Method.Name)
            {
                return;
            }
        }

        UnityEventTools.AddPersistentListener(button.onClick, action);
    }

    internal static void RemoveRootObject(string objectName)
    {
        GameObject existing = GameObject.Find(objectName);
        if (existing != null)
        {
            Object.DestroyImmediate(existing);
        }
    }
}
