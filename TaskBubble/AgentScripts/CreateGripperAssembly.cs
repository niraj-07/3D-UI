using System;
using System.IO;
using System.Linq;
using TaskBubble.Shared;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CreateGripperAssembly
{
    public static object Build()
    {
        const string folder = "Assets/TaskBubble/CAD/Models";
        const string output = "Assets/TaskBubble/CAD/GripperAssembly.prefab";
        if (Application.isPlaying) throw new InvalidOperationException("Leave Play Mode first.");
        if (File.Exists(output)) throw new InvalidOperationException("Assembly already exists; edit it rather than overwrite it.");
        var scene = EditorSceneManager.NewPreviewScene();
        var root = new GameObject("GripperAssembly");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
        try
        {
            var body = new GameObject("GripperBody");
            body.transform.SetParent(root.transform, false);
            var paths = Directory.GetFiles(folder, "*.obj").Select(p => p.Replace('\\', '/'))
                .Where(p => Path.GetFileNameWithoutExtension(p) != "Coupling").OrderBy(p => p).ToArray();
            if (paths.Length != 11) throw new InvalidOperationException("Expected eleven gripper meshes.");
            foreach (var path in paths)
            {
                var part = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path), scene);
                part.name = Path.GetFileNameWithoutExtension(path);
                part.transform.SetParent(body.transform, false);
                foreach (var filter in part.GetComponentsInChildren<MeshFilter>(true))
                {
                    var collider = filter.gameObject.AddComponent<BoxCollider>();
                    collider.center = filter.sharedMesh.bounds.center;
                    collider.size = filter.sharedMesh.bounds.size;
                }
            }
            // Re-centre the source CAD frame: mounting face at y=0, depth centred at z=0.
            var baseBounds = body.transform.Find("Base").GetComponentInChildren<Renderer>().bounds;
            float mountingY = baseBounds.min.y;
            float centreZ = baseBounds.center.z;
            var connector = new GameObject("Connector");
            connector.transform.SetParent(root.transform, false);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(
                AssetDatabase.LoadAssetAtPath<GameObject>(folder + "/Coupling.obj"), scene);
            model.name = "CouplingMesh";
            model.transform.SetParent(connector.transform, false);
            model.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            var bounds = model.GetComponentInChildren<Renderer>().bounds;
            // The upper coupling face sits flush with the base; no arbitrary gap or overlap.
            model.transform.localPosition = new Vector3(-bounds.center.x, -bounds.max.y, -bounds.center.z);
            bounds = model.GetComponentInChildren<Renderer>().bounds;
            float thickness = bounds.size.y;
            var hitbox = connector.AddComponent<BoxCollider>();
            hitbox.center = new Vector3(0f, -thickness / 2f, 0f);
            hitbox.size = bounds.size;
            // Root sits on a surface at y=0; source scale remains one metre per Unity unit.
            body.transform.localPosition = new Vector3(0f, thickness - mountingY, -centreZ);
            connector.transform.localPosition = Vector3.up * thickness;
            ConfigureFingertips(root);
            ApplyDemoPose(root);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, output);
            if (prefab == null) throw new InvalidOperationException("Could not save assembly prefab.");
            return new { success = true, prefab = output, meshCount = root.GetComponentsInChildren<MeshFilter>().Length,
                editableIds = root.GetComponentsInChildren<EditablePart>().Select(p => p.PartId).ToArray(),
                couplingThicknessMetres = thickness };
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }

    public static object UpgradeFingertips()
    {
        const string path = "Assets/TaskBubble/CAD/GripperAssembly.prefab";
        if (Application.isPlaying) throw new InvalidOperationException("Leave Play Mode first.");
        var stage = PrefabStageUtility.GetCurrentPrefabStage();
        if (stage != null && stage.assetPath == path && stage.scene.isDirty)
            throw new InvalidOperationException("Save or undo your prefab edits first.");
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            ConfigureFingertips(root);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            return new { success = true, editableIds = root.GetComponentsInChildren<EditablePart>().Select(p => p.PartId).ToArray() };
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static void ConfigureFingertips(GameObject root)
    {
        var connector = root.transform.Find("Connector");
        var oldEditable = connector.GetComponent<EditablePart>();
        if (oldEditable != null) UnityEngine.Object.DestroyImmediate(oldEditable);
        var body = root.transform.Find("GripperBody");
        foreach (var suffix in new[] { "A", "B" })
        {
            var part = body.Find("Fingertip_" + suffix);
            if (part.GetComponent<EditablePart>() != null) continue;
            var bounds = part.GetComponentInChildren<Renderer>().bounds;
            var pivot = new GameObject("Fingertip_" + suffix);
            pivot.transform.SetParent(body, false);
            pivot.transform.position = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            // Keep the exported mesh in place while moving its pivot to its lower attachment edge.
            part.SetParent(pivot.transform, true);
            part.name = "FingertipMesh";
            var serialized = new SerializedObject(pivot.AddComponent<EditablePart>());
            serialized.FindProperty("partId").stringValue = "gripper.fingertip-" + suffix.ToLowerInvariant();
            serialized.FindProperty("rotationAxis").enumValueIndex = (int)(suffix == "A" ? LocalRotationAxis.Z : LocalRotationAxis.X);
            serialized.FindProperty("minimumAngleDegrees").floatValue = suffix == "A" ? -90f : -45f;
            serialized.FindProperty("maximumAngleDegrees").floatValue = suffix == "A" ? 90f : 45f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    public static object SetDemoPose()
    {
        const string path = "Assets/TaskBubble/CAD/GripperAssembly.prefab";
        if (Application.isPlaying) throw new InvalidOperationException("Leave Play Mode first.");
        var stage = PrefabStageUtility.GetCurrentPrefabStage();
        if (stage != null && stage.assetPath == path && stage.scene.isDirty)
            throw new InvalidOperationException("Save or undo your prefab edits first.");
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            ApplyDemoPose(root);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            return new { success = true, tiltedPart = "gripper.fingertip-a", startingAngle = 70f,
                correctionFromInitialPose = -70f, secondPart = "gripper.fingertip-b", secondAxis = "X",
                secondStartingAngle = 25f, secondCorrectionFromInitialPose = -25f };
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static void ApplyDemoPose(GameObject root)
    {
        var parts = root.GetComponentsInChildren<EditablePart>();
        var tilted = parts.Single(p => p.PartId == "gripper.fingertip-a");
        var twisted = parts.Single(p => p.PartId == "gripper.fingertip-b");
        var fields = new SerializedObject(twisted);
        fields.FindProperty("rotationAxis").enumValueIndex = (int)LocalRotationAxis.X;
        fields.FindProperty("minimumAngleDegrees").floatValue = -45f;
        fields.FindProperty("maximumAngleDegrees").floatValue = 45f;
        fields.ApplyModifiedPropertiesWithoutUndo();
        tilted.transform.localRotation = Quaternion.AngleAxis(70f, Vector3.forward);
        twisted.transform.localRotation = Quaternion.AngleAxis(25f, Vector3.right);
    }
}
