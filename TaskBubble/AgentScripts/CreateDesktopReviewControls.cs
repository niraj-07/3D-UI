using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using TaskBubble.Review;

public static class CreateDesktopReviewControls
{
    public static string Create()
    {
        const string path = "Assets/TaskBubble/Review/DesktopReviewControls.prefab";
        if (Application.isPlaying) throw new InvalidOperationException("Leave Play Mode first.");
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
            throw new InvalidOperationException("Desktop review controls already exist; edit the prefab instead.");
        if (TMP_Settings.defaultFontAsset == null) throw new InvalidOperationException("TMP essentials must be present first.");

        var previous = SceneManager.GetActiveScene();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        try
        {
            // Nested under DesktopPanel's Canvas during integration; no second EventSystem or Canvas.
            var root = new GameObject("DesktopReviewControls", typeof(RectTransform),
                typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
            root.layer = 5;
            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = new Vector2(-60f, -240f);
            rect.sizeDelta = new Vector2(324f, 48f);
            var image = root.GetComponent<UnityEngine.UI.Image>();
            image.color = new Color(0.08f, 0.57f, 0.51f);
            image.raycastTarget = true;
            var button = root.GetComponent<UnityEngine.UI.Button>();
            button.targetGraphic = image;
            button.interactable = false;

            var textRoot = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            textRoot.layer = 5;
            textRoot.transform.SetParent(root.transform, false);
            var text = textRoot.GetComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            text.text = "Submit for review";
            text.fontSize = 18;
            text.fontStyle = FontStyles.Bold;
            text.color = new Color(0.91f, 0.95f, 1f);
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = new Vector2(12f, 0f);
            text.rectTransform.offsetMax = new Vector2(-12f, 0f);

            var fields = new SerializedObject(root.AddComponent<DesktopReviewControls>());
            fields.FindProperty("submitButton").objectReferenceValue = button;
            fields.ApplyModifiedPropertiesWithoutUndo();
            AddFeedback(root);
            if (PrefabUtility.SaveAsPrefabAsset(root, path) == null)
                throw new InvalidOperationException("Could not save desktop review controls.");
            return path;
        }
        finally
        {
            SceneManager.SetActiveScene(previous);
            EditorSceneManager.CloseScene(scene, true);
        }
    }

    public static string AddFeedbackToExistingPrefab()
    {
        const string path = "Assets/TaskBubble/Review/DesktopReviewControls.prefab";
        if (Application.isPlaying) throw new InvalidOperationException("Leave Play Mode first.");
        var root = PrefabUtility.LoadPrefabContents(path);
        try { AddFeedback(root); PrefabUtility.SaveAsPrefabAsset(root, path); return path; }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static void AddFeedback(GameObject root)
    {
        if (root.transform.Find("Feedback") != null) throw new InvalidOperationException("Feedback already exists; edit it instead.");
        var textRoot = new GameObject("Feedback", typeof(RectTransform), typeof(TextMeshProUGUI));
        textRoot.layer = 5;
        textRoot.transform.SetParent(root.transform, false);
        var rect = textRoot.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f); rect.anchorMax = new Vector2(1f, 0f); rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -12f); rect.sizeDelta = new Vector2(0f, 72f);
        var text = textRoot.GetComponent<TextMeshProUGUI>(); text.font = TMP_Settings.defaultFontAsset;
        text.text = "Disconnected"; text.fontSize = 16; text.color = new Color(0.64f, 0.73f, 0.83f);
        text.textWrappingMode = TextWrappingModes.Normal; text.raycastTarget = false;
        var fields = new SerializedObject(root.GetComponent<DesktopReviewControls>());
        fields.FindProperty("feedback").objectReferenceValue = text;
        fields.ApplyModifiedPropertiesWithoutUndo();
    }
}
