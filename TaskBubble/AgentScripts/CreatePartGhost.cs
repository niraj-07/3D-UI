using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using TaskBubble.Preview;

public static class CreatePartGhost
{
    public static string Create()
    {
        const string folder = "Assets/TaskBubble/Preview";
        const string materialPath = folder + "/Ghost.mat";
        const string prefabPath = folder + "/PartGhost.prefab";
        if (Application.isPlaying) throw new InvalidOperationException("Leave Play Mode first.");
        if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(materialPath) != null ||
            AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(prefabPath) != null)
            throw new InvalidOperationException("Ghost assets already exist; edit them instead.");
        var shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) throw new InvalidOperationException("URP Unlit shader is unavailable.");
        var material = new Material(shader) { name = "Ghost" };
        material.SetColor("_BaseColor", new Color(0.10f, 0.90f, 1f, 0.35f));
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", 0f);
        material.SetFloat("_AlphaClip", 0f);
        material.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Back);
        BaseShaderGUI.SetupMaterialBlendMode(material);
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
