# Vertical Slice Reuse Audit

The vertical slice is a demolition puzzle, not another structural-simulation milestone. Stage 01 reuses only the systems that directly support aiming, firing, readable weak points, decisive collapse, and immediate retry.

| System | Decision | Stage 01 use |
| --- | --- | --- |
| `LauncherController` | REUSE | Keep keyboard aiming, firing, Inspector projectile tuning, and the existing muzzle setup. Add only an input gate and shot event for stage flow. |
| Trajectory preview | REUSE | Keep the existing line preview because it makes a three-shot puzzle readable. |
| `Projectile` | REUSE | Keep Rigidbody launch, collision impulse reporting, and post-impact cleanup. Add a small impact event for camera/dust feedback. |
| `StructuralBlock` | DO NOT USE | Stage pieces do not need the M1-M5 supported/released lifecycle. Ordinary pieces remain normal dynamic Rigidbodies. |
| `StructureManager` | DO NOT USE | Stage 01 does not run BFS release, COM, local support, or structural handoff rules. Collapse is authored through a few critical physical supports. |
| `StructuralConnection` | SIMPLIFY | Do not use the M4/M5 capacity model. Stage 01 uses a small `CriticalSupportJoint` component around four gameplay-critical Unity joints. |
| `LocalSupportEvaluator` | DO NOT USE | The introductory stage does not need geometric cantilever reasoning. Physical support removal directly causes collapse. |
| `StructuralLoadEvaluator` | DO NOT USE | No normalized gravity/impact utilization is evaluated in Stage 01. Joint thresholds are tuned directly for gameplay. |
| Global COM logic | DO NOT USE | Success is based on target-piece destruction, not COM stability. Rigidbody physics determines the collapse. |
| M5 ConfigurableJoint system | SIMPLIFY | Reuse the built-in joint idea, but only for four support connections rather than connecting every piece. Angular motion is nearly rigid. |
| Debug visualization | SIMPLIFY | Retain concise support-joint Gizmos and Inspector thresholds. Do not expose the full research graph overlay in the final gameplay view. |

## Stage 01 Architecture

- Two weak lower supports and two stronger upper support connections communicate the intended targets.
- Platform, upper structure, and decorative pieces use ordinary Rigidbody and Collider behavior without an all-piece joint network.
- `VerticalSliceStageController` owns three-shot flow, destruction evaluation, clear/fail timing, HUD, and retry.
- `StageTargetPiece` provides a simple target metric based on falling, displacement, or major rotation.
- `CriticalSupportJoint` synchronizes a physical joint break with stage feedback only; it does not introduce another structural solver.

## Explicitly Rejected

- No M6 or additional simulation layer.
- No BFS/COM/local-support decision in the final Stage 01 scene.
- No joint on every block.
- No save, progression, backend, stage-selection menu, pooling, or generalized scoring architecture.
- No claim that the stage is fun until manual Play Mode testing confirms readable targeting, decisive collapse, and retry appeal.
