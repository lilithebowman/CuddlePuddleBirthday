using System;
using ArchiTech.SDK;
using JetBrains.Annotations;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;

namespace ArchiTech.ProTV
{
    public partial class TVManager
    {
        [SerializeField] internal TVAuthPlugin authPlugin = null;
        private bool hasAuthPlugin;
        [SerializeField] internal string[] domainWhitelist = null;

        [NonSerialized] public readonly string[] defaultDomains =
        {
            // VRChat whitelist domains
            // https://docs.vrchat.com/docs/www-whitelist
            "soundcloud.com", "facebook.com", "nicovideo.jp", "mixcloud.com",
            "twitch.tv", "vimeo.com", "youku.com", "youtube.com", "youtu.be",
            "hyperbeam.com", "hyperbeam.dev", "vrcdn.live", "vrcdn.video", "topaz.chat",
            // non VRChat whitelist domains
            "streamable.com", "bandcamp.com", "bilibili.tv"
        };

        private bool localAuthCache;
        private int localAuthCacheUser = -1;
        private bool authCache;
        private int authCacheUser = -1;
        private bool superAuthCache;
        private int superAuthCacheUser = -1;

        /// <summary>
        /// Getter that checks the following conditions:<br/>
        /// - Owner is not disabled<br/>
        /// - Owner is not in a failed error state<br/>
        /// - Owner meets the authorization requirements of the TV.
        /// </summary>
        public bool IsOwnerValid
        {
            get
            {
                // check standard auth but also that the sync data owner matches the actual owner, if not, current owner probably hasn't initialized the script yet.
                var authed = _IsAuthorized(Owner);
                if (IsTraceEnabled) Trace($"IsOwnerValid({Owner.displayName}): {!ownerDisabled} && {errorStateOwner != TVErrorState.FAILED} && ({!locked} || {authed}) && {currentOwner == Owner.displayName}");
                return !ownerDisabled && errorStateOwner != TVErrorState.FAILED && (!locked || authed) && currentOwner == Owner.displayName;
            }
        }

        /// <summary>
        /// Getter that checks the following conditions:<br/>
        /// - TV has completed reached it's ready state<br/>
        /// - TV is syncing to the current owner<br/>
        /// - auto ownership is allowed<br/>
        /// - current owner IS NOT valid <see cref="IsOwnerValid"/>
        /// </summary>
        public bool AutoOwnershipAvailable
        {
            get
            {
                var valid = IsOwnerValid;
                var authed = _IsAuthorized();
                if (IsTraceEnabled) Trace($"AutoOwnershipAvailable({localPlayer.displayName}): {syncToOwner} && {enableAutoOwnership} && {!isAndroid} && {!valid} && ({authed} || {ownerDisabled})");
                return isReady && syncToOwner && enableAutoOwnership && !isAndroid && !valid && (authed || ownerDisabled);
            }
        }

        /// <summary>
        /// Getter that checks the following conditions:<br/>
        /// - TV has reached ready state<br/>
        /// - TV allows unauthorized users to interact with it and it is not in a locked state<br/>
        /// - If not, then check that the local player meets the authorization requirements of the TV.
        /// </summary>
        public bool CanPlayMedia
        {
            get
            {
                var authed = _IsAuthorized();
                if (IsTraceEnabled) Trace($"CanPlayMedia({localPlayer.displayName}): {isReady} && (({!disallowUnauthorizedUsers} && {!locked}) || {authed})");
                return isReady && ((!disallowUnauthorizedUsers && !locked) || authed);
            }
        }

        private void SetupSecurity()
        {
            // if enabled, make sure the list is not empty. Use default list if so.
            if (domainWhitelist == null || domainWhitelist.Length == 0) domainWhitelist = defaultDomains;
            else
                for (int i = 0; i < domainWhitelist.Length; i++)
                    domainWhitelist[i] = domainWhitelist[i].ToLower();
            setInternalLogging();
            // without an auth plugin, there is nothing defining who's an authorized user
            // force unset the disallow flag to prevent absolute lockout of all users
            if (authPlugin != null)
            {
                hasAuthPlugin = true;
                authPlugin.Logger = Logger;
                if (LogLevelOverride) authPlugin.LoggingLevel = LoggingLevel;
            }
            else disallowUnauthorizedUsers = false;

            if (hasLocalPlayer && localPlayer.isMaster)
            {
                locked = lockedByDefault;
                currentMaster = localPlayer.displayName;
                firstMaster = currentMaster;
                if (localPlayer.isInstanceOwner) instanceOwner = currentMaster;
            }
        }

