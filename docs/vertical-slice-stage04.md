# Vertical Slice — Stage 04

## Goal

Stage04 is the vertical tower finale. Three lower support paths create an attack-order puzzle, while four stacked floors provide the largest secondary collapse in the current game.

## Configuration

- Physical pieces: 36
- Objective targets: 21
- Critical joints: 4 Weak outer joints and 2 reinforced center joints
- Outer lower joints: 240 N break force, 95 N·m break torque
- Outer upper joints: 300 N break force, 120 N·m break torque
- Center lower joint: 700 N break force, 280 N·m break torque
- Center upper joint: 850 N break force, 340 N·m break torque
- Ammunition: 4 shots
- Clear threshold: 70%
- Settle duration: 3.25 seconds
- Next scene: none

The tower uses three visibly distinct lower load paths, five horizontal floor/roof targets, sixteen wall targets, and four non-target roof pieces. Only the six gameplay-critical support connections use joints; the stacked upper structure remains ordinary Rigidbody geometry. Thin outer supports and exposed plates identify vulnerable paths, while the thicker center support, band, and braces communicate resistance through shape.

## Intended Play

1. A good hit on one outer support should create a visible partial lean or local failure.
2. The other outer path and thicker center path should keep a one-hit failure from automatically clearing the stage.
3. A second strategic support hit should trigger a larger cascading fall through the stacked floors.
4. Four shots allow one imperfect attempt, while a strong solution should normally use two or three.
5. Clear Stage04 and confirm NEXT remains hidden.

Automated validation checks counts, support-path and joint calibration, four-shot configuration, empty next scene, required references, build order, absence of M1–M5 structural systems, and collider separation. Manual Play Mode remains authoritative for lean quality, collapse scale, attack order, and difficulty.
