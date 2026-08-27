# DemolitionLab

**Current milestone:** M1

DemolitionLab is a small Unity prototype for comparing pure physics demolition with explicit graph-based structural support.

## Controls

- **Aim:** `WASD` or arrow keys
- **Fire:** `Space`
- **Reset scene:** `R`

## Scenes

- `M0_DemolitionLab`: baseline stacked-block scene using only Rigidbody physics
- `M1_StructuralGraph`: support-graph scene with two anchored pillars, a bridge, and a non-critical upper block

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

## Debugging

- Supported blocks use the green M1 material.
- Unsupported dynamic blocks use the red M1 material.
- Destroyed blocks are hidden; their former bounds and broken graph edges are gray Gizmos in Scene view.
- Active supported graph edges are green Gizmos and unsupported edges are red.
- Recalculation logs include the destroyed node ID, visited/supported count, and unsupported count.

## Manual Test Scenarios

- **A — Non-critical node:** Destroy `Roof_Cap`; the remaining supported structure should stay stable.
- **B — Redundant support:** Destroy `Left_Base`; all remaining blocks should still reach `Right_Base` through the bridge.
- **C — Final support paths:** Destroy both `Left_Base` and `Right_Base`; all remaining active nodes should become dynamic.
- **D — Split components:** Destroy `Bridge_Center`; both anchored sides should remain stable while the disconnected `Roof_Cap` falls.

## Current Scope

M1 intentionally uses a small static graph, binary node destruction, BFS traversal, and standard Rigidbody simulation. Structural edge damage, HP, joints, fracture, reconnection, and performance optimization are not implemented yet.

See `docs/m1-structural-support.md` for the development hypothesis and intended learning goals.

Scenes can be regenerated from `DemolitionLab > Build M0 Scene` and `DemolitionLab > Build M1 Scene` in the Unity Editor.
