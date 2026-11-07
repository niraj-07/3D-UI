# VR setup

Task 6, step 1 prepares PC VR. It does not yet connect selection, Delegate, or review UI.

Packages are pinned through Unity Package Manager: XR Interaction Toolkit 3.7.0, OpenXR 1.19.0, and XR Hands 1.10.0. XR Plug-in Management 4.7.0 is a resolved dependency. No Meta SDK is required for this setup.

Windows uses OpenXR with single-pass instanced rendering, the Oculus Touch controller profile, Hand Tracking Subsystem, and Meta Hand Tracking Aim. Automatic XR startup is off so the desktop host does not claim the headset. Android is not configured.

`Assets/TaskBubble/VR/PcVrRig.prefab` contains an XR Origin, a head camera, left/right controller transforms, and left/right hand-aim transforms. All five poses use Input System tracking bindings and require valid tracking state. The camera excludes layer 30, reserved for the desktop close-up. Tracking uses floor mode with a 1.6 m device-mode fallback height.

`PcVrRig.StartVr()` initializes and starts XR explicitly. Call it after Unity's initial Start phase, when VR mode is selected. It activates the tracked objects only after a loader initializes. `StopVr()` deinitializes the loader owned by this rig, and disabling the rig also stops it. Start failure leaves a retry message in `Status`. `IsRunning` means the loader was started; it does not prove that hands or controllers are currently tracked.

During scene integration, assign the rig to the VR participant, disable the desktop camera/audio listener while the VR camera is active, call `StartVr()` after choosing VR, and call `StopVr()` on disconnect. Connection-scene controls are not wired to it yet. The prefab's tracked objects intentionally start inactive. Controller models, visible hands, rays, input switching and trigger/pinch selection follow in the next step.

## Link and hands

Connect Quest 3 to the PC through Meta Horizon/Quest Link with either USB or Air Link. Set Meta's PC app as the active OpenXR runtime. Link transport is configured outside TaskBubble, not in the scripts.

Enable hand tracking on the headset. Meta documents Link hand tracking as a Windows Unity Editor development feature; this step does not promise hand input in the final Windows executable. Check the current Link developer settings for hand-tracking support and restart the Editor if the PC software asks you to after changing those settings. If the runtime does not expose hand tracking, use controllers. If standalone hand input becomes a requirement, plan an Android Quest build separately.

- [Meta: Hand Tracking Overview](https://developers.meta.com/vr/documentation/unity/unity-handtracking-overview/)
- [Meta: Use Link for App Development](https://developers.meta.com/vr/documentation/unity/unity-link/)
- [Unity: XR Interaction Toolkit setup](https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@3.7/manual/general-setup.html)

## Verification so far

Saved assets have one Windows OpenXR loader, automatic startup disabled, all three requested input features enabled, and five tracked pose drivers with position, rotation and tracking-state bindings. OpenXR project validation reports no issues. Play Mode checks confirmed an idle rig does not start XR, repeated Stop is safe, and a disabled rig refuses Start. Compilation and the Console have no errors.

Physical headset/controller/hand tracking, automatic input switching, pinch interaction, and final Windows build behavior have not been verified. The setup builder is `AgentScripts/CreatePcVrRig.cs`, entry `CreatePcVrRig.Build`; it refuses to overwrite an existing rig prefab.
