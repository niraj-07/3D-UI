# Review controls

Desktop prefab: `Assets/TaskBubble/Review/DesktopReviewControls.prefab`.

During integration, place it directly under the `DesktopPanel` Canvas. Its anchors position Submit below the editing card. Assign the scene's `DelegationSession` and `TaskBubbleConnection` to its `DesktopReviewControls` component. The button reference is already wired.

Use the scene's existing EventSystem and InputSystemUIInputModule. The prefab uses the parent Canvas and GraphicRaycaster rather than adding another one.

Submit is enabled only for a connected desktop participant with a spawned session in Editing. Clicking it calls `SubmitProposal()`. The host validates the request, publishes Review, and the button disables. Idle, Requested, Review, Accepted, Rejected, and disconnected sessions cannot submit. The callback also checks these conditions if invoked directly.

VR prefab: `Assets/TaskBubble/Review/VrReviewControls.prefab`.

Assign its scene `DelegationSession` and `TaskBubbleConnection`. Its Accept and Reject references are already wired. Both buttons enable only for a connected VR participant during Review and disable while a decision is pending. An authoritative update or refused operation clears the pending state. Accept calls `AcceptProposal()`; Reject calls `RejectProposal()`. The host remains responsible for validation and changes to the real part.

The prefab has its own world-space Canvas, GraphicRaycaster, and large TextMeshPro button labels. At its default scale it is 0.56 m wide and 0.20 m high. Its position is a starting placement; set the final position and Canvas event camera when integrating your teammate's VR rig. Connect the rig's XR UI input and raycasting there. No XR packages, controller bindings, or EventSystem are added by this step. Mouse/keyboard UI callbacks have been checked in Unity; headset ray interaction has not.

These controls are not yet placed in a scene or nested into the desktop/VR prefabs. Reset and review feedback follow in the next step.
