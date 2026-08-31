# Third-Party Assets

No third-party assets have been imported yet.

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
