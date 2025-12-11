using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.Udon;

public class LocalLampControl : UdonSharpBehaviour
{
    public Light[] lamps;
    public Slider slider;        // 0..1
    public float maxIntensity = 1.5f;

    private bool _initialized;
    private float[] _baseIntensities;

    private void Start()
    {
        if (lamps == null || lamps.Length == 0) return;

        _baseIntensities = new float[lamps.Length];
        for (int i = 0; i < lamps.Length; i++)
        {
            if (lamps[i] != null)
                _baseIntensities[i] = lamps[i].intensity;
        }

        _initialized = true;

        // Optional: apply initial slider value at start
        if (slider != null)
        {
            OnSliderChanged(slider.value);
        }
    }

    // This is called from the Slider's OnValueChanged event in the Inspector
    public void OnSliderChanged(float value)
    {
        if (!_initialized || lamps == null) return;

        for (int i = 0; i < lamps.Length; i++)
        {
            Light l = lamps[i];
            if (l == null) continue;

            if (value <= 0.001f)
            {
                l.enabled = false; // fully off for performance
            }
            else
            {
                if (!l.enabled) l.enabled = true;
                float baseIntensity = (_baseIntensities != null && i < _baseIntensities.Length)
                    ? _baseIntensities[i]
                    : maxIntensity;

                l.intensity = Mathf.Lerp(0f, baseIntensity, value);
            }
        }
    }
}
