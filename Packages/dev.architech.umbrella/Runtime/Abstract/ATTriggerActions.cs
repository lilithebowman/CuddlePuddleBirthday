using System.Diagnostics.CodeAnalysis;
using ArchiTech.SDK;
using UnityEngine;
using UnityEngine.Playables;
using VRC.SDKBase;
using VRC.Udon;

// ReSharper disable MemberCanBeMadeStatic.Local

namespace ArchiTech.Umbrella
{
    [System.AttributeUsage(System.AttributeTargets.Field)]
    internal class ATTriggerMainObjectType : System.Attribute
    {
        public readonly System.Type type;

        public ATTriggerMainObjectType(System.Type type)
        {
            this.type = type;
        }
    }

    public enum ATTriggerActionType
    {
        [I18nInspectorName("Object / Toggle"), ATTriggerMainObjectType(typeof(GameObject))]
        OBJECT_ENABLE = 0,

        [I18nInspectorName("Object / Teleport"), ATTriggerMainObjectType(typeof(Transform))]
        OBJECT_TELEPORT = 1,

        [I18nInspectorName("Object / Re-Parent"), ATTriggerMainObjectType(typeof(Transform))]
        OBJECT_REPARENT = 2,

        [I18nInspectorName("Player / Teleport To"), ATTriggerMainObjectType(typeof(Transform))]
        PLAYER_TELEPORT_TO = 12,

        [I18nInspectorName("Player / Teleport")]
        PLAYER_TELEPORT = 13,

        [I18nInspectorName("Player / Speed")] PLAYER_SPEED = 14,

        [I18nInspectorName("Player / Velocity")]
        PLAYER_VELOCITY = 15,

        [I18nInspectorName("Player / Gravity")]
        PLAYER_GRAVITY = 16,

        [I18nInspectorName("Player / Reset Movement")]
        RESET_MOVEMENT = 17,

        [I18nInspectorName("Collider / Toggle"), ATTriggerMainObjectType(typeof(Collider))]
        COLLIDER_ENABLE = 19,

        [I18nInspectorName("Collider / IsTrigger"), ATTriggerMainObjectType(typeof(Collider))]
        COLLIDER_TRIGGER = 20,

        [I18nInspectorName("Collider / [Sphere] Center"), ATTriggerMainObjectType(typeof(SphereCollider))]
        COLLIDER_SPHERE_CENTER = 21,

        [I18nInspectorName("Collider / [Sphere] Radius"), ATTriggerMainObjectType(typeof(SphereCollider))]
        COLLIDER_SPHERE_RADIUS = 22,

        [I18nInspectorName("Collider / [Box] Center"), ATTriggerMainObjectType(typeof(BoxCollider))]
        COLLIDER_BOX_CENTER = 23,

        [I18nInspectorName("Collider / [Box] Size"), ATTriggerMainObjectType(typeof(BoxCollider))]
        COLLIDER_BOX_SIZE = 24,

        [I18nInspectorName("Collider / [Capsule] Center"), ATTriggerMainObjectType(typeof(CapsuleCollider))]
        COLLIDER_CAPSULE_CENTER = 25,

        [I18nInspectorName("Collider / [Capsule] Radius"), ATTriggerMainObjectType(typeof(CapsuleCollider))]
        COLLIDER_CAPSULE_RADIUS = 26,

        [I18nInspectorName("Collider / [Capsule] Radius"), ATTriggerMainObjectType(typeof(CapsuleCollider))]
        COLLIDER_CAPSULE_HEIGHT = 27,

        [I18nInspectorName("Time Delay (Experimental)")]
        DELAY = 18,

        [I18nInspectorName("Udon / Toggle"), ATTriggerMainObjectType(typeof(UdonBehaviour))]
        UDON_ENABLE = 29,

        [I18nInspectorName("Udon / Event"), ATTriggerMainObjectType(typeof(UdonBehaviour))]
        UDON_EVENT = 3,

        [I18nInspectorName("Udon / Public Variable"), ATTriggerMainObjectType(typeof(UdonBehaviour))]
        UDON_VARIABLE = 33,

        [I18nInspectorName("Udon / Private Variable"), ATTriggerMainObjectType(typeof(UdonBehaviour))]
        UDON_VARIABLE_PRIVATE = 34,

        [I18nInspectorName("Animator / Toggle"), ATTriggerMainObjectType(typeof(Animator))]
        ANIMATOR_ENABLE = 36,

