using TaskBubble.Shared;
using TaskBubble.Networking;
using TMPro;
using UnityEngine;

namespace TaskBubble.Preview
{
    public sealed class PartGhost : MonoBehaviour
    {
        [SerializeField] private Material ghostMaterial;
        [SerializeField] private DelegationSession session;
        [SerializeField] private TaskBubbleConnection connection;
        [SerializeField] private EditablePart editablePart;
        [SerializeField] private Transform assemblyRoot;
        [SerializeField] private TMP_Text acceptanceFeedback;
        [SerializeField] private Camera feedbackCamera;
        private readonly EditablePartRegistry parts = new EditablePartRegistry();
        private EditablePart source;
        private Transform ghostRoot;
        private bool hasGeometry;
        private MeshRenderer[] ghostRenderers;
        private MaterialPropertyBlock tint;
        private bool acceptanceActive;
        private float acceptanceStarted;
        private uint acceptedTaskId;
        private const float AcceptanceDuration = 1.6f;

        private void OnEnable()
        {
            if (assemblyRoot != null) parts.Rebuild(assemblyRoot);
            if (session != null) session.StateChanged += OnStateChanged;
            if (connection != null) connection.Changed += Refresh;
            Refresh();
        }

        private void OnStateChanged(DelegationSnapshot snapshot) => Refresh();

        private void Refresh()
        {
            if (session == null || !session.IsSpawned || connection == null ||
                connection.State != ConnectionState.Connected || connection.LocalRole != ParticipantRole.Vr)
            {
                Hide();
                acceptedTaskId = 0;
                return;
            }
            var snapshot = session.Current;
            if (assemblyRoot != null) parts.TryGet(snapshot.PartId, out editablePart);
            if (editablePart != null && snapshot.TaskId != 0 && snapshot.PartId == editablePart.PartId &&
                (snapshot.State == DelegationState.Editing || snapshot.State == DelegationState.Review))
                Show(editablePart, snapshot.ProposalLocalRotation);
            else if (editablePart != null && snapshot.TaskId != 0 && snapshot.PartId == editablePart.PartId &&
                snapshot.State == DelegationState.Accepted)
            {
                if (acceptedTaskId != snapshot.TaskId)
                {
                    acceptedTaskId = snapshot.TaskId;
                    if (Show(editablePart, snapshot.ProposalLocalRotation))
                    {
                        acceptanceStarted = Time.unscaledTime;
                        acceptanceActive = true;
                        SetAcceptanceTint(.6f);
                        if (acceptanceFeedback != null)
                        {
                            acceptanceFeedback.text = "Change accepted";
                            acceptanceFeedback.gameObject.SetActive(true);
                            PositionFeedback();
                        }
                    }
                }
            }
            else
                Hide();
        }

        private void LateUpdate()
        {
            if (!acceptanceActive) return;
            float progress = (Time.unscaledTime - acceptanceStarted) / AcceptanceDuration;
            if (progress >= 1f || source == null || !source.gameObject.activeInHierarchy)
            {
                Hide();
                return;
            }
            SetAcceptanceTint(.6f * (1f - Mathf.SmoothStep(0f, 1f, progress)));
            PositionFeedback();
        }

        private void PositionFeedback()
        {
            if (acceptanceFeedback == null || ghostRenderers == null || ghostRenderers.Length == 0) return;
            var bounds = ghostRenderers[0].bounds;
            foreach (var renderer in ghostRenderers) if (renderer != null) bounds.Encapsulate(renderer.bounds);
            acceptanceFeedback.transform.position = new Vector3(bounds.center.x, bounds.max.y + .12f, bounds.center.z);
            var viewer = feedbackCamera != null ? feedbackCamera : Camera.main;
            if (viewer != null)
            {
                Vector3 direction = acceptanceFeedback.transform.position - viewer.transform.position;
                if (direction.sqrMagnitude > .0001f)
                    acceptanceFeedback.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
            }
        }

        private void SetAcceptanceTint(float alpha)
        {
            if (tint == null) tint = new MaterialPropertyBlock();
            tint.SetColor("_BaseColor", new Color(.22f, 1f, .42f, alpha));
            foreach (var renderer in ghostRenderers)
                if (renderer != null) renderer.SetPropertyBlock(tint);
        }

        public bool Show(EditablePart part, Quaternion localRotation)
        {
            acceptanceActive = false;
            if (acceptanceFeedback != null) acceptanceFeedback.gameObject.SetActive(false);
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
                ghostRenderers = ghostRoot.GetComponentsInChildren<MeshRenderer>();
                source = part;
            }
            if (!hasGeometry) { Hide(); return false; }

            // A sibling preserves the same parent space, pivot, and scale as the real part.
            ghostRoot.SetParent(part.transform.parent, false);
            ghostRoot.localPosition = part.transform.localPosition;
            ghostRoot.localScale = part.transform.localScale;
            ghostRoot.localRotation = localRotation.normalized;
            foreach (var renderer in ghostRenderers) renderer.SetPropertyBlock(null);
            ghostRoot.gameObject.SetActive(true);
            return true;
        }

        public void Hide()
        {
            acceptanceActive = false;
            if (acceptanceFeedback != null) acceptanceFeedback.gameObject.SetActive(false);
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
            ghostRenderers = null;
        }

        private void OnDisable()
        {
            if (session != null) session.StateChanged -= OnStateChanged;
            if (connection != null) connection.Changed -= Refresh;
            RemoveGhost();
            if (acceptanceFeedback != null) acceptanceFeedback.gameObject.SetActive(false);
        }
    }
}
