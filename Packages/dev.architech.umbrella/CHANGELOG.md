# ArchiTech Umbrella Changelog
All notable changes to this project will be documented in this file

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

## 0.9.0 (2025-10-16)
### Added
- [ ColliderActionProxy ] Add custom inspector for ColliderActionProxy.
- [ ColliderActionProxy ] Enable custom event names on a per-event basis.
- [ ZoneTrigger ] Add UdonBehaviour Private and Public variable assignment.
  - This supports the following primitive types: bool, int, float, Vector2, Vector3, Vector4, VRCUrl
  - It also supports Enums (internally converted to ints) and any UnityEngine.Object type that Udon supports.
  - For Objects, the editor should automatically limit the input type to the variable's expected type.
- [ ZoneTrigger ] Add support for Timeline enable and multiple actions.
  - Supports: Play, Pause, Resume, Stop.
- [ ZoneTrigger ] Add Reset method to switch the script back to a fresh init state.

### Changed
- [ ZoneTrigger ] Update enable/toggle to be consistent with label names and to all use the toggle enum for selecting the state.
  - Any enable/disable should now be in the form `[Context] / Toggle`
  - Some of this involved modifying the backing values of some enums. Double-check your enum selections to make sure they are correct.
- [ ZoneTrigger ] Update UdonBehaviour info to have specific sorting order for event and variable names.
  - Events are sorted alphabetically, variables are sorted non _ first, then _ then any __ prefix variables present.
  - `__` double underscore variables are usually internal local variables that store temporary state while the script is running.
  Don't mess with these UNLESS YOU KNOW EXACTLY WHAT YOU ARE DOING.
- [ ZoneTrigger ] Move Audio enable/disable to its own action instead of being a sub-action.
- [ PickupAutoRespawn ] Update to allow a sync object reference that isn't on the current object.
  - Helps when the pickup isn't on the same object as the sync.

### Fixed
- [ ZoneTrigger ] Fix ZoneTrigger gizmo using the incorrect matrix transformation, causing improper draw locations.

## 0.8.4 (2024-12-11)
### Fixed
- [ Misc ] Corrected typo in dependency requirement.

## 0.8.3 (2024-12-10)
### Added
- [ ZoneTrigger ] Add `Animator / CrossFade` and `Animator / Play` actions.
- [ ZoneTrigger ] Add support to `Animator / Trigger` for resetting the trigger as a toggle option. 

### Changed
- [ ZoneTrigger ] Some minor inspector improvement tweaks for Animator related actions.

## 0.8.2 (2024-12-06)
### Fixed
- [ Misc ] Fix font enforcement not correctly saving for text components inside prefabs.

## 0.8.1 (2024-11-26)
### Fixed
- [ ZoneTrigger ] Fix for undocumented API behavior change with GetPlayerTag.
- [ ZoneTrigger ] Internal typo fix in field name for `canvasGroupInstantFade`.

## 0.8.0 (2024-06-13)
### Added
- [ ZoneTrigger ] Add collider specific actions.
- [ ZoneTrigger ] Add `UdonBehaviour / Toggle` action.
- [ ZoneTrigger ] Add `Audio / Play Clip` action which changes the audio clip then immediately plays the new clip.
- [ Misc ] Add helper algebra for rigidbodies to apply velocity towards a specific position or rotation.

### Changed
- [ ZoneTrigger ] Prevent math checks from running while the game object is disabled.
- [ ZoneTrigger ] Refactor ATTriggerActions for internal simplification.
- [ Misc ] Modify some label and loggin text.

### Fixed
- [ ZoneTrigger ] Fix some NPE and out of bounds errors.
- [ Fling ] Fix Fling teleport to not spin glitch anymore while moving the player.

## 0.7.1 (2024-02-24)
### Fixed
- [ ZoneTrigger ] Fix out of bounds issue when upgrading an existing ZoneTrigger causing inspector failures.
- [ ZoneTrigger ] Remove unused object reference from Player/Teleport action in the inspector.

## 0.7.0 (2024-02-23)
### Added
- [ ZoneTrigger ] Add Vector4 data type support to the trigger actions.
- [ ZoneTrigger ] New trigger actions:
  - `Player / Teleport` - Teleport the player to the location of a given Vector3, optionally interpret the vector as relative to the player's current position.
  - `Player / Teleport To` - Teleport the player to the location of a given transform, optionally make the teleport seamless.
  - `Player / Speed` - Change the player's input movement speed (run/walk/strafe/jump), optionally add to the player's existing input speed.
  - `Player / Velocity` - Change the player's physics movement speed (velocity), optionally add to the player's existing velocity.
  - `Player / Gravity` - Change the player's gravity strength, optionally add to the existing gravity strength.
  - `Player / Reset Movement` - Revert the player's movement speed and gravity to their initial values.
  - `Time Delay` - Make the trigger wait some amount of time before continuing with the remaining action entries.
    - THIS IS CONSIDERED AN EXPERIMENTAL FEATURE. It has been tested to work, but may cause some unexpected behaviour in certain edge-cases. Use with caution.
    - This is a non-blocking delay. Other scripts will continue to run, except any Zone Trigger actions.
    - When a time delay is active, NO OTHER ZONE TRIGGER ACTIONS CAN BE ACTIVATED. The currently executing trigger actions will be forced to complete before other actions can be taken.
