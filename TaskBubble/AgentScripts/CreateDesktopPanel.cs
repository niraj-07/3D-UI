using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using TaskBubble.Desktop;

public static class CreateDesktopPanel
{
    private const string Path = "Assets/TaskBubble/Desktop/DesktopPanel.prefab";
    private static readonly Color TextColor = new Color(0.91f, 0.95f, 1f);

    public static string Create()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Leave Play Mode first.");
        if (AssetDatabase.LoadAssetAtPath<GameObject>(Path) != null)
            throw new InvalidOperationException("DesktopPanel already exists; edit the prefab instead.");
        if (TMP_Settings.defaultFontAsset == null)
            throw new InvalidOperationException("TMP essentials must be present first.");
        if (!AssetDatabase.IsValidFolder("Assets/TaskBubble/Desktop"))
            AssetDatabase.CreateFolder("Assets/TaskBubble", "Desktop");

        var previousScene = SceneManager.GetActiveScene();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        try
        {
            var root = new GameObject("DesktopPanel", typeof(RectTransform), typeof(Canvas),
                typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = root.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 0.5f;

            var card = new GameObject("Card", typeof(RectTransform), typeof(UnityEngine.UI.Image),
                typeof(UnityEngine.UI.VerticalLayoutGroup));
            card.transform.SetParent(root.transform, false);
            var rect = card.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = new Vector2(-32f, 0f);
            rect.sizeDelta = new Vector2(380f, 400f);
            var background = card.GetComponent<UnityEngine.UI.Image>();
            background.color = new Color(0.065f, 0.10f, 0.16f);
            background.raycastTarget = false;
            var layout = card.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
            layout.padding = new RectOffset(28, 28, 24, 24);
            layout.spacing = 10;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            Text(card.transform, "Title", "Desktop editor", 28, 42).fontStyle = FontStyles.Bold;
            var status = Text(card.transform, "TaskStatus", "Idle", 16, 48);
            status.color = new Color(0.64f, 0.73f, 0.83f);
            status.textWrappingMode = TextWrappingModes.Normal;
            Text(card.transform, "PartName", "No part selected", 22, 40);
            Text(card.transform, "RotationAxis", "ROTATION", 14, 24);
            Text(card.transform, "AngleValue", "0.0\u00b0", 40, 60);

            var resources = new UnityEngine.UI.DefaultControls.Resources
            {
                background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"),
                standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"),
                knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd")
            };
            var sliderRoot = UnityEngine.UI.DefaultControls.CreateSlider(resources);
            sliderRoot.name = "RotationSlider";
            sliderRoot.transform.SetParent(card.transform, false);
            Height(sliderRoot, 44);
            var slider = sliderRoot.GetComponent<UnityEngine.UI.Slider>();
            slider.minValue = -45f;
            slider.maxValue = 45f;
            slider.SetValueWithoutNotify(0f);
            slider.interactable = false;
            slider.fillRect.GetComponent<UnityEngine.UI.Image>().color = new Color(0.08f, 0.57f, 0.51f);
            slider.fillRect.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
            sliderRoot.transform.Find("Background").GetComponent<UnityEngine.UI.Image>().color = new Color(0.12f, 0.18f, 0.28f);
            var handleArea = (RectTransform)slider.handleRect.parent;
            handleArea.anchorMin = new Vector2(handleArea.anchorMin.x, 0.5f);
            handleArea.anchorMax = new Vector2(handleArea.anchorMax.x, 0.5f);
            handleArea.sizeDelta = new Vector2(handleArea.sizeDelta.x, 20f);
            var limits = new GameObject("RotationLimits", typeof(RectTransform), typeof(UnityEngine.UI.HorizontalLayoutGroup));
            limits.transform.SetParent(card.transform, false);
            Height(limits, 24);
            var limitsLayout = limits.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            limitsLayout.childControlWidth = limitsLayout.childControlHeight = true;
            limitsLayout.childForceExpandWidth = true;
            limitsLayout.childForceExpandHeight = false;
            Text(limits.transform, "MinimumAngle", "-45\u00b0", 16, 24).color = new Color(0.64f, 0.73f, 0.83f);
            var maximum = Text(limits.transform, "MaximumAngle", "+45\u00b0", 16, 24);
            maximum.color = new Color(0.64f, 0.73f, 0.83f);
            maximum.alignment = TextAlignmentOptions.Right;

            var fields = new SerializedObject(root.AddComponent<DesktopEditingPanel>());
            fields.FindProperty("rotationSlider").objectReferenceValue = slider;
            foreach (var binding in new[] { ("taskStatus", "TaskStatus"), ("partName", "PartName"),
                ("rotationAxis", "RotationAxis"), ("angleValue", "AngleValue"),
                ("minimumAngle", "RotationLimits/MinimumAngle"), ("maximumAngle", "RotationLimits/MaximumAngle") })
                fields.FindProperty(binding.Item1).objectReferenceValue = card.transform.Find(binding.Item2).GetComponent<TMP_Text>();
            fields.ApplyModifiedPropertiesWithoutUndo();

            if (PrefabUtility.SaveAsPrefabAsset(root, Path) == null)
                throw new InvalidOperationException("Could not save DesktopPanel prefab.");
            return Path;
        }
        finally
        {
            SceneManager.SetActiveScene(previousScene);
            EditorSceneManager.CloseScene(scene, true);
        }
    }

    private static TMP_Text Text(Transform parent, string name, string value, int size, int height)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var text = go.GetComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset;
        text.text = value;
        text.fontSize = size;
        text.color = TextColor;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        Height(go, height);
        return text;
    }

    private static void Height(GameObject go, int height)
    {
        var element = go.AddComponent<UnityEngine.UI.LayoutElement>();
        element.minHeight = element.preferredHeight = height;
        element.flexibleHeight = 0;
    }
}
