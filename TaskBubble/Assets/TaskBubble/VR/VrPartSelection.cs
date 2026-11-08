using System;
using System.Collections.Generic;
using TaskBubble.Shared;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Hands;

namespace TaskBubble.VR
{
    public sealed class VrPartSelection : MonoBehaviour
    {
        [Serializable]
        private sealed class Pointer
        {
            public Transform origin;
            public LineRenderer line;
            public bool left;
            public bool hand;
            [NonSerialized] public InputAction press, tracked, trackingState, aimFlags;
            [NonSerialized] public bool valid, wasActive, wasPressed, armed;
        }

        [SerializeField] private Transform assemblyRoot;
        [SerializeField] private Pointer[] pointers = Array.Empty<Pointer>();
        [SerializeField] private float maximumDistance = 4f;
        [SerializeField] private Color selectionColor = new Color(0f, .85f, 1f, 1f);
        private readonly Dictionary<Renderer, MaterialPropertyBlock> originalBlocks = new Dictionary<Renderer, MaterialPropertyBlock>();
        private bool interactionEnabled = true;
        private bool partSelectionEnabled = true;
        private UnityEngine.UI.Button[] buttons = Array.Empty<UnityEngine.UI.Button>();
        private readonly RaycastHit[] nearbyHits = new RaycastHit[32];

        public void SetMenuRoots(params GameObject[] roots)
        {
            var found = new List<UnityEngine.UI.Button>();
            foreach (var root in roots)
                if (root != null) found.AddRange(root.GetComponentsInChildren<UnityEngine.UI.Button>(true));
            buttons = found.ToArray();
        }

        public EditablePart SelectedPart { get; private set; }
        public event Action<EditablePart> SelectionChanged;
        public bool InteractionEnabled
        {
            get => interactionEnabled;
            set { interactionEnabled = value; if (!value) ClearSelection(); }
        }
        public bool PartSelectionEnabled
        {
            get => partSelectionEnabled;
            set { partSelectionEnabled = value; if (!value) ClearSelection(); }
        }

        public void SetAssemblyRoot(Transform root)
        {
            ClearSelection();
            assemblyRoot = root;
        }

        private void OnEnable()
        {
            foreach (var p in pointers)
            {
                string device = (p.hand ? "<MetaAimHand>" : "<XRController>") + (p.left ? "{LeftHand}" : "{RightHand}");
                p.press = new InputAction("Select", InputActionType.Button, device + (p.hand ? "/indexPressed" : "/triggerPressed"));
                p.tracked = new InputAction("Tracked", InputActionType.Button, device + "/isTracked");
                p.trackingState = new InputAction("Tracking state", InputActionType.Value, device + "/trackingState");
                p.aimFlags = new InputAction("Hand aim flags", InputActionType.Value, p.hand ? device + "/aimFlags" : null);
                p.press.Enable(); p.tracked.Enable(); p.trackingState.Enable(); p.aimFlags.Enable();
                p.wasActive = p.wasPressed = p.armed = false;
            }
        }

        private void LateUpdate()
        {
            bool ready = interactionEnabled && assemblyRoot != null && assemblyRoot.gameObject.activeInHierarchy;
            foreach (var p in pointers)
            {
                p.valid = ready && p.origin != null && p.origin.gameObject.activeInHierarchy &&
                    p.tracked.IsPressed() && (p.trackingState.ReadValue<int>() & 3) == 3;
                if (p.hand)
                {
                    var flags = (MetaAimFlags)(uint)p.aimFlags.ReadValue<int>();
                    p.valid &= (flags & MetaAimFlags.Valid) != 0 &&
                        (flags & (MetaAimFlags.SystemGesture | MetaAimFlags.MenuPressed)) == 0;
                }
            }
            bool anyActive = false;
            foreach (var p in pointers)
            {
                bool active = p.valid;
                // A valid hand aim takes priority over controller input for that hand.
                if (!p.hand)
                    foreach (var other in pointers)
                        if (other.hand && other.left == p.left && other.valid) active = false;
                bool pressed = p.press.IsPressed();
                if (p.line != null) p.line.enabled = active;
                if (!active) { p.wasActive = p.armed = false; p.wasPressed = pressed; continue; }
                anyActive = true;
                // Tracking recovery or a modality switch must not turn an already-held pinch into a click.
                if (!p.wasActive) p.armed = !pressed;
                if (!pressed) p.armed = true;
                var ray = new Ray(p.origin.position, p.origin.forward);
                TryPoint(ray, out var point, out var part, out var button);
                if (p.line != null)
                {
                    p.line.SetPosition(0, ray.origin);
                    p.line.SetPosition(1, point);
                    p.line.startColor = p.line.endColor = button != null && button.IsInteractable() || IsSelectable(part)
                        ? new Color(.3f, 1f, .5f) : selectionColor;
                }
                if (pressed && !p.wasPressed && p.armed)
                {
                    if (button != null)
                    {
                        if (button.IsActive() && button.IsInteractable()) button.onClick.Invoke();
                    }
                    else if (partSelectionEnabled)
                    {
                        SetSelected(IsSelectable(part) ? part : null);
                    }
                }
                p.wasActive = true; p.wasPressed = pressed;
            }
            if (!anyActive || (!ReferenceEquals(SelectedPart, null) && !IsSelectable(SelectedPart))) ClearSelection();
        }

