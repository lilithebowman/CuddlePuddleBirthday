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
    private float _lastSliderValue = -1f;

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

        if (slider != null)
        {
            _lastSliderValue = slider.value;
            ApplySliderValue(_lastSliderValue);
        }
    }

    private void Update()
    {
        if (!_initialized || slider == null) return;

        float v = slider.value;
        // Only react when it actually changes
        if (Mathf.Abs(v - _lastSliderValue) > 0.0001f)
        {
            _lastSliderValue = v;
            ApplySliderValue(v);
        }
    }

    private void ApplySliderValue(float value)
    {
        if (lamps == null) return;

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

    // Keeping this in case you ever want to call it manually
    public void OnSliderChanged(float value)
    {
        ApplySliderValue(value);
    }
}
