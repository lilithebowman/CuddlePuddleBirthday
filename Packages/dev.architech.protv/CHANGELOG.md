# ProTV Asset Changelog
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

## 3.0.0-beta.29.4 (2025-11-25)
### Changed
- [ Core ] Default keybind for refresh has been changed from F5 to F6
  - Avoids conflict with VRChat's Third-Person View update (which binds the camera toggle to F5 by VRChat).
  - Unfortunately, older worlds won't be able to avoid the keybind conflict unless they are updated.

## 3.0.0-beta.29.3 (2025-10-25)
### Fixed
- [ Core ] First instance master should no longer have the TVManager script crash with `Disable TV After Initialization` enabled.
    - This was caused by unexpected video error being triggered on re-enabling the TV because the Disable was happening in the same frame as the initialization.
    - Because of this, there was an incidental empty URL leaking through the TVManager logic causing the url parsing to crash.
    - Multiple protections have been added to mitigate this situation.

## 3.0.0-beta.29.2 (2025-10-07)
### Added
- [ Core ] Add convenience getter/setter proxy `BufferDelayAfterLoad` for the respective internal field.
  - This is effectively the same as using Get/SetProgramVariable on the internal field name.

### Changed
- [ Core ] Add some additional trace logging in certain spots for easier debugging.
- [ Core ] Adjust logic to avoid extraneous resync attempts when a new URL is loaded up.
- [ Core ] Add check for early return in deserialization if no data actually changed.
- [ Core ] Add default custom material property of _MainTex if one is not specifically provided (field is empty).
- [ Core ] Additional checks to prevent certain rare edge-case crashes.

### Fixed
- [ Core ] Add null checks for when a custom material entry is null.
  - This issue may have been erroneously noticed as an 'ACCESS DENIED' issue when observing the info display of Media Controls
  due to a behavioural timing side effect.
- [ Misc ] Adjust imports to avoid certain naming conflicts with other packages that don't use namespaces LIKE THEY SHOULD BE FOR CRYING OUT LOUD!
    - This issue was commonly causing issues with use of Unity UI Button in ProTV scripts.

## 3.0.0-beta.29.1 (2025-09-17)
### Changed
- [ Core ] Update _ChangeMedia to allow calling it before the TV has initialized, overrides any autoplay media assigned.

### Fixed
- [ Core ] Tentative fix for rare array oob error when deleting a VP Manager.
- [ Core ] Fix incorrect lifecycle execution for build checks in playmode, fixes broken UI buttons for certain scenarios.
- [ Core ] Fix incomplete implementation of forcing _TexelSize to be correct, this restores avatar video broken in beta.28.
- [ Core ] Fix default internal flag to avoid extraneous forced resync on media load.

## 3.0.0-beta.29 (2025-09-09)
### Added
- [ Core ] Add explicit Disabled option to GammaZone options.
- [ Core ] Add explicit build error when an audio filter is detected on an AVPro speaker which is can cause audio side-effects.
  - This avoids accidental audio breaking for people who don't yet know about the current limitations.

### Changed
- [ Core ] Add checks for RenderTexture Target resize to avoid too small of a texture.
- [ Core ] Rework data sync order of operations to try to stabilize multiple scenarios.
- [ Core ] Improve reliability of AutoOwnership (still experimental).
  - 'Catching up' to the owner of a disabled TV after auto-ownership is applied in some edge-cases won't be correct.
  - Some tweaking of time sync logic might be needed in future updates.
- [ Core ] Include current owner in the synced TV data to improve AutoOwnership reliability.
- [ Playlist ] Reorder and update display text for a couple playlist settings.
- [ Playlist ] Switch playlist generator from youtu.be to youtube.com/embed.
  - This avoids a redirect which causes issues in certain scenarios.

### Removed
- [ Core ] Remove vestigial enforcement of TVPlugin canvas collider enabling.

### Fixed
- [ Core ] Fix missing internal Blit material reference in certain scenarios.
- [ Core ] Fix gammaZone cropping across multiple scenarios.
  - This should resolve the 'video is black' issue introduced in beta.28.
- [ Auth ] Fix Auth check log to have correct newline formatting.
- [ Playlist ] Fix playlist not skipping over title-only entries when checking autoplay next.
- [ Misc ] Prevent unnecessary modification to non-world-space UIs.

## 3.0.0-beta.28 (2025-07-06)
### Added
- [ Core ] Add aspect fit mode to shader and blit.
  - The two options are:
    - `Fit Inside` - This mode is the default and represents existing behavior.  
    It retains the original video resolution aspect and rescales it to fit within the target aspect ratio.  
    This results in letterboxing/pillarboxing depending on which aspect (source or target) is larger.
    - `Fit Outside` - This mode takes the result of `Fit Inside` and scales it up uniformly so it
    removes the letterboxing/pillarboxing. This will cause the unboxed sides to overflow the UV and become cropped.
- [ Core ] Add notice to `TVManager` inspector that plugins aren't visible during playmode.
- [ Shader ] Add `Crop to GammaZone` option to support for individual material control for whether to trim to the defined GammaZone.
  - This will likely be an uncommon feature to mess with, but for example, if using the `ProTV/VideoScreen` shader for the VRSL DMX loop, 
  this toggle should be _disabled_ on that material.
  - It is enabled by default.

### Changed
- [ Shader ] Reorganize `ProTVCore.cginc` to better handle different needs.
  - Probably rare, but for anyone who made a custom shader using the include, you will need to make some updates.
- [ Shader ] Update blit shader to deduplicate code by switching to the `ProTVCore.cginc` include.
- [ Prefab ] Improve handling of adding generic or prefab tvs from menus.
  - Should no longer 'grey out' the options for `Tools/ProTV/Add to Scene` submenus.
- [ Dependency ] Updated ArchiTech.SDK minimum to 0.21.2.
- [ Misc ] Improve some editor error messages, added details.

### Fixed
- [ Core ] Fix RenderTexture gammazone crop not using the correct transform mode check.

## 3.0.0-beta.27 (2025-06-03)
### Changed
- [ Core ] Fix minor casing issue with url parameter detection.
- [ Shader ] `ProTV/VideoScreen` enhancements
    - Add the ability to switch render modes between Normal and Overlay.
        - Overlay is the functional equivalent to what the `ProTV/FullScreen` shader did.
    - Add DepthOnly and DepthNormals passes to help transition with possible URP future work (doesn't really matter for VRC)
    - Improve shader performance by removing all branching logic and switching to compiler defines and strict mathematical ops.
    - Add toggle to enable/disable the edge anti-aliasing (tiny black fade border).
- [ Shader ] Combine cginc files into a single include.
  - Old cginc is deprecated and just imports from the main cginc for whoever may be using it in a shader.
  - The file `Packages/dev.architech.protv/Resources/Shaders/ProTVHelpers.cginc` will be removed in a future version.
  - For `ProTVCore.cginc`, to import only the functions/variables, define either `PROTV_CORE_VARIABLES_INCLUDED`/`PROTV_CORE_FUNCTIONS_INCLUDED` respectively
    before importing the cginc as the script has define guards to control the importing.
- [ Shader ] Removed the SoundOnly texture from _only the ProTV shaders_.
  - The sound only texture in the shader was never actually reached during runtime because
    the sound only texture is assigned during the blit process of the video texture.
  - Having it as part of the protv shader was unnecessarily duplicating things for ProTV's purposes.
  - This is not getting rid of the sound only texture entirely, it still remains configurable via the TVManager.

### Deprecated
- [ Shader ] `ProTVHelpers.cginc` is deprecated and all functionality moved to `ProTVCore.cginc`.
- [ Shader ] `ProTV/FullScreen` is deprecated and all functionality added to `ProTV/VideoScreen`.
- [ Shader ] `ProTV/GlobalScreen` is deprecated and all functionality added to `ProTV/VideoScreen`.

## 3.0.0-beta.26.1 (2025-05-27)
### Added
- [ Core ] New `_ChangeTitle` API for explicitly changing the title.

### Changed
- [ Core ] Tuned the conditions for forcing a refresh
  - Mitigates rare edge case where the video might restart unintentionally.

### Fixed
- [ Playlist ] Guard against null pointer exception when trying to save a playlist.

## 3.0.0-beta.26 (2025-05-26)
### Added
- [ Core ] Add various Set methods for updating other TV component references.
  - All TVPlugins: `_SetTV(TVManager)` (underscore is intentional) which handles updating the event listener references.
    - This causes the `_TvReady` event to be triggered on the new TV instance.
  - Playlist/History/MediaControls: `SetQueue(Queue)` for updating the respective queue references. This can be set to null if you wish.
  - PlaylistUI/HistoryUI/QueueUI: Respective `SetPlaylist(Playlist)`/`SetHistory(History)`/`SetQueue(Queue)` for changing which plugin reference the UI represents. 

### Changed
- [ Playlist ] Moved all editor-only serialized-required data to a separate non-udon script which is excluded from the build.
  - This guarantees that the data stored for editor-purposes-only does not bloat the world size.
- [ Dependency ] Updated ArchiTech.SDK minimum to 0.21.0

### Fixed
- [ Core ] Undo/Redo actions should properly update the Related UIs list for TVPlugins.

## 3.0.0-beta.25.1 (2025-05-13)
### Changed
- [ Core ] Updated the placeholder image references type from `Texture2D` to `Texture`.
  - This enables the use of RenderTextures in place of the default images.
  - Note: while this also allows the use of `Texture3D`, remember that it will only sample the 0th index of that 3d texture (afaik).

## 3.0.0-beta.25 (2025-05-13)
### Added
- [ Core ] New shader setting `Anti-alias Edges` for whether the edge anti-alias fading is enabled.
  - If the video texture has black or transparent edges that you don't want, disable this setting.
  - For Custom Materials output that use the `ProTV/VideoScreen` shader, it's in the shader's inspector for the material.
  - This setting will not affect Custom Materials using any other shader, no edge antialiasing is done for those.
  - For Custom Texture output, you can find it under the `RenderTexture Update Settings` foldout.
- [ Core ] Add API methods for manipulating the custom materials list during runtime.
  - `_AddCustomMaterialTarget(Material, string)`, pass in the material and material texture property you wish to receive the TV's video texture.
  - `_RemoveCustomMaterialTarget(Material)`, pass in the material to remove. If not in the list, nothing happens.
  - Note: as with all custom materials, the video data matrix will always be explicitly assigned to the `_VideoData` property.
- [ Shader ] Add `Use Global Texture Instead` toggle to the `ProTV/VideoScreen` and `ProTV/FullScreen` shaders.
  - This is for whether the material should be assigned to by an individual TV via `_VideoTex`, or if it should read from the shader global `_Udon_VideoTex`.
  - This unifies the logic from the ancient GlobalScreen shader.
- [ Shader ] Add missing helper methods to `ProTVHelpers.cginc` for the flags property (`_11`):
  - `IsLocked`, `IsMuted` and `IsLoading`

### Changed
- [ Playlist ] Clean up handling of `Playlist` storage to mitigate issues with Unity that happen with extreme playlist sizes.
  - Force `PlaylistData` to never be allowed inside a prefab.
  - Prevent importing playlists during prefab editing.

### Deprecated
- [ Shader ] `ProTV/GlobalScreen` is deprecated as all relevant logic has been rolled into the main VideoScreen shader.

### Removed
- [ Shader ] `ProTV/VideoScreen2` functionality was rolled into the main shader a long time ago.

### Fixed
- [ Playlist ] Source text file should no longer be included in the build unintentionally.
- [ Queue ] Guard against a rare OutOfBounds exception.
  - This would cause the queue to crash in long-running instances where many different users queued items.
- [ History ] Fix null pointer exception for HistoryUI on build when history field is missing.
- [ Shader ] Fix some cases where loading shader flag was erroneously unset.
- [ Shader ] Add missing stereo instancing macros to the `ProTV/FullScreen` shader.

## 3.0.0-beta.24.2 (2025-04-07)
### Changed
- [ Playlist ] Adjust logic for `Autoplay On Load` to not require `Autoplay Enabled`.
  - `Autoplay On Load` is used for whether the playlist should immediately attempt to play something when the instance master joins.
  - `Autoplay Enabled` is for whether the playlist should consider the next entry in its list when media has ended.
- [ Playlist ] Improvements to handling of PlaylistData editor logic.
  - If prefab editing is detected, PlaylistData will be prevented from being added.
- [ History ] Fix null pointer error in HistoryUI.
- [ History ] Add Error message for when the list container object is missing.
- [ Queue ] Add Error message for when the list container object is missing.
- [ Misc ] Fix rare null reference exception when entering playmode.

## 3.0.0-beta.24.1 (2025-03-27)
### Changed
- [ Playlist ] When in prefab edit mode, prevent auto-generation of PlaylistData object.
- [ Playlist ] When in prefab edit mode, prevent playlist importing to avoid the massive serialization lag related to prefabs in extreme use-cases.

### Fixed
- [ Playlist ] Playlist no longer takes unreasonable amount of time to import extremely large playlists.
  - Removing the PlaylistData from the prefabs fixed the issue. Thanks Unity!
  - Basically DO NOT SERIALIZE EXCESSIVE AMOUNTS OF DATA INTO OBJECTS IN A PREFAB. Only store extreme data in a non-prefab object.

## 3.0.0-beta.24 (2025-03-23)
### Added
- [ Core ] Add automatic material creation when multiple TVs are detected to be targeting the same materials.
  - On Build, entering Play mode, or via `Tools/ProTV/Open Build Checks Window` menu), the build checks will run which will detect material collision.
  - The user will then be prompted to either proceed or skip. If the user skips, they will trigger the error.
  - Upon proceeding, each TV will be checked in order, and if the materials used by the TV match any previously checked TV, 
    the material will be duplicated into `Assets/ProTV/Materials` folder and the custom material will be updated.
  - Any MeshRenderers that are children of the TV will be automatically searched for matching materials and update the material reference as well.
- [ Playlist ] Add new `PlaylistUI` component.
  - This will allow you to hook up multiple UIs to the same playlist component for easier reuse.
  - Old playlist migration will automatically happen on build.
  - You can also manually trigger it with `Tools/ProTV/Update Scene`.
  - If you are using a `Playlist` prefab from the asset, it should automatically migrate.
  - If you are using a playlist fine (which you should be) you can validate everything by clicking the `Import` button once more.
- [ Playlist ] Add configuration support for the playlist header.
  - The PlaylistUI component now tracks the text/tmp component of the header text.
  - This will be dynamically updated in editor via inspector field and during runtime via API.
  - There is a new `UpdateHeader(string)` API for changing the header. NOTE: THIS API IS NOT SYNCED, you will have to sync the value in your own script.
- [ Playlist ] Add alternate JSON playlist file structure support.
  - This enables an alternate json format you can use for constructing playlists more easily via code.
  - When importing a playlist, json format is automatically detected (via `StartsWith('{')`) and will be implicitly handled.
  - New `Json` button by the `Save` and `Copy` buttons which will copy the JSON structure into your clipboard instead of the custom playlist format.
- [ Playlist ] Add remote playlist importing.
  - Available in the new dropdown section, this give a text field for pasting a URL.
  - This is handled in stages:
    - Url text input, check for existing match in the download cache and display known entry count if found.
    - Download button becomes available. Click to download into the cache folder `Assets/ProTV/RemotePlaylists`. Button will show download progress for large files.
    - Once download completes, Import button becomes available. This takes the cached download and updates the playlist's data with the new data.
- [ Playlist ] Add support for an optional default image to be used when an entry does not provide a valid image path.
  - This can be overridden per `PlaylistUI` component. 
- [ Playlist ] Add Editor API methods for marshalling playlists between text and data form.
  - The `Parse` API(s) are for reading playlist in text file form and extracting the tuple array data from it.
    `Parse` implicitly handles it, but if you want to explicitly call it, the `ParseJson` can be called to explicitly handle the Json structure.
  - The `Pickle` API takes a `Playlist` component and returns the playlist text file representation of that.
  - The `PickleJson` APi takes a `Playlist` component and returns the json variant of the playlist file output.
  - The `UnPickle` API takes a playlist text file representation and applies the data (via `Parse`) to the given `Playlist` component.
- [ Playlist ] Added Autoplay toggle buttons to playlist uis.
    - This dynamically updates across all connected `PlaylistUI`s.
    - The related inspector layout mimics what is commonly seen in the `MediaControls` for configuring toggle buttons.
- [ Queue ] Add auto-detection of connected `QueueUI` components to the bottom of the inspector, similar to Playlist.
- [ Queue ] Header text field is configurable from the `Queue` component, propagates to connected UI components.
- [ Queue ] Add option to append the current entry count into the header text field.
- [ MediaControls ] Explicit error message when RTSP/RTMP is used instead of the necessary RTSPT protocol.
- [ History ] Add new `HistoryUI` component.
    - This will allow you to hook up multiple UIs to the same history component for easier reuse.
- [ History ] Add auto-detection of connected `HistoryUI` components to the bottom of the inspector, similar to Playlist.
- [ History ] Header text field is configurable from the `History` component, propagates to connected UI components.

### Changed
- [ Dependency ] Enforce requiring ArchiTech.SDK 0.19.0 or later.
- [ Core ] Use new ArchiTech.SDK features to simplify some inspector logic.
- [ Core ] `TVPluginUI.OUT_MODE` has been renamed to `TVPluginUI.OUT_INDEX`
- [ Core ] `TVPluginUI.OUT_VALUE` type has been changed from `int` to `float` (type was incorrect previously)
- [ Core ] Rework build script logic to more correctly handle when build logic runs.
- [ Playlist ] Update playlist to enforce using the `PlaylistData` to store entry data, include migration logic from old storage logic.
  - Any interaction with a playlist will force migration, including on build.
- [ Playlist ] Change the import mode from a toggle to a dropdown selection.
- [ Prefab ] Playlist prefabs have been updated to utilize the new header reference.
- [ Prefab ] Update the TV prefabs to better support mono-audio media (modifies the default speaker configurations).
    - Switch to `AVPro Compatibility` via media controls and then switch audio to `2d` to fix the "audio only in left ear" issue.
    - Currently only supported by `Simple` and `Home Theater` prefabs
    - `Live Events` and `Skybox` prefabs assumes mono-audio to be an external problem.
- [ Shader ] Separated helper function logic into a separate cginc file for convenience to other shaders.

### Removed
- [ Playlist ] No longer checked for pre-approved urls due to limitations with networking preventing secure validation of the approval.

### Fixed
- [ Core ] Fix issue where randomly the `Playlist` would lose the playlist file reference.
- [ MediaControls ] Fix NPE when a seekbar is present but not a seek offset, and seek offset change is attempted.
- [ Playlist ] Fix template select action button losing all user-defined event listeners.
- [ Queue ] Speculative fix for soft-lock that happens in high usage multi-user scenarios.
- [ Auth ] Fix TVManagedWhitelist not correctly updating for the first user in the instance.

## 3.0.0-beta.23.1 (2024-12-21)
### Changed
- [ Misc ] Restore some old API usage in deprecated classes to improve compatability with old 2.3 scripts.

## 3.0.0-beta.23 (2024-11-23)
### Added
- [ Core ] Add getter `IsInitialAutoplay` for checking if the URL was loaded automatically from the `TVManager`'s autoplay fields.
  - This getter is only true if the URL is the first url ever to be loaded AND the url matches the TV's autoplay URL. All other times will return false.
- [ Playlist ] Add option `Enable Autoplay on Custom Media`.
  - This option is exclusively opposite from `Disable Autoplay on Custom Media`. You can only have one option active per component instance.
- [ MediaControls ] Rename `Seek` to `ChangeSeek` and `Refresh` to `RefreshMedia` for naming consistency.

### Changed
- [ MediaControls ] Include missing flag options `Show Media Owner` and `Show Remaining Time` in the inspector.
  - They existed, but weren't shown in the editor.
- [ MediaControls ] Rename internal `currentTime` component variable to `currentTimeDisplay` (and TMP variant) for naming consistency.
- [ MediaControls ] Rename internal `info` component variable to `infoDisplay` (and TMP variant) for naming consistency.
- [ Misc ] Update some inspector verbiage.
- [ Misc ] Reorder some options in the component inspectors.

### Deprecated
- [ MediaControls ] Deprecate method `Seek` in favor of `ChangeSeek` for naming consistency.
- [ MediaControls ] Deprecate method `Refresh` in favor of `RefreshMedia` for naming consistency.

### Fixed
- [ Core ] Properly fix issue where changing autoplay on custom media would fire under unexpected conditions.
  - This should resolve any remaining issues surrounding `Disable Autoplay on Custom Media`.
- [ Misc ] Fix null error crash on TVToggles when TV reference is missing.

## 3.0.0-beta.22 (2024-11-18)
### Added
- [ Core ] Add TVPlugin events _TvPlaybackStart and _TvPlaybackEnd.
  - These events trigger in an alternating pattern, meaning when one of these events trigger, it cannot trigger again until after the other one has.
  - _TvPlaybackStart happens after _TvPlay.
  - _TvPlaybackEnd happens after _TvStop/_TvMediaEnd/_TvVideoPlayerError.
  - Pause is not considered playback ending. Use _TvPause event for that state.
- [ Core ] Add missing support for proper addedBy name, specifically supporting the Queue declaring it being added by another user.
  - Add IN_NAME udon variable for the addedBy support.
  - This variable is used at the same point as IN_MAINURL/IN_ALTURL/IN_TITLE are.
- [ Core ] Add missing _ChangeSeekOffset event.
  - Use existing IN_SEEK with this event in Udon systems.
- [ Core ] Add Missing _ChangePlaybackSpeed event.
  - Add IN_SPEED udon variable for this event in Udon systems.

### Changed
- [ Core ] Invert the perspective of the tweaks flag to be "enabling local" instead of "disabling synced" tweaking.
  - The only user-facing change this has is in the Sync Tweaks section, the label is changed from "Disable Synced Tweaks"
    to the label "Enable Local Tweaks".
  - The default is false, this makes it so that any sync tweaks that are enables, are automatically sync enforced.
  - This is a change from before where sync enforcement was disabled by default.
- [ Prefab ] Move LiveEvents prefab to the top level of the asset to be more easily discovered.

### Fixed
- [ Playlist ] Add null check for when the scrollview is missing a vertical scrollbar.
- [ Playlist ] Add null check for rare edge-case where title is missing during UI redraw.
- [ Playlist ] Fix edge case where autoplay unexpectedly ended after the first video when join autoplay is enabled.

## 3.0.0-beta.21.1 (2024-09-25)
### Fixed
- [ Core ] Fix infinite domain reload loop caused by improper version define handling logic.
  - Discovered as a consequence of AudioLink updating to 2.0.0
  - Should also resolve compiler issues when the VPM Resolver package is removed from the project.

## 3.0.0-beta.21 (2024-09-25)
### Added
- [ Core ] Add Gamma Zone option to the RenderTexture Target settings.
  - This enables the gamma correction control for the user-provided RenderTexture.  
    This is useful when you need to integrate certain systems, like LTCGI + VRSL.
