# Desktop editing panel

Prefab: `Assets/TaskBubble/Desktop/DesktopPanel.prefab`.

During scene integration, assign these fields on its `DesktopEditingPanel` component:

- **Session:** the scene's `DelegationSession`.
- **Connection:** the scene's `TaskBubbleConnection`.
- **Editable Part:** the one editable CAD part, with the same ID and settings on both peers.

The UI references are already wired inside the prefab. The scene needs one EventSystem with an InputSystemUIInputModule. The prefab has its own Canvas and GraphicRaycaster.

The desktop host starts a requested task with a zero-degree proposal once. The slider uses the part's local axis and rotation limits. Its changes call `UpdateProposal`; it never changes the real part directly. Status and angle come from the shared snapshot, and rejected operations show their reason.

Controls stay disabled without a connected desktop session or matching part, and during Review, Accepted, Rejected, and Idle. Re-enabling the panel does not duplicate its event listeners.

The component is not yet placed in the connection scene. The part close-up is the next step. Submit and VR decision buttons belong to task 9.
