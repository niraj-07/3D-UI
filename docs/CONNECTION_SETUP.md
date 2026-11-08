# Connection setup

Use `Assets/TaskBubble/Integration/MainScene.unity` for the complete application. The older `Networking/Connection.unity` scene contains only the connection screen.

Run a Windows build as **Desktop (Host)** and the Unity Editor as **VR (Client)**. Start the host, then join `127.0.0.1` from the client. Both processes run on the same PC for the Link/Air Link demo and use UDP port `7777`. Use matching project versions. See the [README](../README.md) for the full run sequence.

Quest Link or Air Link supplies the PC VR connection. Unity Netcode carries TaskBubble's requests, proposals, and decisions between the two application processes.

## Roles and validation

- The local host has the Desktop role. One remote VR client is allowed.
- Connection approval rejects unknown roles, remote Desktop requests, malformed payloads, and incompatible protocol versions.
- The approval payload is protocol byte `1`, followed by the requested role (`Desktop = 1`, `Vr = 2`).
- Disconnect before changing mode or joining again. Disconnect releases the VR slot.

`TaskBubbleConnection` exposes `StartDesktopHost()`, `JoinVrClient(address)`, `Disconnect()`, connection status, and a `Changed` event. `LocalRole` drives local presentation; the host uses `TryGetRole(senderClientId, out role)` to validate RPC senders. A client-supplied role is not proof of permission.

The [shared contract](SHARED_CONTRACT.md) defines editing permissions and the current reconnection limitation.
