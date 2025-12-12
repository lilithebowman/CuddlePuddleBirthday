using UdonSharp;
using UnityEngine;
using VRC.Udon;

public class TabletTabButton : UdonSharpBehaviour
{
    public TabletTabs tabManager;
    public int tabIndex;

    public override void Interact()
    {
        if (tabManager != null)
        {
            tabManager.SetTab(tabIndex);
        }
    }
}
