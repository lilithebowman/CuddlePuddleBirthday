using ArchiTech.SDK;
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Components;
using VRC.Udon.Common.Enums;

namespace ArchiTech.ProTV
{
    [RequireComponent(typeof(Dropdown)), UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class TVDropdownFix : UdonSharpBehaviour
    {
        private Dropdown dropdown;
        public void Start()
        {
            dropdown = GetComponent<Dropdown>();
        }
        
        public override void InputUse(bool value, VRC.Udon.Common.UdonInputEventArgs args)
        {
            // on mouse up try running the fix 2 frames later
            // don't run too early, let unity handle the frame cycle
            // so the normal stuff is prepared before the fix is applied
            if (!value) SendCustomEventDelayedFrames(nameof(FixIt), 2);
        }

        public void UpdateTMPLabel()
        {
            if (dropdown.captionText != null)
            {
                var tmp = gameObject.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null) tmp.text = dropdown.captionText.text;
            }
        }

        public void FixIt()
        {
            if (dropdown == null) return;
            var list = dropdown.transform.Find("Dropdown List");
            if (list == null) return;
            var dropdownParentShape = ATUtility.GetComponentInNearestParent(typeof(VRCUiShape), dropdown.transform);
            var blockerParentCanvas = ATUtility.GetComponentInFurthestParent(typeof(Canvas), dropdown.transform);
            bool canvasDiscrepancy = dropdownParentShape.gameObject.GetComponent<Canvas>() != blockerParentCanvas;
            if (blockerParentCanvas == null) return;
            // the blocker is always created as the last direct child of the highest canvas object containing the dropdown
            var blocker = blockerParentCanvas.transform.Find("Blocker");
            if (blocker != null)
            {
                // to normalize, destroy the nested canvas elements
                var blockerCaster = blocker.GetComponent<GraphicRaycaster>();
                if (blockerCaster != null) Destroy(blockerCaster);
                var blockerCanvas = blocker.GetComponent<Canvas>();
                if (blockerCanvas != null) Destroy(blockerCanvas);
                if (canvasDiscrepancy)
                {
                    var srect = dropdownParentShape.GetComponent<RectTransform>();
                    blocker.SetParent(srect, false);
                    var brect = blocker.GetComponent<RectTransform>();
                    if (brect != null)
                    {
                        brect.pivot = Vector2.zero;
                        brect.anchorMin = Vector2.zero;
                        brect.anchorMax = Vector2.one;
                        brect.anchoredPosition = Vector2.zero;
                        brect.sizeDelta = Vector2.zero;
                    }
                }
            }

            var listCanvas = list.GetComponent<Canvas>();
            if (listCanvas != null)
            {
                // to normalize, destroy the nested canvas elements
                var listCaster = list.GetComponent<GraphicRaycaster>();
                if (listCaster != null) Destroy(listCaster);
                Destroy(listCanvas);
            }

            if (blocker != null)
            {
                // move the dropdown to just after the blocker so it's still interactable
                list.SetParent(blocker.parent, true);
                list.SetSiblingIndex(blocker.GetSiblingIndex() + 1);
            }

            // handle the presence of a parallel TMP component in the list
            var toggles = list.GetComponentsInChildren<Toggle>();
            foreach (var toggle in toggles)
            {
                var tt = toggle.transform;
                var text = tt.GetComponentInChildren<Text>();
                if (text != null)
                {
                    var tmp = tt.GetComponentInChildren<TextMeshProUGUI>();
                    if (tmp != null) tmp.text = text.text;
                }
            }
        }
    }
}