# Vertical Slice — Stage 03

## Goal

Stage03 teaches target selection. The thick blue center support is visually prominent and resistant, while the orange side supports are the efficient solution.

## Configuration

- Physical pieces: 31
- Objective targets: 16, split evenly across left and right upper halves
- Critical joints: 4 Weak and 2 Reinforced
- Weak lower joints: 240 N break force, 95 N·m break torque
- Weak upper joints: 300 N break force, 120 N·m break torque
- Reinforced lower joint: 1200 N break force, 500 N·m break torque
- Reinforced upper joint: 1400 N break force, 600 N·m break torque
- Ammunition: 4 shots
- Clear threshold: 70%
- Next scene: none

The reinforced center uses thicker blue geometry and visible blue bands. Weak side columns remain orange. Each upper half spans from the center support to one outer Weak support. Removing an outer support leaves that half balanced beyond the center edge so it can tilt and fall without graph or load-simulation logic.

## Manual Validation

1. Shoot the reinforced center once with a normal direct hit.
2. Confirm it shows physical response but usually remains intact.
3. Restart Stage03.
4. Shoot one lower orange side support and confirm that half fails more effectively.
5. Shoot the opposite side support and confirm the intended two-side strategy produces a large collapse and clear.
6. Spend one shot on the center, then confirm the remaining three shots still make recovery possible.
7. Clear Stage03 and confirm NEXT remains hidden.

Automated validation checks counts, the eight/eight target split, four Weak and two Reinforced joint calibrations, four-shot configuration, empty next scene, required references, absence of M1–M5 structural systems, and collider separation. Manual Play Mode remains authoritative for whether the center survives ordinary hits and whether side failures feel decisive.
