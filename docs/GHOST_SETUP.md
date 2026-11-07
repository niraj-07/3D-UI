# Part ghost

Prefab: `Assets/TaskBubble/Preview/PartGhost.prefab`. Its cyan transparent material is already assigned.

`PartGhost.Show(editablePart, proposedLocalRotation)` creates mesh-only visuals at the proposed rotation. It keeps the real part's parent space, local position, scale, and child transforms, so it rotates around the same pivot. It returns false and hides the ghost for missing/invalid inputs or a part without static meshes.

`Hide()` hides the copy without changing the real part. Repeated updates reuse the same visual hierarchy. Disabling the component removes that hierarchy.

The copy has no colliders, selection components, or network scripts. It uses the built-in Ignore Raycast layer, casts no shadows, and never changes the real mesh or its materials. Static MeshFilter/MeshRenderer CAD parts are supported.

During scene integration, assign the scene's `DelegationSession`, `TaskBubbleConnection`, and matching `EditablePart` on the PartGhost component. The material is already wired. Keep the component on a separate scene object, outside the CAD assembly's editable geometry.

The component subscribes to the shared session and connection. It automatically shows the authoritative proposed local rotation only for the connected VR role during Editing and Review. It stays hidden in Idle and Requested, and hides after Accepted, Rejected, Reset, or disconnect. Missing references and mismatched part IDs also hide it. Re-enabling the component reads the current snapshot, so an existing Review proposal appears immediately.

The host still owns validation and committed transforms. The ghost sends no network requests and never moves the real part. Its IDs and CAD hierarchy must match the session's part setup.

The binding passed 22 checks using a test mesh and local host/client peers inside one Editor. The rendered proposal, VR-only visibility, live updates, Review, decisions, Reset, mismatch handling, disable/re-enable, and disconnect were checked with zero Console errors or warnings. Main-scene wiring, final CAD geometry, separate builds, and headset rendering remain integration work. Nothing has been added to the Connection scene.
