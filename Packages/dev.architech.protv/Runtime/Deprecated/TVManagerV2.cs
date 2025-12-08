using System;
using ArchiTech.ProTV;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace ArchiTech
{
    [AddComponentMenu(""), Obsolete("Deprecated component type. Recommend updating to TVManager (right click component header for option), but be careful as legacy third-party tooling may rely on the type being TVManagerV2. Ensure any tooling you use has been updated to support the new component type.")]
    public class TVManagerV2 : TVManager
    {
        [Obsolete("Use ActiveManager instead")]
        public VideoManagerV2 activeManager => (VideoManagerV2)ActiveManager;

        [Obsolete("Use defaultVideoManager instead")]
        public int initialPlayer
        {
            get => defaultVideoManager;
            set => defaultVideoManager = value;
        }

        [Obsolete("Use defaultVolume instead")]
        public float initialVolume
        {
            get => defaultVolume;
            set => defaultVolume = value;
        }

        [Obsolete("Use autoplayMainUrl instead")]
        public VRCUrl autoplayURL
        {
            get => autoplayMainUrl;
            set
            {
                Warn("'autoplayURL' is deprecated and WILL be removed in a future version. Please update your scripts to use the 'autoplayMainUrl' variable instead!");
                autoplayMainUrl = value;
            }
        }

        [Obsolete("Use autoplayAlternateUrl instead")]
        public VRCUrl autoplayURLAlt
        {
            get => autoplayAlternateUrl;
            set
            {
                Warn("'autoplayURLAlt' is deprecated and WILL be removed in a future version. Please update your scripts to use the 'autoplayAlternateUrl' variable instead!");
                autoplayAlternateUrl = value;
            }
        }

        [Obsolete("Use title instead")]
        public string localLabel
        {
            get => title;
            set
            {
                Warn("'localLabel' is deprecated and WILL be removed in a future version. Please update your scripts to use the 'Title' (upper-case T) variable instead!");
                title = value;
            }
        }

#pragma warning disable CS1717
        [Obsolete("Use state instead")]
        public int currentState
        {
            get => Math.Max(0, (int)state - 1);
            set
            {
                Warn("'currentState' is deprecated and WILL be removed in a future version. Please update your scripts to use the 'state' variable instead!");
                value = value;
            }
        }
#pragma warning restore CS1717

        [FieldChangeCallback(nameof(_intl_inurl)), Obsolete("Use IN_MAINURL instead")]
        public VRCUrl IN_URL;

        private VRCUrl _intl_inurl
        {
            get => IN_MAINURL;
            set
            {
                Warn("'IN_URL' is deprecated and WILL be removed in a future version. Please update your scripts to use the 'IN_MAINURL' variable instead!");
                IN_MAINURL = value;
            }
        }

        [FieldChangeCallback(nameof(_intl_inalt)), Obsolete("Use IN_ALTURL instead")]
        public VRCUrl IN_ALT;

        private VRCUrl _intl_inalt
        {
            get => IN_ALTURL;
            set
            {
                Warn("'IN_ALT' is deprecated and WILL be removed in a future version. Please update your scripts to use the 'IN_ALTURL' variable instead!");
                IN_ALTURL = value;
            }
        }

        [FieldChangeCallback(nameof(_intl_insubscriber)), Obsolete("Use IN_LISTENER instead")]
        public UdonSharpBehaviour IN_SUBSCRIBER;

        private UdonSharpBehaviour _intl_insubscriber
        {
            get => IN_LISTENER;
            set
            {
                Warn("'IN_SUBSCRIBER' is deprecated and WILL be removed in a future version. Please update your scripts to use the 'IN_LISTENER' variable instead!");
                IN_LISTENER = value;
            }
        }

        [Obsolete("Use _IsAuthorized instead")]
        public bool _IsPrivilegedUser()
        {
            Warn("_IsPrivilegedUser is deprecated and WILL be removed in a future version. Please update your scripts to use the _IsAuthorized method instead!");
            return _IsAuthorized();
        }

        [Obsolete("Use _IsAuthorized(VRCPlayerApi) instead")]
        public bool _CheckPrivilegedUser(VRCPlayerApi p)
        {
            Warn("_CheckPrivilegedUser is deprecated and WILL be removed in a future version. Please update your scripts to use the _IsAuthorized method instead!");
            return _IsAuthorized(p);
        }

        [Obsolete("Use _ChangeMedia(VRCUrl) instead")]
        public void _ChangeMediaTo(VRCUrl _url)
        {
            _ChangeMedia(_url);
        }
    }
}