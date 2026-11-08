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
            AddCloseup(root);
            AddExactAngleInput(root);

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

    public static string AddExactAngleToExistingPrefab()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Leave Play Mode first.");
        var root = PrefabUtility.LoadPrefabContents(Path);
        try
        {
            if (root.transform.Find("Card/ExactAngle") == null) AddExactAngleInput(root);
            FitExactAngleLayout(root.transform.Find("Card"));
            PrefabUtility.SaveAsPrefabAsset(root, Path);
            return Path;
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static void AddExactAngleInput(GameObject root)
    {
        var card = root.transform.Find("Card");
        if (card.Find("ExactAngle") != null)
            throw new InvalidOperationException("Exact angle input already exists; edit it instead.");
        var row = new GameObject("ExactAngle", typeof(RectTransform), typeof(UnityEngine.UI.HorizontalLayoutGroup));
        row.layer = 5;
        row.transform.SetParent(card, false);
        row.transform.SetSiblingIndex(card.Find("RotationSlider").GetSiblingIndex());
        Height(row, 44);
        var layout = row.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();
        layout.spacing = 10;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = layout.childForceExpandHeight = false;
        var label = Text(row.transform, "Label", "EXACT ANGLE", 14, 44);
        label.alignment = TextAlignmentOptions.MidlineLeft;
        var labelSize = label.GetComponent<UnityEngine.UI.LayoutElement>();
        labelSize.minWidth = labelSize.preferredWidth = 100;

        var resources = new TMP_DefaultControls.Resources
        {
            inputField = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/InputFieldBackground.psd")
        };
        var inputRoot = TMP_DefaultControls.CreateInputField(resources);
        inputRoot.name = "Degrees";
        inputRoot.transform.SetParent(row.transform, false);
        Height(inputRoot, 44);
        var inputSize = inputRoot.GetComponent<UnityEngine.UI.LayoutElement>();
        inputSize.minWidth = 140;
        inputSize.flexibleWidth = 1;
        var input = inputRoot.GetComponent<TMP_InputField>();
        input.contentType = TMP_InputField.ContentType.Standard;
        input.lineType = TMP_InputField.LineType.SingleLine;
        input.characterLimit = 24;
        input.richText = false;
        input.keyboardType = TouchScreenKeyboardType.NumbersAndPunctuation;
        input.SetTextWithoutNotify("0");
        input.interactable = false;
        input.textComponent.font = TMP_Settings.defaultFontAsset;
        input.textComponent.fontSize = 22;
        input.textComponent.alignment = TextAlignmentOptions.MidlineLeft;
        input.textComponent.color = TextColor;
        input.textComponent.raycastTarget = false;
        var placeholder = (TMP_Text)input.placeholder;
        placeholder.text = "e.g. -12.5";
        placeholder.font = TMP_Settings.defaultFontAsset;
        placeholder.fontSize = 18;
        placeholder.color = new Color(.64f, .73f, .83f);
        placeholder.raycastTarget = false;
        inputRoot.GetComponent<UnityEngine.UI.Image>().color = new Color(.10f, .16f, .23f);
        input.selectionColor = new Color(.08f, .57f, .51f, .6f);
        var unit = Text(row.transform, "Unit", "\u00b0", 22, 44);
        unit.alignment = TextAlignmentOptions.MidlineLeft;
        var unitSize = unit.GetComponent<UnityEngine.UI.LayoutElement>();
        unitSize.minWidth = unitSize.preferredWidth = 24;
        var fields = new SerializedObject(root.GetComponent<DesktopEditingPanel>());
        fields.FindProperty("angleInput").objectReferenceValue = input;
        fields.ApplyModifiedPropertiesWithoutUndo();
        FitExactAngleLayout(card);
    }

    private static void FitExactAngleLayout(Transform card)
    {
        // Fit the new row inside the original card so Submit and Disconnect keep their space.
        var rect = card.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(rect.sizeDelta.x, 400);
        rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, 0);
        card.GetComponent<UnityEngine.UI.VerticalLayoutGroup>().spacing = 8;
        foreach (var size in new[] { ("TaskStatus", 36), ("PartName", 28), ("AngleValue", 44) })
        {
            var element = card.Find(size.Item1).GetComponent<UnityEngine.UI.LayoutElement>();
            element.minHeight = element.preferredHeight = size.Item2;
        }
        Canvas.ForceUpdateCanvases();
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
    }

    public static string AddCloseupToExistingPrefab()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Leave Play Mode first.");
        var root = PrefabUtility.LoadPrefabContents(Path);
        try
        {
            AddCloseup(root);
            PrefabUtility.SaveAsPrefabAsset(root, Path);
            return Path;
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static void AddCloseup(GameObject root)
    {
        if (root.transform.Find("Closeup") != null)
            throw new InvalidOperationException("Close-up already exists; edit its existing objects.");
        var frame = new GameObject("Closeup", typeof(RectTransform), typeof(UnityEngine.UI.Image));
        frame.transform.SetParent(root.transform, false);
        var rect = frame.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = new Vector2(32f, 0f);
        rect.sizeDelta = new Vector2(800f, 600f);
        frame.GetComponent<UnityEngine.UI.Image>().color = new Color(0.035f, 0.06f, 0.1f);
        frame.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
        var image = new GameObject("PartImage", typeof(RectTransform), typeof(UnityEngine.UI.RawImage), typeof(UnityEngine.UI.AspectRatioFitter));
        image.transform.SetParent(frame.transform, false);
        var fit = image.GetComponent<UnityEngine.UI.AspectRatioFitter>();
        fit.aspectMode = UnityEngine.UI.AspectRatioFitter.AspectMode.FitInParent;
        fit.aspectRatio = 1f;
        var output = image.GetComponent<UnityEngine.UI.RawImage>();
        output.raycastTarget = false;
        output.enabled = false;
        var preview = root.AddComponent<DesktopPartPreview>();
        var fields = new SerializedObject(preview);
        fields.FindProperty("output").objectReferenceValue = output;
        fields.ApplyModifiedPropertiesWithoutUndo();
        fields = new SerializedObject(root.GetComponent<DesktopEditingPanel>());
        fields.FindProperty("preview").objectReferenceValue = preview;
        fields.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void Height(GameObject go, int height)
    {
        var element = go.AddComponent<UnityEngine.UI.LayoutElement>();
        element.minHeight = element.preferredHeight = height;
        element.flexibleHeight = 0;
    }
}
