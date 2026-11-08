# Shared edit contract

VR, desktop, and preview code use `TaskBubble.Shared.IDelegationSession`. `Networking/DelegationSession` implements it with host-authoritative Netcode state. MainScene assigns the session's connection and CAD assembly root before connecting.

## Editable parts

`EditablePart` defines a stable ID, one local rotation axis, and finite angle limits that include zero. IDs must be unique, nonempty, and identical on both peers. The demo uses `gripper.fingertip-a` and `gripper.fingertip-b`.

`EditablePartRegistry.Rebuild(assemblyRoot)` includes inactive children and uses case-sensitive IDs. Invalid or duplicate configuration clears the lookup and reports an error. Rebuild if the assembly or identifiers change.

Proposals are angle offsets from the local rotation captured at delegation:

```csharp
startingRotation * Quaternion.AngleAxis(angleDegrees, axis)
```

`TryGetProposalLocalRotation` calculates this without moving the component. Position, scale, and unrestricted rotation are outside the current interface.

## Operations

| Method | Caller | Result |
|---|---|---|
| `RequestDelegation(partId)` | VR | Start a task from the part's current pose while Idle or Accepted. |
| `UpdateProposal(angleDegrees)` | Desktop | Validate and publish an angle offset. |
| `SubmitProposal()` | Desktop | Freeze the proposal for VR review. |
| `AcceptProposal()` | VR | Commit the reviewed pose on both peers. |
| `RejectProposal()` | VR | Keep the real pose and allow desktop revision. |
| `ResetTask()` | VR | Restore the selected part's original demo pose and clear the task. |

These methods send requests rather than returning synchronous success. The host validates the RPC sender's assigned role, task identity, state, part, and angle. It does not trust a role supplied by the caller.

## State transitions

| From | Operation | To |
|---|---|---|
| Idle or Accepted | RequestDelegation | Requested |
| Requested or Editing | UpdateProposal | Editing |
| Editing | SubmitProposal | Review |
| Review | AcceptProposal | Accepted |
| Review | RejectProposal | Rejected |
| Rejected | UpdateProposal | Editing with a new task ID |
| Rejected | SubmitProposal | Review with a new task ID |
| Any task | ResetTask | Idle |

The desktop initializes a Requested task with `UpdateProposal(0f)` once. Rejection retains the starting pose and reviewed values for revision. Every retry receives a new task ID, including unchanged resubmissions, so an old decision cannot affect the retry.

Acceptance permits another delegation of the same or a different part. Its starting rotation is the current committed pose. Reset instead restores the original demo rotation captured when the assembly first joined the session. Reset in Idle is a no-op. Invalid requests leave the task and real component unchanged.

When VR disconnects, Requested, Editing, or Review clears to Idle without moving the component. Despawning clears the local task view. The task counter remains monotonic during the session object's lifetime.

## Reading updates

`Current` is an immutable `DelegationSnapshot`: `TaskId`, `PartId`, `State`, `StartingLocalRotation`, `ProposalLocalRotation`, and `ProposalAngleDegrees`. Idle uses task ID zero; initialize with `DelegationSnapshot.Idle`.

Subscribe to `StateChanged`, then read `Current` for the initial view. Updates include proposal changes that remain in Editing. Unsubscribe when disabled. Accepted and reset poses are applied before notifying the views. `OperationRejected` supplies refusal feedback only to the caller and is not a state change.

The preview renders a cyan ghost during Editing/Review. Rejection hides it until revision; acceptance briefly shows green feedback. These visuals do not change the task or real pose.

## Reconnection limitation

The session replicates the latest committed part pose, not a complete pose collection for the assembly. Continuously connected peers receive both fingertips' accepted changes. A fresh client joining after several parts have been edited may miss an earlier part's accepted pose. For a clean demonstration after losing a process, restart both instances from the saved scene rather than relying on recovery of every edit. See the [README](../README.md).
