using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDKBase;

namespace ArchiTech.ProTV
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(-1)]
    public class QuickPlay : TVPlugin
    {
        public Queue queue;
        [FormerlySerializedAs("pcUrl")] public VRCUrl mainUrl = new VRCUrl("");
        [FormerlySerializedAs("questUrl")] public VRCUrl alternateUrl = new VRCUrl("");
        public string title;
        public bool useInteractInsteadOfPointer = false;
        private bool hasQueue;

        public override sbyte Priority => 72;

        public override void Start()
        {
            if (init) return;
            base.Start();
            if (mainUrl == null) mainUrl = VRCUrl.Empty;
            if (alternateUrl == null) alternateUrl = VRCUrl.Empty;
            hasQueue = queue != null;
            if (!useInteractInsteadOfPointer) DisableInteractive = true;
        }

        public override void Interact()
        {
            _Activate();
        }

        public void _Activate()
        {
            if (hasQueue) queue._AddEntry(mainUrl, alternateUrl, title);
            else tv._ChangeMedia(mainUrl, alternateUrl, title);
        }

        public override bool _IsPreApprovedUrl(VRCUrl main, VRCUrl alt)
        {
            string murl = main.Get();
            if (!string.IsNullOrEmpty(murl) && murl == mainUrl.Get()) return true;
            string aurl = alternateUrl.Get();
            if (!string.IsNullOrEmpty(aurl) && aurl == alternateUrl.Get()) return true;
            return false;
        }

        [PublicAPI]
        public void SetQueue(Queue plugin)
        {
            queue = plugin;
            hasQueue = queue != null;
        }
    }
}