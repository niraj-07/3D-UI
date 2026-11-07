# Review controls

Desktop prefab: `Assets/TaskBubble/Review/DesktopReviewControls.prefab`.

During integration, place it directly under the `DesktopPanel` Canvas. Its anchors position Submit below the editing card. Assign the scene's `DelegationSession` and `TaskBubbleConnection` to its `DesktopReviewControls` component. The button reference is already wired.

Use the scene's existing EventSystem and InputSystemUIInputModule. The prefab uses the parent Canvas and GraphicRaycaster rather than adding another one.

Submit is enabled only for a connected desktop participant with a spawned session in Editing. Clicking it calls `SubmitProposal()`. The host validates the request, publishes Review, and the button disables. Idle, Requested, Review, Accepted, Rejected, and disconnected sessions cannot submit. The callback also checks these conditions if invoked directly.

This first step does not place the control in a scene or modify the desktop/VR prefabs. VR decisions, Reset, and review feedback follow in the next steps.
