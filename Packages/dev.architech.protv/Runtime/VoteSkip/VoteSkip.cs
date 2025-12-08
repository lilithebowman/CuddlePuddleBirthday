using System;
using UdonSharp;
using UnityEngine;
using Button = UnityEngine.UI.Button;
using Text = UnityEngine.UI.Text;
using UnityEngine.UI;
using VRC.SDKBase;

namespace ArchiTech.ProTV
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    [DefaultExecutionOrder(-1)]
    public class VoteSkip : TVPlugin
    {
        [Range(0.1f, 0.9f)] public float defaultVoteRatio = 0.67f;
        public Slider voteRatioAdjustement;
        public Button voteAction;
        public Text display;
        public VoteZone zone;

        [NonSerialized] public VRCPlayerApi IN_PLAYER;

        // starts at a small but reasonable size. Will auto-expand as needed.
        [UdonSynced] private int[] votes = new int[10];
        [UdonSynced] private float ratioNeeded;
        [UdonSynced] private int urlHashSync;
        private int urlHash;
        private int totalAvailableVoters = 0;
        private bool hasVoted = false;
        private bool allowedToVote = false;
        private bool votingEnabled = true;
        private bool isChangingVote = false;
        private bool voteRequestConfirmed = false;
        private bool voteResponseConfirmed = false;

        private bool isLocked = false;

        private bool hasRatioSlider = false;

        // private bool hasVoteAction = false;
        private bool hasDisplay = false;

        // private bool hasSkip = false;
        // private bool hasUnSkip = false;
        private bool hasVoteZone = false;
        public bool debugInDisplay = false;

        private bool isTvOwner => hasLocalPlayer && Networking.IsOwner(localPlayer, tv.gameObject);

        public override void Start()
        {
            if (init) return;
            base.Start();
            if (!hasTV) return;

            hasRatioSlider = voteRatioAdjustement != null;
            hasDisplay = display != null;
            // hasVoteAction = voteAction != null;
            ratioNeeded = defaultVoteRatio;
            if (hasRatioSlider) voteRatioAdjustement.value = ratioNeeded;
            // check for vote zone usage
            hasVoteZone = zone != null;
            if (hasVoteZone) zone._SetVoteSkip(this);
            // if no colliders, implicitly allow everyone to vote.
            allowedToVote = !hasVoteZone;
            isLocked = tv.lockedByDefault;
            votingEnabled = allowedToVote && !isLocked;
            tv._RegisterListener(this);
        }

        // Needs rework. For now just dont' disable the game object with the script on it.
        // void OnEnable()
        // {
        //     int currentPlayerCount = VRCPlayerApi.GetPlayerCount();
        //     if (!useVoteZones)
        //     {
        //         // the whole world is a zone, just use the count
        //         totalAvailableVoters = currentPlayerCount;
        //     }
        //     else
        //     {
        //         // examine any colliders for player presense
        //         VRCPlayerApi[] players = new VRCPlayerApi[currentPlayerCount];
        //         Collider[] colliders = GetComponents<Collider>();
        //         totalAvailableVoters = 0;
        //         foreach (VRCPlayerApi player in players)
        //         {
        //             Vector3 playerPos = player.GetPosition();
        //             foreach (Collider col in colliders)
        //             {
        //                 if (col.ClosestPoint(playerPos) == playerPos) {
        //                     totalAvailableVoters++;
        //                     break;
        //                 }
        //             }
        //         }
        //     }
        // }

        public void _Enter()
        {
            if (IN_PLAYER == null) return;
            _Enter(IN_PLAYER);
            IN_PLAYER = null;
        }

        public void _Enter(VRCPlayerApi p)
        {
            if (p.isLocal)
            {
                allowedToVote = true;
                Info("Entered voting zone. Voting enabled.");
            }

            totalAvailableVoters++;
            updateVoteInfo();
        }

        public void _Exit()
        {
            if (IN_PLAYER == null) return;
            _Exit(IN_PLAYER);
            IN_PLAYER = null;
        }

        public void _Exit(VRCPlayerApi p)
        {
            if (p == null) return;
            if (p.isLocal)
            {
                allowedToVote = false;
                Info("Exited voting zone. Voting disabled.");
            }

            totalAvailableVoters--;
            if (totalAvailableVoters < 0) totalAvailableVoters = 0;
            if (isTvOwner)
            {
                var id = p.playerId;
                if (voteIsIn(id))
                {
                    Info($"Player {id} exited VOTING ZONE while vote was in. Attempting removal.");
                    Owner = localPlayer;
                    RequestSerialization();
                    removePlayerVote(id);
                }
            }

            updateVoteInfo();
        }

        public override void OnPlayerJoined(VRCPlayerApi plyr)
        {
            // Go upvote this canny and get the devs to fix plskthnxbai
            // https://vrchat.canny.io/vrchat-udon-closed-alpha-bugs/p/vrcplayerapiplayerid-may-returns-1-in-onplayerleft
            var thisIsSoStupidWhyDoWeHaveToAccessThePlayerIdForItToBeRetainedWhenThePlayerLeavesSIGH = plyr.playerId;
            if (!hasVoteZone)
            {
                totalAvailableVoters++;
                Debug($"Player joined. New voter total count {totalAvailableVoters}");
                if (plyr.isLocal) Debug("Voting Enabled");
            }

            updateVoteInfo();
        }

        public override void OnPlayerLeft(VRCPlayerApi plyr)
        {
            if (!hasVoteZone)
            {
                totalAvailableVoters--;
                Debug($"Player left. New voter total count {totalAvailableVoters}");
            }

            if (isTvOwner)
            {
                var id = plyr.playerId;
                Debug($"Checking for player {id}");
                if (voteIsIn(id))
                {
                    Info($"Player {id} left WORLD while vote was in. Attempting removal.");
                    Owner = localPlayer;
                    RequestSerialization();
                    removePlayerVote(id);
                }
            }

            updateVoteInfo();
        }

        public override void OnDeserialization()
        {
            bool voteFound = voteIsIn(localPlayer.playerId);
            // make sure the url hasn't changed before confirming self vote
            if (urlHash == urlHashSync)
            {
                if (voteFound != hasVoted)
                {
                    // vote discrepancy found, re-queue vote
                    voteResponseConfirmed = false;
                    queueVote();
                    Info("Votes received and self vote has discrepancy. Attempting to redo the vote.");
                }
                else
                {
                    voteResponseConfirmed = true;
                    Info("Votes received and self vote verified");
                }
            }
            else urlHash = urlHashSync;

            updateVoteInfo();
            checkForSkipSuccess();
        }

        public override void OnPreSerialization() { }

        public override void OnPostSerialization(VRC.Udon.Common.SerializationResult result)
        {
            if (result.success)
            {
                if (isChangingVote)
                {
                    voteRequestConfirmed = true;
                    isChangingVote = false;
                    Info("Self vote sent successfully");
                }

                updateVoteInfo();
                checkForSkipSuccess();
            }
            else
            {
                // vote failed to send, requeue vote change
                if (isChangingVote)
                {
                    queueVote();
                    Info("Self vote update failed. Retrying...");
                }
                // if not a vote, it is the TV owner updating the data, force retry immediately
                else
                {
                    Owner = localPlayer;
                    RequestSerialization();
                    Info("Owner votes array update failed. Retrying...");
                }
            }
        }

        // === TV EVENTS ===

        public override void _TvMediaChange() => votesReset();

        public override void _TvMediaLoop()
        {
            if (tv.IsManualLoop) votesReset();
        }

        public override void _TvLock()
        {
            isLocked = true;
            votingEnabled = tv._IsAuthorized();
            updateVoteInfo();
        }

        public override void _TvUnLock()
        {
            isLocked = false;
            votingEnabled = true;
            updateVoteInfo();
        }

        private void votesReset()
        {
            // everyone unsets their local vote flag
            hasVoted = false;
            if (isTvOwner)
            {
                // owner takes control and clears all synced votes
                Owner = localPlayer;
                RequestSerialization();
                urlHash = urlHashSync = tv.url.Get().GetHashCode();
                clearPlayerVotes();
            }

            updateVoteInfo();
        }

        public void _ToggleVote()
        {
            if (hasVoted) _RemoveVote();
            else _Vote();
        }

        public void _Vote()
        {
            if (tv.IsEnded || tv.IsSkipping) return; // nothing to vote on
            if (allowedToVote && votingEnabled) { }
            else
            {
                Warn("Not allowed to vote currently.");
                return; // local player is not within the voting range or TV is locked
            }

            if (tv.loading || tv.waitingForMediaRefresh)
            {
                Info("Cannot vote yet. Wait for video to finish loading.");
                return;
            }

            if (voteIsIn(localPlayer.playerId))
            {
                Info("Already voted");
                return; // no double voting
            }

            hasVoted = true;
            isChangingVote = true;
            voteRequestConfirmed = false;
            voteResponseConfirmed = false;
            Owner = localPlayer;
            RequestSerialization();
            addPlayerVote(localPlayer.playerId);
            Info("Attempting to vote");
            // if player is all by their lonesome, simply fire the skip checker since serialization never happens when alone.
            // also check for privileged access when tv is locked
            if (VRCPlayerApi.GetPlayerCount() == 1 || isLocked && tv._IsAuthorized())
                checkForSkipSuccess();
            // TODO figure out a better way of handling UI/ and vote state for solo voting
            updateVoteInfo();
        }

        public void _RemoveVote()
        {
            if (tv.loading || tv.waitingForMediaRefresh)
            {
                Info("Cannot vote yet. Wait for video to finish loading.");
                return;
            }

            if (voteIsIn(localPlayer.playerId)) { }
            else
            {
                Info("Have not voted yet.");
                return; // no double un-voting
            }

            hasVoted = false;
            isChangingVote = true;
            voteRequestConfirmed = false;
            voteResponseConfirmed = false;
            Owner = localPlayer;
            RequestSerialization();
            removePlayerVote(localPlayer.playerId);
            Info("Attempting to remove vote");
            updateVoteInfo();
        }

        public void _UpdateRatio()
        {
            if (voteRatioAdjustement.value == ratioNeeded) return;
            if (tv._IsAuthorized())
            {
                Owner = localPlayer;
                RequestSerialization();
                ratioNeeded = voteRatioAdjustement.value;
                updateVoteInfo();
            }
            // Does not have permission to update the ratio, revert to previous value
            else voteRatioAdjustement.value = ratioNeeded;
        }

        private void addPlayerVote(int id)
        {
            // find first empty vote slot
            var i = System.Array.IndexOf(votes, 0);
            if (i > -1) votes[i] = id;
            else appendVote(id);
        }

        private void removePlayerVote(int id)
        {
            // remove occupied vote slot
            var i = System.Array.IndexOf(votes, id);
            if (i > -1) votes[i] = 0;
        }

        private void clearPlayerVotes()
        {
            for (int i = 0; i < votes.Length; i++) votes[i] = 0;
        }

        private void queueVote()
        {
            SendCustomEventDelayedSeconds(hasVoted ? nameof(_Vote) : nameof(_RemoveVote), 0.5f);
        }

        private bool voteIsIn(int id) => System.Array.IndexOf(votes, id) > -1;

        private int tallyVotes()
        {
            int count = 0;
            foreach (int id in votes)
                if (id > 0)
                    count++;
            return count;
        }

        private void updateVoteInfo()
        {
            // update display information
            int voteCount = tallyVotes();
            int votesRequired = Mathf.CeilToInt(totalAvailableVoters * ratioNeeded);
            if (hasDisplay)
            {
                string txt;
                if (!isLocked || votingEnabled) txt = hasVoted ? "UnSkip" : "Skip";
                else txt = "Locked";
                // donot display the ratio when the TV is locked and user isn't privileged.
                if (!isLocked) txt += $" ({voteCount}/{votesRequired})";
                display.text = txt;
            }

            // if (hasVoteAction)
            // {
            //     voteAction.gameObject.SetActive(votingEnabled);
            // }
            if (hasDisplay && debugInDisplay)
            {
                object[] args = { totalAvailableVoters, ratioNeeded, hasVoted, voteRequestConfirmed, voteResponseConfirmed };
                display.text += string.Format("<size=5>\nTotal*Ratio: {0}*{1:F2} | Voted? {2} | Validation: Req_{3} Res_{4}</size>", args);
            }

            if (hasRatioSlider) voteRatioAdjustement.gameObject.SetActive(tv._IsAuthorized());
        }

        private void checkForSkipSuccess()
        {
            // disable skipping if no voters are present
            if (totalAvailableVoters <= 0) return;
            // check for vote success
            if (isTvOwner)
            {
                int voteCount = tallyVotes();
                int votesRequired = Mathf.CeilToInt(totalAvailableVoters * ratioNeeded);
                if (tv.locked || voteCount >= votesRequired) tv._Skip();
            }
        }

        private void appendVote(int id)
        {
            int len = votes.Length;
            int[] newVotes = new int[len + 1];
            System.Array.Copy(votes, newVotes, len);
            newVotes[len] = id;
            votes = newVotes;
        }
    }
}