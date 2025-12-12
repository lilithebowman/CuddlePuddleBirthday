using System.Collections.Generic;
using UnityEditor;

namespace jp.lilxyzw.lilpbr
{
    internal class LILFoldoutSaver : ScriptableSingleton<LILFoldoutSaver>
    {
        public List<string> openedFoldout = new();

        public static bool IsOpened(string key) => instance.openedFoldout.Contains(key);

        public static void Open(string key)
        {
            if(!IsOpened(key)) instance.openedFoldout.Add(key);
        }

        public static void Close(string key)
        {
            if(IsOpened(key)) instance.openedFoldout.Remove(key);
        }
    }
}
