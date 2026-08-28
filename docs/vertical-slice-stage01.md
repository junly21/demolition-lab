# Vertical Slice — Stage 01

## Goal

Stage 01 tests the complete 20–60 second loop: aim, identify a weak support, spend limited ammunition, watch the structure collapse, receive a result, and retry immediately.

## Stage Setup

- Scene: `Assets/Scenes/VerticalSlice_Stage01.unity`
- Physical pieces: 20
- Objective target pieces: 8
- Critical breakable joints: 4 (`2 Weak`, `2 Reinforced`)
- Ammunition: 3 shots
- Clear condition: at least 70% of target pieces fall, move, or rotate beyond their objective threshold
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

The HUD shows the stage number, remaining shots, and live destruction percentage. After all ammunition is spent or the objective threshold is reached, input stops for a 2.75-second collapse-settle delay. The result panel then shows clear/fail, shots used, destruction percentage, Retry, and a Stage 02 placeholder on success.

Meaningful projectile impacts trigger a brief camera shake and reusable dust burst. A critical support break changes its support material and triggers stronger feedback. Projectiles destroy themselves 1.25 seconds after their first destructible impact.

## Automated Validation

Use `DemolitionLab > Validate Vertical Slice Stage 01`. It checks piece and target counts, four critical joints and their thresholds, all stage-controller and launcher references, projectile tuning, collider separation, and absence of the M1–M5 structural simulation stack.

Automated checks establish scene-state correctness only. They do not establish that collapse quality or difficulty feels good.

## Manual Play Mode Validation

1. **A — Good clear:** Aim at an orange critical support. Confirm one or two well-placed hits can reach at least 70% destruction and show `STAGE CLEAR`.
2. **B — Miss:** Fire away from the structure. Confirm one shot is consumed and the HUD updates.
3. **C — Failure:** Spend all three shots poorly. Confirm input stops, the collapse settles, and `STAGE FAILED` shows the final percentage.
4. **D — Retry:** Click `RETRY` after either result. Confirm the scene, ammunition, targets, and HUD reset immediately.
5. **E — Critical collapse:** Hit a weak lower or upper support well. Confirm local impact transfers into a large upper-structure collapse while the blue side is more resistant.
6. **F — No sticky structures:** Observe fallen pieces. Confirm there are no impossible hanging clusters, rubber-chain motion, or unrelated remote joint failures.

## Known Validation Gap

Manual Play Mode is still required to judge whether the support thresholds reliably produce a satisfying one- or two-shot clear, whether the weakness is readable from the game camera, and whether retry encourages another attempt. No gameplay-feel claim is made from batch validation.