        [I18nInspectorName("Animator / Play"), ATTriggerMainObjectType(typeof(Animator))]
        ANIMATOR_PLAY = 30,

        [I18nInspectorName("Animator / Cross Fade"), ATTriggerMainObjectType(typeof(Animator))]
        ANIMATOR_CROSSFADE = 31,

        [I18nInspectorName("Animator / Trigger"), ATTriggerMainObjectType(typeof(Animator))]
        ANIMATOR_TRIGGER = 4,

        [I18nInspectorName("Animator / Bool"), ATTriggerMainObjectType(typeof(Animator))]
        ANIMATOR_BOOL = 5,

        [I18nInspectorName("Animator / Integer"), ATTriggerMainObjectType(typeof(Animator))]
        ANIMATOR_INT = 6,

        [I18nInspectorName("Animator / Float"), ATTriggerMainObjectType(typeof(Animator))]
        ANIMATOR_FLOAT = 7,

        [I18nInspectorName("Audio / Toggle"), ATTriggerMainObjectType(typeof(AudioSource))]
        AUDIO_ENABLE = 37,

        [I18nInspectorName("Audio / Action"), ATTriggerMainObjectType(typeof(AudioSource))]
        AUDIO_ACTION = 8,

        [I18nInspectorName("Audio / Option"), ATTriggerMainObjectType(typeof(AudioSource))]
        AUDIO_OPTION = 9,

        [I18nInspectorName("Audio / Play Clip"), ATTriggerMainObjectType(typeof(AudioSource))]
        AUDIO_CLIP_PLAY = 28,

        [I18nInspectorName("Audio / Change Clip"), ATTriggerMainObjectType(typeof(AudioSource))]
        AUDIO_CLIP_CHANGE = 10,

        [I18nInspectorName("Particles / Action"), ATTriggerMainObjectType(typeof(ParticleSystem))]
        PARTICLE_ACTION = 11,

        [I18nInspectorName("Timeline / Toggle"), ATTriggerMainObjectType(typeof(PlayableDirector))]
        TIMELINE_ENABLE = 35,

        [I18nInspectorName("Timeline / Action"), ATTriggerMainObjectType(typeof(PlayableDirector))]
        TIMELINE_ACTION = 32
    }

    public enum ATTriggerToggleAction
    {
        [I18nInspectorName("Enable")] ENABLE = 1,
        [I18nInspectorName("Disable")] DISABLE = 2,
    }

    [System.Flags]
    public enum ATTriggerTeleportAction
    {
        [I18nInspectorName("World Position")] POSITION = (1 << 0),
        [I18nInspectorName("World Rotation")] ROTATION = (1 << 1),
        [I18nInspectorName("Local Scale")] SCALE = (1 << 2),
    }

    public enum ATTriggerAudioAction
    {
        [I18nInspectorName("Play")] PLAY = 1,
        [I18nInspectorName("Pause")] PAUSE = 2,
        [I18nInspectorName("UnPause")] UNPAUSE = 3,
        [I18nInspectorName("Stop")] STOP = 4,
        [I18nInspectorName("Mute")] MUTE = 5,
        [I18nInspectorName("Unmute")] UNMUTE = 6,
        [I18nInspectorName("Enable Loop")] LOOP = 7,
        [I18nInspectorName("Disable Loop")] NOLOOP = 8,
    }

    public enum ATTriggerAudioOption
    {
        [I18nInspectorName("Volume"), Range(0f, 1f)]
        VOLUME = 1,

        [I18nInspectorName("Pitch"), Range(-3f, 3f)]
        PITCH = 2,
        [I18nInspectorName("Time")] TIME = 3,

        [I18nInspectorName("Stereo Pan"), Range(-1f, 1f)]
        STEREO_PAN = 4,

        [I18nInspectorName("Spatial Blend"), Range(0f, 1f)]
        SPATIAL_BLEND = 5,

        [I18nInspectorName("Reverb Zone Mix"), Range(0f, 1.1f)]
        REVERB_MIX = 6,

        [I18nInspectorName("Spread"), Range(0f, 360f)]
        SPREAD = 7,

        [I18nInspectorName("Doppler Level"), Range(0f, 5f)]
        DOPPLER = 8,
        [I18nInspectorName("Min Distance")] MIN_DIST = 9,
        [I18nInspectorName("Max Distance")] MAX_DIST = 10,

