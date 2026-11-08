# TaskBubble tasks

Implementation checklist. Video recording and delivery are handled separately from the source repository.

- [x] **1 [Setup] Project setup** — Unity project, pinned packages, and Git exclusions.
- [x] **2 [Shared] Edit contract** — Part IDs, rotation constraints, task states, and shared methods.
- [x] **3 [Network] Host/client connection** — Desktop host, VR client, address field, and connection feedback.
- [x] **4 [Network] Shared task state** — One active task, live proposals, and host validation.
- [x] **5 [CAD] Assembly preparation** — Separate meshes, selection colliders, fingertip pivots, and stable IDs.
- [x] **6 [VR] Part selection** — Controller/hand pointing, highlighting, and Delegate.
- [x] **7 [Desktop] Editing view** — Component close-up, rotation slider, and task status.
- [x] **8 [Preview] Live ghost** — Translucent proposal without moving the real component.
- [x] **9 [Review] Submit and review controls** — Desktop Submit and VR Accept/Reject.
- [x] **10 [Network] Apply, reject, and reset** — Approved poses, rejection retries, and per-part reset.
- [x] **11 [Integration] Main scene and builds** — Shared workshop, role-based UI, XR startup, and Windows build setup.
- [x] **12 [Demo] Instructions** — Setup, controls, demo sequence, and submission README.
- [x] **13 [Desktop/CAD] Precision editing and second correction** — Exact angle input and different rotation axes on A and B.

## Git workflow

Work was split into task branches such as `task/13-precision-editing-scenarios`, with small commits for individual steps. Completed tasks were brought onto main through rebase and consolidated into one commit per task. The original task branches retain their individual commits. New squash commit messages start with `SQUASH`.

Runtime files are grouped by feature under `Assets/TaskBubble/`: `Shared`, `Networking`, `CAD`, `VR`, `Desktop`, `Preview`, `Review`, and `Integration`. Prefabs keep feature work separate; MainScene connects them. Package and project settings are shared across both application instances.
