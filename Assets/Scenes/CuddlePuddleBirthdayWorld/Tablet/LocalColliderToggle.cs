using UdonSharp;
using UnityEngine;
using VRC.Udon;

public class LocalColliderToggle : UdonSharpBehaviour
{
    public Collider[] colliders;
    private bool _enabled = true;

    public override void Interact()
    {
        _enabled = !_enabled;

        if (colliders == null) return;

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
                colliders[i].enabled = _enabled;
        }
    }
}
