using System.Security.Cryptography;
using System.Text;
using UdonSharp;
using UnityEditor;
using UnityEngine;

namespace ArchiTech.ProTV.Editor
{
    public static class ProTVEditorPrefs
    {
        private static string ProjectKeyPrefix;

        public const string PreviewCustomTextures = "PreviewCustomTextures";
        public const string SkipTextureContaminationPrompt = "SkipTextureContaminationPrompt";

        [InitializeOnLoadMethod]
        public static void Init()
        {
            var pathHash = GetMd5Hash(Application.dataPath);
            ProjectKeyPrefix = $"ProTV-{pathHash}-";
        }

        [MenuItem("Tools/ProTV/Misc/Reset ProTV Preferences")]
        private static void ResetPreferences()
        {
            DeleteKey(PreviewCustomTextures);
            DeleteKey(SkipTextureContaminationPrompt);
        }

        // Lazily sourced from https://medium.com/@altaf.navalur/editorprefs-in-unity-22a0bf39732b

        private static string GetMd5Hash(string input)
        {
            MD5 md5 = MD5.Create();
            byte[] data = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
            StringBuilder sb = new StringBuilder();
            foreach (var t in data) sb.Append(t.ToString("x2"));
            return sb.ToString();
        }

        public static string GetKey(string key)
        {
            return ProjectKeyPrefix + key;
        }

        public static bool HasKey(string key)
        {
            return EditorPrefs.HasKey(ProjectKeyPrefix + key);
        }

        public static string GetString(string key)
        {
            return EditorPrefs.GetString(ProjectKeyPrefix + key);
        }

        public static string GetString(string key, string defaultValue)
        {
            return EditorPrefs.GetString(ProjectKeyPrefix + key, defaultValue);
        }

        public static void SetString(string key, string value)
        {
            EditorPrefs.SetString(ProjectKeyPrefix + key, value);
        }

        public static bool GetBool(string key)
        {
            return EditorPrefs.GetBool(ProjectKeyPrefix + key);
        }

        public static bool GetBool(string key, bool defaultValue)
        {
            return HasKey(key) ? EditorPrefs.GetBool(ProjectKeyPrefix + key) : defaultValue;
        }

        public static void SetBool(string key, bool value)
        {
            EditorPrefs.SetBool(ProjectKeyPrefix + key, value);
        }

        public static int GetInt(string key)
        {
            return EditorPrefs.GetInt(ProjectKeyPrefix + key);
        }

        public static int GetInt(string key, int defaultValue)
        {
            return HasKey(key) ? EditorPrefs.GetInt(ProjectKeyPrefix + key) : defaultValue;
        }

        public static void SetInt(string key, int value)
        {
            EditorPrefs.SetInt(ProjectKeyPrefix + key, value);
        }

        public static float GetFloat(string key)
        {
            return EditorPrefs.GetFloat(ProjectKeyPrefix + key);
        }

        public static float GetFloat(string key, float defaultValue)
        {
            return HasKey(key) ? EditorPrefs.GetFloat(ProjectKeyPrefix + key) : defaultValue;
        }

        public static void SetFloat(string key, float value)
        {
            EditorPrefs.SetFloat(ProjectKeyPrefix + key, value);
        }

        public static void DeleteKey(string key)
        {
            EditorPrefs.DeleteKey(ProjectKeyPrefix + key);
        }
    }
}