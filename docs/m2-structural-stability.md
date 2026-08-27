# M2 Structural Stability Development Note

## Problem Observed in M1

M1 manual connectivity scenarios passed, but an upside-down U structure exposed a modeling failure. After removing the left support, the remaining L-shaped structure still had a graph path to the right ground anchor, so BFS kept every remaining block kinematic. The structure was connected but had an obvious overhang that should be able to rotate or topple.

## Conclusion

**Connected does not necessarily mean stable.**

Graph reachability answers whether a structural path to ground exists. It does not determine whether the component's mass can be supported by the remaining ground footprint.

## Hypothesis

A mass-weighted center of mass compared against a simple horizontal support interval should identify obvious overhangs while preserving M1 connectivity as the first structural test.

## Implementation

### Connectivity

`StructureManager` uses BFS over active graph edges to identify every active connected component. A component is graph-connected to ground when it contains at least one active ground-anchor node. A component without an anchor is released immediately.

### Center of Mass

For each active component, `StructuralStabilityEvaluator` uses each block's Rigidbody mass and world position:

`COM = Σ(position_i × mass_i) / Σ(mass_i)`

The M2 stability decision uses `COM.x`; the full world-space vector is retained for Gizmo visualization.

### Support Interval

For every active ground anchor in the component, the evaluator reads its Collider world bounds. The horizontal support interval is:

`supportMinX = min(anchor.collider.bounds.min.x)`

`supportMaxX = max(anchor.collider.bounds.max.x)`

Collider half-widths are therefore included automatically rather than treating anchor pivots as point supports.

### Stability Rule

With Inspector-configurable tolerance `t`:

`stable = connected && COM.x >= supportMinX - t && COM.x <= supportMaxX + t`

The default tolerance is `0.05` world units.

### Physics Handoff

- Stable connected components remain kinematic.
- Connected but unstable components are released together with `isKinematic = false`.
- Disconnected components are released together regardless of COM.
- Once released, a component is not automatically frozen or reconnected in M2.
- The directly hit node is removed from graph evaluation but remains visible as dynamic gray debris with an impulse based on projectile mass and impact velocity.

### Debug Evidence

- Green: stable supported state
- Yellow: unstable released state
- Red: graph-disconnected state
- Gray: structurally destroyed dynamic debris
- Gizmo sphere: calculated component COM
- Ground line: active support interval
- Logs: representative node, node count, total mass, COM.x, support interval, connectivity, stability, and release count

The visualization is intended to support later portfolio screenshots. No screenshots or manual results are included yet.

## Expected Manual Scenarios

- **A — Both supports intact:** connected, COM inside the combined support interval, stable.
- **B — Non-critical upper block removed:** remains stable if COM stays inside the interval.
- **C — Left support removed:** still connected to the right anchor, but COM outside the right footprint; release the entire active component.
- **D — Right support removed:** the deliberately heavier left side keeps COM inside the left footprint; remain stable.
- **E — Both supports removed:** no ground-connected component; release regardless of COM.

These are expected outcomes, not manual test results.

## Known Simplifications

- Stability is evaluated only along world X in a primarily X/Y side-view scene.
- The support region is a one-dimensional interval, not a 2D convex hull or 3D polygon.
- Rigidbody positions are used as block mass points; custom Rigidbody center-of-mass offsets are not modeled.
- The graph has binary nodes and static authored edges with no strength or stress model.
- There is no fracture, joint simulation, torque solver, or finite element analysis.
- Released blocks do not reconnect and stability is not continuously recalculated from moving geometry.
- No performance optimization or scalability claim is attempted.
