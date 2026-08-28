# Vertical Slice — Stage 01

## Goal

Stage 01 tests the complete 20–60 second loop: aim, identify a weak support, spend limited ammunition, watch the structure collapse, receive a result, and retry immediately.

## Stage Setup

- Scene: `Assets/Scenes/VerticalSlice_Stage01.unity`
- Physical pieces: 20
- Objective target pieces: 8
- Critical breakable joints: 4 (`2 Weak`, `2 Reinforced`)
- Ammunition: 3 shots
- Clear condition: after at least one accepted shot, at least 70% of target pieces fall, move, or rotate beyond their objective threshold
- Projectile: 2 kg at 18 m/s
- Weak joints: 350 N break force, 140 N·m break torque
- Reinforced joints: 900 N break force, 400 N·m break torque

Orange supports communicate weak connections. Blue supports communicate reinforced connections. The concrete upper platform, walls, and roof form the objective set; brown pieces are non-critical debris.

## Controls

- Aim: `WASD` or arrow keys
- Fire: `Space`
- Retry during play: `R`
- Retry after clear/fail: click `RETRY`

## Flow And Feedback

The reusable controller follows `Initializing -> Ready -> Active -> Settling -> Clear/Failed`. Target reference transforms are explicitly captured during initialization, and targets cannot count until Active gameplay begins. The HUD shows the configured stage label, remaining shots, and live destruction percentage.

After all ammunition is spent or the objective threshold is reached following a shot, input stops for a 2.75-second collapse-settle delay. The result panel then shows clear/fail, shots used, destruction percentage, and Retry. `nextSceneName` is configurable; NEXT stays hidden when it is empty or unavailable. Stage01 intentionally has no next scene configured.

Meaningful projectile impacts trigger a brief camera shake and reusable dust burst. A critical support break changes its support material and triggers stronger feedback. Projectiles destroy themselves 1.25 seconds after their first destructible impact.

## Automated Validation

Use `DemolitionLab > Validate Vertical Slice Stage 01`. It checks piece and target counts, four critical joints and their thresholds, all stage-controller and launcher references, projectile tuning, collider separation, and absence of the M1–M5 structural simulation stack. It also checks initial 0% target evaluation, ammo decrement, Initializing/Ready/Active/Settling/Clear/Failed transitions, fallen-target counting, the 70% threshold, Retry build configuration, and empty-next-scene behavior.

Automated checks establish scene-state correctness only. They do not establish that collapse quality or difficulty feels good.

## Manual Play Mode Validation

1. **A — Idle startup:** Enter Play Mode and wait 10 seconds without input. Confirm destruction remains 0%, shots remain 3, and no result panel appears.
2. **B — One shot:** Press `Space` once. Confirm exactly one projectile appears and ammunition changes from 3 to 2 once.
3. **C — Destruction update:** Hit an objective piece or weak support. Confirm destruction rises only after a target falls, moves, or rotates beyond its threshold.
4. **D — Legitimate clear:** Reach at least 70% after firing. Confirm input stops during settling and `STAGE CLEAR` appears afterward.
5. **E — Exhausted failure:** Use all three shots poorly. Confirm the final projectile gets time to resolve, then `STAGE FAILED` appears.
6. **F — Retry:** Click `RETRY`. Confirm a completely fresh Stage01 loads with 3 shots, 0% destruction, and no result panel.
7. **G — No next stage:** Clear Stage01. Confirm NEXT is absent because `nextSceneName` is empty and Stage02 does not exist.

## Stage Template Configuration

Each future stage configures its label, starting ammunition, target list, success threshold, settle duration, and optional next-scene name on `VerticalSliceStageController`. Weak and reinforced break values remain stage content configured on the four physical joints; they are not hardcoded into the shared runtime controller.

## Stage-Flow Fix

The original controller calculated HUD destruction in `Awake`. Unity does not guarantee `Awake` order between scene objects, so target pieces could still have default reference transforms. Their real world positions then exceeded the displacement threshold and latched as destroyed, causing the first update to enter resolution and disable launcher input. The launcher also skipped aim initialization when the stage controller disabled gameplay before launcher `Awake` ran.

Targets now explicitly establish reference state before Ready, ignore evaluation until Active, and require at least one accepted shot before success evaluation. Launcher aim initialization no longer depends on whether gameplay input is currently enabled.

## Known Validation Gap

Manual Play Mode is still required to verify actual keyboard delivery, projectile spawning, scene reload timing, and gameplay feel. Batch validation cannot prove that collapse thresholds reliably produce a satisfying one- or two-shot clear.
