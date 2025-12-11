using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.Udon;

public class Ikso3DPointerRespawnButton : UdonSharpBehaviour
{
    public VRCObjectSync pointerSync;

    public override void Interact()
    {
        if (pointerSync != null)
        {
            pointerSync.Respawn();
        }
    }
}
