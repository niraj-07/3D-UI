using TaskBubble.Networking;
using TaskBubble.VR;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TaskBubble.Integration
{
    // Owns the desktop/headset view switch, not task state or network commands.
    public sealed class WorkspaceView : MonoBehaviour
    {
        [SerializeField] private TaskBubbleConnection connection;
        [SerializeField] private PcVrRig vrRig;
        [SerializeField] private Camera desktopCamera;
        [SerializeField] private GameObject connectionPanel;
        [SerializeField] private GameObject previewPanel;
        [SerializeField] private UnityEngine.UI.Button previewButton;
        [SerializeField] private TMP_Text previewStatus;
        [SerializeField] private GameObject desktopEditingPanel;
        [SerializeField] private GameObject vrDelegationPanel;
        [SerializeField] private GameObject vrReviewPanel;
        private bool startQueued;
        private bool networkVr;
        private bool menusVisible;
        private VrPartSelection selection;
        private bool desktopFrameLimitApplied;
        private int previousTargetFrameRate;

        private void OnEnable()
        {
            selection = vrRig != null ? vrRig.GetComponentInChildren<VrPartSelection>(true) : null;
            if (selection != null)
            {
                selection.SetMenuRoots(vrDelegationPanel, vrReviewPanel);
                selection.MenuToggleRequested += ToggleMenus;
            }
            if (previewButton != null) previewButton.onClick.AddListener(PreviewInHeadset);
            if (connection != null) connection.Changed += OnConnectionChanged;
        }

        private void Start() => OnConnectionChanged();

        private void OnConnectionChanged()
        {
            bool connectedVr = connection != null && connection.State == ConnectionState.Connected &&
                connection.LocalRole == ParticipantRole.Vr;
            if (connectedVr && !networkVr) startQueued = true;
            else if (!connectedVr && networkVr) ReturnToDesktop();
            networkVr = connectedVr;
            RefreshPanels();
        }

        private bool IsDesktop => connection != null && connection.State == ConnectionState.Connected &&
            connection.LocalRole == ParticipantRole.Desktop;

        public void PreviewInHeadset()
        {
            if (IsDesktop) { connection.Disconnect(); return; }
            startQueued = true;
        }

        private void RefreshPanels()
        {
            bool running = vrRig != null && vrRig.IsRunning;
            SetDesktopFrameLimit(IsDesktop && !running);
            bool connectedVr = connection != null && connection.State == ConnectionState.Connected &&
                connection.LocalRole == ParticipantRole.Vr;
            if (desktopEditingPanel != null) desktopEditingPanel.SetActive(IsDesktop && !running);
            if (vrDelegationPanel != null) vrDelegationPanel.SetActive(connectedVr && running);
            if (vrReviewPanel != null) vrReviewPanel.SetActive(connectedVr && running);
            SetMenuVisible(vrDelegationPanel, menusVisible && connectedVr && running);
            SetMenuVisible(vrReviewPanel, menusVisible && connectedVr && running);
            if (connectionPanel != null) connectionPanel.SetActive(!running && !IsDesktop);
            if (previewPanel != null) previewPanel.SetActive(!running);
            if (previewButton != null)
            {
                var label = previewButton.GetComponentInChildren<TMP_Text>();
                if (label != null) label.text = IsDesktop ? "Disconnect" : "Preview in headset";
                var rect = previewButton.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = rect.pivot = IsDesktop ? Vector2.one : new Vector2(1, 0);
                rect.anchoredPosition = IsDesktop ? new Vector2(-24, -80) : new Vector2(-24, 24);
                rect.sizeDelta = IsDesktop ? new Vector2(160, 44) : new Vector2(300, 50);
            }
            if (previewStatus != null)
                previewStatus.text = IsDesktop ? "Desktop host ready. Delegate a fingertip from VR." :
                    "Connect Quest Link or Air Link, then preview. Esc returns to desktop.";
        }

        private void SetDesktopFrameLimit(bool apply)
        {
            if (apply && !desktopFrameLimitApplied)
            {
                previousTargetFrameRate = Application.targetFrameRate;
                // Leave GPU time for the VR process and Air Link encoding on the same PC.
                Application.targetFrameRate = previousTargetFrameRate > 0 ? Mathf.Min(previousTargetFrameRate, 30) : 30;
                desktopFrameLimitApplied = true;
            }
            else if (!apply && desktopFrameLimitApplied)
            {
                Application.targetFrameRate = previousTargetFrameRate;
                desktopFrameLimitApplied = false;
            }
        }

        private static void SetMenuVisible(GameObject panel, bool visible)
        {
            if (panel == null) return;
            // Keep session subscriptions alive while hiding both visuals and hit targets.
            foreach (var canvas in panel.GetComponentsInChildren<Canvas>(true)) canvas.enabled = visible;
            foreach (var collider in panel.GetComponentsInChildren<Collider>(true)) collider.enabled = visible;
        }

        private void ToggleMenus()
        {
            if (!networkVr || vrRig == null || !vrRig.IsRunning) return;
            menusVisible = !menusVisible;
            if (menusVisible)
            {
                var head = vrRig.GetComponentInChildren<Camera>(true);
                if (head != null)
                {
                    var forward = Vector3.ProjectOnPlane(head.transform.forward, Vector3.up).normalized;
                    if (forward.sqrMagnitude < .5f) forward = vrRig.transform.forward;
                    var right = Vector3.Cross(Vector3.up, forward);
                    PlaceMenu(vrDelegationPanel, head.transform.position, forward, right, -.65f);
                    PlaceMenu(vrReviewPanel, head.transform.position, forward, right, .65f);
                }
            }
            RefreshPanels();
        }

        private static void PlaceMenu(GameObject panel, Vector3 head, Vector3 forward, Vector3 right, float offset)
        {
            if (panel == null) return;
            Vector3 position = head + forward * 1.1f + right * offset + Vector3.down * .2f;
            panel.transform.SetPositionAndRotation(position, Quaternion.LookRotation(position - head, Vector3.up));
        }

        private void LateUpdate()
        {
            // LateUpdate runs after every Start; manual XR initialization requires graphics to be ready.
            if (startQueued)
            {
                startQueued = false;
                StartHeadsetView();
            }
            if (vrRig != null && vrRig.IsRunning && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                ReturnToDesktop();
        }

        private void StartHeadsetView()
        {
            if (vrRig == null || desktopCamera == null) return;
            SetDesktopFrameLimit(false);
            menusVisible = false;
            desktopCamera.enabled = false;
            if (!vrRig.StartVr())
            {
                desktopCamera.enabled = true;
                var desktopListener = desktopCamera.GetComponent<AudioListener>();
                if (desktopListener != null) desktopListener.enabled = true;
                if (previewStatus != null) previewStatus.text = vrRig.Status;
                return;
            }
            var listener = desktopCamera.GetComponent<AudioListener>();
            if (listener != null) listener.enabled = false;
            var selection = vrRig.GetComponentInChildren<VrPartSelection>(true);
            // Offline preview can select locally; a connected session is gated by Delegate controls.
            if (selection != null && !networkVr)
            {
                selection.InteractionEnabled = true;
                selection.PartSelectionEnabled = true;
            }
            RefreshPanels();
            if (previewStatus != null) previewStatus.text = "Headset view active. Press Esc on the PC to return.";
        }

        public void ReturnToDesktop()
        {
            startQueued = false;
            menusVisible = false;
            if (vrRig != null) vrRig.StopVr();
            if (desktopCamera != null)
            {
                desktopCamera.enabled = true;
                var listener = desktopCamera.GetComponent<AudioListener>();
                if (listener != null) listener.enabled = true;
            }
            RefreshPanels();
        }

        private void OnDisable()
        {
            if (selection != null) selection.MenuToggleRequested -= ToggleMenus;
            if (previewButton != null) previewButton.onClick.RemoveListener(PreviewInHeadset);
            if (connection != null) connection.Changed -= OnConnectionChanged;
            ReturnToDesktop();
            SetDesktopFrameLimit(false);
        }
    }
}
