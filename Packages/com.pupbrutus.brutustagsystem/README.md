# Brutus Tag System (BTS)

A VRChat world prefab that lets players pick tags (pronouns, interests, ice-breakers, anything you design) from a menu and shows them above their heads to everyone in the instance. Each player's picks and tag height are saved with VRChat persistence, so they come back on the next visit.

Credit is appreciated but not required (MIT licence). Thanks to [PlayerBush001](https://www.youtube.com/@PlayerBush001) for the tutorial and troubleshooting help behind the first version.

**[Demo world](https://vrchat.com/home/launch?worldId=wrld_341620ab-e985-4176-b842-599cdfb20ed1)**

## Requirements

- Unity 2022.3 (the VRChat version)
- VRChat Worlds SDK 3.10.5 or later (includes UdonSharp). BTS uses PlayerObject persistence, which arrived in SDK 3.7.4; 3.0.0 was tested on 3.10.5 and 3.10.6.

No other packages are needed. (Versions before 2.0 needed CyanPlayerObjectPool; BTS no longer uses it.)

## Installing

Both ways install the same files to `Packages/com.pupbrutus.brutustagsystem`.

**Creator Companion (VCC / ALCOM).** Add the Brutus Tag System listing to the VCC (Settings > Packages > Add Repository), then add *Brutus Tag System* to your project from Manage Project. Updates then show up in the VCC.

**.unitypackage.** Download `com.pupbrutus.brutustagsystem-<version>.unitypackage` from the [releases page](https://github.com/PupBrutus/BrutusTagSystem/releases) and import it (Assets > Import Package > Custom Package). To update, import the newer one.

Upgrading from 2.x? Read [Upgrading from 2.x](#upgrading-from-2x) first.

## Setting up your world

1. Drag `Packages/Brutus Tag System/Runtime/Prefabs/BTS` into your scene. Place `BTS/TagUIManager` (the tag menu) where players can reach it.
2. Right-click `BTS` > Prefab > **Unpack** (not *Unpack Completely*). Keep `btsTagDisplay` and `TagUIManager` as prefabs.
3. Build & Test. The menu toggles tags; the slider sets their height. `Demo/TagSetupScene` shows a working setup.

Out of the box BTS shows the 31 example tags. To use your own, see [Your own tags](#your-own-tags).

**Things not to change**

- **Don't delete and re-add BTS once your world is public.** Saved tags are stored against the scene network ID of `btsTagDisplay` (a PlayerObject). A new object gets a new ID and every player's saved tags are lost. For the same reason, never use *Regenerate Scene IDs* in the Network ID Utility on a live world.
- **Don't rename `btsTagDisplay`.** `btsUiManager` finds each player's copy by that name.
- **Tag ids are positions.** A tag's id is its position in the tag config, and each button's GameObject name is the id it saves. Never reorder or remove entries; only append.

## Your own tags

Tags are drawn as plain meshes that share one atlas texture and one material, not as UI Canvases (see [Performance](#performance)). The baker builds them from a **tag config**: a list of tag images in tag-id order.

1. Make each tag image from `Tag Template/TEMPLATE.psd` or an image in `Example Tags/`. Import it with **Texture Type: Sprite (2D and UI)**. Keep your images under `Assets/`, never in the package folder, which updates replace.
2. Run **Tools > Brutus Tag System > Bake Tag Meshes** with your scene open. The first time, choose **Create my config**: this copies the example config to `Assets/BrutusTagSystem Tags/TagBakeConfig.asset` and bakes it. Your baked atlas, material and meshes go into `Assets/BrutusTagSystem Tags/Baked/`.
3. Select your `TagBakeConfig` and **append** your sprites to *Tag Sprites*. To start from your own set instead of the examples, replace the entries before any player has saved tags; after launch, only append.
4. For each new tag, duplicate a button under `TagUIManager/UI-Canvas/ViewPort/Content/`, rename it to the tag's id (its position in the config, from 0), and set its Image's *Source Image* to the tag sprite. Check its `btsUiManager`, `enabledIndicator` and `MaxTagsMessage` references.
5. Bake again, then save the scene.

The bake rebuilds `btsTagSourceMesh` (inactive templates, one per tag) next to `btsTagDisplay`, links them and each tag's height to `btsTagDisplay`, and points every button that shows a tag image at its atlas sprite, so the original images stay out of the world build. Keep one `TagBakeConfig` under `Assets/`; the baker uses whichever one it finds there, wherever it is. `Max Tags` on `TagUIManager` (default 5) caps how many tags a player can wear.

## Tag visibility

These settings on `btsTagDisplay` only change what each player sees. They never touch tag data, sync or saved tags.

| Setting | Default | What it does |
|---|---|---|
| `maxTagDistance` | 60 | Tags of players farther away than this (metres) are hidden |
| `nearDistance` / `midDistance` | 15 / 30 | Within `nearDistance` a tag moves every frame, within `midDistance` every 2nd frame, beyond it every 3rd |
| `visibilityCheckInterval` | 0.5 | Seconds between visibility checks for each tag |
| `tagMirror` | empty | Optional. Your own tag only shows (and moves) while this mirror object is on, since you can't see it above your own head. Empty: always shown. Have the mirror's toggle also call `btsUiManager._OnTagMirrorChanged` so it updates at once |
| `zoneTemplate` / `zoneVariableName` | empty / `zoneId` | Optional zone filter, see below. Empty: distance only |

Tag meshes are on the **UI** layer, so mirrors and cameras only show them if their culling or reflect layers include UI.

### Optional zone filter

In a world with separate areas (rooms, floors, an outside), you can hide the tags of players who are in a different area, even when they're within range through a wall.

1. Drag `Runtime/Prefabs/btsZones` into your scene.
2. Move its marker children to the middle of each area and set `btsZoneMap` > *Marker Zone* to each marker's zone id (1–255; several markers can share an id). A player belongs to the zone of the nearest marker within *Max Marker Distance*; farther than that counts as 0, which matches every zone.
3. Drag `btsZones/btsPlayerZone` (a PlayerObject, one copy per player) into `btsTagDisplay` > *Zone Template*.

Each player's copy syncs one byte, and only when their zone changes. You can use your own zone script instead: any PlayerObject UdonBehaviour with a synced `byte` named as in `zoneVariableName`.

## Performance

The 3.0 tag display was rebuilt for crowded instances:

- Baked mesh tags with one shared atlas and material instead of a Canvas per player, so tags batch together.
- Tags out of range or in another zone are hidden; distant tags move less often; your own tag only updates while the tag mirror is on.
- No Camera, AudioListener, Canvas or layout components in the per-player object.

Measured in the Unity editor with ClientSim, 80 simulated players wearing 5 random tags each, in the world BTS was developed for (not measured in the VRChat client; one 3.0 run against the average of eight 2.x runs):

| Per frame | 2.x | 3.0 |
|---|---|---|
| Tag draw calls | ~125 | ~3.5 |
| Udon tag cost | 1.46 ms | 1.02 ms |
| Canvas update | 2.79 ms | 0.07 ms |
| Whole player loop | 4.96 ms | 1.00 ms |

Your numbers will differ with your world and tag count.

## Test world (Demo/TagSetupScene)

The demo scene is a generic test location for every feature. Upload it as a private world, or use Build & Test with 2 or more clients. Tag visibility depends on where other players are, so one client can't show it.

| Area | What to check |
|---|---|
| Spawn (blue floor, zone 1) | The tag menu and height slider. Rejoin and your picks and height come back (persistence). The **Tag mirror** toggle: your own tag shows only while the mirror is on |
| Zone 2 (orange room) | One player each side of the doorway: within 15 m of each other, but each player's tag is hidden for the other because they're in different zones. Step through and it shows |
| Distance lane (behind spawn) | Stand on the dark pad; another player walks down the lane. Gates at 15, 30 and 60 m match `nearDistance`, `midDistance` and `maxTagDistance`: past 15 m their tag moves every 2nd frame, past 30 m every 3rd, past 60 m it hides |

The **status boards** (spawn, lane, zone 2) show your zone, whether your own tag is visible, and for each other player: distance, zone, and whether their tag is hidden (and why) or how often it moves. `btsDemoStatusBoard` only reads state and is for this scene only; your world doesn't need it.

The zone setup is the `btsZones` prefab with four markers: three for zone 1 (spawn, lane at 30 m and 60 m) and one for zone 2. The mirror toggle is a plain UI Toggle with two events, `GameObject.SetActive` on the mirror and `UdonBehaviour.SendCustomEvent("_OnTagMirrorChanged")` on `TagUIManager`, so you can copy it as is.

## How it works

| Script | On | Role |
|---|---|---|
| `btsTagDisplay` | `btsTagDisplay` (PlayerObject with persistence, Manual sync) | One per player. Syncs and saves that player's tags (`_playerTagString`, a JSON list of tag ids) and tag height (`_playerHeightOffset`). Every client stacks the matching mesh templates above the owner's head in `PostLateUpdate`, with the visibility rules above |
| `btsTagButton` | each tag button | Adds or removes its tag (its GameObject name) in the local player's list, enforces `Max Tags`, shows its enabled indicator |
| `btsUiManager` | `TagUIManager` | Finds the local player's `btsTagDisplay`, holds `Max Tags`, forwards tag mirror changes |
| `btsSlider` | the height slider | Writes the local player's tag height |
| `btsZoneMap`, `btsPlayerZone` | `btsZones` (optional) | The zone filter above |

Editor only: `BtsTagMeshBaker` (the bake) and `BtsTagBakeConfig` (the tag config asset type).

## Upgrading from 2.x

Script, prefab and image GUIDs are unchanged, so your scene's BTS objects reconnect to the new files. Saved tags carry over as long as the scene's `btsTagDisplay` object (and its network ID) stays.

1. Back up your project.
2. Move any tag images you added out of `Assets/BrutusTagSystem` (for example to `Assets/BrutusTagSystem Tags/`).
3. Delete `Assets/BrutusTagSystem`, then install 3.0.0 (VCC or .unitypackage). Don't install first, or Unity will see two copies of every file.
4. Open your scene. In `Assets/BrutusTagSystem Tags/`, create an **empty** config: right-click > Create > Brutus Tag System > Tag Bake Config.
5. Run **Tools > Brutus Tag System > Bake Tag Meshes**. With an empty config, the baker fills it from your scene's current tag templates in tag-id order, then bakes. Check the order, then save the scene. If it reports it can't read a tag (for example because BTS wasn't unpacked, so the old templates came from the prefab), fill the config by hand in tag-id order and bake again.
6. If you use a tag mirror, make sure its reflect layers include **UI**, and optionally set `tagMirror` (see [Tag visibility](#tag-visibility)).

## Troubleshooting

- **No tags appear:** check the scene has exactly one `btsTagDisplay` and that `btsTagSourceMesh` exists next to it; re-run the bake and save the scene.
- **Wrong tag shows for a button:** the button's name must equal its position in the config.
- **"More than one tag config under Assets":** keep one `TagBakeConfig` under `Assets/`.
- **Your own tag isn't visible:** that's intended unless the tag mirror is on (when `tagMirror` is set).
- Report problems on [GitHub issues](https://github.com/PupBrutus/BrutusTagSystem/issues). The scripts contain commented-out `Debug.Log` lines you can enable while debugging.
