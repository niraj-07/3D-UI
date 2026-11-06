# TaskBubble — IEEE VR 2027 3DUI Contest PRD

**Status:** Proposed direction; novelty and benefits not yet validated  
**Contest:** [IEEE VR 2027 3DUI Contest](https://ieeevr.org/2027/contribute/3dui-contest/) — *Beyond Shared Reality: 3D User Interfaces for Shared Spatial Interaction*  
**First milestone:** [Hackathon MVP](./PRD_HACKATHON_MVP.md)

## Product context

**TaskBubble** is a collaborative 3D interaction technique for people using different devices. A VR user identifies a difficult or precision-sensitive edit in a shared CAD assembly and temporarily delegates a *limited operation* to a desktop collaborator. The desktop user proposes a precise adjustment; the VR user sees it in the original scene and decides whether to apply it.

The CAD model provides a realistic setting, **not a complete CAD editor**. The research question is whether bounded delegation and contextual previews improve coordination, perceived control, or errors compared with ordinary shared editing. Similar work already exists in cross-device collaboration and task handoff, so **novelty is a hypothesis to investigate, not an established claim**.

## Use case

Two collaborators inspect a mechanical assembly, such as a robotic gripper. The VR participant sees the whole mechanism at scale and notices a misaligned connector. They delegate its allowed rotation or translation. The desktop participant edits the selected part using precision controls. A translucent proposed pose appears in VR while the actual part remains unchanged. The VR participant checks clearance/alignment and accepts, rejects, or requests a revision.

This should be useful when **desktop precision and immersive contextual judgment provide complementary value**, rather than making two people do a task one person could perform just as easily.

## Product features

**Core interaction (carried over from hackathon):**

- Shared CAD assembly with individually selectable parts and consistent identities on both devices.
- VR part selection and explicit request to delegate an edit.
- Desktop close-up view with precise position/rotation controls.
- Allowed operations/axes defined for each delegated task; clear indication of who controls the edit.
- Live translucent preview in VR; original object is unchanged until approval.
- Accept/reject and safe reset, consistent for both users.

**Potential IEEE extensions (not automatically approved work):**

- Better constraint/clearance feedback and more meaningful precision tasks.
- Revision requests, clearer ownership cues, and recovery if a task is interrupted.
- Additional editable parts and improved runtime manipulation controls.
- Comparison against VR-only and direct PC+VR editing, with selected usability measures.

Build only extensions that strengthen the final interaction contribution and have been approved by the team.

## IEEE VR judging criteria

The contest evaluates the following **without published numerical weights**:

| Criterion | What TaskBubble needs to demonstrate |
|---|---|
| Innovation of the 3DUI | A clearly differentiated delegation/preview technique, not merely VR + desktop editing |
| Shared spatial interaction | Both collaborators make meaningful contributions in one shared 3D task |
| Usability and user experience | Understandable controls, visible ownership, useful feedback, low friction |
| Technical quality and robustness | Consistent state, reliable live interaction, repeatable demo |
| Impact and creativity | A credible engineering use case and clear benefit over a simpler alternative |

**Research standard:** Compare TaskBubble with prior systems such as asymmetric VR collaboration, virtual replicas, and collaborative PC/VR editors. An attractive CAD model and a functioning networked demo alone do not demonstrate a novel technique.

**Submission:** Plan for team registration **December 11, 2026**, and abstract/video submission **December 16, 2026**, subject to the official contest page. A two-page extended abstract, 3–5 minute demo video, and working system or deployment instructions are expected.

## Scope rule for contributors and AI agents

Implement agreed requirements fully and cleanly. **Do not add speculative features, new dependencies, extra architectural layers, or broad test infrastructure without asking the team first.** Use proportionate verification of the actual interaction; necessary bug fixes and straightforward implementation decisions do not require repeated approval.
