# Changelog

## 1.1.0 - 2026-11-27

- Add RimWorld 1.7 support while preserving the existing RimWorld 1.6 binary.
- Separate version assemblies and share translations and command textures.
- With Humanoid Alien Races on 1.7, allow child apparel on the shared outfit
  stand when permitted by the policy and selected body.
- Preserve policy exclusions when applying the body age restriction.
- Provide a fallback icon for HAR's race command when its icon is missing.
- Update the preview for RimWorld 1.7.
- Add configurable build, consistency, test deployment and release packaging
  commands. Existing save keys and defNames are preserved.

Validation: Release build and structural checks passed. The user reported
successful in-game tests of the deployed 1.7 build on 27 November 2026.
The preserved 1.6 binary was not retested in game during this migration.

## 1.0.0

Previous tagged version, including apparel policy application, manual reapply
and clear commands, and automatic resync of managed stands on load.
