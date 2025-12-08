using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace ArchiTech.SDK
{
    [System.AttributeUsage(System.AttributeTargets.All, AllowMultiple = true)]
    public class I18nTooltipAttribute : TooltipAttribute
    {
        public readonly SystemLanguage lang;

        public I18nTooltipAttribute(string tooltip) : base(null)
        {
            lang = SystemLanguage.English;
            // handle readonly parent class field
            GetType().GetField("tooltip", BindingFlags.Public | BindingFlags.Instance)?.SetValue(this, tooltip);
        }

        public I18nTooltipAttribute(SystemLanguage lang, string tooltip) : base(null)
        {
            this.lang = lang;
            // handle readonly parent class field
            GetType().GetField("tooltip", BindingFlags.Public | BindingFlags.Instance)?.SetValue(this, tooltip);
        }

        // TODO: Try moving text into a file and using cursed reflection for setting the tooltip
        // Cache the translations in a static global and access via key
        // maybe disallow multiple a cache all variations of a particular key

        // public void GetTooltip(SystemLanguage lang)
        // {

        // }

        // public void GetLabel(SystemLanguage lang)
        // {

        // }
    }
}