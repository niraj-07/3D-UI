# TaskBubble

Collaborative CAD adjustment between VR and desktop. The VR user selects a part and delegates an edit. The desktop user proposes a rotation with a slider or exact angle input. VR sees a live ghost preview and accepts or rejects it. Only acceptance changes the real part.

## Run the demo

Requires a Windows PC, Unity **6000.6.8f1**, and a Quest 3 connected through **Quest Link or Air Link**. Set Meta's PC app as the active OpenXR runtime. Enable Unknown Sources in that app if it blocks the application.

1. Clone this repo and open the **TaskBubble** folder in Unity Hub. Let Unity restore the packages.
2. Open `Assets/TaskBubble/Integration/MainScene.unity`.
3. In **File > Build Profiles**, use the **Windows** profile and shared scene list, with MainScene first. Build a Windows x86_64 player outside `Assets`, for example in `Builds/Submission/`.
4. Run the build, choose **Desktop (Host)**, and click **Host**.
5. In the Editor, enter Play Mode, choose **VR (Client)**, and join **127.0.0.1**. The headset view starts automatically. Both instances run on the same PC, using UDP port **7777**.

Use matching source/build versions. Keep the executable, its `_Data` folder, and the other build files together. Builds are not stored in Git. Unity CLI and the agent plugin are not required to open or run the project.

## Controls

| Action | Controllers | Tracked hands |
|---|---|---|
| Show/hide menus | Double-press **Y** on the left controller | Double-pinch **left middle finger + thumb** |
| Select a part or press a button | Point and press trigger | Point and pinch **index finger + thumb** |

Release between presses or pinches. Menus start hidden. **Esc** in the focused Editor Game view returns to the desktop view. Hand tracking has been tried through Air Link in the Editor; availability depends on the PC runtime. Use controllers if hands are unavailable. Standalone Windows hand input has not been verified.

## Try a correction

Select a fingertip in VR and press **Delegate**. On desktop, adjust its angle and press **Submit**. Inspect the cyan ghost in VR, then **Accept** or **Reject**. Rejection allows desktop revision and resubmission. After acceptance, either fingertip can be delegated again.

| Part | Allowed local axis | Initial correction |
|---|---|---|
| Fingertip A | Z | **-70 degrees** |
| Fingertip B | X | **-25 degrees** |

Angles are offsets from the pose when delegated. **Reset** restores the current task's part to its original misaligned pose and clears the task.

## Code and development

`Assets/TaskBubble/` contains the runtime code and prefabs. [DelegationSession.cs](TaskBubble/Assets/TaskBubble/Networking/DelegationSession.cs) validates roles, task IDs, states, and rotations on the host. Desktop and VR controls use the [shared interface](docs/SHARED_CONTRACT.md); the ghost is separate from the committed object.

See the [task list](docs/TASKS_2.md) and [scene notes](docs/INTEGRATION_SETUP.md). Task branches retain small implementation commits. Main has one squash commit per completed task.

## Scope and limits

Two editable fingertips, one active task, and one configured rotation axis per part. This is an inspection demo, not a mechanical simulation or full CAD editor. There is no Android APK or runtime CAD import.

Fresh joins currently recover only the latest committed part pose. After editing both parts and losing the connection, restart both instances for a clean demo.

## Assets

Gripper and coupling CAD: **Robotiq 2F-85 / W-010-B**, from [Robotiq Support](https://robotiq.com/support), converted with FreeCAD. Converted meshes are included; raw working documents are excluded. Hand meshes: Unity XR Hands sample, with [notice](TaskBubble/Assets/TaskBubble/VR/Hands/NOTICE.txt) and [license](TaskBubble/Assets/TaskBubble/VR/Hands/LICENSE.md). Asset ownership remains with the respective creators.
