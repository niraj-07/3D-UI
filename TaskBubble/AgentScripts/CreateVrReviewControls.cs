using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using TaskBubble.Review;

public static class CreateVrReviewControls
{
    public static string Create()
    {
        const string path = "Assets/TaskBubble/Review/VrReviewControls.prefab";
        if (Application.isPlaying) throw new InvalidOperationException("Leave Play Mode first.");
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
            throw new InvalidOperationException("VR review controls already exist; edit the prefab instead.");
        if (TMP_Settings.defaultFontAsset == null) throw new InvalidOperationException("TMP essentials must be present first.");
        var previous = SceneManager.GetActiveScene();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        try
        {
            var root = new GameObject("VrReviewControls", typeof(RectTransform), typeof(Canvas),
                typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster),
                typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.VerticalLayoutGroup));
            root.layer = 5;
            var rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(560f, 200f);
            rect.localScale = Vector3.one * 0.001f;
            rect.position = new Vector3(0f, 1.3f, 1.2f);
            root.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            root.GetComponent<UnityEngine.UI.CanvasScaler>().dynamicPixelsPerUnit = 10f;
            root.GetComponent<UnityEngine.UI.Image>().color = new Color(0.065f, 0.10f, 0.16f);
            root.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
            var layout = root.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
            layout.padding = new RectOffset(24, 24, 24, 24);
            layout.spacing = 16f;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            var title = Text(root.transform, "Title", "Review proposal", 28);
            title.fontStyle = FontStyles.Bold;
            title.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().preferredHeight = 48;

            var row = new GameObject("Decisions", typeof(RectTransform), typeof(UnityEngine.UI.HorizontalLayoutGroup), typeof(UnityEngine.UI.LayoutElement));
            row.layer = 5;
            row.transform.SetParent(root.transform, false);
            row.GetComponent<UnityEngine.UI.LayoutElement>().preferredHeight = 72;
            var rowLayout = row.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            rowLayout.spacing = 16;
            rowLayout.childControlWidth = rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = rowLayout.childForceExpandHeight = true;
            var accept = Button(row.transform, "Accept", new Color(0.08f, 0.57f, 0.51f));
            var reject = Button(row.transform, "Reject", new Color(0.62f, 0.20f, 0.25f));
            var fields = new SerializedObject(root.AddComponent<VrReviewControls>());
            fields.FindProperty("acceptButton").objectReferenceValue = accept;
            fields.FindProperty("rejectButton").objectReferenceValue = reject;
            fields.ApplyModifiedPropertiesWithoutUndo();
            AddResetAndFeedback(root);
            if (PrefabUtility.SaveAsPrefabAsset(root, path) == null) throw new InvalidOperationException("Could not save VR review controls.");
            return path;
        }
        finally
        {
            SceneManager.SetActiveScene(previous);
            EditorSceneManager.CloseScene(scene, true);
        }
    }

    private static UnityEngine.UI.Button Button(Transform parent, string name, Color color)
    {
        var root = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
        root.layer = 5;
        root.transform.SetParent(parent, false);
        var image = root.GetComponent<UnityEngine.UI.Image>(); image.color = color; image.raycastTarget = true;
        var button = root.GetComponent<UnityEngine.UI.Button>(); button.targetGraphic = image; button.interactable = false;
        var text = Text(root.transform, "Label", name, 24); text.alignment = TextAlignmentOptions.Center; text.fontStyle = FontStyles.Bold;
        text.rectTransform.anchorMin = Vector2.zero; text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = new Vector2(12f, 0f); text.rectTransform.offsetMax = new Vector2(-12f, 0f);
        return button;
    }

    public static string AddResetAndFeedbackToExistingPrefab()
    {
        const string path = "Assets/TaskBubble/Review/VrReviewControls.prefab";
        if (Application.isPlaying) throw new InvalidOperationException("Leave Play Mode first.");
        var root = PrefabUtility.LoadPrefabContents(path);
        try { AddResetAndFeedback(root); PrefabUtility.SaveAsPrefabAsset(root, path); return path; }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static void AddResetAndFeedback(GameObject root)
    {
        if (root.transform.Find("Reset") != null || root.transform.Find("Feedback") != null)
            throw new InvalidOperationException("Reset and feedback already exist; edit them instead.");
        root.GetComponent<RectTransform>().sizeDelta = new Vector2(560, 400);
        var feedback = Text(root.transform, "Feedback", "Disconnected", 22);
        feedback.textWrappingMode = TextWrappingModes.Normal;
        feedback.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().preferredHeight = 96;
        feedback.transform.SetSiblingIndex(1);
        var reset = Button(root.transform, "Reset", new Color(0.20f, 0.27f, 0.37f));
        reset.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().preferredHeight = 64;
        var fields = new SerializedObject(root.GetComponent<VrReviewControls>());
        fields.FindProperty("resetButton").objectReferenceValue = reset;
        fields.FindProperty("feedback").objectReferenceValue = feedback;
        fields.ApplyModifiedPropertiesWithoutUndo();
    }

    private static TMP_Text Text(Transform parent, string name, string value, int size)
    {
        var root = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)); root.layer = 5;
        root.transform.SetParent(parent, false);
        var text = root.GetComponent<TextMeshProUGUI>(); text.font = TMP_Settings.defaultFontAsset;
        text.text = value; text.fontSize = size; text.color = new Color(0.91f, 0.95f, 1f);
        text.raycastTarget = false; text.textWrappingMode = TextWrappingModes.NoWrap;
        return text;
    }
}
