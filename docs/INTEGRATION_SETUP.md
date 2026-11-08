# Integrated workspace

Task 11, step 1 creates `Assets/TaskBubble/Integration/MainScene.unity`. The scene has a simple floor, back wall, inspection table, and the gripper enlarged uniformly by 4. The assembly sits on the 0.85 m table. Its two editable fingertips keep their IDs, pivots, limits and initial 70-degree misalignment.

The scene now includes its shared `DelegationSession`, desktop editing/Submit UI, VR Delegate and Accept/Reject/Reset controls, and ghost preview. The desktop panel and ghost resolve whichever fingertip ID is in the shared snapshot. Only one delegation can be active.

## Try it in the headset

1. Connect Quest 3 to the PC through Quest/Meta Horizon Link, using USB or Air Link. Set Meta's PC app as the active OpenXR runtime.
2. Open `MainScene.unity` and enter Play Mode.
3. Click **Preview in headset** in the Game view. A host or new Windows build is not required for this local preview.
4. Look around the table and gripper. Try pointing with a controller or tracked hand; trigger or index-thumb pinch selects a fingertip and turns it cyan. Hand availability depends on the PC runtime exposing it. You will see pointing rays, not rendered hand meshes.
5. Press **Esc on the PC**, with the Game view focused, to stop VR and return to the desktop controls.

If XR initialization fails, the desktop camera and controls stay available and the preview status shows a retry message. Connect Link and retry. A started loader alone does not prove that the headset or hands are tracking; the physical check is still required.

The VR origin starts at floor level, facing the table along +Z. Keep physical space clear; TaskBubble does not provide room-boundary or locomotion features. Use the headset's normal boundary and recenter controls if the table is not in front of you.

Offline preview still shows local selection only. Delegate and review controls appear when connected as VR; preview alone does not create a second participant.

## Try the complete workflow

This requires a new Windows build of the current MainScene. The older Connection-only build cannot exercise the integrated workflow. Build creation and separate-process verification are the next step.

1. Run the new Windows build, leave the mode as Desktop, and click Host. The editing panel appears, with controls locked until a part is delegated. Disconnect is above the editor.
2. With Air Link running, open MainScene in the Unity Editor, enter Play Mode, select VR Client and join `127.0.0.1`. Joining starts the headset view. On different PCs, enter the desktop host's LAN IPv4 address instead.
3. In VR, select the tilted Fingertip A and point at Delegate, then trigger/pinch. The desktop starts a zero-offset proposal automatically; the real fingertip stays at its starting pose.
4. On desktop, adjust the slider toward -70 degrees to straighten A. The desktop close-up and VR ghost follow the proposal, while the real part remains still.
5. Click Submit for review. The slider locks. In VR, choose Reject to discard the ghost, or Accept to apply the proposed pose on both peers.
6. After rejection, adjust and resubmit from desktop. After acceptance, select and Delegate the same fingertip again to revise its accepted pose. Reset is optional: it restores the original misalignment and returns to Idle. Fingertip B can also be delegated; its label, close-up, limits and ghost are resolved independently.

CAD selection locks while a request/task is active. Pointer rays remain available for the world-space buttons, whose box colliders match their rectangles. A fresh trigger/pinch invokes a button without selecting the CAD behind it. The button controls retain their own role, state and pending-operation guards.

## Connection behavior

The desktop Host/VR Join controls are also present. Joining as VR automatically starts the headset view; a desktop host stays in the desktop view. Disconnecting a network VR client returns it to desktop. For this path, both instances need matching builds containing MainScene. An old Connection-only executable is not a valid integrated-scene test because Netcode scene synchronization can load its old scene on the client.

`WorkspaceView` owns camera/audio-listener and panel switching. XR remains explicitly initialized through `PcVrRig`. The connected desktop sees editing controls; the connected VR participant sees Delegate and review controls. Desktop overlays hide in headset mode. Offline preview enables local selection without the session UI. Esc returns to desktop without disconnecting a VR client; Preview in headset can resume its view.

MainScene is first in the shared build list; the other entries are retained. The existing Windows build profile must use this shared scene list when the build step is reached. `AgentScripts/CreateWorkspaceScene.cs`, entry `CreateWorkspaceScene.Build`, creates the original scene; `AgentScripts/WireWorkspaceWorkflow.cs`, entry `WireWorkspaceWorkflow.Build`, adds the shared workflow to it. Both refuse to overwrite an existing setup.

## Verification

The first checkpoint passed fifteen simulated XR lifecycle checks, and the user subsequently confirmed the headset scene and selection worked through Air Link.

The shared workflow passed twenty-two checks with two real local Netcode peers on isolated UDP port 17781, using the actual prefabs, button events and slider changes. Checks covered A and B delegation, automatic Editing, per-part limits and close-ups, real-part immobility while proposing, ghost geometry/pose switching, submit locking, reject, accept on both peers, reset to the original poses, and disconnect cleanup. Eleven additional checks verified MainScene's saved references, actual scene NetworkObject spawn, desktop role/UI visibility and XR staying stopped on the host. Desktop and VR panel layouts were rendered, and review ray hitboxes matched their resolved layout.

These are same-Editor peer checks, not a complete Quest-to-Windows test. Ghost rendering, the integrated review controls in the physical headset, standalone input support and separate-process scene synchronization still need verification with the new build.