        [I18nInspectorName("Priority"), Range(0f, 256f)]
        PRIORITY = 11,
    }

    public enum ATTriggerParticleAction
    {
        [I18nInspectorName("Play")] PLAY = 1,
        [I18nInspectorName("Pause")] PAUSE = 2,
        [I18nInspectorName("Stop")] STOP = 3,
        [I18nInspectorName("Clear Particles")] CLEAR = 4,
    }

    public enum ATTriggerTimelineAction
    {
        [I18nInspectorName("Play")] PLAY = 1,
        [I18nInspectorName("Pause")] PAUSE = 2,
        [I18nInspectorName("Resume")] RESUME = 3,
        [I18nInspectorName("Stop")] STOP = 4,
    }

    public abstract class ATTriggerActions : SDK.ATBehaviour
    {
        // delay controls
        private const string delayInfoKey = "ATTriggerDelay";
        private string delayInfoKeyId;
        private int delayContinueIndex;
        private float delayContinueTime;


        private float initPlayerWalk;
        private float initPlayerStrafe;
        private float initPlayerRun;
        private float initPlayerJump;

        protected bool DelayIsActiveSelf => delayContinueIndex > 0 && localPlayer.GetPlayerTag(delayInfoKey) == delayInfoKeyId;

        protected bool DelayIsActiveOther
        {
            get
            {
                var playerTag = localPlayer.GetPlayerTag(delayInfoKey);
                return !string.IsNullOrEmpty(playerTag) && playerTag != delayInfoKeyId;
            }
        }

        protected float DelayTime => delayContinueTime;
        protected float DelayTimeRemaining => delayContinueIndex == 0 ? 0f : Mathf.Max(0f, delayContinueTime - Time.realtimeSinceStartup);
        protected bool DelayIsComplete => DelayIsActiveSelf && Time.realtimeSinceStartup >= delayContinueTime;
        protected bool DelayIsProcessing => DelayIsActiveSelf && Time.realtimeSinceStartup < delayContinueTime;

        public override void Start()
        {
            if (init) return;
            base.Start();
            delayInfoKeyId = ((uint)(uint.MaxValue * UnityEngine.Random.value)).ToString();
            SendCustomEventDelayedFrames(nameof(_CachePlayerMovement), 2);
        }

        public void _CachePlayerMovement()
        {
            initPlayerWalk = localPlayer.GetWalkSpeed();
            initPlayerStrafe = localPlayer.GetStrafeSpeed();
            initPlayerRun = localPlayer.GetRunSpeed();
            initPlayerJump = localPlayer.GetJumpImpulse();
        }

