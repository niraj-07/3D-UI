# CAD preparation

Source: the supplied Robotiq 2F-85 Open gripper and W-010-B blank coupling exports. The manufacturer download page is [Robotiq Support](https://robotiq.com/support). Original OBJ/MTL exports remain in `Assets/CAD model/Converted`; the STEP and FreeCAD working documents are left untouched.

The original gripper OBJ declares eleven objects, but Unity imports it as one mesh. `AgentScripts/PrepareCadModels.py` splits those declarations into separate files under `Assets/TaskBubble/CAD/Models`, remapping face indices while preserving coordinate, UV, normal, winding, and material data. It also prepares the single coupling model. It refuses to overwrite modified generated files.

The models are Base, Finger1–4 A/B, Fingertip A/B, and Coupling. A/B identifies the paired exported instances; it does not yet assert left/right placement.

`AgentScripts/ImportCadModels.cs`, entry `ImportCadModels.Import`, sets each generated model's Scale Factor to 0.001. The source coordinates are millimetres, and the imported components are metres. The gripper's combined dimensions are approximately 0.1524 × 0.1527 × 0.0750 m. Do not apply a second millimetre conversion when assembling the prefab.

Import validation checks twelve separate static meshes, matching scaled bounds for both source models, and unchanged triangle counts: 258,566 for the gripper and 16,898 for the coupling. The three gripper colors and black coupling material import as URP Lit materials. No geometry has been removed or simplified in this step.

The components retain their source assembly coordinates. Their pivots are not yet prepared for independent joint rotation. The next step is to create the assembly prefab, align the coupling, and configure its pivot, collider, stable ID, and rotation limits. A separate parent can enlarge the complete assembly for the table-top inspection scene later.
