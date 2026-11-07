using System;
using System.IO;
using System.Linq;
using TaskBubble.Integration;
using TaskBubble.Networking;
using TaskBubble.VR;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class CreateWorkspaceScene
{
    const string Folder = "Assets/TaskBubble/Integration";
    public static object Build()
    {
        string path = Folder + "/MainScene.unity";
        if (Application.isPlaying || File.Exists(path)) throw new InvalidOperationException("Leave Play Mode; do not overwrite an existing MainScene.");
        if (!AssetDatabase.IsValidFolder(Folder + "/Materials")) AssetDatabase.CreateFolder(Folder, "Materials");
        var floor = Material("Floor", new Color(.22f, .26f, .30f), .1f);
        var wall = Material("Wall", new Color(.31f, .40f, .44f), .1f);
        var top = Material("TableTop", new Color(.42f, .50f, .54f), .3f);
        var frame = Material("TableFrame", new Color(.10f, .14f, .18f), .45f);
        var previous = SceneManager.GetActiveScene();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        try
        {
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.45f, .49f, .55f);
            var environment = new GameObject("Environment");
            Cube(environment.transform, "Floor", new Vector3(0, -.05f, 1), new Vector3(6, .1f, 6), floor);
            Cube(environment.transform, "Back Wall", new Vector3(0, 1.5f, 3.5f), new Vector3(6, 3, .1f), wall);
            var table = new GameObject("Inspection Table"); table.transform.SetParent(environment.transform, false);
            Cube(table.transform, "Top", new Vector3(0, .81f, 1.25f), new Vector3(1.8f, .08f, 1f), top);
            foreach (float x in new[] { -.77f, .77f }) foreach (float z in new[] { .85f, 1.65f })
                Cube(table.transform, "Leg", new Vector3(x, .385f, z), new Vector3(.08f, .77f, .08f), frame);
            var cad = Instantiate("Assets/TaskBubble/CAD/GripperAssembly.prefab");
            cad.transform.position = new Vector3(0, .85f, 1.25f);
            cad.transform.localScale = Vector3.one * 4f;

            var light = new GameObject("Key Light").AddComponent<Light>();
            light.type = LightType.Directional; light.intensity = 1.2f; light.shadows = LightShadows.Soft;
            light.transform.rotation = Quaternion.Euler(45, -35, 0);
            var fill = new GameObject("Fill Light").AddComponent<Light>();
            fill.type = LightType.Directional; fill.intensity = .35f; fill.transform.rotation = Quaternion.Euler(35, 140, 0);
            var camera = new GameObject("Desktop Camera", typeof(AudioListener)).AddComponent<Camera>();
            camera.tag = "MainCamera"; camera.nearClipPlane = .05f; camera.farClipPlane = 30f;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.10f, .14f, .19f);
            camera.cullingMask &= ~(1 << 30);
            camera.rect = new Rect(.38f, 0, .62f, 1);
            camera.transform.position = new Vector3(1.9f, 1.85f, -1.35f);
            camera.transform.LookAt(new Vector3(0, 1.04f, 1.25f)); camera.fieldOfView = 42f;

            var network = Instantiate("Assets/TaskBubble/Networking/TaskBubbleConnection.prefab").GetComponent<TaskBubbleConnection>();
            var panel = Instantiate("Assets/TaskBubble/Networking/ConnectionPanel.prefab");
            var fields = new SerializedObject(panel.GetComponent<ConnectionPanel>());
            fields.FindProperty("connection").objectReferenceValue = network; fields.ApplyModifiedPropertiesWithoutUndo();
            var card = panel.transform.Find("Card").GetComponent<RectTransform>();
            card.anchorMin = card.anchorMax = new Vector2(0, .5f); card.pivot = new Vector2(0, .5f);
            card.anchoredPosition = new Vector2(24, 0); card.sizeDelta = new Vector2(440, 600);
            var instructions = card.Find("Instructions"); if (instructions != null) instructions.gameObject.SetActive(false);
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            var rig = Instantiate("Assets/TaskBubble/VR/PcVrRig.prefab");
            rig.GetComponentInChildren<VrPartSelection>(true).SetAssemblyRoot(cad.transform);
            // Step 1 is local selection only; network delegation and review are wired next.
            rig.GetComponentInChildren<VrDelegationControls>(true).gameObject.SetActive(false);

            var preview = new GameObject("Headset Preview", typeof(RectTransform), typeof(Canvas),
                typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            preview.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            preview.GetComponent<Canvas>().sortingOrder = 10;
            var scaler = preview.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = .5f;
            var status = Text(preview.transform, "PreviewStatus", "Connect Quest Link or Air Link, then preview. Esc returns to desktop.", 18);
            status.rectTransform.anchorMin = status.rectTransform.anchorMax = Vector2.one;
            status.rectTransform.pivot = Vector2.one; status.rectTransform.anchoredPosition = new Vector2(-24, -24);
            status.rectTransform.sizeDelta = new Vector2(550, 72); status.alignment = TextAlignmentOptions.TopRight;
            var buttonRoot = new GameObject("PreviewInHeadset", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
            buttonRoot.transform.SetParent(preview.transform, false);
            var rect = buttonRoot.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(1, 0); rect.pivot = new Vector2(1, 0);
            rect.anchoredPosition = new Vector2(-24, 24); rect.sizeDelta = new Vector2(300, 50);
            var graphic = buttonRoot.GetComponent<UnityEngine.UI.Image>(); graphic.color = new Color(.08f, .57f, .51f);
            var button = buttonRoot.GetComponent<UnityEngine.UI.Button>(); button.targetGraphic = graphic;
            var label = Text(buttonRoot.transform, "Label", "Preview in headset", 22);
            label.rectTransform.anchorMin = Vector2.zero; label.rectTransform.anchorMax = Vector2.one; label.rectTransform.sizeDelta = Vector2.zero;
            var view = new GameObject("Workspace View").AddComponent<WorkspaceView>();
            fields = new SerializedObject(view);
            fields.FindProperty("connection").objectReferenceValue = network;
            fields.FindProperty("vrRig").objectReferenceValue = rig.GetComponent<PcVrRig>();
            fields.FindProperty("desktopCamera").objectReferenceValue = camera;
            fields.FindProperty("connectionPanel").objectReferenceValue = panel;
            fields.FindProperty("previewPanel").objectReferenceValue = preview;
            fields.FindProperty("previewButton").objectReferenceValue = button;
            fields.FindProperty("previewStatus").objectReferenceValue = status;
            fields.ApplyModifiedPropertiesWithoutUndo();
            if (!EditorSceneManager.SaveScene(scene, path)) throw new InvalidOperationException("Could not save MainScene.");
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(path, true) }
                .Concat(EditorBuildSettings.scenes.Where(s => s.path != path)).ToArray();
            return new { success = true, scene = path, modelScale = 4, tableHeightMetres = .85f,
                buildSceneIndex = 0, checkpoint = "Headset preview and local selection; editing workflow not wired yet" };
        }
        finally { SceneManager.SetActiveScene(previous); EditorSceneManager.CloseScene(scene, true); }
    }

    static GameObject Instantiate(string path) => (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
    static Material Material(string name, Color color, float smoothness)
    {
        string path = Folder + "/Materials/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null) return material;
        material = new Material(Shader.Find("Universal Render Pipeline/Lit")); material.SetColor("_BaseColor", color);
        material.SetFloat("_Smoothness", smoothness); AssetDatabase.CreateAsset(material, path); return material;
    }
    static void Cube(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
    {
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube); cube.name = name; cube.transform.SetParent(parent, false);
        cube.transform.localPosition = position; cube.transform.localScale = scale; cube.GetComponent<Renderer>().sharedMaterial = material;
    }
    static TMP_Text Text(Transform parent, string name, string value, int size)
    {
        var root = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)); root.transform.SetParent(parent, false);
        var text = root.GetComponent<TextMeshProUGUI>(); text.font = TMP_Settings.defaultFontAsset; text.text = value; text.fontSize = size;
        text.color = new Color(.91f, .95f, 1f); text.alignment = TextAlignmentOptions.Center; text.raycastTarget = false; return text;
    }
}
