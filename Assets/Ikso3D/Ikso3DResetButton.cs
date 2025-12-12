using UdonSharp;
using UnityEngine;
using VRC.Udon;

public class Ikso3DResetButton : UdonSharpBehaviour
{
    public Ikso3DBoard board;

    public override void Interact()
    {
        if (board != null)
        {
            board.ResetBoard();
        }
    }
}
