using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using System.Runtime.CompilerServices;
using VRC.Udon;

#if VUDON_LOGGER
using Varneon.VUdon.Logger.Abstract;
#endif

[assembly: InternalsVisibleTo("ArchiTech.SDK.Editor")]

namespace ArchiTech.SDK
{
    public enum ATLogLevel
    {
        [I18nInspectorName("Always")] ALWAYS,
        [I18nInspectorName("Error")] ERROR,
        [I18nInspectorName("Warn")] WARN,
        [I18nInspectorName("Info")] INFO,
        [I18nInspectorName("Debug")] DEBUG,
        [I18nInspectorName("Trace")] TRACE
    }

    /// <seealso cref="UdonSharpBehaviour"/>
    public abstract class ATBehaviour : UdonSharpBehaviour
    {
        protected readonly Vector3 V3ONE = Vector3.one;
        protected readonly Vector3 V3HALF = Vector3.one * 0.5f;
        protected readonly Vector3 V3ZERO = Vector3.zero;
        protected readonly Vector3 V3NEGHALF = Vector3.one * -0.5f;
        protected readonly Vector3 V3NEGONE = -Vector3.one;
        protected readonly Vector3 V3FORWARD = Vector3.forward;
        protected readonly Vector3 V3UP = Vector3.up;
        protected readonly Vector3 V3RIGHT = Vector3.right;
        protected readonly Vector3 V3INF = Vector3.positiveInfinity;
        protected const float INF = float.PositiveInfinity;
        protected const float EPSILON = float.Epsilon;
        protected const string EMPTYSTR = "";

        [SerializeField, Tooltip("The granularity at which this component will allow logging. Eg: If set to INFO, TRACE and DEBUG outputs will be skipped.")]
        protected internal ATLogLevel _maxLogLevel = ATLogLevel.DEBUG;

        public virtual ATLogLevel LoggingLevel
        {
            get => _maxLogLevel;
            set
            {
                _maxLogLevel = value;
                updateEnabledFlags();
            }
        }

        [SerializeField, InspectorName("Custom Logger"), Tooltip("Reference to some implementation of the UdonLogger class from VUdon")]
#if VUDON_LOGGER
        internal UdonLogger _logger;
#else
        internal UdonSharpBehaviour _logger;
#endif

#if VUDON_LOGGER
        public UdonLogger Logger
#else
         public UdonSharpBehaviour Logger
#endif
        {
            get => _logger;
            set
            {
                _logger = value;
                hasLogger = _logger != null;
            }
        }


        /// <summary>
        /// Cached value of <c>Networking.LocalPlayer</c>
        /// </summary>
        protected VRCPlayerApi localPlayer;

        /// <summary>
        /// Flag for whether <c>localPlayer</c> is valid and not null.
        /// </summary>
        protected bool hasLocalPlayer;

        private bool hasLogger;

        /// <summary>
        /// Simple getter which returns a null-safe check on whether the localPlayer is the owner of the gameobject the component is attached to
        /// </summary>
        public bool IsOwner
        {
            get => hasLocalPlayer && Networking.IsOwner(gameObject);
        }

        /// <summary>
        /// Simple getter and setter for handling the owner of the current game object.
        /// Setter will implicitly check if the given player already owns the object.
        /// </summary>
        public VRCPlayerApi Owner
        {
            get => Networking.GetOwner(gameObject);
            set
            {
                var owned = Networking.IsOwner(gameObject);
                if (!owned) Networking.SetOwner(value, gameObject);
            }
        }

        /// <summary>
        /// Simple getter which returns a null-safe check on whether the localPlayer is the master of the current world instance
        /// </summary>
        protected bool IsMaster
        {
            get => hasLocalPlayer && localPlayer.isMaster;
        }

