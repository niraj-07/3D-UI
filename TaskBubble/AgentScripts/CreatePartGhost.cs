using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using TaskBubble.Preview;

public static class CreatePartGhost
{
    public static object AddApprovalFeedback()
    {
        const string prefabPath = "Assets/TaskBubble/Preview/PartGhost.prefab";
        const string materialPath = "Assets/TaskBubble/Preview/ApprovalBadge.mat";
        var scene = SceneManager.GetActiveScene();
        if (Application.isPlaying || scene.path != "Assets/TaskBubble/Integration/MainScene.unity" || scene.isDirty)
            throw new InvalidOperationException("Open and save MainScene in Edit Mode first.");
        var prefab = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            var fields = new SerializedObject(prefab.GetComponent<PartGhost>());
            if (fields.FindProperty("acceptanceFeedback").objectReferenceValue != null)
                throw new InvalidOperationException("Approval feedback already exists.");
            var material = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "ApprovalBadge" };
            material.SetColor("_BaseColor", new Color(.025f, .06f, .08f));
            AssetDatabase.CreateAsset(material, materialPath);
            var badge = new GameObject("Approval Feedback", typeof(TextMeshPro));
            badge.transform.SetParent(prefab.transform, false);
            badge.layer = 2;
            var text = badge.GetComponent<TextMeshPro>();
            text.font = TMP_Settings.defaultFontAsset;
            text.text = "Change accepted";
            text.fontSize = .7f;
            text.color = new Color(.5f, 1f, .65f);
            text.alignment = TextAlignmentOptions.Center;
            text.rectTransform.sizeDelta = new Vector2(.6f, .11f);
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
            text.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var plate = GameObject.CreatePrimitive(PrimitiveType.Quad);
            plate.name = "Badge background";
            plate.transform.SetParent(badge.transform, false);
            plate.transform.localPosition = new Vector3(0, 0, .003f);
            plate.transform.localScale = new Vector3(.62f, .12f, 1f);
            plate.layer = 2;
            UnityEngine.Object.DestroyImmediate(plate.GetComponent<Collider>());
            plate.GetComponent<Renderer>().sharedMaterial = material;
            plate.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            badge.SetActive(false);
            fields.FindProperty("acceptanceFeedback").objectReferenceValue = text;
            fields.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(prefab); }

        var ghost = scene.GetRootGameObjects().Select(g => g.GetComponent<PartGhost>()).Single(g => g != null);
        var rig = scene.GetRootGameObjects().Select(g => g.GetComponent<TaskBubble.VR.PcVrRig>()).Single(r => r != null);
        var binding = new SerializedObject(ghost);
        binding.FindProperty("feedbackCamera").objectReferenceValue = rig.GetComponentInChildren<Camera>(true);
        binding.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.RecordPrefabInstancePropertyModifications(ghost);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        return new { success = true, badge = "Change accepted", durationSeconds = 1.6f };
    }

    public static string Create()
    {
        const string folder = "Assets/TaskBubble/Preview";
        const string materialPath = folder + "/Ghost.mat";
        const string prefabPath = folder + "/PartGhost.prefab";
        if (Application.isPlaying) throw new InvalidOperationException("Leave Play Mode first.");
        if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(materialPath) != null ||
            AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(prefabPath) != null)
            throw new InvalidOperationException("Ghost assets already exist; edit them instead.");
        var shader = Shader.Find("TaskBubble/Ghost");
        if (shader == null) throw new InvalidOperationException("TaskBubble ghost shader is unavailable.");
        var material = new Material(shader) { name = "Ghost" };
        material.SetColor("_BaseColor", new Color(0.10f, 0.90f, 1f, 0.35f));
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        AssetDatabase.CreateAsset(material, materialPath);

        var previous = SceneManager.GetActiveScene();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        try
        {
            var root = new GameObject("PartGhost", typeof(PartGhost));
            var fields = new SerializedObject(root.GetComponent<PartGhost>());
            fields.FindProperty("ghostMaterial").objectReferenceValue = material;
            fields.ApplyModifiedPropertiesWithoutUndo();
            if (PrefabUtility.SaveAsPrefabAsset(root, prefabPath) == null)
                throw new InvalidOperationException("Could not save PartGhost prefab.");
            return prefabPath;
        }
        finally
        {
            SceneManager.SetActiveScene(previous);
            EditorSceneManager.CloseScene(scene, true);
        }
    }
}
