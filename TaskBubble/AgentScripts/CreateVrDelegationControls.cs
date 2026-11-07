using System;
using System.IO;
using TaskBubble.VR;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CreateVrDelegationControls
{
    public static object Build()
    {
        const string path = "Assets/TaskBubble/VR/VrDelegationControls.prefab";
        const string rigPath = "Assets/TaskBubble/VR/PcVrRig.prefab";
        if (Application.isPlaying) throw new InvalidOperationException("Leave Play Mode first.");
        if (File.Exists(path)) throw new InvalidOperationException("Delegate prefab already exists.");
        var stage = PrefabStageUtility.GetCurrentPrefabStage();
        if (stage != null && stage.assetPath == rigPath && stage.scene.isDirty)
            throw new InvalidOperationException("Save or undo rig edits first.");
        var scene = EditorSceneManager.NewPreviewScene();
        try
        {
            var panel = new GameObject("VrDelegationControls", typeof(RectTransform), typeof(Canvas),
                typeof(UnityEngine.UI.GraphicRaycaster), typeof(UnityEngine.UI.Image));
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(panel, scene);
            panel.layer = 5;
            panel.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            panel.GetComponent<RectTransform>().sizeDelta = new Vector2(480, 260);
            panel.transform.localScale = Vector3.one * .001f;
            panel.GetComponent<UnityEngine.UI.Image>().color = new Color(.04f, .08f, .12f, .98f);
            var title = Text(panel.transform, "Title", "Request help", 26, 86, 40);
            title.fontStyle = FontStyles.Bold;
            var selected = Text(panel.transform, "SelectedPart", "No part selected", 21, 42, 40);
            var feedback = Text(panel.transform, "Feedback", "Connect as VR to request help", 19, -8, 54);
            feedback.textWrappingMode = TextWrappingModes.Normal;
            var buttonObject = new GameObject("Delegate", typeof(RectTransform), typeof(UnityEngine.UI.Image),
                typeof(UnityEngine.UI.Button), typeof(BoxCollider));
            buttonObject.layer = 5; buttonObject.transform.SetParent(panel.transform, false);
            var rect = buttonObject.GetComponent<RectTransform>(); rect.sizeDelta = new Vector2(432, 64);
            rect.anchoredPosition = new Vector2(0, -82);
            var image = buttonObject.GetComponent<UnityEngine.UI.Image>(); image.color = new Color(.08f, .57f, .51f);
            var button = buttonObject.GetComponent<UnityEngine.UI.Button>(); button.targetGraphic = image; button.interactable = false;
            buttonObject.GetComponent<BoxCollider>().size = new Vector3(432, 64, 8);
            var label = Text(buttonObject.transform, "Label", "Delegate", 24, 0, 64);
            label.fontStyle = FontStyles.Bold;
            var fields = new SerializedObject(panel.AddComponent<VrDelegationControls>());
            fields.FindProperty("delegateButton").objectReferenceValue = button;
            fields.FindProperty("selectedPartLabel").objectReferenceValue = selected;
            fields.FindProperty("feedback").objectReferenceValue = feedback;
            fields.ApplyModifiedPropertiesWithoutUndo();
            if (PrefabUtility.SaveAsPrefabAsset(panel, path) == null) throw new InvalidOperationException("Could not save panel.");
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
        var root = PrefabUtility.LoadPrefabContents(rigPath);
        try
        {
            if (root.GetComponentInChildren<VrDelegationControls>(true) != null) throw new InvalidOperationException("Rig already has Delegate controls.");
            var panel = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path), root.scene);
            panel.transform.SetParent(root.transform, false);
            panel.transform.localPosition = new Vector3(-.45f, 1.25f, 1f);
            var fields = new SerializedObject(panel.GetComponent<VrDelegationControls>());
            fields.FindProperty("selection").objectReferenceValue = root.GetComponentInChildren<VrPartSelection>(true);
            fields.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, rigPath);
            return new { success = true, panel = path, nestedInRig = true, sceneReferences = "Session and connection assigned during integration" };
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static TMP_Text Text(Transform parent, string name, string value, int size, float y, float height)
    {
        var root = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        root.layer = 5; root.transform.SetParent(parent, false);
        var text = root.GetComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset; text.fontSize = size; text.text = value;
        text.color = new Color(.91f, .95f, 1f); text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        text.rectTransform.sizeDelta = new Vector2(432, height);
        text.rectTransform.anchoredPosition = new Vector2(0, y);
        return text;
    }
}
