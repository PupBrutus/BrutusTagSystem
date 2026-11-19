# Changelog

All notable changes to Brutus Tag System. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and versions follow [Semantic Versioning](https://semver.org/).

## [3.0.0]

Upgrading from 2.x: follow "Upgrading from 2.x" in the README. Saved tags carry over.

### Added
- Distributed as a package (`com.pupbrutus.brutustagsystem`): install through the VRChat Creator Companion or the .unitypackage from GitHub releases. Both install to `Packages/com.pupbrutus.brutustagsystem`.
- Baked mesh tags: **Tools > Brutus Tag System > Bake Tag Meshes** packs every tag image into one atlas with one material and one quad mesh per tag, and points the tag buttons at atlas sprites. Your tag config and bake live under `Assets/`, so package updates never overwrite them.
- Visibility settings on `btsTagDisplay`: maximum tag distance, distance-based update rate (every 1st/2nd/3rd frame), visibility check interval.
- Optional tag mirror link (`tagMirror`): your own tag only shows and moves while the mirror is on. `btsUiManager._OnTagMirrorChanged` refreshes it at once.
- Optional zone filter (`zoneTemplate`) and the `btsZones` example prefab (`btsZoneMap`, `btsPlayerZone`): hide tags of players in another area of the world.
- Assembly definitions (`PupBrutus.BrutusTagSystem`, `.Editor`) with a U# assembly definition.
- The demo scene is now a test world: two zones, a distance lane with gates at 15/30/60 m, a tag mirror toggle, and status boards (`btsDemoStatusBoard`, demo only) showing each tag's zone, distance and update rate.

### Changed
- `btsTagDisplay` stacks the baked mesh tags itself, in pick order. The Canvas, CanvasScaler, GraphicRaycaster, layout components, panel Image, Camera and AudioListener were removed from the per-player object.
- Tag meshes are on the UI layer, so mirrors show them only if their reflect layers include UI.
- Player data is never written before persistence has restored it, so a late restore can't overwrite saved tags or height with defaults.
- Tag buttons ignore clicks until the player's saved tags are restored.

### Removed
- The CyanPlayerObjectPool dependency (BTS uses VRChat PlayerObjects).
- The image tag templates (`btsTagSource`) from the BTS prefab. `btsTag.prefab` remains only so 2.x scenes can be upgraded.

## [2.0] - 2025-07-24

- Rebuilt on VRChat PlayerObject persistence: tags and height are saved per player.
