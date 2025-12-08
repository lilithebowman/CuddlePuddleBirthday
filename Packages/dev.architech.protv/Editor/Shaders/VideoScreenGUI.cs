using System;
using UnityEngine;

namespace UnityEditor
{
    public class VideoScreenGUI : ShaderGUI
    {
        private MaterialProperty _GIBrightness;
        
        private void FindProperties(MaterialProperty[] props)
        {
            _GIBrightness = FindProperty(nameof(_GIBrightness), props);
        }
        
        public override void OnGUI (MaterialEditor materialEditor, MaterialProperty[] props)
        {
            FindProperties(props);
            base.OnGUI (materialEditor, props);

            if (_GIBrightness != null)
            {
                Material[] materials = Array.ConvertAll(materialEditor.targets, o => (Material)o);
                var flags = _GIBrightness.floatValue != 0 ? MaterialGlobalIlluminationFlags.RealtimeEmissive : MaterialGlobalIlluminationFlags.None;
                foreach (var mat in materials) mat.globalIlluminationFlags = flags;
                using (new EditorGUI.DisabledScope(true)) materialEditor.LightmapEmissionFlagsProperty(0, true, true);
            }
        }
    }
}