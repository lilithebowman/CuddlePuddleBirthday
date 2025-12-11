using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class Ikso3DBoard : UdonSharpBehaviour
{
    [Header("Cell renderers in index order 0..26")]
    public Renderer[] cellRenderers;   // drag I3DP0..26 here

    [Header("Materials")]
    public Material matBlankSphere;    // transparent
    public Material[] playerMaterials; // size 9: Red..Pink (0..8)

    // -1 = empty, 0..8 = player color index
    [UdonSynced] private int[] cellColors = new int[27];

    private void Start()
    {
        // Ensure initial state is empty when world loads
        for (int i = 0; i < cellColors.Length; i++)
        {
            if (cellColors[i] == 0 && cellRenderers[i].sharedMaterial == null)
            {
                // First load case, force empty
                cellColors[i] = -1;
            }

            if (cellColors[i] == -1)
            {
                ApplyCellVisual(i);
            }
        }
    }

    public override void OnDeserialization()
    {
        // When network sync updates, redraw everything
        for (int i = 0; i < cellColors.Length; i++)
        {
            ApplyCellVisual(i);
        }
    }

    // Called by pointers when they click a cell
    public void ClickCell(int cellIndex, int playerColorIndex)
    {
        if (cellIndex < 0 || cellIndex >= cellColors.Length) return;
        if (playerColorIndex < 0 || playerColorIndex >= playerMaterials.Length) return;

        int current = cellColors[cellIndex];

        // If empty -> claim it
        if (current == -1)
        {
            cellColors[cellIndex] = playerColorIndex;
        }
        // If already mine -> clear it
        else if (current == playerColorIndex)
        {
            cellColors[cellIndex] = -1;
        }
        else
        {
            // Someone else's piece: do nothing
            return;
        }

        ApplyCellVisual(cellIndex);
        RequestSerialization();
    }

    public void ResetBoard()
    {
        for (int i = 0; i < cellColors.Length; i++)
        {
            cellColors[i] = -1;
            ApplyCellVisual(i);
        }

        RequestSerialization();
    }

    private void ApplyCellVisual(int index)
    {
        Renderer r = cellRenderers[index];
        if (r == null) return;

        int colorIndex = cellColors[index];

        if (colorIndex == -1)
        {
            r.sharedMaterial = matBlankSphere;
        }
        else
        {
            if (colorIndex >= 0 && colorIndex < playerMaterials.Length)
            {
                r.sharedMaterial = playerMaterials[colorIndex];
            }
        }
    }
}
