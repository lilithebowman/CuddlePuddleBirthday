using System;
using UnityEngine;

namespace ArchiTech.ProTV
{
    public partial class VPManager
    {
        [Obsolete("Use Show() instead")]
        public void _Show() => Show();

        [Obsolete("Use Stop() instead")]
        public void _Stop() => Stop();

        [Obsolete("Use UpdateState() instead")]
        public void _UpdateState() => UpdateState();

        [Obsolete("Use ChangeMute(bool) instead")]
        public void _ChangeMute(bool muted) => ChangeMute(muted);

        [Obsolete("Use ChangeVolume(float, bool) instead")]
        public void _ChangeVolume(float useVolume, bool suppressLog = false) => ChangeVolume(useVolume, suppressLog);

        [Obsolete("Use ChangeAudioMode(bool) instead")]
        public void _ChangeAudioMode(bool use3dAudio) => ChangeAudioMode(use3dAudio);

        [Obsolete("Use ChangePlaybackSpeed(float) instead")]
        public void _ChangePlaybackSpeed(float speed) => ChangePlaybackSpeed(speed);

        [Obsolete("Use SetTV(TVManager) instead")]
        public void _SetTV(TVManager manager) => SetTV(manager);

        [Obsolete("Use tv._Blit() instead")]
        public void _Blit() => tv._Blit(this);

        [Obsolete("Use tv._Blit() instead")]
        public void Blit() => tv._Blit(this);

        [Obsolete("Use GetVideoTexture(out Vector4) instead")]
        public Texture _GetVideoTexture(out Vector4 textureST) => GetVideoTexture(out textureST);
    }
}