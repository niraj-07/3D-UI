# Integration checkpoint

Task 11, step 1 creates `Assets/TaskBubble/Integration/MainScene.unity`. The scene has a simple floor, back wall, inspection table, and the gripper enlarged uniformly by 4. The assembly sits on the 0.85 m table. Its two editable fingertips keep their IDs, pivots, limits and initial 70-degree misalignment.

This checkpoint supports headset viewing and local selection. Desktop editing, ghost previews, delegation and review are not connected in MainScene yet. The rig's Delegate panel is disabled in this scene until the network session is added in the next step. Existing feature prefabs remain separate.

## Try it in the headset

1. Connect Quest 3 to the PC through Quest/Meta Horizon Link, using USB or Air Link. Set Meta's PC app as the active OpenXR runtime.
2. Open `MainScene.unity` and enter Play Mode.
3. Click **Preview in headset** in the Game view. A host or new Windows build is not required for this local preview.
4. Look around the table and gripper. Try pointing with a controller or tracked hand; trigger or index-thumb pinch selects a fingertip and turns it cyan. Hand availability depends on the PC runtime exposing it. You will see pointing rays, not rendered hand meshes.
5. Press **Esc on the PC**, with the Game view focused, to stop VR and return to the desktop controls.

If XR initialization fails, the desktop camera and controls stay available and the preview status shows a retry message. Connect Link and retry. A started loader alone does not prove that the headset or hands are tracking; the physical check is still required.

The VR origin starts at floor level, facing the table along +Z. Keep physical space clear; TaskBubble does not provide room-boundary or locomotion features. Use the headset's normal boundary and recenter controls if the table is not in front of you.

## Connection behavior

The desktop Host/VR Join controls are also present. Joining as VR automatically starts the headset view; a desktop host stays in the desktop view. Disconnecting a network VR client returns it to desktop. For this path, both instances need matching builds containing MainScene. An old Connection-only executable is not a valid integrated-scene test because Netcode scene synchronization can load its old scene on the client.

`WorkspaceView` owns camera/audio-listener switching and preview controls. XR remains explicitly initialized through `PcVrRig`. Desktop overlays hide in headset mode. Preview selection uses the scene's CAD root but sends no delegation request yet.

MainScene is first in the shared build list; the other entries are retained. The existing Windows build profile must use this shared scene list when the build step is reached. The builder is `AgentScripts/CreateWorkspaceScene.cs`, entry `CreateWorkspaceScene.Build`; it refuses to overwrite an existing MainScene.

## Verification

The desktop scene was inspected in Play Mode. Fifteen checks passed using a temporary simulated XR loader: model placement, desktop staying outside XR, VR-role startup, camera/audio switching, overlay visibility, selection activation, duplicate connection notifications, disconnect cleanup, offline preview, explicit return and failed-start fallback. This does not verify the actual headset, Air Link, controllers, hands or final builds.

Next: add the shared session, bind desktop controls and ghost to the selected fingertip, and connect Delegate and review/reset controls. Then verify the full two-instance workflow before making final builds.
