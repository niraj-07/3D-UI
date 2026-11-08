# Ghost preview

MainScene contains `Assets/TaskBubble/Preview/PartGhost.prefab`, with its session, connection, assembly root, and headset camera references assigned.

During **Editing** and **Review**, the connected VR participant sees the authoritative proposal as a cyan ghost. The real component stays in place. The ghost resolves the task's part ID and uses that part's parent space, pivot, local position, scale, and child transforms.

`PartGhost.Show(editablePart, proposedLocalRotation)` creates mesh-only visuals and reuses them for later updates. `Hide()` hides the copy. Disabling the component removes the temporary geometry. Static `MeshFilter`/`MeshRenderer` parts are supported.

The ghost has no colliders or network scripts, uses Ignore Raycast, and casts no shadows. It does not change the source meshes or materials. `TaskBubble/Ghost` is a transparent URP unlit shader with stereo/instancing support and a depth bias to reduce z-fighting where proposed and real faces coincide.

## Acceptance feedback

An authoritative Accepted update shows a green highlight fading over 1.6 seconds and a **Change accepted** badge above the corrected part. The badge faces the headset camera and remains visible when menus are hidden. Feedback triggers once per task ID and uses unscaled time.

The highlight uses a renderer property block on the ghost geometry. It preserves the real part's material and selection highlight; the next proposal restores cyan. Reset, a new task, disconnect, or disabling the component clears feedback. Rejected proposals hide the VR ghost until the next edit or resubmission.
