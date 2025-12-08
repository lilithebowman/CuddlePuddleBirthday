using System;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using Toggle = UnityEngine.UI.Toggle;
using Text = UnityEngine.UI.Text;
using UnityEngine.UI;

namespace ArchiTech.ProTV
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(-1)]
    public class VideoSettings : TVPlugin
    {
        public Material[] materials = new Material[0];

        [Header("Default Material Values")] //
        public int _mirrorFlipMode = 1;

        public float _aspectRatio = 1.7778f;
        public float _brightness = 1f;

        [Header("Default TV Values")] //
        [FormerlySerializedAs("_mode3d")]
        // 
        public TV3DMode _video3d = TV3DMode.NONE;

        public bool _fullSize3d = false;
        public bool _force2D = false;
        public bool _skipGamma = false;
        public bool _enableVideo = true;

        [Header("UI References")] //
        public Dropdown mirrorFlipMode;

        public Slider aspectRatio;
        public Slider brightness;
        [FormerlySerializedAs("mode3d")] public Dropdown video3d;
        public Toggle fullSize3d;
        public Toggle force2D;
        public Toggle skipGamma;
        public Toggle enableVideo;
        public Text debugInfo;

        [Header("Property Names")] //
        public string shaderPropertyMirrorFlipMode = "_Mirror";

        public string shaderPropertyAspectRatio = "_Aspect";
        public string shaderPropertyBrightness = "_Brightness";

        private bool hasDebugInfo;
        private bool hasMirrorFlipMode;
        private bool hasAspectRatio;
        private bool hasBrightness;
        private bool has3D;
        private bool hasFullSize3d;
        private bool hasForce2D;
        private bool hasSkipGamma;
        private bool hasEnableVideo;


        public override void Start()
        {
            if (init) return;
            base.Start();
            hasDebugInfo = debugInfo != null;
            hasMirrorFlipMode = mirrorFlipMode != null;
            hasAspectRatio = aspectRatio != null;
            hasBrightness = brightness != null;
            has3D = video3d != null;
            hasFullSize3d = fullSize3d != null;
            hasForce2D = force2D != null;
            hasSkipGamma = skipGamma != null;
            hasEnableVideo = enableVideo != null;
            _UpdateValues();
        }

        public override void _TvMediaReady()
        {
            if (hasAspectRatio) aspectRatio.SetValueWithoutNotify(tv.aspectRatio);
            if (has3D) video3d.SetValueWithoutNotify((int)tv.video3d);
            if (hasFullSize3d) fullSize3d.SetIsOnWithoutNotify(tv.video3dFull);
            if (hasForce2D) force2D.SetIsOnWithoutNotify(tv.force2D);
            if (hasSkipGamma) skipGamma.SetIsOnWithoutNotify(tv.skipGamma);
            if (hasEnableVideo) enableVideo.SetIsOnWithoutNotify(!tv.disableVideo);
            updateCache();
            updateMaterials();
            updateDebug();
        }

        public void _UpdateValues()
        {
            updateCache();
            updateTVSettings();
            updateMaterials();
            updateDebug();
        }

        private void updateCache()
        {
            if (hasMirrorFlipMode) _mirrorFlipMode = mirrorFlipMode.value;
            if (hasAspectRatio) _aspectRatio = aspectRatio.value;
            if (hasBrightness) _brightness = brightness.value;
            if (has3D) _video3d = (TV3DMode)video3d.value;
            if (hasFullSize3d) _fullSize3d = fullSize3d.isOn;
            if (hasForce2D) _force2D = force2D.isOn;
            if (hasSkipGamma) _skipGamma = skipGamma.isOn;
            if (hasEnableVideo) _enableVideo = enableVideo.isOn;
        }

        private void updateTVSettings()
        {
            if (hasTV)
            {
                if (hasAspectRatio) tv.aspectRatio = _aspectRatio;
                if (has3D) tv.video3d = _video3d;
                if (hasFullSize3d) tv.video3dFull = _fullSize3d;
                if (hasForce2D) tv.force2D = _force2D;
                if (hasSkipGamma) tv.skipGamma = _skipGamma;
                if (hasEnableVideo) tv.disableVideo = !_enableVideo;
            }
        }

        private void updateMaterials()
        {
            foreach (var material in materials)
            {
                if (hasMirrorFlipMode) material.SetFloat(shaderPropertyMirrorFlipMode, _mirrorFlipMode);
                if (hasBrightness) material.SetFloat(shaderPropertyBrightness, _brightness);
                if (hasAspectRatio) material.SetFloat(shaderPropertyAspectRatio, _aspectRatio);
            }
        }

        private void updateDebug()
        {
            if (!hasDebugInfo) return;
            var txt = "";
            if (hasMirrorFlipMode) txt += $"{shaderPropertyMirrorFlipMode}={mirrorFlipMode.value} | ";
            if (hasAspectRatio) txt += $"{shaderPropertyAspectRatio}={aspectRatio.value} | ";
            if (hasBrightness) txt += $"{shaderPropertyBrightness}={brightness.value} | ";
            if (has3D) txt += $"Video 3D={video3d.value} | ";
            if (hasFullSize3d) txt += $"Swap Eyes={fullSize3d.isOn} | ";
            if (hasForce2D) txt += $"Force 2D={force2D.isOn} | ";
            if (hasSkipGamma) txt += $"Skip Gamma={skipGamma.isOn} | ";
            if (hasEnableVideo) txt += $"Enabled Video={enableVideo.isOn} | ";
            debugInfo.text = txt;
        }

        [Obsolete("Use _UpdateValues instead")]
        public void _UpdateMaterial() => _UpdateValues();
    }
}