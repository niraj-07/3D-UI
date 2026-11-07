# Part ghost

Prefab: `Assets/TaskBubble/Preview/PartGhost.prefab`. Its cyan transparent material is already assigned.

`PartGhost.Show(editablePart, proposedLocalRotation)` creates mesh-only visuals at the proposed rotation. It keeps the real part's parent space, local position, scale, and child transforms, so it rotates around the same pivot. It returns false and hides the ghost for missing/invalid inputs or a part without static meshes.

`Hide()` hides the copy without changing the real part. Repeated updates reuse the same visual hierarchy. Disabling the component removes that hierarchy.

The copy has no colliders, selection components, or network scripts. It uses the built-in Ignore Raycast layer, casts no shadows, and never changes the real mesh or its materials. Static MeshFilter/MeshRenderer CAD parts are supported.

This first step only supplies the visual. The prefab starts without geometry until Show is called. Connecting it to shared task state, VR mode, and Editing/Review visibility is the next step. Nothing has been added to the main scene yet.
