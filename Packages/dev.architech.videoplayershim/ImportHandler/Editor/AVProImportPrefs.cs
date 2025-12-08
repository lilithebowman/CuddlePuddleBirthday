using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace ArchiTech.VideoPlayerShim.ImportHandler
{
    public static class AVProImportPrefs
    {
        private const string prefsStoragePath = "ProjectSettings/AVProImportPrefs.asset";
        private static Dictionary<string, object> data = null;

        private static void Load()
        {
            data = new Dictionary<string, object>();
            if (!File.Exists(prefsStoragePath)) return;
            var json = File.ReadAllText(prefsStoragePath);
            data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
        }

        private static void Save()
        {
            if (data == null) return;
            var json = JsonConvert.SerializeObject(data);
            File.WriteAllText(prefsStoragePath, json);
        }

        public static bool HasKey(string key)
        {
            if (data == null) Load();
            return data?.ContainsKey(key) ?? false;
        }

        public static void DeleteAll()
        {
            if (data == null) Load();
            data?.Clear();
            Save();
        }

        public static void DeleteKey(string key)
        {
            if (HasKey(key)) data.Remove(key);
        }

        public static bool GetBool(string key, bool defaultValue = false)
        {
            if (!HasKey(key)) return defaultValue;
            return data[key] is bool val ? val : defaultValue;
        }

        public static void SetBool(string key, bool val)
        {
            if (data == null) Load();
            data[key] = val;
            Save();
        }

        public static System.Int32 GetInt(string key, int defaultValue = 0)
        {
            if (!HasKey(key)) return defaultValue;
            // newtonsoft implicitly loads as int64, so downgrade to int
            if (data[key] is Int64) data[key] = Convert.ToInt32(data[key]);
            return data[key] is int val ? val : defaultValue;
        }


        public static void SetInt(string key, int val)
        {
            if (data == null) Load();
            data[key] = val;
            Save();
        }

        public static float GetFloat(string key, float defaultValue = 0f)
        {
            if (!HasKey(key)) return defaultValue;
            // newtonsoft implicitly loads as double, so downgrade to float
            if (data[key] is double) data[key] = Convert.ToSingle(data[key]);
            return data[key] is float val ? val : defaultValue;
        }

        public static void SetFloat(string key, float val)
        {
            if (data == null) Load();
            data[key] = val;
            Save();
        }

        public static string GetString(string key, string defaultValue = "")
        {
            if (!HasKey(key)) return defaultValue;
            return data[key] as string ?? defaultValue;
        }

        public static void SetString(string key, string val)
        {
            if (data == null) Load();
            data[key] = val;
            Save();
        }
    }
}