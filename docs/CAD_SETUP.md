# CAD assembly

The model is the Robotiq 2F-85 Open gripper with a W-010-B blank coupling, downloaded from [Robotiq Support](https://robotiq.com/support). The converted OBJ/MTL exports are under `Assets/CAD model/Converted`. Manufacturer source assets remain subject to their original terms.

`AgentScripts/PrepareCadModels.py` splits the gripper's eleven OBJ object declarations into separate files under `Assets/TaskBubble/CAD/Models`, preserving coordinates, normals, UVs, winding, and materials. `ImportCadModels.cs` imports them at scale 0.001 to convert source millimetres to Unity metres. Do not apply that conversion twice.

## Prefab

`Assets/TaskBubble/CAD/GripperAssembly.prefab` contains eleven gripper meshes under `GripperBody` and the fixed coupling under `Connector`. Its root is at the coupling's bottom, with the fingers pointing up. MainScene enlarges the whole assembly uniformly by 4.

| Editable part | Stable ID | Axis | Offset limits | Initial pose | First correction |
|---|---|---|---|---|---|
| Fingertip A | `gripper.fingertip-a` | Local Z | -90 to +90 degrees | +70 degrees Z | -70 degrees |
| Fingertip B | `gripper.fingertip-b` | Local X | -45 to +45 degrees | +25 degrees X | -25 degrees |

Each fingertip has a pivot at the centre of its lower attachment edge and a nested `FingertipMesh`. Zero local rotation aligns the mesh. These are inspection-demo poses, not a simulation of the manufacturer's gripper mechanism. A/B follows the exported names.

Slider/input values are offsets from the rotation at delegation time. After acceptance, delegating again starts at angle zero relative to that accepted pose. Reset restores only the current task's part to its original demo pose.

Every mesh has a simple box collider for selection. A fingertip hit resolves through `GetComponentInParent<EditablePart>()`. The colliders are not detailed collision geometry; there are no rigidbodies or simulated joints.

The models retain their original geometry: 258,566 gripper triangles and 16,898 coupling triangles. Static URP Lit materials provide the part colours.

## Authoring

`CreateGripperAssembly.Build` creates the prefab through Unity APIs. `SetDemoPose` restores A's +70 Z and B's +25 X starting poses and B's axis/limits. These are authoring tools; no regeneration is needed to run the saved app.

To inspect a correction, open the prefab and temporarily change A's local Z from 70 to 0 or B's local X from 25 to 0. Undo afterward to retain the demonstration poses. Keep both clients' IDs, hierarchy, pivots, and constraints identical.
