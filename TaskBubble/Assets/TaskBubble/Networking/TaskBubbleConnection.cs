using System;
using System.Net;
using System.Net.Sockets;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace TaskBubble.Networking
{
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

        private NetworkManager networkManager;
        private UnityTransport transport;
        private bool sessionStarted;
        private bool disconnectRequested;
        private bool startingHost;
        private string failureReason;
        private bool previousRunInBackground;
        private bool backgroundOverride;

        public ConnectionState State { get; private set; } = ConnectionState.Disconnected;
        public string Status { get; private set; } = "Disconnected";
        public string ServerAddress { get; private set; } = "127.0.0.1";
        public bool IsDesktopHost => networkManager != null && networkManager.IsHost;
        public bool CanStart => Application.isPlaying && isActiveAndEnabled && networkManager != null &&
            !sessionStarted && !networkManager.IsListening && !networkManager.ShutdownInProgress;

        public event Action Changed;

        private void Awake()
        {
            networkManager = GetComponent<NetworkManager>();
            transport = GetComponent<UnityTransport>();
            networkManager.NetworkConfig.NetworkTransport = transport;
        }

        private void OnEnable()
        {
            networkManager.OnClientConnectedCallback += OnClientConnected;
            networkManager.OnClientDisconnectCallback += OnClientDisconnected;
            networkManager.OnTransportFailure += OnTransportFailure;
        }

        private void OnDisable()
        {
            if (networkManager == null) return;

            Disconnect();
            networkManager.OnClientConnectedCallback -= OnClientConnected;
            networkManager.OnClientDisconnectCallback -= OnClientDisconnected;
            networkManager.OnTransportFailure -= OnTransportFailure;
            sessionStarted = false;
            RestoreBackgroundMode();
            SetState(ConnectionState.Disconnected, "Disconnected");
        }

        private void Update()
        {
            // Shutdown completes asynchronously. Don't permit another start until it has finished.
            if (!sessionStarted || networkManager.IsListening || networkManager.ShutdownInProgress) return;

            sessionStarted = false;
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

        private bool StartConnection(bool asHost, string address)
        {
            if (!CanStart) return false;

            startingHost = asHost;
            ServerAddress = address;
            disconnectRequested = false;
            failureReason = null;
            sessionStarted = true;
            previousRunInBackground = Application.runInBackground;
            backgroundOverride = true;
            // Both Windows instances must keep networking while the other has focus.
            Application.runInBackground = true;
            SetState(ConnectionState.Connecting, asHost ? "Starting desktop host..." : "Connecting to desktop host...");

            try
            {
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
                SetState(ConnectionState.Connected, startingHost ? "Desktop host ready." : "VR client connected.");
            else
                Changed?.Invoke();
        }

        private void OnClientDisconnected(ulong clientId)
        {
            if (clientId != networkManager.LocalClientId)
            {
                Changed?.Invoke();
                return;
            }

            if (!disconnectRequested)
                Fail(string.IsNullOrWhiteSpace(networkManager.DisconnectReason)
                    ? "Connection lost or the host could not be reached. You can reconnect."
                    : networkManager.DisconnectReason);
        }

        private void OnTransportFailure()
        {
            Fail("Network transport failed. Check the address and that port 7777 is available.");
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
