using System;
using System.Net;
using System.Net.Sockets;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace TaskBubble.Networking
{
    public enum ParticipantRole : byte
    {
        Desktop = 1,
        Vr = 2
    }

    public enum ConnectionState
    {
        Disconnected,
        Connecting,
        Connected,
        Disconnecting,
        Error
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkManager), typeof(UnityTransport))]
    public sealed class TaskBubbleConnection : MonoBehaviour
    {
        public const ushort Port = 7777;
        private const byte ProtocolVersion = 1;

        private NetworkManager networkManager;
        private UnityTransport transport;
        private bool sessionStarted;
        private bool disconnectRequested;
        private bool startingHost;
        private string failureReason;
        private bool previousRunInBackground;
        private bool backgroundOverride;
        private ulong? vrClientId;

        public ConnectionState State { get; private set; } = ConnectionState.Disconnected;
        public string Status { get; private set; } = "Disconnected";
        public string ServerAddress { get; private set; } = "127.0.0.1";
        public bool IsDesktopHost => networkManager != null && networkManager.IsHost;
        public ParticipantRole? LocalRole { get; private set; }
        public bool HasVrClient => IsDesktopHost && !networkManager.ShutdownInProgress &&
            vrClientId.HasValue && networkManager.ConnectedClients.ContainsKey(vrClientId.Value);
        public bool CanStart => Application.isPlaying && isActiveAndEnabled && networkManager != null &&
            !sessionStarted && !networkManager.IsListening && !networkManager.ShutdownInProgress;

        public event Action Changed;

        private void Awake()
        {
            networkManager = GetComponent<NetworkManager>();
            transport = GetComponent<UnityTransport>();
            networkManager.NetworkConfig.NetworkTransport = transport;
            networkManager.NetworkConfig.ConnectionApproval = true;
        }

        private void OnEnable()
        {
            networkManager.OnClientConnectedCallback += OnClientConnected;
            networkManager.OnClientDisconnectCallback += OnClientDisconnected;
            networkManager.OnTransportFailure += OnTransportFailure;
            networkManager.ConnectionApprovalCallback = ApproveConnection;
        }

        private void OnDisable()
        {
            if (networkManager == null) return;

            Disconnect();
            networkManager.OnClientConnectedCallback -= OnClientConnected;
            networkManager.OnClientDisconnectCallback -= OnClientDisconnected;
            networkManager.OnTransportFailure -= OnTransportFailure;
            if (networkManager.ConnectionApprovalCallback == ApproveConnection)
                networkManager.ConnectionApprovalCallback = null;
            sessionStarted = false;
            vrClientId = null;
            LocalRole = null;
            RestoreBackgroundMode();
            SetState(ConnectionState.Disconnected, "Disconnected");
        }

        private void Update()
        {
            // Shutdown completes asynchronously. Don't permit another start until it has finished.
            if (!sessionStarted || networkManager.IsListening || networkManager.ShutdownInProgress) return;

            sessionStarted = false;
            vrClientId = null;
            LocalRole = null;
            RestoreBackgroundMode();
            if (disconnectRequested)
                SetState(ConnectionState.Disconnected, "Disconnected");
            else
                SetState(ConnectionState.Error, failureReason ?? "Connection closed. You can reconnect.");
        }

        public bool StartDesktopHost()
        {
            return StartConnection(true, "127.0.0.1");
        }

        public bool JoinVrClient(string serverAddress)
        {
            if (!CanStart) return false;

            string address = serverAddress?.Trim();
            if (!IPAddress.TryParse(address, out var parsed) || parsed.AddressFamily != AddressFamily.InterNetwork ||
                parsed.Equals(IPAddress.Any) || parsed.Equals(IPAddress.Broadcast))
            {
                SetState(ConnectionState.Error, "Enter the host's IPv4 address, e.g. 127.0.0.1 on the same PC.");
                return false;
            }

            return StartConnection(false, parsed.ToString());
        }

        public void Disconnect()
        {
            if (networkManager == null || (!sessionStarted && !networkManager.IsListening)) return;

            disconnectRequested = true;
            SetState(ConnectionState.Disconnecting, "Disconnecting...");
            if (!networkManager.ShutdownInProgress) networkManager.Shutdown();
        }

        // Task 4 must call this on the host using the RPC sender's client ID.
        // Pending approvals, disconnected clients, and client-side lookups cannot grant permissions.
        public bool TryGetRole(ulong clientId, out ParticipantRole role)
        {
            role = default;
            if (!IsDesktopHost || networkManager.ShutdownInProgress ||
                State != ConnectionState.Connected || !networkManager.ConnectedClients.ContainsKey(clientId))
                return false;

            if (clientId == NetworkManager.ServerClientId)
            {
                role = ParticipantRole.Desktop;
                return true;
            }

            if (vrClientId == clientId)
            {
                role = ParticipantRole.Vr;
                return true;
            }

            return false;
        }

        private bool StartConnection(bool asHost, string address)
        {
            if (!CanStart) return false;

            startingHost = asHost;
            ServerAddress = address;
            disconnectRequested = false;
            failureReason = null;
            sessionStarted = true;
            vrClientId = null;
            LocalRole = null;
            previousRunInBackground = Application.runInBackground;
            backgroundOverride = true;
            // Both Windows instances must keep networking while the other has focus.
            Application.runInBackground = true;
            SetState(ConnectionState.Connecting, asHost ? "Starting desktop host..." : "Connecting to desktop host...");

            try
            {
                networkManager.NetworkConfig.ConnectionApproval = true;
                networkManager.NetworkConfig.ConnectionData = new[]
                {
                    ProtocolVersion, (byte)(asHost ? ParticipantRole.Desktop : ParticipantRole.Vr)
                };
                transport.SetConnectionData(address, Port, "0.0.0.0");
                bool started = asHost ? networkManager.StartHost() : networkManager.StartClient();
                if (started) return true;

                Fail("Could not start the connection. Check that port 7777 is available.");
            }
            catch (Exception exception)
            {
                Fail("Could not start the connection: " + exception.Message);
            }

            sessionStarted = networkManager.IsListening || networkManager.ShutdownInProgress;
            if (!sessionStarted) RestoreBackgroundMode();
            return false;
        }

        private void OnClientConnected(ulong clientId)
        {
            if (clientId == networkManager.LocalClientId)
            {
                LocalRole = startingHost ? ParticipantRole.Desktop : ParticipantRole.Vr;
                if (!startingHost) SetState(ConnectionState.Connected, "VR client connected.");
            }
            if (IsDesktopHost) RefreshHostStatus();
        }

        private void OnClientDisconnected(ulong clientId)
        {
            if (clientId != networkManager.LocalClientId)
            {
                if (vrClientId == clientId) vrClientId = null;
                RefreshHostStatus();
                return;
            }

            LocalRole = null;
            if (!disconnectRequested)
                Fail(string.IsNullOrWhiteSpace(networkManager.DisconnectReason)
                    ? "Connection lost or the host could not be reached. You can reconnect."
                    : networkManager.DisconnectReason);
        }

        private void OnTransportFailure()
        {
            Fail("Network transport failed. Check the address and that port 7777 is available.");
        }

        private void ApproveConnection(
            NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            response.Approved = false;
            response.Pending = false;
            response.CreatePlayerObject = false;

            // The host's own ID is assigned by Netcode, never taken from client-supplied data.
            if (request.ClientNetworkId == NetworkManager.ServerClientId)
            {
                response.Approved = true;
                return;
            }

            if (request.Payload == null || request.Payload.Length != 2)
            {
                response.Reason = "Invalid TaskBubble connection data. Use a matching app build.";
                return;
            }
            if (request.Payload[0] != ProtocolVersion)
            {
                response.Reason = "TaskBubble connection versions do not match. Use the same app build.";
                return;
            }
            if (request.Payload[1] == (byte)ParticipantRole.Desktop)
            {
                response.Reason = "Only the desktop host can use the Desktop role. Join as VR.";
                return;
            }
            if (request.Payload[1] != (byte)ParticipantRole.Vr)
            {
                response.Reason = "Invalid TaskBubble participant role. Join as VR.";
                return;
            }
            if (vrClientId.HasValue && vrClientId != request.ClientNetworkId)
            {
                response.Reason = "A VR client is already connected. Disconnect it before joining another.";
                return;
            }

            // Reserve the slot during approval so simultaneous requests cannot both take it.
            vrClientId = request.ClientNetworkId;
            response.Approved = true;
        }

        private void RefreshHostStatus()
        {
            if (!IsDesktopHost || disconnectRequested || networkManager.ShutdownInProgress) return;
            SetState(ConnectionState.Connected, HasVrClient
                ? "Desktop host ready. VR client connected."
                : "Desktop host ready. Waiting for a VR client.");
        }

        private void Fail(string reason)
        {
            failureReason = reason;
            SetState(ConnectionState.Error, reason);
            if (networkManager.IsListening && !networkManager.ShutdownInProgress) networkManager.Shutdown();
        }

        private void SetState(ConnectionState state, string status)
        {
            State = state;
            Status = status;
            Changed?.Invoke();
        }

        private void RestoreBackgroundMode()
        {
            if (!backgroundOverride) return;
            Application.runInBackground = previousRunInBackground;
            backgroundOverride = false;
        }
    }
}
