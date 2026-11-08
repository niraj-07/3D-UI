# VR setup

The demo uses Quest 3 through USB Link or Air Link on Windows. Set Meta's PC software as the active OpenXR runtime before starting VR. Android is not configured.

Packages are pinned in `Packages/manifest.json`: XR Interaction Toolkit 3.7.0, OpenXR 1.19.0, and XR Hands 1.10.0. The project enables the Oculus Touch profile, Hand Tracking Subsystem, and Meta Hand Tracking Aim. No Meta SDK is required.

## Controls

| Action | Controllers | Hands |
|---|---|---|
| Show or hide menus | Double-press **Y** on the left controller | Double-pinch **left middle finger + thumb** |
| Select a part or press a button | Point and press the trigger | Point and pinch **index finger + thumb** |

Release between the two menu presses/pinches and complete them within half a second. Menus start hidden. Showing them places Delegate to the left and review controls farther right, leaving the assembly visible. The current review offset is 1.30 m right; this final placement has not been separately confirmed in the headset.

The ray turns green over a usable target. Selection highlights the part cyan. A small aiming tolerance helps with the fingertips, while solid objects block selection. CAD selection locks during an active task; rays still work on menu buttons. Acceptance allows selecting and delegating the same part again. Rejection returns editing to desktop.

Press **Esc** in the focused PC Game view to leave the headset view. This keeps an existing VR network connection; **Preview in headset** resumes the view. Offline preview supports inspection and selection, but delegation needs a connected session.

## Rig

`Assets/TaskBubble/VR/PcVrRig.prefab` contains the XR Origin, headset camera, controller and hand-aim poses, selection rays, hand meshes, and Delegate controls. MainScene supplies the assembly and network references.

`PcVrRig.StartVr()` explicitly initializes XR after startup. Automatic XR startup is off so the desktop host does not claim the headset. `StopVr()` stops the loader owned by the rig. Tracking uses floor mode with a 1.6 m fallback for device mode. A running loader does not by itself prove that input is tracked.

Tracked hand meshes use Unity XR Hands skeleton drivers and hide on tracking loss. They have no colliders. The source license and notice are retained under `VR/Hands`. Valid hand aim takes priority over a controller on the same side. After tracking recovery or switching input, release a held selection gesture before pressing again.

## Hardware status

The developer has checked the integrated workflow and hands in the headset through Air Link with the Unity Editor. Hand tracking depends on the PC runtime exposing it. Hand input in a standalone Windows VR executable has not been verified; use controllers if hands are unavailable. This is a PC VR demo, not a Quest APK.

Keep the physical area clear and use the headset's boundary and recenter controls. TaskBubble does not supply locomotion or boundary protection.

The complete startup sequence is in the [README](../README.md).
