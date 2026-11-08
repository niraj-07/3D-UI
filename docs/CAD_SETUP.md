# CAD preparation

Source: the supplied Robotiq 2F-85 Open gripper and W-010-B blank coupling exports. The manufacturer download page is [Robotiq Support](https://robotiq.com/support). Original OBJ/MTL exports remain in `Assets/CAD model/Converted`; the STEP and FreeCAD working documents are left untouched.

The original gripper OBJ declares eleven objects, but Unity imports it as one mesh. `AgentScripts/PrepareCadModels.py` splits those declarations into separate files under `Assets/TaskBubble/CAD/Models`, remapping face indices while preserving coordinate, UV, normal, winding, and material data. It also prepares the single coupling model. It refuses to overwrite modified generated files.

The models are Base, Finger1–4 A/B, Fingertip A/B, and Coupling. A/B identifies the paired exported instances; it does not yet assert left/right placement.

`AgentScripts/ImportCadModels.cs`, entry `ImportCadModels.Import`, sets each generated model's Scale Factor to 0.001. The source coordinates are millimetres, and the imported components are metres. The gripper's combined dimensions are approximately 0.1524 × 0.1527 × 0.0750 m. Do not apply a second millimetre conversion when assembling the prefab.

Import validation checks twelve separate static meshes, matching scaled bounds for both source models, and unchanged triangle counts: 258,566 for the gripper and 16,898 for the coupling. The three gripper colors and black coupling material import as URP Lit materials. No geometry has been removed or simplified in this step.

## Assembly prefab

`Assets/TaskBubble/CAD/GripperAssembly.prefab` contains the eleven gripper meshes under `GripperBody` and the fixed coupling under `Connector`. The assembly root sits at the bottom of the coupling, with the fingers pointing up. Scale is still 1; use a uniform parent scale to enlarge the whole assembly for the inspection scene.

The coupling is turned to match the gripper's mounting axis and sits flush under the base. It is fixed because its circular shape makes rotation hard to see. The two rectangular fingertips are editable instead:

| Object | Stable ID | Local axis | Limits |
|---|---|---|---|
| `GripperBody/Fingertip_A` | `gripper.fingertip-a` | Z | -90 to +90 degrees |
| `GripperBody/Fingertip_B` | `gripper.fingertip-b` | X | -45 to +45 degrees |

Each fingertip has a parent pivot at the centre of its lower attachment edge and the original model nested as `FingertipMesh`. The mesh retains its aligned placement at zero rotation. These are simple demo adjustments, not a simulation of the manufacturer's gripper mechanism. A/B follows the source model names. Only one part can be delegated at a time.

The saved demo pose starts `Fingertip_A` at +70 degrees around local Z and `Fingertip_B` at +25 degrees around local X. These demonstrate two different corrections: sideways alignment on A and a forward/back tilt on B. For their first delegations, proposals of -70 degrees for A and -25 degrees for B return each to its aligned orientation. The slider and exact input are offsets from the pose at delegation time, not absolute CAD angles. After acceptance, a later delegation starts at zero relative to that accepted pose. Reset restores only the current task's part to its original demo rotation: +70 Z for A or +25 X for B.

Each gripper mesh has a simple box collider. The fingertip colliders are on their mesh children; resolve the editable part with `GetComponentInParent<EditablePart>()`. The connector has one box collider matching its mesh bounds. These are selection hitboxes, not detailed collision geometry. No rigidbodies or joint simulation are added.

`AgentScripts/CreateGripperAssembly.cs`, entry `CreateGripperAssembly.Build`, creates the prefab using Unity APIs. It refuses to overwrite an existing prefab. `CreateGripperAssembly.UpgradeFingertips` updates the earlier connector-only prefab. Verification checked twelve meshes and colliders, two unique registered editable IDs, preserved aligned geometry, and independent 30-degree fingertip rotations without moving other parts. Calculating a proposal leaves the actual part unchanged.

`CreateGripperAssembly.SetDemoPose` restores the saved +70 Z / +25 X starting poses on an existing prefab and sets B's axis and limits. Fresh builds include the same settings. Task 13's second step passed 22 checks with the actual CAD prefab and two local Netcode peers: stable IDs, geometry/collider counts, both axis configurations, valid limits, B selection from headset height, pure proposal calculation, network delivery, rejection, retry, acceptance on both peers, independent resets, and unchanged body placement. Before/after captures confirmed that B's 25-degree tilt is visible from an oblique inspection view. This does not replace a physical headset check.

To inspect it, double-click `GripperAssembly.prefab` in the Project window. Change A's local Z rotation from 70 to 0, or B's local X rotation from 25 to 0, to see each correction; undo afterwards to retain the demo poses. MainScene inherits both fingertip configurations from the prefab. Their IDs, pivots, mesh offsets, and colliders are unchanged. The enlarged table scene and network ghost are already integrated; the remaining Task 13 step checks the two-scenario desktop workflow and updates the demo/build instructions.