        /// <summary>
        /// Simple getter which returns a null-safe check on whether the localPlayer is the owner of the current world instance
        /// </summary>
        protected bool IsInstanceOwner
        {
            get => hasLocalPlayer && localPlayer.isInstanceOwner;
        }

#if UNITY_STANDALONE
        protected const bool isPC = true;
#else
        protected const bool isPC = false;
#endif
#if UNITY_ANDROID
        protected const bool isAndroid = true;
#else
        protected const bool isAndroid = false;
#endif
#if UNITY_IOS
        protected const bool isIOS = true;
#else
        protected const bool isIOS = false;
#endif
        /// <summary>
        /// isQuest will also return true for the Pico platform or any Android-based VR build.
        /// </summary>
        protected bool isQuest = false;

        protected bool isInVR = false;
        protected bool init = false;
        protected bool IsTraceEnabled = false;
        protected bool IsDebugEnabled = false;
        protected bool IsInfoEnabled = false;
        protected bool IsWarnEnabled = false;
        protected bool IsErrorEnabled = false;

        // Logging settings
        /// <summary>
        /// A custom branding prefix for logging that any sub-class can override.
        /// </summary>
        protected virtual string LogPrefixHeader => "<color=#1F84A9>A</color><color=#A3A3A3>T</color>";

        private const string logPrefixFormat = "[{2} {0}\t<color={3}>{4} ({5})</color>] {1}";
        private const string logPrefixImportantFormat = "[{2}\t<color={3}>{4} ({5})</color>] {1}";

        private readonly string[] logLevelPrefix =
        {
            $"<color=#008000>{nameof(ATLogLevel.ALWAYS)}</color>",
            $"<color=#ff0000>{nameof(ATLogLevel.ERROR)}</color> ",
            $"<color=#ffff00>{nameof(ATLogLevel.WARN)}</color>    ",
            $"<color=#c0c0c0>{nameof(ATLogLevel.INFO)}</color>    ",
            $"<color=#ffa500>{nameof(ATLogLevel.DEBUG)}</color> ",
            $"<color=#ff00ff>{nameof(ATLogLevel.TRACE)}</color> "
        };

        private readonly string[] _formatArgs = new string[6];
        private bool logInit = false;

        /// <summary><para>
        /// Method for initializing any logic in the behaviour.
        /// <example>
        /// Can be overridden in child classes like so:
        /// <code>
        /// public override void Start()
        /// {
        ///     if (init) return;
        ///     base.Start();
        ///     // Rest of the init logic goes here
        /// }
        /// </code>
        /// </example>
        /// </para></summary>
        public virtual void Start()
        {
            if (init) return;
            init = true;
            localPlayer = Networking.LocalPlayer;
            hasLocalPlayer = VRC.SDKBase.Utilities.IsValid(localPlayer);
#if VUDON_LOGGER
            hasLogger = _logger != null;
#endif
            if (hasLocalPlayer) isInVR = localPlayer.IsUserInVR();
            if (isAndroid) isQuest = isInVR;
            _formatArgs[2] = LogPrefixHeader;
            if (_formatArgs[3] == null) _formatArgs[3] = "#ffaa66";
            var typeNameParts = GetUdonTypeName().Split('.');
            _formatArgs[4] = typeNameParts[typeNameParts.Length - 1];
            if (_formatArgs[5] == null) _formatArgs[5] = gameObject.name;
            updateEnabledFlags();
        }

