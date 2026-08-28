# DemolitionLab

**Current build:** First playable vertical slice — Stage 01

DemolitionLab is a short demolition puzzle where three carefully aimed shots bring down a readable structure.

## Controls

- **Aim:** `WASD` or arrow keys
- **Fire:** `Space`
- **Reset scene:** `R`
- **Retry after result:** click `RETRY`

## Scenes

- `VerticalSlice_Stage01`: final-game prototype with three shots, a destruction objective, HUD, result flow, and four critical support joints
- `M0_DemolitionLab`: baseline stacked-block scene using only Rigidbody physics
- `M1_StructuralGraph`: support-graph scene with two anchored pillars, a bridge, and a non-critical upper block
- `M2_StructuralStability`: side-view structure that adds center-of-mass stability to graph connectivity
- `M3_LocalSupport`: M2.1 structure with geometric vertical support and bounded cantilever validation
- `M4_BreakableConnections`: side-by-side weak and reinforced structures with finite connection strength
- `M5_PhysicalConnections`: 45-piece dynamic frame with weak and reinforced physical joints

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

M2.1 normalizes the test structure to touching, non-overlapping Collider geometry and uses density-consistent masses: `1 kg` unit blocks and `3 kg` bases with three times the volume. Released blocks leave the static BFS/COM model immediately, while continuing under Rigidbody physics.

## M3 Local Support

M3 keeps the global graph and COM decision, then validates blocks in each globally stable component from its ground anchors. Actual vertical contacts preserve the current cantilever span, while authored horizontal neighbors with touching Collider faces add one block to the span. Blocks beyond the configured `maxCantileverBlocks` limit never enter the locally supported set and are released with any blocks that depend on them.

Local releases trigger another global and local evaluation of the remaining static structure. This continues until no additional block loses support. Released blocks never re-enter the static model.

## M4 Breakable Connections

M4 replaces implicit infinite-strength graph edges with authored `StructuralConnection` components. M4.1 separates gravity and impact capacities, normalizes each demand, and breaks a connection when their dimensionless utilization sum exceeds one.

The M4 scene compares a weak single-path structure with a reinforced structure containing an additional physical brace and alternate connection path. It contains 23 blocks and 25 explicit connections.

## M5 Physical Connections

M5 keeps the logical graph and support evaluators but adds a `ConfigurableJoint` to every active test-scene connection. Four ground anchors remain kinematic; the other 41 blocks are dynamic from startup so collision force and torque can propagate through the frame. Unity joint failure is authoritative in M5 and immediately synchronizes the logical connection before graph, COM, and local-support reevaluation.

## Debugging

- Supported blocks use the green M1 material.
- Unsupported dynamic blocks use the red M1 material.
- Destroyed blocks are hidden; their former bounds and broken graph edges are gray Gizmos in Scene view.
- Active supported graph edges are green Gizmos and unsupported edges are red.
- Recalculation logs include the destroyed node ID, visited/supported count, and unsupported count.
- M2 displays each component COM as a Gizmo sphere and its support interval as a ground line.
- M2 graph/state colors are green for stable, yellow for unstable, red for disconnected, and gray for destroyed debris.
- M3 draws cyan wireframes and links for locally supported blocks, blue links for cantilever paths, and magenta wireframes for locally unsupported blocks selected for release.
- M4 connection Gizmos move from green toward red as load approaches strength, use magenta for broken edges, and gray for connections whose blocks have left the static model.
- M5 physical joints use orange for Weak, cyan for Reinforced, magenta for broken, and gray for released endpoints.

## M1 Manual Test Scenarios

- **A — Non-critical node:** Destroy `Roof_Cap`; the remaining supported structure should stay stable.
- **B — Redundant support:** Destroy `Left_Base`; all remaining blocks should still reach `Right_Base` through the bridge.
- **C — Final support paths:** Destroy both `Left_Base` and `Right_Base`; all remaining active nodes should become dynamic.
- **D — Split components:** Destroy `Bridge_Center`; both anchored sides should remain stable while the disconnected `Roof_Cap` falls.

## M2 Manual Test Scenarios

- **A — Both supports:** The intact structure should remain connected and stable.
- **B — Upper block:** Removing `Roof_Right` should remain stable if COM stays in the support interval.
- **C — Left support:** The connected overhang should become unstable and topple after the whole component is released.
- **D — Right support:** The wider left footprint should keep the remaining COM supported without extreme mass weighting.
- **E — No supports:** Removing both anchors should release the remaining disconnected component regardless of COM.

## M3 Manual Test Scenarios

- **A — Both supports:** All intended blocks should remain globally and locally supported.
- **B — Left support:** The globally unstable structure should release as in M2.1.
- **C — Right support:** The global component should pass COM, but unsupported right-side blocks should release locally.
- **D — Short cantilever:** One- and two-block extensions should remain static with the default limit of two.
- **E — Excessive cantilever:** The third horizontal block and its unsupported dependents should release.
- **F — Middle support:** Destroying `Right_Mid` should release `Right_Top` and the excessive overhang while preserving valid static blocks.

## M4 Manual Test Scenarios

- **A — Symmetric support:** Both weak and reinforced structures should remain static before impact.
- **B — Weak support joint:** A strong hit near the weak right joint should break it and trigger reevaluation.
- **C — Long cantilever:** The newly single-supported weak span should overload additional carrying connections.
- **D — Distant impact:** A roof hit should break only directly incident weak connections, not the remote base joint.
- **E — Reinforced path:** The weak primary joint may break while the strong brace path preserves the structure.
- **F — Debug evidence:** Inspector load values and connection Gizmos should clearly show active, loaded, and broken states.

## Current Scope

The vertical slice deliberately does not use the M1–M5 graph, COM, local-support, or connection-load simulation. Stage 01 uses Rigidbody debris plus four gameplay-critical `FixedJoint` connections so intact supports remain readable and failure can produce a decisive collapse.

See `docs/vertical-slice-stage01.md` and `docs/vertical-slice-reuse.md` for the final-game scope and reuse decisions. M0–M5 remain as preserved R&D scenes and documentation.

R&D scenes and Stage 01 can be regenerated from their corresponding `DemolitionLab` Unity Editor menu items.
