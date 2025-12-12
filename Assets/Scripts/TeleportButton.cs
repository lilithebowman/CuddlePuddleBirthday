/**
 * TeleportButton.cs
 * 
 * A simple UdonSharp script for a teleportation button in VRChat.
 * When the button is pressed, it teleports the player to a predefined location.
 * Created by Lilithe Bowman (@lilithebowman on Github) 2025/12/11
 */

using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class TeleportButton : UdonSharpBehaviour
{
    public Transform teleportLocation;

    void Start()
    {
        if (teleportLocation == null)
        {
            Debug.LogError("TeleportButton: Teleport location is not set.");
        }
    }

    public override void Interact()
    {
        VRCPlayerApi localPlayer = Networking.LocalPlayer;
        if (localPlayer != null && teleportLocation != null)
        {
            localPlayer.TeleportTo(teleportLocation.position, teleportLocation.rotation);
        }
    }
}
