# VR setup

Task 6 has the PC VR rig, local part selection, and Delegate controls connected to the shared delegation API. Main-scene and headset verification remain.

Packages are pinned through Unity Package Manager: XR Interaction Toolkit 3.7.0, OpenXR 1.19.0, and XR Hands 1.10.0. XR Plug-in Management 4.7.0 is a resolved dependency. No Meta SDK is required for this setup.

Windows uses OpenXR with single-pass instanced rendering, the Oculus Touch controller profile, Hand Tracking Subsystem, and Meta Hand Tracking Aim. Automatic XR startup is off so the desktop host does not claim the headset. Android is not configured.

`Assets/TaskBubble/VR/PcVrRig.prefab` contains an XR Origin, a head camera, left/right controller transforms, and left/right hand-aim transforms. All five poses use Input System tracking bindings and require valid tracking state. The camera excludes layer 30, reserved for the desktop close-up. Tracking uses floor mode with a 1.6 m device-mode fallback height.

`PcVrRig.StartVr()` initializes and starts XR explicitly. Call it after Unity's initial Start phase, when VR mode is selected. It activates the tracked objects only after a loader initializes. `StopVr()` deinitializes the loader owned by this rig, and disabling the rig also stops it. Start failure leaves a retry message in `Status`. `IsRunning` means the loader was started; it does not prove that hands or controllers are currently tracked.

MainScene assigns the rig to the VR participant, disables the desktop camera/audio listener while the VR camera is active, and stops VR on disconnect. The prefab's tracked objects intentionally start inactive. The rig shows pointing rays and tracked hand meshes. There are no controller models.

## Part selection

`VrPartSelection` lives on the rig's `Tracked Objects` object. Assign the scene's CAD assembly to `Assembly Root` in the Inspector or call `SetAssemblyRoot(root)`. The reusable prefab leaves that scene reference empty.

Point at either fingertip and press the controller trigger or pinch the index finger and thumb. The ray turns green over a selectable part or usable button. Selection turns the fingertip cyan without moving it or editing its shared material. A 12 mm aiming tolerance helps with small fingertips; solid objects still block selection. Buttons also use their visible rectangles, so pointing does not depend solely on thin physics hitboxes. Only configured, enabled parts under the assigned assembly can be selected. Ghosts on Ignore Raycast and the desktop close-up on layer 30 are excluded.

There are four rays: one controller and one hand aim per side. A valid hand aim takes priority over the controller for the same side. Invalid hand aim, hand system/menu gestures and incomplete position/rotation tracking cannot select. After a modality switch, tracking recovery or re-enabling interaction, release a held trigger/pinch before pressing again. Holding a press while aiming at another part does not change selection. A fresh press on empty space or a noneditable object clears it.

The latest selected part is available as `SelectedPart`, with a `SelectionChanged` event for the Delegate control. `InteractionEnabled = false` clears selection and hides the rays. `PartSelectionEnabled = false` locks CAD selection but keeps rays usable for buttons. Losing all usable pointers or disabling tracking also clears selection and restores the original renderer property blocks. Selection alone sends no network command.

`CreatePcVrRig.AddSelection` adds this setup to the earlier rig; fresh `CreatePcVrRig.Build` runs include it. Both refuse to overwrite an existing selection setup. `SelectionRay.mat` is shared by the four rays.

## Delegate

The rig contains a nested `VrDelegationControls.prefab`, a world-space panel with a selected-part name, feedback and Delegate button. The button has a box collider so the same trigger/pinch ray can press it; it remains a normal uGUI button for mouse input in a scene with an EventSystem. A disabled button consumes the ray press without changing the selected CAD part.

Assign the scene's `DelegationSession` and `TaskBubbleConnection` to `VrDelegationControls` during integration. Its selection reference is already assigned within the rig. Assign the same CAD root to the session and selection component. The panel sits to the left of the rig origin and can be positioned beside the inspection table later.

Delegate is available for a connected VR client with a spawned session, an Idle or Accepted task and a configured selected part. Pressing it captures that part's ID and calls `RequestDelegation(id)`. The button locks immediately until the host responds. Pending requests and active tasks lock further CAD selection; UI rays stay available. Acceptance unlocks selection so the same part can be delegated again from its accepted rotation. Rejection lets desktop adjust and resubmit. Reset remains available to restore the original demo pose. Disconnect or the desktop role disables interaction. The host remains responsible for authoritative validation.

The panel is built by `AgentScripts/CreateVrDelegationControls.cs`, entry `CreateVrDelegationControls.Build`, which also nests it in the existing rig. It refuses to overwrite existing controls. MainScene connects both menu roots to the pointer, including their button rectangles and colliders.

## Hands and menus

The rig uses Unity XR Hands' left and right hand meshes, with all 26 joints mapped by `XRHandSkeletonDriver`. `XRHandMeshController` hides a hand when tracking is lost. The meshes have no colliders and cannot block the selection rays. `AgentScripts/AddTrackedHandMeshes.cs` adds this setup; model attribution and license are in `Assets/TaskBubble/VR/Hands`.

In a connected VR session, menus start hidden. On the **left hand**, pinch your middle finger and thumb twice within half a second, releasing between pinches. The menus appear beside your view, with Delegate 0.85 m to the left and review 1.05 m to the right, keeping the center clear for the machine. Repeat to hide them. Index-finger pinches still select parts and press buttons. With controllers, double-press the left secondary button (Y) instead.

Hiding menus disables their canvases and hitboxes, while keeping task subscriptions and CAD selection alive. Showing them again places them in front of your current view. Disconnecting or leaving VR hides them. Offline Preview in headset shows the model and hands; task menus require a VR network connection.

Verification: seven pointing checks, fifteen hand-rig checks, and fourteen gesture/menu checks passed in the Editor. Hand meshes were rendered with synthetic poses. Physical tracking alignment, pointing feel, and gesture comfort still need a headset check.

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

The setup builder is `AgentScripts/CreatePcVrRig.cs`, entry `CreatePcVrRig.Build`; it refuses to overwrite an existing rig prefab. Task 11 now assigns the scene references and role-based startup in MainScene; see `INTEGRATION_SETUP.md`. The reusable rig itself does not automatically initialize XR, and the old Connection scene remains separate. The integrated headset workflow still needs physical verification.
