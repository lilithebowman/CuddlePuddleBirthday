using UdonSharp;
using UnityEngine;
using VRC.Udon;

public class TabletTabs : UdonSharpBehaviour
{
    [Header("Pages in tab order")]
    public GameObject[] pages;   // 0: Welcome, 1: TV Remote, ...

    [Header("Optional: highlight objects per tab")]
    public GameObject[] tabHighlights; // e.g. underline / glow meshes, same length as pages

    private int currentTab = -1;

    private void Start()
    {
        // Default tab: Welcome (0)
        SetTab(0);
    }

    public void SetTab(int index)
    {
        if (pages == null || pages.Length == 0) return;
        if (index < 0 || index >= pages.Length) return;

        for (int i = 0; i < pages.Length; i++)
        {
            bool active = (i == index);
            if (pages[i] != null)
            {
                pages[i].SetActive(active);
                }

            if (tabHighlights != null && i < tabHighlights.Length && tabHighlights[i] != null)
            {
                tabHighlights[i].SetActive(active);
                }
        }

        currentTab = index;
    }
}
