using System;
using UdonSharp;
using UnityEngine;

namespace ArchiTech.ProTV
{
    public partial class TVManager
    {
        [Obsolete("Method renamed to _ToggleGammaCorrection")]
        public void _ToggleBlitGamma() => _ToggleColorCorrection();

        #region Deprecated in 3.0.0

        [Obsolete, HideInInspector, SerializeField]
        internal Material customMaterial = null;

        [Obsolete, HideInInspector, SerializeField]
        internal string customMaterialProperty = null;

        [Obsolete("Use allowLocalTweaks instead."), HideInInspector, SerializeField] 
        public bool enforceSyncTweaks = false;

        [Obsolete("Use the 'ActiveManager' property instead.")]
        public VPManager _GetVideoManager() => ActiveManager;

        [Obsolete("Use _ChangeUrlMode(bool) instead")]
        public void _ChangeUrlTo(bool useAlternate) => _ChangeUrlMode(useAlternate);

        [Obsolete("Use _ToggleUrlMode instead")]
        public void _ToggleUrl() => _ToggleUrlMode();

        [Obsolete("Use _ChangeMute(bool) instead")]
        public void _ChangeMuteTo(bool isMute) => _ChangeMute(isMute);

        [Obsolete("Use _ChangeVolume(float) instead")]
        public void _ChangeVolumeTo(float useVolume) => _ChangeVolume(useVolume);

        [Obsolete("Use _ChangeAudioModeTo(bool) instead")]
        public void _ChangeAudioModeTo(bool use3dAudio) => _ChangeAudioMode(use3dAudio);

        [Obsolete("Use _ChangeSync(bool) instead")]
        public void _ChangeSyncTo(bool sync) => _ChangeSync(sync);

        [Obsolete("Use _ChangeLock(bool) instead")]
        public void _ChangeLockTo(bool lockActive) => _ChangeLock(lockActive);

        [Obsolete("Use _ChangeInteractions(bool) instead")]
        public void _ChangeInteractionsTo(bool newState) => _ChangeInteractions(newState);

        [Obsolete("Use _RegisterListener instead")]
        public void _RegisterUdonEventReceiver() => _RegisterListener();

        [Obsolete("Use _RegisterListener instead")]
        public void _RegisterUdonSharpEventReceiver(UdonSharpBehaviour target) => _RegisterListener(target);

        [Obsolete("Use _UnregisterListener instead")]
        public void _UnregisterUdonEventReceiver() => _UnregisterListener();

        [Obsolete("Use _UnregisterListener instead")]
        public void _UnregisterUdonEventReceiver(UdonSharpBehaviour target) => _UnregisterListener(target);

        [Obsolete("Use _EnableListener instead")]
        public void _EnableUdonEventReceiver() => _EnableListener();

        [Obsolete("Use _EnableListener instead")]
        public void _EnableUdonEventReceiver(UdonSharpBehaviour target) => _EnableListener(target);

        [Obsolete("Use _DisableListener instead")]
        public void _DisableUdonSharpEventReceiver() => _DisableListener();

        [Obsolete("Use _DisableListener instead")]
        public void _DisableUdonSharpEventReceiver(UdonSharpBehaviour target) => _DisableListener(target);

        [Obsolete("Use _SetPriorityFirst instead")]
        public void _SetUdonSubscriberPriorityToFirst() => _SetPriorityFirst();

        [Obsolete("Use _SetPriorityFirst instead")]
        public void _SetUdonSharpSubscriberPriorityToFirst(UdonSharpBehaviour target) => _SetPriorityFirst(target);

        [Obsolete("Use _SetPriorityHigh instead")]
        public void _SetUdonSubscriberPriorityToHigh() => _SetPriorityHigh();

        [Obsolete("Use _SetPriorityHigh instead")]
        public void _SetUdonSubscriberPriorityToHigh(UdonSharpBehaviour target) => _SetPriorityHigh(target);

        [Obsolete("Use _SetPriorityLow instead")]
        public void _SetUdonSubscriberPriorityToLow() => _SetPriorityLow();

        [Obsolete("Use _SetPriorityLow instead")]
        public void _SetUdonSubscriberPriorityToLow(UdonSharpBehaviour target) => _SetPriorityLow(target);

        [Obsolete("Use _SetPriorityLast instead")]
        public void _SetUdonSubscriberPriorityToLast() => _SetPriorityLast();

        [Obsolete("Use _SetPriorityLast instead")]
        public void _SetUdonSubscriberPriorityToLast(UdonSharpBehaviour target) => _SetPriorityLast(target);

        [Obsolete("Use _EnableGlobalTexture instead")]
        public void _EnableGSV() => _EnableGlobalTexture();

        [Obsolete("Use _DisableGlobalTexture instead")]
        public void _DisableGSV() => _DisableGlobalTexture();

        [Obsolete("Use _ToggleGlobalTexture instead")]
        public void _ToggleGSV() => _ToggleGlobalTexture();

        [Obsolete("Use InternalTexture instead")]
        public RenderTexture RawTexture => _internalTexture;

        [Obsolete("Use IsLoadingMedia instead")]
        public bool LoadingMedia => IsLoadingMedia;

        [Obsolete("Use IsWaitingForMedia instead")]
        public bool WaitingForMedia => IsWaitingForMedia;

        [Obsolete("Use IsBuffering instead")]
        public bool Buffering => IsBuffering;

        #endregion
    }
}