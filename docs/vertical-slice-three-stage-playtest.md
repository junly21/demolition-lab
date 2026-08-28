# Three-Stage Vertical Slice Playtest

## Intended Difficulty Curve

- Stage01: identify one orange critical support and cause a large collapse in one strong hit or two mediocre hits.
- Stage02: collapse one six-target half, then remove the second independent support path to finish the stage.
- Stage03: avoid wasting ammunition on the thick blue reinforced center and remove the orange side supports.

Difficulty comes from structure layout, redundant support, and target selection. It does not come from uniformly increasing every joint threshold.

## Full Manual Procedure

### Stage01

1. Enter Play Mode in `VerticalSlice_Stage01` and wait 10 seconds; confirm 3 shots, 0% destruction, and no automatic result.
2. Restart and hit the lower orange Weak support with a well-aligned shot.
3. Confirm the hit causes an obvious upper-structure rumble and large collapse.
4. Confirm one strong hit clears or nearly reaches 70%, and a second reasonable hit can finish.
5. Clear and click NEXT; confirm Stage02 loads.

### Stage02

1. Hit one lower orange support.
2. Confirm only its upper half tilts or collapses and the opposite half remains supported.
3. Confirm the first half alone remains below the 70% threshold.
4. Hit the remaining lower support and confirm the second half collapses.
5. Confirm two good shots normally clear, while one miss still leaves a recoverable two-shot solution.
6. Click NEXT and confirm Stage03 loads.

### Stage03

1. Shoot the thick blue reinforced center once with a normal direct hit.
2. Confirm visible impact transfer but that the center usually survives.
3. Retry Stage03.
4. Shoot one lower orange side support and compare the larger local failure.
5. Shoot the opposite side support and confirm the intended side-target strategy produces a large collapse.
6. Confirm the four-shot allowance permits one poor center attempt plus the intended recovery.
7. Clear and confirm NEXT is hidden.

## Tuning Logs

The Console reports stage label, shot number, impacted object, collision impulse, critical joint break type, settled destruction numerator/percentage, clear/fail, and shots used. These logs are for manual calibration only and are not an analytics system.

## Acceptance Question

After playing Stage01 → Stage02 → Stage03, judge whether the sequence feels like one small demolition puzzle game. Automated validation establishes configuration correctness only; it does not establish collapse quality, difficulty, or fun.