        private bool takeOwnership()
        {
            if (!init) return false;
            if (IsOwner) return true; // local already owns the TV
            localPlayer = Networking.LocalPlayer;
            hasLocalPlayer = true;
            Owner = localPlayer;
            syncData.Owner = localPlayer;
            return true;
        }

        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            if (!IsOwner) return;
            if (player.isInstanceOwner) instanceOwner = player.displayName;
        }

        public override void OnPlayerLeft(VRCPlayerApi player)
        {
            if (!VRC.SDKBase.Utilities.IsValid(player) || player.isLocal)
            {
                hasLocalPlayer = false;
                return;
            }

            // leaving player wasn't the master, skip
            if (player.displayName != currentMaster) return;

            // find the new master
            VRCPlayerApi[] players = new VRCPlayerApi[VRCPlayerApi.GetPlayerCount()];
            VRCPlayerApi.GetPlayers(players);
            foreach (var p in players)
            {
                if (p.isMaster)
                {
                    currentMaster = p.displayName;
                    break;
                }
            }
        }

        public override void OnOwnershipTransferred(VRCPlayerApi newOwner)
        {
            bool newOwnerLocal = newOwner.isLocal;
            if (newOwnerLocal) syncData.Owner = newOwner;
            RequestSync();
            lockedBySuper = locked && _IsSuperAuthorized(newOwner);
            Log(ATLogLevel.ALWAYS, $"Owner changed to {newOwner.displayName}");
            // Was an automatic ownership transfer handled by VRChat
            // eg: player leaving or auto master assignment.
            if (newOwnerLocal)
                SendCustomNetworkEvent(NetworkEventTarget.Owner, ownerDisabled ? nameof(ALL_OwnerDisabled) : nameof(ALL_OwnerEnabled));
            SendManagedVariable(nameof(TVPlugin.OUT_OWNER), newOwner.playerId);
            SendManagedEvent(nameof(TVPlugin._TvOwnerChange));
        }

        public override bool OnOwnershipRequest(VRCPlayerApi requestingPlayer, VRCPlayerApi requestedOwner)
        {
            // allow transfer if unlocked or if the requesting player has enough privilege
            bool authorized = (!locked && !disallowUnauthorizedUsers) || _IsAuthorized(requestingPlayer);
            if (IsDebugEnabled)
            {
                string status = authorized ? "<color=green>passed</color>" : "<color=red>rejected</color>";
                Debug($"Ownership transfer request {status} from {Owner.displayName} [{Owner.playerId}] to {requestedOwner.displayName} [{requestedOwner.playerId}] by {requestingPlayer.displayName} [{requestingPlayer.playerId}]");
            }

            return authorized;
        }

        /// <summary>
        /// Simple lower-case fuzzy check to see if a domain is allowed by the whitelist.
        /// </summary>
        /// <param name="domains">Domains to validate</param>
        /// <returns>Whether the requested domain was within the whitelist</returns>
        public bool _CheckDomainWhitelist(params string[] domains)
        {
            // auto-pass if whitelist is not enforced or the user is sufficiently authorized
            if (!enforceDomainWhitelist || _IsSuperAuthorized() || (enableAuthUserDomainBypass && _IsAuthorized())) return true;
            int pass = 0;
            foreach (var given in domains)
            {
                if (string.IsNullOrEmpty(given))
                {
                    pass++;
                    continue;
                }

                var domain = _GetUrlDomain(given).ToLower();
                foreach (string expected in domainWhitelist)
                {
                    if (domain.Contains(expected))
                    {
                        pass++;
                        break;
                    }
                }
            }

            // number of passed domains is the number of domains passed, check is passed.
            if (IsTraceEnabled) Trace($"Domain Check: {pass} passed out of {domains.Length}");
            return pass == domains.Length;
        }

