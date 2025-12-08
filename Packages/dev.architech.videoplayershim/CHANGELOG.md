# VideoPlayerShim Changelog
Manually curated document of all notable changes to this project sorted by version number in descending order.

Structure used for this document:
```
## Version Number (Publish Date)
- Changes
```

## 1.5.0 (2025-12-04)
- Improve reliability of finding the correct YTDL executable via PATH env variable.
- Add error logs for any detected error values returned from YTDL.
  - This helps users identify what the actual problem with the media is.
- Clean up log statements for better consistency.
- Improve the video error result to much more closely match VRChat's behaviour.
- Fix rare null pointer exception.

## 1.4.0 (2025-02-21)
- Switch AVPro import settings from being session-based to being project-based.
  - This means the import prompt will no longer pop up again, for the same project, after selecting "No, Skip" option.
  - You can still manually trigger the import via the Unity menu `Tools/VideoPlayerShim/Import AVPro`

## 1.3.10 (2024-12-16)
- Fix incorrect variable name being used when VPM Resolver is missing from the project causing compilation failure.

## 1.3.9 (2024-12-10)
- Move import handler into its own parent folder.
  - Should fix AVPro import menu option not showing when main package fails compiling due to old AVPro version.

## 1.3.8 (2024-09-25)
- Fixed version define handling to prevent compiler error when VPM Resolver is not present in the project.

## 1.3.7 (2024-09-14)
- Removed handling of the internal AVPro closing event to align with behaviour exhibited in VRChat.
  - Should resolve some discontinuities as well.

## 1.3.6 (2024-06-14)
- Add menu options for forcing the video player to throw an error. `Tools/VideoPlayerShim/Force Video Error/*`
  - These options completely skip the video loading and just return the error.

## 1.3.5 (2024-05-20)
- Improve error message handling for when YTDL is not findable.
- Add version specific settings between AVPro 2 and AVPro 3 in preparation for AVPro 3.
- Improve version handling for AVPro scripting defines.
- Add Menu checkbox for avpro importing when it's already detected.
- Add support for vrcsdk 3.6.1 changes to the IAVProVideoPlayerInternal interface.

## 1.3.4 (2024-03-24)
- Add better support for additional platforms.
- Fix null reference issue during UnityVideo player initialization.

## 1.3.3 (2024-03-21)
- Add `/opt/homebrew/bin` to path search for YTDL for M-series Mac support.

## 1.3.2 (2024-03-19)
- Fix VRC component autoplay URLs not being handled for UnityVideo.
- Fix VRC component autoplay URLs attempting to load empty URLs.

## 1.3.1 (2024-03-19)
- Fix VRC VideoPlayer component autoplay URLs not being correctly passed to the YTDL resolver.

## 1.3.0 (2024-03-18)
- Add support for running YTDL on Mac/Linux.
  - When on Mac or Linux, you must make sure that the YTDL executable file is in your PATH somewhere.
  - The editor script uses the `which` command to find the executable.
  - The editor script searches for the following file names: "yt-dlp", "yt-dl", "ytdlp", "ytdl", "youtube-dlp", "youtubedlp", "youtube-dl", "youtubedl"
  - Make sure your executable matches one of those names.
- Add menu option `Tools/VideoPlayerShim/Select Custom YTDL Install` for explicitly specifying the location of your YTDL install.
  - Menu option will be checked when a custom install location is enabled.
  - You can remove the custom install location by selecting the menu option and clicking Cancel in the dialog window.
  - Note: This is also usable on windows as well if you don't want to use the default VRChat ytdl location.
- Fix `Tools/VideoPlayerShim/Import AVPro` not showing up correctly on Mac (and possibly Linux)

## 1.2.1 (2024-02-10)
- Fix scripting define not being properly set for android build causing AVPro to not initialize correctly when testing in editor with the Android build target.

## 1.2.0 (2024-01-11)
- Move AVPro scripting define logic into a sub-assembly.
- Add logic for handling downloading and importing the AVPro trial package automatically.
  - When triggered for the first time, if AVPro is not detected, a prompt for auto-importing AVPro trial package will be displayed.
  - To force update AVPro to the target version, select the menu option `Tools/VideoPlayerShim/Import AVPro`.

## 1.1.2 (2023-12-26)
- Fix compilation error.

## 1.1.1 (2023-12-24)
- Restore alternate file path check for AVPro scripting define to cover edge-cases where the assembly reference is unexpectedly not available.

## 1.1.0 (2023-12-15)
- Update to unity's Package format for VPM compatibility.
- Update namespace to ArchiTech.VideoPlayerShim for consistency.
- Update AudioOuputStub to inherit and override AudioOutput for cleaner implementation.
- Rename AudioOutputStub to AudioOutputShim as a more accurate name.
- Remove AudioOutputManagerStub as it's no longer needed.
- Add assembly definitions.
- Add Changelog.

## 1.0.4 (2023-10-26)
- Fix AVPro StereoMix mode not properly using all necessary channels.

## 1.0.3 (2023-01-31)
- Fix certain live media not being able to be properly loaded
- Prevent redundant OnVideoEnd from being called.
- Add debug statement to resolve logic for when the resolution fails.
- Update AVProShim to allow fallback to the original URL if resolution fails (feature parity with VRC).

## 1.0.2 (2023-01-20)
- Remove extraneous import statement breaking compilation.

## 1.0.1 (2023-01-18)
- Fix non-shared materials not working properly

## 1.0.0 (2023-01-18)
- Initial commit
- Add Support for Unity Video and AVPro in-editor playback