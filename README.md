# DemolitionLab

**Current milestone:** M2

DemolitionLab is a small Unity prototype for comparing physics demolition, graph connectivity, and center-of-mass structural stability.

## Controls

- **Aim:** `WASD` or arrow keys
- **Fire:** `Space`
- **Reset scene:** `R`

## Scenes

- `M0_DemolitionLab`: baseline stacked-block scene using only Rigidbody physics
- `M1_StructuralGraph`: support-graph scene with two anchored pillars, a bridge, and a non-critical upper block
- `M2_StructuralStability`: side-view structure that adds center-of-mass stability to graph connectivity

## M0 Baseline

M0 showed that lower blocks can eventually be pushed out and cause a collapse, but the timing depends on accumulated displacement, friction, and incidental contacts. Pure physics has no explicit concept of whether a block still has a valid structural path to the ground.

## M1 Structural Support

M1 represents each structural block as a node and each configured neighbor relationship as an undirected edge. Two base blocks are marked as ground anchors.

After a projectile destroys a structural block, the node becomes inactive and its renderer and collider are disabled. `StructureManager` then runs breadth-first search (BFS) from every active ground anchor:

1. Enqueue all active ground-anchor nodes.
2. Traverse active neighboring nodes.
3. Keep reached nodes kinematic and visually mark them as supported.
4. Make active nodes not reached by BFS non-kinematic and visually mark them as unsupported.

The graph decides **whether** a block is supported. Unity Rigidbody physics decides **how** unsupported blocks fall and collide.

## M2 Structural Stability

M1 connectivity scenarios passed, but manual play exposed an overhanging L-shaped structure that remained frozen because one graph path to ground still existed. M2 tests the conclusion that **connected does not necessarily mean stable**.

For each active connected component, M2:

1. Checks whether the component contains an active ground anchor.
2. Computes mass-weighted center of mass from actual Rigidbody masses.
3. Builds an X-axis support interval from the active anchors' Collider bounds.
4. Keeps the component kinematic only when `COM.x` lies inside the support interval plus tolerance.
5. Releases the entire unstable or disconnected component to Unity Physics together.

M2 hit blocks are structurally removed but remain as gray dynamic debris. Released components are never automatically reconnected or frozen again.

## Debugging

- Supported blocks use the green M1 material.
- Unsupported dynamic blocks use the red M1 material.
- Destroyed blocks are hidden; their former bounds and broken graph edges are gray Gizmos in Scene view.
- Active supported graph edges are green Gizmos and unsupported edges are red.
- Recalculation logs include the destroyed node ID, visited/supported count, and unsupported count.
- M2 displays each component COM as a Gizmo sphere and its support interval as a ground line.
- M2 graph/state colors are green for stable, yellow for unstable, red for disconnected, and gray for destroyed debris.

## M1 Manual Test Scenarios

- **A — Non-critical node:** Destroy `Roof_Cap`; the remaining supported structure should stay stable.
- **B — Redundant support:** Destroy `Left_Base`; all remaining blocks should still reach `Right_Base` through the bridge.
- **C — Final support paths:** Destroy both `Left_Base` and `Right_Base`; all remaining active nodes should become dynamic.
- **D — Split components:** Destroy `Bridge_Center`; both anchored sides should remain stable while the disconnected `Roof_Cap` falls.

## M2 Manual Test Scenarios

- **A — Both supports:** The intact structure should remain connected and stable.
- **B — Upper block:** Removing `Roof_Cap` should remain stable if COM stays in the support interval.
- **C — Left support:** The connected overhang should become unstable and topple after the whole component is released.
- **D — Right support:** The heavy left-supported component should remain stable despite losing one support.
- **E — No supports:** Removing both anchors should release the remaining disconnected component regardless of COM.

## Current Scope

M2 intentionally uses a static graph, binary node destruction, 2D X-axis support intervals, and standard Rigidbody simulation. Structural edge strength, stress, joints, fracture, support polygons, dynamic reconnection, and performance optimization are not implemented yet.

See `docs/m1-structural-support.md` and `docs/m2-structural-stability.md` for milestone hypotheses and implementation notes.

Scenes can be regenerated from the corresponding `DemolitionLab > Build M* Scene` Unity Editor menu items.
