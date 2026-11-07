using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using TaskBubble.Networking;

public static class CreateConnectionScreen
{
    private const string Root = "Assets/TaskBubble/Networking/";
    private static readonly Color TextColor = new Color(0.91f, 0.95f, 1f);
    private static readonly Color FieldColor = new Color(0.12f, 0.18f, 0.28f);

    // One-time authoring script. Refuse to replace existing assets or discard unsaved work.
    public static string Create()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Leave Play Mode first.");
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save open scenes first.");
        if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(Root + "Connection.unity") != null ||
            AssetDatabase.LoadAssetAtPath<GameObject>(Root + "ConnectionPanel.prefab") != null)
            throw new InvalidOperationException("Connection screen already exists; edit the existing assets instead.");

        var connectionPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "TaskBubbleConnection.prefab");
        if (connectionPrefab == null || TMP_Settings.defaultFontAsset == null)
            throw new InvalidOperationException("Connection prefab and TMP essentials must exist first.");

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
        camera.tag = "MainCamera";
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.035f, 0.06f, 0.1f);
        camera.transform.position = new Vector3(0, 0, -10);

        var canvasRoot = new GameObject("ConnectionPanel", typeof(RectTransform), typeof(Canvas),
            typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
        canvasRoot.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasRoot.GetComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 0.5f;

        var card = new GameObject("Card", typeof(RectTransform), typeof(UnityEngine.UI.Image),
            typeof(UnityEngine.UI.VerticalLayoutGroup));
        card.transform.SetParent(canvasRoot.transform, false);
        var cardRect = card.GetComponent<RectTransform>();
        cardRect.anchorMin = cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = new Vector2(680, 620);
        card.GetComponent<UnityEngine.UI.Image>().color = new Color(0.065f, 0.10f, 0.16f);
        var layout = card.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
        layout.padding = new RectOffset(32, 32, 26, 26);
        layout.spacing = 10;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        Text(card.transform, "Title", "TaskBubble", 38, 48).fontStyle = FontStyles.Bold;
        Text(card.transform, "Subtitle", "Connect the desktop and VR instances", 18, 30);
        Text(card.transform, "ModeLabel", "THIS INSTANCE", 14, 22);

        var resources = new TMP_DefaultControls.Resources
        {
            standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"),
            background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"),
            inputField = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/InputFieldBackground.psd"),
            checkmark = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd"),
            dropdown = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/DropdownArrow.psd"),
            mask = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UIMask.psd")
        };
        var dropdownRoot = TMP_DefaultControls.CreateDropdown(resources);
        dropdownRoot.name = "Mode";
        dropdownRoot.transform.SetParent(card.transform, false);
        Height(dropdownRoot, 46);
        var mode = dropdownRoot.GetComponent<TMP_Dropdown>();
        mode.ClearOptions();
        mode.AddOptions(new List<string> { "Desktop (Host)", "VR (Client)" });
        mode.targetGraphic.color = FieldColor;
        mode.template.GetComponent<UnityEngine.UI.Image>().color = FieldColor;
        foreach (var label in dropdownRoot.GetComponentsInChildren<TMP_Text>(true))
        {
            label.color = TextColor;
            label.fontSize = 18;
            label.raycastTarget = false;
        }

        Text(card.transform, "AddressLabel", "HOST IP ADDRESS  /  PORT 7777", 14, 22);
        var addressRoot = TMP_DefaultControls.CreateInputField(resources);
        addressRoot.name = "HostAddress";
        addressRoot.transform.SetParent(card.transform, false);
        Height(addressRoot, 46);
        var address = addressRoot.GetComponent<TMP_InputField>();
        address.text = "127.0.0.1";
        address.characterLimit = 15;
        address.targetGraphic.color = FieldColor;
        address.textComponent.color = TextColor;
        address.textComponent.fontSize = 20;
        address.textComponent.raycastTarget = false;
        ((TMP_Text)address.placeholder).text = "127.0.0.1";
        ((TMP_Text)address.placeholder).color = new Color(0.55f, 0.65f, 0.76f);
        ((TMP_Text)address.placeholder).fontSize = 20;
        address.placeholder.raycastTarget = false;

        var row = new GameObject("Actions", typeof(RectTransform), typeof(UnityEngine.UI.HorizontalLayoutGroup));
        row.transform.SetParent(card.transform, false);
        Height(row, 48);
        var rowLayout = row.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();
        rowLayout.spacing = 12;
        rowLayout.childControlWidth = rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = rowLayout.childForceExpandHeight = true;
        var host = Button(row.transform, "Host", new Color(0.08f, 0.57f, 0.51f));
        var join = Button(row.transform, "Join", new Color(0.16f, 0.36f, 0.70f));
        var disconnect = Button(row.transform, "Disconnect", new Color(0.20f, 0.27f, 0.37f));
        var status = Text(card.transform, "Status", "Disconnected", 20, 60);
        var instructions = Text(card.transform, "Instructions",
            "Start the desktop host first, then join from the VR instance.", 17, 62);
        instructions.color = new Color(0.64f, 0.73f, 0.83f);

        var panel = canvasRoot.AddComponent<ConnectionPanel>();
        var fields = new SerializedObject(panel);
        Assign(fields, "mode", mode);
        Assign(fields, "hostAddress", address);
        Assign(fields, "hostButton", host);
        Assign(fields, "joinButton", join);
        Assign(fields, "disconnectButton", disconnect);
        Assign(fields, "status", status);
        Assign(fields, "instructions", instructions);
        fields.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.SaveAsPrefabAssetAndConnect(canvasRoot, Root + "ConnectionPanel.prefab", InteractionMode.AutomatedAction);

        var networkRoot = (GameObject)PrefabUtility.InstantiatePrefab(connectionPrefab, scene);
        fields.Update();
        Assign(fields, "connection", networkRoot.GetComponent<TaskBubbleConnection>());
        fields.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.RecordPrefabInstancePropertyModifications(panel);

        var events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        if (!EditorSceneManager.SaveScene(scene, Root + "Connection.unity"))
            throw new InvalidOperationException("Could not save the connection scene.");
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(Root + "Connection.unity", true) }
            .Concat(EditorBuildSettings.scenes.Where(s => s.path != Root + "Connection.unity")).ToArray();
        AssetDatabase.SaveAssets();
        return Root + "Connection.unity";
    }

    private static TMP_Text Text(Transform parent, string name, string value, int size, int height)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var label = go.GetComponent<TextMeshProUGUI>();
        label.font = TMP_Settings.defaultFontAsset;
        label.text = value;
        label.fontSize = size;
        label.color = TextColor;
        label.raycastTarget = false;
        label.textWrappingMode = TextWrappingModes.Normal;
        Height(go, height);
        return label;
    }

    private static UnityEngine.UI.Button Button(Transform parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
        go.transform.SetParent(parent, false);
        go.GetComponent<UnityEngine.UI.Image>().color = color;
        var button = go.GetComponent<UnityEngine.UI.Button>();
        button.targetGraphic = go.GetComponent<UnityEngine.UI.Image>();
        var label = Text(go.transform, "Label", name, 18, 48);
        label.alignment = TextAlignmentOptions.Center;
        var rect = label.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        return button;
    }

    private static void Height(GameObject go, int height)
    {
        var element = go.GetComponent<UnityEngine.UI.LayoutElement>() ?? go.AddComponent<UnityEngine.UI.LayoutElement>();
        element.preferredHeight = height;
        element.flexibleHeight = 0;
        element.minHeight = height;
    }

    private static void Assign(SerializedObject fields, string name, UnityEngine.Object value)
    {
        fields.FindProperty(name).objectReferenceValue = value;
    }
}
