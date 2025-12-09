using UdonSharp;
using UnityEngine;
using VRC.Udon;

public class Ikso3DCell : UdonSharpBehaviour
{
    public Ikso3DBoard board;
    public int cellIndex;

    // Called by a pointer when it hits + clicks this cell
    public void OnPointerClick(int playerColorIndex)
    {
        if (board != null)
        {
            board.ClickCell(cellIndex, playerColorIndex);
        }
        else
        {
            Debug.LogError("Ikso3DBoard not wired!!!");
        }
    }
}
