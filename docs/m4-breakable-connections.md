# M4 Breakable Structural Connections

## Observed M3 Limitation

M3 global and local support checks significantly reduced floating blocks, and manual right-base removal produced a more natural result. However, an authored graph edge still behaved like an invisible connection with unlimited strength whenever its support path remained valid.

## Conclusion

**Structural adjacency does not imply unlimited structural strength.**

M4 keeps authored relationships but gives each relationship a finite, inspectable failure threshold.

## Connection Representation

Each `StructuralConnection` component stores:

- endpoint `BlockA`
- endpoint `BlockB`
- scalar `connectionStrength`
- active/broken state
- current estimated load

M4 managers build adjacency only from active connections whose endpoints still participate in the static model. Earlier scenes contain no explicit connection list and continue using their legacy authored neighbor lists.

## Load Model

For each active connection:

`connectionLoad = impactLoad + gravityCantileverLoad`

The connection breaks when:

`connectionLoad > connectionStrength`

This is a gameplay-oriented approximation, not a physical stress calculation.

### Impact Contribution

Only active connections directly incident to the impacted block receive impact load:

`impactLoad = |collision impulse| × impactLoadScale`

The default scale is `1`. No full-structure shock wave or recursive impact propagation is applied. Connections elsewhere receive zero impact contribution, although a topology change may later alter their gravity load.

### Gravity and Cantilever Contribution

The evaluator temporarily removes one connection and traverses from each endpoint.

- If the endpoints still reach each other through another route, the connection has an alternate path and receives zero cantilever load.
- If both separated sides contain anchors, neither side is treated as dependent.
- If exactly one side contains an anchor, the opposite side is the dependent mass carried through that connection.
- If neither side contains an anchor, global connectivity handles the unsupported structure.

For a dependent side:

`leverDistance = |dependentCOM.x - connectionMidpoint.x|`

`gravityCantileverLoad = dependentMass × |gravity| × (1 + leverDistance × leverArmMultiplier)`

The default `leverArmMultiplier` is `0.5`. Longer and heavier overhangs therefore produce greater estimated load than short, light overhangs.

## Break Event and Reevaluation

Connection evaluation runs only during structural events: initialization, projectile impact, block removal, and connection-break cascades.

When one or more connections exceed their strength:

1. Mark the connections broken.
2. Rebuild adjacency from remaining active connections.
3. Recalculate gravity/cantilever loads until no additional connection breaks.
4. Run graph connectivity.
5. Run global COM/support-interval stability.
6. Run M3 local support and cantilever validation.
7. Release newly unsupported blocks to Rigidbody physics.

Breaking a connection does not destroy either block. M4 impacts target connections, while previous milestone scenes retain their existing hit-block destruction behavior.

## Interaction with M3

Connection strength is the first structural-capacity check in M4. The M3 `maxCantileverBlocks` rule remains a conservative geometric fallback after the active connection graph is rebuilt. A connection may fail before the local span limit, while a surviving connection path must still satisfy actual vertical/horizontal contact and the configured local span.

## Test Scene

`M4_BreakableConnections` contains 23 blocks and 25 explicit connections:

- Weak structure: 11 blocks, 11 connections, strength `50`
- Reinforced structure: 12 blocks, 14 connections
- Reinforced primary right joint: strength `50`
- Reinforced brace path: strength `120`

The additional brace is a physical block with connections to the right base, right mid, and upper beam. Under the deterministic `60` impulse test, the weak primary joint breaks in both structures, but the reinforced backup path survives.

## Debugging and Event Metrics

- Green connection: low normalized load
- Yellow/red connection: load approaching strength
- Magenta connection: broken
- Gray connection: endpoint released or destroyed
- Inspector: strength, current load, active state
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

- Scalar connection strength only
- No stress tensor or distributed stress field
- No deformation, material fatigue, or beam bending solver
- X-axis lever distance only
- Impact contribution limited to directly incident connections
- No joints or physical constraint between blocks
- No dynamic connection reconstruction or reconnection
- Parallel connections between the same endpoints are not modeled
