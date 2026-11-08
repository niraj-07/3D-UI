using System;
using System.Linq;
using TaskBubble.Desktop;
using TaskBubble.Integration;
using TaskBubble.Networking;
using TaskBubble.Preview;
using TaskBubble.Review;
using TaskBubble.VR;
using Unity.Netcode;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class WireWorkspaceWorkflow
{
    public static object Build()
    {
        const string path = "Assets/TaskBubble/Integration/MainScene.unity";
        if (Application.isPlaying) throw new InvalidOperationException("Leave Play Mode first.");
        var scene = SceneManager.GetSceneByPath(path);
        bool opened = !scene.IsValid() || !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        if (scene.isDirty) throw new InvalidOperationException("Save MainScene edits before wiring it.");
        var previous = SceneManager.GetActiveScene();
        SceneManager.SetActiveScene(scene);
        try
        {
            var roots = scene.GetRootGameObjects();
            if (roots.Any(r => r.GetComponent<DelegationSession>() != null)) throw new InvalidOperationException("Session is already wired; edit it instead.");
            var connection = roots.Select(r => r.GetComponent<TaskBubbleConnection>()).Single(c => c != null);
            var cad = roots.Single(r => r.name == "GripperAssembly");
            var rig = roots.Select(r => r.GetComponent<PcVrRig>()).Single(r => r != null);
            var view = roots.Select(r => r.GetComponent<WorkspaceView>()).Single(v => v != null);
            var session = new GameObject("Delegation Session", typeof(NetworkObject)).AddComponent<DelegationSession>();
            Set(session, "connection", connection); Set(session, "assemblyRoot", cad.transform);
            var selector = rig.GetComponentInChildren<VrPartSelection>(true);
            Set(selector, "assemblyRoot", cad.transform);
            var delegation = rig.GetComponentInChildren<VrDelegationControls>(true);
            Set(delegation, "connection", connection); Set(delegation, "session", session);

            var desktop = Instantiate("Assets/TaskBubble/Desktop/DesktopPanel.prefab");
            var editing = desktop.GetComponent<DesktopEditingPanel>();
            Set(editing, "session", session); Set(editing, "connection", connection); Set(editing, "assemblyRoot", cad.transform);
            var submit = Instantiate("Assets/TaskBubble/Review/DesktopReviewControls.prefab");
            submit.transform.SetParent(desktop.transform, false);
            Set(submit.GetComponent<DesktopReviewControls>(), "session", session);
            Set(submit.GetComponent<DesktopReviewControls>(), "connection", connection);
            desktop.SetActive(false);

            var review = Instantiate("Assets/TaskBubble/Review/VrReviewControls.prefab");
            review.transform.SetParent(rig.transform, false);
            review.transform.localPosition = new Vector3(.48f, 1.25f, 1f);
            Set(review.GetComponent<VrReviewControls>(), "session", session);
            Set(review.GetComponent<VrReviewControls>(), "connection", connection);
            foreach (var button in review.GetComponentsInChildren<UnityEngine.UI.Button>(true))
            {
                var box = button.gameObject.AddComponent<BoxCollider>();
                box.size = new Vector3(100, 64, 8);
            }
            // Layout-group sizes are resolved after the canvas rebuild.
            Canvas.ForceUpdateCanvases();
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(review.GetComponent<RectTransform>());
            foreach (var button in review.GetComponentsInChildren<UnityEngine.UI.Button>(true))
            {
                var rect = button.GetComponent<RectTransform>();
                button.GetComponent<BoxCollider>().size = new Vector3(rect.rect.width, rect.rect.height, 8);
            }
            review.SetActive(false);
            delegation.gameObject.SetActive(false);

            var ghost = Instantiate("Assets/TaskBubble/Preview/PartGhost.prefab").GetComponent<PartGhost>();
            Set(ghost, "session", session); Set(ghost, "connection", connection); Set(ghost, "assemblyRoot", cad.transform);
            Set(view, "desktopEditingPanel", desktop);
            Set(view, "vrDelegationPanel", delegation.gameObject);
            Set(view, "vrReviewPanel", review);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save workflow scene.");
            return new { success = true, scene = path, session = "Scene NetworkObject", editableParts = 2,
                desktop = "Editing + Submit", vr = "Delegate + ghost + Accept/Reject/Reset" };
        }
        finally
        {
            SceneManager.SetActiveScene(previous);
            if (opened) EditorSceneManager.CloseScene(scene, true);
        }
    }
    static GameObject Instantiate(string path) => (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
    static void Set(UnityEngine.Object target, string name, UnityEngine.Object value)
    {
        var serialized = new SerializedObject(target);
        serialized.FindProperty(name).objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        if (PrefabUtility.IsPartOfPrefabInstance(target)) PrefabUtility.RecordPrefabInstancePropertyModifications(target);
    }
}