- [ Misc ] Font Enforcement Window`
  - Commissioned by Digital
  - This tool enables bulk update of Font assets on both Unity UI and TextMeshPro components.
  - This tool is available under `Window -> Text -> Font Enforcement` or `Tools -> Umbrella -> Font Enforcement`

### Changed
- [ ZoneTrigger ] Abstract the core trigger actions logic into a parent class for easy reuse in other utilities.
- [ ZoneTrigger ] Rework action selection menu into sub categories to avoid too much vertical bloat when the menu is open.
- [ ZoneTrigger ] Reorganize some internal logic for cleaner handling and better language support.
- [ Misc ] Update the namespace for Tether and Fling utilities.

## 0.6.0 (2024-01-02)
### Added
- [ ZoneTrigger ] ZoneTrigger V2 Overhaul
  - CONTAINS BREAKING CHANGES FROM PREVIOUS VERSION.  
  You will need to rebuild any actions you had previously with the new structure.
  - Add support for animator bools/float/ints.
  - Add support for audio options (float values).
  - Add support for changing audio clips.
  - Add support for object re-parenting.
  - Add support for particle system actions (play/pause/stop/clear).
  - Add rotation support for math (range/area) modes.
  - Add support for specifying what to affect during a Teleport (position/rotation/scale).

### Changed
- [ ZoneTrigger ] ZoneTrigger V2 Overhaul
  - CONTAINS BREAKING CHANGES FROM PREVIOUS VERSION.  
    You will need to rebuild any actions you had previously with the new structure.
  - Separate into sequential actions on a per-state basis.
    - This means that users can now choose the exact order in which to run the actions.
  - Change visuals persistence to be better retained across domain reloads.
  - Update inspector to display options in the new per-state list structure.
- [ Dependency ] Update ArchiTech.SDK minimum to 0.15.0.

## 0.5.3 (2023-11-28)
### Changed
- [ Dependency ] Update ArchiTech.SDK to 0.14.0.

## 0.5.2 (2023-11-07)
### Changed
- [ Dependency ] Update ArchiTech.SDK to 0.13.0.

## 0.5.1 (2023-10-15)
### Changed
- [ Dependency ] Update package.json with more forgiving version matching.

## 0.5.0 (2023-10-09)
### Changed
- [ Dependency ] Update VRCSDK minimum to 3.4.0.
- [ Dependency ] Update ArchiTech.SDK to 0.11.0.

## 0.4.6 (2023-09-21)
### Fixed
- [ ZoneTrigger ] Fix incorrect calculation of zone center for Area mode.

## 0.4.5 (2023-09-21)
### Changed
- [ Dependency ] Update the dependencies in preparation for VRChat's 3.4.0 merge update.

## 0.4.4 (2023-09-21)
### Fixed
- [ ZoneTrigger ] Fix issue with sphere mode improperly modifying the ZoneTrigger's transform position of the object it's on.

## 0.4.3 (2023-09-16)
### Fixed
- [ ZoneTrigger ] Fix Area edit trigger mode drag handles being located at the incorrect world position.

## 0.4.2 (2023-08-23)
### Changed
- [ Dependency ] Update ArchiTech.SDK minimum to 0.10.0.

### Fixed
- [ ZoneTrigger ] Add missing type parameter for fling editor array normalizing.

## 0.4.1 (2023-08-07)
### Added
- [ ZoneTrigger ] Add dropzone validation check integration to the lists for better visual feedback.

### Changed
- [ Dependency ] Update ArchiTech.SDK minimum to 0.9.0.

## 0.4.0 (2023-07-22)
### Changed
- [ ZoneTrigger ] Update ZoneTrigger usage of ATReorderableList to match the new usage.
- [ ZoneTrigger ] Clear stranded UdonTrigger event names and animator trigger names upon swapping the reference object of an entry when the new reference doesn't contain those events/triggers.
- [ Dependency ] Update ArchiTech.SDK minimum to 0.8.0.

### Fixed
- [ ZoneTrigger ] Fix issue where animator trigger dropdowns would not be populated correctly.
- [ ZoneTrigger ] Fix the component order in the UdonTriggers dropdown to reflect the same order on the game object.


## 0.3.2 (2023-07-01)
### Changed
- [ Dependency ] Update ArchiTech.SDK dependency version.

### Fixed
- [ ZoneTrigger ] Fix zone trigger editor script not showing the force state option when in collider mode.

## 0.3.1 (2023-07-01)
### Added
- [ ZoneTrigger ] Add option to trigger the initial state operations.

### Changed
- [ ZoneTrigger ] Update gizmos and handles for ZoneTrigger to correctly respect the orientation of the local object when in Collider mode.


## 0.3.0 (2023-06-14)
### Added
- [ ZoneTrigger ] Add custom tooltip containing the list of trigger source options selected just by hovering your mouse over the element in the inspector.

### Changed
- [ Misc ] Library has been renamed to `Umbrella`
- [ Dependency ] Update ArchiTech.SDK dependency minimum to 0.7.0.
- [ Misc ] Move the Algebra class inside the ArchiTech namespcae.

## 0.2.3 (2023-06-01)
### Changed
- [ ZoneTrigger ] 
  - `Trigger Source` is now a multi-select option
    - This now allows you to pick and choose multiple positions to track.
    - If any of those positions are within the zone(s), it will be considered active
    - A helpful tooltip hover is available on the label that shows which sources are active without having to open the dropdown
  - Simplified some internal editor stuff by abstracting redundant code into the SDK
- [ Dependency ] Update ArchiTech.SDK dependency minimum to 0.6.0.

## 0.2.2 (2023-05-15)
### Fixed
- [ ZoneTrigger ]
  - Clicking the + button on the Animator triggers will no longer cause an error.
  - Fix unexpected deletion of elements in other triggers that are not selected when removing an entry via DELETE key.
  - Fix handle locations for area/range modes when not using scale.
  - Update default to use local object scale such that it resembles the sphere/box collider defaults.

## 0.2.1 (2023-05-14)
### Changed
- [ ZoneTrigger ] 
  - Enable multiple colliders on the object to operate as a trigger group.
  - Fix Area/Range options to calculate the positioning and scale just like their collider counter-parts.
  - Fix gizmos and handles to optionally take object scale into account.
  - Add toggle for whether to utilize the object scale or not.
  - Unset editing flag when another editor tool has been detected as selected.
- [ Dependency ] Update ArchiTech.SDK dependency minimum to 0.4.8.

## 0.2.0 (2023-05-11)
### Added
- [ ZoneTrigger ]
  - This creates a customizable area that will react to the local player and do various configurable actions based on if the player is within or outside of the area.
  - You can use either a custom Sphere or Box zone, or opt to use a collider on the same GameObject as the scripts.
  - If you use a custom zone, you can specify which trigger source you want: player position, viewpoint, hand (left or right), or playspace origin
  - If you use a collider, this will utilize the OnPlayerTriggerEnter/Exit events instead of a custom trigger source.
  - Actions available:
    - Auto-fade in/out a CanvasGroup's alpha value
    - Automatically enable/disable a VRCUiShape's collider (disabling the raycast basically) (VRCUiShape must be on the same object as the script)
    - Object Toggles
      - You can specify what action takes place regarding the active state of each provided game object
    - Udon Events
      - You can specify what udon events you wish to be called when the player enters and exits the area
    - Animator Triggers
      - You can specify what triggers are called when the player enters and exits the area
      - This only does triggers. Support for other parameter types is not planned.
    - Object Teleports
      - You can specify transforms to move a given transform to upon entering/exiting the area
      - The teleport applies world-space position AND rotation, and local-space scaling to the target transform.
- [ ObjectAutoRespawn ]
  - This is a convenience component that you attach to a pickup-able game object.
  - It will track the pickup state of the object, and if the object has been dropped for more than some given number of seconds, it will respawn to it's original location/rotation
  - It also handles when a VRCSyncObject component is also attached.
- [ Fling ]
  - This is a movement utility which uses some primitive spline math and teleportation to move a player throughout the world in various configurable ways.
  - it is pretty stable, but there may be some edge-case issues such as recursive object references having undefined behaviour currently.
- [ Tether ]
  - This is a movement utility that creates a walkable bridge that the player can choose the connecting points for in-game
  - Still in development, not feature complete

### Changed
- Move the UdonAction/ATToggle stuff into an Experimental folder. The stuff generally works, but is unstable.

## 0.1.0 (2022-10-21)
### Added
- Toggle
  - Includes custom editor
- SyncedToggle
  - Toggle, but synced across clients
- ObjectProximityToggle
  - Toggle, but detects the positions of a given set of transforms respective to a given set of destinations within a proximity.
- ColliderActionProxy
  - Captures player -> collider interactions and sends event signal to a designated behaviour.

