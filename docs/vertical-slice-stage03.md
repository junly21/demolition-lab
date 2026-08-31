# Vertical Slice — Stage 03

## Goal

Stage03 teaches target selection through a low, wide bunker silhouette. The thick blue center support is visually prominent and resistant, while the thin orange side supports are the efficient solution.

## Configuration

- Physical pieces: 31
- Objective targets: 16, split evenly across left and right upper halves
- Critical joints: 4 Weak and 2 Reinforced
- Weak lower joints: 210 N break force, 80 N·m break torque
- Weak upper joints: 260 N break force, 100 N·m break torque
- Reinforced lower joint: 1200 N break force, 500 N·m break torque
- Reinforced upper joint: 1400 N break force, 600 N·m break torque
- Ammunition: 4 shots
- Clear threshold: 70%
- Next scene: `VerticalSlice_Stage04`

The reinforced center has a wider foundation, substantially thicker geometry, visible bands, and two diagonal braces. Weak side columns remain much thinner but are slightly wider than the first bunker pass, with larger exposed plates and moderately lower break thresholds so a good intended hit registers more reliably. Low armor blocks and broad roof slabs keep the structure squat. Each half still spans from the center support to one outer Weak support, preserving the existing side-target lesson without graph or load-simulation logic.

## Manual Validation

1. Shoot the reinforced center once with a normal direct hit.
2. Confirm it shows physical response but usually remains intact.
3. Restart Stage03.
4. Shoot one lower orange side support and confirm that half fails more effectively.
5. Shoot the opposite side support and confirm the intended two-side strategy produces a large collapse and clear.
6. Spend one shot on the center, then confirm the remaining three shots still make recovery possible.
7. Clear Stage03 and confirm NEXT loads Stage04.

Automated validation checks counts, the eight/eight target split, four Weak and two Reinforced joint calibrations, four-shot configuration, Stage04 progression, required references, absence of M1–M5 structural systems, and collider separation. Manual Play Mode remains authoritative for whether the center survives ordinary hits and whether side failures feel decisive.