        internal bool CheckPreApprovedUrls(VRCUrl mainUrl, VRCUrl altUrl)
        {
            string murl = mainUrl.Get();
            string aurl = altUrl.Get();
            bool check = !string.IsNullOrEmpty(murl) && murl == autoplayMainUrl.Get();
            if (!check) check = !string.IsNullOrEmpty(aurl) && aurl == autoplayAlternateUrl.Get();
            if (check) Debug("Url pre-approved by autoplay.");
            foreach (var listener in _eventListeners)
            {
                var plugin = (TVPlugin)listener;
                if (listener != null && plugin._IsPreApprovedUrl(mainUrl, altUrl))
                {
                    Debug($"Url pre-approved by plugin: {plugin.GetUdonTypeName()}");
                    check = true;
                    break;
                }
            }

            return check;
        }

        public void _Reauthorize()
        {
            setInternalLogging();
            SendManagedEvent(nameof(TVPlugin._TvAuthChange));
        }


        /// <returns>Whether the local user has privilege or not</returns>
        /// <seealso cref="_IsAuthorized(VRCPlayerApi, bool)"/>
        [PublicAPI]
        public bool _IsAuthorized()
        {
            // ensure local player has been cached.
            localPlayer = Networking.LocalPlayer;
            hasLocalPlayer = VRC.SDKBase.Utilities.IsValid(localPlayer);

            if (hasLocalPlayer) return _IsAuthorized(localPlayer);
            Warn("No local player available.");
            return false;
        }

        /// <summary>
        /// A user who is considered authorized will have permission to lock the TV and interact with it while locked.
        /// The exception to this is when a super user has locked the TV.
        /// You can liken this to a 'moderator' level of permissions.
        /// If master control is enabled, the instance master will have the same permission level as a normal authorized user.
        /// NOTE: Groups[+/Public] do not have any user where isInstanceOwner returns true. An auth plugin is REQUIRED to handle special permissions for those types.
        /// If you call this method prior to the internal ready up phase, it will implicitly return false.
        /// <br/><br/>
        /// User will be considered authorized for any of the following conditions:<br/>
        /// - User is instance owner<br/>
        /// - User is super authorized by an auth plugin<br/>
        /// - User is generally authorized by an auth plugin if the tv has not been locked by the instance owner or super user<br/>
        /// - User is the instance master and master control is enabled but the tv has not been locked by the instance owner or super user<br/>
        /// - TV is not syncing to owner
        /// </summary>
        /// <param name="user">The PlayerAPI object to check</param>
        /// <param name="quiet">Whether to suppress the log output entirely</param>
        /// <returns>Whether the given user has enough privilege or not</returns>
        [PublicAPI]
        public bool _IsAuthorized(VRCPlayerApi user, bool quiet = false)
        {
            // explicitly do NOT check authorization until the ready up event is done.
            // This is because any TVAuthPlugins might not be prepared until the ready-up phase.
            if (!isReady || !VRC.SDKBase.Utilities.IsValid(user)) return false;
            var pid = user.playerId;
            if (localAuthCacheUser == pid) return localAuthCache;
            if (authCacheUser == pid) return authCache;
            string details = "\n";
            bool allow = !syncToOwner;
            bool showTrace = !quiet && IsTraceEnabled;
            if (IsDebugEnabled) details += $"IsMaster {user.isMaster} \nIs First Master {user.displayName == firstMaster} \nIs Instance Owner {user.isInstanceOwner} \n";
            if (showTrace) details += $"Is the TV not syncing to the owner? {allow}\n";
            if (!allow)
            {
                allow = (allowMasterControl && user.isMaster) || (allowFirstMasterControl && user.displayName == firstMaster);
                if (showTrace) details += $"Is the user implicitly authorized? {allow}\n";
            }

            if (!allow)
            {
                allow = hasAuthPlugin && authPlugin._IsAuthorizedUser(user);
                if (showTrace) details += $"Is the user explicitly authorized? {allow}\n";
            }

            // even if allowed by other means, double check the super user lock override and that a lock wasn't initiated by a super user
            if (allow)
            {
                allow = !superUserLockOverride || !lockedBySuper;
                if (showTrace) details += $"And TV is not locked by a superuser? {allow}\n";
            }

            if (!allow)
            {
                allow = (instanceOwnerIsSuper && user.isInstanceOwner) || (allowFirstMasterControl && firstMasterIsSuper && user.displayName == firstMaster);
                if (showTrace) details += $"Is the user implicitly a superuser? {allow}\n";
                if (showTrace) details += $"Owns instance? {user.isInstanceOwner} First Master? {firstMaster}\n";
            }

            if (!allow)
            {
                allow = hasAuthPlugin && authPlugin._IsSuperUser(user);
                if (showTrace) details += $"Is the user explicitly a superuser? {allow}\n";
            }

            if (!quiet && IsDebugEnabled)
            {
                details = $"Is the user {user.displayName} authorized? {allow}{details.TrimEnd(new[] { '\n' })}";
                Debug(details);
            }

            if (user.isLocal)
            {
                // to further reduce redundant checks,
                // separate the auth cache for local user and other users
                localAuthCacheUser = pid;
                localAuthCache = allow;
            }
            else
            {
                authCacheUser = pid;
                authCache = allow;
            }

            return allow;
        }

