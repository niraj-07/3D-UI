using TMPro;
using UnityEngine;

namespace TaskBubble.Networking
{
    public sealed class ConnectionPanel : MonoBehaviour
    {
        [SerializeField] private TaskBubbleConnection connection;
        [SerializeField] private TMP_Dropdown mode;
        [SerializeField] private TMP_InputField hostAddress;
        [SerializeField] private UnityEngine.UI.Button hostButton;
        [SerializeField] private UnityEngine.UI.Button joinButton;
        [SerializeField] private UnityEngine.UI.Button disconnectButton;
        [SerializeField] private TMP_Text status;
        [SerializeField] private TMP_Text instructions;

        private bool wired;

        private void Awake()
        {
            wired = mode != null && hostAddress != null && hostButton != null && joinButton != null &&
                disconnectButton != null && status != null;
            if (!wired)
            {
                Debug.LogError("ConnectionPanel has missing UI references.", this);
                enabled = false;
                return;
            }

            mode.onValueChanged.AddListener(OnModeChanged);
            hostButton.onClick.AddListener(Host);
            joinButton.onClick.AddListener(Join);
            disconnectButton.onClick.AddListener(Disconnect);
        }

        private void OnEnable()
        {
            if (connection != null) connection.Changed += Refresh;
            Refresh();
        }

        private void Start()
        {
            // All connection components have now completed Awake, regardless of prefab order.
            Refresh();
        }

        private void OnDisable()
        {
            if (connection != null) connection.Changed -= Refresh;
        }

        private void OnDestroy()
        {
            if (!wired) return;
            mode.onValueChanged.RemoveListener(OnModeChanged);
            hostButton.onClick.RemoveListener(Host);
            joinButton.onClick.RemoveListener(Join);
            disconnectButton.onClick.RemoveListener(Disconnect);
        }

        private void OnModeChanged(int selectedMode) => Refresh();

        private void Host()
        {
            if (mode.value == 0 && connection != null) connection.StartDesktopHost();
        }

        private void Join()
        {
            if (mode.value == 1 && connection != null) connection.JoinVrClient(hostAddress.text);
        }

        private void Disconnect()
        {
            if (connection != null) connection.Disconnect();
        }

        private void Refresh()
        {
            if (!wired) return;

            bool canStart = connection != null && connection.CanStart;
            bool vr = mode.value == 1;
            mode.interactable = canStart;
            hostAddress.interactable = canStart && vr;
            hostButton.interactable = canStart && !vr;
            joinButton.interactable = canStart && vr;
            disconnectButton.interactable = connection != null && !canStart &&
                connection.State != ConnectionState.Disconnected && connection.State != ConnectionState.Disconnecting;
            status.text = connection != null ? connection.Status : "Connection component is not assigned.";
            if (instructions != null)
                instructions.text = vr
                    ? "Join the desktop host. Use 127.0.0.1 when both instances run on this PC."
                    : "Start the desktop host first, then join from the VR instance.";
        }
    }
}
