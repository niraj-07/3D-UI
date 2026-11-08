# Desktop editing panel

Prefab: `Assets/TaskBubble/Desktop/DesktopPanel.prefab`.

During scene integration, assign these fields on its `DesktopEditingPanel` component:

- **Session:** the scene's `DelegationSession`.
- **Connection:** the scene's `TaskBubbleConnection`.
- **Assembly Root:** the CAD assembly containing the registered editable parts. The panel resolves the snapshot's part ID and switches its limits, label and close-up automatically. Both peers need matching IDs and settings.

With no Assembly Root assigned, **Editable Part** still supports the earlier single-part setup. MainScene uses Assembly Root for its two fingertips.

The UI references are already wired inside the prefab. The scene needs one EventSystem with an InputSystemUIInputModule. The prefab has its own Canvas and GraphicRaycaster.

The desktop host starts a requested task with a zero-degree proposal once. The slider uses the part's local axis and rotation limits. Its changes call `UpdateProposal`; it never changes the real part directly. Status and angle come from the shared snapshot, and rejected operations show their reason.

Controls stay disabled without a connected desktop session or matching part, and during Review, Accepted, and Idle. Rejected proposals can be adjusted and submitted again, including unchanged resubmissions. Re-enabling the panel does not duplicate its event listeners.

The left side shows a close-up through `DesktopPartPreview`, already connected inside the prefab. It copies static MeshFilter/MeshRenderer visuals and child transforms, reuses their meshes/materials, and excludes scripts and colliders. The preview follows the proposed local rotation during Editing/Review, shows the committed result in Accepted, and retains the proposed rotation for revision in Rejected. Idle and disconnect hide it. Disabling the panel destroys the temporary geometry, camera, light, and render texture.

The preview uses a square 640-pixel render texture without stretching, a dedicated camera, and currently unused layer **30**. Reserve that layer for desktop previews and exclude it from the main/VR cameras during scene integration. Its visual stage is kept away from the CAD assembly. No project layers or camera settings are changed automatically. Prepare the CAD part with a correct pivot and consistent scale in task 5; skinned meshes are outside this CAD MVP.

The prefab is not yet placed in the connection scene. Submit and VR decision buttons belong to task 9; scene wiring belongs to task 11.

MainScene now includes this panel with Submit controls and Assembly Root assigned. `WorkspaceView` shows it for the connected desktop role. See `INTEGRATION_SETUP.md` for the current run sequence.
