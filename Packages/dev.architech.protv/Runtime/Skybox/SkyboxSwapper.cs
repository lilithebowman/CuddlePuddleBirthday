using ArchiTech.SDK;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace ArchiTech.ProTV
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None), DefaultExecutionOrder(-1)]
    [HelpURL("https://protv.dev/guides/skybox")]
    public class SkyboxSwapper : TVPlugin
    {
        public Material skybox;
        public Material fallback;
        public Slider brightness;

        [I18nInspectorName("Require URL Parameter"), I18nTooltip("Only trigger the skybox swap when the url parameter 'skybox' is present.")]
        public bool requireURLParameter;

        [Header("UI Colors")] //
        public ColorBlock uiColors = new ColorBlock()
        {
            normalColor = new Color(0.25f, 0.25f, 0.25f),
            selectedColor = new Color(0.4f, 0.4f, 0.4f),
            highlightedColor = new Color(0.4f, 0.4f, 0.4f),
            pressedColor = new Color(0.5f, 0.5f, 0.5f),
            disabledColor = new Color(0.75f, 0.75f, 0.75f, 0.5f),
            colorMultiplier = 1,
            fadeDuration = 0.1f
        };

        private bool hasBrightness;
        private bool isSwapped;

        public override void Start()
        {
            if (init) return;
            base.Start();
            if (!hasTV) return;

            if (fallback == null) fallback = RenderSettings.skybox;

            hasBrightness = brightness != null;

            _Panoramic();
            _Not3D();
            if (hasBrightness) brightness.value = 1f;

            tv._RegisterListener(this);
        }


        private void OnDisable()
        {
            revert();
        }

        // ========== Shader Toggles =========

        public void _Brightness()
        {
            if (hasBrightness) skybox.SetFloat("_Exposure", brightness.value);
        }

        public void _Not3D()
        {
            skybox.SetFloat("_Layout", 0);
        }

        public void _SideBySide()
        {
            skybox.SetFloat("_Layout", 1);
        }

        public void _OverUnder()
        {
            skybox.SetFloat("_Layout", 2);
        }

        public void _Flip()
        {
            float flipTo = (skybox.GetFloat("_Flip") + 1) % 2;
            skybox.SetFloat("_Flip", flipTo);
        }

        public void _SwapEyes()
        {
            float swapTo = (skybox.GetFloat("_SwapEyes") + 1) % 2;
            skybox.SetFloat("_SwapEyes", swapTo);
        }

        public void _Deg180()
        {
            skybox.SetFloat("_ImageType", 1);
            skybox.SetFloat("_Mapping", 1); // 180 implies panoramic
        }

        public void _Panoramic()
        {
            skybox.SetFloat("_Mapping", 1);
            skybox.SetFloat("_ImageType", 0); // Panoramic implies 360
        }

        public void _CubeMap()
        {
            skybox.SetFloat("_Mapping", 0);
            skybox.SetFloat("_ImageType", 0); // CubeMap implies 360
        }

        // =========== TV Events ==============

        public override void _TvPlay() => activate();
        public override void _TvMediaReady() => activate();
        public override void _TvMediaEnd() => revert();
        public override void _TvStop() => revert();
        public override void _TvVideoPlayerError() => revert();

        private void activate()
        {
            if (isSwapped) return;
            if (requireURLParameter && !tv._HasUrlParam("skybox")) return;
            isSwapped = true;
            fallback = RenderSettings.skybox;
            RenderSettings.skybox = skybox;
            foreach (string option in tv.urlParamKeys)
            {
                switch (option.ToLower())
                {
                    case "180":
                        _Deg180();
                        break;
                    case "panoramic":
                        _Panoramic();
                        break;
                    case "cubemap":
                        _CubeMap();
                        break;
                    case "sidebyside":
                        _SideBySide();
                        break;
                    case "overunder":
                        _OverUnder();
                        break;
                    case "nolayout": // same as not3d
                    case "standard": // same as not3d
                    case "not3d":
                        _Not3D();
                        break;
                }
            }
        }

        private void revert()
        {
            if (!isSwapped) return;
            isSwapped = false;
            RenderSettings.skybox = fallback;
        }
    }
}