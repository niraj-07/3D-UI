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
        private bool startQueued;
        private bool networkVr;

        private void OnEnable()
        {
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
        }

        public void PreviewInHeadset() => startQueued = true;

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
            if (connectionPanel != null) connectionPanel.SetActive(false);
            if (previewPanel != null) previewPanel.SetActive(false);
            var selection = vrRig.GetComponentInChildren<VrPartSelection>(true);
            if (selection != null)
            {
                selection.InteractionEnabled = true;
                selection.PartSelectionEnabled = true;
            }
            if (previewStatus != null) previewStatus.text = "Headset view active. Press Esc on the PC to return.";
        }

        public void ReturnToDesktop()
        {
            startQueued = false;
            if (vrRig != null) vrRig.StopVr();
            if (desktopCamera != null)
            {
                desktopCamera.enabled = true;
                var listener = desktopCamera.GetComponent<AudioListener>();
                if (listener != null) listener.enabled = true;
            }
            if (connectionPanel != null) connectionPanel.SetActive(true);
            if (previewPanel != null) previewPanel.SetActive(true);
            if (previewStatus != null) previewStatus.text = "Connect Quest Link or Air Link, then preview. Esc returns to desktop.";
        }

        private void OnDisable()
        {
            if (previewButton != null) previewButton.onClick.RemoveListener(PreviewInHeadset);
            if (connection != null) connection.Changed -= OnConnectionChanged;
            ReturnToDesktop();
        }
    }
}