        protected void HandleActions(
            ATTriggerActionType[] actionTypes,
            Object[] mainObjects,
            bool[] boolDatas,
            int[] intOrEnumDatas,
            float[] floatDatas,
            string[] stringDatas,
            string[] stringExtraDatas,
            Object[] referenceDatas,
            Vector4[] vectorDatas,
            VRCUrl[] urlDatas
        )
        {
            if (IsTraceEnabled) Trace($"Handling Actions from index {delayContinueIndex} (Current TID: {delayInfoKeyId} | Active TID: {localPlayer.GetPlayerTag(delayInfoKey)})`");
            for (int i = delayContinueIndex; i < actionTypes.Length; i++)
            {
                var actionType = actionTypes[i];
                var mainObj = mainObjects[i];
                var intOrEnumData = intOrEnumDatas[i];
                var boolData = boolDatas[i];
                var floatData = floatDatas[i];
                var stringData = stringDatas[i];
                var stringExtraData = stringExtraDatas[i];
                var refData = referenceDatas[i];
                var vectorData = vectorDatas[i];
                var urlData = urlDatas[i];

                switch (actionType)
                {
                    case ATTriggerActionType.DELAY:
                        if (IsTraceEnabled) Trace($"Delay: {floatData}s @ [{i}]");
                        doDelaySetup(floatData, i);
                        return;
                    case ATTriggerActionType.OBJECT_ENABLE:
                        doObjectToggle((GameObject)mainObj, (ATTriggerToggleAction)intOrEnumData);
                        break;
                    case ATTriggerActionType.OBJECT_TELEPORT:
                        doTransformTeleport((Transform)mainObj, intOrEnumData, (Transform)refData);
                        break;
                    case ATTriggerActionType.OBJECT_REPARENT:
                        doTransformReparent((Transform)mainObj, (Transform)refData, boolData);
                        break;
                    case ATTriggerActionType.PLAYER_TELEPORT:
                        doPlayerTeleport(vectorData, boolData);
                        break;
                    case ATTriggerActionType.PLAYER_TELEPORT_TO:
                        doPlayerTeleportTo((Transform)mainObj, boolData);
                        break;
                    case ATTriggerActionType.PLAYER_SPEED:
                        doPlayerSpeed(vectorData, boolData);
                        break;
                    case ATTriggerActionType.PLAYER_VELOCITY:
                        doPlayerVelocity(vectorData, boolData);
                        break;
                    case ATTriggerActionType.PLAYER_GRAVITY:
                        doPlayerGravity(floatData, boolData);
                        break;
                    case ATTriggerActionType.RESET_MOVEMENT:
                        doResetSpeed();
                        break;
                    case ATTriggerActionType.COLLIDER_ENABLE:
                        doColliderEnable((Collider)mainObj, (ATTriggerToggleAction)intOrEnumData);
                        break;
                    case ATTriggerActionType.COLLIDER_TRIGGER:
                        doColliderTrigger((Collider)mainObj, boolData);
                        break;
                    case ATTriggerActionType.COLLIDER_BOX_CENTER:
                    case ATTriggerActionType.COLLIDER_SPHERE_CENTER:
                    case ATTriggerActionType.COLLIDER_CAPSULE_CENTER:
                        doColliderCenter((Collider)mainObj, vectorData);
                        break;
                    case ATTriggerActionType.COLLIDER_BOX_SIZE:
                        doColliderBoxSize((BoxCollider)mainObj, vectorData);
                        break;
                    case ATTriggerActionType.COLLIDER_SPHERE_RADIUS:
                    case ATTriggerActionType.COLLIDER_CAPSULE_RADIUS:
                        doColliderRadius((Collider)mainObj, floatData);
                        break;
                    case ATTriggerActionType.COLLIDER_CAPSULE_HEIGHT:
                        doColliderHeight((Collider)mainObj, floatData);
                        break;
                    case ATTriggerActionType.UDON_ENABLE:
                        doUdonEnable((UdonBehaviour)mainObj, (ATTriggerToggleAction)intOrEnumData);
                        break;
                    case ATTriggerActionType.UDON_EVENT:
                        doUdonEvent((UdonBehaviour)mainObj, stringData);
                        break;
                    case ATTriggerActionType.UDON_VARIABLE:
                    case ATTriggerActionType.UDON_VARIABLE_PRIVATE:
                        doUdonVariable((UdonBehaviour)mainObj, stringData, boolData, intOrEnumData, floatData, stringExtraData, refData, vectorData, urlData);
                        break;
                    case ATTriggerActionType.ANIMATOR_ENABLE:
                        doAnimatorEnable((Animator)mainObj, (ATTriggerToggleAction)intOrEnumData);
                        break;
                    case ATTriggerActionType.ANIMATOR_PLAY:
                        doAnimatorPlay((Animator)mainObj, stringData, floatData, boolData);
                        break;
                    case ATTriggerActionType.ANIMATOR_CROSSFADE:
                        doAnimatorCrossFade((Animator)mainObj, stringData, floatData, boolData);
                        break;
                    case ATTriggerActionType.ANIMATOR_TRIGGER:
                        doAnimatorTrigger((Animator)mainObj, stringData, boolData);
                        break;
                    case ATTriggerActionType.ANIMATOR_BOOL:
                        doAnimatorBool((Animator)mainObj, stringData, boolData);
                        break;
                    case ATTriggerActionType.ANIMATOR_INT:
                        doAnimatorInteger((Animator)mainObj, stringData, intOrEnumData);
                        break;
                    case ATTriggerActionType.ANIMATOR_FLOAT:
                        doAnimatorFloat((Animator)mainObj, stringData, floatData);
                        break;
                    case ATTriggerActionType.AUDIO_ENABLE:
                        doAudioEnable((AudioSource)mainObj, (ATTriggerToggleAction)intOrEnumData);
                        break;
                    case ATTriggerActionType.AUDIO_ACTION:
                        doAudioAction((AudioSource)mainObj, (ATTriggerAudioAction)intOrEnumData);
                        break;
                    case ATTriggerActionType.AUDIO_OPTION:
                        doAudioOption((AudioSource)mainObj, (ATTriggerAudioOption)intOrEnumData, floatData);
                        break;
                    case ATTriggerActionType.AUDIO_CLIP_CHANGE:
                        doAudioClip((AudioSource)mainObj, refData);
                        break;
                    case ATTriggerActionType.AUDIO_CLIP_PLAY:
                        doAudioPlayClip((AudioSource)mainObj, refData);
                        break;
                    case ATTriggerActionType.PARTICLE_ACTION:
                        doParticleSystemAction((ParticleSystem)mainObj, (ATTriggerParticleAction)intOrEnumData);
                        break;
                    case ATTriggerActionType.TIMELINE_ENABLE:
                        doTimelineEnable((PlayableDirector)mainObj, (ATTriggerToggleAction)intOrEnumData);
                        break;
                    case ATTriggerActionType.TIMELINE_ACTION:
                        doTimelineAction((PlayableDirector)mainObj, (ATTriggerTimelineAction)intOrEnumData);
                        break;
                }
            }

            delayContinueIndex = 0;
            localPlayer.SetPlayerTag(delayInfoKey);
        }

