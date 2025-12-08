using System;
using System.Diagnostics;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ArchiTech.SDK
{
    [Serializable]
    internal class I18nTranslations
    {
        public I18nMessage[] messages = new I18nMessage[0];
    }
    
    [Serializable]
    internal class I18nMessage
    {
        public string id;
        public I18nOption[] options = new I18nOption[0];
    }

    [Serializable]
    internal class I18nOption
    {
        public string language;
        public string text;
    }
    
    
    public static class I18n
    {
        public static SystemLanguage Language;
        
        private static I18nTranslations _translations;

        public static string Tr(string input) => Tr(input, 0);

        public static string Tr(string input, int depth)
        {
            return input; // TODO actual translation implementation
            // bool missingTranslationEntry = false;
            // if (missingTranslationEntry)
            // {
            //     var frame = new StackTrace(true).GetFrame(depth + 1);
            //     UnityEngine.Debug.Log($"Unregistered translation text: \"<a href=\"{frame.GetFileName()}\" line=\"{frame.GetFileLineNumber()}\">{input}</a>\"");
            // }
        }

        public static GUIContent TrContent(string text) => TrContent(text, 0);
        public static GUIContent TrContent(string text, string tooltip) => TrContent(text, tooltip, 0);

        public static GUIContent TrContent(string text, int depth) => new GUIContent(Tr(text, depth + 1));
        public static GUIContent TrContent(string text, string tooltip, int depth) => new GUIContent(Tr(text, depth + 1), Tr(tooltip, depth + 1));
        
        
        
        #if UNITY_EDITOR && !COMPILER_UDONSHARP
        

        [InitializeOnLoadMethod]
        private static void loadTranslations()
        {
            Language = Application.systemLanguage;
            var json = Resources.Load<TextAsset>("i18n.json");
            if (json == null) return;
            _translations = JsonUtility.FromJson<I18nTranslations>(json.text);

        }

        [MenuItem("CONTEXT/ATBehaviour/Language/English")]
        private static void SwitchToEnglish(MenuCommand command) => I18n.Language = SystemLanguage.English;

        [MenuItem("CONTEXT/ATBehaviour/Language/French")]
        private static void SwitchToFrench(MenuCommand command) => I18n.Language = SystemLanguage.French;
        
        #endif
    }
}