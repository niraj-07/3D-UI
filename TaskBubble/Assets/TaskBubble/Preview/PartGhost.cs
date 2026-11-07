using TaskBubble.Shared;
using UnityEngine;

namespace TaskBubble.Preview
{
    public sealed class PartGhost : MonoBehaviour
    {
        [SerializeField] private Material ghostMaterial;
        private EditablePart source;
        private Transform ghostRoot;
        private bool hasGeometry;

        public bool Show(EditablePart part, Quaternion localRotation)
        {
            float magnitude = Quaternion.Dot(localRotation, localRotation);
            if (part == null || !part.IsConfigured || !part.gameObject.activeInHierarchy || ghostMaterial == null ||
                float.IsNaN(magnitude) || float.IsInfinity(magnitude) || magnitude < 0.000001f)
            {
                Hide();
                return false;
            }
            if (source != part || ghostRoot == null)
            {
                RemoveGhost();
                ghostRoot = new GameObject(part.name + " (ghost)") { hideFlags = HideFlags.DontSave }.transform;
                hasGeometry = CopyMeshes(part.transform, ghostRoot) > 0;
                source = part;
            }
            if (!hasGeometry) { Hide(); return false; }

            // A sibling preserves the same parent space, pivot, and scale as the real part.
            ghostRoot.SetParent(part.transform.parent, false);
            ghostRoot.localPosition = part.transform.localPosition;
            ghostRoot.localScale = part.transform.localScale;
            ghostRoot.localRotation = localRotation.normalized;
            ghostRoot.gameObject.SetActive(true);
            return true;
        }

        public void Hide()
        {
            if (ghostRoot != null) ghostRoot.gameObject.SetActive(false);
        }

        private int CopyMeshes(Transform original, Transform copy)
        {
            copy.gameObject.layer = 2; // Built-in Ignore Raycast; no selection components are copied.
            int count = 0;
            var mesh = original.GetComponent<MeshFilter>();
            var renderer = original.GetComponent<MeshRenderer>();
            if (mesh != null && mesh.sharedMesh != null && renderer != null && renderer.enabled)
            {
                copy.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh.sharedMesh;
                var visual = copy.gameObject.AddComponent<MeshRenderer>();
                var materials = new Material[mesh.sharedMesh.subMeshCount];
                for (int i = 0; i < materials.Length; i++) materials[i] = ghostMaterial;
                visual.sharedMaterials = materials;
                visual.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                visual.receiveShadows = false;
                count++;
            }
            foreach (Transform child in original)
            {
                if (!child.gameObject.activeSelf) continue;
                var node = new GameObject(child.name).transform;
                node.SetParent(copy, false);
                node.localPosition = child.localPosition;
                node.localRotation = child.localRotation;
                node.localScale = child.localScale;
                count += CopyMeshes(child, node);
            }
            return count;
        }

        private void RemoveGhost()
        {
            if (ghostRoot != null)
            {
                ghostRoot.gameObject.SetActive(false);
                Destroy(ghostRoot.gameObject);
            }
            ghostRoot = null;
            source = null;
            hasGeometry = false;
        }

        private void OnDisable() => RemoveGhost();
    }
}