        private void doDelaySetup(float delay, int currentIndex)
        {
            localPlayer.SetPlayerTag(delayInfoKey, delayInfoKeyId);
            delayContinueIndex = currentIndex + 1;
            delayContinueTime = Time.realtimeSinceStartup + delay;
        }

        private void doObjectToggle(GameObject obj, ATTriggerToggleAction action)
        {
            if (obj == null) return;
            switch (action)
            {
                case ATTriggerToggleAction.ENABLE:
                    obj.SetActive(true);
                    break;
                case ATTriggerToggleAction.DISABLE:
                    obj.SetActive(false);
                    break;
            }
        }

        private void doTransformTeleport(Transform t, int actionFlags, Transform data)
        {
            if (t == null) return;
            if (data == null) return;
            Vector3 position = t.position;
            Quaternion rotation = t.rotation;
            Vector3 scale = t.localScale;
            if ((actionFlags & (int)ATTriggerTeleportAction.POSITION) != 0) position = data.position;
            if ((actionFlags & (int)ATTriggerTeleportAction.ROTATION) != 0) rotation = data.rotation;
            if ((actionFlags & (int)ATTriggerTeleportAction.SCALE) != 0) scale = data.localScale;
            t.SetPositionAndRotation(position, rotation);
            t.localScale = scale;
        }

        private void doTransformReparent(Transform t, Transform parent, bool worldPositionStays)
        {
            if (t == null) return;
            if (parent == null) return;
            t.SetParent(parent, worldPositionStays);
        }

        private void doPlayerTeleport(Vector4 positionData, bool additive)
        {
            Vector3 position = positionData;
            if (additive)
            {
                var existingPos = localPlayer.GetPosition();
                position.x += existingPos.x;
                position.y += existingPos.y;
                position.z += existingPos.z;
            }

            localPlayer.TeleportTo(position, localPlayer.GetRotation(), VRC_SceneDescriptor.SpawnOrientation.AlignPlayerWithSpawnPoint);
        }

#pragma warning disable CS0219
        [SuppressMessage("ReSharper", "ConvertToConstant.Local")]
        private void doPlayerTeleportTo(Transform t, bool seamless)
        {
            if (t == null) return;
            /*
                Copyright (c) 2023 @Phasedragon on GitHub
                Additional help by @Nestorboy
                Permission is hereby granted, free of charge, to any person obtaining
                a copy of this software and associated documentation files (the
                "Software"), to deal in the Software without restriction, including
                without limitation the rights to use, copy, modify, merge, publish,
                distribute, sublicense, and/or sell copies of the Software, and to
                permit persons to whom the Software is furnished to do so, subject to
                the following conditions:
                The above copyright notice and this permission notice shall be
                included in all copies or substantial portions of the Software.

                THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
                EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
                MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
                NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
                LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
                OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
                WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
            */
            VRCPlayerApi player = localPlayer;
            Vector3 teleportPos = t.position;
            Quaternion teleportRot = t.rotation;
            teleportRot = Quaternion.Euler(0, teleportRot.eulerAngles.y, 0);
            // This code only runs in-game because it's broken in ClientSim
#if !UNITY_EDITOR
            var orientation = VRC_SceneDescriptor.SpawnOrientation.AlignRoomWithSpawnPoint;
            if (seamless)
            {
                Vector3 playerPos = player.GetPosition();
                Quaternion playerRot = player.GetRotation();
                Quaternion invPlayerRot = Quaternion.Inverse(playerRot);
                VRCPlayerApi.TrackingData origin = player.GetTrackingData(VRCPlayerApi.TrackingDataType.Origin);
                Vector3 originPos = origin.position;
                Quaternion originRot = origin.rotation;
                Vector3 offsetPos = originPos - playerPos;
                Quaternion offsetRot = invPlayerRot * originRot;
                teleportPos += teleportRot * invPlayerRot * offsetPos;
                teleportRot *= offsetRot;
            }
#else
            var orientation = VRC_SceneDescriptor.SpawnOrientation.AlignPlayerWithSpawnPoint;
#endif
            player.TeleportTo(teleportPos, teleportRot, orientation, false);
        }
#pragma warning restore CS0219

