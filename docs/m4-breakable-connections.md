# M4.1 Normalized Structural Connection Loads

## Observed M3 Limitation

M3 global and local support checks significantly reduced floating blocks, and manual right-base removal produced a more natural result. However, an authored graph edge still behaved like an invisible connection with unlimited strength whenever its support path remained valid.

## Conclusion

**Structural adjacency does not imply unlimited structural strength.**

M4 keeps authored relationships but gives each relationship finite, inspectable gravity and impact capacities.

## Observed Manual-Play Problem

Manual Play Mode testing showed that structures remained fully static after repeated projectile impacts even though automated M4 verification passed.

The investigation found four causes:

- Normal direct hits on the weak structure produced roughly `29.7-35.2 N·s`, below the old scalar strength of `50`.
- Impact did not accumulate, so repeated sub-threshold hits never approached failure.
- Automated checks injected a synthetic impulse of `60`, substantially above representative weak gameplay hits.
- The old model directly added gravitational force in newtons to collision impulse in newton-seconds.

Automated verification therefore passed while gameplay calibration failed. M4.1 corrects the model and calibration, but manual gameplay success is not claimed until Play Mode validation is completed.

## Connection Representation

Each `StructuralConnection` component stores:

- endpoint `BlockA`
- endpoint `BlockB`
- `gravityCapacity` in newtons
- `impactCapacity` in newton-seconds
- active/broken state
- current gravity load and impact impulse
- gravity utilization, impact utilization, and combined failure score

M4 managers build adjacency only from active connections whose endpoints still participate in the static model. Earlier scenes contain no explicit connection list and continue using their legacy authored neighbor lists.

## Load Model

For each active connection, the two demands are normalized against capacities with matching units:

`gravityUtilization = gravityLoad / gravityCapacity`

`impactUtilization = impactImpulse / impactCapacity`

`failureScore = gravityUtilization + impactUtilization`

The connection breaks when `failureScore > 1.0`.

The combined score is dimensionless. This avoids directly adding force and impulse while preserving a simple game-oriented relationship: existing gravity demand reduces the remaining impact margin.

### Impact Contribution

Only active connections directly incident to the impacted block receive impact load:

`impactUtilization = |collision impulse| / impactCapacity`

No full-structure shock wave or recursive impact propagation is applied. Connections elsewhere receive zero impact contribution, although a topology change may later alter their gravity load. Impact is evaluated independently for each collision and does not accumulate across hits.

### Gravity and Cantilever Contribution

The evaluator temporarily removes one connection and traverses from each endpoint.

- If the endpoints still reach each other through another route, the connection has an alternate path and receives zero cantilever load.
- If both separated sides contain anchors, neither side is treated as dependent.
- If exactly one side contains an anchor, the opposite side is the dependent mass carried through that connection.
- If neither side contains an anchor, global connectivity handles the unsupported structure.

For a dependent side:

`leverDistance = |dependentCOM.x - connectionMidpoint.x|`

`gravityCantileverLoad = dependentMass × |gravity| × (1 + leverDistance × leverArmMultiplier)`

The default `leverArmMultiplier` remains `0.5`. Longer and heavier overhangs therefore produce greater estimated load than short, light overhangs. M4.1 does not change this gravity approximation.

## Initial Calibration

The capacities are based on the measured gameplay impulse range rather than the previous synthetic `60 N·s` assumption:

| Connection type | Gravity capacity | Impact capacity |
| --- | ---: | ---: |
| Weak | `120 N` | `28 N·s` |
| Reinforced | `240 N` | `60 N·s` |

A `30 N·s` hit with no gravity demand produces a weak failure score of `1.071`, so a solid representative direct hit can break a weak connection. The same hit produces a reinforced score of `0.5`, so it normally survives. A measured weak cantilever load of `73.575 N` combined with a moderate `12 N·s` impact produces a weak score of `1.041`, demonstrating that gravity reduces the remaining impact margin.

The reinforced capacities are twice the weak gravity capacity and slightly more than twice the impact capacity. This keeps the distinction easy to explain while allowing a sufficiently strong hit or significant combined gravity and impact demand to break reinforced connections.

## Break Event and Reevaluation

Connection evaluation runs only during structural events: initialization, projectile impact, block removal, and connection-break cascades.

When one or more connections exceed a failure score of `1.0`:

1. Mark the connections broken.
2. Rebuild adjacency from remaining active connections.
3. Recalculate gravity/cantilever loads until no additional connection breaks.
4. Run graph connectivity.
5. Run global COM/support-interval stability.
6. Run M3 local support and cantilever validation.
7. Release newly unsupported blocks to Rigidbody physics.

Breaking a connection does not destroy either block. M4 impacts target connections, while previous milestone scenes retain their existing hit-block destruction behavior.

## Interaction with M3

Normalized connection capacity is the first structural-capacity check in M4. The M3 `maxCantileverBlocks` rule remains a conservative geometric fallback after the active connection graph is rebuilt. A connection may fail before the local span limit, while a surviving connection path must still satisfy actual vertical/horizontal contact and the configured local span.

## Test Scene

`M4_BreakableConnections` contains 23 blocks and 25 explicit connections:

- Weak structure: 11 blocks, 11 connections, capacities `120 N` and `28 N·s`
- Reinforced structure: 12 blocks, 14 connections, capacities `240 N` and `60 N·s`

The additional brace is a physical block with connections to the right base, right mid, and upper beam. Automated checks now use representative `30 N·s` and `35 N·s` direct hits, plus measured gravity-load values from the M4 investigation.

## Debugging and Event Metrics

- Green connection: low failure score
- Yellow/red connection: utilization approaching failure
- Magenta connection: broken
- Gray connection: endpoint released or destroyed
- Inspector: both capacities, both current demands, both utilizations, failure score, and active state
- Break log: gravity load/utilization, impact impulse/utilization, and final failure score
- Event log: active block count, active connection count, connection evaluations

There is no per-frame full-structure load evaluation.

## Expected Manual Scenarios

- Symmetric supported structures remain static.
- A local weak-joint impact breaks the nearby connection.
- A resulting long cantilever can overload additional carrying connections.
- A distant roof impact does not directly break the remote base joint.
- The reinforced alternate path survives the same impact that compromises the weak structure.

These are automated state expectations. Natural-looking Play Mode behavior still requires manual validation.

## Simplifications

- Scalar gravity and impact capacities per connection
- No stress tensor or distributed stress field
- No deformation, material fatigue, or beam bending solver
- X-axis lever distance only
- Impact contribution limited to directly incident connections
- No joints or physical constraint between blocks
- No dynamic connection reconstruction or reconnection
- Parallel connections between the same endpoints are not modeled
