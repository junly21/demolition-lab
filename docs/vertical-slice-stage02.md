# Vertical Slice — Stage 02

## Goal

Stage02 teaches redundant support through layout rather than a new structural algorithm. The upper structure is split into two independently supported halves.

## Configuration

- Physical pieces: 24
- Objective targets: 12, split evenly across left and right halves
- Critical joints: 4 Weak joints
- Lower joints: 240 N break force, 95 N·m break torque
- Upper joints: 300 N break force, 120 N·m break torque
- Ammunition: 3 shots
- Clear threshold: 70%
- Next scene: `VerticalSlice_Stage03`

Each side contains one foundation, a two-block orange Weak column, an upper platform half, four wall targets, one roof target, and two decorative roof pieces. Destroying one side can remove approximately half of the objective but should not clear the stage. Destroying the second support should complete the collapse.

## Manual Validation

1. Hit the first lower Weak support well.
2. Confirm its half visibly tilts or collapses while the opposite half remains meaningfully supported.
3. Confirm destruction remains below 70% after only that half fails.
4. Hit the remaining lower Weak support.
5. Confirm the second half collapses and the stage clears in two good shots.
6. Miss once and confirm the remaining two shots still permit the intended solution.

Automated validation checks scene counts, the six/six target split, joint thresholds, controller configuration, Stage03 progression, and collider separation. Manual Play Mode remains authoritative for partial-collapse quality.
