# Vertical Slice — Stage 04

## Goal

Stage04 is the large vertical tower stage. Three lower support paths create an attack-order puzzle, while four stacked floors provide a major secondary collapse before the final industrial stage.

## Configuration

- Physical pieces: 36
- Objective targets: 21
- Critical joints: 4 Weak outer joints and 2 reinforced center joints
- Outer lower joints: 200 N break force, 75 N·m break torque
- Outer upper joints: 250 N break force, 95 N·m break torque
- Center lower joint: 700 N break force, 280 N·m break torque
- Center upper joint: 850 N break force, 340 N·m break torque
- Ammunition: 4 shots
- Projectile: 19.8 m/s, 2.3 kg through stage multipliers of 1.10× speed and 1.15× mass
- Clear threshold: 70%
- Settle duration: 3.25 seconds
- Post-clear collapse-view delay: 2.0 seconds
- Next scene: `VerticalSlice_Stage05`

The tower uses three visibly distinct lower load paths, five horizontal floor/roof targets, sixteen wall targets, and four non-target roof pieces. Only the six gameplay-critical support connections use joints; the stacked upper structure remains ordinary Rigidbody geometry. Thin outer supports and exposed plates identify vulnerable paths, while the thicker center support, band, and braces communicate resistance through shape. A baseline breakable lower-support probe measured about 25.20 N·s at 18.02 m/s. The tuned launch raises momentum from 36.00 to 45.54 N·s and kinetic energy from 324 to about 451 J. The final breakable probe reported 19.60 N·s at 19.82 m/s because the weaker joint released during contact, shortening the collision transfer instead of resisting the projectile as a rigid constraint. When destruction reaches 70%, success is locked and firing stops; Stage04 then adds a 2.0-second collapse-view period after the normal settle interval before showing the result panel.

## Intended Play

1. A good hit on one outer support should create a visible partial lean or local failure.
2. The other outer path and thicker center path should keep a one-hit failure from automatically clearing the stage.
3. A second strategic support hit should trigger a larger cascading fall through the stacked floors.
4. Four shots allow one imperfect attempt, while a strong solution should normally use two or three.
5. Confirm success can lock at 70% while the result panel waits long enough to show the continuing collapse.
6. Clear Stage04 and confirm NEXT loads Stage05.

Automated validation checks counts, support-path and joint calibration, four-shot configuration, the Stage04-only 2.0-second post-clear delay, Stage05 progression, required references, build order, absence of M1–M5 structural systems, and collider separation. Manual Play Mode remains authoritative for lean quality, collapse scale, attack order, presentation timing, and difficulty.
