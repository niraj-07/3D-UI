using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class ImportCadModels
{
    public static object Import()
    {
        const string folder = "Assets/TaskBubble/CAD/Models";
        if (Application.isPlaying) throw new InvalidOperationException("Leave Play Mode first.");
        AssetDatabase.Refresh();
        var paths = Directory.GetFiles(folder, "*.obj").Select(p => p.Replace('\\', '/')).OrderBy(p => p).ToArray();
        if (paths.Length != 12) throw new InvalidOperationException("Expected eleven gripper components and one coupling.");
        foreach (var path in paths)
        {
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null) throw new InvalidOperationException("Missing model importer: " + path);
            if (Math.Abs(importer.globalScale - 0.001f) > 0.0000001f)
            {
                importer.globalScale = 0.001f;
                importer.SaveAndReimport();
            }
        }
        var models = paths.Select(path => new { path, model = AssetDatabase.LoadAssetAtPath<GameObject>(path) }).ToArray();
        if (models.Any(m => m.model == null || m.model.GetComponentsInChildren<MeshFilter>(true).Length != 1))
            throw new InvalidOperationException("Each component must import as exactly one static mesh.");
        var original = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CAD model/Converted/Gripper.obj");
        var reference = BoundsOf(original);
        reference.center *= 0.001f;
        reference.size *= 0.001f;
        var gripper = models.Where(m => Path.GetFileNameWithoutExtension(m.path) != "Coupling").ToArray();
        var combined = BoundsOf(gripper[0].model);
        foreach (var item in gripper.Skip(1)) combined.Encapsulate(BoundsOf(item.model));
        if (Vector3.Distance(combined.min, reference.min) > 0.00001f || Vector3.Distance(combined.max, reference.max) > 0.00001f)
            throw new InvalidOperationException("Split component bounds differ from the scaled original assembly.");
        long triangles = gripper.Sum(m => Triangles(m.model));
        if (triangles != Triangles(original)) throw new InvalidOperationException("Gripper triangle count changed during splitting.");
        var originalCoupling = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CAD model/Converted/coupling.obj");
        var coupling = models.Single(m => Path.GetFileNameWithoutExtension(m.path) == "Coupling").model;
        var couplingReference = BoundsOf(originalCoupling);
        couplingReference.center *= 0.001f; couplingReference.size *= 0.001f;
        var couplingBounds = BoundsOf(coupling);
        if (Vector3.Distance(couplingBounds.min, couplingReference.min) > 0.00001f ||
            Vector3.Distance(couplingBounds.max, couplingReference.max) > 0.00001f || Triangles(coupling) != Triangles(originalCoupling))
            throw new InvalidOperationException("Coupling geometry differs from the scaled original.");
        return new { success = true, componentCount = models.Length, gripperTriangles = triangles,
            gripperSizeMetres = new { x = combined.size.x, y = combined.size.y, z = combined.size.z },
            models = models.Select(m => new { name = Path.GetFileNameWithoutExtension(m.path), triangles = Triangles(m.model),
                materials = m.model.GetComponentsInChildren<MeshRenderer>(true).SelectMany(r => r.sharedMaterials)
                    .Select(mat => mat == null ? "MISSING" : mat.name + " / " + mat.shader.name).Distinct().ToArray() }).ToArray() };
    }

    private static long Triangles(GameObject root) => root.GetComponentsInChildren<MeshFilter>(true)
        .Sum(f => Enumerable.Range(0, f.sharedMesh.subMeshCount).Sum(i => (long)f.sharedMesh.GetIndexCount(i) / 3));

    private static Bounds BoundsOf(GameObject root)
    {
        bool initialized = false;
        var result = new Bounds();
        foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true))
        {
            var bounds = filter.sharedMesh.bounds;
            for (int i = 0; i < 8; i++)
            {
                var corner = bounds.center + Vector3.Scale(bounds.extents,
                    new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                var point = filter.transform.localToWorldMatrix.MultiplyPoint3x4(corner);
                if (!initialized) { result = new Bounds(point, Vector3.zero); initialized = true; }
                else result.Encapsulate(point);
            }
        }
        return result;
    }
}
