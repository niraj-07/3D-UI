# Shared edit contract

VR, desktop, and preview code use `TaskBubble.Shared`. The networking code will implement `IDelegationSession` in task 4 and the approval/reset behavior in task 10.

## The editable part

Add `EditablePart` to the CAD part that can be edited. Set its ID, local rotation axis, and angle limits in the Inspector. Use the same prefab and settings on both clients.

- Use a permanent ID such as `gripper.connector`. Keep it when renaming or moving the object. Each editable part needs a unique, nonempty ID without surrounding spaces.
- The default is local Z with offsets from -45 to +45 degrees. These are starter values; choose the actual axis and limits when preparing the CAD part.
- Limits include both endpoints and must include zero, which means no change. Invalid settings, non-finite angles, out-of-range angles, and invalid starting rotations are rejected.
- `TryGetProposalLocalRotation(startingRotation, angle, out rotation)` calculates a local rotation without changing the real object. It rotates around the part's local axis at the start of the task: `startingRotation * Quaternion.AngleAxis(angle, axis)`.

Task 4 must check for missing or duplicate IDs when registering parts and refuse delegation for invalid parts. This component does not register itself or start networking.

`TaskBubble.Networking.EditablePartRegistry` handles this lookup. Call `Rebuild(assemblyRoot)` after the assembly loads, then use `TryGet(partId, out part)`. Registration includes inactive children and uses case-sensitive IDs. If any part is invalid, registration returns false, exposes the reason through `Error`, and clears the whole lookup. Fix the part and rebuild before allowing delegation. Rebuild again if the assembly or its IDs change; the registry does not watch scene changes automatically.

## Methods

| Method | Caller | Meaning |
|---|---|---|
| `RequestDelegation(string partId)` | VR | Request an edit of that part while Idle. |
| `UpdateProposal(float angleDegrees)` | Desktop | Propose an angle offset from the rotation captured when the task started. |
| `SubmitProposal()` | Desktop | Freeze the proposal and ask VR to review it. |
| `AcceptProposal()` | VR | Apply the reviewed proposal to the real part on both clients. |
| `RejectProposal()` | VR | Discard the proposal and leave the real part unchanged. |
| `ResetTask()` | VR | Restore the original demo rotation and clear the task on both clients. |

`UpdateProposal` takes degrees rather than a Unity `Transform`: the MVP allows rotation on one axis, so position, scale, and other rotation axes are not editable.

These methods send requests; they do not report synchronous success. The host must check the caller's assigned role, current task, state, and rotation limits. Clients cannot approve their own desktop proposals.

## State flow

`Idle → Requested → Editing → Review → Accepted/Rejected → Idle`

| Current state | Valid request | Result |
|---|---|---|
| Idle | RequestDelegation | Requested; capture the current committed local rotation and start with angle zero. |
| Requested | UpdateProposal | Editing; calculate and publish the proposal. |
| Editing | UpdateProposal | Editing; publish the new proposal. |
| Editing | SubmitProposal | Review; stop accepting adjustments. |
| Review | AcceptProposal | Accepted; apply the proposal to the committed part. |
| Review | RejectProposal | Rejected; discard the proposal. |
| Any active or finished task | ResetTask | Idle; restore the original demo pose and clear the task. |

The desktop view can call `UpdateProposal(0f)` when opening a newly requested task to start editing. Do that once on Requested, not on every state update. Submit is valid only in Editing.

Keep Accepted/Rejected visible until ResetTask. Reset while Idle is a no-op. All other invalid requests leave the shared state and real part unchanged.

## Reading updates

`Current` returns an immutable `DelegationSnapshot` containing:

- `TaskId`: a host-issued nonzero ID for this delegation. Zero means Idle.
- `PartId` and `State`.
- `StartingLocalRotation`: the committed local rotation captured at delegation time. It stays fixed throughout that task.
- `ProposalLocalRotation` and `ProposalAngleDegrees`: the latest validated proposal, including the final reviewed proposal in Accepted/Rejected.

Initialize sessions with `DelegationSnapshot.Idle`, not `default(DelegationSnapshot)`.

