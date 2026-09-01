# DemolitionLab Portfolio Facts

## Project

- Unity version: Unity `6000.5.10f1` (Unity 6.5).
- Render pipeline: Universal Render Pipeline (URP).
- Runtime language: C#.
- Release product name: `Demolition Lab`.
- Release version: `1.0.0-portfolio`.
- Playable stage count: five, preceded by one Title scene.
- Release branch: `vertical-slice`.
- Release-candidate commit: branch `HEAD`, subject `chore: prepare portfolio release candidate`.

## Core Loop

The player aims a launcher, spends limited ammunition, fires Rigidbody projectiles, targets
readable Weak supports, observes physics-driven collapse, and clears a stage by reaching its
configured target-destruction percentage. A failed stage can be retried, successful Stage01–04
results expose Next, and successful Stage05 completion returns to the Title screen through the
restart action.

## Final Physical Architecture

- Structural physical roots own Rigidbody, BoxCollider, gameplay-critical FixedJoint, and optional `StageTargetPiece` components.
- Replaceable `VisualRoot` children own renderer-only presentation geometry and do not alter mass, collision dimensions, joint anchors, or target evaluation.
- Projectiles use Rigidbody launch velocity and record Unity collision impulse/contact data.
- Stage structures use ordinary Rigidbody collision plus a small authored set of Weak/Reinforced FixedJoints; the final stages do not run the M1–M5 research simulation stack.
- Target evaluation begins only after explicit reference-state initialization and counts configured displacement, fall, rotation, or destruction conditions after gameplay starts.

## Stage Concepts

- Stage01: introductory support choice with eight targets, two Weak and two Reinforced critical joints, and three shots.
- Stage02: long bridge with two independently supported target halves and three shots.
- Stage03: low reinforced bunker where thin Weak side supports are more efficient than the reinforced center; four shots.
- Stage04: tall tower with three lower support paths, 21 targets, four shots, and an additional two-second collapse-view delay after clear.
- Stage05: asymmetric industrial gantry with three five-target sections, six Weak and four Reinforced critical joints, four shots, and no Next scene.
- Every stage uses a 70% clear threshold; Stage05 requires 11 of 15 targets.

## Technical Iterations

- M0: baseline Rigidbody demolition prototype with launcher, projectile lifetime, collision response, and scene reset.
- M1: authored structural graph and breadth-first ground-anchor connectivity.
- M2/M2.1: component center-of-mass stability, density-consistent masses, contact-gap validation, and cleaner physics handoff.
- M3: local vertical support and bounded cantilever evaluation.
- M4/M4.1: breakable logical connections and dimensionless gravity/impact utilization.
- M5/M5.1: physical joint force transmission, stiffness calibration, break-threshold calibration, and logical/physical break synchronization in the research scene.
- Final vertical slice: retained the lessons but deliberately uses standard Rigidbody, Collider, and FixedJoint stage content instead of the research graph/COM/load stack.

## Measured Debugging Examples

- M2.1 removed a previous `20:1` base-to-block mass bias and used `1 kg` unit blocks with `3 kg` bases matching their three-times volume.
- M4 investigation measured normal Weak direct-hit impulses around `29.7–35.2 N·s`; the old automated input of `60 N·s` was not representative.
- M4.1 normalized `gravityLoad / gravityCapacity` and `impactImpulse / impactCapacity` instead of adding newtons directly to newton-seconds.
- M5.1 manual data ranged up to `17.55 N·s`, with a `5.5 N·s` median and useful direct hits around `12–17.5 N·s`; this informed lower physical joint thresholds without increasing the `2 kg`, `18 m/s` baseline projectile.
- Stage04's launch multipliers produce a configured `19.8 m/s`, `2.3 kg` projectile; a final breakable probe reported `19.60 N·s` at `19.82 m/s`.

## Third-Party Assets

- In-use environment/prop assets: Kenney City Kit (Industrial) and Kenney Factory Kit, both CC0.
- In-use ground material source: Poly Haven Asphalt 01, CC0.
- Poly Haven Concrete is imported but not assigned to final structural pieces.
- Full attribution and modification records: `docs/third-party-assets.md`.
- No third-party audio assets are currently used.

## Evidence Locations

- Stage documentation: `docs/vertical-slice-stage01.md` through `docs/vertical-slice-stage05.md`.
- Structural research documentation: `docs/m1-structural-support.md`, `docs/m2-structural-stability.md`, `docs/m3-local-support.md`, `docs/m4-breakable-connections.md`, and `docs/m5-physical-connections.md`.
- Presentation asset attribution: `docs/third-party-assets.md`.
- Release QA checklist: `docs/release-qa.md`.