        private void doPlayerSpeed(Vector4 speedValues, bool additive)
        {
            var walk = speedValues.x;
            var strafe = speedValues.y;
            var run = speedValues.z;
            var jump = speedValues.w;
            if (additive)
            {
                walk += localPlayer.GetWalkSpeed();
                strafe += localPlayer.GetStrafeSpeed();
                run += localPlayer.GetRunSpeed();
                jump += localPlayer.GetJumpImpulse();
            }

            localPlayer.SetWalkSpeed(walk);
            localPlayer.SetStrafeSpeed(strafe);
            localPlayer.SetRunSpeed(run);
            localPlayer.SetJumpImpulse(jump);
        }

        private void doPlayerVelocity(Vector4 velocityValues, bool additive)
        {
            Vector3 velocity = velocityValues;
            if (additive)
            {
                var existingVelocity = localPlayer.GetVelocity();
                velocity.x += existingVelocity.x;
                velocity.y += existingVelocity.y;
                velocity.z += existingVelocity.z;
            }

            localPlayer.SetVelocity(velocity);
        }

        private void doPlayerGravity(float gravityValue, bool addititve)
        {
            if (addititve) gravityValue += localPlayer.GetGravityStrength();
            localPlayer.SetGravityStrength(gravityValue);
        }

        private void doResetSpeed()
        {
            localPlayer.SetWalkSpeed(initPlayerWalk);
            localPlayer.SetStrafeSpeed(initPlayerStrafe);
            localPlayer.SetRunSpeed(initPlayerRun);
            localPlayer.SetJumpImpulse(initPlayerJump);
            localPlayer.SetGravityStrength();
        }

        private void doColliderEnable(Collider col, ATTriggerToggleAction action)
        {
            if (col == null) return;
            switch (action)
            {
                case ATTriggerToggleAction.ENABLE:
                    col.enabled = true;
                    break;
                case ATTriggerToggleAction.DISABLE:
                    col.enabled = false;
                    break;
            }
        }

        private void doColliderTrigger(Collider col, bool isTrigger)
        {
            if (col == null) return;
            col.isTrigger = isTrigger;
        }

        private void doColliderCenter(Collider col, Vector3 center)
        {
            if (col == null) return;
            var type = col.GetType();
            if (type == typeof(BoxCollider)) ((BoxCollider)col).center = center;
            else if (type == typeof(SphereCollider)) ((SphereCollider)col).center = center;
            else if (type == typeof(CapsuleCollider)) ((CapsuleCollider)col).center = center;
        }

        private void doColliderBoxSize(Collider col, Vector3 size)
        {
            if (col == null) return;
            var type = col.GetType();
            if (type == typeof(BoxCollider)) ((BoxCollider)col).size = size;
        }

        private void doColliderRadius(Collider col, float radius)
        {
            if (col == null) return;
            var type = col.GetType();
            if (type == typeof(SphereCollider)) ((SphereCollider)col).radius = radius;
            else if (type == typeof(CapsuleCollider)) ((CapsuleCollider)col).radius = radius;
        }

        private void doColliderHeight(Collider col, float height)
        {
            if (col == null) return;
            var type = col.GetType();
            if (type == typeof(CapsuleCollider)) ((CapsuleCollider)col).height = height;
        }

