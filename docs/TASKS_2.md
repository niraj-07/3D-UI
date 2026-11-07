# TaskBubble — Task List

One CAD assembly, one editable part, and rotation around one axis. Assign tasks later. The tags show which part of the project each task belongs to.

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
