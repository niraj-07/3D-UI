# CAD preparation

Source: the supplied Robotiq 2F-85 Open gripper and W-010-B blank coupling exports. The manufacturer download page is [Robotiq Support](https://robotiq.com/support). Original OBJ/MTL exports remain in `Assets/CAD model/Converted`; the STEP and FreeCAD working documents are left untouched.

The original gripper OBJ declares eleven objects, but Unity imports it as one mesh. `AgentScripts/PrepareCadModels.py` splits those declarations into separate files under `Assets/TaskBubble/CAD/Models`, remapping face indices while preserving coordinate, UV, normal, winding, and material data. It also prepares the single coupling model. It refuses to overwrite modified generated files.

The models are Base, Finger1–4 A/B, Fingertip A/B, and Coupling. A/B identifies the paired exported instances; it does not yet assert left/right placement.

`AgentScripts/ImportCadModels.cs`, entry `ImportCadModels.Import`, sets each generated model's Scale Factor to 0.001. The source coordinates are millimetres, and the imported components are metres. The gripper's combined dimensions are approximately 0.1524 × 0.1527 × 0.0750 m. Do not apply a second millimetre conversion when assembling the prefab.

Import validation checks twelve separate static meshes, matching scaled bounds for both source models, and unchanged triangle counts: 258,566 for the gripper and 16,898 for the coupling. The three gripper colors and black coupling material import as URP Lit materials. No geometry has been removed or simplified in this step.

## Assembly prefab

`Assets/TaskBubble/CAD/GripperAssembly.prefab` contains the eleven fixed meshes under `GripperBody` and the coupling under `Connector`. The assembly root sits at the bottom of the coupling, with the fingers pointing up. Scale is still 1; use a uniform parent scale to enlarge the whole assembly for the inspection scene.

The coupling is turned to match the gripper's mounting axis and sits flush under the base. Its pivot is at the centre of the upper mounting face. `Connector` has the only `EditablePart`: ID `gripper.connector`, local Y rotation, limits -45 to +45 degrees. Its mesh is offset under that pivot. The circular outline changes little during rotation; watch the holes and cutouts.

Each fixed mesh has a simple box collider. The connector has one box collider matching its mesh bounds; these are selection hitboxes, not detailed collision geometry. No rigidbodies or joint simulation are added.

`AgentScripts/CreateGripperAssembly.cs`, entry `CreateGripperAssembly.Build`, creates the prefab using Unity APIs. It refuses to overwrite an existing prefab. Verification checked twelve meshes, twelve colliders, one configured editable part, no mounting gap, and a 30-degree rotation with a fixed pivot and unchanged gripper body. Calculating a proposal leaves the actual connector unchanged.

To inspect it, double-click `GripperAssembly.prefab` in the Project window. Select `Connector` and change its local Y rotation to see the pivot; undo the change afterwards. Scene wiring, the enlarged table display, and the network ghost remain for integration.
