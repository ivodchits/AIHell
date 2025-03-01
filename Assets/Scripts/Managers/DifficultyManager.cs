using UnityEngine;
using System.Collections.Generic;
using AIHell.Core.Data;

public class DifficultyManager : MonoBehaviour
{
    [System.Serializable]
    public class DifficultyParameters
    {
        public float eventFrequency = 0.3f;
        public float effectIntensity = 0.5f;
        public float roomComplexity = 1f;
        public float psychologicalPressure = 1f;
    }

    private DifficultyParameters currentParameters;
    private float baseEventFrequency = 0.3f;
    private float adaptationRate = 0.1f;
    private Dictionary<string, float> psychologicalThresholds;

    private void Awake()
    {
        InitializeDifficulty();
    }

    private void InitializeDifficulty()
    {
        currentParameters = new DifficultyParameters();
        psychologicalThresholds = new Dictionary<string, float>
        {
            { "fear", 0.7f },
            { "obsession", 0.6f },
            { "aggression", 0.5f },
            { "curiosity", 0.4f }
        };
    }

    public void UpdateDifficulty(PlayerAnalysisProfile profile, int currentLevel)
    {
        // Base difficulty scaling with level
        float levelScale = 1f + (currentLevel * 0.2f);

        // Adjust parameters based on psychological state
        AdjustEventFrequency(profile, levelScale);
        AdjustEffectIntensity(profile, levelScale);
        AdjustRoomComplexity(profile, levelScale);
        AdjustPsychologicalPressure(profile, levelScale);

        // Apply adaptive difficulty
        ApplyAdaptiveDifficulty(profile);
    }

    private void AdjustEventFrequency(PlayerAnalysisProfile profile, float levelScale)
    {
        float baseFreq = baseEventFrequency * levelScale;
        
        // Increase frequency based on psychological state
        if (profile.FearLevel < psychologicalThresholds["fear"])
        {
            baseFreq *= 1.2f; // More events if fear is low
        }
        
        if (profile.ObsessionLevel > psychologicalThresholds["obsession"])
        {
            baseFreq *= 1.3f; // More events for obsessive players
        }

        currentParameters.eventFrequency = Mathf.Clamp(baseFreq, 0.1f, 0.8f);
    }

    private void AdjustEffectIntensity(PlayerAnalysisProfile profile, float levelScale)
    {
        float baseIntensity = 0.5f * levelScale;
        
        // Scale intensity based on psychological state
        float psychologicalMultiplier = 1f;
        if (profile.FearLevel > psychologicalThresholds["fear"])
        {
            psychologicalMultiplier += 0.3f;
        }
        if (profile.ObsessionLevel > psychologicalThresholds["obsession"])
        {
            psychologicalMultiplier += 0.2f;
        }

        currentParameters.effectIntensity = Mathf.Clamp(baseIntensity * psychologicalMultiplier, 0.3f, 1f);
    }

    private void AdjustRoomComplexity(PlayerAnalysisProfile profile, float levelScale)
    {
        float baseComplexity = 1f * levelScale;
        
        // Adjust complexity based on curiosity and aggression
        if (profile.CuriosityLevel > psychologicalThresholds["curiosity"])
        {
            baseComplexity *= 1.2f; // More complex for curious players
        }
        if (profile.AggressionLevel > psychologicalThresholds["aggression"])
        {
            baseComplexity *= 0.8f; // Less complex for aggressive players
        }

        currentParameters.roomComplexity = Mathf.Clamp(baseComplexity, 0.5f, 2f);
    }

    private void AdjustPsychologicalPressure(PlayerAnalysisProfile profile, float levelScale)
    {
        float basePressure = 1f * levelScale;
        
        // Calculate psychological resistance
        float resistance = CalculatePsychologicalResistance(profile);
        
        // Adjust pressure inversely to resistance
        basePressure *= (2f - resistance);

        currentParameters.psychologicalPressure = Mathf.Clamp(basePressure, 0.5f, 2f);
    }

    private float CalculatePsychologicalResistance(PlayerAnalysisProfile profile)
    {
        // Higher resistance means player is less affected by psychological elements
        float resistance = 0f;
        
        if (profile.FearLevel < psychologicalThresholds["fear"])
            resistance += 0.3f;
        
        if (profile.ObsessionLevel < psychologicalThresholds["obsession"])
            resistance += 0.2f;
        
        if (profile.AggressionLevel > psychologicalThresholds["aggression"])
            resistance += 0.2f;

        return Mathf.Clamp01(resistance);
    }

    private void ApplyAdaptiveDifficulty(PlayerAnalysisProfile profile)
    {
        // Gradually adjust difficulty based on player's psychological state
        float targetDifficulty = CalculateTargetDifficulty(profile);
        float currentDifficulty = (currentParameters.eventFrequency + 
                                 currentParameters.effectIntensity + 
                                 currentParameters.psychologicalPressure) / 3f;

        float adjustment = (targetDifficulty - currentDifficulty) * adaptationRate;

        // Apply adjustment to all parameters
        currentParameters.eventFrequency = Mathf.Clamp(
            currentParameters.eventFrequency + adjustment, 0.1f, 0.8f);
        currentParameters.effectIntensity = Mathf.Clamp(
            currentParameters.effectIntensity + adjustment, 0.3f, 1f);
        currentParameters.psychologicalPressure = Mathf.Clamp(
            currentParameters.psychologicalPressure + adjustment, 0.5f, 2f);
    }

    private float CalculateTargetDifficulty(PlayerAnalysisProfile profile)
    {
        // Calculate target difficulty based on player's psychological resilience
        float targetDifficulty = 0.5f; // Base difficulty

        // Adjust based on psychological metrics
        if (profile.FearLevel < 0.3f)
            targetDifficulty += 0.2f; // Increase difficulty for less fearful players
        else if (profile.FearLevel > 0.8f)
            targetDifficulty -= 0.1f; // Decrease for very fearful players

        if (profile.ObsessionLevel > 0.7f)
            targetDifficulty += 0.15f; // Increase for obsessive players

        if (profile.AggressionLevel > 0.6f)
            targetDifficulty += 0.1f; // Increase for aggressive players

        return Mathf.Clamp01(targetDifficulty);
    }

    public DifficultyParameters GetCurrentParameters()
    {
        return currentParameters;
    }

    public void ResetDifficulty()
    {
        currentParameters = new DifficultyParameters();
    }
}