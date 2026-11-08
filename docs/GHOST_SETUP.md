# Part ghost

Prefab: `Assets/TaskBubble/Preview/PartGhost.prefab`. Its cyan transparent material is already assigned.

The material uses `TaskBubble/Ghost`, a small URP unlit shader with alpha blending and no depth writes. A raster depth bias prevents striped z-fighting where real and proposed faces coincide. It does not move vertices or change the proposed transform. The shader includes Unity's stereo/instancing macros; headset rendering still needs hardware verification.

`PartGhost.Show(editablePart, proposedLocalRotation)` creates mesh-only visuals at the proposed rotation. It keeps the real part's parent space, local position, scale, and child transforms, so it rotates around the same pivot. It returns false and hides the ghost for missing/invalid inputs or a part without static meshes.

`Hide()` hides the copy without changing the real part. Repeated updates reuse the same visual hierarchy. Disabling the component removes that hierarchy.

The copy has no colliders, selection components, or network scripts. It uses the built-in Ignore Raycast layer, casts no shadows, and never changes the real mesh or its materials. Static MeshFilter/MeshRenderer CAD parts are supported.

During scene integration, assign the scene's `DelegationSession`, `TaskBubbleConnection`, and CAD **Assembly Root** on the PartGhost component. The ghost resolves the current snapshot's part ID and switches geometry when another part is delegated. With no Assembly Root, **Editable Part** retains the earlier fixed-part setup. The material is already wired. Keep the component on a separate scene object, outside the CAD assembly's editable geometry.

The component subscribes to the shared session and connection. It shows the authoritative proposed local rotation in cyan for the connected VR role during Editing and Review. Acceptance briefly reuses that geometry as a green confirmation at the committed pose, then hides it. Idle, Requested, Rejected, Reset, disconnect, missing references, and mismatched part IDs hide it. Re-enabling the component reads the current snapshot, so an existing Review proposal appears immediately.

The host still owns validation and committed transforms. The ghost sends no network requests and never moves the real part. Its IDs and CAD hierarchy must match the session's part setup.

The binding passed 22 checks using a test mesh and local host/client peers inside one Editor. The rendered proposal, VR-only visibility, live updates, Review, decisions, Reset, mismatch handling, disable/re-enable, and disconnect were checked with zero Console errors or warnings. Main-scene wiring, final CAD geometry, separate builds, and headset rendering remain integration work. Nothing has been added to the Connection scene.

Task 11 now wires the ghost in MainScene and verifies switching between the two CAD fingertips with local Netcode peers. Separate-process builds and physical headset rendering still need verification; see `INTEGRATION_SETUP.md`.

## Acceptance feedback

An accepted edit gets a green highlight that fades over 1.6 seconds, plus a dark badge reading **Change accepted** above the corrected part. The badge faces the assigned headset camera and works while the task menus are hidden. Only an authoritative Accepted snapshot triggers it, once per task ID. Both visuals use Ignore Raycast and have no colliders. Input stays available throughout.

The highlight uses a property block on the reused ghost geometry. It does not change the real part's material, its selection highlight, or the shared cyan ghost material. The next proposal restores the normal cyan preview. Reset, a new task, disconnect, and disabling the component clear feedback. Timers use unscaled time.

`CreatePartGhost.AddApprovalFeedback` adds the badge to the existing prefab and assigns the headset camera in a saved MainScene. No audio, particles, camera motion, extra lights, or runtime manager were added. The UI feedback text on both peers also reads Change accepted.

Nineteen checks passed with two local Netcode peers, including authoritative acceptance, matching committed pose, geometry reuse, green tint/fade, duplicate-event suppression, expiry, original-material preservation, next-proposal tint, badge visibility, and Reset cleanup. A capture on the actual gripper confirmed readability. Scoped visual review: feedback visibility 4/5; its green/dark palette matches the existing interface. The pass used the Unity craft game-feel reference and Unity Pipeline. Headset comfort and appearance still need physical verification.
