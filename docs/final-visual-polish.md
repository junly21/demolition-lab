# Final Visual Polish

## Material Palette

- Clean concrete: cool light gray, low metallic, 0.32 normal strength
- Dark concrete: medium cool gray, 0.28 normal strength
- Worn concrete: slightly warm gray, 0.38 normal strength
- Foundation concrete: darker neutral gray, 0.36 normal strength
- Steel: dark blue-charcoal, 0.82 metallic, 0.32 smoothness
- Weak steel: dark worn steel, 0.72 metallic, 0.24 smoothness; orange/yellow is limited to connector accents
- Reinforced steel: deep blue steel, 0.90 metallic, 0.38 smoothness

Concrete variants are selected deterministically from object names so stages remain reproducible and visually coherent.

## Lighting And Depth

- Key light: warm directional, intensity 1.55, rotation `(52, -38, 0)`, soft shadows at 0.88 strength
- Fill light: cool directional, intensity 0.26, rotation `(28, 142, 0)`, no shadows
- SSAO: intensity 0.55, radius 0.35, direct-light contribution 0.20
- Linear fog: start 22 m, end 55 m, desaturated blue-gray
- Ambient light: cool trilight with 0.82 intensity

## Post Processing

- Color Adjustments: exposure `+0.08`, contrast `+12`, saturation `-8`, subtle cool filter
- Tonemapping: ACES
- Bloom: threshold `1.15`, intensity `0.12`, scatter `0.45`, clamp `4`
- Vignette: intensity `0.11`, smoothness `0.38`

The settings are deliberately restrained so the trajectory line and HUD remain crisp.

## Gameplay Lock

The visual polish does not change Rigidbody, Collider, FixedJoint, projectile, ammunition, target, success-threshold, balance, or progression settings.
