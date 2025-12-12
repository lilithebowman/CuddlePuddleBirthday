using UdonSharp;
using UnityEngine;
using VRC.Udon;

public class Ikso3DCell : UdonSharpBehaviour
{
    public Ikso3DBoard board;
    public int cellIndex;

    public void OnPointerClick(int playerColorIndex)
    {
        if (board == null)
        {
            return;
        }

        board.ClickCell(cellIndex, playerColorIndex);
    }
}
