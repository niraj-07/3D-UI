# TaskBubble Lite — Immersion Studio Hackathon PRD

**Status:** 72-hour MVP of [TaskBubble for IEEE VR 2027](./PRD_IEEE_VR_2027.md)  
**Event:** Immersion Studio Hackathon, November 6–9, 2026, Texas State University  
**Goal:** A complete, repeatable collaborative interaction—not a throwaway animation or full CAD tool.

## Product context

TaskBubble Lite demonstrates how a VR user can temporarily hand a precision-sensitive operation to a desktop collaborator while remaining in the 3D scene. The desktop user proposes an adjustment, and the VR user previews it before deciding whether it should become permanent.

The MVP should be **small in scope but reusable** for the IEEE VR project. A working interaction matters more than an elaborate environment or a large number of editing features.

## Use case and demo experience

A small CAD-derived robotic gripper contains a misaligned connector.

1. **VR user** points at the connector and selects **Delegate**.
2. **Desktop user** sees the component in a close-up view and adjusts its angle using a slider (optional position adjustment only if approved).
3. **VR user** watches a translucent ghost follow the proposed edit in real time. The original component stays in place.
4. **VR user** selects **Accept** to commit the change or **Reject** to discard it.
5. Both views show the same final state, and the demo can be reset and repeated.

The demo should make clear why the two perspectives matter: **the desktop offers precision, while VR offers full-scene spatial review**.

## MVP features

| Required | Description |
|---|---|
| Shared assembly | One pre-imported CAD assembly with separate parts; one editable component is enough |
| VR controls | Controller-ray selection and Delegate / Accept / Reject |
| Desktop controls | Close-up component view and at least one rotation slider |
| Live shared state | Desktop edits update the VR proposal, without moving the committed object |
| Ghost preview | Proposed transform is visually distinct from the original |
| Reliable task flow | One active delegation; accept, reject, and reset behave consistently |
| Demo readiness | Runs on actual hardware without touching Unity Editor during the interaction |

**Out of scope for this MVP:** Full CAD modeling, runtime STEP import, realistic assembly physics, multiple concurrent tasks, custom hand tracking, and arbitrary multi-object editing. These may be explored later if useful.

**Hardware requirement to confirm:** The event refers to Immersion Studio projection and interactive technology. Verify that the planned VR + PC/projected setup qualifies; don't add specialized hardware integration unless required and approved.

## Hackathon judging criteria

| Criterion | Weight | What to emphasize |
|---|---:|---|
| **Novelty & technical contribution** | **35%** | Clear delegation mechanism, proposal vs. committed state, why the interaction is useful |
| **Technical execution** | **25%** | Reliable VR + desktop connection, correct state updates, smooth repeatable workflow |
| **Presentation & clarity** | **20%** | Understandable use case and 3–5 minute video showing the real system and key code |
| **Collaboration & team dynamics** | **20%** | Meaningful contributions from all four members; shared understanding of the code |

**Submission:** Repository link and **3–5 minute demo video**, including a short explanation of important code, via the [submission form](https://forms.cloud.microsoft/r/0hwfGAFkXA) by **Monday, November 9 at noon**. Commit throughout the event; finalists should be ready for a short team Q&A.

## Scope rule for contributors and AI agents

Implement the agreed MVP completely. **Don't generate extra managers, helper files, packages, features, or redundant tests merely because they might be useful later. Ask before adding unapproved scope or major complexity.** 
