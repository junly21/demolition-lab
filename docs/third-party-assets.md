# Third-Party Assets

## Assets In Use

### Kenney City Kit (Industrial)

- Asset name: City Kit (Industrial) 1.0
- Author / publisher: Kenney
- Source: https://kenney.nl/assets/city-kit-industrial
- License: Creative Commons Zero (CC0 1.0)
- What it is used for: Distant industrial warehouses, plant silhouettes, cooling chimneys, and storage tanks
- Whether modified: Wrapped in project-owned, visual-only prefabs; URP material override and scene scale/placement applied

### Kenney Factory Kit

- Asset name: Factory Kit 3.0
- Author / publisher: Kenney
- Source: https://kenney.nl/assets/factory-kit
- License: Creative Commons Zero (CC0 1.0)
- What it is used for: Cargo crates, factory machinery, pipe runs, yard crane, and launcher housing detail
- Whether modified: Wrapped in project-owned, visual-only prefabs; all non-render components stripped and URP material override applied

### Poly Haven Asphalt 01

- Asset name: Asphalt 01
- Author / publisher: Poly Haven
- Source: https://polyhaven.com/a/asphalt_01
- License: Creative Commons Zero (CC0)
- What it is used for: Demolition-yard ground material
- Whether modified: Imported at a 2048 maximum runtime size; roughness converted to a URP metallic/smoothness mask

## Imported But Not Currently Used

### Poly Haven Concrete

- Asset name: Concrete
- Author / publisher: Poly Haven
- Source: https://polyhaven.com/a/concrete
- License: Creative Commons Zero (CC0)
- What it is used for: Retained as an imported source asset, but no longer assigned to structural pieces because its directional surface read as wood-like at gameplay scale
- Whether modified: Imported source remains unchanged; structural concrete now uses a project-owned procedural mottle texture

## Asset Record Template

- Asset name:
- Author / publisher:
- Source:
- License:
- What it is used for:
- Whether modified:

## Safe Import Rules

1. Import original packages under `Assets/Art/ThirdParty/<AssetName>` where practical.
2. Record the asset and license in this document before using it in a scene.
3. Create project-owned visual prefabs under `Assets/Art/Prefabs` instead of editing imported source files directly.
4. Remove or disable Rigidbody, Collider, Joint, and gameplay components on visual prefabs.
5. Replace content only below a structural piece's `VisualRoot`; never replace or transform its physical root.
6. Keep `VisualRoot` at local position zero, identity rotation, and unit scale. Offset or scale imported geometry in a child below it.
7. Run the presentation and vertical-slice validators after every replacement batch.
