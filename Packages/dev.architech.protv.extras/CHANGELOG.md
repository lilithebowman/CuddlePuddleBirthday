# ProTV Extras Changelog
Manually curated document of all notable changes to this project sorted by version number in descending order.

Structure used for this document:
```
## Version Number (Publish Date)
### Added
### Changed
### Deprecated
### Removed
### Fixed
```

<!-- CHANGELOG -->

## 0.9.3-beta (2025-03-28)
- Fixed Hangout prefab video engine selector being overlapped by the Plugins Drawer UI.

## 0.9.2-beta (2025-03-27)
- Remove PlaylistData objects from playlist prefabs to avoid serialization lag on large playlist imports.
- Update ProTV dependency to 3.0.0-beta.24.1

## 0.9.1-beta (2025-03-27)
- Fix animator bugs in the _Special prefabs.
  - The Drawer prefabs toggle should work correctly now.

## 0.9.0-beta (2025-03-23)
- Update ProTV dependency to 3.0.0-beta.24
- Update prefabs to match the changes to playlist and history UI script separation.
- UI rework on the Hangout prefab, should feel much easier to use.
- Hangout prefab UI can now be duplicated across the world with proper synchronization between each UI.
  - Just duplicate the `Room` game object.
- Removed obsolete `Samples/Prefabs/Monochrome/MediaControls Drawer (Monochrome)` in favor of the redesigned `Samples/Prefabs/_Special/TVControls Drawer (Monochrome)` prefab.
  - Unpack the prefab before you update if you wish to keep the old design.

## 0.8.3-beta (2024-06-29)
- Fix RiskiPlayer default volume not being correctly respected on init.

## 0.8.2-beta
- Fix RiskiPlayer UI title not being updated correctly.
- Bump RiskiPlayer version to 3.1.1.
- Restore the screen material to the correct material reference.

## 0.8.1-beta (2024-06-23)
- Fix RiskiPlayer UI title not being updated correctly.
- Bump RiskiPlayer version to 3.1.1.
- Restore the screen material to the correct material reference.

## 0.8.1-beta (2024-06-13)
- Add Readme with information about each community prefab.
- Fix edge case where lock icon starts incorrectly for the RiskiPlayer.

## 0.8.0-beta (2024-03-28)
- Add Blu UI theme and prefabs.
- Add RiskiPlayer theme and prefabs.
- Add default namespace to the assembly definition.
- Add flag to package json to more easily find package contents in unity searches.
- Add arrow images to PlaylistQueueDrawer to signal which tab is active.
- Fix CyberBlue and CyberRed UI SFX balance.
- Fix CyberBlue and CyberRed video player selector dropdown text scaling.

## 0.7.5-beta (2024-02-28)
- Update Queue with QueueUI usage in the PlaylistQueueDrawer prefab that was missed previously.

## 0.7.4-beta (2024-02-27)
- Update ArchiTech.ProTV dependency to beta.13.2.
- Update Queue prefabs to reflect the new QueueUI component usage.

## 0.7.3-beta (2024-01-27)
- Add alternate url and title inputs to Cyber Red skin.
- Adjust layer of the default screens to Environment instead of Player.
- Update TV prefabs to default to low latency mode and include a non-low-latency option.

## 0.7.2-beta (2024-01-12)
- Move some animations from core ProTV into Extras as core doesn't need them.
- Add placeholder scripts to the Runtime and Editor folders to avoid the warning about assemblies with no scripts.
- 2022 upgrade for prefabs and sprite atlases.

## 0.7.1-beta (2023-11-17)
- Aesthetic tweaks to Cyber Red skin by BluWizard

## 0.7.0-beta (2023-11-09)
- Add Cyber Red skins, contributed by BluWizard

## 0.6.1-beta (2023-11-09)
- Rebuild the VPManager components on prefabs to fix some bad data stored in the prefab.

## 0.6.0-beta (2023-10-20)
- Fix incorrect icon references.
- Update Monochrome prefabs for the latest protv beta.

## 0.5.3-beta (2023-09-21)
- Remove explicit UdonSharp dependency.
- Update version number to a prerelease to avoid some confusion with users who forget to enable prereleases.
- Update Android settings for sprite atlases.

## 0.5.2-beta (2023-08-28)
- Add CONTRIBUTING document.
- Rename TwinRetro items to remove the name in favor the contributing document.
- Update ProTV dependency minimum to 3.0.0-beta.1
- Add new Cyber theme and Cyber Blue skins, contributed by BluWizard
- Rename the folder Resources/UI to Resources/Themes

## 0.5.1-beta (2023-08-25)
- Update package naming from ProTV.Skins to ProTV.Extras
  - This is to be more inclusive of community contributions that aren't theme related, like custom plugins.

## 0.5.0-beta (2023-08-23)
- Initial commit.
- Add TwinRetro theme contributed by MissStabby.
- Add Neon theme contributed by Shyaong.
- Compatible with ProTV 3.0.0-alpha.30 and later.