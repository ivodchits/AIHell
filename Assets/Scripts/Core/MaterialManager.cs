using UnityEngine;
using System;
using System.Collections.Generic;
using AIHell.Core.Data;

public class MaterialManager : MonoBehaviour
{
    private Dictionary<string, Material> effectMaterials;
    private const string SHADER_PATH = "Hidden/";

    [System.Serializable]
    public class MaterialEffect
    {
        public string name;
        public AnimationCurve intensityCurve;
        public float duration;
        public float currentTime;
        public bool isActive;
    }

    private List<MaterialEffect> activeEffects;

    private void Awake()
    {
        InitializeMaterials();
    }

    private void OnDestroy()
    {
        CleanupMaterials();
    }

    private void InitializeMaterials()
    {
        effectMaterials = new Dictionary<string, Material>();
        activeEffects = new List<MaterialEffect>();

        // Initialize effect materials
        CreateEffectMaterial("ParanoiaEffect");
        CreateEffectMaterial("RealityBreak");
        CreateEffectMaterial("PsychologicalEffect");
        CreateEffectMaterial("TimeDistortion");
    }

    private void CleanupMaterials()
    {
        try
        {
            foreach (var material in effectMaterials.Values)
            {
                if (material != null)
                {
                    if (Application.isPlaying)
                        Destroy(material);
                    else
                        DestroyImmediate(material);
                }
            }
            effectMaterials.Clear();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error cleaning up materials: {ex.Message}");
        }
    }

    private void CreateEffectMaterial(string shaderName)
    {
        try
        {
            Shader shader = Shader.Find(SHADER_PATH + shaderName);
            if (shader != null && shader.isSupported)
            {
                Material material = new Material(shader);
                if (material != null)
                {
                    effectMaterials[shaderName] = material;
                    return;
                }
            }
            Debug.LogError($"Failed to create material for shader: {shaderName}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error creating effect material {shaderName}: {ex.Message}");
        }
    }

    public Material GetEffectMaterial(string effectName)
    {
        if (effectMaterials.TryGetValue(effectName, out Material material))
        {
            return material;
        }
        return null;
    }

    public void ApplyPsychologicalEffect(string effectName, float intensity, float duration)
    {
        try
        {
            Material material = GetEffectMaterial(effectName);
            if (material == null)
            {
                Debug.LogWarning($"Material not found for effect: {effectName}");
                return;
            }

            var effect = new MaterialEffect
            {
                name = effectName,
                intensityCurve = AnimationCurve.EaseInOut(0, 0, 1, intensity),
                duration = Mathf.Max(0.1f, duration), // Ensure minimum duration
                currentTime = 0,
                isActive = true
            };

            activeEffects.Add(effect);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error applying psychological effect: {ex.Message}");
        }
    }

    private void Update()
    {
        UpdateActiveEffects();
    }

    private void UpdateActiveEffects()
    {
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            var effect = activeEffects[i];
            if (effect.isActive)
            {
                effect.currentTime += Time.deltaTime;
                float normalizedTime = effect.currentTime / effect.duration;

                if (normalizedTime >= 1f)
                {
                    ResetEffect(effect);
                    activeEffects.RemoveAt(i);
                }
                else
                {
                    UpdateEffectParameters(effect, normalizedTime);
                }
            }
        }
    }

    private void UpdateEffectParameters(MaterialEffect effect, float normalizedTime)
    {
        Material material = GetEffectMaterial(effect.name);
        if (material == null) return;

        float intensity = effect.intensityCurve.Evaluate(normalizedTime);

        switch (effect.name)
        {
            case "ParanoiaEffect":
                UpdateParanoiaEffect(material, intensity);
                break;
            case "RealityBreak":
                UpdateRealityBreakEffect(material, intensity);
                break;
            case "PsychologicalEffect":
                UpdatePsychologicalEffect(material, intensity);
                break;
            case "TimeDistortion":
                UpdateTimeDistortionEffect(material, intensity);
                break;
        }
    }

    private void UpdateParanoiaEffect(Material material, float intensity)
    {
        material.SetFloat("_Intensity", intensity);
        material.SetFloat("_DistortionAmount", 0.2f * intensity);
        material.SetFloat("_PulseSpeed", 1f + intensity);
        material.SetFloat("_EdgeIntensity", 0.5f * intensity);
    }

    private void UpdateRealityBreakEffect(Material material, float intensity)
    {
        material.SetFloat("_RealityBreak", intensity);
        material.SetFloat("_GlitchAmount", 0.3f * intensity);
        material.SetFloat("_ChromaticAberration", 0.02f * intensity);
        material.SetFloat("_WarpIntensity", 0.5f * intensity);
    }

    private void UpdatePsychologicalEffect(Material material, float intensity)
    {
        material.SetFloat("_EffectIntensity", intensity);
        material.SetFloat("_UneaseFactor", 0.5f * intensity);
        material.SetFloat("_ShadowIntensity", 0.3f * intensity);
        material.SetFloat("_PulseRate", 1f + (intensity * 0.5f));
    }

    private void UpdateTimeDistortionEffect(Material material, float intensity)
    {
        material.SetFloat("_TimeDistortion", intensity);
        material.SetFloat("_WaveAmplitude", 0.1f * intensity);
        material.SetFloat("_TimeEchoStrength", 0.5f * intensity);
        material.SetFloat("_TimeslipIntensity", 0.3f * intensity);
    }

    private void ResetEffect(MaterialEffect effect)
    {
        Material material = GetEffectMaterial(effect.name);
        if (material == null) return;

        switch (effect.name)
        {
            case "ParanoiaEffect":
                material.SetFloat("_Intensity", 0);
                material.SetFloat("_DistortionAmount", 0);
                break;
            case "RealityBreak":
                material.SetFloat("_RealityBreak", 0);
                material.SetFloat("_GlitchAmount", 0);
                break;
            case "PsychologicalEffect":
                material.SetFloat("_EffectIntensity", 0);
                material.SetFloat("_UneaseFactor", 0);
                break;
            case "TimeDistortion":
                material.SetFloat("_TimeDistortion", 0);
                material.SetFloat("_WaveAmplitude", 0);
                break;
        }
    }

    public void TriggerPsychologicalManifestation(PlayerAnalysisProfile profile)
    {
        // Choose effect based on psychological state
        string effectName = DetermineEffectFromProfile(profile);
        float intensity = CalculateEffectIntensity(profile);
        float duration = CalculateEffectDuration(profile);

        ApplyPsychologicalEffect(effectName, intensity, duration);
    }

    private string DetermineEffectFromProfile(PlayerAnalysisProfile profile)
    {
        if (profile.FearLevel > 0.7f)
            return "ParanoiaEffect";
        if (profile.ObsessionLevel > 0.7f)
            return "RealityBreak";
        if (profile.AggressionLevel > 0.6f)
            return "PsychologicalEffect";
        return "TimeDistortion";
    }

    private float CalculateEffectIntensity(PlayerAnalysisProfile profile)
    {
        return Mathf.Max(
            profile.FearLevel,
            profile.ObsessionLevel,
            profile.AggressionLevel
        );
    }

    private float CalculateEffectDuration(PlayerAnalysisProfile profile)
    {
        // Base duration modified by psychological state
        float baseDuration = 3f;
        float psychologicalFactor = (profile.FearLevel + profile.ObsessionLevel) / 2f;
        return baseDuration * (1f + psychologicalFactor);
    }
}