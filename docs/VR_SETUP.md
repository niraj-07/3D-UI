# VR setup

Task 6 has the PC VR rig, local part selection, and Delegate controls connected to the shared delegation API. Main-scene and headset verification remain.

Packages are pinned through Unity Package Manager: XR Interaction Toolkit 3.7.0, OpenXR 1.19.0, and XR Hands 1.10.0. XR Plug-in Management 4.7.0 is a resolved dependency. No Meta SDK is required for this setup.

Windows uses OpenXR with single-pass instanced rendering, the Oculus Touch controller profile, Hand Tracking Subsystem, and Meta Hand Tracking Aim. Automatic XR startup is off so the desktop host does not claim the headset. Android is not configured.

`Assets/TaskBubble/VR/PcVrRig.prefab` contains an XR Origin, a head camera, left/right controller transforms, and left/right hand-aim transforms. All five poses use Input System tracking bindings and require valid tracking state. The camera excludes layer 30, reserved for the desktop close-up. Tracking uses floor mode with a 1.6 m device-mode fallback height.

`PcVrRig.StartVr()` initializes and starts XR explicitly. Call it after Unity's initial Start phase, when VR mode is selected. It activates the tracked objects only after a loader initializes. `StopVr()` deinitializes the loader owned by this rig, and disabling the rig also stops it. Start failure leaves a retry message in `Status`. `IsRunning` means the loader was started; it does not prove that hands or controllers are currently tracked.

During scene integration, assign the rig to the VR participant, disable the desktop camera/audio listener while the VR camera is active, call `StartVr()` after choosing VR, and call `StopVr()` on disconnect. Connection-scene controls are not wired to it yet. The prefab's tracked objects intentionally start inactive. The rig currently shows pointing rays, not controller models or rendered hand meshes.

## Part selection

`VrPartSelection` lives on the rig's `Tracked Objects` object. Assign the scene's CAD assembly to `Assembly Root` in the Inspector or call `SetAssemblyRoot(root)`. The reusable prefab leaves that scene reference empty.

Point at either fingertip and press the controller trigger or pinch the index finger and thumb. Selection turns the fingertip cyan without moving it or editing its shared material. The closest solid collider blocks the ray; colliders resolve their `EditablePart` on a parent. Only configured, enabled parts under the assigned assembly can be selected. Ghosts on Ignore Raycast and the desktop close-up on layer 30 are excluded.

There are four rays: one controller and one hand aim per side. A valid hand aim takes priority over the controller for the same side. Invalid hand aim, hand system/menu gestures and incomplete position/rotation tracking cannot select. After a modality switch, tracking recovery or re-enabling interaction, release a held trigger/pinch before pressing again. Holding a press while aiming at another part does not change selection. A fresh press on empty space or a noneditable object clears it.

The latest selected part is available as `SelectedPart`, with a `SelectionChanged` event for the Delegate control. `InteractionEnabled = false` clears selection and hides the rays. `PartSelectionEnabled = false` locks CAD selection but keeps rays usable for buttons. Losing all usable pointers or disabling tracking also clears selection and restores the original renderer property blocks. Selection alone sends no network command.

`CreatePcVrRig.AddSelection` adds this setup to the earlier rig; fresh `CreatePcVrRig.Build` runs include it. Both refuse to overwrite an existing selection setup. `SelectionRay.mat` is shared by the four rays.

## Delegate

The rig contains a nested `VrDelegationControls.prefab`, a world-space panel with a selected-part name, feedback and Delegate button. The button has a box collider so the same trigger/pinch ray can press it; it remains a normal uGUI button for mouse input in a scene with an EventSystem. A disabled button consumes the ray press without changing the selected CAD part.

Assign the scene's `DelegationSession` and `TaskBubbleConnection` to `VrDelegationControls` during integration. Its selection reference is already assigned within the rig. Assign the same CAD root to the session and selection component. The panel sits to the left of the rig origin and can be positioned beside the inspection table later.

Delegate is available only for a connected VR client with a spawned session, an Idle task and a configured selected part. Pressing it captures that part's ID and calls `RequestDelegation(id)`. The button locks immediately until the host responds. Pending requests and active tasks lock further CAD selection; UI rays stay available. Refused requests show the host's reason and allow a new selection. Reset returning the session to Idle unlocks selection. Disconnect or the desktop role disables interaction. The host remains responsible for authoritative validation.

The panel is built by `AgentScripts/CreateVrDelegationControls.cs`, entry `CreateVrDelegationControls.Build`, which also nests it in the existing rig. It refuses to overwrite existing controls. Existing review prefabs are still separate; to operate their buttons with these rays during integration, give each button a matching box collider on the Button object. UI hitboxes should be in front of other solid colliders. Hand/controller hover animations and rendered hand models are not part of this step.

## Link and hands

Connect Quest 3 to the PC through Meta Horizon/Quest Link with either USB or Air Link. Set Meta's PC app as the active OpenXR runtime. Link transport is configured outside TaskBubble, not in the scripts.

Enable hand tracking on the headset. Meta documents Link hand tracking as a Windows Unity Editor development feature; this step does not promise hand input in the final Windows executable. Check the current Link developer settings for hand-tracking support and restart the Editor if the PC software asks you to after changing those settings. If the runtime does not expose hand tracking, use controllers. If standalone hand input becomes a requirement, plan an Android Quest build separately.

- [Meta: Hand Tracking Overview](https://developers.meta.com/vr/documentation/unity/unity-handtracking-overview/)
- [Meta: Use Link for App Development](https://developers.meta.com/vr/documentation/unity/unity-link/)
- [Unity: XR Interaction Toolkit setup](https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@3.7/manual/general-setup.html)

## Verification so far

Saved assets have one Windows OpenXR loader, automatic startup disabled, all three requested input features enabled, and five tracked pose drivers with position, rotation and tracking-state bindings. OpenXR project validation reports no issues. Play Mode checks confirmed an idle rig does not start XR, repeated Stop is safe, and a disabled rig refuses Start. Compilation and the Console have no errors.

Step 2 passed eighteen Play Mode checks with simulated Input System devices: trigger and pinch selection, original highlight restoration, held-input rejection, solid occlusion, assembly scope, hand/controller priority, tracking loss/recovery, system gestures, interaction gating and disable cleanup. The cyan highlight was also rendered on the actual gripper. These checks do not establish physical headset/controller/hand behavior or final Windows build support.

Step 3 passed thirteen checks with two real local Netcode peers on isolated UDP port 17779 and a simulated hand pinch: the world-space Delegate button sent the correct fingertip ID, duplicate requests were blocked, task state locked CAD selection while keeping the UI ray, reset unlocked selection, a missing server-side part produced refusal feedback, disconnect disabled controls and the desktop role could not delegate. The panel layout was rendered separately. Physical interaction has not been checked.

The setup builder is `AgentScripts/CreatePcVrRig.cs`, entry `CreatePcVrRig.Build`; it refuses to overwrite an existing rig prefab. Hardware validation and main-scene references remain for integration. This step does not automatically start VR or change the connection scene.
