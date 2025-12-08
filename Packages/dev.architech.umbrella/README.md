# ArchiTechAnon Umbrella Library

## SUPPORT ME!
<a href='https://ko-fi.com/I3I84I3Z8' target='_blank'><img height='36' style='border:0px;height:36px;' src='https://cdn.ko-fi.com/cdn/kofi2.png?v=2' border='0' alt='Support me at ko-fi.com' /></a>

### You can access the distributions for this package at [vpm.techanon.dev](https://vpm.techanon.dev)

This library contains a collection of generic and/or miscellaneous use scripts and prefabs that make general world development and interactions easier to implement.


## Available Systems and Tools

### ZoneTrigger
- This tool allows you to define some area which will be considered the "active" area. 
- When the local player enters this area, the script will run all the activation actions (aka On Enter) in the order listed in the inspector.
- You can choose to make the activation area from either a single Area or Range, or an arbitrary combination of native Box/Sphere/Capsule colliders (make sure IsTrigger is checked).
  - The former two options (Area/Range) use vector math to calculate if the player is within the activation area. 
  - This has the benefit of not being affected by stations, but also enabling precision combinations (such as only allowing either hand to trigger the area).
  - The latter option is affected by the usual station issues, but you are able to create an arbitrary shaped space as all the colliders on the same game object automatically work in tandem.
- The actions that are triggered are of two types: Triggers and Transitions
- Transitions involve some form of interpolation over a given period of time. Currently the only supported transition is the alpha fade of some given CanvasGroup component.
- Triggers are currently split up into 4 types:
  - Object Toggles: A very common sight in world development. This list determines which objects to act upon and in what way when the ZoneTrigger is either activated or deactivated.
  - Udon Triggers: This list allows you to specify some given custom udon event to call when activating or deactivating the ZoneTrigger.
  - Animator Triggers: Similar to Udon Triggers, you can select which target to set when activating or deactivating. Note that it only works with Animation Trigger parameters, not flags, ints or floats.  
  - Object Teleports: With this, you can use the ZoneTrigger to trigger moving things around the world based on the given transforms for the activation/deactivation values.
    - Note: All aspects of the target transform are applied to the moved object, INCLUDING rotation _and_ local scale.
