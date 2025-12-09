using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class Ikso3DBoard : UdonSharpBehaviour
{
    public Renderer[] cellRenderers;
    public Material matBlankSphere;
    public Material[] playerMaterials;

    [UdonSynced] private int[] cellColors = new int[27];

    private const int EMPTY = -1;

    private void Start()
    {
        // On first run, all entries are 0 – treat that as "uninitialized" and make them blank
        for (int i = 0; i < cellColors.Length; i++)
        {
            if (cellColors[i] == 0)
            {
                cellColors[i] = EMPTY;
            }

            ApplyCellVisual(i);
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

    public void ClickCell(int cellIndex, int playerColorIndex)
    {
        if (cellIndex < 0 || cellIndex >= cellColors.Length) return;
        if (playerColorIndex < 0 || playerColorIndex >= playerMaterials.Length) return;

        // Make sure the person clicking actually becomes owner before changing synced data
        if (!Networking.IsOwner(gameObject))
        {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
        }

        int current = cellColors[cellIndex];

        if (current == EMPTY)
        {
            cellColors[cellIndex] = playerColorIndex;
        }
        else if (current == playerColorIndex)
        {
            cellColors[cellIndex] = EMPTY;
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
        if (!Networking.IsOwner(gameObject))
        {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
        }

        for (int i = 0; i < cellColors.Length; i++)
        {
            cellColors[i] = EMPTY;
            ApplyCellVisual(i);
        }

        RequestSerialization();
    }

    private void ApplyCellVisual(int index)
    {
        Renderer r = cellRenderers[index];
        if (r == null) return;

        int colorIndex = cellColors[index];

        if (colorIndex == EMPTY)
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
