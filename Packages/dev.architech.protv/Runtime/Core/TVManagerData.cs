using ArchiTech.SDK;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common;

namespace ArchiTech.ProTV
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    [DefaultExecutionOrder(-10_000)]
    public class TVManagerData : ATBehaviour
    {
        private TVManager tv;

        [UdonSynced] internal TVPlayState state = TVPlayState.STOPPED;
        [UdonSynced] internal TVErrorState errorState = TVErrorState.NONE;
        [UdonSynced] internal VRCUrl mainUrl = new VRCUrl("");
        [UdonSynced] internal VRCUrl alternateUrl = new VRCUrl("");
        [UdonSynced] internal string title = EMPTYSTR;
        [UdonSynced] internal string addedBy = EMPTYSTR;
        [UdonSynced] internal bool locked = false;
        [UdonSynced] internal bool loading = false;
        [UdonSynced] internal int urlRevision = 0;
        [UdonSynced] internal int videoPlayer = -1;
        [UdonSynced] internal float playbackSpeed = 1f;
        [UdonSynced] internal float volume = 0;
        [UdonSynced] internal bool audio3d = false;
        [UdonSynced] internal TV3DMode video3d = 0;
        [UdonSynced] internal bool video3dFull = false;
        [UdonSynced] internal int loop = 0;
        [UdonSynced] internal float time = 0;
        [UdonSynced] internal string currentOwner = EMPTYSTR;
        [UdonSynced] internal string currentMaster = EMPTYSTR;
        [UdonSynced] internal string firstMaster = EMPTYSTR;
        [UdonSynced] internal string instanceOwner = EMPTYSTR;

        public override void Start()
        {
            if (init) return;
            SetLogPrefixColor("#cccc44");
            base.Start();
            tv = GetComponentInParent<TVManager>();
            if (tv == null) SetLogPrefixLabel($"<Missing TV Ref>/{name}");
            else
            {
                SetLogPrefixLabel($"{tv.gameObject.name}/{name}");
                if (Logger == null) Logger = tv.Logger;
                if (tv.LogLevelOverride) LoggingLevel = tv.LoggingLevel;
            }
        }

        public override void OnPreSerialization()
        {
            // Extract data from TV for manual sync
            state = tv.state;
            mainUrl = tv.urlMain;
            alternateUrl = tv.urlAlt;
            title = tv.title;
            addedBy = tv.addedBy;
            // sanity check for rare case where unity serialization fucks up and nullifies the internal string of a supposedly empty VRCUrl
            if (mainUrl.Get() == null) mainUrl = VRCUrl.Empty;
            if (alternateUrl.Get() == null) alternateUrl = VRCUrl.Empty;
            // pull the owner's values
            locked = tv.locked;
            loading = tv.loading;
            urlRevision = tv.urlRevision;
            videoPlayer = tv.videoPlayer;
            errorState = tv.errorState;
            volume = tv.volume;
            audio3d = tv.audio3d;
            video3d = tv.video3d;
            video3dFull = tv.video3dFull;
            playbackSpeed = tv.playbackSpeed;
            loop = tv.loop;
            time = tv.currentTime;
            currentOwner = tv.Owner.displayName;
            currentMaster = tv.currentMaster;
            firstMaster = tv.firstMaster;
            instanceOwner = tv.instanceOwner;
            if (videoPlayer >= tv.videoManagers.Length || videoPlayer < 0) videoPlayer = 0;

            if (IsTraceEnabled) serializedDataLog("PreSerialization Data:");
        }

        public override void OnPostSerialization(SerializationResult result)
        {
            if (result.success)
            {
                Debug("All good.");
                updateSyncedData();
                tv.stateOwner = state;
            }
            else
            {
                Warn("Failed to sync, retrying.");
                SendCustomEventDelayedSeconds(nameof(_RequestData), 5f);
            }
        }

        public override void OnDeserialization()
        {
            if (IsTraceEnabled) serializedDataLog("Deserialization Data:");
            updateSyncedData();
            tv._PostDeserialization();
        }

        private void serializedDataLog(string header)
        {
            string log = header;
            log += $"\nPlay State {state} | Error State {errorState} | Loading State {loading}";
            log += $"\nLocked {locked} | Url Revision {urlRevision} | Video Player {videoPlayer}";
            log += $"\nMain URL {mainUrl} | Alt URL {alternateUrl} | Title {title}";
            log += $"\nVolume {volume} | Time {time}";
            log += tv.allowMasterControl ? $"\nInstance Master: {currentMaster}" : "\nInstance Master Control Disabled";
            log += tv.allowFirstMasterControl ? $"\nFirst Master: {firstMaster}" : "\nFirst Master Control Disabled";
            log += instanceOwner != EMPTYSTR ? $"\nInstance Owner: {instanceOwner}" : "\nInstance Owner Not Detected";
            log += currentOwner != EMPTYSTR ? $"\nTV Owner: {currentOwner}" : "\nTV Owner Not Detected";
            Trace(log);
        }

        private void updateSyncedData()
        {
            tv.syncState = state;
            tv.syncUrlMain = mainUrl;
            tv.syncUrlAlt = alternateUrl;
            tv.syncTitle = title;
            tv.syncLoading = loading;
            tv.syncAddedBy = addedBy;
            tv.syncLocked = locked;
            tv.syncUrlRevision = urlRevision;
            tv.syncVideoPlayer = videoPlayer;
            tv.syncErrorState = errorState;
            tv.syncVolume = volume;
            tv.syncAudio3d = audio3d;
            tv.syncVideo3d = video3d;
            tv.syncVideo3dFull = video3dFull;
            tv.syncLoop = loop;
            tv.syncPlaybackSpeed = playbackSpeed;
            tv.syncTime = time;
            tv.currentOwner = currentOwner;
            tv.currentMaster = currentMaster;
            if (string.IsNullOrEmpty(tv.instanceOwner))
                tv.instanceOwner = instanceOwner;
            if (string.IsNullOrEmpty(tv.firstMaster))
                tv.firstMaster = firstMaster;
        }

        public override bool OnOwnershipRequest(VRCPlayerApi requestingPlayer, VRCPlayerApi requestedOwner)
        {
            // allow transfer if unlocked or if the requesting player has enough privilege
            bool transfer = (!tv.locked && !tv.disallowUnauthorizedUsers) || tv._IsAuthorized(requestingPlayer);
            if (IsDebugEnabled)
            {
                string status = transfer ? "<color=green>passed</color>" : "<color=red>rejected</color>";
                Debug($"Ownership transfer request {status} from {Owner.displayName} [{Owner.playerId}] to {requestedOwner.displayName} [{requestedOwner.playerId}] by {requestingPlayer.displayName} [{requestingPlayer.playerId}]");
            }

            return transfer;
        }

        public void _RequestData()
        {
            Start();
            if (IsDebugEnabled) Debug("Requesting serialization");
            RequestSerialization();
        }
    }
}