Subscribe to `StateChanged`, then read `Current` to draw the initial view. `StateChanged` fires after each authoritative update arrives, including angle changes while the state stays Editing. Unsubscribe when the view is disabled. Show a ghost during Editing/Review and hide it in Idle/Accepted/Rejected.

`OperationRejected` reports a readable reason only to the caller whose request failed. Use it for feedback; do not treat it as a state update.

Task 4 owns serialization and transport. Capture `Current.TaskId` when sending task-specific requests and include it in the network message. The host must reject stale IDs and issue a new ID for every delegation, even when the same part is selected again. The UI does not generate IDs or pass client roles as proof of permission.

## Delegation session setup

`TaskBubble.Networking.DelegationSession` implements the shared interface. During scene integration, add one scene object with `NetworkObject` and `DelegationSession`, assign the scene's `TaskBubbleConnection` and CAD assembly root, and use the same scene on both peers. Keep the session enabled before starting the connection so Netcode spawns it with the scene. It registers the assembly on network spawn; load the assembly before connecting.

Step 2 implements `RequestDelegation`: the host checks the RPC sender's assigned VR role, Idle state, registered part, and starting rotation. It publishes a server-written snapshot with a nonzero task ID and zero angle. Joining clients receive the current snapshot too. Rejected requests notify only their caller. No request moves the real part.

Step 3 implements `UpdateProposal`: it sends the task ID observed by the caller with the requested angle. The host requires the Desktop role, a matching nonzero task ID, Requested or Editing state, and a valid registered part. It uses the captured starting rotation and the part's local axis and limits to calculate the proposal. Valid updates publish Editing, the new angle, and the proposed local rotation to both peers. Invalid updates leave the snapshot unchanged. Views can use these snapshots for the ghost; the session does not create or move a ghost or the real part.

Step 4 implements `SubmitProposal`: the host requires the Desktop role, the current nonzero task ID, Editing state, and a valid part and angle. Submission changes the state to Review while keeping the captured start, proposed rotation, and angle. Further adjustments and duplicate submissions are refused.

When VR disconnects, the host clears Requested, Editing, or Review to Idle without moving the part. Both local views also clear when their network object despawns. The session clears its stored snapshot on server despawn so restarting it cannot revive an unfinished task. Its task ID counter stays monotonic while that session object exists, including across reconnects.

Task 10's first step implements `AcceptProposal`. The host requires the VR role, a matching task ID, and Review state. It rechecks the part and rotation settings against the reviewed proposal before committing. Only then do both peers apply the proposed local rotation and publish Accepted. Duplicate approvals, desktop approvals, stale IDs, and invalid parts leave the result unchanged.

The MVP's one editable part has a separate server-written committed rotation. Rejoining clients receive it, and clearing or respawning the task session does not undo an accepted edit. Each peer applies the rotation before notifying its views of Accepted. Keep the same part ID and CAD hierarchy on both peers.

Task 10's second step implements `RejectProposal`. The host requires the VR role, the current nonzero task ID, and Review state. Rejection publishes Rejected while retaining the reviewed values for feedback. It does not change either real part or the committed rotation, including an earlier accepted edit. Duplicate decisions and further adjustments are refused; Rejected stays visible until Reset.

Task 10's final step implements `ResetTask`. The host requires the VR role and a matching task ID. Reset in Idle is a no-op. From an active or finished task, it restores the selected part's original demo rotation, updates the committed pose, and clears the public snapshot to Idle on both peers. Stale resets and unavailable parts leave state and poses unchanged. The reset update includes its restored pose so views observe the part restored before receiving Idle.

All shared session methods are now implemented. This component has not yet been added to the connection scene or connected to UI; Accept, Reject, and Reset buttons are separate UI/integration work.

The host captures valid original demo rotations at the first network spawn, before applying any saved committed pose, and keeps them for the session object's lifetime. Load the assembly before connecting and keep its IDs stable. Respawning the same session object does not recapture accepted rotations as original poses. `StartingLocalRotation` remains the current committed rotation at task creation, which may differ from the original demo rotation. Accepted and reset poses are applied before notifying views of the resulting state.

Keep all proposal calculations in local space, with the same CAD hierarchy on both clients. Shared types do not depend on Netcode, XR, or a UI framework.
