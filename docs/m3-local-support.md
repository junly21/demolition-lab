# M3 Local Support Development Note

## Observed M2.1 Limitation

M2.1 manual testing confirmed that left-base removal produced a reasonable global collapse. Right-base removal exposed a different limitation: the remaining component passed the global COM test, so blocks above the missing support could remain frozen despite having empty space beneath them.

## Conclusion

**Global stability does not guarantee local support.**

The global COM rule answers whether the aggregate component mass lies over its remaining support footprint. It does not prove that every individual block has a plausible geometric load path to that footprint.

## Evaluation Order

M3 preserves the M2.1 decision order:

1. Build active connected components from the authored graph.
2. Release any component with no active ground anchor.
3. Apply the mass-weighted COM and support-interval test.
4. Release the entire component if global stability fails.
5. Run local support validation only for globally stable components.
6. Release locally unsupported subsets and evaluate the remaining static structure again.

Released and destroyed blocks do not participate in later graph, COM, anchor, or local support evaluation.

## Vertical Support Rule

Ground anchors begin as local support roots with cantilever span zero.

A graph neighbor is directly supported from below when:

- its Collider center is above the supporting block,
- its bottom Y face is within `contactTolerance` of the lower block's top Y face,
- the two Collider bounds overlap in X and Z by more than the tolerance.

Vertical propagation preserves the current horizontal span. A logical graph edge without matching geometric contact does not provide vertical support.

## Cantilever Rule

Horizontal support requires an authored graph edge and Collider faces that touch along X while overlapping in Y and Z.

Each horizontal step adds one to the support path's cantilever span. A block is locally supported when the shortest discovered path from any active ground anchor has:

`horizontalSteps <= maxCantileverBlocks`

The M3 scene uses `maxCantileverBlocks = 2`. One- and two-block extensions remain static; a third horizontal step does not enter the supported set.

## Propagation

`LocalSupportEvaluator` performs queue-based traversal from all ground anchors. Only blocks reached through valid upward or horizontal geometric contacts are marked supported. Unsupported blocks are therefore never used as intermediate support for other blocks.

After releasing the unsupported set, `StructureManager` repeats global component, COM, and local evaluation. This handles secondary disconnection or COM changes without running a heavy per-frame analysis.

## Debug Visualization

- Green COM/support Gizmos: globally stable component
- Cyan wireframe and link: locally supported block or vertical support path
- Blue link: horizontal cantilever support path
- Magenta wireframe: locally unsupported block selected for release
- Yellow material/graph state: released unstable Rigidbody
- Red graph state: graph-disconnected Rigidbody
- Gray material: destroyed debris

The Inspector exposes `maxCantileverBlocks` and `contactTolerance` on `LocalSupportEvaluator`.

## Expected Manual Scenarios

- **A — Both supports intact:** every intended block remains globally and locally supported.
- **B — Left base removed:** global COM fails and the full remaining component releases before local evaluation matters.
- **C — Right base removed:** global COM passes, but `Right_Mid`, `Right_Top`, and the excessive right overhang release locally.
- **D — Short cantilever:** the first and second horizontal blocks remain supported.
- **E — Excessive cantilever:** blocks beyond the two-block limit release with unsupported dependents.
- **F — Middle support removed:** a block above the missing support releases unless another valid bounded path reaches it.

These are expected outcomes and automated state checks, not completed manual Play Mode results.

## Simplifications

- This is a discrete geometric support rule, not structural engineering.
- There is no stress, strength, bending, torque-capacity, or material model.
- Support paths use authored graph neighbors plus axis-aligned Collider contact tests.
- Cantilever cost counts horizontal blocks rather than physical bending moment.
- Released blocks do not reconnect and dynamic contacts do not rebuild the graph.
- Global stability remains a one-dimensional X-axis support-interval test.
