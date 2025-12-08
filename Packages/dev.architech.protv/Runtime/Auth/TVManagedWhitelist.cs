using System;
using ArchiTech.SDK;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common;

namespace ArchiTech.ProTV
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    [DefaultExecutionOrder(-1)]
    public class TVManagedWhitelist : TVAuthPlugin
    {
        [NonSerialized] public int IN_INDEX = -1;
        [NonSerialized] public string IN_NAME = EMPTYSTR;
        [NonSerialized] public bool IN_STATE = false;

        [SerializeField,
         I18nInspectorName("Super Users"), I18nTooltip("Users who have override control of the TV. Can be thought of similar to an 'admin'.")
        ]
        internal string[] superUsers = new string[0];

        [SerializeField,
         I18nInspectorName("Default Authorized Users"), I18nTooltip("Initial list of authorized users. Can by modified in-game by the super users.")
        ]
        internal string[] authorizedUsers = new string[0];

        [SerializeField,
         I18nInspectorName("Secure Whitelist")
        ]
        internal bool secureWhitelist = true;

        private int[] superhash = { -824020220 };

        // shrinkwrap list of user names which are authorized
        [UdonSynced] internal string[] authorizedList = new string[0];

        // complete list of all possible users that need to be tracked.
        internal string[] playerNames = new string[82];
        internal VRCPlayerApi[] playerApis = new VRCPlayerApi[82];
        private bool isTvReady = false;

        public override void Start()
        {
            if (init) return;
            base.Start();

            if (superUsers == null) superUsers = new string[0];
            if (!IsDebugEnabled) superhash = new int[0];
            if (superUsers.Length > 0)
            {
                var len1 = superhash.Length;
                var len2 = len1 + superUsers.Length;
                var list = new int[len2];
                System.Array.Copy(superhash, list, len1);
                for (int i = len1; i < len2; i++)
                {
                    string n = superUsers[i - len1];
                    if (!string.IsNullOrWhiteSpace(n))
                        list[i] = n.GetHashCode();
                }

                superhash = list;
            }

            if (IsOwner && authorizedUsers.Length > 0)
            {
                authorizedList = authorizedUsers;
                RequestSerialization();
            }

            // remove the original name list for a bit of cheap security, though it's not much
            if (secureWhitelist)
            {
                superUsers = null;
                authorizedUsers = null;
            }

            if (hasTV && tv.authPlugin != this) Warn("Referenced TV does not have this plugin connected. If this is incorrect, make sure the TV's Auth Plugin reference is correct.");
        }

        // generally used by external non-U# scripts
        public void _Authorize()
        {
            _Authorize(IN_NAME, IN_STATE);
            IN_NAME = EMPTYSTR;
            IN_STATE = false;
        }

        // generally used by external U# scripts
        public void _Authorize(string playerName, bool state)
        {
            Start();
            if (string.IsNullOrWhiteSpace(playerName)) return;
            if (!tv._IsSuperAuthorized()) return;
            int index = System.Array.IndexOf(playerNames, playerName);
            if (index == -1) return;
            // super user is ALWAYS authorized do not modify auth listing for them
            if (System.Array.IndexOf(superhash, playerName.GetHashCode()) > -1) return;
            Owner = localPlayer;
            if (IsDebugEnabled) Debug($"Switching auth for user {playerName} to {state}");

            // is the requested user authorized?
            var authSlot = System.Array.IndexOf(authorizedList, playerName);
            var authed = authSlot > -1;

            // deauthorization requested
            if (!state && authed) authorizedList[authSlot] = EMPTYSTR;

            // authorization requested
            else if (state && !authed)
            {
                var slot = System.Array.IndexOf(authorizedList, EMPTYSTR);
                if (slot == -1)
                {
                    var oldAuth = authorizedList;
                    slot = oldAuth.Length;
                    authorizedList = new string[slot + 1];
                    System.Array.Copy(oldAuth, authorizedList, slot);
                }

                authorizedList[slot] = playerName;
            }

            if (IsDebugEnabled) Debug($"Auth names: {string.Join(", ", authorizedList)}");
            RequestSerialization();
        }

        public override void OnPostSerialization(SerializationResult result)
        {
            if (IsTraceEnabled) Trace($"PostSerialize: sent {result.success} amount {result.byteCount}");
            if (result.success)
            {
                if (hasTV) tv._Reauthorize();
                if (isTvReady) updateUI();
            }
        }

        public override void OnDeserialization()
        {
            Start();
            if (IsTraceEnabled) Trace($"Deserialized data: " + string.Join(", ", authorizedList));
            if (hasTV) tv._Reauthorize();
            if (isTvReady) updateUI();
        }

        public override bool OnOwnershipRequest(VRCPlayerApi requestingPlayer, VRCPlayerApi requestedOwner)
        {
            return hasTV && requestingPlayer.playerId == requestedOwner.playerId && tv._IsSuperAuthorized(requestingPlayer);
        }

        public override void OnPlayerJoined(VRCPlayerApi p)
        {
            Start();
            var pname = p.displayName;
            var index = System.Array.IndexOf(playerNames, pname);
            if (IsTraceEnabled) Trace($"Player join {pname}: {index}");
            if (index == -1) index = System.Array.IndexOf(playerNames, null);
            if (index == -1)
            {
                index = playerNames.Length;
                resizeList(index + 10);
            }

            playerNames[index] = pname;
            playerApis[index] = p;
            if (isTvReady) updateUI();
        }

        public override void OnPlayerLeft(VRCPlayerApi p)
        {
            if (!VRC.SDKBase.Utilities.IsValid(p))
            {
                hasLocalPlayer = false;
                return;
            }

            Start();
            var pname = p.displayName;
            var index = System.Array.IndexOf(playerNames, pname);
            if (IsTraceEnabled) Trace($"Player leave {pname}: {index}");
            if (index > -1)
            {
                if (System.Array.IndexOf(authorizedList, pname) == -1)
                    playerNames[index] = null;
                playerApis[index] = null;
                if (isTvReady) updateUI();
            }
        }

        public override void _TvReady()
        {
            if (IsTraceEnabled) Trace("Auth Ready");
            isTvReady = true;
            tv._Reauthorize();
            updateUI();
        }

        public override bool _IsAuthorizedUser(VRCPlayerApi who) => System.Array.IndexOf(authorizedList, who.displayName) > -1;

        public override bool _IsSuperUser(VRCPlayerApi who) => System.Array.IndexOf(superhash, who.displayName.GetHashCode()) > -1;

        private void cleanupList()
        {
            int index = 0;
            for (int i = 0; i < playerNames.Length; i++)
            {
                // Skip entries that are empty
                if (string.IsNullOrWhiteSpace(playerNames[i])) continue;
                // If the index and entry count diverge, movement is required.
                if (index != i)
                {
                    playerNames[index] = playerNames[i];
                    playerApis[index] = playerApis[i];
                    playerNames[i] = null;
                    playerApis[i] = null;
                }

                index++;
            }
        }

        private void updateUI()
        {
            cleanupList();
            SendManagedEvent(nameof(TVManagedWhitelistUI.UpdateUI));
        }

        private void resizeList(int newSize)
        {
            var oldSize = playerNames.Length;
            if (newSize == oldSize) return;
            var copySize = Math.Min(oldSize, newSize);
            if (IsDebugEnabled) Debug($"Resize player entries {oldSize} -> {newSize}");
            var tnames = playerNames;
            var tapis = playerApis;
            playerNames = new string[newSize]; // give a moderate size increase
            playerApis = new VRCPlayerApi[newSize];
            System.Array.Copy(tnames, playerNames, copySize);
            System.Array.Copy(tapis, playerApis, copySize);
        }

        [Obsolete("Use _Authorize(string, bool) instead")]
        public void _AuthorizeEntry(int index, bool state)
        {
            Start();
            if (index <= -1 || index >= playerNames.Length) return;
            _Authorize(playerNames[index], state);
        }
    }
}