        private void doUdonEnable(UdonBehaviour udonBehaviour, ATTriggerToggleAction action)
        {
            if (udonBehaviour == null) return;
            switch (action)
            {
                case ATTriggerToggleAction.ENABLE:
                    udonBehaviour.enabled = true;
                    break;
                case ATTriggerToggleAction.DISABLE:
                    udonBehaviour.enabled = false;
                    break;
            }
        }

        private void doUdonEvent(UdonBehaviour udonBehaviour, string action)
        {
            if (udonBehaviour == null) return;
            if (string.IsNullOrWhiteSpace(action)) return;
            udonBehaviour.SendCustomEvent(action);
        }

        private void doUdonVariable(UdonBehaviour udonBehaviour, string fieldData, bool flag, int optionOrNumber, float value, string text, UnityEngine.Object reference, Vector4 vector, VRCUrl url)
        {
            if (udonBehaviour == null) return;
            if (string.IsNullOrWhiteSpace(fieldData)) return;
            var varDataSplit = fieldData.Split(':', 2);
            string fieldName = varDataSplit[0];
            string fieldTypeKey = varDataSplit.Length > 1 ? varDataSplit[1] : "";

            switch (fieldTypeKey)
            {
                case "B":
                    udonBehaviour.SetProgramVariable(fieldName, flag);
                    break;
                case "I":
                case "E":
                    udonBehaviour.SetProgramVariable(fieldName, optionOrNumber);
                    break;
                case "F":
                    udonBehaviour.SetProgramVariable(fieldName, value);
                    break;
                case "S":
                    udonBehaviour.SetProgramVariable(fieldName, text);
                    break;
                case "O":
                    udonBehaviour.SetProgramVariable(fieldName, reference);
                    break;
                case "U":
                    udonBehaviour.SetProgramVariable(fieldName, url);
                    break;
                case "Y":
                    udonBehaviour.SetProgramVariable(fieldName, new Vector2(vector.x, vector.y));
                    break;
                case "Z":
                    udonBehaviour.SetProgramVariable(fieldName, new Vector3(vector.x, vector.y, vector.z));
                    break;
                case "W":
                    udonBehaviour.SetProgramVariable(fieldName, vector);
                    break;
            }
        }

        private void doAnimatorEnable(Animator animator, ATTriggerToggleAction action)
        {
            if (animator == null) return;
            switch (action)
            {
                case ATTriggerToggleAction.ENABLE:
                    animator.enabled = true;
                    break;
                case ATTriggerToggleAction.DISABLE:
                    animator.enabled = false;
                    break;
            }
        }

        private void doAnimatorPlay(Animator animator, string state, float time, bool inSeconds)
        {
            if (animator == null) return;
            if (string.IsNullOrWhiteSpace(state)) return;
            if (time < 0) time = 0;
            // state is stored as Layer.State
            if (inSeconds) animator.PlayInFixedTime(state, -1, time);
            else animator.Play(state, -1, time);
        }

        private void doAnimatorCrossFade(Animator animator, string state, float time, bool inSeconds)
        {
            if (animator == null) return;
            if (string.IsNullOrWhiteSpace(state)) return;
            if (time < 0) time = 0;
            // state is stored as Layer.State
            if (inSeconds) animator.CrossFadeInFixedTime(state, time);
            else animator.CrossFade(state, time);
        }

        private void doAnimatorTrigger(Animator animator, string parameter, bool reset)
        {
            if (animator == null) return;
            if (string.IsNullOrWhiteSpace(parameter)) return;
            if (reset) animator.ResetTrigger(parameter);
            else animator.SetTrigger(parameter);
        }

        private void doAnimatorBool(Animator animator, string parameter, bool data)
        {
            if (animator == null) return;
            if (string.IsNullOrWhiteSpace(parameter)) return;
            animator.SetBool(parameter, data);
        }

        private void doAnimatorInteger(Animator animator, string parameter, int data)
        {
            if (animator == null) return;
            if (string.IsNullOrWhiteSpace(parameter)) return;
            animator.SetInteger(parameter, data);
        }

        private void doAnimatorFloat(Animator animator, string parameter, float data)
        {
            if (animator == null) return;
            if (string.IsNullOrWhiteSpace(parameter)) return;
            animator.SetFloat(parameter, data);
        }

        private void doAudioEnable(AudioSource audioSource, ATTriggerToggleAction action)
        {
            if (audioSource == null) return;
            switch (action)
            {
                case ATTriggerToggleAction.ENABLE:
                    audioSource.enabled = true;
                    break;
                case ATTriggerToggleAction.DISABLE:
                    audioSource.enabled = false;
                    break;
            }
        }

