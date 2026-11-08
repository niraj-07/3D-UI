# TaskBubble — Task List

One CAD assembly, one active delegation at a time, and rotation around each part's configured axis. The tags show which part of the project each task belongs to.

1. **[Setup] Project setup** — Keep Unity and package versions consistent, add Git exclusions, and create the starting scene.
2. **[Shared] Edit contract** — Define stable part IDs, rotation limits, task states, shared methods, and state-change notifications. Agree on this and merge it before connecting the other parts.
3. **[Network] Host/client connection** — Add connection controls, an address field, and a way to choose VR or desktop mode. Keep connection code separate from delegation logic.
4. **[Network] Shared task state** — Sync the selected part, task status, original rotation, and proposed rotation. Allow one active task and validate actions on the host.
5. **[CAD] Assembly preparation** — Import the model with separate parts and fix the editable part's scale, pivot, collider, and ID. Keep it in a CAD prefab.
6. **[VR] Part selection** — Set up the XR rig and controller ray, highlight the selected part, and add Delegate. Call the shared methods from the VR prefab.
7. **[Desktop] Editing view** — Show the delegated part in a close-up with a rotation slider, angle value, and task status. Send proposed changes through the shared methods. Keep this in a desktop prefab.
8. **[Preview] Live ghost** — Read the shared proposal and display it as a translucent, noninteractive ghost in VR. Keep the real part in place. This task handles the ghost's appearance and movement; task 4 handles synchronization.
9. **[Review] Submit and review controls** — Add desktop Submit and VR Accept/Reject controls in separate review UI prefabs. Call the shared methods and show when actions are available; leave state transitions to the network code.
10. **[Network] Apply, reject, and reset** — Accept applies the proposal on both clients; Reject discards it. Reset restores the original misalignment and clears the task. Publish the result so both views can show it.
11. **[Integration] Main scene and builds** — Connect the CAD, VR, desktop, preview, and review prefabs in the main scene. Create VR and desktop builds that start the workflow without Editor changes.
12. **[Demo] Instructions and submission** — Write short setup/run instructions, record the 3–5 minute demo with code explanations, and submit the repository and video.
13. **[Desktop/CAD] Precision editing and a second correction** — Add exact angle entry and give the second fingertip a different rotation axis so the demo shows two kinds of correction. See the steps below.

## Task 13 — Precision editing and a second correction

Branch: `task/13-precision-editing-scenarios`

The demo will have two corrections: straighten fingertip A around local Z, then remove a smaller twist from fingertip B around local X. Both use the existing Delegate, ghost, Submit, Reject, Accept, and Reset workflow. These are inspection-demo poses, not a mechanical simulation of the gripper.

### Step 1 — Exact angle entry

- Add a degrees input beside the desktop slider in `DesktopPanel.prefab`.
- Typing an angle and pressing Enter or leaving the field sends a proposal through the existing session API.
- Keep the field and slider in sync without sending duplicate updates. Use the selected part's limits and accept decimal values, including negative angles.
- Invalid or out-of-range input leaves the proposal unchanged, explains the valid range, and restores the current value. Lock the field under the same conditions as the slider, including during VR review.
- Check typing, slider changes, rejection/retry, and switching between parts before committing.

Commit: `feat: add exact desktop angle input`

### Step 2 — A different correction on fingertip B

- Keep A's existing ID, local Z axis, +70-degree starting pose, and limits.
- Keep B's existing ID and pivot, change its allowed axis to local X, and start it at +25 degrees with limits of -45 to +45 degrees.
- Inspect the result from desktop and headset-height views. The twist must be obvious and the part easy to point at; adjust the demo angle if necessary before finalizing it.
- A first proposal of -25 degrees should align B without moving the body or A. Reset should restore B's initial twist.
- Update `CreateGripperAssembly` and `CAD_SETUP.md` to match the saved prefab. Keep the enlarged scene instance consistent.

Commit: `feat: add an X-axis fingertip correction scenario`

### Step 3 — Finish the two-part demonstration

- Confirm the desktop displays the correct axis, angle, limits, close-up, and input state when either part is delegated.
- Run A through reject, revise, and accept; then delegate B and correct it with exact angle entry. Re-delegating an accepted part must still start from its accepted pose.
- Confirm that proposals only move the ghost, acceptance updates both clients, and Reset restores each selected part's own demo pose.
- Update the short demo instructions and produce a fresh Windows build for the PC/Quest check. Record any hardware verification still outstanding.

Commit: `docs: describe the two-part precision editing demo` (include integration fixes only if verification finds a concrete issue).

No new model, package, network operation, scenario manager, or movement system is needed. Each part still permits rotation around one configured axis; unrestricted three-axis rotation and position editing are outside this task.

## Working without stepping on each other

- Claim a task before starting. Use a branch such as `task/6-vr-selection` and make small commits.
- Merge tasks 1 and 2 first. After that, networking, CAD, and desktop work can start separately. VR selection follows the CAD setup; preview and review connect once the shared state is ready.
- Tasks 4 and 10 edit the same delegation code. Finish and merge task 4 before starting task 10. They can go to the same person or be handed over after the merge.
- Keep files with their feature: `Shared/`, `Networking/`, `CAD/`, `VR/`, `Desktop/`, `Preview/`, and `Review/` under `Assets/TaskBubble/`. Only create folders when there are actual files to put in them.
- Each task owns its scripts and prefabs. For task 9, wire the review prefabs into the VR/desktop prefabs during task 11 rather than editing someone else's prefab at the same time.
- One person handles `MainScene.unity` during integration. Work in separate feature scenes until then.
- Coordinate changes to `Packages/` and `ProjectSettings/` through one person. If XR needs a package, agree on it before installing it.
- Keep the shared contract stable. Agree on API changes before editing `Shared/`; VR, desktop, and preview should use it without changing network internals.
- Commit every asset with its `.meta` file. Agree on file moves and renames before doing them, and keep their existing `.meta` files.
- Merge finished work regularly and update your branch before starting the next task. Avoid unrelated formatting or cleanup in a task commit.