        private bool TryPoint(Ray ray, out Vector3 point, out EditablePart part, out UnityEngine.UI.Button button)
        {
            const int mask = Physics.DefaultRaycastLayers & ~(1 << 30);
            bool hit = Physics.Raycast(ray, out var info, maximumDistance, mask, QueryTriggerInteraction.Ignore);
            float distance = hit ? info.distance : maximumDistance;
            point = ray.GetPoint(distance);
            part = hit ? info.collider.GetComponentInParent<EditablePart>() : null;
            button = hit ? info.collider.GetComponentInParent<UnityEngine.UI.Button>() : null;
            // Test the visible button rectangle, independent of thin or stale physics hitboxes.
            foreach (var candidate in buttons)
            {
                if (candidate == null || !candidate.gameObject.activeInHierarchy) continue;
                var canvas = candidate.GetComponentInParent<Canvas>();
                if (canvas == null || !canvas.isActiveAndEnabled) continue;
                var rect = (RectTransform)candidate.transform;
                var plane = new Plane(rect.forward, rect.position);
                if (!plane.Raycast(ray, out float d) || d > maximumDistance || d > distance + .012f) continue;
                Vector3 local = rect.InverseTransformPoint(ray.GetPoint(d));
                if (!rect.rect.Contains(new Vector2(local.x, local.y))) continue;
                distance = d; point = ray.GetPoint(d); button = candidate; part = null; hit = true;
            }
            if (button != null || IsSelectable(part)) return hit;
            // A 12 mm tolerance helps small fingertips without selecting through a solid surface.
            int count = Physics.SphereCastNonAlloc(ray, .012f, nearbyHits, maximumDistance, mask, QueryTriggerInteraction.Ignore);
            float closest = distance;
            for (int i = 0; i < count; i++)
            {
                var candidate = nearbyHits[i].collider.GetComponentInParent<EditablePart>();
                if (!IsSelectable(candidate) || nearbyHits[i].distance > closest) continue;
                closest = nearbyHits[i].distance; part = candidate;
                point = nearbyHits[i].point; hit = true;
            }
            return hit;
        }

        private bool IsSelectable(EditablePart part) => part != null && part.isActiveAndEnabled && part.IsConfigured &&
            assemblyRoot != null && part.transform.IsChildOf(assemblyRoot);

        public void ClearSelection() => SetSelected(null);

        private void SetSelected(EditablePart part)
        {
            if (ReferenceEquals(SelectedPart, part)) return;
            foreach (var pair in originalBlocks)
                if (pair.Key != null) pair.Key.SetPropertyBlock(pair.Value);
            originalBlocks.Clear();
            SelectedPart = part;
            if (part != null)
            {
                foreach (var renderer in part.GetComponentsInChildren<Renderer>())
                {
                    var original = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(original);
                    originalBlocks.Add(renderer, original);
                    var highlight = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(highlight);
                    highlight.SetColor("_BaseColor", selectionColor);
                    highlight.SetColor("_Color", selectionColor);
                    renderer.SetPropertyBlock(highlight);
                }
            }
            SelectionChanged?.Invoke(part);
        }

        private void OnDisable()
        {
            ClearSelection();
            foreach (var p in pointers)
            {
                p.press?.Dispose(); p.tracked?.Dispose(); p.trackingState?.Dispose(); p.aimFlags?.Dispose();
                if (p.line != null) p.line.enabled = false;
            }
        }
    }
}