        private void doAudioAction(AudioSource audioSource, ATTriggerAudioAction audioAction)
        {
            if (audioSource == null) return;
            switch (audioAction)
            {
                case ATTriggerAudioAction.MUTE:
                    audioSource.mute = true;
                    break;
                case ATTriggerAudioAction.UNMUTE:
                    audioSource.mute = false;
                    break;
                case ATTriggerAudioAction.PLAY:
                    audioSource.Play();
                    break;
                case ATTriggerAudioAction.PAUSE:
                    audioSource.Pause();
                    break;
                case ATTriggerAudioAction.UNPAUSE:
                    audioSource.UnPause();
                    break;
                case ATTriggerAudioAction.STOP:
                    audioSource.Stop();
                    break;
                case ATTriggerAudioAction.LOOP:
                    audioSource.loop = true;
                    break;
                case ATTriggerAudioAction.NOLOOP:
                    audioSource.loop = false;
                    break;
            }
        }

        private void doAudioOption(AudioSource audioSource, ATTriggerAudioOption audioOption, float data)
        {
            if (audioSource == null) return;
            switch (audioOption)
            {
                case ATTriggerAudioOption.VOLUME:
                    audioSource.volume = data;
                    break;
                case ATTriggerAudioOption.PITCH:
                    audioSource.pitch = data;
                    break;
                case ATTriggerAudioOption.TIME:
                    audioSource.time = data;
                    break;
                case ATTriggerAudioOption.STEREO_PAN:
                    audioSource.panStereo = data;
                    break;
                case ATTriggerAudioOption.SPATIAL_BLEND:
                    audioSource.spatialBlend = data;
                    break;
                case ATTriggerAudioOption.REVERB_MIX:
                    audioSource.reverbZoneMix = data;
                    break;
                case ATTriggerAudioOption.SPREAD:
                    audioSource.spread = data;
                    break;
                case ATTriggerAudioOption.DOPPLER:
                    audioSource.dopplerLevel = data;
                    break;
                case ATTriggerAudioOption.MIN_DIST:
                    audioSource.minDistance = data;
                    break;
                case ATTriggerAudioOption.MAX_DIST:
                    audioSource.maxDistance = data;
                    break;
                case ATTriggerAudioOption.PRIORITY:
                    audioSource.priority = (int)data;
                    break;
            }
        }

        private void doAudioClip(AudioSource audioSource, Object refData)
        {
            if (audioSource == null) return;
            if (refData == null) audioSource.clip = null;
            else audioSource.clip = (AudioClip)refData;
        }

        private void doAudioPlayClip(AudioSource audioSource, Object refData)
        {
            if (audioSource == null) return;
            audioSource.Stop();
            if (refData == null) audioSource.clip = null;
            else
            {
                audioSource.clip = (AudioClip)refData;
                audioSource.Play();
            }
        }

        private void doParticleSystemAction(ParticleSystem particles, ATTriggerParticleAction particleAction)
        {
            if (particles == null) return;
            switch (particleAction)
            {
                case ATTriggerParticleAction.PLAY:
                    particles.Play();
                    break;
                case ATTriggerParticleAction.PAUSE:
                    particles.Pause();
                    break;
                case ATTriggerParticleAction.STOP:
                    particles.Stop();
                    break;
                case ATTriggerParticleAction.CLEAR:
                    particles.Clear();
                    break;
            }
        }

        private void doTimelineEnable(PlayableDirector director, ATTriggerToggleAction action)
        {
            if (director == null) return;
            switch (action)
            {
                case ATTriggerToggleAction.ENABLE:
                    director.enabled = true;
                    break;
                case ATTriggerToggleAction.DISABLE:
                    director.enabled = false;
                    break;
            }
        }

        private void doTimelineAction(PlayableDirector director, ATTriggerTimelineAction timelineAction)
        {
            if (director == null) return;
            switch (timelineAction)
            {
                case ATTriggerTimelineAction.PLAY:
                    director.Play();
                    break;
                case ATTriggerTimelineAction.PAUSE:
                    director.Pause();
                    break;
                case ATTriggerTimelineAction.RESUME:
                    director.Resume();
                    break;
                case ATTriggerTimelineAction.STOP:
                    director.Stop();
                    break;
            }
        }
    }
}