using TaskBubble.Shared;
using UnityEngine;

namespace TaskBubble.Desktop
{
    public sealed class DesktopPartPreview : MonoBehaviour
    {
        [SerializeField] private UnityEngine.UI.RawImage output;
        private const int PreviewLayer = 30;
        private GameObject stage;
        private Transform model;
        private Camera previewCamera;
        private RenderTexture texture;
        private EditablePart source;
        private MeshRenderer[] renderers;
        private float radius;

        public void Show(EditablePart part, DelegationSnapshot snapshot)
        {
            if (output == null || part == null || !part.gameObject.activeInHierarchy)
            {
                Clear();
                return;
            }
            if (stage == null) CreateStage();
            stage.SetActive(true);
            if (source != part || model == null)
            {
                if (model != null) { model.gameObject.SetActive(false); Destroy(model.gameObject); }
                model = new GameObject("Part visuals").transform;
                model.SetParent(stage.transform, false);
                model.localScale = part.transform.lossyScale;
                CopyVisuals(part.transform, model);
                renderers = model.GetComponentsInChildren<MeshRenderer>();
                source = part;
                var initialBounds = Bounds();
                radius = Mathf.Max(initialBounds.extents.magnitude, 0.001f);
            }
            if (renderers.Length == 0) { Clear(); return; }
            model.gameObject.SetActive(true);
            model.localRotation = snapshot.State == DelegationState.Requested
                ? snapshot.StartingLocalRotation : snapshot.ProposalLocalRotation;
            var center = Bounds().center;
            previewCamera.transform.position = center + new Vector3(1f, 0.7f, -1f).normalized * (radius * 3f);
            previewCamera.transform.LookAt(center);
            previewCamera.orthographicSize = radius * 1.2f;
            previewCamera.nearClipPlane = radius * 0.01f;
            previewCamera.farClipPlane = radius * 10f;
            output.texture = texture;
            output.enabled = true;
        }

        public void Clear()
        {
            if (output != null) output.enabled = false;
            if (stage != null) stage.SetActive(false);
        }

        private void CreateStage()
        {
            stage = new GameObject("Desktop part preview") { hideFlags = HideFlags.DontSave };
            stage.transform.position = new Vector3(1000f, 1000f, 1000f);
            texture = new RenderTexture(640, 640, 24) { name = "Desktop part close-up" };
            texture.Create();
            var cameraRoot = new GameObject("Preview camera", typeof(Camera));
            cameraRoot.transform.SetParent(stage.transform, false);
            previewCamera = cameraRoot.GetComponent<Camera>();
            previewCamera.orthographic = true;
            previewCamera.cullingMask = 1 << PreviewLayer;
            previewCamera.clearFlags = CameraClearFlags.SolidColor;
            previewCamera.backgroundColor = new Color(0.035f, 0.06f, 0.1f);
            previewCamera.targetTexture = texture;
            var lightRoot = new GameObject("Preview light", typeof(Light));
            lightRoot.transform.SetParent(stage.transform, false);
            lightRoot.transform.localRotation = Quaternion.Euler(35f, -30f, 0f);
            var light = lightRoot.GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.cullingMask = 1 << PreviewLayer;
            light.shadows = LightShadows.None;
        }

        private void CopyVisuals(Transform original, Transform copy)
        {
            copy.gameObject.layer = PreviewLayer;
            var mesh = original.GetComponent<MeshFilter>();
            var renderer = original.GetComponent<MeshRenderer>();
            if (mesh != null && mesh.sharedMesh != null && renderer != null && renderer.enabled)
            {
                copy.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh.sharedMesh;
                var visual = copy.gameObject.AddComponent<MeshRenderer>();
                visual.sharedMaterials = renderer.sharedMaterials;
                visual.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                visual.receiveShadows = false;
            }
            foreach (Transform child in original)
            {
                if (!child.gameObject.activeSelf) continue;
                var node = new GameObject(child.name).transform;
                node.SetParent(copy, false);
                node.localPosition = child.localPosition;
                node.localRotation = child.localRotation;
                node.localScale = child.localScale;
                CopyVisuals(child, node);
            }
        }

        private Bounds Bounds()
        {
            var bounds = new Bounds(model.position, Vector3.zero);
            if (renderers.Length == 0) return bounds;
            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        private void OnDisable()
        {
            Clear();
            if (output != null) output.texture = null;
            if (stage != null) Destroy(stage);
            if (texture != null) { texture.Release(); Destroy(texture); }
            stage = null;
            model = null;
            source = null;
        }
    }
}
