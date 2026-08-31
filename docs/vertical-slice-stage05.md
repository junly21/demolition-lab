# Vertical Slice — Stage 05

## Goal

Stage05 is the final asymmetric industrial gantry. It combines weak-point recognition, redundant support paths, reinforced-target avoidance, and attack order without introducing another simulation system.

## Configuration

- Physical pieces: 45
- Objective targets: 15, divided into three five-target frame sections
- Critical joints: 10 total — 6 Weak and 4 Reinforced
- Weak lower joints: 300 N break force, 110 N·m break torque
- Weak upper joints: 350 N break force, 140 N·m break torque
- Reinforced main-column joints: 1000 N break force, 420 N·m break torque
- Ammunition: 4 shots
- Projectile: Stage04 configuration, 19.8 m/s and 2.3 kg
- Clear threshold: 70%, requiring 11 of 15 targets
- Settle duration: 3.5 seconds
- Next scene: none

The heavy left foundation and four-piece reinforced main column carry the left frame together with a thin exposed transfer support. Independent Weak supports carry the middle frame and remote right cantilever. The right upper beam extends beyond its lower platform and uses a visible cable/hook silhouette, while double braces and bands make the main column visually inefficient to attack. Only support stacks use FixedJoints; upper frames remain recognizable Rigidbody chunks.

## Intended Three-Shot Solution

1. Remove the remote Weak support to drop or lean the right cantilever section.
2. Remove the middle Weak support to collapse the secondary frame. Ten of fifteen target pieces represent only 66.7%, so these two isolated sections alone cannot guarantee clear.
3. Attack the exposed transfer support beside the reinforced main column. The left frame should lose its second support path and produce the final cascading failure.

An excellent chain reaction may clear in two shots, but the authored target split does not force that result. Direct attacks on the reinforced main column should visibly react while remaining inefficient.

## Manual Validation

1. Wait 10 seconds without input; confirm no sag, joint break, feedback, or automatic clear.
2. Hit the reinforced main support; confirm it reacts but usually survives.
3. Retry, then hit the remote Weak support; confirm a meaningful shift without instant clear.
4. Hit the middle Weak support; confirm major partial collapse remains below or near the threshold.
5. Hit the exposed transfer support; confirm a large final cascading collapse and normal three-shot clear.
6. Deliberately spend shots on reinforced or decorative areas and confirm four shots are not trivially sufficient.
7. Play Stage01 through Stage05 continuously and judge the final industrial silhouette and combined lesson.

Automated validation checks piece/target/joint counts, startup 0%, 10-second idle stability, collider penetration, reference wiring, Weak/Reinforced thresholds, Stage04 progression, hidden Next, projectile configuration, build order, and absence of M1–M5 structural systems. It does not establish gameplay quality.
