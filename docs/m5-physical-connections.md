# M5 Physical Breakable Connections

## Observed M4.1 Limitation

M4.1 improved targeting and made weak logical connections break under representative projectile impulses. Manual Play Mode still looked artificial: supported blocks remained frozen until a rule released them, nearby pieces could unlock one at a time, and removing a middle block did not physically influence both sides while they remained logically supported.

The root conclusion is:

**Logical structural connection is not physical force transmission.**

`StructuralConnection` described authored adjacency, capacity, and support semantics, but a logical edge could not transmit force or torque between Rigidbody components. M5 preserves that model and adds a built-in Unity physical constraint for the dedicated M5 scene.

## M5.1 Manual Calibration Finding

Manual M5 testing confirmed force transmission, but the initial `3 degree` limit accumulated visibly across many joints and made the frame behave like an articulated rubber chain. The original Weak `1100 N / 550 N·m` and Reinforced `2600 N / 1300 N·m` thresholds were also too high for actual gameplay. The latest 40-hit manual sequence ranged from near zero to `17.55 N·s` with a `5.5 N·s` median; useful direct hits reached roughly `12-17.5 N·s`, substantially below the earlier automated `30 N·s` input.

M5.1 therefore targets a mostly rigid intact frame with subtle local response, followed by decisive Weak failure under a well-placed gameplay hit. It does not return supported blocks to kinematic mode.

## Physical Joint Representation

Each M5 `StructuralConnection` references one `ConfigurableJoint` attached to one endpoint Rigidbody and connected to the other endpoint Rigidbody.

The joint configuration is intentionally small:

- linear X/Y/Z motion is locked
- angular X/Y/Z motion is limited to `0.5 degrees`
- angular limit spring is `5000`, with damper `300`
- Slerp drive spring is `4000`, damper `250`, maximum force `10000`
- position-and-rotation projection uses `0.01 m` and `0.5 degrees`
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
| Weak | `450 N` | `180 N·m` |
| Reinforced | `1200 N` | `500 N·m` |

The physical values preserve an explainable stronger/weaker relationship without reinterpreting `28 N·s` as `28 N`. Automated PhysX simulation now uses the measured gameplay range: a centered `8 N·s` Weak hit survives, a well-placed `15 N·s` hit breaks at least one Weak joint, and the equivalent Reinforced target transmits motion without breaking a Reinforced joint.

`StructuralConnection` samples `ConfigurableJoint.currentForce` and `currentTorque` during Play Mode and exposes current and peak values in the Inspector. These values support later manual calibration but are not treated as a new failure model.

## Projectile Baseline and Cleanup

Projectile power remains unchanged at `2 kg` and `18 m/s`. M5.1 solves the mismatch through physical threshold calibration rather than an exaggerated projectile. The latest manual sequence showed collision impulses from `0-17.55 N·s`, with stronger useful contacts around `12-17.5 N·s`.

After its first collision with a tagged structural block, a projectile is destroyed after `1.25 seconds`. This preserves short impact feedback while preventing already-spent red projectiles from remaining for the full `10 second` launch lifetime.

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
- 500 fixed physics steps, equivalent to 10 seconds, remain within idle speed/drift limits without joint failure
- a centered `8 N·s` Weak impact survives
- a well-placed `15 N·s` Weak impact propagates to a neighbor and breaks a Weak joint
- the same `15 N·s` Reinforced-side impact propagates motion without breaking a Reinforced joint
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
