# Release Candidate QA

Build under test: `Builds/Release/DemolitionLab-macOS/Demolition Lab.app`

## Title

- [ ] START loads Stage01.
- [ ] Aim, fire, and retry controls are readable.
- [ ] No diagnostic text or debug visualization is visible.

## Stage01–05

Repeat these checks on every stage.

- [ ] WASD and arrow-key aiming moves the trajectory.
- [ ] Space fires exactly one projectile.
- [ ] Ammo decreases exactly once per shot.
- [ ] Destruction percentage starts at 0% and updates after legitimate destruction.
- [ ] Reaching the configured threshold produces Clear after settling.
- [ ] Exhausting ammo below the threshold produces Failed after settling.
- [ ] Retry reloads a fresh copy of the current stage after failure.
- [ ] Next follows Stage01 → Stage02 → Stage03 → Stage04 → Stage05.
- [ ] Projectile impacts appear at the collision point.
- [ ] Assigned SFX play without clipping or runaway overlap.
- [ ] Collapse feedback is restrained and does not obscure the structure.
- [ ] Camera feedback is short and remains readable.

## Final

- [ ] Stage05 success displays `TEST PROGRAM COMPLETE`.
- [ ] Stage05 restart returns to the Title scene.
- [ ] Stage05 failure retry reloads Stage05 instead of returning to Title.
- [ ] No audio, particles, projectiles, or camera shake persist after a scene change.

## Release

- [ ] Standalone launches without the Unity Editor.
- [ ] Window/output is 16:9 with unclipped HUD at 1920×1080.
- [ ] No materials or shaders are missing or pink.
- [ ] No visible debug data or development-only Gizmos appear.
- [ ] No required serialized references are missing.
- [ ] A complete Title → Stage05 → Title run succeeds.
- [ ] Player log contains no exceptions or repeated error spam.

## Known Asset Check

The release contains audio hooks, but no AudioClip assets are currently assigned for cannon
fire, concrete impact, metal impact, critical break, collapse, UI click, clear, or fail. Verify
the accepted silent presentation intentionally, or import and credit suitable clips before
checking the SFX items above.

## Automated Preflight

- [x] Release scene list contains only Title and Stage01–05 in progression order.
- [x] Product name, version, and 1920×1080 defaults are configured.
- [x] Stage01–04 Next destinations and Stage05 completion destination are serialized.
- [x] Verbose impact/initial-state diagnostics are disabled in Stage01–05.
- [x] Existing M1–M5 and final portfolio validators pass.
- [x] Locked Rigidbody, Collider, FixedJoint, projectile, ammo, target, threshold, and progression values are unchanged except for the explicit Stage05 completion destination.

