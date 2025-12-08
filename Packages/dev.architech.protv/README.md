# ArchiTech.ProTV Asset

## SUPPORT THE PROJECT!
[<img height='36' src='https://cdn.ko-fi.com/cdn/kofi2.png?v=2' alt='Support the project at ko-fi.com' />](https://ko-fi.com/I3I84I3Z8)

### You can get find the distribution link on the [ProTV Documentation](https://protv.dev/start)

### ProTV **STRONGLY** recommends also grabbing the [Video Player Shim](https://gitlab.com/techanon/videoplayershim/-/releases/) tool for video playback in-editor!
#### You can also install the VideoPlayerShim via the unity menu option `Tools -> ProTV -> Enable Media Playback In Unity` after importing!

## BEFORE IMPORTING PROTV YOU MUST:
- Ensure latest VRC Worlds SDK is imported
- Ensure latest 1.x version of UdonSharp is imported (included by default in VRCSDK 3.5.0 and later)
- Ensure you have reloaded the SDK plugins (DO THIS TO AVOID ISSUES WITH URL INPUT FIELDS)
    - Open the VRCSDK unity menu and select `Reload SDK`

## Basic Usage
- Drag a ProTV prefab (located at `Packages/ArchiTech.ProTV/Samples/Prefabs`) into your scene wherever you like, rotate in-scene and customize as needed.

## Features
- Full media synchronization (play/pause/stop/seek/loop)
- Resilient and automatic sync correction for both Audio/Video and cross-user Time sync
- Sub-second sync delta between viewers
- Automatic ownership management
- Local only mode, for TVs that need to operate independently for all users.
- Media resync and reload capability
- 3D/2D audio toggle
- Near frame-perfect media looping (audio looping isn't always frame-perfect, depends on the media's codec)
- Media autoplay URL support
- Media autoplay delay offsets which help mitigate rate-limit issues with multiple TVs
- Custom url parameter support (t/start/end/loop/live/retry) (see [Understanding Urls](https://protv.dev/urls/parameters))
- Video player swap management for multiple video player configurations
- Pub/Sub event system for modular extension
- Instance owner/master/whitelist locking support (master control is configurable, instance owner is always allowed)

## Core Architecture
In addition to the standard proxy controls for video players (play/pause/stop/volume/seek/etc), the two main unique driving factors that the core architecture accomplishes is event driven modularity as well as the multi-configuration management/swap mechanism.

ProTV has been architected to be more modular and extensible. This is done through a pseudo pub/sub system. In essence, a behavior will pass its own reference to the TV (supports all udon compilers) and then will receive custom events (see the [`Events Document`](https://protv.dev/docs/v2-docs/Events)) based on the TV's activity and state. The types of events directly reflect the various supported core features of the TV, such as the standard video and audio controls, as well as the video player swap mechanism for managing multiple configurations.

More details about the core architecture can be found in the [`Architecture Document`](https://protv.dev/guides/core-architecture).  
Details for ready-made plugins for the TV can be found in the [`Plugins Document`](https://protv.dev/plugins).  
