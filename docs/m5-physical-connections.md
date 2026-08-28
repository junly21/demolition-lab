# M5 Physical Breakable Connections

## Observed M4.1 Limitation

M4.1 improved targeting and made weak logical connections break under representative projectile impulses. Manual Play Mode still looked artificial: supported blocks remained frozen until a rule released them, nearby pieces could unlock one at a time, and removing a middle block did not physically influence both sides while they remained logically supported.

The root conclusion is:

**Logical structural connection is not physical force transmission.**

`StructuralConnection` described authored adjacency, capacity, and support semantics, but a logical edge could not transmit force or torque between Rigidbody components. M5 preserves that model and adds a built-in Unity physical constraint for the dedicated M5 scene.

## Physical Joint Representation

Each M5 `StructuralConnection` references one `ConfigurableJoint` attached to one endpoint Rigidbody and connected to the other endpoint Rigidbody.

The joint configuration is intentionally small:

- linear X/Y/Z motion is locked
- angular X/Y/Z motion is limited to `3 degrees`
- angular limit spring is `1200`, with damper `90`
- Slerp drive spring is `900`, damper `80`, maximum force `3000`
- position-and-rotation projection uses `0.03 m` and `3 degrees`
- connected-body collision is disabled

`ConfigurableJoint` was selected instead of a perfectly rigid `FixedJoint` so the prototype can show small controlled angular response before complete failure while still transmitting linear force and torque. It remains a standard Unity constraint; no custom solver is implemented.

## Anchoring and Rigidbody Roles

The M5 test frame contains 45 structural blocks:

- 4 ground/base anchors are kinematic and explicitly hold the frame to the ground.
- 41 upper, beam, and stepped-brace blocks are dynamic from initialization.
- Dynamic blocks use `1 kg`; anchors use `4 kg`.
- Dynamic bodies use linear damping `0.12`, angular damping `0.35`, 12 solver iterations, 4 solver-velocity iterations, interpolation, and continuous speculative collision detection.

`StructureManager.usePhysicalJoints` controls this M5-only behavior. Earlier scenes leave it disabled, so M1-M4 retain their original supported-kinematic lifecycle. In physical mode, `KeepSupported` updates logical state and materials but never freezes a dynamic upper block back into world space. Released anchors remain kinematic; released non-anchors continue dynamically.

## Weak and Reinforced Profiles

The normalized M4.1 capacities remain logical metadata:

| Profile | Gravity capacity | Impact capacity |
| --- | ---: | ---: |
| Weak | `120 N` | `28 N·s` |
| Reinforced | `240 N` | `60 N·s` |

Physical thresholds are calibrated separately because Unity joint force and torque are not the same quantities as M4.1 utilization inputs:

| Profile | Joint break force | Joint break torque |
| --- | ---: | ---: |
| Weak | `1100 N` | `550 N·m` |
| Reinforced | `2600 N` | `1300 N·m` |

The physical values preserve an explainable stronger/weaker relationship without reinterpreting `28 N·s` as `28 N`. Automated PhysX simulation verifies that a representative `30 N·s` impulse can break at least one Weak joint while the equivalent Reinforced target transmits motion without breaking a Reinforced joint.

## Break Authority and Synchronization

M5 gives Unity joint physics authoritative responsibility for connection failure.

1. A projectile collides normally and Unity resolves its collision impulse.
2. The impacted dynamic block transmits force and torque through connected joints.
3. PhysX breaks a joint when its measured force or torque exceeds the configured threshold.
4. `StructuralConnection.OnJointBreak` records the event; a follow-up state check identifies the destroyed joint component even when multiple joints share one block.
5. The matching logical connection is marked inactive.
6. `StructureManager` rebuilds adjacency and reruns graph connectivity, global COM stability, and local support evaluation.
7. Unsupported blocks leave logical evaluation while their Rigidbody motion and surviving internal joints continue naturally.

The M4.1 normalized evaluator is not allowed to independently break connections in physical-joint mode. It remains available as inspectable capacity metadata and still controls M4.1. This prevents the logical and physical systems from disagreeing over which system broke an M5 connection.

Destroying or deactivating a logical connection also removes its referenced physical joint, preserving synchronization in the opposite direction.

## Test Structure

`M5_PhysicalConnections` is a 45-piece side-view engineering frame with 50 explicit connections:

- four five-block columns
- three top beam spans
- two middle beam spans
- left Weak stepped-brace bay
- right Reinforced stepped-brace bay
- four explicit ground anchors

All authored cubes touch without initial collider penetration. Orange joint Gizmos identify Weak physical connections, cyan identifies Reinforced connections, magenta identifies broken joints, and gray identifies connections whose endpoints have left logical evaluation.

## Automated Validation

`DemolitionLab > Validate M5 Scene` checks:

- exactly 45 blocks, 50 connections, and 4 anchors
- every logical connection has a matching physical joint
- Weak and Reinforced capacities and break thresholds
- no initial collider penetration greater than `0.0001 m`
- anchors initialize kinematic and all 41 non-anchors initialize dynamic
- all blocks initially pass graph, COM, and local-support evaluation
- 150 fixed physics steps remain within idle speed/drift limits without joint failure
- a `30 N·s` Weak impact propagates to a neighbor and breaks a Weak joint
- the same Reinforced-side impact propagates motion without breaking a Reinforced joint
- a destroyed physical joint cannot remain logically active

These tests verify deterministic state and basic stability. They do not prove final visual quality.

## Manual Validation Scenarios

- **A — Stable idle:** Confirm no visible explosion, persistent jitter, or gradual collapse.
- **B — Weak lower connection:** Hit the lower left frame and confirm connected upper pieces respond before or during Weak joint failure.
- **C — Reinforced equivalent:** Apply a comparable hit to the right frame and confirm motion transmits while Reinforced joints usually survive.
- **D — Middle beam:** Confirm neighboring blocks react instead of one isolated cube appearing to unlock.
- **E — Support removed:** Confirm the remaining connected assembly responds immediately to gravity and torque without frozen floating blocks.
- **F — Chain failure:** Confirm one physical break can redistribute motion and lead to later joint failures without scripted remote breaks.
- **G — Distant impact:** Confirm unrelated remote joints do not fail randomly.

Manual Play Mode validation is still required before claiming that M5 produces natural or final-quality demolition.

## Simplifications and Stability Trade-offs

- Built-in Unity joints only; no custom constraint solver.
- Small angular limits provide visible compliance but are not beam deformation.
- Projection limits drift and runaway separation but can correct constraint error non-physically.
- Increased per-body solver iterations improve the 50-joint frame at a higher CPU cost acceptable for this test scene.
- Moderate damping reduces residual oscillation without freezing dynamic blocks.
- No deformable materials, stress tensor, fracture mesh, fatigue, plastic deformation, or dynamic reconnection.
- Logical COM remains an X-axis gameplay approximation.
- Joint thresholds are gameplay calibration values, not engineering material properties.
