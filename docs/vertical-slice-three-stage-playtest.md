# Five-Stage Vertical Slice Playtest

## Intended Difficulty Curve

- Stage01: identify one orange critical support and cause a large collapse in one strong hit or two mediocre hits.
- Stage02: collapse one six-target half, then remove the second independent support path to finish the stage.
- Stage03: avoid wasting ammunition on the thick blue reinforced center and remove the orange side supports.
- Stage04: read the three lower support paths, create a partial lean, then choose the next support for a cascading tower collapse.
- Stage05: combine previous lessons across a reinforced main column, remote cantilever, and three Weak support decisions.

Difficulty comes from structure layout, redundant support, and target selection. It does not come from uniformly increasing every joint threshold.

## Final Balance Lock

| Stage | Intended Good Clear | Ammo | Recovery Margin |
| --- | --- | ---: | --- |
| Stage01 | 1–2 shots | 3 | Tutorial forgiveness |
| Stage02 | 2 shots | 3 | One recovery shot |
| Stage03 | 2–3 deliberate shots | 4 | One poor center attempt |
| Stage04 | 2–3 strategic shots | 4 | One recovery shot |
| Stage05 | About 3 good shots | 4 | Final shot as recovery |

Stage03 final tuning lowers only the four Weak joint thresholds so a successful first hit leaves a persistent broken support; no rapid-fire window or damage accumulation exists. Stage04 separates successful gameplay determination from result presentation: reaching 70% locks Clear and disables firing, while a stage-specific 2.0-second post-clear delay lets the major collapse remain visible before the panel appears. Stage01, Stage02, and Stage05 retain their existing ammunition and physics calibration.

## Full Manual Procedure

### Impact Feedback Stability

1. Enter each stage, wait 10 seconds without input, and confirm no unexplained dust or camera shake occurs.
2. Fire at the far-right target and confirm dust originates at the actual right-side contact.
3. Repeat Stage01 → Stage02 → Stage03 → Stage04 transitions several times.
4. If an anomaly occurs, enable `logImpactDiagnostics` on the stage controller and inspect the accepted/rejected event line.

Feedback is accepted only for a player-fired projectile launched in the active scene while the stage is `Active` or appropriately `Settling`. The diagnostic line includes scene, frame/time, projectile ID and position, collision object, contact, particle and shake source positions, stage state, player-fired provenance, impulse, and acceptance result.

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
5. Wait and observe without firing immediately; confirm the partial failure remains and aiming stays useful.
6. Shoot the opposite side deliberately and confirm the intended strategy clears in two or three shots.
7. Confirm the four-shot allowance permits one poor center attempt plus the intended recovery.
8. Clear and click NEXT; confirm Stage04 loads.

### Stage04

1. Wait 10 seconds and confirm the tall tower remains idle without impact feedback.
2. Hit one outer lower support well and confirm a partial failure or lean rather than an automatic clear.
3. Choose a second lower support based on the new lean and fire again.
4. Confirm the stacked floors produce the largest cascading collapse in the sequence.
5. Confirm a strong solution normally uses two or three of the four shots.
6. Confirm the stage locks successful at 70% and firing stops while the collapse remains visible.
7. Confirm the result panel appears after the normal 3.25-second settle plus the Stage04-only 2.0-second viewing delay without feeling sluggish.
8. Clear and click NEXT; confirm Stage05 loads.

### Stage05

1. Wait 10 seconds and confirm the asymmetric industrial frame remains fully stable.
2. Hit the reinforced main column and confirm it usually survives.
3. Retry and remove the remote right Weak support.
4. Remove the middle Weak support and confirm the two fallen five-target sections do not trivially clear.
5. Remove the exposed transfer support beside the main column and confirm the final large collapse.
6. Confirm a good solution normally takes three shots, with four available.
7. Retry, choose poor reinforced/decorative targets, and confirm four shots are barely sufficient or fail.
8. Clear and confirm NEXT is hidden.

## Tuning Logs

The Console reports stage label, shot number, collision impulse, critical joint break type, settled destruction numerator/percentage, clear/fail, and shots used. Optional impact diagnostics additionally report the complete accepted/rejected feedback path. These logs are for manual calibration only and are not an analytics system.

## Acceptance Question

After playing Stage01 → Stage02 → Stage03 → Stage04 → Stage05, judge whether the sequence reads at a glance as gate, bridge, bunker, tower, and asymmetric industrial gantry. Automated validation establishes configuration correctness only; it does not establish collapse quality, difficulty, or fun.
