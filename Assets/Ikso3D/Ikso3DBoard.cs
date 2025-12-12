using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class Ikso3DBoard : UdonSharpBehaviour
{
    [Header("Cells")]
    public Renderer[] cellRenderers;      // 27 renderers, index 0..26

    [Header("Materials")]
    public Material matBlankSphere;       // transparent / empty
    public Material[] playerMaterials;    // 9 materials, 0..8

    [UdonSynced] private int[] cellColors = new int[27]; // -1 = empty, 0..8 = player color

    private const int EMPTY = -1;

    private void Start()
    {
        // Ensure array matches renderer count
        if (cellRenderers == null)
        {
            return;
        }

        int cellCount = cellRenderers.Length;

        if (cellColors == null || cellColors.Length != cellCount)
        {
            cellColors = new int[cellCount];
        }

        // If we are the owner and everything is still default 0,
        // treat this as uninitialized and set to EMPTY, then sync.
        VRCPlayerApi local = Networking.LocalPlayer;
        if (local != null && Networking.IsOwner(gameObject))
        {
            bool allZero = true;
            for (int i = 0; i < cellColors.Length; i++)
            {
                if (cellColors[i] != 0)
                {
                    allZero = false;
                    break;
                }
            }

            if (allZero)
            {
                for (int i = 0; i < cellColors.Length; i++)
                {
                    cellColors[i] = EMPTY;
                }
                RequestSerialization();
            }
        }

        // Apply visuals based on current cellColors
        ApplyAllCellVisuals();
    }

    public override void OnDeserialization()
    {
        // When network data arrives, redraw based on synced cellColors
        ApplyAllCellVisuals();
    }

    public void ClickCell(int cellIndex, int playerColorIndex)
    {
        if (cellRenderers == null || cellColors == null)
        {
            return;
        }

        if (cellIndex < 0 || cellIndex >= cellColors.Length)
        {
            return;
        }

        if (playerColorIndex < 0 || playerMaterials == null || playerColorIndex >= playerMaterials.Length)
        {
            return;
        }

        VRCPlayerApi local = Networking.LocalPlayer;
        if (local == null)
        {
            return;
        }

        // Take ownership of the board so our changes replicate
        if (!Networking.IsOwner(gameObject))
        {
            Networking.SetOwner(local, gameObject);
        }

        int current = cellColors[cellIndex];
        int newValue = current;

        // Rules:
        // EMPTY        -> becomes this pointer's color
        // own color    -> becomes EMPTY
        // other color  -> no-op (can't overwrite)
        if (current == EMPTY)
        {
            newValue = playerColorIndex;
        }
        else if (current == playerColorIndex)
        {
            newValue = EMPTY;
        }
        else
        {
            // Do nothing if trying to overwrite someone else's color
            return;
        }

        if (newValue == current)
        {
            return;
        }

        cellColors[cellIndex] = newValue;
        ApplyCellVisual(cellIndex);

        RequestSerialization();
    }

    public void ResetBoard()
    {
        if (cellRenderers == null)
        {
            return;
        }

        int cellCount = cellRenderers.Length;

        if (cellColors == null || cellColors.Length != cellCount)
        {
            cellColors = new int[cellCount];
        }

        VRCPlayerApi local = Networking.LocalPlayer;
        if (local != null && !Networking.IsOwner(gameObject))
        {
            Networking.SetOwner(local, gameObject);
        }

        for (int i = 0; i < cellColors.Length; i++)
        {
            cellColors[i] = EMPTY;
        }

        ApplyAllCellVisuals();

        RequestSerialization();
    }

    private void ApplyAllCellVisuals()
    {
        if (cellRenderers == null || cellColors == null)
        {
            return;
        }

        int len = Mathf.Min(cellRenderers.Length, cellColors.Length);
        for (int i = 0; i < len; i++)
        {
            ApplyCellVisual(i);
        }
    }

    private void ApplyCellVisual(int index)
    {
        if (cellRenderers == null || index < 0 || index >= cellRenderers.Length)
        {
            return;
        }

        Renderer r = cellRenderers[index];
        if (r == null)
        {
            return;
        }

        int colorIndex = cellColors[index];

        if (colorIndex == EMPTY)
        {
            if (matBlankSphere != null)
            {
                r.sharedMaterial = matBlankSphere;
            }
        }
        else if (playerMaterials != null &&
                 colorIndex >= 0 &&
                 colorIndex < playerMaterials.Length &&
                 playerMaterials[colorIndex] != null)
        {
            r.sharedMaterial = playerMaterials[colorIndex];
        }
    }
}
