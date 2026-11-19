# BrutusTagSystem

A VRChat world prefab that lets players choose tags (pronouns, interests, ice-breakers, anything you design) and shows them above their heads. Picks are saved per player with VRChat persistence. Tags are drawn as baked meshes that share one atlas and material, so a busy instance costs a handful of draw calls.

**[Demo world](https://vrchat.com/home/launch?worldId=wrld_341620ab-e985-4176-b842-599cdfb20ed1)** · **[Releases](https://github.com/PupBrutus/BrutusTagSystem/releases)** · **[Changelog](Packages/com.pupbrutus.brutustagsystem/CHANGELOG.md)**

## Install

Requires Unity 2022.3 and the VRChat Worlds SDK 3.10.5 or later.

- **Creator Companion:** open the [listing page](https://pupbrutus.github.io/BrutusTagSystem/) and click **Add to VCC** (or add `https://pupbrutus.github.io/BrutusTagSystem/index.json` in Settings > Packages > Add Repository), then add *Brutus Tag System* to your project.
- **.unitypackage:** download it from [Releases](https://github.com/PupBrutus/BrutusTagSystem/releases) and import it.

Then follow the **[setup guide](Packages/com.pupbrutus.brutustagsystem/README.md)**: placing the prefab, adding your own tags, visibility and zone options, and upgrading from 2.x.

## This repository

This is the Unity project used to develop BTS. The package itself is `Packages/com.pupbrutus.brutustagsystem` (package id `com.pupbrutus.brutustagsystem`); its `Demo/TagSetupScene` is the test scene.

Releasing: bump `version` in the package's `package.json` and add a CHANGELOG entry, merge to `main`, then run **Actions > Build Release** on `main`. It builds the VCC zip and the .unitypackage, publishes a GitHub release tagged with that version, adds the zip to `source.json`, and rebuilds the VCC listing on GitHub Pages. **Actions > Build Repo Listing** rebuilds only the listing.

One-time setup: the repository variable `PACKAGE_NAME` = `com.pupbrutus.brutustagsystem` (Settings > Secrets and variables > Actions > Variables) and Settings > Pages > Source: **GitHub Actions**. The listing page is built from `Website/` (from [vrchat-community/template-package-listing](https://github.com/vrchat-community/template-package-listing)); to show a banner, add `Website/banner.png` and `"bannerUrl": "banner.png"` to `source.json`.

Issues and suggestions: [GitHub issues](https://github.com/PupBrutus/BrutusTagSystem/issues). Licence: [MIT](LICENSE).
