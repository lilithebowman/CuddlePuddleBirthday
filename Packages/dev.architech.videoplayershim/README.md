# VRChat SDK Video Player Shim

### Demonstration video
![AVPro Playmode Walkthrough](/uploads/4e2117eec7b640309407462ed1832960/AVPro_Playmode_Walkthrough.mp4)

This package contains a set of scripts which enable support for both UnityVideo and AVPro _in play mode_, including YTDL integration.

- Install ArchiTech.VideoPlayerShim through VCC from the [ArchiTechVR Listing](https://vpm.techanon.dev)
- Upon importing the package, it should prompt you to automatically import the requisite AVPro version. Click agree/continue to import it.
  - If the auto-import fails, you can download the requisite AVPro Trial package here: [UnityPlugin-AVProVideo-v2.8.5-Trial.unitypackage](https://github.com/RenderHeads/UnityPlugin-AVProVideo/releases/download/2.8.5/UnityPlugin-AVProVideo-v2.8.5-Trial.unitypackage)
- **REMEMBER THE TRIAL PACKAGE IS REQUIRED FOR AVPRO TO WORK IN EDITOR PLAYMODE!**

### Manual AVPro Import Instructions:
- If you don't care for AVPro support, you can simply import the VideoPlayerShim package as is.
- If you _DO_ want AVPro support, you will need to download the same AVPro package that VRChat is currently using.  
- Last version checked was 2.8.5, but it may be another version in the future.
- To check the version of AVPro that VRChat is using, you will need to go into a world _**with an enabled AVPro video player**_ so the log containing the version will be written.  
- Open the debug log ([relevant VRChat docs](https://creators.vrchat.com/worlds/udon/debugging-udon-projects/#steam-launch-options)) 
- Look for the line that starts with `[AVProVideo] Initializing AVPro Video vX.X.X` where the X.X.X is the version that VRChat is using.
- Download the trial unitypackage file for that version from the [RenderHeads Github](https://github.com/RenderHeads/UnityPlugin-AVProVideo/releases) ([2.8.5 for example](https://github.com/RenderHeads/UnityPlugin-AVProVideo/releases/tag/2.8.5))
- Import that unitypackage into your project, then import the VideoPlayerShim unitypackage after it.
- Setup your VRCAVProVideoPlayers/Speakers/Screens as desired (importing a community video player prefab will also work)
- Press play in unity and try playing a youtube (UnityVideo and AVPro) or twitch link (AVPro only)

That should just work. Please open an issue if you find something isn't matching up like you expect.

Special Notes:
- If testing with VRCDN, it is recommended to use the MPEG-TS link for reliability within the editor. As always, validate in-game as appropriate.

Copyright notice:  
A portion of the code in this package is modified logic from the AVPro Trial package in order to make it work with the VRCSDK/ClientSim.  
All rights of the original trial version code are reserved by RenderHeads.
