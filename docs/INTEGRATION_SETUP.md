# Integrated workspace

The application starts in `Assets/TaskBubble/Integration/MainScene.unity`. See the [README](../README.md) for requirements, building, controls, and the demonstration sequence.

## Scene contents

- A manufacturing inspection room and 0.85 m workbench.
- `GripperAssembly`, enlarged uniformly by 4 and placed on the inspection mat.
- One shared `DelegationSession` with the connection and assembly references assigned.
- Desktop editing/Submit controls, the PC VR rig, Delegate and review menus, and ghost preview.
- One EventSystem for desktop input, a desktop camera, and two scene lights.

The scene inherits two fingertip configurations from the CAD prefab: A starts at +70 degrees around local Z; B starts at +25 degrees around local X. The connector and body are fixed. Only one task is active at a time.

## View switching

`WorkspaceView` switches cameras, audio listeners, and panels according to the local role. The desktop host stays outside XR and displays editing controls. Joining as VR queues explicit XR startup through `PcVrRig`, hides desktop overlays, and starts with task menus hidden.

**Preview in headset** also works offline for local inspection and selection. It does not create a network participant. **Esc** returns to desktop without disconnecting an existing VR client; Preview resumes its headset view. Disconnecting VR returns to desktop.

The desktop host is capped at 30 FPS to reduce competition with VR rendering and Air Link encoding. Decorative geometry has no colliders and uses Ignore Raycast. The room has two lights, with shadows on the key light only; static details are combined by material.

## Development tools

`AgentScripts` contains one-time Unity authoring scripts for the scene and prefabs. They are outside Assets and are not runtime dependencies. Open the saved MainScene for the submitted app; rebuilding the scene is not required to run it.

The developer has checked the integrated workflow and hands through Air Link in the Unity Editor. Standalone Windows VR hand input is not verified. The [shared contract](SHARED_CONTRACT.md) records the current late-join limitation.
