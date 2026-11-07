# Connecting the two instances

Open `Assets/TaskBubble/Networking/Connection.unity`. This is also the first scene in the build settings.

1. Run one instance in **Desktop (Host)** mode and click **Host**.
2. Run a second instance in **VR (Client)** mode and click **Join**.
3. Use `127.0.0.1` when both run on the same PC. The port is `7777`.
4. Click **Disconnect** before changing mode or joining again.

The host status shows whether a VR client is connected. Failed joins show the reason in the client status. Both instances need a matching version of the app.

For the hackathon, Quest Link connects the headset to the PC's VR instance. The connection screen handles the link between the two app instances; XR startup and headset interaction are separate work.

## Role rules

- The desktop instance hosts and receives the Desktop role.
- Only one remote VR client is allowed. Its slot is reserved during approval, so simultaneous joins cannot both succeed.
- Remote clients requesting Desktop, unknown roles, malformed data, or a different protocol version are rejected.
- Disconnecting releases the VR slot. Stopping the host clears all roles.
- Mode and address controls stay locked during a connection.

The approval payload contains two bytes: protocol version `1`, then the requested role (`Desktop = 1`, `Vr = 2`). `TaskBubbleConnection` fills this in; UI code should use `StartDesktopHost()` or `JoinVrClient(address)` rather than starting Netcode directly.

## Using roles in Task 4

The connection component exposes:

- `LocalRole`: this instance's accepted role, or null while disconnected/connecting. Use this for local presentation.
- `HasVrClient`: whether the host currently has an accepted VR client.
- `TryGetRole(clientId, out role)`: host-side lookup of a connected client's role. It returns false for unknown/disconnected clients and whenever called on a non-host.
- `Changed`: refresh the view or check connection state when a client joins, leaves, or the local connection changes.

For incoming RPCs, pass Netcode's sender client ID to `TryGetRole`. Do not trust a role or client ID supplied as a command argument. The [shared contract](SHARED_CONTRACT.md) defines which role may perform each editing action.

This completes the connection layer. Delegation state, ghost updates, and approval of edits belong to Tasks 4 and 10.
