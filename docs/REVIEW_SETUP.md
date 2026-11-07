# Review controls

Desktop prefab: `Assets/TaskBubble/Review/DesktopReviewControls.prefab`.

During integration, place it directly under the `DesktopPanel` Canvas. Its anchors position Submit below the editing card. Assign the scene's `DelegationSession` and `TaskBubbleConnection` to its `DesktopReviewControls` component. The button reference is already wired.

Use the scene's existing EventSystem and InputSystemUIInputModule. The prefab uses the parent Canvas and GraphicRaycaster rather than adding another one.

Submit is enabled only for a connected desktop participant with a spawned session in Editing. Clicking it calls `SubmitProposal()`. The host validates the request, publishes Review, and the button disables. Idle, Requested, Review, Accepted, Rejected, and disconnected sessions cannot submit. The callback also checks these conditions if invoked directly.

VR prefab: `Assets/TaskBubble/Review/VrReviewControls.prefab`.

Assign its scene `DelegationSession` and `TaskBubbleConnection`. Its Accept and Reject references are already wired. Both buttons enable only for a connected VR participant during Review and disable while a decision is pending. An authoritative update or refused operation clears the pending state. Accept calls `AcceptProposal()`; Reject calls `RejectProposal()`. The host remains responsible for validation and changes to the real part.

The prefab has its own world-space Canvas, GraphicRaycaster, and large TextMeshPro button labels. At its default scale it is 0.56 m wide and 0.40 m high. Its position is a starting placement; set the final position and Canvas event camera when integrating your teammate's VR rig. Connect the rig's XR UI input and raycasting there. No XR packages, controller bindings, or EventSystem are added by this step. Mouse/keyboard UI callbacks have been checked in Unity; headset ray interaction has not.

Reset is wired into the VR prefab. It enables for the VR participant during Requested, Editing, Review, Accepted, or Rejected, and calls `ResetTask()`. Idle and disconnected sessions cannot reset. All VR actions lock while a decision or reset is pending; failed requests unlock the available actions for retry.

Both prefabs show task progress, Accepted/Rejected results, and Disconnected status. Refused operations display their reason only on the caller's panel. Reset clears the result back to No active task. The feedback text references are optional; removing a message does not disable the core controls. Keep the desktop feedback area below Submit clear of other UI when integrating.

The combined prefab check passed 21 assertions using a temporary desktop host and VR client over localhost inside one Editor. It used the real DesktopPanel slider and close-up, nested DesktopReviewControls, and VrReviewControls with a test mesh. Both decision paths, Reset, UI re-enabling during Review, and disconnect during Review worked together. The combined desktop render was checked at 1280 x 720, with zero Console errors or warnings. No permanent test scene or test scripts were added.

After scene integration, request an edit from VR, adjust the desktop slider, and click Submit. Confirm the slider locks while the proposal is reviewed. Accept should apply it on both clients; Reject should leave the committed part unchanged. Reset should restore the original demo rotation and clear the UI. Repeat with a disconnect during Review.

These controls are not yet placed in the main scene or nested into the committed desktop/VR prefabs. The check did not use separate Windows builds, the final CAD assembly, or headset input. Those checks remain part of integration.
