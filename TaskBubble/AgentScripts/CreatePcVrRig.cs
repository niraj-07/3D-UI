using System;
using System.IO;
using TaskBubble.VR;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEditor.XR.OpenXR;
using UnityEditor.XR.OpenXR.Features;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.Hands.OpenXR;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.Interactions;

public static class CreatePcVrRig
{
    public static object Build()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Leave Play Mode first.");
        const string path = "Assets/TaskBubble/VR/PcVrRig.prefab";
        if (File.Exists(path)) throw new InvalidOperationException("Rig already exists; do not overwrite it.");
        if (!AssetDatabase.IsValidFolder("Assets/XR")) AssetDatabase.CreateFolder("Assets", "XR");
        if (!EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.settingsKey, out XRGeneralSettingsPerBuildTarget targets))
        {
            targets = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
            AssetDatabase.CreateAsset(targets, "Assets/XR/XRGeneralSettings.asset");
            EditorBuildSettings.AddConfigObject(XRGeneralSettings.settingsKey, targets, true);
        }
        var group = BuildTargetGroup.Standalone;
        if (!targets.HasManagerSettingsForBuildTarget(group)) targets.CreateDefaultManagerSettingsForBuildTarget(group);
        var general = targets.SettingsForBuildTarget(group);
        general.InitManagerOnStart = false;
        general.Manager.automaticLoading = false;
        general.Manager.automaticRunning = false;
        if (!XRPackageMetadataStore.AssignLoader(general.Manager, typeof(OpenXRLoader).FullName, group))
            throw new InvalidOperationException("Could not assign OpenXR for Windows.");
        // The package's settings factory is on an internal Editor type; use its factory so
        // OpenXR owns asset creation and build registration rather than writing serialized settings.
        var factory = typeof(FeatureHelpers).Assembly.GetType("UnityEditor.XR.OpenXR.OpenXRPackageSettings");
        factory.GetMethod("GetOrCreateInstance").Invoke(null, null);
        var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(group);
        FeatureHelpers.RefreshFeatures(group);
        settings.renderMode = OpenXRSettings.RenderMode.SinglePassInstanced;
        settings.GetFeature<OculusTouchControllerProfile>().enabled = true;
        settings.GetFeature<HandTracking>().enabled = true;
        settings.GetFeature<MetaHandTrackingAim>().enabled = true;
        EditorUtility.SetDirty(settings);
        EditorUtility.SetDirty(general);
        EditorUtility.SetDirty(general.Manager);
        EditorUtility.SetDirty(targets);
        AssetDatabase.SaveAssets();

        var scene = EditorSceneManager.NewPreviewScene();
        try
        {
            var root = new GameObject("PcVrRig");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
            var origin = root.AddComponent<XROrigin>();
            var rig = root.AddComponent<PcVrRig>();
            var offset = new GameObject("Tracked Objects");
            offset.transform.SetParent(root.transform, false);
            var cameraObject = new GameObject("Head Camera");
            cameraObject.transform.SetParent(offset.transform, false);
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.nearClipPlane = .05f; camera.farClipPlane = 100f;
            camera.cullingMask &= ~(1 << 30); // Desktop close-up render layer.
            cameraObject.AddComponent<AudioListener>();
            Track(cameraObject, "<XRHMD>", "centerEyePosition", "centerEyeRotation");
            origin.Camera = camera;
            origin.CameraFloorOffsetObject = offset;
            origin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;
            origin.CameraYOffset = 1.6f;
            foreach (var hand in new[] { "LeftHand", "RightHand" })
            {
                var controller = new GameObject(hand + " Controller");
                controller.transform.SetParent(offset.transform, false);
                Track(controller, "<XRController>{" + hand + "}", "devicePosition", "deviceRotation");
                var aim = new GameObject(hand + " Hand Aim");
                aim.transform.SetParent(offset.transform, false);
                Track(aim, "<MetaAimHand>{" + hand + "}", "devicePosition", "deviceRotation");
            }
            var serialized = new SerializedObject(rig);
            serialized.FindProperty("trackedObjects").objectReferenceValue = offset;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            ConfigureSelection(root);
            offset.SetActive(false); // StartVr explicitly enables tracking after XR initializes.
            if (PrefabUtility.SaveAsPrefabAsset(root, path) == null) throw new InvalidOperationException("Could not save rig.");
            return new { success = true, prefab = path, startup = "Explicit VR only", hands = "XR Hands + Meta Hand Tracking Aim", controllers = "Oculus Touch" };
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }

    public static object AddSelection()
    {
        const string path = "Assets/TaskBubble/VR/PcVrRig.prefab";
        if (Application.isPlaying) throw new InvalidOperationException("Leave Play Mode first.");
        var stage = PrefabStageUtility.GetCurrentPrefabStage();
        if (stage != null && stage.assetPath == path && stage.scene.isDirty)
            throw new InvalidOperationException("Save or undo your prefab edits first.");
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            ConfigureSelection(root);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            return new { success = true, pointers = 4, inputs = "Controller trigger or hand index pinch" };
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static void ConfigureSelection(GameObject root)
    {
        var offset = root.transform.Find("Tracked Objects");
        if (offset.GetComponent<VrPartSelection>() != null) throw new InvalidOperationException("Selection is already configured.");
        const string materialPath = "Assets/TaskBubble/VR/SelectionRay.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            material.SetColor("_BaseColor", new Color(0f, .85f, 1f, 1f));
            AssetDatabase.CreateAsset(material, materialPath);
        }
        var serialized = new SerializedObject(offset.gameObject.AddComponent<VrPartSelection>());
        var pointers = serialized.FindProperty("pointers");
        pointers.arraySize = 4;
        int index = 0;
        foreach (var side in new[] { "LeftHand", "RightHand" })
        foreach (bool hand in new[] { false, true })
        {
            var anchor = offset.Find(side + (hand ? " Hand Aim" : " Controller"));
            var rayObject = new GameObject("Selection Ray");
            rayObject.transform.SetParent(anchor, false);
            rayObject.layer = 2;
            var line = rayObject.AddComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.positionCount = 2;
            line.useWorldSpace = true;
            line.startWidth = line.endWidth = .002f;
            line.startColor = line.endColor = Color.white;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.enabled = false;
            var pointer = pointers.GetArrayElementAtIndex(index++);
            pointer.FindPropertyRelative("origin").objectReferenceValue = anchor;
            pointer.FindPropertyRelative("line").objectReferenceValue = line;
            pointer.FindPropertyRelative("left").boolValue = side == "LeftHand";
            pointer.FindPropertyRelative("hand").boolValue = hand;
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void Track(GameObject target, string device, string position, string rotation)
    {
        var driver = target.AddComponent<TrackedPoseDriver>();
        driver.positionInput = new InputActionProperty(new InputAction("Position", InputActionType.Value,
            device + "/" + position, expectedControlType: "Vector3"));
        driver.rotationInput = new InputActionProperty(new InputAction("Rotation", InputActionType.Value,
            device + "/" + rotation, expectedControlType: "Quaternion"));
        driver.trackingStateInput = new InputActionProperty(new InputAction("Tracking State", InputActionType.Value,
            device + "/trackingState", expectedControlType: "Integer"));
        driver.ignoreTrackingState = false;
    }
}