- [ Core ] Add internal string label to the triggerRefresh action so log statements knows what part of the code caused a refresh. Traceability improvement.
- [ Core ] Add TVManager option `Default TV Settings -> Start with Audio Muted`.
- [ Core ] Add TVManager option `Misc Options -> Enable Local Pause` for those who don't want non-owners to be able to pause locally.
- [ Core ] Add _ChangeMedia method overload that only needs the mainUrl.
- [ Playlist ] Add option `Enable Autoplay On Interact` which makes clicking on a playlist entry activate autoplay.
- [ Playlist ] Add option `Disable Autoplay On Custom Media` which implicitly deactivates autoplay when a URL is loaded that is not in the playlist.
    - Combining these two options turns the playlist into a 'toggle' kind of system where when people want to use the playlist, they can click to start automatically playing,
      but when someone enters their own video, the playlist autoplay kind of 'pauses' until a user selects another entry on it.
- [ Misc ] Add TVToggles script which manages game object and collider states based on certain conditions.
  - Currently, it only tracks Super/Auth/UnAuth users for toggling the state.

### Changed
- [ Core ] Serialize internal and global textures to the debug inspector for advanced users.
  - Don't mess with these unless you know what you are doing.
- [ Playlist ] Improve custom editor organization and readability.
- [ Playlist ] Move sample playlists from Resources to Samples folder.

### Fixed
- [ Core ] Fix OnEnable not considering mediaEnd state when checking whether it should resume media via play.
  - This fixes the 'Video restarts when owner re-enables the video player' issue.
- [ Core ] Fix edge-case where loop state would not be retained properly during certain ownership changes.
- [ Core ] Fix Play State Takes Ownership not respecting local pausing option correctly.

## 3.0.0-beta.20.3 (2024-09-14)
### Changed
- [ Core ] Improve the wording in certain log statements.
- [ Queue ] Increase the queue max limit range slider to 100 in the inspector.
  - You can still set a higher value by using the debug inspector mode if you wish.

### Fixed
- [ Core ] Fix t= parameter not working.
- [ Core ] Fix non-owners never receiving the _TvMediaEnd event.
  - This also fixes AudioAdapter not resuming world audio on media end for non-owners.
- [ Core ] Fix playback speed being set to half when re-enabling the VPManager.
- [ Core ] Fix constant reloading for livestreams that weren't RTSP based.
- [ Core ] Fix first reload after a new url is input not keeping time continuity.
- [ Core ] Fix late-joiner time sync edge case.
- [ Core ] Whitelist check was not considering pre-approved urls.
- [ Core ] Fix manual loop trigger soft-locking the internal update logic.
- [ Core ] Reset jumpToTime to 0 if a VPManager swap fails.
- [ AudioAdapter ] Fix NPE in AudioAdapter log statement.
- [ Auth ] Fix null array copy crash in managed whitelist UI.
- [ Auth ] Fix UI alignment issue of the Footer element. (fixed by @XenIneX)

## 3.0.0-beta.20.2 (2024-08-01)
### Fixed
- [ Core ] Fix legacy udon method call related to older SDK versions... Again...

## 3.0.0-beta.20.1 (2024-07-25)
### Fixed
- [ Core ] Fix legacy udon method call related to older SDK versions.

## 3.0.0-beta.20 (2024-07-08)
### Added
- [ Core ] Add property handlers for the standby textures.
  - `StandbyTexture`, `SoundOnlyTexture`, `LoadingTexture`, `VideoErrorTexture`
  - To get or set the textures in U#, you should use these instead of the internal field names.

### Changed
- [ Core ] Improve reliability of the RTGIUpdater script.
- [ Queue ] Minor re-organization of the Queue custom inspector fields.

### Fixed
- [ Core ] Fix RTSPT streams reloading in perpetuity.
- [ Core ] Force blit update to happen every frame (why was it set to every 2? no clue...).
- [ Core ] Fix weird internal state when swapping to a video player swap option that fails to load.
- [ Shader ] Prevent a shader compiler error occurring in certain scenarios when fog is enabled.

## 3.0.0-beta.19 (2024-06-29)
### Added
- [ Core ] Add option to change the reload keybind key. Defaults to F5.
- [ Queue ] Add burst throttling to Queue to help prevent user input spam.
- [ Queue ] Add option to Queue to enable preventing unauthorized users from swapping entries.
- [ Shader ] Add logic to disable the RTGIUpdater script on mobile platforms for performance.
  - There is a flag to allow it to run on mobile platforms that is disabled by default.

### Changed
- [ Core ] Rename internal waiting field to waitingForMediaRefresh for naming clarity.
  - Technically is a public field, but is considered an internal flag.
  - Any third-party script using this value should switch to using `TVManager.IsLoadingMedia` for stability.
- [ Core ] Rename TVManager.LoadingMedia to IsLoadingMedia for naming consistency.
- [ Core ] Rename TVManager.WaitingForMedia to IsWaitingForMedia for naming consistency.
- [ Core ] Rename TVManager.Buffering to IsBuffering for naming consistency.
- [ Core ] Update TVManager inspector misc options text for clarity.
- [ Core ] Move the _TvTitleChange event to occur just before _TvMediaReady instead of just after _TvMediaChange to fix some unexpected title swapping behaviour.
  - This should make the title display more consistently accurate as well to what is actively playing.
- [ MediaControls ] Adjust handling of input hiding, will now only hide inputs if no text is present in any of them.
  - This helps mitigate some usability issues when multiple people are trying to enter content.
- [ Shader ] Update global texture getter to fall back to the internal texture when not available.

### Deprecated
- [ Core ] Deprecate `TVManager.LoadingMedia` in favor of `TVManager.IsLoadingMedia` for naming consistency.
- [ Core ] Deprecate `TVManager.WaitingForMedia` in favor of `TVManager.IsWaitingForMedia` for naming consistency.
- [ Core ] Deprecate `TVManager.Buffering` in favor of `TVManager.IsBuffering` for naming consistency.

### Fixed
- [ Core ] Fix for edge cases where the global texture wasn't being updated properly.
- [ Core ] Update internal syncTime flag for when a skip is triggered to -1 instead of INF.
  - This should resolve some edge cases with live media where the video engine unexpectedly returns INF as current time.
- [ Core ] Add logic to implicitly reload current media when OnVideoPlayerEnd is triggered and the media was live.
  - This is a tentative mitigation for livestreams that die, but for some reason trigger the video player end event instead.
- [ Core ] Fix edge-case where, when the owner stopped a video and someone joined with disable on start active, then enabling the TV would not properly respect the owner's play state.
- [ Queue ] Modify the active loading bar logic to avoid certain null pointer issues.
- [ Shader ] Fix shader global illumination setup to properly assign the GI flags to the material.

## 3.0.0-beta.18 (2024-06-19)
### Added
- [ Core ] Expose VPManager.IsAVPro as a public getter.
- [ Core ] Add VRSL Presets support for 480p mode.
- [ Core ] Add security tweaks to allow super-users or authorized users to always log Trace for easy auditing post-mortem.
    - This feature is enabled by default.
- [ Core ] Add blit check to enable standby texture when the tv object gets disabled.
- [ Core ] Formalize exposure for texture getters: GlobalTexture, CustomTexture, InternalTexture, SourceTexture.
    - SourceTexture is the tex that comes directly from the video engines without any modifications.
    - InternalTexture has had texture transform and gamma corrections applied. This is the same texture that is passed into the material targets texture slots.
    - CustomTexture is the render texture that is provided explicitly by the creator via inspector. This texture is used commonly with LTCGI/AreaLit/etc.
    - GlobalTexture is the optional (might be null) texture that will exist if the [Avatar Support] texture transform mode has bake mode enabled.

### Changed
- [ Core ] Formalize exposure for corresponding tiling offset getter: SourceTextureST.
  - This is the tiling/offset vector that couples with SourceTexture.
  - If you are using the SourceTexture for anything, you must include using the SourceTextureST so to correct any video engine transform oddities.
- [ Shader ] Consolidate shader logic to use the ProTVCore.cginc include.
- [ Shader ] Deprecate VideoScreen2 shader.
  - All logic has been finalized and moved into the main VideoScreen shader, so just use that one.
- [ Shader ] Cleanup ProTVCore.cginc include to handle additional use-cases.

### Deprecated
- [ Core ] Deprecate RawTexture getter in favor of the InternalTexture getter.

### Fixed
- [ Core ] Fix URL desync that could happen when tv owner switches videos too fast.
- [ Core ] Change the preset values for VRSL 720p to the correct size.

## 3.0.0-beta.17.1 (2024-06-17)
### Added
- [ Core ] Add support for negative values for Texture Transform By Pixels mode.
  - When size or origin is negative, the values are calculated from the opposite edge.
  - eg: size `x = 0` and `y = -208` means that the bottom 208 pixels will be excluded from the resulting area.
- [ Core ] Add VRSL presets to the Texture Transform selection.
  - This adds explicit support for all 4 VRSL DMX data modes (horizontal/vertical and 720p/1080p modes)

### Changed
- [ Core ] Remove the Start Hidden option in the inspector as it's no longer useful, retained internally for compatibility.

### Fixed
- [ Core ] Fix Plugins being unable to load media after the TV has been enabled if there was never a URL loaded yet.
  - This should solve some issues with the Queue and Playlist involving disabling/enabling the TV object.
- [ Core ] Fix Start Disabled option to trigger after the TV has finished its internal initialization.
  - This should solve a number of the odd behaviours that happen involving disabling/enabling the TV object.
- [ Core ] Fix stop button call while loading reflect the correct action state if there is no media actively playing.
- [ Misc ] Fix fullscreen not passing the internal shader data correctly.
  - This fixes the issue where fullscreen was not respecting 3d modes.

## 3.0.0-beta.17 (2024-06-13)
### Added
- [ Core ] New setting for controlling the brightness of the RenderTexture Target.
- [ Core ] Add Gamma Zone control for explicitly specifying what portion of the video should have the AVPro gamma adjustment applied to.
  - This feature is generally intended for scenarios involving embedded stream data, like VRSL or shadermotion.
- [ Core ] Add option for specifying the 3D Mode width for standby textures.
- [ Core ] Add autoplay loop option for convenience.
- [ Core ] Add Queue option for enabling adding entries to the queue while the TV is locked.
- [ Core ] Add TVPlugin api for checking if a URL is pre-approved to allow unauthorized object owners to play a video, useful for playlists.
  - This makes it so that when an unauthorized user is in control of the TV, it won't prevent urls from playing as long as one of the connected plugins verifies that it is a valid URL.
- [ Shader ] Add Front/Backface culling option to the standard ProTV Shaders.
- [ Misc ] Add new 3D logo. Thanks MissStabby!

### Changed
- [ Core ] Move _Blit function from VPManager to TVManager to simplify some things.
  - This removes a lot of caching to enable dynamically changing certain things related to blit, such as the standby textures.
- [ Core ] Make a most of the methods for the build checks public, so they can be called from any necessary script.
- [ Core ] Improve autoplay detection for the automatic autoplay offset.
- [ Core ] Extract method for populating tv dropdown selector to a public static for anyone to use.
- [ Core ] Force retryCount to 1 if 0 and isLive after media has successfully loaded.
  - This helps resolve stability issues with live media failing repeatedly.
- [ Core ] Force media to always start playing when media is live.
- [ Core ] Improve the structure of the auth check logs for better auditing.

### Fixed
- [ Core ] Fix null pointer that could happen when dealing with a UI shape that doesn't have a box collider.
- [ Core ] Catch exception that can happen with accessing a missing fillrect of a slider.
- [ Core ] Fix exception that can happen on playmode exit that caused the udon graph editor to freeze as a side-effect.
- [ Core ] Remove old logic preventing certain deserialization logic to run while loading media to fix certain edge cases.
- [ Core ] Fix media still triggering a play after hitting stop during media loading.
- [ Core ] Fix stop not being correctly enforced for late-joiners under desired scenarios.
- [ Playlist ] Fix search crashing when a playlist is disabled on build and has yet to be enabled while Search Hidden Playlists is active.
- [ Playlist ] Fix search percent display not reaching 100% as expected when searching hidden lists where the search is fully processed within a single frame.
- [ Shader ] Skybox shader should no longer have seam artifacts where the edges meet.
- [ Shader ] Include passing video data to the fullscreen shader.

## 3.0.0-beta.16 (2024-04-21)
### Added
- [ Core ] New sync tweak `Sync Video 3D Mode`.
  - This TVManager setting makes the 3d mode value and 3d Wide flag be respected on remote clients.
- [ Core ] New TVManager Media Load setting `Implicit Replay Duration`.
  - Media will implicitly loop once if the duration is shorter than the specified time (in seconds). Set to 0 to disable.
  - This value used to be built into the logic with 30 seconds. It has been converted to a setting and now defaults to 15 seconds.
- [ Core ] New standby texture options: `Loading Media` and `Video Error`
- [ Core ] Add video texture previews to the TVManager inspector while in playmode.
  - You can view them with a toggle at the bottom of the `Rendering Options` section