        /// <summary>
        /// General purpose logging method. Checks defined log level and logs the given message depending on the desired log level.
        /// If the desired level granularity is more than the level defined on this component, it will skip the output.
        /// </summary>
        /// <param name="level">the desired log level</param>
        /// <param name="message">the string to output for the log</param>
        protected void Log(ATLogLevel level, string message)
        {
            if (!logInit) updateEnabledFlags();
            // use explicit int casting due to https://github.com/vrchat-community/UdonSharp/issues/68
            if ((int)level > (int)LoggingLevel) return;
            _formatArgs[0] = logLevelPrefix[(int)level];
            _formatArgs[1] = message;
            if (level == ATLogLevel.ERROR)
            {
#if VUDON_LOGGER
                if (_logger) _logger.LogErrorFormat(logPrefixImportantFormat, _formatArgs);
#endif
                UnityEngine.Debug.LogErrorFormat(logPrefixImportantFormat, _formatArgs);
            }
            else if (level == ATLogLevel.WARN)
            {
#if VUDON_LOGGER
                if (_logger) _logger.LogWarningFormat(logPrefixImportantFormat, _formatArgs);
#endif
                UnityEngine.Debug.LogWarningFormat(logPrefixImportantFormat, _formatArgs);
            }
            else
            {
#if VUDON_LOGGER
                if (_logger) _logger.LogFormat(logPrefixFormat, _formatArgs);
#endif
                UnityEngine.Debug.LogFormat(logPrefixFormat, _formatArgs);
            }
        }

        protected void LogRaw(string message)
        {
            UnityEngine.Debug.Log(message);
#if VUDON_LOGGER
            if (_logger) _logger.Log(message);
#endif
        }

        protected void LogFormatRaw(string format, params object[] args)
        {
            UnityEngine.Debug.LogFormat(format, args);
#if VUDON_LOGGER
            if (_logger) _logger.LogFormat(format, args);
#endif
        }

        /// <summary>
        /// Convenience method for logging specifically a trace
        /// </summary>
        /// <param name="message">the string to output for the log</param>
        protected void Trace(string message)
        {
            if (!init) updateEnabledFlags();
            if (IsTraceEnabled) Log(ATLogLevel.TRACE, message);
        }

        /// <summary>
        /// Convenience method for logging specifically a debug message
        /// </summary>
        /// <param name="message">the string to output for the log</param>
        protected void Debug(string message)
        {
            if (!init) updateEnabledFlags();
            if (IsDebugEnabled) Log(ATLogLevel.DEBUG, message);
        }

        /// <summary>
        /// Convenience method for logging specifically an info message
        /// </summary>
        /// <param name="message">the string to output for the log</param>
        protected void Info(string message)
        {
            if (!init) updateEnabledFlags();
            if (IsInfoEnabled) Log(ATLogLevel.INFO, message);
        }

        /// <summary>
        /// Convenience method for logging specifically a warning message
        /// </summary>
        /// <param name="message">the string to output for the log</param>
        protected void Warn(string message)
        {
            if (!init) updateEnabledFlags();
            if (IsWarnEnabled) Log(ATLogLevel.WARN, message);
        }

        /// <summary>
        /// Convenience method for logging specifically an error message
        /// </summary>
        /// <param name="message">the string to output for the log</param>
        protected void Error(string message)
        {
            if (!init) updateEnabledFlags();
            if (IsErrorEnabled) Log(ATLogLevel.ERROR, message);
        }

        /// <summary>
        /// This updates the internal color for the component prefix portion of the logging output.
        /// </summary>
        /// <param name="hexColor">desired color to use</param>
        protected void SetLogPrefixColor(string hexColor) => _formatArgs[3] = hexColor;

        /// <summary>
        /// This updates the internal string for the component prefix portion of the logging output.
        /// Typically is some sort of reference to the gameobject the component exists on.
        /// </summary>
        /// <param name="label">desired string to use</param>
        protected void SetLogPrefixLabel(string label) => _formatArgs[5] = label;

        protected void updateEnabledFlags()
        {
            logInit = true;
            // use explicit int casting due to https://github.com/vrchat-community/UdonSharp/issues/68
            IsTraceEnabled = (int)_maxLogLevel >= (int)ATLogLevel.TRACE;
            IsDebugEnabled = (int)_maxLogLevel >= (int)ATLogLevel.DEBUG;
            IsInfoEnabled = (int)_maxLogLevel >= (int)ATLogLevel.INFO;
            IsWarnEnabled = (int)_maxLogLevel >= (int)ATLogLevel.WARN;
            IsErrorEnabled = (int)_maxLogLevel >= (int)ATLogLevel.ERROR;
        }
    }
}