        /// <returns>Whether the local user has super privilege or not</returns>
        /// <seealso cref="_IsSuperAuthorized(VRCPlayerApi, bool)"/>
        [PublicAPI]
        public bool _IsSuperAuthorized()
        {
            // ensure local player has been cached.
            localPlayer = Networking.LocalPlayer;
            hasLocalPlayer = VRC.SDKBase.Utilities.IsValid(localPlayer);

            if (hasLocalPlayer) return _IsSuperAuthorized(localPlayer);
            Warn("No local player available.");
            return false;
        }

        /// <summary>
        /// A user who is considered super authorized will have permission to lock the TV and interact with it while locked,
        /// and to bypass the domain whitelist restrictions if that feature is enabled.
        /// If a super user locks the TV, generally authorized users cannot control the TV in any way.
        /// You can liken this to an 'admin' level of permissions.
        /// The instance owner (invite[+]/friends[+] instances) will always have super user permissions.
        /// NOTE: Groups[+/Public] do not have any user where isInstanceOwner returns true. An auth plugin is REQUIRED to handle special permissions for those types.
        /// If you call this method prior to the internal ready up phase, it will implicitly return false.
        /// User will be considered super for the following conditions:<br/>
        /// - User is instance owner<br/>
        /// - User is super authorized by an auth plugin<br/>
        /// - TV is not syncing to owner
        /// </summary>
        /// <param name="user">The PlayerAPI object to check</param>
        /// <param name="quiet">Whether to suppress the log output entirely</param>
        /// <returns>Whether the local user has super privilege or not</returns>
        public bool _IsSuperAuthorized(VRCPlayerApi user, bool quiet = false)
        {
            // explicitly do NOT check authorization until the ready up event is done.
            // This is because any TVAuthPlugins might not be prepared until the readyup phase.
            if (!isReady || !VRC.SDKBase.Utilities.IsValid(user)) return false;
            var pid = user.playerId;
            if (superAuthCacheUser == pid) return superAuthCache;
            string details = "\n";
            bool allow = !syncToOwner;
            bool showTrace = !quiet && IsTraceEnabled;
            if (IsDebugEnabled) details += $"IsFirstMaster {user.displayName == firstMaster}\nIsInstanceOwner {user.isInstanceOwner}\n";
            if (showTrace) details += $"Is the TV not syncing to the owner? {allow}\n";

            if (!allow)
            {
                allow = (instanceOwnerIsSuper && user.isInstanceOwner) || (allowFirstMasterControl && firstMasterIsSuper && user.displayName == firstMaster);
                if (showTrace) details += $"Is the user implicitly a superuser? {allow}\n";
            }

            if (!allow)
            {
                allow = hasAuthPlugin && authPlugin._IsSuperUser(user);
                if (showTrace) details += $"Is the user explicitly a superuser? {allow}\n";
            }

            if (!quiet && IsDebugEnabled)
            {
                details = $"Is the user {user.displayName} super authorized? {allow}{details.TrimEnd(new[] { '\n' })}";
                Debug(details);
            }

            superAuthCacheUser = pid;
            superAuthCache = allow;
            return allow;
        }
    }
}