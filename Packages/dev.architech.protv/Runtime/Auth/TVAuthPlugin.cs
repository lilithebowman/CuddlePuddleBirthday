using ArchiTech.SDK;
using UnityEngine;
using VRC.SDKBase;

namespace ArchiTech.ProTV
{
    public abstract class TVAuthPlugin : ATEventHandler
    {
        [SerializeField] protected internal TVManager tv;
        protected bool hasTV;

        /// <summary>
        /// Simple getter which returns a null-safe check on whether the localPlayer is the current TV owner
        /// </summary>
        protected bool IsTVOwner => hasTV && Networking.IsOwner(localPlayer, tv.gameObject);

        public override void Start()
        {
            if (init) return;
            if (tv == null) tv = transform.GetComponentInParent<TVManager>();
            hasTV = tv != null;
            base.Start();
            if (hasTV)
            {
                if (Logger == null) Logger = tv.Logger;
                if (tv.LogLevelOverride) LoggingLevel = tv.LoggingLevel;
                SetLogPrefixLabel($"{tv.gameObject.name}/{name}");
            }
            else
            {
                SetLogPrefixLabel($"<Missing TV Ref>/{name}");
                Warn("The TV reference was not provided. Please make sure the plugin knows what TV to connect to.");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual void _TvReady() { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="who"></param>
        /// <returns></returns>
        public abstract bool _IsAuthorizedUser(VRCPlayerApi who);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="who"></param>
        /// <returns></returns>
        public abstract bool _IsSuperUser(VRCPlayerApi who);
    }
}