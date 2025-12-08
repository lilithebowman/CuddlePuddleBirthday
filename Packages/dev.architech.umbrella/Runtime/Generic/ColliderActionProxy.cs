using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using System.Runtime.CompilerServices;
using ArchiTech.SDK;

[assembly: InternalsVisibleTo("ArchiTech.Umbrella.Editor")]

namespace ArchiTech.Umbrella
{
    /// <summary>
    /// 
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [RequireComponent(typeof(Collider))]
    public class ColliderActionProxy : SDK.ATBehaviour
    {
        public UdonBehaviour eventTarget;

        [SerializeField] internal bool eventInteract = true;
        [SerializeField] internal bool eventOnCollisionEnter = false;
        [SerializeField] internal bool eventOnCollisionExit = false;
        [SerializeField] internal bool eventOnTriggerEnter = false;
        [SerializeField] internal bool eventOnTriggerExit = false;

        [SerializeField, I18nInspectorName("Override"), I18nTooltip("Optional event name override")] internal string eventInteractName = "";
        [SerializeField, I18nInspectorName("Override"), I18nTooltip("Optional event name override")] internal string eventOnCollisionEnterName = "";
        [SerializeField, I18nInspectorName("Override"), I18nTooltip("Optional event name override")] internal string eventOnCollisionExitName = "";
        [SerializeField, I18nInspectorName("Override"), I18nTooltip("Optional event name override")] internal string eventOnTriggerEnterName = "";

        [SerializeField, I18nInspectorName("Override"), I18nTooltip("Optional event name override")]
        internal string eventOnTriggerExitName = "";

        [SerializeField] internal bool detectRemotePlayers = false;
        [SerializeField] internal bool takeOwnershipOfTarget = false;
        public override void Start()
        {
            if (init) return;
            base.Start();
            DisableInteractive = !eventInteract;
        }

        private void activate(string eventName, string targetParameter = null, VRCPlayerApi player = null)
        {
            if (takeOwnershipOfTarget)
                Networking.SetOwner(localPlayer, eventTarget.gameObject);
            if (targetParameter != null)
                eventTarget.SetProgramVariable(targetParameter, player);
            eventTarget.SendCustomEvent(eventName);
        }

        public override void Interact()
        {
            if (eventInteract)
            {
                var eventName = string.IsNullOrWhiteSpace(eventInteractName) ? "_interact" : eventInteractName;
                activate(eventName);
            }
        }

        public override void OnPlayerCollisionEnter(VRCPlayerApi p)
        {
            if (eventOnCollisionEnter)
                if (detectRemotePlayers || p.isLocal)
                {
                    bool isDefault = string.IsNullOrWhiteSpace(eventOnCollisionEnterName);
                    var eventName = isDefault ? "_onPlayerCollisionEnter" : eventOnCollisionEnterName;
                    var targetParameter = isDefault ? "onPlayerCollisionEnterPlayer" : null;
                    activate(eventName, targetParameter, p);
                }
        }

        public override void OnPlayerCollisionExit(VRCPlayerApi p)
        {
            if (eventOnCollisionExit)
                if (detectRemotePlayers || p.isLocal)
                {
                    bool isDefault = string.IsNullOrWhiteSpace(eventOnCollisionEnterName);
                    var eventName = isDefault ? "_onPlayerCollisionExit" : eventOnCollisionEnterName;
                    var targetParameter = isDefault ? "onPlayerCollisionExitPlayer" : null;
                    activate(eventName, targetParameter, p);
                }
        }

        public override void OnPlayerTriggerEnter(VRCPlayerApi p)
        {
            if (eventOnTriggerEnter)
                if (detectRemotePlayers || p.isLocal)
                {
                    bool isDefault = string.IsNullOrWhiteSpace(eventOnCollisionEnterName);
                    var eventName = isDefault ? "_onPlayerTriggerEnter" : eventOnCollisionEnterName;
                    var targetParameter = isDefault ? "onPlayerTriggerEnterPlayer" : null;
                    activate(eventName, targetParameter, p);
                }
        }

        public override void OnPlayerTriggerExit(VRCPlayerApi p)
        {
            if (eventOnTriggerExit)
                if (detectRemotePlayers || p.isLocal)
                {
                    bool isDefault = string.IsNullOrWhiteSpace(eventOnCollisionEnterName);
                    var eventName = isDefault ? "_onPlayerTriggerExit" : eventOnCollisionEnterName;
                    var targetParameter = isDefault ? "onPlayerTriggerExitPlayer" : null;
                    activate(eventName, targetParameter, p);
                }
        }
    }
}