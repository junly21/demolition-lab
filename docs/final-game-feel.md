# Final Game Feel

## Fire Feedback

- Player fire emits one short muzzle flash and 10 smoke particles at the existing launcher muzzle.
- Fire adds a camera impulse of `0.022` for `0.08 s` without moving the launcher or changing projectile creation.
- The fire audio hook is spatial and mixed at `0.85` relative volume.

## Material Impact Mapping

- Concrete/Foundation: 18 dust particles, six tiny debris particles, and the concrete-impact audio hook.
- Steel/Reinforced: 14 sparks, three tiny debris particles, and the metal-impact audio hook.
- Weak: 18 sparks, five dust particles, four debris particles, and a slightly stronger metal-impact mix.
- Every projectile effect uses the accepted `ProjectileImpact.ContactPosition`; no cached transform position is used.

## Critical Break Feedback

- Critical breaks emit local mixed sparks, dust, and debris with `0.085–0.10` camera shake for `0.18 s`.
- If a break occurs within `0.18 s` and `0.8 m` of the accepted projectile impact, the additional spark count is reduced to avoid a duplicate-looking burst.
- The structural-break audio hook is spatial and mixed at `0.78`.

## Structural Ground Impacts

- Only target pieces report direct contacts with the authored `Ground` object.
- Contacts below `5 N·s` are ignored.
- Each piece has a `0.35 s` cooldown and the stage has a `0.18 s` global feedback cooldown.
- Medium contacts emit 12 dust particles; contacts at or above `14 N·s` emit 22 dust particles and a `0.045` camera impulse for `0.12 s`.

## Major Collapse

- A major collapse is recognized when at least three target pieces become destroyed within a rolling `1.35 s` window.
- Major-collapse feedback has a `1.2 s` cooldown, emits 38 dust particles near the average destroyed-piece position, and uses `0.12` camera shake for `0.22 s`.
- This is presentation-only observation of existing stage state; it does not alter destruction evaluation or physics.

## Camera Shake Levels

- Fire recoil: `0.022` for `0.08 s`.
- Normal projectile impact: impulse-scaled `0.018–0.055` for `0.12 s`.
- Critical joint break: `0.085` for Weak or `0.10` for Reinforced, for `0.18 s`.
- Large structural ground impact: `0.045` for `0.12 s`.
- Major collapse: `0.12` for `0.22 s`.

## Audio Hooks And Mix

- World source: spatial blend `1`, linear rolloff, minimum distance `3 m`, maximum distance `28 m`, Doppler disabled.
- UI/result source: non-spatial.
- Relative levels: fire `0.85`, impact `0.62`, critical break `0.78`, collapse `0.90`, UI `0.42`, clear/fail `0.68`.
- No AudioMixer was added because no audio clips are currently imported and the two-source split is sufficient for this prototype.

Required external clips remain unassigned:

- cannon fire
- concrete impact
- metal impact
- joint/metal break
- heavy collapse
- UI click
- clear
- fail

No placeholder audio was synthesized.

## External SFX Credits

No external SFX assets are currently imported or used. Add future asset names, authors,
sources, and licenses to `docs/third-party-assets.md` when the eight hooks above are assigned.

## Gameplay Lock

Rigidbody, Collider, FixedJoint, projectile, ammunition, success-threshold, target, progression, balance, material, HUD, and title configuration remain unchanged in this pass.
