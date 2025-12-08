#if AVPRO_IMPORTED
using RenderHeads.Media.AVProVideo;
using UnityEngine.Events;
#endif
using System.Reflection;
using UnityEngine;

namespace ArchiTech.VideoPlayerShim
{
    [AddComponentMenu("")]
    public class AudioOutputShim :
#if AVPRO_IMPORTED
        AudioOutput
#else
        MonoBehaviour
#endif
    {
#if AVPRO_IMPORTED

        private float _volume;
        private bool _positionalAudio;
        private FieldInfo spaInfo;

        void Start()
        {
            spaInfo = GetType().BaseType?.GetField("_supportPositionalAudio", (BindingFlags)~0);
            Debug.Assert(spaInfo != null);
            AudioSettings.OnAudioConfigurationChanged += OnAudioConfigurationChanged;
            ChangeMediaPlayer(Player);
        }

        void OnAudioConfigurationChanged(bool deviceChanged)
        {
            if (Player == null || Player.Control == null)
                return;
            Player.Control.AudioConfigurationChanged(deviceChanged);
        }

        void OnDestroy()
        {
            ChangeMediaPlayer(null);
        }

        void Update()
        {
            var source = GetAudioSource();
            if (source == null) source = cacheAudioSource();
            _volume = source.volume;
            if (spaInfo != null) _positionalAudio = (bool)spaInfo.GetValue(this);
            if (Player != null && Player.Control != null)
                source.pitch = Player.PlaybackRate;
        }

        private AudioSource cacheAudioSource()
        {
            var audioSource = GetComponent<AudioSource>();
            var asInfo = GetType().BaseType?.GetField("_audioSource", (BindingFlags)~0);
            Debug.Assert(asInfo != null);
            asInfo.SetValue(this, audioSource);
            Debug.Assert(audioSource != null);
            return audioSource;
        }

        // must take control of the ChangeMediaPlayer action so we can hook up the custom event listener to prevent AVPro from undesirably messing with the audio settings.
        public new void ChangeMediaPlayer(MediaPlayer newPlayer)
        {
            if (Player != null) Player.Events.RemoveListener(OnMediaPlayerEventActual);
            base.ChangeMediaPlayer(newPlayer);
            if (newPlayer != null)
            {
                var mpeInfo = GetType().BaseType?.GetMethod("OnMediaPlayerEvent", BindingFlags.Instance | BindingFlags.NonPublic);
                Debug.Assert(mpeInfo != null);
                var oldMethod = (UnityAction<MediaPlayer, MediaPlayerEvent.EventType, ErrorCode>)
                    mpeInfo.CreateDelegate(typeof(UnityAction<MediaPlayer, MediaPlayerEvent.EventType, ErrorCode>), this);
                newPlayer.Events.RemoveListener(oldMethod);
                newPlayer.Events.AddListener(OnMediaPlayerEventActual);
            }
        }

        private void OnMediaPlayerEventActual(MediaPlayer mp, MediaPlayerEvent.EventType et, ErrorCode errorCode)
        {
            switch (et)
            {
                case MediaPlayerEvent.EventType.Closing:
                    GetAudioSource().Stop();
                    break;
                case MediaPlayerEvent.EventType.Started:
                    GetAudioSource().Play();
                    break;
            }
        }

#if (UNITY_EDITOR_WIN || UNITY_EDITOR_OSX) || (!UNITY_EDITOR && (UNITY_STANDALONE_WIN || UNITY_WSA_10_0 || UNITY_STANDALONE_OSX || UNITY_IOS || UNITY_TVOS || UNITY_ANDROID))
        private void OnAudioFilterRead(float[] audioData, int channelCount)
        {
            if (Player == null || Player.Control == null || GetAudioSource() == null) return;
            Player.AudioVolume = _volume;
            AudioOutputManager.Instance.RequestAudio(this, Player, audioData, channelCount, ChannelMask, OutputMode, _positionalAudio);
        }
#endif
#endif
    }
}