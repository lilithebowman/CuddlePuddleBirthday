using System;

namespace ArchiTech.ProTV
{
    public partial class AudioAdapter
    {
        
        /// <summary>
        /// Use <see cref="UpdateWorldAudioVolume()"/> instead
        /// </summary>
        [Obsolete("Use UpdateWorldAudioVolume() instead")]
        public void _UpdateWorldAudioVolume() => UpdateWorldAudioVolume();
        
        /// <summary>
        /// Use <see cref="UpdateMediaTime()"/> instead
        /// </summary>
        [Obsolete("Use UpdateMediaTime() instead")]
        public void _UpdateMediaTime() => UpdateMediaTime();
        
        /// <summary>
        /// Use <see cref="EnableAudioLinkState()"/> instead
        /// </summary>
        [Obsolete("Use EnableAudioLinkState() instead")]
        public void _EnableAudioLinkState() => EnableAudioLinkState();
        
        /// <summary>
        /// Use <see cref="DisableAudioLinkState()"/> instead
        /// </summary>
        [Obsolete("Use DisableAudioLinkState() instead")]
        public void _DisableAudioLinkState() => DisableAudioLinkState();
        
        /// <summary>
        /// Use <see cref="ToggleAudioLinkState()"/> instead
        /// </summary>
        [Obsolete("Use ToggleAudioLinkState() instead")]
        public void _ToggleAudioLinkState() => ToggleAudioLinkState();
        
        /// <summary>
        /// Use <see cref="ChangeAudioLinkState(bool)"/> instead
        /// </summary>
        [Obsolete("Use ChangeAudioLinkState(bool) instead")]
        public void _ChangeAudioLinkState(bool state) => ChangeAudioLinkState(state);
        
        /// <summary>
        /// Use <see cref="ActivateWorldMusic()"/> instead
        /// </summary>
        [Obsolete("Use ActivateWorldMusic() instead")]
        public void _ActivateWorldMusic() => ActivateWorldMusic();
    }
}