# Desktop editing

MainScene contains `Assets/TaskBubble/Desktop/DesktopPanel.prefab`, wired to its `DelegationSession`, `TaskBubbleConnection`, and CAD assembly root. It resolves the task's part ID to display the correct name, axis, limits, and close-up.

When VR delegates a part, the desktop starts an angle-zero proposal. The slider and Exact Angle field send proposals through `UpdateProposal`; neither moves the real component. Angles are offsets from the pose captured at delegation time.

## Exact angle entry

Enter a signed decimal value such as `-12.5`, using a dot as the decimal separator. Press Enter or leave the field to apply it. Invalid, non-finite, or out-of-range values leave the proposal unchanged, restore the current value, and show the valid range.

The field and slider share the part's limits. They are editable during Requested, Editing, and Rejected, and lock during Review, Accepted, Idle, or disconnect. An unchanged input sends no extra update. Updates for the same task preserve unfinished typing; text from an earlier task is not submitted to a new one.

Submit freezes the proposal for VR review. After rejection, change the angle or submit it again. After acceptance, VR can delegate the same or another part. See [review controls](REVIEW_SETUP.md).

## Close-up

`DesktopPartPreview` copies static mesh visuals and child transforms without scripts or colliders. It follows the proposal during editing/review, retains a rejected proposal for revision, and shows the committed result after acceptance. Idle and disconnect hide it.

The close-up uses a 640-pixel render texture and a separate camera on layer **30**. MainScene's desktop and VR cameras exclude that layer. Meshes and materials are reused; disabling the preview removes its temporary geometry, camera, light, and render texture.

The desktop host is capped at 30 FPS to leave GPU time for VR and Air Link on the same PC. The earlier frame-rate setting is restored when leaving that role/view.