- [ Core ] New TextureTransform controls for declaring what portion of the global texture should be exposed to avatars.
  - This group of settings (only available when the [Avatar Support] option is enabled) defines the value of 
    the `_Udon_VideoTex_ST` vector, which shall be used by shaders to trim the video texture based on the world's specification.
  - `As-Is` mode does not transform the texture at all. It's there mainly to signify that the transform logic should be skipped.
  - `Normalized` mode is effectively the same as what you'd see in a shader inspector with the `Tiling` and `Offset` values.
  - `By Pixels` mode allows you to specify, in pixels, the portion of the global texture to be visible. 
    - Internally the values get converted into a Normalized format.
    - The `Pixel Origin` value is calculated from the Top-Left corner of the texture.
  - Note: If you _absolutely_ need to keep a portion of the texture off of avatars (some shaders don't implement the _ST vector correctly),
    you will need to enable `Bake Global Texture`. This causes the internal texture to be Blit into a second internal render texture
    with the portion trimmed as desired. **_Do be warned_** that baking the texture will involve an additional RenderTexture, costing some performance
    and memory space.
  - If you bake the texture, the `_Udon_VideoTex_ST` property will be set to the default value of `(1, 1, 0, 0)`.
  - The purpose of this feature is to handle situations where the video has a special section of it that should be used only for avatars,
    or if there is additional embedded data in the video that needs to be hidden (eg: DMX data for VRSL).

### Changed
- [ Core ] Renamed some internal udon fields. You will need to update usage if you relied on these fields directly.
  - `mode3d` -> `video3d`
  - `width3dFull` -> `video3dFull`
- [ Core ] Property `IsStopped` will now also check for the waiting play-state to handle init-stage edge-cases.
- [ Prefab ] Updated UI materials to the VRChat super-sample shader, this time properly with a custom material.

### Removed
- [ Core ] Fully removed the remaining usage of the deprecated 3d comfort adjust (aka spread3d) feature.

### Fixed
- [ Core ] Improved handling of autoplay to fix Queue prefill from a Playlist not auto-starting.
- [ Playlist ] Fix show URLs setting not being respected in all scenarios.
- [ Shader ] Fix the aspect trimming for 3d videos in the global screen shader.

## 3.0.0-beta.15 (2024-04-09)
### Added
- [ Core ] Add methods `_EnableGlobalTexture`, `_DisableGlobalTexture`, `_ToggleGlobalTexture`
  - These methods replace the `_EnableGSV`, `_DisableGSV`, `_ToggleGSV` methods.
- [ Misc ] Add AssemblyInfo file for the editor folder.

### Changed
- [ Core ] Rename property `Remember First Master` to `Allow First Master Control` for consistency.
- [ Core ] Rename property `Pause Takes Ownership` to `PlayState Change Takes Ownership` as it includes Play and Stop in the action as well.
- [ Core ] Update internal avpro speaker creation to allow specify which channel mode to use.
- [ Core ] Include tracking current master and instance owner names.
- [ Core ] Improve detail of ownership transfer log statement for auditing.
- [ Core ] Properly decouple allow first master control option from the allow master control option.
- [ Prefab ] Move QueueUI from the Helpers subfolder to the parent Plugins folder.
- [ Prefab ] Update prefabs with the new VRChat super-sampled UI material.
- [ Shader ] Include shadow-caster pass to the video screen and global screen shaders.

### Deprecated
- [ Core ] Obsolete methods `_EnableGSV`, `_DisableGSV`, `_ToggleGSV`
- [ Plugins ] Restore missing deprecated `_TvLoadingStop` method.

### Removed
- [ Core ] Remove usage of 3d spread value as it's no longer being used.

### Fixed
- [ Core ] Fix UnityVideo not looping for remote users.
- [ Core ] Fix UnityVideo not loading sometimes for remote users when playing threshold is less than the current time of the video owner.
- [ Core ] Fix UnityVideo not handling seek reliably.
- [ Core ] Fix video swap not respecting the stopped play state.
  - It should properly swap video managers without forcing an immediate reload if the player is stopped.
- [ Core ] Fix script upgrades to not force it when they are part of a prefab.
- [ Core ] Fix rare NPE edge-case in TVManager inspector.
- [ Core ] Autoplay video defined on the TVManager should now be loaded before any other autoplay.
  - Leave empty to skip, as usual.
- [ Queue ] Fix queue attempting to play media when media is loading.
- [ Misc ] Change the access level for custom inspectors.

## 3.0.0-beta.14.6 (2024-03-29)
### Added
- Include root namespace in asmdef file.
- Add check for version number game object which will include the protv name if the term prefix is present in the gameobject name.
- Add editor script getter for pulling the protv version string `ProTVEditorUtility.Version`.
- Add AssemblyInfo file based on unity's documentation.

### Changed
- Convert some remaining auth type references to TVManagedWhitelist.
- Put exception catch in build requested checks which will trigger an decision dialog when an unexpected error occurs.

### Fixed
- Remove specific script upgrade build check that has been causing problems.
- Fix fullscreen script crashing when missing the TV object.
- Add check in Queue to prevent errors causing unexpected skipping for other users.

## 3.0.0-beta.14.5 (2024-03-25)
### Changed
- [ Core ] F5 refresh button will be ignored if Shift is pressed to avoid conflict with Avatar Gestures.
- [ Auth ] Change TVUsernameWhitelist to inherit from TVManagedWhitelist to avoid some upgrade bugs.

### Fixed
- [ Core ] Fix exception throwing when checking for the gsvfix on build when no TVs are in the scene.

## 3.0.0-beta.14.4 (2024-03-23)
### Changed
- [ Core ] Update field name `Global Shader Variables` to `[Avatar Support] Global Video Texture` for clarity of purpose for the flag.
- [ Core ] Update field name `Enable Auto-Ownership (Experimental)` to `[Experimental] Enable Auto-Ownership` to make it more clear that the feature flag is not yet stable. 
- [ Core ] Improved reliability of the GSV flag.

### Fixed
- [ Prefab ] Restore some lost prefab reference data.

## 3.0.0-beta.14.3 (2024-03-22)
### Fixed
- [ Core ] Fix regression bug of standby textures not showing.
- [ AudioAdapter ] Should now correctly respect the TV's mute state.

## 3.0.0-beta.14.2 (2024-03-21)
### Changed
- [ Misc ] Update default domain whitelist options.

### Fixed
- [ Auth ] Fix TVManagedWhitelistUI not registering to the provided whitelist.
- [ Shader ] Further cleanup for the anti-aliasing shader logic.
- [ AudioAdapter ] Adapter should now account for worldAudioResumeDuringSilence during all necessary tv events.

## 3.0.0-beta.14.1 (2024-03-21)
### Fixed
- [ Playlist ] Add compile flag check for 2019 unity for a specific API call.

## 3.0.0-beta.14 (2024-03-21)
### Added
- [ Core ] Add sync tweak for syncing the audio mode.
- [ Core ] Add loop state sync.
- [ Core ] Add sync tweak option for enforcing the state of sync tweaks.
  -  This means that instead of the default behaviour where the tweak is synced only on change (allowing the non-owner to change the values locally), it forces non-owners to conform to the owner's state of the sync tweaks.
- [ MediaControls ] Add loading spinner reverse flag and speed value.
  - Helps customize the look of the loading spinner animation.
- [ MediaControls ] Add option to skip clearing/resetting input field data.
- [ Playlist ] Add option to specify a title-only entry as interactable when the title begins with a `~` (tilde)
  - This enables creating a playlist with custom title entries that you can send to the TV. Useful for dynamically updating the TV's title with pre-made options during a performance.
  - The default title behaviour makes the entry non-interactable, as if it was a section header.
  - In the .playlist files, to enable interaction on a title-only entry, you will need the line to start with 2 tildes `~~`, first signifying the title line, second signifying that it should be a clickable entry.
  - In the inspector editor, the title field needs to be prefixed with 1 tilde `~` to signify it should be a clickable entry.
- [ Queue ] Add queue option for being able to add to the queue even while it's locked.

### Changed
- [ Core ] Improve reliability of the Custom Textures preview menu action.
- [ Auth ] Split the UI logic from the functional logic for TVManagedWhitelist.
  - Similar to the prior logic split of the Queue, if you unpacked the prefab you will need to integrate the new TVManagedWhitelistUI component, or grab a fresh copy of the prefab.
- [ Misc ] Custom Texture preview menu can be disabled/cleared to view the scene without the preview.
- [ Misc ] Change the audio source priority for speakers to 16 to avoid audio clipping in certain edge-cases.

### Deprecated
- [ Auth ] TVUsernameWhitelist is redundant to TVManagedWhitelist, thus has been deprecated.
  - You can upgrade any use of the TVUsernameWhitelist by right clicking the component header in the inspector and selecting `Upgrade Component to TVManagedWhitelist`.

### Fixed
- [ Core ] Synced volume control should work correctly again.
- [ Core ] Swapping VPManagers should correctly retain the current time again.
- [ Core ] Speakers should no longer be soft-lock muted when trying to swap VPManagers then toggle audio modes.
- [ Core ] When ownership is changed externally via SetOwner, the sync data script will now correctly match the owner.
- [ Playlist ] Playlist should no longer make redundant copies of the PlaylistRPC helper script on build.
- [ Playlist ] Prevent playlist from triggering unexpected videos from non-tv-owners.
  - This fixes the issue where if a user fails to load a video on join, it might force a video to play or get queued unexpectedly.
- [ Shader ] Fix VideoScreen shader anti-alias logic to properly support the uvClip for 3D modes.
  - This fixes the issue where there would be bleed from opposite eyes with 3D videos.

## 3.0.0-beta.13.2 (2024-02-25)
### Fixed
- [ Playlist ] Fix regression bug with `Prioritize on Interact`. Should work again.

## 3.0.0-beta.13.1 (2024-02-25)
### Added
- [ Shader ] Add anti-aliasing to the aspect ratio border (letter-box/pillar-box)

### Fixed
- [ Plugins ] Added missing auto-removal of deprecated UI events.
  - Fixes MediaControls, Playlist, Queue and History UI events being effectively called twice in the same frame, which broke things unexpectedly.

## 3.0.0-beta.13 (2024-02-24)
### Added
- [ Core ] Add option (enabled by default) to have the F5 keybind reload the TV from anywhere in the world.
- [ Core ] Add option to auto-reload the TV every given X minutes if the media is detected as a livestream.
- [ Core ] Add a base class for all TVPlugin UIs.
- [ Auth ] Add warning for ManagedWhitelist when the TV is not correctly connected.
- [ Shader ] Add experimental rework of the VideoScreen shader to include support for light probes and depth fog.
  - This includes an additional udon script for triggering the screen updates for the light probes.
- [ MediaControls ] Add option for default title input value.
- [ MediaControls ] Add UI references for Resync and Reload buttons to auto-assign the correct UI events.
- [ Queue ] Add QueueUI component for managing the UI logic.
  - This enables easy replication of the object so one Queue can be used from multiple locations.

### Changed
- [ Core ] Update certain settings to be stored as project-level instead of just session-level.
- [ MediaControls ] Add check in _TvReady to ensure the play/pause/stop are hidden while in the waiting state.
- [ Playlist ] Expose the storage reference in inspector and add logic for synchronizing playlists that use the same storage object.
- [ Queue ] Separate the UI logic from the main logic.
  - This enables the Queue's UI to be duplicated and all be in-sync with the main Queue script.
  - **THIS IS CONSIDERED A BREAKING CHANGE**
  - **THE QUEUE PREFAB HAS BEEN SIGNIFICANTLY MODIFIED TO FACILITATE THIS CHANGE.**
  - **YOU WILL NEED TO GRAB A FRESH COPY OF THE QUEUE PREFAB OR MODIFY YOUR EXISTING USAGE TO ACCOMODATE THE CHANGE.**
- [ Plugins ] Update plugin scripts that have no synced variables to sync mode `None`.
- [ Plugins ] Renamed the `_` prefix for all public methods on certain scripts.
  - Affects `MediaControls`, `Playlist`, `AudioAdapter`, `History`, `VPManager`
- [ Misc ] Remove VPM dependency on the resolver component and use ASM version defines instead.
- [ Misc ] Move scripts from the Runtime/Abstract folder to their respective folders.

### Deprecated
- [ Plugins ] All public methods in the plugin beginning with the `_` prefix (except `_Tv` prefix) have been marked as obsolete.
  - Affects `MediaControls`, `Playlist`, `AudioAdapter`, `History`, `VPManager`

### Fixed
- [ Core ] Include VPMResolver asm define to handle when the package is removed from the project.
  - This also removes the dependency on VPM resolver by making it optional in order to restore 2019 support.
- [ Core ] Add UI Shape Fixes check in case a VRCUiShape is not on a GameObject with a Rect Transform.
- [ Core ] Fix consistency issues with volume when Start with 2D Audio is enabled.
- [ Core ] Volume slider should now correctly propagate the _TvVolumeChange event correctly again.
  - This should also fix the global volume sync option not working as expected.
- [ Auth ] Add missing Reauthorization call to the _TvReady event.
  - This fixes the issue where when a user joins and they are authorized as part of deserialization, they correctly have they plugins update the auth state.
- [ Shader ] Fix fullscreen shader accidental double UV correction that resulted in incorrect aspect scaling.
- [ MediaControls ] Fix incorrect text sizing for some dropdowns.

## 3.0.0-beta.12 (2024-01-27)
### Added
- [ Core ] Add implicit setting of the isLive flag when certain url protocols are detected.
  - Currently checks for RTSP/RTSPT protocols.
- [ Branding ] Add parallax poster prefab for branding promotion.
- [ Misc ] Add help urls to package.json.

### Changed
- [ Core ] Improve support for the _Test material name prefix to prevent auto-removal under correct scenarios.
- [ Core ] Rename editor DrawCoreReferences method to DrawTVReferences.
- [ Core ] Rename editor SetupCoreReferences method to SetupTVReferences.
- [ Prefab ] Move promotional models prefabs from Resources to Samples.
- [ Prefab ] Update Monochrome prefabs to have color transition states.
- [ Misc ] Rename CONTRIBUTION.md to CONTRIBUTING.md and update contents.
- [ Misc ] Update license copyright year.

### Removed
- [ Misc ] Remove unused animations (these were moved to ProTV.Extras previously)

### Fixed
- [ Core ] Fix default standby texture being shown for audio-only livestreams instead of the sound-only texture.
- [ Core ] Mitigation for android phones having issues with MIPS when ANSIO is active.
- [ Core ] Fix rare scenario where a VPManager would draw to the wrong material.
- [ Core ] Fix late-joiners tripping a PlayerError condition when maxAllowedLoadTime is 0.
- [ MediaControls ] Fix controls not auto-filling the default url contents when controls is disabled by default.

## 3.0.0-beta.11 (2024-01-09)
### Added
- [ MediaControls ] Add alternate url and title inputs to the options page of the media controls prefabs.
- [ Misc ] Add new label icon.

### Changed
- [ Core ] Move arealit auto-setup button into the Rendering Options section.
- [ MediaControls ] Update MediaControls V1 and V2 Color prefabs to be variants of the Monochrome to reduce maintenance time of updating them.
- [ MediaControls ] Check for multi-input will check for both presence AND visibility.
  - This means that if you have both main and alt url inputs, but hide the alt, ending edit for the main url will trigger the change media automatically.
  - If any 2 of the MainURL/AltURL/Title fields are visible and connected, an explicit call to `controls._ChangeMedia` is required, usually via a Send button.
- [ Misc ] Consolidate desktop fullscreen prefab to a single game object.
- [ Misc ] Update desktop fullscreen to implicitly handle the material without needing to add it to the Materials Target list.
- [ Misc ] Update VideoPlayerShim button to a simple pacakge import instead of a full window, move AVPro import into VideoPlayerShim itself.
- [ Prefab ] Update prefabs to finish support for defaulting to LL with a buffered alternative option available (mainly for AMD).
8
### Fixed
- [ Core ] Fix timestamp continuity when simultaneously changing video managers and URLs.
- [ Core ] When inputting a URL that was the same as the last one, it will do a full reload as if it was new media, including sync.
  - Refreshing/reloading media without a url will still retain seek continuity and only happen locally.

## 3.0.0-beta.10.7 (2023-12-29)
### Changed
- [ Core ] Enable persistence toggle to handling of preview for custom textures.
  - This allows users to control whether the preview textures should be applied or not.
- [ Core ] Remove some improper value checks causing unintended side-effects during speaker management.

## 3.0.0-beta.10.6 (2023-12-28)
### Added
- [ Core ] Add getter for internal default volume value.
- [ Playlist ] Add new alternative playlist collection files.
- [ Prefab ] Add Center channel to all speaker setups for better fallback support for surround sound media.

### Changed
- [ Core ] Disable video texture if not already when game object is disabled.
- [ Core ] Prevent certain methods from running if the old and new values are the same.
- [ Core ] Add special material name check for build checks so if it starts with _Test it won't force remove the material.
  - 99.99999% of people won't use this. It is specifically for enabling testing of the raw AVPro material in-game.
- [ Core ] Update create button render texture to use ansio for better angle viewing.
- [ MediaControls ] Move some MediaControls editor options behind null checks for related UI references.
- [ Playlist ] Rename folder `Resources/Playlists/Mixes` to `Resources/Playlists/Collections`.
- [ Misc ] Update fullscreen shader script to temp-hide fullscreen while the player is moving or holding Shift.

### Fixed
- [ Core ] Restore missing deprecated ChangeVolumeTo method for migration reasons.

## 3.0.0-beta.10.5 (2023-12-19)
### Changed
- [ MediaControls ] Update custom editor to auto-enable dropdown TMP toggles if a TMP component is detected.
- [ Shader ] Some misc shader cleanup, no functional changes.

### Removed
- [ Misc ] Remove unused animations.

### Fixed
- [ Core ] Update internal render texture to use ansio to avoid poor legibility of the screen at sharp angles.
- [ MediaControls ] Update prefabs to avoid extreme values when the mouse moves during a click action for certain buttons ([fixes #15](https://gitlab.com/techanon/protv/-/issues/15)).
- [ Queue ] Fix queue not automatically playing a new entry when the queue is empty after manually removing all entries ([fixes #16](https://gitlab.com/techanon/protv/-/issues/16)).

## 3.0.0-beta.10.4 (2023-12-17)
### Added
- [ Misc ] Add VPM resolver dependency for the VideoPlayerShim requirements.

### Changed
- [ Core ] Mitigation for excessive on-join resync actions causing undesirable stutter.
- [ Core ] Cleanup playback speed logic.
- [ Misc ] Update support for VideoPlayerShim to handle checking for the new VPM package from 1.1.0.

### Fixed
- [ Playlist ] Fix autoplay playlist not setting priority during init.

## 3.0.0-beta.10.3 (2023-12-09)
### Changed
- [ Core ] During _RefreshMedia, trigger the loading event after the media change event.
- [ Core ] Remove old OnPlayerJoin resync logic that is made obsolete by other recent init changes.
- [ AudioAdapter ] Cleaned up compile defines
- [ Misc ] Update a couple obsolete methods.

### Fixed
- [ Core ] Fix erroneous muting when swapping audio modes on a TV that starts with 2d Audio.
- [ Core ] Fix VPManager init not taking the default audio3d value into account if the TV isn't ready yet.
- [ AudioAdapter ] Update _ChangeAudioLinkState method to properly handle enabling and disabling internal AL handling.
  - This includes the `_ToggleAudioLinkState`, `_EnableAudioLinkState` and `_DisableAudioLinkState` events.
  - Pairs well with ArchiTech.Umbrella's ZoneTrigger utility or other custom udon scripts.
- [ Playlist ] Prevent erroneous video triggering during _TvVideoPlayerError event when another plugin has already triggered a video.
  - This fixes the issue where when a playlist video errors, it another playlist takes priority unintentionally.
- [ Queue ] Fix entry titles not using the correct url for displaying the domain past the first entry.
- [ Misc ] Fix the normals for provided 3d models.

## 3.0.0-beta.10.2 (2023-12-01)
### Added
- [ BuildChecks ] Add opt-out-able dialog prompt for when multiple TVs are using the same RenderTexture.
- [ Misc ] Add non-transparent version of the square logo.

### Changed
- [ AudioAdapter ] Update default AudioLink speaker volume to 0.002.

### Fixed
- [ AudioAdapter ] Restore missing AudioLink prefab file location.
- [ AudioAdapter ] Correctly respects the managed state of the active speaker.
  - This fixes weird edge cases where a speaker would erroneously be enabled.

## 3.0.0-beta.10.1 (2023-11-28)
### Fixed
- [ AudioAdapter ] Remove undesired using statement causing a compiler error.

## 3.0.0-beta.10 (2023-11-28)
### Added
- [ Core ] Add a prefilled list of TV options from the current scene for any TVPlugin inspectors.
- [ Core ] Add convenience hookup button for AreaLit if detected.
- [ Core ] Add build check that will error when multiple TVs are detected to be using the same Material/prop combo or RenderTexture.
- [ Core ] Add new `OUT_URL` string for TV plugins to use during `_TVMediaChange` and `_TVMediaReady`.
- [ MediaControls ] Add detection for nested `MediaControls` and display a button to merge it into the parent component, includes corresponding utility method.
- [ MediaControls ] Add `_ToggleUrlMode`, replacing `_ToggleUrl`, for better naming clarity.

### Changed
- [ Core ] Update all plugin editors to a common plugin editor base class.
- [ Core ] Improve the visual layout of most of the custom editors, adds section titles and visual boxes.
- [ Core ] Move TV field setup to the common base editor class for easy integration into custom plugins.
  - This includes a new method `DrawCoreReferences` for rendering common plugin elements.
- [ Core ] Decouple first master logic from allowing master control.
  - This means if you have master control disabled but first master is a super user, the first master will still be recognized correctly.
- [ Core ] Add security tweak for allowing authorized users to also bypass the domain whitelist.
- [ Core ] Move LTCGI helper button into the `Rendering Options` section of the `TVManager` editor.
- [ Core ] Add mechanisms to preview the default standby texture in the custom texture if both are present.
- [ Core ] Stabilize the automatic handling of multiple GSV flags being set.
- [ Core ] When automatically connecting AudioLink, implicitly add an AudioLink specific speaker for AVPro managers.
- [ Core ] Move rendering of the volume/mute toggles for auto-management behind a toggle.
- [ Core ] Improved VPManager's handling of mute between 2d/3d audio modes so it retains the correct auto-managed state.
- [ Core ] Cleanup internal texture generation to better ensure that only one is being used across all VPManagers of a single TV.
- [ AudioAdapter ] Cleanup some logic in the AudioAdapter.
- [ Shader ] Update blit to use the more accurate color space conversion logic.
- [ Dependency ] Update ArchiTech.SDK minimum to 0.14.0.
- [ Misc ] Improve support for Unity 2022.
- [ Misc ] Update 3D logo to new branding.
- [ Misc ] Prevent the BuildLog window prompt dialog from displaying if the window is already open.
- [ Misc ] Change AVProSpeaker generation method to only make one per call instead of an array.

### Deprecated
- [ MediaControls ] `_ToggleUrl` in favor of the new `_ToggleUrlMode`.

### Fixed
- [ Core ] Trigger a blit call when seek is updated while the media is paused.
- [ Auth ] Add missing Reauthorize call for the owner of the ManagedWhitelist.
- [ Queue ] Fix queue not correctly running next media when the last media was skipped.
- [ Shader ] Fix stereo eye detection nonsense for pico standalone because idfk it wasn't working normally.
- [ Shader ] Update the shader's default render queue to prevent certain weird edge cases with other shaders at the same queue value.

## 3.0.0-beta.9.4 (2023-11-09)
### Fixed
- [ Core ] Simple tv prefab was moved but the path reference in the context menu was not updated to reflect that.
- [ Core ] Add null check for VPManager when UnityVideo has only one entry in the UnityVideo target audio sources list and it is missing the audio source (such as the speaker was deleted).

## 3.0.0-beta.9.3 (2023-11-08)
### Changed
- [ Prefab ] Reconstruct prefab VPManagers to try to mitigate weird issues that can occur when prefab is unpacked.

## 3.0.0-beta.9.2 (2023-11-06)
### Changed
- [ Core ] Update version injection to support non GUI TMP text objects.
- [ Core ] Add build checks to avoid certain array index errors in VPManager.
  - This should fix some weird behaviour that can occur during the upgrade process.
- [ Misc ] Update download links for VideoPlayerShim (to 1.0.4) and AVProTrial (to 2.8.5).

### Fixed
- [ Prefab ] Fix broken sprite reference in the Live Events prefab.

## 3.0.0-beta.9.1 (2023-11-05)
### Changed
- [ Core ] Allow disabling of the max allowed load time by setting it to 0.

### Fixed
- [ Core ] Null pointer exception when an explicit render texture is not provided.

## 3.0.0-beta.9 (2023-11-02)
### Added
- [ Core ] Add public getters for CustomTexture and RawTexture.
- [ Misc ] Update VideoSettings to better handle value changes.

### Changed
- [ Core ] Rework internal blit op into two separate blit ops for different purposes.
  - The first blit is the main operation which does orientation and color corrections.
  - The result of the first blit is what will be passed to all consumers of the raw texture (like material targets, globals shader variables, etc.)
  - If the world creator provides a render texture, the second blit op is enabled.
  - The second blit is treated as a RenderTexture variant of what the ProTV shader does.
  - It bakes in corrections for 3D and aspect ratio into the texture.
  - The 2nd blit texture is typically used by LTCGI or AreaLit.

## 3.0.0-beta.8 (2023-11-02)
### Added
- [ Core ] Add buttons to detected playlists section for linking to the current TV.
- [ Playlist ] Add automatic playlist detection with a dropdown in the inspector.
- [ Playlist ] Add new themed sample playlists.
- [ Playlist ] Add SwitchToRandomUnfilteredEntry event.
- [ Queue ] Add QueueListener class and implement listener events for the Queue's activity.
  - You can make a class that inherits from this type to listen for activity from a given Queue.
- [ Queue ] Add synced QueueChangeMode and additional index value to signal to non-owners what kind of modification occurred on the queue.
- [ AudioAdapter ] Add enable/disable/toggle events to AudioAdapter for controlling whether AudioLink is to be used.

### Changed
- [ Core ] Improve speaker management for VPManagers.
  - Speakers are now separated into explicitly 2D mode and 3D mode lists.
  - Pan/Spread/Spatial is no longer modified during runtime, only volume and mute.
  - Each speaker will now have its own separate flags for whether mute or volume should be managed by the TV.
- [ Core ] Update classes to utilize the new ATEventHandler type from the ATSDK.
- [ Playlist ] Update playlist file save to use the `.playlist` extension.
  - This extension is used in place of the previously used `.txt` file to specially denote the file as a playlist.
  - This extension is used during the new auto-detection logic for finding existing playlists.
  - If you want your playlist to show up in the dropdown, change the file extension from `txt` to `playlist` and reimport the file.
- [ Playlist ] Update playlist SwitchToRandomEntry to use the filtered view by default.
- [ Dependency ] Update ArchiTech.SDK minimum to 0.13.
- [ Prefab ] Move the basic prefab up a folder so it's a bit easier to find.

### Deprecated
- [ Playlist ] Deprecate SwitchToRandomFilteredEntry.

### Fixed
- [ Core ] Mitigations for the TV failing to continue videos when one errors out in certain scenarios.
- [ Core ] Enable updating the aspect ratio via inspector during playmode.
- [ Core ] Prevent Skip from being called while a video is loading.
- [ Core ] Fix shaderData assignment for volume/seekPercent/PlaybackSpeed to correctly match the spec.
- [ Core ] Fix audio mode swap not correctly respecting mute state.
- [ MediaControls ] Fix reset not updating correctly when a default url value is provided.
- [ Misc ] Prevent VideoSettings from doing a double update to the aspect ratio value.
- [ Prefabs ] Improve option defaults in the VideoSettings prefab.
- [ Prefabs ] Fix some incorrect values in the media controls prefabs.
- [ Prefabs ] Cleanup audio sources on the `Simple (ProTV)` prefab.
- [ Shader ] Fix FullScreen shader not using the correct value for determining eye width in OVUN mode.

## 3.0.0-beta.7 (2023-10-20)
### Added
- [ Core ] Add check to warn about screens missing the VideoPlayer reference.
- [ MediaControls ] Add default URL values for main and alternate.
- [ Queue ] Add API for getting data from a given entry.
- [ Misc ] Add Enable Video toggle to the VideoSettings prefab.

### Changed 
- [ Misc ] Remove redundant UI icons and update related prefabs.

### Fixed
- [ Core ] Fix incorrect scaling for unity video texture during the blit operation.
  - This resolves the black video for unity player on the 2022 version of VRChat.
- [ AudioAdapter ] Correct some audiolink media states for more accurate values.

## 3.0.0-beta.6.3 (2023-10-16)
### Fixed
- [ AudioAdapter ] Update AudioLink media state SetMediaTime to pull the normalized seek value instead of the raw play time.

## 3.0.0-beta.6.2 (2023-10-16)
### Fixed
- [ Prefab ] Swapped the audio channel used by AVProHQ Stereo to the correct StereoMix mode.
- [ Prefab ] Fix typo in a game object name.

## 3.0.0-beta.6.1 (2023-10-15)
### Changed
- [ Misc ] Update branding on protv box model.

### Fixed
- [ Core ] Add a missing null check for internal texture.

## 3.0.0-beta.6 (2023-10-15)
### Added
- [ Core ] Update VPManager to handle managed mute/volume control on a per-speaker basis.
- [ Core ] Add TVManager setting for disabling video on start.
- [ History ] Add empty title placeholder value.

### Changed
- [ Core ] Update TVManager to sort detected plugins by Priority.
- [ Auth ] Update ManagedWhitelist to make the UI elements optional.
- [ Misc ] Add enforcement to disable autoSetMediaState if there are AudioAdapters controlling AudioLink.
- [ Misc ] Update AudioLink auto-connect button to utilize AudioLink's built-in method for adding it to the scene.
- [ Misc ] Switch youtube prefix to short url format for youtube generator.
- [ Shader ] Update shaders to default fog support to on.
- [ Shader ] Cleanup some extraneous macros from the Blit shader.
- [ Dependency ] Update ArchiTech.SDK minimum to 0.12.0.

### Fixed
- [ Core ] Add missing addedBy assignments.
- [ Core ] Update TV to force play on load if the prior jumpToTime was non-zero, fixes reload continuity.
- [ Playlist ] Fix playlist entries being erroneously assigned the _ManualPlay event even when disableAutoplayOnInteract is not set.
  - This should resolve the edge-cases where the playlist will unexpectedly stop autoplaying.
- [ Queue ] Re-expose the maxQueueLength property in the inspector and increase limit to 50.
- [ Queue ] Fix queue not properly tracking when media playing is not the active entry.
- [ Queue ] Fix queue not always cleaning out the ended media correctly.
- [ Queue ] Fix queue loading bar being active in improper situations.
- [ History ] Fix occasionally adding sequential duplicates.

## 3.0.0-beta.5.1 (2023-10-10)
### Fixed
- [ Core ] Remove some old experimental code that slipped through.

## 3.0.0-beta.5 (2023-10-09)
### Added
- [ Core ] New SeekOffset value for displaying the local timestamp by +-5 seconds, great for karaoke worlds.
- [ Core ] New PlaybackSpeed integration via animator.
  - This only works for UnityVideo, automatically disabled for AVPro due to AVPro being dumb as usual.
- [ Core ] Added getter property `Buffering` for checking if the TV has loaded the media but is in the buffer waiting period.
- [ Core ] New API surface for TVPlugin:
  - `_TvSeekOffsetChange` 
  - `OUT_MODE` and `_Tv3DModeChange`
  - `_Tv3DWidthHalf` and `_Tv3DWidthFull`
  - `_TvColorSpaceCorrected` and `_TvColorSpaceRaw`
- [ MediaControls ] New API surface for media controls:
  - `_ToggleColorCorrection`, `_EnableColorCorrection`, `_DisableColorCorrection` and `_ChangeColorCorrection(bool)`
  - `_Change3DMode(int)`
  - `_Toggle3DWidth`, `_Width3DFull`, `_Width3DHalf` and `_Change3DWidth(bool)`
  - `_ChangeSeekOffset(float)`
- [ MediaControls ] Add new Options menu with control options:
  - Playback Speed
  - Seek Offset
  - 3D Mode Dropdown and 3D Width Toggle
  - Color Correction Toggle (only for AVPro, generally for AMD GPUs in software rendering mode)
  - Audio Mode Toggle (Restored from 2.3)
  - Loop Toggle (only swaps between infinite and no looping, for limited count looping the URL params are required)
- [ MediaControls ] New Video Options section in the component inspector.
- [ AudioAdapter ] Add option for whether to allow muting the audio source for AudioLink during silence.
- [ Shader ] Add fog support to the VideoScreen and GlobalScreen shaders.

### Changed
- [ Core ] Improve handling of the stop action while loading media.
- [ Core ] Seek value sent to listeners via OUT_SEEK is now the raw value instead of the normalized percentage.
- [ MediaControls ] Update localTime to clockTime for better naming.
- [ MediaControls ] Expose the error messages to public fields so they can be modified during runtime for language translation needs.
- [ Misc ] Improve some image compression options.
- [ Misc ] Some deduplication of custom inspector logic.
- [ Misc ] Disable some excessive trace logs.
- [ Dependency ] Update VRCSDK dependency to minimum SDK 3.4.0.
- [ Dependency ] Update ArchiTech.SDK minimum to 0.11.0.
- [ Shader ] Restored scaling/offset for VideoTex on the VideoScreen shader.

### Deprecated
- [ Core ] `_ToggleBlitGamma` is renamed to `_ToggleColorCorrection`

### Fixed
- [ Core ] Prevent seek from sending a managed event if an event is already running.
- [ Core ] Fix jump time not being retained correctly due to UnityVideo triggering VideoStart multiple times when swapping between video player options.
- [ MediaControls ] Fix improper volume icon logic under certain scenarios.
- [ MediaControls ] Update loop toggle variables and display for clearer usage.
- [ AudioAdapter ] Update scripting defines for AudioLink to account for the new canonical scripting define for AudioLink 1.x.
- [ Shader ] Fix OverUnder incorrectly applying fullwidth to the horizontal axis instead of vertical.
- [ Misc ] VideoPlayerShim package link should no longer be incorrect.

## 3.0.0-beta.4 (2023-09-21)
### Changed
- [ Dependency ] Update the dependencies in preparation for VRChat's 3.4.0 merge update.

### Fixed
- [ Core ] Add conditional exception for currentTime for rare scenarios where it's Infinity.
- [ Misc ] Add missing sprite to sprite atlas.
- [ Shader ] Re-add missing SPS-I macro to shaders.

## 3.0.0-beta.3 (2023-09-16)
### Changed
- [ Prefab ] Layout adjustments to AssetInfo, increase the size of the AssetInfo toggle button on certain prefabs.
- [ Misc ] Updated VideoSettings to handle the mirror flip mode options.

### Fixed
- [ Shader ] 3D in the mirror should now work correctly for all headset configurations.
  - Previously there was some rare circumstances where the eyes would be swapped compared to the non-mirror viewing.

## 3.0.0-beta.2 (2023-09-16)
### Added
- [ Core ] Add support for full-width 3D mode
  - This means that instead of each eye being half of the texture expecting to be rendered as full size (stretching),
    each eye is rendered a full resolution with a double sized texture (pixel accurate).
  - To use this mode, just specify the negative value of the respective SBS or OverUnder mode (see docs more more info).
- [ MediaControls ] Add passthrough event for `_ToggleBlitGamma`.
- [ Prefab ] Add a AVPro Center speaker for better out-of-the-box support of surround sound (SS) without forcing a full SS setup.
  - Many SS videos have voice audio focused on the center speaker. Previously those videos would be hard to watch and would require a world that supported SS audio.
- [ Shader ] Add shader option to make the aspect letter/pillar boxing be removed (transparent instead of black)

### Changed
- [ Core ] Removed depth layers from generated render texture to save on VRAM as it's not needed.
- [ Core ] Update default domain whitelist to match the latest from VRChat.
- [ Misc ] Update the VideoSettings namespace to be in-line with the rest of the scripts.
- [ Prefab ] Update VideoSettings to handle the new full-size 3D option, the new 3d mode values and the gamma toggle for AMD.
- [ Dependency ] ArchiTech.SDK 0.10.1 fixes an issue with `Prioritize on Interact` when Playlist is connected to a Queue.
- [ Shader ] Update the structure of `_VideoData` to condense most of the boolean flags into a single bit-packed int field.
  - The purpose of this change is to clean up the shader matrix data for future possible values being passed in.
  - Flags for locked, mute, live, loading and force2d have been moved into the `_11` field.
  - State enum moved from `_21` to `_12`
  - ErrorState enum moved from `_23` to `_13`
  - SeekPercent value moved from `_32` to `_22`
  - PlaybackSpeed value moved from `_33` to `_23`
  - Read the documentation for more details.

### Fixed
- [ Core ] Fix URL parsing issue where only the first two parameters would be processed.
- [ Core ] Fix regression with the url parameter `t` not being respected.
- [ MediaControls ] Fix Volume slider not updating the icon correctly. MR !4
  - Also fixes the bug where dragging from 0 back to 0 doesn't correctly silence the audio.
- [ Playlist ] Rouge null pointer exception happening under rare scenarios when importing a playlist.
- [ Queue ] Fix authorization conditions for maxEntriesPerPlayer so the value is properly respected.
- [ Queue ] Delete entry button should show under correct authorization conditions now. (purely visual, clicking it when unauthorized does nothing)
- [ Queue ] Player names being erroneously purged from the queue when the media switches.
- [ Prefab ] Updated some video player dropdowns to auto-size the font correctly.
- [ Misc ] TVDropdownFix now correctly handles the blocker element when the VRCUiShape is not on the same object as the canvas.
- [ Shader ] Corrected 3D logic to fix rare issue where both eyes would render as the right eye, appearing 2D, for a small amount of users.
  - This should fix 3D for anyone who was unable to view 3D content correctly before.
  - Note that for certain users, in the mirror the eyes might be swapped. A solution is being researched for this issue.

## 3.0.0-beta.1 (2023-08-28)
### Changed
- [ Core ] Fix some editor script grammar issues.
- [ Shader ] Update skybox shader with basic _VideoTex usage to bring it in line with the other ProTV shaders.
- [ Branding ] Move QR code images to the branding folder.

### Removed
- [ Docs ] Remove documentation folder entirely as it has been moved to another repo.

## 3.0.0-alpha.32 (2023-08-27)
### Changed
- [ Core ] Switch prefabs default to not override lock by superusers.
- [ Core ] Update solo controls folder name to help clarify a requirement for using them.
- [ Core ] Re-enable scale/offset for non-VideoTex properties on the ProTV videoscreen shaders.
- [ Core ] Move logic for disabling on interact to build checks UI events correction to avoid logic contamination.
- [ Core ] Default superUserLockOverride to false.
- [ Prefab ] Move URLControls into the helpers prefabs subfolder.
- [ Prefab ] Remove booth and gumroad QRs from the asset info prefab in favor of the new site link.
- [ Branding ] Move the branding folder to the root and suffix tilde to hide from unity.
- [ Branding ] Update branding in preparation for ProTV 3 launch.

### Fixed
- [ Core ] Fix v2 script upgrades not parsing filenames correctly.
- [ Playlist ] Fix playlist random choice not getting the correct index when the playlist is filtered via search.
- [ Queue ] Fix on-build errors only displaying one at a time when it should collect all possible at once.

## 3.0.0-alpha.31 (2023-08-25)
### Added
- [ Core ] Add check to enforce navigation mode to none for respective UI elements.
- [ MediaControls ] Add API surface for handling for toggling loop.
- [ MediaControls ] Add skip button to the mediacontrols UI integration.
- [ Playlist ] Add API method for activating a random playlist entry of either unfiltered or filtered selections.
  - If random is filtered, it will only pick an index of the currently visible entries, such as after a search.
- [ Shader ] Add dynamic mirror mode to the standard and global videoscreen shader.
  - This mode enables correcting the screen's orientation even while viewing the screen upside-down, like with horizon adjust.
  - There are some goofy artifacts that happen when viewing the screen at 90deg angle when using dynamic mirror mode.
  - This is not a bug but a side-effect of the logic needed to accomplish the flip correctly.

### Changed
- [ Core ] Update the public method GetUrlDomain to be _ prefixed.
- [ Core ] LTCGI auto-connect button will enable the apply aspect options by default.
- [ MediaControls ] Expose GetReadableTime as a static public method on MediaControls for convenience of other scripts.

### Fixed
- [ Core ] Mitigate issue where null entries in the VRCUnityVideoPlayer audio sources list would cause some errors.
- [ Core ] Mitigate NPE issue related to the blit material upon exiting playmode.
- [ Core ] Fix regression with videoplayer swapper not retaining the jump timestamp.
- [ MediaControls ] Fix alignment issue on the media controls seek bar handle.
- [ Queue ] Make title respect the showUrlsInQueue option.
- [ Misc ] Fix fullscreen script missing the delayed event call.
- [ Shader ] Add missing 3D modes to the fullscreen shader.

## 3.0.0-alpha.30.1 (2023-08-23)
### Fixed
- [ Core ] Added missing scripting define for one of the third-party references.

## 3.0.0-alpha.30 (2023-08-23)
### Added
- [ Core ] Add auto-connect button for LTCGI.
- [ Auth ] Add logger assignment to the auth plugin class.
- [ Core ] Add new disableVideo flag which will force the render texture to be treated as null in any shaders.
- [ Core ] Add API events for manipulating the enableGSV flag and the disableVideo flag.
- [ Core ] Add playback speed control (only works on UnityVideo).
- [ Core ] Add new API method for extracting a url parameter.
- [ MediaControls ] Add fallback text option for when the tv has an empty title. If empty, it'll use the current url's domain.
- [ MediaControls ] Add color option to respective icon swap options.
- [ MediaControls ] Add timed message display for when media controls successfully queues a url.
- [ SkyboxSwapper ] Add flag for whether it should only apply the skybox change when a specific URL parameter is present or not.
- [ Prefab ] Add Monochrome/Color based prefabs.
- [ Prefab ] Add new Live Events tv prefab.
- [ Prefab ] Add explicit visual slider bar handle to seek elements.
- [ Misc ] Add new script and prefab for handling desktop fullscreen shader via keybind.

### Changed
- [ Core ] Update version defines, correct AudioLink and add LTCGI defines.
- [ Core ] Move the ProTV unity menu under the Tools submenu and reorganize them a bit.
- [ Core ] Move the Blit shader to the Hidden shader prefix so it doesn't show up in the shader selector for materials.
- [ Core ] Due to general instabilities with quest/mobile using YTDL, the flag check for android to prevent auto-ownership on those systems has been restored.
- [ Queue ] Update _AddEntry method to return a bool of whether adding was successful or not.
- [ Dependency ] Update ArchiTech.SDK minimum to 0.10.0
- [ UI ] Add Plain UI theme.
- [ Misc ] Move items from the Resources/Core directory in the parent directory.
- [ Misc ] Move dropdown fix script from abstract into misc folder.

### Removed
- [ UI ] Remove all other UI themes (moved to skins repo).
- [ Prefab ] Remove all other prefabs (moved to skins repo).
- [ Misc ] Remove ProTV Showcase demo scene (will be repurposed elsewhere).

### Fixed
- [ Core ] Fix VPManager not correctly retaining custom speakers during certain situations.
- [ Core ] Fix build checks incorrectly handling certain situations of upgrading 2.x structure to 3.x, mainly with screens.
- [ Auth ] Fix certain edge-case scenarios which managed whitelist would incorrectly detect the user's authorization level.
- [ Core ] Fix media ready not properly caching the swapped data values during a swap.
- [ Core ] Fix edge-case where a user that failed to load a video would break the sync when they tried to pause when transferOnPause is enabled when they have authorization.
- [ Core ] Fix some NPEs for isMaster checks.
- [ Core ] Fix VPManager not exposing the RenderTexture reference to the TV when it's dynamically created.
- [ Core ] Fix standby textures not displaying consistently under all necessary scenarios.
- [ AudioAdapter ] Fix AudioLink scripting defines implementation for much better reliability including for upgrading to AL 1.0.0.
- [ AudioAdapter ] Tentative fix for rare cases where the audio adapter would incorrectly mute the speaker.
- [ Queue ] Fix WillBeEmpty check to correctly account for the loop flag.
- [ Queue ] Fix possible div-by-0 error that might occur during a loop check.
- [ Core ] Fix editor issue causing some NPEs.
- [ Misc ] Restore some broken GUID references.
- [ Misc ] Update TVDropdownFix to accomodate for nested canvases.

## 3.0.0-alpha.29.1 (2023-08-13)
### Added
- [ Auth ] Add tooltips to some whitelist fields.

### Changed
- [ Core ] Cleanup editor scripts to make use of the auto propertylist logic in the ArchiTech.SDK.
- [ Dependency ] Update ArchiTech.SDK minimum to 0.9.3.

## 3.0.0-alpha.29 (2023-08-12)
### Added
- [ Auth ] Add explicit authorized users lists to TVUsernameWhitelist and TVManagedWhitelist.
- [ Core ] Add overloads to ProTVEditorUtility.FindParentTVManager and make them public for general use.

### Changed
- [ Core ] Update VPManager speakers listing to use the new ATFoldoutArrayTuple layout.
- [ Core ] Cleanup handling of videoManagers in the TVManagerEditor.
- [ AudioAdapter ] Enable the SetMedia logic for AudioLink since it has been merged into the upcoming 1.x release.
- [ Dependency ] Update ArchiTech.SDK minimum to 0.9.2
  - This should fix some non-critical edge case error logs that occasionally popped up.

### Removed
- [ Core ] Migrated some additional utility methods to the ArchiTech.SDK.

### Fixed
- [ AudioAdapter ] Fix some compiler issues in AudioAdapter related to AudioLink scripting defines.

## 3.0.0-alpha.28 (2023-08-12)
### Changed
- [ Core ] Reorganize and rename some inspector fields.
  - The terminology of `Fallback Texture` has been updated to `Standby Texture` for language clarity.
  - The `Default Aspect Ratio` has been updated to `Texture Aspect Ratio` and has been moved into the Rendering Options section.
  - The Rendering Options section now has two foldouts for better organization: 
    - `Texture Update Settings` for options on how to handle the render texture during blit.
    - `Standby Texture Settings` for options dealing with in-active or sound-only media states.
- [ Core ] Default the filter mode of the render texture made via the Create button to Trilinear.
  - This improves compatibility with AreaLit.
- [ AudioAdapter ] Reworked the plugin to remove the requirement of AudioLink being present for it to run.
  - This enables the current world audio management, as well as future added features, to work without needing AudioLink imported.
- [ AudioAdapter ] Improve handling of the scripting defines to properly handle the differences between AudioLink 0.x and the upcoming AudioLink 1.x releases.
- [ Dependency ] Update ArchiTech.SDK minimum to 0.9.1

### Removed
- [ Core ] Migrated some generic utility methods from the ProTVEditorUtility to the ArchiTech.SDK ATEditorUtility script.
  - Specifically `ToRelativePath`, `HasComponentInScene`, `GetComponentInScene`, `GetComponentsInScene` and `SwapUdonSharpComponentTypeTo`
- [ Skybox ] Remove some vestigial testing assets that haven't been relevant for a long time.

### Fixed
- [ Core ] Inspector text for RenderTexture Target was not connected to the control field. Right click should work on the text now.
- [ AudioAdapter ] Folder for the plugin now has the correct name.

## 3.0.0-alpha.27 (2023-08-10)
### Added
- [ Core ] Added missing toggle event for swapping the "skip gamma" flag added in the previous update.
- [ Prefab ] Add missing skip gamma ui toggle component to the VideoSettings prefab.
- [ Misc ] Add popup dialog on entering playmode which prompts for importing the VideoPlayerShim tool if it's not already in the project.
  - If "no" is selected on the dialog, it will not pop up again for the remainder of the editor session.

### Changed
- [ Core ] Build script will now auto-detect video screens that should have their materials added to the custom materials list of the respective TV.
  - Any extraneous AVProVideoScreens detected will be removed from the GameObjects to improve performance.
  - It will ONLY remove excess screens that are related to AVProVideoPlayers associated with a ProTV component.
  - It will NOT interfere with AVPro components from other non-ProTV video players in the world.
- [ AudioAdapter ] AudioLinkAdapter has been renamed to AudioAdapter.
  - This rename is for clarity of purpose as it handles more than just AudioLink integration and will likely handle additional features in the future.
  - The old AudioLinkAdapter type is retained for compatibility and is marked as obsolete/deprecated.

### Fixed
- [ Core ] Managed speakers should be properly retained when adding custom audio sources to the list.
- [ Playlist ] Scrollbar position now correctly reflects the scroll content.
  - It should no longer have excess scroll travel distance near the end of the playlist during runtime.

### Removed
- [ Core ] Remove unary check for android users in the auto ownership check since Quest now has YTDL implemented natively.

## 3.0.0-alpha.26 (2023-08-07)
### Added
- [ Core ] Add toggleable flag to force Blit to skip applying gamma to AVPro textures, used for AMD GPU software rendering.
- [ Core ] Add handling for material target changes to auto-detect internal shaders.

### Changed
- [ Core ] Update USharp references fix to include all runtime classes because unity occasionally likes to be unity and break U# things.
- [ Core ] Adjust foldout layout indents for easier visuals.
- [ Dependency ] ArchiTech.SDK update to 0.9.0.

### Fixed
- [ Core ] Tentative fix for edge-case where 3D mode would be applied to a 2D fallback texture.

## 3.0.0-alpha.25.1 (2023-08-04)
### Fixed
- [ Core ] Regenerated a meta file which had become corrupted causing compiler failures.

## 3.0.0-alpha.25 (2023-08-02)
### Fixed
- [ Core ] Fix edge-case where RTSPT links fail to correctly show visuals (such as VRCDN).

## 3.0.0-alpha.24 (2023-08-01)
### Added
- [ Core ] New settings
  - `Fallback 3D Mode`: Determines what 3D mode should be applied to the optional fallback textures when shown.
  - `Show Fallback on Media Pause`: When enabled, the fallback texture will be displayed while the media is paused.
  - `Auto-MipMap Texture`: If no explicit RenderTexture is provided, this option determines if the generated one should have mipmaps or not.
- [ Core ] New TVPlugin event `_TvAuthChange`
  - This is currently a passive event that auth plugins can call to notify regular plugins that some authorization levels have changed and should recheck stuff.
- [ Auth ] New plugin `TVManagedWhitelist`
  - This plugin implements an in-game UI which super users defined on a whitelist can dynamically choose who is authorized.
  - It is great for tightly controlled events or popups where the authorized users need to change on the fly.
- [ Core ] Add build check for preventing MeshFilters from being on VideoManager game objects.
  - Trying to draw a mesh on that object completely bypasses all the rendering settings of ProTV, so it is disallowed.
- [ Core ] Add API event for toggling whether video should be forced into 2d mode or not: `_ToggleVideoForce2d`
- [ Core ] Add new entries to the TV3DMode options for explicitly swapping the eye layout of either SBS or OVUN.
- [ Playlist ] New API methods:
  - `_FillQueue` this is a generic event that will fill up the attached queue with as many entries as the queue can fit.
  - `_FillQueue(int)` this method will attempt to fill up the attached queue up to the specified number of entries.

### Changed
- [ Core ] Default value of 3D spread updated to 0.
  - The previous default value was an experimental value that neglected to get reverted.
- [ Auth ] Log output text for authorization checks have been cleaned for consistency.
- [ Queue ] Plugin now reacts to the tv's lock and auth events for updating the UI.
- [ Queue ] UI will now hide the button background for persistent entries when the user does not have enough authorization.
  - This helps signify that the button isn't interactable but will still show the indicator icon.
- [ History ] Plugin now reacts to the tv's lock and auth events for updating the UI.
- [ Docs ] Update documentation for TVPlugin events to add missing events and fix some terminology.

### Fixed
- [ Core ] Over/Under 3D videos should now have the correct eye layout.
- [ Core ] Some non-issue errors that occurred upon leaving a world should no longer appear.
- [ Core ] Fix edge-cases of incorrect behaviour when interacting with live media.
  - Live media video should show up correctly as expected.
- [ MediaControls ] The text 'Live' should now correctly display when live media is playing.

## 3.0.0-alpha.23 (2023-07-26)
### Added
- [ Core ] New Security tweaks
  - Option which enables/disables the lock override for super users.
    - This means authorized users can still interact with the TV if a super user has it locked when the option is disabled.
  - Option which specifies whether the instance owner should be treated as a super user.
  - Option which specifies whether the instance master should be treated as a super user.
    - This option requires the `Allow Mater Control` and `Remember First Master` settings enabled.
  - Option for allowing the pause action to take ownership if the user is authorized.
- [ Core ] Finalize and enable the domain whitelist feature.
- [ Core ] Add API methods for individual managers to explicitly control the state of the speakers, regardless of the auto-manage flags.
- [ Core ] Add rendering option (enabled by default) which makes the fallback texture be shown when the media is considered to be "ended".
- [ History ] Add copy url button to entries.
  - Add toggle to enable the feature.
  - Add toggle to require authorization to access the copy url button.
  - Is available by default, if it is not desired, just delete the Copy game object from the prefab.
- [ Shaders ] Fix conditionals required for when the sound-only texture is displayed.
- [ Misc ] Add old ProTV files for LTCGI to the legacy files list of the package so they are removed on upgrade from 2.x to prevent compilation issues.

### Changed
- [ Core ] Sort some security and sync settings into subsection foldouts in the inspector.
- [ Core ] Make the Auth Plugin reference modifiable in the inspector instead of a completely automatic detection.
- [ Core ] Local user's auth checks are now cached separately from other user's auth checks.
- [ Prefab ] Moved all video managers and TVData/TVAuth objects under a unified "Internal" game object.

### Removed
- [ Core ] Disabled the first-class pixel extraction until a stable solution for the feature is developed.
  - This was technically not public as it was hidden behind a debug flag, but it is now disabled entirely as it's too unstable currently.

### Fixed
- [ Core ] Play Drift Threshold is now ignored when the owner is marked as disabled.
- [ Core ] Fix edge-cases with media end phase that would cause the TV to act unexpectedly.
  - This should make the Queue's behaviour much more predictable and stable.


## 3.0.0-alpha.22 (2023-07-18)
### Added
- [ Core ] Add new getter property `WaitingForMedia` which returns whether any media has been played previously or not.
  - Friendly reminder about the getter property `CanPlayMedia` which powers all authentication checks for whether the local user can control the TV.
  - Use `CanPlayMedia` in place of any `!tv.locked || tv._IsAuthenticated()` check combinations.
- [ Core ] New setting for syncing volume control.
- [ Core ] New setting for making the TV remember the first master of the instance.
  - This feature is intended to help bridge the gap between public/group instances and invite/friends instances.
  - Since public/group instances do not have the instance creator as it's owner, 
  the setting will retain the name of the first person in the instance
  and grant them the same authorization level as the current master.
- [ Playlist ] New setting for defining an amount of entries for the playlist to pre-load into the connected Queue on start.
  - This pre-load only happens once per instance.
- [ Queue ] Added new getter properties:
  - `CurrentSize` for the count of entries currently in the queue.
  - `MaxSize` for the maximum number of entries the queue allows.
  - `IsEmpty` for checking if there are any entries currently in the queue.
  - `IsFull` for checking if no more entries are currently allowed to be added.
  - `WillBeEmpty` for checking if the queue is expected to empty out next.
    - This applies only during the `_TvMediaEnd` event and can be used to predicatively add something to the queue.

### Changed
- [ Core ] Update the start + version log message to an ALWAYS level.
- [ Core ] Delay the retry of internal ready from 2 to 5 seconds in order to allow for extra time for the data to be received.
- [ Core ] Add internal check to delay the first `_PostDeserialization` call by 2 frames to allow for other scripts to update their own sync data first.
- [ Core ] Condense the log statements from the `_PostDeserialization` into one log statement for brevity.
- [ Core ] Integrate manual time sync into the TVManagerData script for more precise time updates.
- [ MediaControls ] Info text will no longer display the player name is the TV is not syncing to the owner.
- [ Dependency ] Update ArchiTech.SDK minimum to 0.7.9.

### Fixed
- [ Core ] Fix the order of operations for how the `_PostDeserialization` data is handled in relation to the internal ready up call.
  - This fixes a handful of edge-case race-conditions for plugins that expect the synced data to be present during the `_TvReady` event
- [ Core ] Fix bug where autoplayStartOffset would not clear for TVs that were enabled previously but have since been disabled prior to the build phase.
- [ Core ] Fix stranded VideoData properties which are now properly cleared when exiting playmode. (Does not affect anything in-game)
- [ Core ] Restore the ProTVSimple material to the default shader.
- [ Core ] Fix issue where when super auth user joins with enableAutoOwnership, lockedByDefault and allowMasterControl turned on,
  the TV would forcefully take control from the master even though the master was correctly authorized.
- [ Queue ] Fix issue where the original owner of an entry would not correctly be reassigned when they rejoin the instance.
- [ Queue ] Fix issue where the TV would do an extraneous reload if the current owner of the TV was unauthorized when the queue checks for media end event.
  - The expected behaviour is that the next media is not played until an authorized user takes control of the TV and resumes the queue.


## 3.0.0-alpha.21 (2023-07-14)
### Added
- [ Core ] Domain whitelist is now fully implemented and available.
- [ MediaControls ] Add media controls option for whether to visually update the seek bar position every second or every frame.

### Changed
- [ Core ] Implicitly have a one-off loop for media that does not have loop param defined and is less than 30 seconds long.
  - This help handling the issue where non-owners may take a while to load and miss the early part of the media on the first play-through.
- [ Core ] Adjust auth logs to not have an extraneous newline when Trace is disabled.
- [ Core ] Fix first video load being incorrectly delayed for remote users.
- [ MediaControls ] Update the Resync action to include updating the info.

### Fixed
- [ Core ] Fix bug that would crash the TVManager script when no plugins were attached.
- [ Core ] Fix race condition issue related to owner vs non-owner loading state.
- [ Playlist ] Fix entries not being interactable until the scrollbar was triggered.

## 3.0.0-alpha.20 (2023-07-13)
### Added
- [ Core ] New `playDriftThreshold` variable for defining a sync drift tolerance during playback. Defaults to disabled (`Infinity`).
- [ Core ] Add integration for fallback textures during the Blit operation.
  - Optionally supports both the default and sound-only fallback textures.

### Changed
- [ Core ] Renamed `pausedResyncThreshold` to `pauseDriftThreshold` for defining a sync drift tolerance while paused. Defaults to disabled (`Infinity`).
- [ Core ] Move all obsolete/deprecated code for TVManager into a separate partial file for better organization.
- [ Core ] Rename certain Action methods for naming consistency, deprecated prior method names.
- [ Playlist ] Cleanup and optimize logging operations.
- [ Queue ] Cleanup and optimize logging operations.
- [ Prefab ] Move all speakers for each TV prefab under a single parent GameObject for organizational cleanliness.
- [ Dependency ] Update ArchiTech.SDK minimum to 0.7.8.
- [ Documentation ] Some initial cleanup and corrections, mainly around field and method names.

### Fixed
- [ Core ] Add missing auth and sync child objects to the generic ProTV creation menu option.
- [ Core ] Fix Skip action not being properly detected by plugins.
- [ Core ] Fix incorrect serialization of old data which caused unity to error out when trying to deserialize during runtime.
- [ Core ] Fix placement of some hint boxes for custom inspectors.
- [ Playlist ] Fix incorrect handling of the 'continue from last entry' logic when TV ownership swapped users.
- [ Playlist ] Fix autoplay causing a soft-lock in Queue when triggered too early.
- [ Queue ] Improve queue entry matching logic for more consistent results.
- [ Queue ] Fix incorrect authentication checks.
- [ Queue ] Fix regression issue which would cause the Queue script to crash after the second entry added was finished.

## 3.0.0-alpha.19 (2023-07-12)
### Added
- [ Core ] New ProTV menu options (both in the Unity menu and the GameObject context menu) for easily creating a new ProTV instance.
  - Generic creates a skeleton copy of ProTV which only has the core scripts present. Any plugins need to be added manually.
  - Prefab creates an instance of the `Simple (ProTV)` prefab in the asset.
  - More prefab options may be added to the menu at a later time. Starting with just this for now.
- [ Core ] Add missing Controls_ActiveState component type migration logic.

### Changed
- [ Core ] Rename shaders for naming consistency.
- [ Core ] Swapped a script from continuous to manual sync to try and mitigate seek timing issues across users.
- [ Core ] Moved some repeating logic into ProTVEditorUtils.
- [ Core ] Inspector error is now displayed if a nested set of TVManagers is detected.
- [ Core ] Inspector warning is now displayed when security conditions are at risk of causing a soft-lock under certain scenarios.
- [ Core ] To improve migration compatability with third-party tooling and plugins, TVManagerV2 component type upgrade has been limited to an explicit menu option.
  - The VPManager and TVManagerV2ManualSync types will still upgrade to the respective component types correctly.
- [ Core ] Add value boundary checks to 3d mode url param to keep values consistent.
- [ Core ] Make _Play and _Pause return early while the TV is in a loading state.
- [ Core ] Restore some 2.x methods as proxies to help with migration compatability with third party tooling.
- [ Core ] EnableGSV now defaults to true.
  - Added some editor logic to ensure that no two TVs have EnableGSV enabled at the same time in editor.
  - Does NOT guarantee that condition during the Udon runtime. That requires manually managing the variable.
- [ Core ] Update messaging in the Obsolete statements of the 2.x component types.
- [ AudioLinkAdapter ] Make the implicit first setup of the speakers selection ignore disabled speakers, or use the first if all are disabled.
- [ Prefab ] Move AssetInfo prefab and a couple others into a generic Misc folder since they aren't explicitly part of a plugin.
- [ Prefab ] Updated asset info prefab to include buttons for discord and vrc group.
- [ Misc ] Further adjustments to images in the asset to improve compression and disk space usage.

### Fixed
- [ Core ] Restored certain methods to the TVManager script to ease the migration process.
- [ Core ] Fix VPManager inspector causing errors when the component was manually added to a GameObject.
- [ Core ] Add null check to prevent unnecessary error log from showing up while migrating from 2.x.
- [ Core ] Fix custom material migration checking against incorrect values.
- [ Queue ] Fix behaviour crashing when a playlist with autoplay queues up media from a _TVMediaEnd event.

## 3.0.0-alpha.18 (2023-07-07)
### Changed
- [ Core ] Additional checks and handling for ensuring that no two TVs have enableGSV active at the same time prior to and during build.

### Fixed
- [ Core ] Custom materials/properties arrays should no longer fail.
  - The old singular values from pre alpha.17 should now be properly carried over into the new array format during build.
- [ Core ] Fix provided prefabs from not having their material/property options updated to the new array format.
- [ Core ] Fix material textures being erroneously removed under certain circumstances.
  - Note: If a custom texture is not provided, the attached texture for the materials WILL be removed when exiting playmode. This is intentional.
  - Other wise if there is a custom texture and auto-resize is enabled, it should size it down to the default 16x16 size when exiting playmode.

## 3.0.0-alpha.17 (2023-07-06)
### Added
- [ Core ] Add support for multiple custom materials and properties to be provided.
- [ Shaders ] Add a fullscreen shader for desktop usage.

### Changed
- [ Core ] Update interactions toggle to support being set prior to the TV being ready (such as through a third-party toggle script).
- [ Core ] Split aspect adjustment into two separate options, one for the physical texture, other for the blit render.
- [ Core ] Update URL param checks to be case-insensitive.

### Fixed
- [ Core ] Fixed edge-case of TVManagerData script being erroneously removed when the parent TV GameObject is disabled.
- [ Core ] Fix skip action not correctly ending the media if no plugins triggered a new url.
- [ Core ] Fix locallyPaused flag being set for the owner which can cause undesired behaviour in certain scenarios.
- [ Core ] Fix volume and audio mode not always respecting the auto-manage flags.
- [ Core ] Fix 3D mode not respecting aspect correctly in all requisite scenarios.
- [ MediaControls ] Fix seekbar interaction state being erroneously enabled in certain scenarios.

## 3.0.0-alpha.16 (2023-07-01)
### Changed
- [ Core ] Add sync data for whether the owner is loading or not.
- [ Core ] Modify async loops to only run when the game object is enabled.
- [ Core ] Rework ready up logic for handling proper waiting for owner data and handling the fallthrough when data does not actually get received.
- [ Core ] Rework skip action logic to avoid forcing the timestamp to the end while playing a video.
- [ Core ] Rework media buffering to take into account the owner's loading state and possible failure states.
- [ Core ] Adjustments to handing of the jumpToTime for cleaner seeking.
- [ Misc ] Updated various colors for class names in the log outputs.
- [ Misc ] Add specific android overrides for images to get better compression for size in worlds.
- [ Misc ] Update log color prefix for multiple components.

### Fixed
- [ Core ] Prevent an "accessing destroyed object" error when running the build checks against a project upgraded from 2.x.
- [ Core ] Non-owners should no longer have the video start as paused when the owner isn't in a stopped state.


## 3.0.0-alpha.15 (2023-06-28)
### Added
- [ Core ] Per-frame caching of the authentication checks.
  - This means the first time *per frame* that `_IsAuthorized` or `_IsSuperAuthorized` is called, the result is cached *for the current frame only*.
  - If one of the auth methods are called with a different user from the cached value, it will rerun the logic and cache the new result.
  - This is meant to both mitigate excessive logs, but also to reduce the number of calls to any third party logic that may be implemented via TVAuthPlugins.
  - Both auth checks have their own respective cached value.
  - This also allows plugins to call the checks however they need to without incurring a lot of extra repeated overhead.

### Changed
- [ Core ] Further tweaks to stabilizing the auto-ownership feature.
  - Notable: if the TV is locked down and the instance owner leaves then comes back, they will take back ownership and seamlessly resume playing where everyone else was at.
  - Notable: Has improved handling of disabling the root TV object for situations like occlusion culling/portals in worlds or manual user-accessed toggles.
- [ MediaControls ] Make it easier to click on and manipulate the volume and seek sliders in the prefabs.
- [ Playlist ] Search and Sort now have U# compatible API method for triggering their respective actions without needing a UI input field.
  - The normal generic udon methods are still available as well.
- [ Playlist ] Modify the loading bar update logic to only run when necessary.

### Deprecated
- [ Core ] `_GetVideoManager` is deprecated in favor of the `ActiveManager` getter property

### Fixed
- [ Core ] Media no longer forcefully seeks to the end of the previous media when a new one is loading in.
- [ MediaControls ] Prevent interaction with Seek if user does not meet the TV's authorization requirements.
- [ MediaControls ] Disable allow rotation and tight packing on the neon ui sprite atlas as that causes issues with rendering in-game.
- [ MediaControls ] Input field active state should now properly respect all authorization requirements of the TV.
- [ Playlist ] In the playlist file, the title line prefix no longer requires being immediately before the description.
  - For legacy playlists, the unprefixed title line still requires being after all other prefixed lines.
- [ Prefab ] Fix lost reference for video swap dropdown arrow icon.


## 3.0.0-alpha.14 (2023-06-25)
### Added
- [ Core ] New synced property `addedBy` which stores the descriptive name (generally displayName) of the user who submitted a link.
  - This is commonly used as a sort of 'pass through' value for plugins to have a usable player string even if that user leaves the instance before the media plays.
  - It also can help track down users who entered malicious/undesirable links.
- [ History ] New plugin `History` is now available.
  - This plugin will store the most recent (configurable up to 50) media changes in reverse chronological order (most recent at the top)
  - Each entry in the history list has a corresponding Restore/Replay button which you can click to immediately play or re-queue said entry.
  - The Restore/Replay button will not be enabled if the main and alt urls are the same as what the TV currently has, or if the local player does not meet the authorization requirements.
- [ Playlist ] Add missing getter `CurrentEntryDescription`

### Changed
- [ Core ] The `_TvMediaReady` event will now be sent _after_ the buffering has completed instead of before.
- [ Misc ] Moved editor scripts into relevant subfolders.

### Removed
- [ Core ] AspectRatio is no longer provided to the VideoData matrix since the value is now implicitly handled via internal blit operation.
  - Shaders can implement their own additional aspect ratio adjustment if desired.
  - Any shaders using `[_Udon]_VideoData._34` should update to an custom shader-specific aspect ratio property.

### Fixed
- [ Core ] Corrections to the rendering pipeline logic
  - Make blit texture respect the aspect ratio when the flag is set.
  - Make aspect ratio rendering be skipped during blit if a 3D mode is enabled. Downstream 3D shaders should handle any aspect adjustment themselves.
  - If aspect ratio is set to 0 at all, the aspect adjustments will be completely skipped.
    - This also applies to the provided `ProTV/VideoScreen[Global]` shaders.
- [ Core ] Fixed seeking inconsistencies under certain conditions.
- [ Core ] Fixed 3D shader usage being broke.

## 3.0.0-alpha.13 (2023-06-14)
### Added
- [ Core ] TVPlugin now uses `_TvMediaReady` event as a more contextually accurate name. This replaces the `_TvMediaStart` event, which is now deprecated.
- [ Core ] Add new `Force Aspect Ratio` rendering flag for whether you wish to have the blit operation enforce the TV's expected aspect ratio value.
  - If you have multiple materials with different desired aspects, you can leave this flag unchecked and handle it yourself in the shader.
- [ Playlist ] Importing a playlist will now output the execution time that the import process took into the unity console.
- [ AudioLinkAdapter ] New logic for handling an anticipated future feature of AudioLink. This logic will be disabled entirely until the feature has been published in a future version.
- [ Misc ] New UI theme `Neon`. Not implemented in any prefabs yet.
- [ Misc ] New helper menu option for resetting the Z scale of UI children.
- [ Docs ] New set of instructions explicitly for upgrading from 2.x to 3.x.
- [ Docs ] Add summary documentation to the `TVPlugin` script.

### Changed
- [ Core ] Rework the internal rendering pipeline to remove the need for the AVPro flag to handle those quirks.
  - This means that all corrections are handled internally.
  - If you provide a render texture, you can reference that from ANY shader. Works 100% with Unlit/Texture so it should work with anything else.
- [ Core ] Updated the structure of the VideoData matrix.
  - The aspect value (`m22`/`_33`) has been moved to (`m23`/`_34`).
  - The error state value (`m11`/`_22`) has been moved to (`m12`/`_23`).
- [ Misc ] Rename icon files to have a relevant prefix for the theme it belongs to.
- [ Misc ] Insert additional TRACE level logs for additional debugging help.
- [ Dependency ] Updated ArchiTech.SDK minimum to 0.7.0.
  - Notably this includes optional integration for the VUdon Logger utility.

### Deprecated
- [ Core ] `_TvMediaStart` in favor of the more correctly named `_TvMediaReady`

### Removed
- [ Core ] The AVPro flag (was `m12`/`_23`) has been removed from the VideoData matrix.

### Fixed
- [ Core ] The VideoManagerV2 and TVManagerV2ManualSync scripts should now be auto-updated to the respective VPManager and TVManagerData components during the upgrade process.
- [ Core ] Fix seek controls being assigned the incorrect UI event target on build.
- [ Core ] Put `_TvSeekChange` event where it should have been, but was missing or using the incorrect event name.
- [ Core ] Tentative fix for edge-cases where user-user sync would break unexpectedly.
- [ Core ] Tentative fix for edge-cases where the owner state would be incorrectly synced.
- [ Core ] Insert missing `OUT_TITLE` assignment when sending the `_TvTitleChange` event.
- [ Playlist ] Fix missing logic for updating contents of certain TMP elements during runtime.
- [ Playlist ] Fix cache issue when trying to reimport the same playlist in editor where the entry count has been changed.
- [ Playlist ] Fix playlist editor getting stuck in an infinite loop when trying to import by file.
- [ Misc ] The `Add TMP to Dropdown` menu option should now be correctly greyed out when the selected object does not have a Dropdown attached.


## 3.0.0-alpha.12 (2023-06-08)
### Added
- [ Prefab ] Any Vert/Standard/Advanced controls that were missing a video player swap has had one added to it.
- [ Core ] New TVDropdownFix that goes on any video player swap dropdown object to fix stupid unity behaviour and also handle related TMP text.

### Changed
- [ Core ] Additional changes to certain authorization checks for handling specific edge-cases when auto-ownership is enabled.
  - If you notice issues with authentication control, please report it.
- [ MediaControls ] When clicking the OK button on the in-game keyboard, if only one input is present it should now automatically send the input to the TV.
- [ Prefab ] Moved the clear input buttons for the PlaylistSearch and PlaylistSort prefabs to avoid interference with the input field and in-game keyboard.  
- [ Playlist ] Modified how the playlist entries are counted for the inspector.
  - This should reduce the amount of lag experienced when dealing with massive playlists.
  - Note: This does not eliminate all lag, because any sufficiently massive playlist is going to be a bit laggy due to the quantity involved.

### Removed
- [ Misc ] DropdownTemplateFix is removed in favor of TVDropdownFix
  - These two scripts have different uses and targets, so a new script was required.
  - The is a straight up removal because the original script was never released outside the alpha.

### Fixed
- [ AudioLinkAdapter ] The adapter should now correctly respond to the TV's Ready state.
- [ Misc ] Updated all UI objects to avoid explicit z=0 scale since that breaks TMP rendering.

## 3.0.0-alpha.11.2 (2023-06-01)
### Fixed
- [ Misc ] Remove invalid using statements.

## 3.0.0-alpha.11.1 (2023-06-01)
### Changed
- [ Dependency ] Updated ArchiTech.SDK to 0.6.0

## 3.0.0-alpha.11 (2023-05-24)
### Added
- [ Core ] New option to prevent users who are not authorized from playing media on the TV.

### Fixed
- [ Core ] Fix bug introduced in alpha 9 for authorized users not being able to play URLs when the TV is locked.


## 3.0.0-alpha.10 (2023-05-24)
### Added
- [ Misc ] Add editor window utility `ProTV/Enable Media Playback In Unity` for easy importing of the VideoPlayerShim tool.
  - This will open the download URL of either AVPro or VideoPlayerShim for the user to download.
  - The user then needs to navigate to the download location to select the package for importing.

### Changed
- [ Core ] Only hide the legacy screens and video managers if there are more than one related manager involved.  
- [ Core ] When using GSV without an explicit RenderTexture, an implicit temporary RT will be used.
  - This mitigates the explicit need to use the _Udon_VideoData info just to fix AVPro's shenanigans. Just use the _Udon_VideoTex as usual.
  - This also comes with a flag on the TVManager component for whether to apply the aspect ratio to the implicit RT.
- [ Playlist ] Enforce using PlaylistData when importing a text playlist with entry count more than 100
- [ Playlist ] Hide fields on PlaylistData to help make the inspector not lag (they aren't needed as it's visible on the parent Playlist component)
- [ Playlist ] If a playlist import is more than 100 entries, the PlaylistData component will automatically be used.
  - This fixes early interaction lag issues.
- [ Playlist ] Fix issues with the template references being incorrectly cached in certain scenarios
- [ Queue ] Fix issues with the template references being incorrectly cached in certain scenarios
- [ Misc ] Mitigate various minor null pointer issues
- [ Misc ] Make TMP injector respect the original label's font auto-sizing and color properties. 

### Fixed
- [ Misc ] Fixes for migration from 2.x
  - Fix issues with TMP being freshly introduced.
  - Include additional checks to playlist and queue to ensure the component references are preserved as best as possible.
  - Restore old UIShapeFixes script and add build check to remove them entirely from the scene on build check.
  - Add mitigations against Unity breaking U# references between old and new scripts.


## 3.0.0-alpha.9.1 (2023-05-22)
### Fixed
- [ Misc ] Fixes for the migration from 2.x
  - Fix placement of CS0618 warning disable to prevent unnecessary warnings in the console.
  - Move isPrefab flag outside of audiolink compile flags for TVManagerEditor.
  - Add back deprecated materials from 2.x and modify the shader reference to the new default VideoScreen shader.

## 3.0.0-alpha.9 (2023-05-22)
### Added
- [ MediaControls ] New flag for video player swap dropdown to enable injecting generic TextMeshPro text into it at build-time.
  - If you want to customize the Text Mesh Pro elements, you can bake the TMP elements in it via right-click 
    on the desired GameObject with a Dropdown component and select the `ProTV/Util/Add TMP to Dropdown`
  - This will create all requisite game objects and components that you can then modify.
- [ MediaControls ] Add optional localtime display (TMP supported)
- [ MediaControls ] When tv is locked and you aren't the owner, if you are authorized, clicking the lock with first take ownership of the TV, second click will then unlock it. 
- [ Playlist ] New option in the playlist generator for whether to include the playlist title in the entry titles or not.
- [ Auth ] Optional _TvReady event exposed for auth plugins.
  - This runs immediately before the managed _TvReady events for any TVPlugin scripts attached.

### Changed
- [ Core ] Stabilize the ownership handling between late-joiners, disabled object states and auto-ownership.
- [ Core ] The `disableAutoOwnership` has been renamed to `enableAutoOwnership` for easier understanding, logic has been updated accordingly.
- [ Core ] Internal disable check added to allow the _Stop action to halt a video that is loading when the gameobject has been disabled during said loading state. 
- [ MediaControls ] Update the video player swap's TMP implementation from a secondary TMPDropdown to a few embedded TMP text fields on the original dropdown.
- [ Prefab ] Added a copy of the TMP default asset with the known working shader pre-selected and assigned that font asset to all prefabbed TMPUGUI components.
  - This should prevent the issue of the default TMP asset using a shader that doesn't seem to render correctly, by having the embedded material use the correct shader.

### Fixed
- [ Core ] Internal wait times now only respect the highest value.
  - This prevents situations where an expected wait time is incidentally updated to be less than it was originally, causing undesired race-conditions.
- [ MediaControls ] Fix video player swap TMP elements not being updated correctly.
- [ MediaControls ] Input field visibility should now correctly reflect the lock state under all requisite conditions.
- [ Misc ] Provide alternate default font asset which has the SSD shader in use so the font stops failing to render correctly out of the box.
- [ Misc ] 3d logo no longer missing materials
- [ Misc ] Resize some images for better compression
- [ Misc ] Menu operation `GameObject/ProTV/Util/Add TMP to Dropdown` now correctly respects Undo operations.

## 3.0.0-alpha.8 (2023-05-15)
### Added
- [ Core ] New property `enablePixelExtraction` which determines if the blit operation should be pulling pixels from the active video texture.
  - If you want to freeze the pixels on a particular frame, just set this property to false. Set to true to resume the extraction loop.
- [ Core ] New `GetPixels` API methods which pull from the most recent successful pixel extraction via AsyncGPUReadback.
- [ Core ] New auto-ownership mechanism for helping handle cases where the owner disables the TV object due to various reasons (such as occlusion culling).
  - You can prevent the auto-ownership from happening if you turn on the `Disable Auto Ownership` option under the security settings section.
- [ Playlist ] TextMeshPro support added.
- [ Playlist ] (**For Advanced Users Only**) Custom entry indicators can now be defined in the playlist file.
  - This is for those who wish to use entry item prefixes that aren't the default ones (ie: `@^/#~`)
  - You can do this by having the first line start with a question mark `?` followed by the symbols you wish to use.
  - If there are no spaces between the symbols, it assumes each prefix only has one symbol. (eg: `?@^/#~`)
  - If you want your prefixes to contain multiple symbols, make sure to put a space between them. (eg: `?@ ^ // # ~`)
  - The symbols are hardcoded to the order: MainUrl, AlternateUrl, Image, Tags, Title
- [ Prefab ] Added a copy of Unity Video to multiple prefabs by default.
- [ Prefab ] Added video player swap element to both Standard and Vert UI panel variations.
- [ Misc ] Added option to playlist generator to include/exclude the youtube playlist title from the playlist entry titles.

### Changed
- [ Core ] Minor improvements to the BuildChecks logic
  - Prior dry-run flag is new respected by the editor window
  - Prior execution time now shown in the editor window
  - Additional sub/related scope objects now represented by a layout indent below the primary scope object.
    - Remember, you can access the BuildChecks at any point via the `ProTV->Build Log` menu
- [ AudioLinkAdapter ] Modified how the version defines for AudioLink
  - The namespace will be changing for AudioLink v1.0.0 by removing the VRC prefix from it.
- [ MediaControls ] Improved TMP support, especially for dropdowns.  
- [ Prefab ] Reorganized the folder for less confusion.
  - The most straightforward prefab `Simple (ProTV)` is at the top level. More complex/specific-use prefabs are within sub-folders.
- [ Misc ] Various tooltips and descriptions have been updated.
- [ Misc ] Updated parameters used with ytdl usage for playlist generators to improve the speed of fetching information.
- [ Dependencies ] Update ArchiTech.SDK to 0.4.8

### Fixed
- [ Core ] Fixed errors on TVManagerData that would happen when you disable one of the parent objects to the script.

## 3.0.0-alpha.7 (2023-04-18)
### Added
- [ Core ] New `_TvSeekChange` plugin event added.
  - This includes a new plugin variable `OUT_SEEK` which will be assigned the normalized (0 to 1f) value of where the seek is on the timeline.  
  - If you want to get the actual timestamp you can multiply OUT_SEEK by the tv's videoDuration value.
  - Note: MediaControls will only trigger this event once when a user starts to drag a seek slider, and once more when the user releases the slider.
- [ Core ] Add auto-populated list of detected `TVPlugins` connected to the selected TV.
- [ Core ] Add initial foundation of `TVAuthPlugin` setup for being able to define custom authentication schemes for defining who has what permissions for the TV.
  - This setup is split into two methods for now:
  - `_IsAuthorized` for permission to control the TV even while it's locked (so long as a super user didn't lock it first).
  - `_IsSuperAuthorized` for effectively 'admin' permissions within the TV. This will have the same permissions as an instance owner.
  - NOTE: Only one TVAuthPlugin is allowed to be attached per TV. (TODO expose better error/correction measure to user when multiple are detected)
- [ Core ] Add new seekOffset value, which defines how far into the past the media should be kept in relation to the owner's sync seek position.
- [ Core ] Optional parameter `suppress` added to `_ChangeSeekPercent` and `_ChangeSeekTime` for whether or not to trigger the event calls when updating the seek.
- [ MediaControls ] Add corresponding seekOffset UI element component for enabling users to control their own sync delay.
  - The primary use-case for this is for karaoke worlds that need to have the listeners intentionally delay the media to match up with the signer.
  - NOTE: This has not had thorough QA testing yet. Be careful when using this for now.

### Changed  
- [ Core ] Reworked some internal handling of connecting up the TVManagerData component.
- [ Core ] Moved the flag for RenderTexture aspect ratio off the shader into the TVManager so it can be on a per-TV basis without requiring new materials to be made.  
- [ Core ] Move the username whitelist from the TVManagerData into it's own TVAuthPlugin script and add a copy to each provided TV prefab.
- [ Core ] Updated internal `lockedByInstanceOwner` to `lockedBySuper` to correctly align with it's usage.

### Deprecated
- [ Misc ] Added deprecated types that match the old class names from 2.x to try an improve backwards compatability.

### Fixed
- [ Core ] The initial media refresh was not properly being triggered for the autoplay data.
- [ Core ] Add null check for speakers to prevent VPManagerEditor throwing errors when an AudioSource is deleted from the UnityVideo speakers array.
- [ Core ] Tentative fix for some UdonBehaviour crashes upon leaving the world/exiting playmode. Generally a non-issue, but hopefully helps avoid some confusion.


## 3.0.0-alpha.6 (2023-04-10)
### Added
- [ Core ] Add first class support for Custom Material to the TVManager.
  - The shader variables `_VideoTex`, `_VideoTex_ST` and `_VideoData` are provided to the custom material.
  - `_VideoTex` is the Texture reference for the currently active media.
    - If there is no active media, the `_VideoTex` may be null. We can check for the presence of the texture by examining if `_VideoTex_TexelSize.z > 16`.
    - This is a configurable property on the TVManager settings, so you can target any texture property for your custom shader.
  - `_VideoTex_ST` is the scale/offset value used to correct the texture flip from AVPro when dealing with Quest. If you have a custom shader, make sure this is applied correctly.
    - If the `_VideoTex` property is changed in the configuration, this variable also is modified.
    - eg: Change `_VideoTex` to `_MainTex` and this goes from `_VideoTex_ST` to `_MainTex_ST`
  - `_VideoData` is a float4x4 (aka matrix) storing various bits of data related to the internal state of the TV.
    - For the specific meaning of the values, please examine the summary info located at `./Runtime/Core/TVManager_Helpers.cs::updateShaderData`
    - This is primarily a temporary location for the information. More detailed documentation on this is forthcoming.
    - This is not configurable and must be explicitly accounted for by the shader to make use of the data.
- [ Core ] Add first class support for RenderTexture to the TVManager.
  - By default the RenderTexture will apply the respective aspect ratio to the input texture during the blit operation.
  - You can disable this by unchecking the `Apply Aspect Radio` option on the TVBlit shader
- [ Core ] Add first class support for Global Shader Variables
  - The global shader variables (GSV) are effectively the same as the custom material variables, but prefixed with the requisite `_Udon`.
  - `_Udon_VideoTex`, `_Udon_VideoTex_ST`, `_Udon_VideoData` are the GSV.
  - There is a flag you can turn on and off on the TV to enable the writing of data to the GSV: `enableGSV`
  - If you plan on swapping the GSV between TVs, make sure to set the flag to false on the previous TV.
  - NOTE: If you enable GSV _while also having a RenderTexture in use_, the GSV texture will be that RenderTexture reference.
- [ Core ] New `_TvTitleChange` event will fire anytime the TV's title value is modified.
- [ Core ] If AudioLink is detected in the project, TVManager will expose a button to automatically add and assign AudioLink to the TV.
- [ Core ] Default aspect ratio is now defined on the TVManager component. This will be the default value in the shader data for when no media is active.
- [ Core ] Add new context menu items for adding an AVPro or Unity video player to the TV.
  - When you right click, if the game object is a TVManager or child thereof, there will be 2 entries available under the ProTV menu.
  - These will add either an AVProVideo player or UnityVideo player to the respective TVManager as a child game object, including the necessary material and speaker setups.
- [ Core ] Add explicit option to choose whether quest prioritizes the alternate or main URL.
- [ Shader ] Add texture option for when only audio is detected by the TV.
- [ MediaControls ] First class TMP support for info and timestamps
- [ MediaControls ] Can now toggle between displaying the current time or remaining time.
  - All prefab have been updated to support this feature.
- [ MediaControls ] Add missing Volume Indicator option to the custom editor.
- [ MediaControls ] Properly setup the MediaControlsEditor including adding first-class TMP support.  

### Changed
- [ Core ] Complete rework of all material usage.
  - All TV prefabs now default to using a render texture blit and a single material and mesh for the screen.
  - Duplicated screens have been removed from the prefabs.
  - You can technically still use the old shared material method if desired.
- [ Core ] TVManager is now split into partial classes for better organization.
- [ Shader ] Rework the usage of the default/fallback texture
- [ MediaControls ] Cleaned up the 'classic' style icons.
  - Separated the icon from the background.
- [ MediaControls ] Some internal field names have been updated
- [ MediaControls ] Volume and Seek now does a soft-update while dragging the slider.
  - On first click it will seek once, then while dragging it will only update the preview timestamp. 
  - Once released, it will then trigger the seek to the new timestamp.
- [ Shaders ] Improvements to the 3D Mode of the provided screen shader. 
  - **STILL CONSIDERED EXPERIMENTAL! YOU HAVE BEEN WARNED!**
- [ Prefab ] The _Prefab folder has been moved from Runtime to Samples.
- [ Prefab ] Updated all stuff in the demo scene and prefabs to match up with the latest set of changes (icons, blit, etc)
- [ Dependencies ] Updated ArchiTech.SDK dependency version to 0.4.4
- [ Dependencies ] Updated minimum allowed AudioLink version to 0.3.2

### Deprecated
- [ Core ] Due to the new way that materials/shaders are handled, the VideoManager (VPManager) screens is now removed from the editor script.
  - The logic is still present for legacy TV setups that might exist, but those should be upgraded to the new format.
  - The logic will be removed in a future version.
- [ Prefab ] ALL PREVIOUS PREFABS HAVE BEEN DEPRECATED! They are now located in the `./Samples/Prefabs/_v2 (deprecated)` folder.
  - These are only retained for backwards compatibility during an upgrade. 
  - PLEASE AVOID USING THESE AND UPDATE ANY USAGE TO THE NEW COPIES IN THE `./Samples/Prefabs` FOLDER.
  - This is to clear out any prior issues that Unity has had with the older prefabs before.

### Removed
- [ Core ] Old materials that are no longer used.
- [ Core ] URLResolverShim has been moved to [it's own project](https://gitlab.com/techanon/videoplayershim/-/releases) for use by video players other than ProTV

### Fixed
- [ Core ] Looping should work properly again.
- [ Core ] Tentative fix for title sync changes happening when no URLs were changed.
- [ Core ] Some edge cases around build checks for screens and speakers have been resolved.
- [ MediaControls ] Add event suppression for volume to prevent event spam while dragging a volume slider.
- [ Queue ] Fix queue not being able to properly loop when only one persistent video is present.
- [ AudioLinkAdapter ] Fix custom editor not properly init-ing the speakers when freshly added to the scene.


## 3.0.0-alpha.5 (2023-01-04)
### Added
- [ Queue ] Rewrote the entire plugin from the ground up.
  - You can now customize the max length of the queue between 5 and 30 entries.
  - Template object is now supported for custom UIs.
  - URL input has been removed for the time being, instead attach the Queue to a MediaControls or Playlist component.
  - First-class support for TextMeshPro usage built-in.
  - Privileged users can now mark any queue entry as persistent.
  - Users can now switch to any queue entry. When TV is locked, switching is limited to privileged users.
  - Queue can now be looped similar to Playlist.
    - Note: Current implementation has it where if the playlist loops with less than 2 entries remaining,
      it will defer action to other plugins. (eg: an attached autoplay-enabled playlist will be allowed to be triggered)
- [ SkyboxSwapper ] You can now specify the entire ColorBlock data which will be assigned to all Selectable children using the Color Tint setting.
- [ Shader ] Add _MirrorFlip option to be able to disable the auto-flip in mirrors.

### Changed
- [ Core ] `TVManagerData._RequestSync` has been renamed to `TVManagerData._RequestData`
- [ Core ] Switched all naming of url references to be consistent with the pc/quest duality.
  (eg: `IN_URL`/`IN_ALT` is now `IN_PCURL`/`IN_QUESTURL`)
- [ Misc ] Replaced various Update loops with more efficient delayed event usage.
- [ Prefab ] Updated any non-script-dependent Text component to a TextMeshPro equivalent. 
  - Future updates will bring first-class TMP support into other plugin separately.
  - Note: Not all of the prefabs in the _MediaControls/Micro_ folder have been updated with TMP.
  These are undergoing some revision for a later update.
- [ Prefab ] Updated all TV prefabs to have simplified screen usage and updated materials.
- [ Demo Scene ] Updated all non-prefab texts to TMP for better readability.

### Removed
- [ SkyboxSwapper ] Removed explicit UI element references in favor of implicitly assigning colors where appropriate.

### NOTES
- You will need to go to any playlists you have in the scene and click the "Update Scene" button.
- Until TMP_Dropdown gets exposed in Udon, reactivity for the video player swappers using TMP will broken. 
The functionality will still work per dropdown, but they will not dynamically update in a reactive way.
  - If you want that reactivity back, simply delete the TMPDisplay variation of the dropdown in any respective prefab,
  and then set the scale of the non-TMP dropdown back to 1,1,1 


## 3.0.0-alpha.4 (2022-12-07)
### Changed
- [ Core ] Pulled in changes from 2.3.12

### Fixed
- [ AudioLinkAdapter ] Add missing save call on adapter objects to retain changes to the scene.

## 3.0.0-alpha.3 (2022-12-03)
### Added
- [ AudioLinkAdapter ] New `Allow AudioLink Control` option to specify if the adapter should interact with the AudioLink instance in the scene.
  - Build checks will no longer require AudioLink if no adapters specify allowing AudioLink control.
  - If no AudioLink is in the scene, AudioLink control will also be skipped.

### Changed
- [ Core ] Pulled in changes from 2.3.10 & 2.3.11
- [ AudioLinkAdapter ] AudioLink is now optional. You can use this plugin to manage world audio without needing to include an AudioLink instance.

### Fixed
- [ Playlist ] Fix null pointer error for internal description cache.
- [ AudioLinkAdapter ] The build flags from the ASMDEF are not yet supported in UdonSharpBehaviours. Add the old `AUDIOLINK` define as a fallback for now.


## 3.0.0-alpha.2 (2022-11-06)
### Added
- [ Core ] Convenient SeekPercent getter field which returns the normalized seek position.
- [ Playlist ] Now supports a new description field.
  - For backwards compatibility, when certain explicit things are missing, the description will roll itself back into the title.
  - This essentially makes the end result in-scene the same as before, but internally it has them being handled distinctly.
  - The title is either the first implicitly non-prefixed line, OR any sequential lines prefixed with the entry title indicator (default is `=`)
  - The remainder text prior to the next entry indicator is simply dumped into the description.
  - This setup is designed to be intuitive, while retaining backwards compatibility with existing playlist file formats.
  - To separate the title from the description, add a game object with a Text component to the playlist template object.
- [ Playlist ] Saving a playlist to file will now open the save dialog to the existing file's directory if a text asset is assigned.
  - This simplifies playlist changes so you can modify in-editor and then easily save back out into the same file the playlist loaded from.

### Changed
- [ Docs ] Rename folder `Docs~` to `Documentation~` for UPM naming conventions.  
- [ Docs ] Rename folder `Samples/ProTV with AudioLink` to `Samples/ProTV AudioLink`
- [ Core ] Pulled in changes from 2.3.9

### Fixed
- [ Core ] Fix improperly syncing urls under certain conditions.
- [ Core ] Fix rare exception where build checks would fail unexpectedly.
- [ Core ] Added some missing compile flag checks for AudioLink usage.


## 3.0.0-alpha.1 (2022-10-14)
### Added
- [ Core ] Additional checks to the build hook script
    - Fixes missing Video Managers array items
    - Fixes UiShape colliders to be less stupid (used to be the UiShapeFixes script)
    - Fixes missing TVManagerData (used to be TVManagerV2SyncData)
    - Fixes missing TV reference on plugins
    - Fixes broken VRCUrlInputFields
    - and much more...
- [ Core ] Editor window for displaying the activity (and errors) of the ProTVCorrections build hook.
  - If an error is detected, the build hook will fail and prompt the user to open to window to investigate.
- [ Core ] New menu entry (ProTV -> Fixes) that contains certain actions utilized by the build helpers script.
  - This allows you to fix certain issues without having to build the scene entirely.
- [ Core ] Add assembly definitions for UPM/VPM compliance
- [ Core ] New custom editors for various scripts (mostly empty, in-place for future changes)
- [ Core ] Custom Inspector for TVManager
  - Default VPManager is now a dropdown instead of array index value.
- [ Core ] Custom Inspector for VPManager
  - Automatically pull in available speakers and screens for selection to be managed by the VPManager.
  - Can provide custom AudioSources and GameObjects seamlessly.
- [ Playlist ] Add new playlist generators under (Menu) ProTV -> Generators -> Playlist from Youtube
  - You can now create playlists from a youtube video (via chapters) or a youtube playlist
- [ AudioLinkAdapter ] Custom Inspector for AudioLinkAdapter
  - Automatically pulls in available speakers for the respective VPManagers
  - Explicitly pick each speaker to use for AudioLink instead of relying on name matching
  - Enforces the use of AudioLink version 0.3.0 or later (the earliest version which is UPM compatible) via the asmdef files.

### Changed
- [ Core ] Update file structure to be UPM/VPM compliant
- [ Core ] Updated the internal architecture to utilize many new U#1.x features.
    - NOTE: THIS CHANGE MAKES IT BACKWARDS INCOMPATIBLE WITH NON-VCC PROJECTS! If you are still not on the creator companion system yet, you will need to use the 2.3 version of ProTV. If you import this into a non-vcc project, it will break things. YOU HAVE BEEN WARNED.
- [ Core ] Improved logic for handling missing references between various scripts to try and rectify issues with unity serialization.
- [ Core ] `tv.localLabel` renamed to `tv.title` and is now synchronized along with the other data.
- [ Core ] VPManager now dynamically collects available AudioSources to choose what should be managed.
- [ Core ] VPManager now dynamically collects available MeshRenderers.
- [ Core ] Renaming of various class and field names. NOTE: THIS IS A MAJOR BREAKING CHANGE
    - `TVManagerV2` is now `TVManager`
    - `TVManagerV2SyncData` is now `TVManagerData`
    - `VideoManager` is now `VPManager`
    - `Controls_ActiveState` is now `MediaControls`
- [ Core ]The `_TvReady` event will now run after the initial waiting period on join instead of during the `Start` event.
    - This fixes certain niche bugs related to initialization.
- [ Core ] Updated the event listener management for a more streamlined API surface. All old methods have been removed in favor of the following:
    - `_RegisterListener`
    - `_UnregisterListener`
    - `_EnableListener`
    - `_DisableListener`
    - `_SetPriorityToFirst`
    - `_SetPriorityToHigh`
    - `_SetPriorityToLow`
    - `_SetPriorityToLast`
- [ Core ] Integrate new ArchiTech.SDK usage into asset. This involves the following:
    - Consistent logging structure with more fine-grained control over what gets logged when.
    - Consistent structure for common functionality across event listeners and managers.
    - Simplify the data setup process.
- [ Core ] TVManager media change events/methods have all been condensed into a single `_ChangeMedia` method
- [ Playlist ] Input variable `SWITCH_TO_INDEX` changed to `IN_ENTRY` for naming consistency with the rest of the asset.

### Removed
- [ Core ] UiShapeFixes script. This logic has been moved into the ProTVHelpers script so it's implicitly handled without extra Udon script requirement.
- [ Core ] All deprecated prefabs and script methods/fields.
- [ Core ] Removed the following TVManager events in favor of the reworked `_ChangeMedia` event
  - `_ChangeMediaTo`
  - `_ChangeAltMediaTo`
  - `_ChangeMediaWithAltTo`
  - `_DelayedChangeMediaTo`
  - `_DelayedChangeMediaWithAltTo`

## 2.3.15 (2023-10-10)
### Added
- [ Misc ] New dialog prompt for directing users to try the new protv version.

### Fixed
- [ AudioLinkAdapter ] Audiolink 1.0 support for the new namespace.

## 2.3.14 (2023-07-17)
### Fixed
- [ Core ] Fix race condition which can cause late-joiners to not correctly sync the initial video load.

## 2.3.13 (2023-04-01)
### Fixed
- [ Playlist ] Fix for playlist prefabs not properly retaining data assigned to them.
  - This should make it so you no longer are required to unpack the prefab.

### Changed
- [ Core ] Tentative fix for issues where late joiners sometime fail to load a video if the TV has an autoplay url assigned on build.
- [ Core ] Additional security checks.

## 2.3.12 (2022-12-07)
### Added
- [ Core ] Add option `secureWhitelist` to enable or disable purging of the whitelist during start. For security reasons, this is enabled by default.

### Fixed
- [ Core ] If a user joins after a video has ended, the media should now correctly seek to the end.
- [ Core ] Tentative mitigation for some situations where a user on-join would not receive synced data within a reasonable amount of time.
- [ Core ] Fix for late-join not properly respecting when the owner has a video selected, but the state is stopped.
- [ Core ] Fix for late-join not always properly syncing time if the video had ended before they joined.
  - Also fixes clicking play on a video after a user joined should properly play the video

## 2.3.11 (2022-11-16)
### Fixed
- [ Core ] Calling play after media has ended should now correctly restart the video for all users when sync to owner is enabled.
- [ Core ] Corrected some edge cases where freshly played media would instantly seek to the end of the video for non-owners.
- [ Playlist ] The playlist should no longer glitch when the entry count fills up less than the height of the container.
  - This issue was also made evident when doing searching, also fixed for that.
- [ Prefab ] Hangout Prefab no longer has both queue and playlist tab both active by default.

### Changed
- [ Core ] Improved sync time when using the seek bar.

## 2.3.10 (2022-11-14)
### Fixed
- [ Core ] TV init phase should now respect the VideoManagerV2's auto-manage flags.
  - This will prevent custom setups on audio sources being messed with when the respective flags are unchecked.
- [ Core ] Add missing logic to clear an internal flag related to manual looping (calling _Play after some media has ended)
  - This caused manual looping to fail to switch to the internal paused state on the second media ending.
  - This should also correct the seek bar not properly updating under the same conditions.

## 2.3.9 (2022-11-07)
### Fixed
- [ Core ] Another attempt at solving the "Unexpectedly Paused on Load for NonOwners" issue.

## 2.3.8 (2022-10-14)
### Changed
- [ Core ] Security updates

## 2.3.7 (2022-10-09)
### Added
- [ AudioLinkAdapter ] New property to flag if the world music should be triggered if the TV is not producing audio (paused or muted)

### Changed
- [ Core ] Another tentative fix for the "Unexpectedly paused after a video loads" issue.
- [ Core ] Moved the _TvVideoPlayerChange event from immediately in the respective UI event, to after the active manager has actually been updated.
  - This also fixes the issue of the _TvVideoPlayerChange not being called until you actually swap video players. It should be called for the initial video player as well.
- [ AudioLinkAdapter ] The speaker associated with AudioLink will now mute itself when the TV is not producing any audio.
  - This fixes the issue where if the world has a 2D audio source for AL (typical for world-wide AL effects),
    anytime the TV has been muted or the internal volume is 0 (effectively mute),
    the AL AudioSource will also be muted implicitly to avoid unintentional audio leaks.
- [ Prefab ] Made all prefabs default paused sync threshold to Infinity to effectively disable the feature because too many people viewed it as unexpected behaviour,
  so it should be something that the creator explicitly enables.


## 2.3.6 (2202-09-22)
### Fixed
- [ Core ] Looping was broken due to the mediaEnded flag not being cleared properly when the loop was triggered.
- [ Core ] _TvReady event should now correctly run once the TV has completed it initial waiting period instead of during the Start phase.
    - This should fix the issue of not being able to play a video from within _TvReady event.
- [ Core ] Late joiners should now properly respect the owner's play/paused state.
- [ Queue ] Fix certain edge cases where when multiple playlists are autoplay, they would all add something to the queue, resulting in excessive queued videos.
    - Now it should just utilize the highest priority playlist (as was the original intent of the integration)
- [ MediaControls ] Prevent accidental div-by-0. Fixes occasional instances where the seekbar would break entirely.

### Changed
- [ Core ] Do not set autoplay offset for TVs that are disabled by default.
    - This prevents excessive offsets when it's unnecessary as the offsets are specifically for avoiding rate-limit spam on-join.
- [ Playlist ] Use delayed media change if TV hasn't finished initializing.
    - This prevents improperly trying to play media before the TV is ready for it.


## 2.3.5 (2022-08-17)
### Fixed
- [ Core ] Use full namespace for the Stopwatch class in build helpers script to avoid naming collisions on compilation.


## 2.3.4 (2022-08-15)
### Added
- [ MediaControls ] Add `_UpdateMedia` event that will change URLs like usual, except that if one of the URLs are empty, it will default to the URL (main/alt respectively) to the one that is already present in the TV.
    - This enables being able to safely change the url for PC or Quest users without having to re-enter the URL for the other platform.
- [ Misc ] ProTV 3D Box model added.

### Fixed
- [ Core ] There were some occasions where when the URL is changed, the non-owners would be put into a paused state unexpectedly even though the TV was not in a locally paused state.
- [ Core ] Updated the build scripts to support arbitrary folder relocation for projects that require a custom folder structure.
- [ Core ] When a subscriber was disabled at start and enabled at some point later, the TV manager would erroneously call _TvReady on subscribers that already had that event called.
    - This fixes an issue where if you have plugins disabled at start (like playlists), when you enable them, it will no longer cause the TV to seek the time to 0 (the start of the media).
- [ Core ] Fix rare instances where media ends but does not trigger the media ending logic.
- [ Playlist ] Playlist autoplay on-load was skipping the queue entirely when a queue was provided.


## 2.3.3 (2022-08-02)
### Fixed
- [ Core ] The skip logic inadvertently triggered intermittently when a player joined, or when some other form of resync occurred.
    - This issue was inconsistent but occurred enough to reproduce and get a fix.
- [ Core ] Regression in recent logic changes that prevented proper media reloading on remote users when the owner clicks Stop then Play.
- [ Core ] Regression in recent logic changes that prevented the local pause from overriding the owner's sync state control.
    - The intended behavior is that when a remote user pauses a video, if the owner presses Pause then Play, the remote user won't be forced into the play state and instead retain the locally paused state as before.
- [ MediaControls ] Quest URL input game object was not being properly hidden for non-privileged users when tv became locked.


## 2.3.2 (2022-07-31)
### Changed
- [ Docs ] Version numbers now use semantic version syntax.

### Fixed
- [ Core ] Calling _IsPrivilegedUser during the start event would sometimes return false due to the TV not having been initialized.
- [ Core ] Extraneous reload occurred for users after lock being called by a user who wasn't the current tv owner.
- [ MediaControls ] Lock button should always be visible to users so they can see if the TV is in a locked state.
    - This is to prevent confusion for users who would otherwise be unable to determine on their own why they are unable to play any media.
- [ MediaControls ] Seek slider on VertControls (Retro) was not using the correct layout settings.
- [ Queue ] TV lock state was not always being respected.
    - While the TV is locked, only privileged users should be able to queue media.
    - Existing media will remain and can be removed by respective users, but new media should not be added by unprivileged users while locked.
- [ VoteSkip ] Was not always respecting the TV lock state.
- [ VoteSkip ] Clarified text display to represent the tv's locked state meaning voting is disabled.
    - While TV is locked, any privileged user can immediately skip the media. The vote ratio is ignored while in the locked state.
    - The voting ratio is hidden while the TV is in the locked state if the user is unprivileged.


## 2.3.1 (2022-07-28)
### Added
- [ MediaControls ] Enable having the media input field send the URL to a queue if available instead of immediately playing it.
    - This bring parity with the Playlist being able to do so as well.
- [ Core ] Added missing sprite atlas for the retro theme icons.

### Changed
- [ Docs ] Updated MediaControls documentation for recent changes.

### Fixed
- [ Prefab ] Corrected certain missing references.


## 2.3 Stable Release (2022-07-27)
### Changed
- Version bump for release


## 2.3 Beta 3.15 (2022-07-27)
### Added
- [ Core ] Option for specifying if the TV should locally stop or pause the currently active media when it becomes disabled.

### Changed
- [ Demo Scene ] Clean up old references and make things more in-line with the latest prefabs available.


## 2.3 Beta 3.14 (2022-07-26)
### Changed
- [ Core ] Reworked the logic for handling the disable/enable state of the TV itself to use the ownerError control logic.
    - This means that owners can now safely disable the TV itself without forcing everyone else to pause!
    - It also tracks when the owner disabled the game object, and when they enable it, it jumps to where the media should have been if they hadn't disabled it so it doesn't interrupt other's viewing experience!


## 2.3 Beta 3.13 (2022-07-25)
### Added
- [ MediaControls ] New 2000's retro UI variations! Includes variations of the Standard and Advanced controls!
- [ Branding ] New QR code images for use in worlds. Is also included in the `AssetInfo` prefab by default.

### Changed
- [ Core ] Moved all UI icons from different locations into a central UI folder.
- [ MediaControls ] Simplified some handling of main/alt url switching logic.
- [ MediaControls ] Renamed 'ClassicControls' to 'StandardControls' as the term Classic is now being used as a style category term rather than a specific prefab reference.

### Fixed
- [ Playlist ] Autofill Quest Urls option was not properly autofilling the very last entry in a given playlist.
- [ Playlist ] After switching to playlist search using text box, on PC the input field would always lag 1 character behind due to the UI events being called before the associated text component was updated. Should work correctly now.


## 2.3 Beta 3.12 (2022-07-21)
### Changed
- [ Playlist ] Updated playlist search to use the text boxes of the input urls instead of the input URLs themselves to (hopefully) allow quest to be able to run searches.
    - If you are implementing a custom keyboard for the search boxes, you will need to remove the Input Field component off the title/tags searches, and switch to modifying the Text component + explicitly calling the `_UpdateSearch` event on the PlaylistSearch script directly.


## 2.3 Beta 3.11 (2022-07-21)
### Added
- [ Core ] New `_Skip` method which is used to force the media to finish.
    - Supports both livestreams and fixed-length videos.
    - Ignores non-owners (the trigger is based on syncTime data).
    - Privileged users can call this and will take ownership before skipping.

### Changed
- [ VoteSkip ] Updated trigger logic to call new `_Skip` method instead of forcing the seek time.
    - This enables proper support for skipping livestreams.


## 2.3 Beta 3.10 (2022-07-20)
### Changed
- [ Prefab ] Fixed broken references to the playlist in the PlaylistQueueDrawer caused by the previous beta.


## 2.3 Beta 3.9 (2022-07-20)
### Added
- [ Core ] New build script logic which automatically updates the options of any MediaControls dropdowns.
    - This removes the need to update the count and labels of the dropdown options.
- [ Core ] VideoManagers now have a custom label you can specify.
    - Currently this is primarily used by the MediaControls for auto-populating video player swap dropdown options.
- [ Core ] New `defaultRetryCount` field for specifying an implicit retry count per-TV instead of per-URL.
- [ Core ] New build helper method to automatically update any MediaControls dropdowns.
- [ MediaControls ] New `customLabel` field which allows you to specify a custom name for the video manager which will then be populated in the dropdowns during the build phase.
- [ Playlist ] Playlists can now autofill the alternate/quest url based on a given format string.
    - In this format string, the main URL will be injected where the special value `$URL` is.
    - eg: If main url is `https://youtu.be/VIDEO_ID` with the autofill format of `https://mydomain.tld/?url=$URL` the result would be `https://mydomain.tld/?url=https://youtu.be/VIDEO_ID`

### Changed
- [ Core ] Reordered the properties on the TVManagerV2 script for better organization, also added new section headers.
- [ Core ] Restored the `startHidden` option as it is still useful for niche situations.
- [ Core ] Enabled local user to be considered a privileged user ONLY IF the tv is currently NOT syncing to the owner.
- [ Queue ] Update prefabs to have a color tint effect on the queue media and next media buttons.
- [ Prefab ] Corrected a couple improper references.
- [ Prefab ] In the PlaylistQueueDrawer, moved the Playlist script onto a game object outside of the toggled parent game object so that it will always properly initialize for autoplay without needing to manually switch the tabs to it.
- [ Docs ] Added missing release dates to various 2.3 beta releases in the CHANGELOG.
- [ Docs ] Updated various documents related to changes in the 2.3 release.


## 2.3 Beta 3.8 (2022-07-18)
### Changed
- [ Core ] Added sanity null checks against the strings contained within VRCUrls for rare instances where unity serialization messes up
- [ Core ] Rename prefab `ProTV Modern` to `ProTV Advanced` for better clarification on the contents of the prefab
- [ MediaControls ] Rename prefab `ModernControls` to `AdvancedControls` for better clarification on the contents of the prefab


## 2.3 Beta 3.7 (2022-08-18)
### Added
- [ Queue ] New `_Purge` event will remove all entries that a player has control over.
    - If a privileged user calls this event, it will wipe the whole queue.
    - If a non-privileged user calls this event, it will remove all of that player's particular entries.

### Changed
- [ Core ] Updated sync data to notify when the owner's video had failed, and ignore certain sync info until a proper change occurs.
    - This should alleviate confusion and issues that are caused by media failing on the owner's side.
    - eg: If a youtube video is loaded when a quest user was the owner, it'd fail for them causing the video to stop for everyone else.
    - NOTE: This will cause an intentional partial desync because the owner media sync fails.
    - If you wish to re-enable full sync without changing videos, you will need a privileged user to either lock, unlock, or reload the video.
- [ Core ] Security improvements.


## 2.3 Beta 3.6 (2022-07-17)
### Added
- [ Core ] New toggle setting for choosing the initial audio mode between 2D(Stereo) and 3D(Spacialized)
- [ Core ] Add 3D support with the new `ProTV/VideoScreen3D` shader.
    - Includes some auto-detection logic for switching between 2D/3D video rendering.
    - Logic is an aspect ratio threshold check, which is adjustable in the shader properties.  
      Defaults to a ratio of greater than 2:1 as the threshold.
    - All provided screen materials now default to this shader.
    - The old shader `ProTV/VideoScreen` is retained for backwards-compatibility.

### Fixed
- [ Core ] Extra null checks against badly formed VRCUrls in certain edge-cases.
- [ Core ] Tentative fix for random edge-case of video failure when a player joins.
- [ Core ] Tentative fix for MediaControls having the input field not being unhidden during media retry if the media fails (eg: livestream offline).


## 2.3 Beta 3.5 (2022-07-11)
### Fixed
- [ AudioLinkAdapter ] Fixed compiliation error when audiolink is not present in the project


## 2.3 Beta 3.4 (2022-07-11)
### Changed
- [ Core ] Change the default value for `retryWithAlternateUrl` to `true`

### Fixed
- [ Core ] Restored missing reference to the MediaInput element in the PlaylistQueueDrawer prefab


## 2.3 Beta 3.3 (2022-07-10)
### Changed
- [ Playlist ] Improved handling of error failures to be more reliable against various edge cases
- [ Playlist ] Reduced excessive reloading of already failed urls when `retryWithAlternateUrl` is enabled

### Fixed
- [ Core ] There were certain situations where the `_TvMediaChange` event was not being triggered on loading a URL
- [ Core ] Added conditional checks for properly handling time jumping when `retryWithAlternateUrl` is enabled
    - This fixes an issue of the media sometimes skipping the first few seconds of the media.


## 2.3 Beta 3.2 (2022-07-10)
### Fixed
- [ Playlist ] Tentative fix for certain edge cases where url failures do not proceed to the next entry correctly


## 2.3 Beta 3.1 (2022-07-10)
### Changed
- [ Core ] Moved the logo image used in the `AssetInfo` from the Docs/Branding folder to the Images folder for being consistent about its use.

### Removed
- [ Core ] Temporarily removed the ProTVUtils script as it is not being used by any existing part of ProTV at the moment.
    - This should fix U# 1.x failing to compile


## 2.3 Beta 3.0 (2022-07-08)
### Added
- [ Core ] Now checks for a url query parameter which typically indicates a proxy service is being used (eg: Jinnai, Qroxy).
    - If detected, the proxied URL domain will be used for the tv's localLabel value instead of the original url domain.
- [ Core ] Add new `AssetInfo` prefab which will automatically display the current version of ProTV along with the gumroad and discord links.
    - Prefab located at `Assets/ArchiTechAnon/ProTV/Prefabs/Misc/AssetInfo`
    - This prefab is embedded by default in each of the MediaControls prefabs behind a very small toggle button, so it generally stays out of the way.
    - The particular toggle icon will be updated to something more appropriate in a future update.
    - The version number in the `AssetInfo` prefab will automatically update whenever you build the world.
    - More specifically, any scene object named `ProTV Version` with a Text component attached will be updated with the version number on build.
- [ Core ] Add new advanced options flag `retryWithAlternateUrl` to allow specifying if you want to automatically try the other URL (main/alt) if the one attempted returned an error.
    - Will flip the flag internally to attempt the other URL (graceful fallback for missing urls still apply).
    - Then if that too fails, it will swap back until the original URL succeeds or a new URL is input.
- [ Core ] Make retry count default to 1 if `retryWithAlternateUrl` is enabled
- [ Core/Shader ] Horizontal auto-flipping in mirrors has been added.
- [ Skybox/Shader ] Vertical flip issues have been corrected.

### Changed
- [ Core/Shader ] Rename `Video/RealTimeEmmisiveGammaWithAspectRatio` to `ProTV/VideoScreen`
- [ All Plugins ] Slight improvement to how the default plugins handle error messaging when the TV reference is missing.
- [ Skybox/Shader ] Rename `Video/Skybox` to `ProTV/Skybox`
- [ MediaControls ] Consolidated logic for `_ChangeAltMedia` into `_ChangeMedia` so the latter now cleanly handles both main and alt url inputs.
- [ MediaControls ] Reworked the `AlternateUrlControls` into a more useful, general-purpose `UrlControls` prefab.
- [ MediaControls ] Fixed some minor issues with the DrawerControls animator.
- [ Playlist ] Improved scroll behaviour for existing prefabs.
- [ Queue ] Improved scroll behaviour for existing prefabs.

### Fixed
- [ MediaControls ] Use new VRCUrl instance instead of the global VRCUrl.Empty instance for the field defaults in QuickPlay script.

### Removed
- [ Core ] Temporarily removed the custom editors for VideoManager and TVManager. These will be rebuilt for the next stable version.


## 2.3 Beta 2.5 (2022-05-27)
### Fixed
- [ Core ] Custom editor for video manager wasn't calling the base.OnInspectorGUI method thus was not populating the inspector.


## 2.3 Beta 2.4 (2022-05-27)
### Added
- [ Core ] Loading timeout limit for preventing infinite loading states

### Fixed
- [ Core ] Fixed stupid typos for usage of the word 'privilege' from the incorrect spelling of 'priviledge'


## 2.3 Beta 2.3 (2022-05-26)
### Added
- [ Core ] Added primitive whitelist name check. Whitelist is available on the TVManagerV2ManualSync object.
    - NOTE: The location of the whitelist is subject to change and notice will be given if it does. The current location is considered a temporary stop-gap until a better solution gets implemented.
    - This whitelisted users have the same control priviledge as the instance master, except that they are not beholden to the `allowMasterControl` check.
    - Instance Owner will always have priviledge over everyone else, as usual.
- [ Playlist ] Add some helper getters for certain data for U# scripts
    - `SortView` returns the list of indices for the current sort order of the playlist
    - `FilteredView` returns the list of indices for the current search filter applied to the playlist (hidden indices won't be present in this array)
    - `CurrentEntryIndex` returns the original playlist entry index that has been detected. If no entry is detected to be active, -1 will be returned.
    - `NextEntryIndex` returns the original playlist entry index for the next expected entry. Primarily used for autoplay mode.
    - `CurrentEntryMainUrl` returns the main VRCUrl for the current active detected entry. Returns VRCUrl.Empty if no entry is active.
    - `CurrentEntryAltUrl` returns the alternate VRCUrl for the current active detected entry. Returns VRCUrl.Empty if no entry is active.
    - `CurrentEntryTags` returns the tag string for the current active detected entry. Returns string.Empty if no entry is active.
    - `CurrentEntryInfo` returns the description (aka title) for the current active detected entry. Returns string.Empty if no entry is active.
    - `CurrentEntryImage` returns the Sprite type image for the current active detected entry. Returns null if not entry is active.

### Changed
- [ Core ] Cleaned up internal usage and exposed a debug flag which will disable logging for a given TV and it's plugins when unchecked.
- [ Core ] Added error message when the manual sync data component is not present.
- [ Core ] Renamed `_CanTakeControl` to `_IsPrivilegedUser` to better describe the purpose of the logic. The method signature for `_CanTakeControl` will simply call the new `_IsPrivilegedUser` method signature for backwards compatibility.
- [ AudioLinkAdapter ] AudioLinkAdapter is now a first-class plugin. It is now located in the standard `ProTV/Plugins` folder!
    - NOTE: Certain changes to make this work *REQUIRES* AudioLink 0.2.8. Earlier version will not properly detect the AudioLink asset.

### Fixed
- [ MediaControls ] Fix lock not showing for instanceOwner correctly under certain conditions
- [ Playlist ] Fix starting on a random entry sometimes causing an array out of bounds failure


## 2.3 Beta 2.2 (2022-05-13)
### Fixed
- [ Core ] Fix default values for video swap input variables to be the correct -1 so that swapping to index 0 works correctly


## 2.3 Beta 2.1 (2022-05-07)
### Added
- [ Core ] Add prefab that specifically uses the UnityPlayer with a RenderTexture
- [ Core ] Add `_TogglePlay` event to be able to use a single event to switch between pausing and playing.
    - If stopped, it will attempt to reload the current media.

### Changed
- [ Docs ] Update README to better reflect the 2.3 beta 2.0 changes.


## 2.3 Beta 2.0 (2022-05-06)
### Added
- [ Core ] Add support for loop/start/end/t params to be defined via the url hash (after the #, more in the docs)
- [ Core ] Add hash param `live` which declares a url is expected to be live media. This helps signal how to handle errors for media that isn't loaded properly.
- [ Core ] Add hash param `retry` which declares how many times a url should be retried before signalling that an error has actually occurred.
    - If set to `-1`, the TV will infinitely retry the url, with 15 second intervals. Useful for livestreams.
    - Any number greater than `0` will make the TV attempt to reload the video up to that number of times before moving on.
- [ Core ] Add autoplay label field to have the autoplay urls replaced with custom text on UIs
- [ Core ] Add _EnableInteractions/_DisableInteractions/_ToggleInteractions to enable a global interaction toggle for any attached subscribers
    - It goes through the event subscribers and searches for all VRCUIShapes, then finds any attached colliders and then either disables or enables them.
    - This makes it so the player's raycast pointer either does or does not interact with it.
- [ MediaControls ] Add alternate url support to the QuickPlay script
- [ Playlist ] Add explicit warning when a playlist has no entries present
- [ Playlist ] New playlist tagging
    - A metadata field for putting search terms in for each playlist entry. Never displayed
- [ Playlist ] New playlist search syntax, supports searching through tags, titles and urls
    - Search for combined terms using a plus (+) and search for optional terms using a comma (,)
        - Eg: "animated+1999,animated+2000" could be used to find animated movies in 1999 or 2000
- [ Playlist ] Add option for a playlist to send the selection to a queue instead of playing immediately
    - To enable this mode, simply connect a Queue component to the Playlist queue slot.
- [ Playlist ] Add alternate url support, including with the file format. Alternate urls are prefixed with `^`.
- [ Playlist ] Add new Save button to bring up a file save menu for much quicker playlist exporting.
    - This will also automatically update the playlist to switch to import mode with the freshly saved playlist assigned.
- [ Playlist ] Add new PlaylistData type for offloading playlist entries onto a separate game object.
    - This helps manage performance issues for VERY LARGE playlists by moving the unity serialization issues onto a game object that the regular playlist script isn't on.
    - Completely optional, will default to use the same game object if PlaylistData is not present.
- [ Queue ] Add alternate url support
- [ VoteSkip ] New VoteSkip plugin and prefab
    - Includes a VoteZone component for handling spacial areas in which the VoteSkip can be used by.
    - This is to make it so that not everyone in the world needs to be involved with the vote skip, just those
      that are currently paying attention to the TV (ie: being inside one of the VoteZones defined).
    - If no VoteZones are provided in the world, then VoteSkip will simply use the global player count in the world
- [ Docs ] Added Docs file for VoteSkip

### Changed
- [ Core ] Renaming/restructuring old prefabs:
    - ProTV Slim > ProTV Classic
    - ProTV Music Player > ProTV Music
    - ProTV Modern Model > ProTV Modern
- [ Core ] Looping is now specified with a count
    - If `loop=0` or if loop is not present in the url, no looping will occur
    - If `loop=-1` (or any other negative number) or if `loop` is present without a value, looping will be infinite
    - Otherwise the video will loop the given number of times after the inital playthrough
    - `loop=1` means the video will actually play just 2 times
- [ Core/Security ] Instance owner now properly overrides control of master, so when the instance owner locks the TV, the master is no longer able to implicitly unlock it.
- [ MediaControls ] Improved the error messaging between live and fixed length media. Should be a bit more clear as to the actual problems that occur.
- [ MediaControls ] TV error messages now respect the showVideoOwner(ID) fields
- [ MediaControls ] Improve error messaging for livestream media
- [ Playlist ] Fix handling of video errors. Only process once the TV has signaled that an error actually occurred.
- [ Playlist ] Searching over multiple frames (async) enabled
- [ Playlist ] (Internal) All references to "rawView" have been converted into "sortView" to more properly align the naming with what that cache is accomplishing.
- [ Playlist ] Renamed `AncientPlaylist` to `FlatPlaylist` and renamed `SlimPlaylist` to `ClassicPlaylist`
- [ Docs ] Updated Changelog to be more organized, added historical dates
- [ Docs ] Moved READMEs for plugins into the Docs folder so they aren't spread across the folders. Renamed the files according to the respective plugins.

### Deprecated
- [ Core ] Deprecating old prefabs:
    - ProTV Legacy Model Extended
    - ProTV Legacy Model
- [ Core ] Deprecate use of loop/start/end/t in the query parameters in favor of having them in the url hash
- [ Core ] Deprecate the longform IN variables in favor of shorter more memorable variable names:
    - `IN_ChangeMedia_VRCUrl_Url` -> `IN_URL`
    - `IN_ChangeMedia_VRCUrl_Alt` -> `IN_ALT`
    - `IN_ChangeVideoPlayer_int_Index` -> `IN_VIDEOPLAYER`
    - `IN_ChangeVolume_float_Percent` -> `IN_VOLUME`
    - `IN_ChangeSeekTime_float_Seconds` -> `IN_SEEK`
    - `IN_ChangeSeekPercent_float_Percent` -> `IN_SEEK`
    - `IN_RegisterUdonEventReceiver_UdonBehavior_Subscriber` -> `IN_SUBSCRIBER`
    - `IN_RegisterUdonEventReceiver_byte_Priority` -> `IN_PRIORITY`  
      These deprecated variables will still work as expected, but will be removed in a later version.

### Removed
- [ Core ] Remove duplicated isMaster call during init causing the script to fail during publish.
- [ Core ] Removed `retryLiveMedia` as the new `retry` and `live` hash params replace that behaviour
- [ Core ] Removed the longform OUT variables in favor of shorter, more memorable ones:
    - `OUT_TvVideoPlayerError_VideoError_Error` -> `OUT_ERROR`
    - `OUT_TvVolumeChange_float_Percent` -> `OUT_VOLUME`
    - `OUT_TvVideoPlayerChange_int_Index` -> `OUT_VIDEOPLAYER`
    - `OUT_TvOwnerChange_int_Id` -> `OUT_OWNER`  
      These longform variables will no longer receive the data and will need to be updated to the new short form to continue working properly.
- [ Playlist ] Removed `Skip to next entry on error` option in favor of it being implicit behavior by default (always enabled).

### Fixed
- [ Core ] Certain edge cases where the start/end params wouldn't work properly
- [ Core ] Immediately after join if video is active, a non-owner would be unable to play/pause locally
- [ Core ] Video Swap not working for late joiners
- [ Core ] Video Swap not retaining the current playing timestamp properly (aka lossless reload)
- [ Core ] Fix audio/video resync not working for livestreams
- [ Playlist ] Lists with 1 entry were not looping correctly.
- [ Playlist ] No longer crashing when 0 entries are present
- [ Queue ] Behaviour no longer crashes when the next media button is not provided.
    - This allows a world creator to have an add-only queue, making it easier to manage via the new VoteSkip plugin.
- [ Queue ] Fix crashing on quest when the title input field was accessed in the code.
    - Because apparently, VRC destroys the InputField components on quest as the way to prevent the keyboard from showing up.
      Why? No idea but that's what's been observed, so we need to do null checks for that stuff.

### <span style="color:cyan">Upgrading from previous versions</span>
If you have imported a previous version of ProTV (2.3 Beta 1.0 or earlier), after importing this version (2.3 Beta 2.0 or later) you will need to do two things.
1) If you have any playlists in the world, simply click on them to show their inspector, then toggle any of the flag options. This is to make it so that the searialized data gets updated to the new struture with tags and alt urls.
2) If you have any queue's in the world, you'll want to delete those and drag in a new copy. Queue has been reworked a bit plus it now has 20 entries in it.  
   This includes the ProTV Composite prefab (now known as ProTV Hangout). You'll want to replace the copy in your scene with a new one since it has a Queue integrated into it.

That should be it.


## 2.3 Beta 1.0 (2022-03-05)
### Fixed
- Fix loop not working properly on TVs that aren't synced to owner
- Tentitive fix for certain situations where synced looping wasn't working


## 2.2 Stable Release (2022-01-24)
### Changed
- Version bump for release


## 2.2 Beta 3.1 (2022-01-22)
### Added
- Add Playlist option for specifying if autoplay should start running on load or wait until interaction: `Autoplay On Load`
- Add Playlist option for specifying if autoplay should make the playlist restart if it reaches the end: `Loop Playlist`


## 2.2 Beta 3.0 (2022-01-18)
### Added
- Add an udon graph compatible `_Switch` event for the playlist, utilizing new variable `SWITCH_TO_INDEX`
- Add optional buffer time for allowing a video to pre-load before playing
- Add descripive text to CompositeUI detailing how to interact with the visibility

### Changed
- Update Resync (Micro) to use the correct icon

### Fixed
- Fix inconsistent animations on the UI for ProTV Composite
- Fix MediaControls info data not being updated at all the points it was supposed to
- Fix owner reloading not doing proper jumpTime to the active timestamp (aka lossless owner video reload)
- Fix skybox settings UI interactions not working as expected
- Fix video player selection sync being improperly implemented
- Updated documentation for new changes


## 2.2 Beta 2.1 (2021-12-14)
### Changed
- Adjust GeneralQueue so that there isn't any conflicting sync types

### Fixed
- Attempts at fixing Unity being absolutlely insufferable with broken references


## 2.2 Beta 2.0 (2021-12-04)
### Added
- Add optional toggle for syncing the current video player selection.

### Changed
- Udpate logic for handling consistent setup of the canvas colliders.
    - If you have your own UI or you unpacked a prefab, you will need to add the new script `ProTV/Scripts/UiShapeFixes` to any canvas gameobject with a `VRCUiShape` component on it. If you are using the prefabs, they should automatically update with the new script.

### Removed
- Remove some udon overhead by switching some scripts to None sync type.


## 2.2 Beta 1.1 (2021-11-10)
### Added
- Add functionality to MediaControls url inputs to be able to submit the URL upon pressing enter.


## 2.2 Beta 1.0 (2021-11-08)
### Added
- Add new Misc folder and new prefab "PlaylistQueueDrawer". Contains new visuals for playlist and queue in a drawer like layout.
- Add new MediaControls prefab "DrawerControls".
- Add new TV Prefab "ProTV Composite". Contains both "DrawerControls" and "PlaylistQueueDrawer" prefabs.
- Add configurable player-specific limit value to the Queue plugin.
- Add support for changing and shifting priorities for event subscribers.
    - First (before all other priorities)
    - High (first of its current priority)
    - Low (last of its current priority)
    - Last (after all other priorities)
- Add udon events for interacting with the new priority shifting mechanism.
- Add playlist integration with priority shifting. This allows for a playlist to prioritize itself when interacted with. Set the new `Prioritize on interact` flag under the autoplay section of the playlist script to enable.

### Changed
- Update Queue to utilize array sync for urls (since VRChat recently added support for that).
- Update URL resolver shim to look for the new ytdlp executable.
- Update playlist init code to prefer the TV's autoplay url field over its own list.

### Fixed
- Fix Queue plugin causing videos to abruptly stop when certain players leave the world.
- Fix looping via play button after video ends causing extraneous events to trigger that shouldn't.
- Fix race condition with seeking where it wouldn't always seek to the desired time depending on when the seek is requested.


## 2.1 Stable Release (2021-10-31)
### Changed
- Version bump for release


## 2.1 Beta 3.0 (2021-10-08)
### Changed
- Updated VideoManger to have separate lists for managed and unmanaged speakers and screens.
    - This is intended to allow for more fine grained control of what the video managers should actually affect.
    - Immediate use case is for dealing with audio link speakers(they are typically at 0.001) that you don't want the volume changed on, but still want to have the TV control the auible speakers' volume.
- Updated example scene to reflect changes.

### Removed
- Removed the autoManageScreenVisibility flag.
    - With the new Unmanaged Screens list, this flag is duplicated functionality.

### Notes
- Existing TV setups should still work as expected after importing. There is fallback logic that exists for handling the previous references that Unity already has serialized. The only exception is if you set the autoManageScreenVisibiliy flag to prevent the VideoManager from controlling the referenced screens. To fix, you will need to add that screen to the Unmanaged Screens list for the same behaviour.


## 2.1 Beta 2.9 (2021-10-14)
### Added
- Add additional null check for the Queue plugin to remove unintended error occuring in non-cyanemu playmode (like build ; publish)


## 2.1 Beta 2.8 (2021-10-14)
### Changed
- Adjustments to the Modern Model and Legacy Model prefabs for the options available. This clarifies some options as well as includes a default Unity player option in the general controls options list. Legacy Model Extended still retains the original list from the example scene.

### Fixed
- Fix playlist unintentionally scrolling when contained within a pickup object (something that moves the playlist's world postition)
- Fix timestamp not being preserved as it used to during a video player swap.


## 2.1 Beta 2.7 (2021-10-13)
### Added
- Add null checks to remove unintended errors occuring in non-cyanemu playmode (like build ; publish)


## 2.1 Beta 2.6 (2021-10-13)
### Added
- Add null check to the video swap dropdown layering check incase the dropdown isn't a direct child of it's associated canvas object. This avoids an incedental script crash, but can cause odd layering issue if it's not a direct child, so be careful.


## 2.1 Beta 2.5 (2021-10-12)
### Added
- Add events on the playlist to manage the autoplay mode

### Fixed
- Fix bad execution ordering between TVManagerV2 and TVManagerV2ManualSync scripts


## 2.1 Beta 2.4 (2021-09-28)
### Fixed
- Fix layering issues and pointer issues with the general controls video swap dropdown


## 2.1 Beta 2.3 (2021-09-27)
### Added
- Add explicit Refresh button to the slim UI prefab.
- Add new VertControls UI prefab. Similar to Slim UI, but layout is vertical with some elements removed.

### Changed
- Updated the icon for the Resync button on the slim UI prefab to be distinct from the Refresh button.

### Removed
- Remove forced canvas sort order for controls UIs.


## 2.1 Beta 2.2 (2021-09-14)
### Added
- Add warning message when a playlist import detects one or more entries that do not have a title associated with it. This is just an alert and can be ignored if the missing titles are intentional.

### Changed
- Minor performance improvment to the playlist editor script

### Fixed
- Fix playlist not updating the TV's localLabel on non-owners
- Fix attempt for videos having issues with looping (stutter and occasionally unexpected pausing)
- Fix playlist producing null titles instead of empty strings causing the search feature to fail


## 2.1 Beta 2.1 (2021-09-07)
### Changed
- Change `useAlternateUrl` to be not exposed to the inspector. It is still a public variable for runtime though.
- Change url logic to have quest default to the alternate, with fallback to main url when alternate is not provided (this allows seemless backwards compatibility)


## 2.1 Beta 2.0 (2021-09-04)
### Added
- Add first class support for alternate urls. This alleviates issues with requiring separate URLs for each platform (notably VRCDN)
- Add toggle prefab to allow switching between Main and Alt urls. This is _HIGHLY_ recommended to have in-world if you make use of the alternate url feature.
- New events related to alternate urls: `_UseMainUrl`, `_UseAltUrl`, `_ToggleUrl`
    - BE SURE TO CHECK YOUR SCENE REFERENCES TO ENSURE THEY ARE CONNECTED PROPERLY. Some variable names changed related to urls and certain references _may_ have become disconnected.

### Changed
- Update `PlayURL (Micro)` prefab to support alternate url. Great for predetermined stream splitting.

### Fixed
- Fix playlist not always correctly representing the loading bar percent while scrolling.
- Fix certain issues with the skybox playlist not working properly.


## 2.1 Beta 1.1 (2021-09-01)
### Added
- Add playlist shuffling via `_Shuffle` event.
- Add option to automaticially shuffle the playlist on world load (currently not synced).
- Add option to start autoplaying at a random index in the playlist.
- Add playlist view automatically seeking to the current index on world load (complements the random index start).

### Changed
- Additional internal state caching improvements for playlist
- Some code golfing micro-optimizations


## 2.1 Beta 1.0 (2021-08-29)
### Added
- Add playlist pagination prefab.
- Add U#'s URL Resolver shim for playmode testing with the unity player (AVPro still doesn't work in-editor yet)

### Changed
- Improve some internal state caching for playlist

### Fixed
- Fix scrollbar not resizing with the playlist when a filter is applied (aka playlist search)


## 2.0 Stable Release (2021-08-26)
### Changed
- Version bump for release


## 2.0 Beta 8.5 (2021-08-22)
### Changed
- Update automatic resync interval to Infinity if value is 0 (both values should represent the same effect).
- Mitigations for when the TV starts off in the world as disabled. Should be able to just toggle the game object at will, though if you want the TV to start off as disabled, make sure the game object itself is off instead of relying on other scripts to toggle it off for you (like a toggle script). There is a known bug with having it on and disabling it during Start. See and upvote: https://feedback.vrchat.com/vrchat-udon-closed-alpha-bugs/p/1123-udon-objects-with-udon-children-initialize-late-despite-execution-order-ove
- Forcefully disable the built-in auto-resync cause it breaks things reeeee
- Improve the skybox options in the demo scene.
- Update playlist structure and logic for vastly improved performance at larger list sizes.


## 2.0 Beta 8.4 (2021-08-08)
### Added
- Add skybox support for CubeMap style 360 video.
- Add skybox support for 3D video modes SideBySide and OverUnder.
- Add skybox support for brightness control.
- Add settings UI to the skybox TV prefab.
- Add custom meta support for the URLs.
    - Can now specify custom data that is arbitrarily stored in the TV in the `string[] urlMeta` variable.
    - All meta entries are separated by a `;` and proceeds a hash (`#`) in the URL.
    - Example: With a url like `https://vimeo.com/207571146#Panoramic;OverUnder`, the `urlMeta` field will contain both `"Panoramic"` and `"OverUnder"`.
    - This meta portion of the URL can be used for pretty much anything as anything as the hash of a URL is ignored by servers. Use it to store information about any particular individual url (such as what skybox modes to apply).

### Changed
- Updated demo scene with new skybox data.

### Fixed
- Fixed entry placement regression in playlist auto-grid.


## 2.0 Beta 8.3 (2021-08-07)
### Fixed
- Fixed playlist auto-grid being limited to 255 rows or columns. Should be able to have many more than that now.
- Fixed playlist in-game performance issues by swapping from game object toggling to canvas component toggling.
    - This specifically fixes lag issue when desiring to hide the playlist.   
      While game object toggling is still supported, this new mode is highly recommended. Is utilized by calling `playlist -> _Enable/_Disable/_Toggle` events.
    - Playlist Search also makes use of this performance improvements by having a canvas component on the template root object (and thus on every playlist entry object).


## 2.0 Beta 8.2 (2021-08-05)
### Added
- Added KoFi support links to the Docs. Support is inifinitely appreciated!
- Added Micro style controls to the MediaControls plugin.
- Added a one-off play url button control to the MediaControls plugin. This has definitely been requested quite a bit.

### Changed
- Cleaned up names of prefabs a bit (no breaking changes)
- Exported with 2019 LTS


## 2.0 Beta 8.1 (2021-07-30)
### Added
- Added better support for plugins being disabled by default getting enabled after the world load phase.
    - This guarantees that AT LEAST the `_TvReady` event will _ALWAYS_ be the first event called on a subscribed behavior.
- Add playlist search toggle for skipping playlists who's gameobject is disabled.

### Changed
- Update Controls script to utilize the new usage of `videoDuration` and to properly display when the time is less than start time (for example if the AVPro buffer bug prevents the complete auto-seek that is expected, it will have the current time be a negative value)
- Change default automatic resync interval to 5 minutes.
- Update VideoManagerV2 to rework the configuration options to have clearer names as well as more precise purpose.

### Fixed
- Fixed support for start/end time usage.
    - Adds script variable `videoLength` to represent the full length of the backing video, where `videoDuration` now represents the amount of time between the start and end time of a video.
- Fixed initial volume not being assigned properly during the Start phase.


## 2.0 Beta 8.0 (2021-07-26)
### Added
- Added pagination to the playlist inspector for easier navigation.- Added playlist search prefab (part of the Playlist plugin system)
    - PROTIP: To add extra text to search by in a title, you can set the text size to any part of the title to 0  
      Such as: `Epic Meme Compilation #420 <size=0>2008 2012 ancient throwback classic </size>`
- Final reorganization of folder structure.
    - The root folder has been renamed from `TV` to `ProTV`
    - Updated documentation to reflect the update folder structure.
    - Anything that used to be in the `TV/Scripts/Plugins` folder is in their respective `ProTV/Plugins/*` folders.
    - All plugin specific files have been moved to the plugin specific folders (eg: `TV/Stuff/UI` -> `ProTv/Plugins/MediaControls/UI`)
    - The base `Stuff` folder has been removed in favor of individual folders.
- Add configuration options to `VideoManagerV2` for defining how the audio is handled during video player swap.
- Add missing and cleanup existing documentation.

### Changed
- Playlist titles are now no longer limited to 140 characters

### Removed
- Remove the ProTV v1 TVManager and VideoManager (the legacy ones that should no longer be in use anyways)

### Fixed
- Fixed playlist performance issues.
- Fixed the MediaControls dropdown nested canvas issue (the one where the cursor hid parts of the menu)
- Fix improper queue behavior when the TV is in a locked state.

#### Known Issues
- If the owner has a video paused and a late joiner joins, the video won't be paused for them, it'll still play.
- (AVPro issue) Unable to seek to any point in the video until the download buffer (internal to AVPro) has reached that point.
- When testing locally, it is recommended NOT to disable the `Allow Master Control`. Due to an issue with how instance owner works locally, you will get locked out of the TV if you have `Locked By Default` enabled. This issue is NOT present once uploaded to VRChat servers, and can be safely disabled prior to uploading if the feature is needed.
- (*WHEN UPGRADING FROM BETA 6.8 OR PRIOR*) To complete the upgrade, you need to manually rename the file `SimplePlaylist.cs` to  `Playlist.cs`, which was located at `Assets/ArchiTechAnon/TV/Scripts/Plugins`, because unity hates file name changes apparently.
- (*WHEN UPGRADING FROM BETA 7.1 OR EARLIER*) If you have any playlists in your scene you will need to click the "Update Scene" button on each of them to regenerate the scene structure for the new click detection required for uncapped playlist entry count.


## 2.0 Beta 7.1 (2021-07-18)
### Added
- Added a configurable auto resync interval that will trigger a resync for both Audio/Video and time sync between users.
    - This helps ensure tight and accurate playback between all users, even in certain low performance situations.
- Create folder `Assets/ArchiTechAnon/ProTV/Plugins` as the location for all plugin specific things to be moved to prior to official release.

### Changed
- Updated 360 video from a sphere mesh to a new custom skybox swap mechanism.
    - This is available as a prefab in `Assets/ArchiTechAnon/ProTV/Plugins/SkyboxSwapper`

### Removed
- Removed the `Playing Threshold` configuration option as it's no longer used.

### Fixed
- Fix improper implementation of _ChangeSeek* methods.
    - `_ChangeSeekTime` and `_ChangeSeekTimeTo(float)` now operate with an explicit time in seconds.  
      It uses the variable `IN_ChangeSeekTo_float_Seconds`.
    - Added `_ChangeSeekPercent` and `_ChangeSeekPercentTo(float)` to operate with a normalized percent value between 0.0 and 1.0.  
      It uses the variable `IN_ChangeSeekPercent_float_Percent`.
      It automatically takes into consideration any custom start and end time given via query parameters.


## 2.0 Beta 7.0 (2021-07-12)
### Added
- Add new Queue plugin.
- Image support and Auto-Grid support added to the playlist plugin
- Example of a 360 video usage added to the demo scene.
- Added mitigations for certain audio/video desync issues.

### Changed
- Aspect-Ratio now renders correctly (Thanks Merlin ; Texelsaur!)
- Mitigated race condition for owner vs other when loading media.
- All TV events have been renamed from using the `_On` prefix to using the `_Tv` prefix to avoid naming confusion with normal udon events.
    - Example: `_OnPlay` would be `_TvPlay` and `_OnMediaStart` is now `_TvMediaStart`
    - NOTE: The outgoing variable names have also been updated respectively. Example: `OUT_OnOwnerChange_int_Id` is now `OUT_TvOwnerChange_int_Id`
- Simplified extension script and plugin names
    - `SimplePlaylist` is now just `Playlist`
    - Previously mentioned `LiveQueue` (new in this release) is going to be called simply `Queue`
- Renamed `allowMasterLockToggle` flag on `TVManagerV2` to `allowMasterControl` for clarity on how the flag is actually used.

### Fixed
- Fix various stability issues with live streams
- Fix some edge-case issues with autoplay.
- Fix the implementation of the MediaChange event to occur at the correct times.

#### Known issues
- If TV owner has the player paused, late joiners will still play the video on join until a sync check occurs (play/pause/stop/seek/etc).


## 2.0 Beta 6.8 (2021-06-19)
### Added
- Add livestream auto-reload for attempting to reload the stream after it goes down
- Add missing _OnMediaChange event trigger for non-owners
- Add instance owner (different than master) as always having access to control the TV (lock, change video, etc)

### Changed
- Minor logic optimizations
- Improve loading of autoplay video for non-owners

### Fixed
- Fix url sync from not being applied correctly for the local player
- Fix url not reloading when the owner puts in the same url as what the local user already has cached
- Fix livestream detection when the stream returns a length of 0 instead of Infinity


## 2.0 Beta 6 (2021-05-18)
### Added
- Add network lag compensation logic to improve sync time accuracy
- Add Resync UI action for triggering the sync enforcement for one frame (in case the video sync drifts)
- Add Reload UI action for explicitly doing a media reload with a single click (just does _Stop then _Play behind the scenes)

### Changed
- Update sync data to take advantage of the Udon Network Update (UNU) changes
- Move all occasional data into manually synced variables
- `BasicUI.cs` and `SlimUI.cs` have been merged into a single plugin `Controls_ActiveState.cs`
- Many refinements to the controls UI plugin
- Remove loop buttons; looping is now controlled exclusively by the loop url parameter
- Adjusted some UI layout parameters for better structure
- `BasicUI` plugin has been rebuilt as the `GeneralControls` plugin
- `SlimUI` plugin has been rebuilt as the `SlimControls` plugin
- `SlimUIReduced` plugin has been rebuilt as the `MinifiedControls` plugin
- Updated the example scene to account for the controls plugins changes
- Update playlist inspector to accept pulling playlist info from a custom txt file

### Removed
- Remove the playerId value from the info display text


## 2.0 Beta 5 (2021-04-30)
### Added
- Start using a formal CHANGELOG

### Changed
- Modify how time sync works. It now only enforces sync time from owner for the first few seconds, and then any time a state change of the TV affects the current sync time. Basically, the enforcement is a bit more lax to help support Quest playback better.
- Update the UIs to make use of the modified sync time activity. Sync button is now an actual "Resync" action, that will do a one-time activation of the sync time enforcement which will jump the video to the current time sync from the owner.


## 2.0 Beta 4 (2021-04-14)
### Changed
- Updated BasicUI prefab to be dark mode (permanently)
- Some structural cleanup in the example scene and prefabs.


## 2.0 Beta 3 (2021-04-12)
### Added
- Added new prefab modules
- Added new ready-made TV prefabs
- Added additional documentation with pictures
- Added new custom TV model commissioned from Chim-Cham

### Changed
- Updated the example scene
- Modified the layout of the folder structure for better asset organization
- Updated SimplePlaylist script to support progress bars (in the form of UI Sliders; Sleek Playlist Module makes use of this)

### Fixed
- Fix some edgecase video looping issues and a couple other bugs I can't remember


## 2.0 Beta 2 (2021-04-03)
### Added
- Added newline support to the display titles (can be combined with the richtext support to create video descriptions)

### Changed
- SimplePlaylist UI template adjustments (strictly visual)

### Fixed
- Fixed Video Playlist Items scroll area not sizing correctly


## 2.0 Beta 1 (2021-03-21)
### Added
- Added pub/sub style events system for extensibility
- Added playlist extension module
- Added a lot of spit, shine and elbow grease to make it more robust and self-correcting

### Changed
- Decoupled UI from the core TV functionality
- Modified the legacy UI into an extension module
