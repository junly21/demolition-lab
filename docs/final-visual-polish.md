# Final Visual Polish

## Material Palette

- Concrete: project-owned low-contrast, non-directional mottle texture; metallic `0`, smoothness `0.12–0.16`, no directional normal map
- Concrete variants: cool light gray, medium cool gray, restrained worn gray, and darker foundation gray
- Steel: lifted charcoal blue-gray, metallic `0.78`, smoothness `0.30`
- Weak steel: neutral worn steel, metallic `0.70`, smoothness `0.23`; orange/yellow remains limited to connector accents
- Reinforced steel: readable dark blue-gray, metallic `0.86`, smoothness `0.34`

Concrete variants are selected deterministically from object names so stages remain reproducible and visually coherent.

## Lighting And Depth

- Key light: warm directional, intensity `1.50`, rotation `(52, -38, 0)`, soft shadows at `0.88` strength
- Fill light: cool directional, intensity `0.48`, rotation `(28, 142, 0)`, no shadows
- SSAO: intensity 0.55, radius 0.35, direct-light contribution 0.20
- Linear fog: start 22 m, end 55 m, desaturated blue-gray
- Ambient light: lifted cool trilight with `1.02` intensity

## Post Processing

- Color Adjustments: exposure `+0.20`, contrast `+4`, saturation `-8`, subtle cool filter
- Tonemapping: ACES
- Bloom: threshold `1.15`, intensity `0.12`, scatter `0.45`, clamp `4`
- Vignette: intensity `0.055`, smoothness `0.32`

The settings are deliberately restrained so the trajectory line and HUD remain crisp.

## Environment And Launcher

- Existing warehouse, container, barrier, pipe, and industrial silhouettes frame the firing lane without entering it.
- Stage04 and Stage05 use larger and more distant backdrop masses to support their scale while keeping the target structure highest contrast.
- The launcher keeps its exact transform and muzzle but uses a charcoal metal housing, orange turntable/muzzle accents, a clearer yaw ring, support legs, breech sleeve, and pivot brackets under presentation-only roots.

## Impact And Collapse Feedback

- Concrete/Foundation hit: 22-particle dust burst at the accepted `ProjectileImpact.ContactPosition`
- Steel/Weak/Reinforced hit: 18 stretched sparks at the same accepted contact position
- Critical joint break: medium camera shake and mixed spark/dust feedback for Weak connections
- Major collapse: one 34-particle ground dust puff after three additional targets become destroyed, throttled to once per 0.8 seconds
- Camera shake: normal hit `0.018–0.055` for `0.12 s`; critical break `0.085–0.11` for `0.20 s`; collapse `0.13` for `0.24 s`

All particles use world simulation, contain no physics components, remain below 100 particles per system, and are cleared on scene initialization.

## Audio Hooks

`PresentationAudioHooks` exposes clips for fire, concrete impact, steel impact, critical break, large collapse, UI click, clear, and fail. No placeholder audio is synthesized; external SFX remain required.

## HUD And Presentation Flow

- The existing Stage, Shots, and Destruction information remains unchanged.
- Stage and ammo use compact typography with short orange baseline accents and no card panels.
- Destruction uses a compact label and a `380 × 6` thin progress line without a backing card.
- Result panels use one restrained translucent test-report surface, a thin orange edge, and lighter outlined buttons.
- Stage05 clear reads `TEST PROGRAM COMPLETE`; its Retry action is presented as `RESTART TEST`.
- `VerticalSlice_Title` uses a dedicated camera, right-side braced industrial frame silhouette, one full-screen translucent overlay, split `DEMOLITION / LAB` typography, and a single `START TEST` action into Stage01.
- The title is inserted directly before Stage01 in build settings without changing Stage01–05 Next-scene progression.

## Screenshot Mode

- Press `F10` during a stage to toggle only the trajectory line.
- Gameplay HUD, physics, input, and stage state remain active and unchanged.

## Gameplay Lock

The visual polish does not change Rigidbody, Collider, FixedJoint, projectile, ammunition, target, success-threshold, balance, or progression settings.
