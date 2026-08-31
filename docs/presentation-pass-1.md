# Presentation Pass 1

## Art Direction

The five-stage vertical slice uses a clean, slightly stylized industrial demolition test range. Prototype geometry remains intentional and readable rather than photorealistic or asset-heavy. Every stage shares the same yard, lighting, material categories, launcher treatment, and restrained HUD cleanup while preserving its original silhouette.

## Environment

- Rough concrete demolition pad using a dedicated presentation material
- Low steel perimeter barriers and a hazard-striped rear wall
- Simple warehouse, container, and distant industrial silhouettes
- Painted firing line and lane markings
- Procedural blue-gray sky, linear distance fog, and warm directional sunlight
- All added environment geometry is visual-only and has no Collider or Rigidbody

## Material Categories

- `StructuralConcrete`: neutral rough concrete for slabs and beams
- `WeakSteel`: worn orange industrial steel for vulnerable supports
- `ReinforcedSteel`: dark blue-gray heavy steel for reinforced supports
- `BarrierSteel` / `UtilitySteel`: dark metal for braces, yard barriers, and secondary details
- `YardConcrete`: darker rough demolition-pad surface
- `LauncherMetal` / `LauncherAccent`: dark metallic housing with a restrained orange accent

## Connection Language

Weak connections use exposed front plates, two visible bolts, and narrow yellow hazard straps. Reinforced connections use thicker upper/lower collars and crossed front braces. These are child render meshes without physics components, so silhouette and construction communicate strength in addition to color.

## Launcher Presentation

The existing launcher transforms and mechanics are unchanged. A compact base housing, turntable, yaw ring, four support legs, breech sleeve, muzzle collar, and pivot brackets make it read as a demolition-range cannon. All additions are non-colliding child visuals, and the trajectory LineRenderer remains untouched.

## Camera Framing

- Stage01: `(8.2, 5.6, -12.8)`, FOV `49` — closer tutorial gate framing with the full launcher base visible
- Stage02: `(9.2, 5.0, -13.8)`, FOV `50` — preserves the bridge width
- Stage03: `(9.2, 5.3, -14.0)`, FOV `49` — emphasizes the low bunker silhouette
- Stage04: `(10.8, 7.2, -17.2)`, FOV `49` — keeps vertical collapse headroom
- Stage05: `(12.0, 7.0, -18.5)`, FOV `51` — fits the asymmetric gantry and right cantilever

The launcher remains visible in the lower foreground and large stages retain extra collapse margin.

## HUD Cleanup

The inherited M0 `OnGUI` controls are disabled only in Stage01–05. The current Stage, Shots, and Destruction labels receive consistent spacing and a subtle outline. Result panel and button colors use the industrial palette without introducing a full UI redesign.

## Reproduction

Use the `DemolitionLab/Presentation` Editor menu to apply or validate the environment, structural presentation, and camera framing. The same presentation builder is also called by every vertical-slice scene builder.

## Physics And Presentation Separation

Every Stage01–05 object tagged `Destructible` keeps its Transform, Rigidbody, Collider, FixedJoint, target component, and critical-support component on the physical root. Its prototype MeshFilter, MeshRenderer, and connection details live below a local-identity `VisualRoot` child. `PresentationVisual` records a presentation-only category for later prefab or material assignment and contains no gameplay behavior.

Imported structural art must be placed below `VisualRoot` without Rigidbody, Collider, Joint, or gameplay components. Mesh offsets and scale corrections belong on an additional child, never on the physical root or `VisualRoot`. Use `DemolitionLab/Presentation/Validate Physics And Visuals` after replacing any geometry.

## Gameplay Lock

Presentation Pass 1 does not change projectile settings, Rigidbody values, colliders, joint thresholds, target rules, ammunition, success thresholds, settle timing, or stage progression. Manual Play Mode remains required to judge final screenshot composition and collapse framing.

## Manual Screenshot Checklist

1. Enter each Stage01–05 and confirm the legacy M0 HUD is absent.
2. Confirm Weak and Reinforced supports remain distinguishable without relying only on color.
3. Fire into detailed supports and confirm visual children do not block the projectile or alter collapse.
4. Confirm trajectory preview and launcher origin remain readable.
5. Capture Stage01, Stage02, and Stage03 intact.
6. Capture Stage04 intact and during its major collapse.
7. Capture Stage05 intact and during its final collapse.
