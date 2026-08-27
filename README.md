# DemolitionLab

**Current milestone:** M0

DemolitionLab is a minimal playable prototype for testing physics-driven projectile impacts against a simple block structure.

## Controls

- **Aim:** `WASD` or arrow keys
- **Fire:** `Space`
- **Reset scene:** `R`

## Implemented Features

- Playable URP test scene with a ground plane, block wall, launcher, lighting, and framing camera
- Adjustable launcher yaw and pitch with a visible ballistic trajectory preview
- Rigidbody projectile launch with Inspector-tunable speed, mass, and lifetime
- Rigidbody blocks that remain stacked at startup and react physically to projectile collisions
- Repeatable firing with automatic projectile cleanup
- One-key scene reload for fast iteration
- Concise fire and destructible-impact debug logging

## Current Scope

Structural destruction logic is **not implemented yet**. M0 relies entirely on standard Unity Rigidbody and Collider behavior; there are no support graphs or custom collapse rules.

The scene can be regenerated from `DemolitionLab > Build M0 Scene` in the Unity Editor.
