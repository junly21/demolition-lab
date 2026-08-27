# M1 Structural Support Development Note

## Problem Observed in M0

M0 relies entirely on stacked dynamic Rigidbodies. Manual testing showed that repeated impacts can push lower blocks far enough for upper blocks to collapse, but the result depends on friction, accumulated displacement, and incidental contacts. The scene cannot explain whether a block is logically supported; it only exposes the final physical motion.

## Hypothesis

A small explicit support graph should make destruction behavior more intentional and explainable. If each structural block is a node, neighbor relationships are edges, and base nodes are ground anchors, reachability from those anchors can provide a clear binary support decision.

## M1 Approach

- Store configured neighboring `StructuralBlock` references on each node.
- Build an undirected adjacency list in `StructureManager` at scene start.
- Start every active structural Rigidbody as kinematic.
- Treat a projectile hit as binary structural removal of the directly hit node.
- Run BFS from all remaining active ground anchors after initialization and each node removal.
- Keep reached nodes kinematic and change unreached active nodes to dynamic Rigidbodies.
- Let Unity Physics handle falling and collision after the graph releases a block.
- Use materials, Gizmos, and concise traversal logs to expose the graph decision.

## What We Expect to Learn

- Whether support-path removal produces collapses that are easier to predict than M0.
- Whether redundant paths prevent unnecessary collapse when one support is removed.
- Whether static neighbor configuration is understandable enough for small authored structures.
- Whether the boundary between logical support and physical motion is useful for later milestones.

These are hypotheses, not measured results. Manual testing is required before drawing conclusions.

## Intentionally Out of Scope

- Hit points, damage thresholds, damage types, or partial failure
- Edge-specific damage or connection strength
- Joints, constraints, mesh fracture, or procedural geometry
- Runtime graph creation from physical contacts
- Structural reconnection after blocks move
- Object pooling, save/load, multiplayer, ECS, or DOTS
- Performance optimization for large structures
- Claims about gameplay quality before M1 manual verification
