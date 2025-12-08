using ArchiTech.SDK;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace ArchiTech.ProTV
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class TVFullscreenToggle : TVPlugin
    {
        [I18nInspectorName("Fullscreen Renderer")]
        public MeshRenderer fsrend;

        [I18nInspectorName("Toggle Key Input")]
        public KeyCode keyInput = KeyCode.F11;

        [I18nInspectorName("Use Stereo Audio")]
        public bool forceStereoAudio = true;

        private bool isActive = false;
        private bool isMovingH;
        private bool isMovingV;
        private bool hasFSGO;

        private Texture srcTex = null;
        private Material drawMat = null;
        private int videoTexId;
        private int videoDataId;

        public override void Start()
        {
            base.Start();
            hasFSGO = fsrend != null;
            videoTexId = VRCShader.PropertyToID("_VideoTex");
            videoDataId = VRCShader.PropertyToID("_VideoData");
            if (hasFSGO)
            {
                fsrend.enabled = false;
                if (!isInVR) SendCustomEventDelayedFrames(nameof(InternalUpdate), 1);
            }
        }

        public override void InputMoveHorizontal(float value, VRC.Udon.Common.UdonInputEventArgs args) => isMovingH = value != 0;
        public override void InputMoveVertical(float value, VRC.Udon.Common.UdonInputEventArgs args) => isMovingV = value != 0;

        public void InternalUpdate()
        {
            if (!hasLocalPlayer || !hasFSGO) return;
            SendCustomEventDelayedFrames(nameof(InternalUpdate), 1);

            if (Input.GetKeyDown(keyInput))
            {
                isActive = !isActive;
                fsrend.enabled = isActive;
                if (hasTV && forceStereoAudio) tv._ChangeAudioMode(!isActive);
            }

            if (hasTV && isActive)
            {
                if (isMovingH || isMovingV || Input.GetKey(KeyCode.LeftShift))
                {
                    if (fsrend.enabled) fsrend.enabled = false;
                }
                else if (!fsrend.enabled) fsrend.enabled = true;

                srcTex = tv.InternalTexture;
                if (!drawMat) drawMat = fsrend.material;
                drawMat.SetTexture(videoTexId, srcTex);
                drawMat.SetMatrix(videoDataId, tv.shaderVideoData);
            }
        }

        public override void PostLateUpdate()
        {
            if (hasFSGO && isActive) fsrend.transform.position = localPlayer.GetBonePosition(HumanBodyBones.Head);
        }
    }
}