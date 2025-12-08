using ArchiTech.SDK;
using UnityEngine;
using VRC.SDKBase;

namespace ArchiTech.ProTV
{
    [AddComponentMenu("")]
    public class PlaylistConfig : MonoBehaviour, IEditorOnly
    {
#if !COMPILER_UDONSHARP
        // Hide in edit mode
        void OnValidate()
        {
            hideFlags = HideFlags.HideInInspector | HideFlags.DontSaveInBuild;
        }

        // Reset is called when the component is added in the editor
        void Reset()
        {
            hideFlags = HideFlags.HideInInspector | HideFlags.DontSaveInBuild;
        }

        // Hide in play mode
        void Awake()
        {
            hideFlags = HideFlags.HideInInspector;
        }
#endif

        [SerializeField] internal PlaylistImportMode importMode;

        [SerializeField,
         I18nInspectorName("Import From")
        ]
        internal string importUrl;

        [SerializeField,
         I18nInspectorName("Import Path")
        ]
        internal string importPath;

        [SerializeField,
         I18nInspectorName("Import Source File")
        ]
        internal TextAsset importSrc;

        [SerializeField,
         I18nInspectorName("Auto-update on Build"), I18nTooltip("Set whether the build script should automatically attempt to download and import the remote playlist.")
        ]
        internal bool autoUpdateRemote;

        [SerializeField,
         I18nInspectorName("Autofill Alternate Urls")
        ]
        internal bool autofillAltURL;

        [SerializeField,
         I18nInspectorName("Autofill URL Escape")
        ]
        internal bool autofillEscape;

        [SerializeField,
         I18nInspectorName("Autofill Format")
        ]
        internal string autofillFormat = "$URL";
    }
}