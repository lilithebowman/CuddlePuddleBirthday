using System.Reflection;
using UnityEngine;

namespace ArchiTech.SDK
{
    [System.AttributeUsage(System.AttributeTargets.All, AllowMultiple = true)]
    public class I18nInspectorNameAttribute : InspectorNameAttribute
    {
        public readonly SystemLanguage lang;
        
        public I18nInspectorNameAttribute(string displayName) : base(null)
        {
            lang = SystemLanguage.English;
            // handle readonly parent class field
            GetType().GetField("displayName", BindingFlags.Public | BindingFlags.Instance)?.SetValue(this, displayName);
        }

        public I18nInspectorNameAttribute(SystemLanguage lang, string displayName) : base(null)
        {
            this.lang = lang;
            // handle readonly parent class field
            GetType().GetField("displayName", BindingFlags.Public | BindingFlags.Instance)?.SetValue(this, displayName);
        }
    }
}