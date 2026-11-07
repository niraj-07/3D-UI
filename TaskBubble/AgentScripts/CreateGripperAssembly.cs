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
            var editable = connector.AddComponent<EditablePart>();
            var serialized = new SerializedObject(editable);
            serialized.FindProperty("partId").stringValue = "gripper.connector";
            serialized.FindProperty("rotationAxis").enumValueIndex = (int)LocalRotationAxis.Y;
            serialized.FindProperty("minimumAngleDegrees").floatValue = -45f;
            serialized.FindProperty("maximumAngleDegrees").floatValue = 45f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            // Root sits on a surface at y=0; source scale remains one metre per Unity unit.
            body.transform.localPosition = new Vector3(0f, thickness - mountingY, -centreZ);
            connector.transform.localPosition = Vector3.up * thickness;
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, output);
            if (prefab == null) throw new InvalidOperationException("Could not save assembly prefab.");
            return new { success = true, prefab = output, meshCount = root.GetComponentsInChildren<MeshFilter>().Length,
                editableId = editable.PartId, axis = editable.RotationAxis.ToString(), couplingThicknessMetres = thickness };
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }
}
