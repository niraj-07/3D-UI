# Review controls

MainScene includes both review prefabs and their session/connection references.

- `Review/DesktopReviewControls.prefab` provides Submit under the desktop editing panel.
- `Review/VrReviewControls.prefab` provides Accept, Reject, Reset, and status in VR.

## Behavior

Submit is available to the connected desktop participant during **Editing** or **Rejected**. It calls `SubmitProposal()` and locks editing while VR reviews the proposal. A rejected proposal can be adjusted or resubmitted unchanged.

Accept and Reject are available to VR during **Review**. Accept commits the proposal on both peers; Reject keeps the real component unchanged and allows desktop revision. Reset is available to VR whenever a task exists. It restores that task's part to its original demo pose and clears the task.

VR actions lock while a decision or reset is pending. Authoritative updates or refusal feedback release that lock. The host checks role, task identity, state, and rotation rules independently of the buttons.

The VR buttons use the same controller/hand rays as part selection. Their visible rectangles and colliders provide hit targets. Hiding menus disables both canvases and hitboxes without stopping task updates. See [VR controls](VR_SETUP.md).

Acceptance also gives a brief green part highlight and **Change accepted** badge; see [ghost preview](GHOST_SETUP.md).
