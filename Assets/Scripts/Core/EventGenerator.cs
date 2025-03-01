using UnityEngine;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AIHell.Core.Data;

public static class EventGenerator
{
    public static GameEvent GenerateEventForProfile(Room room, PlayerAnalysisProfile profile)
    {
        try
        {
            string eventType = DetermineEventType(profile);
            float intensity = CalculateEventIntensity(profile);
            string description = GenerateEventDescription(eventType, intensity);

            return new GameEvent(
                id: $"evt_{System.Guid.NewGuid():N}",
                type: eventType,
                description: description,
                intensity: intensity,
                triggers: GenerateEventTriggers(eventType)
            );
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error generating event: {ex.Message}");
            return GenerateFallbackEvent();
        }
    }

    private static string DetermineEventType(PlayerAnalysisProfile profile)
    {
        // Choose event type based on dominant psychological state
        if (profile.FearLevel > profile.ObsessionLevel && profile.FearLevel > profile.AggressionLevel)
            return "paranoia";
        if (profile.ObsessionLevel > profile.AggressionLevel)
            return "obsession";
        if (profile.AggressionLevel > 0.6f)
            return "aggression";
        return "psychological";
    }

    private static float CalculateEventIntensity(PlayerAnalysisProfile profile)
    {
        // Base intensity on highest psychological factor
        float baseIntensity = Mathf.Max(
            profile.FearLevel,
            profile.ObsessionLevel,
            profile.AggressionLevel
        );

        // Add random variation
        float variation = UnityEngine.Random.Range(-0.1f, 0.1f);
        return Mathf.Clamp01(baseIntensity + variation);
    }

    private static string GenerateEventDescription(string eventType, float intensity)
    {
        switch (eventType.ToLower())
        {
            case "paranoia":
                return GenerateParanoiaDescription(intensity);
            case "obsession":
                return GenerateObsessionDescription(intensity);
            case "aggression":
                return GenerateAggressionDescription(intensity);
            default:
                return GeneratePsychologicalDescription(intensity);
        }
    }

    private static string GenerateParanoiaDescription(float intensity)
    {
        string[] descriptions = {
            "Shadows seem to move at the corner of your vision...",
            "You feel watched by unseen eyes...",
            "The walls appear to pulse with malevolent intent...",
            "Whispers echo from impossible directions..."
        };
        return descriptions[UnityEngine.Random.Range(0, descriptions.Length)];
    }

    private static string GenerateObsessionDescription(float intensity)
    {
        string[] descriptions = {
            "Patterns in the architecture demand your attention...",
            "Numbers and symbols start to form meaningful sequences...",
            "The room's geometry seems to hold hidden significance...",
            "Your thoughts begin to loop in ritualistic patterns..."
        };
        return descriptions[UnityEngine.Random.Range(0, descriptions.Length)];
    }

    private static string GenerateAggressionDescription(float intensity)
    {
        string[] descriptions = {
            "Your muscles tense with unexplained hostility...",
            "The urge to destroy something grows stronger...",
            "Violence seems to permeate the air...",
            "Your reflection shows a darker version of yourself..."
        };
        return descriptions[UnityEngine.Random.Range(0, descriptions.Length)];
    }

    private static string GeneratePsychologicalDescription(float intensity)
    {
        string[] descriptions = {
            "Reality seems to shift subtly...",
            "Time feels distorted and uncertain...",
            "Your perception begins to waver...",
            "The boundary between real and unreal blurs..."
        };
        return descriptions[UnityEngine.Random.Range(0, descriptions.Length)];
    }

    private static string[] GenerateEventTriggers(string eventType)
    {
        switch (eventType.ToLower())
        {
            case "paranoia":
                return new[] { "look", "investigate", "hide", "run" };
            case "obsession":
                return new[] { "examine", "touch", "count", "repeat" };
            case "aggression":
                return new[] { "break", "hit", "destroy", "attack" };
            default:
                return new[] { "observe", "wait", "think", "feel" };
        }
    }

    private static GameEvent GenerateFallbackEvent()
    {
        return new GameEvent(
            id: $"fallback_{System.Guid.NewGuid():N}",
            type: "psychological",
            description: "A strange feeling washes over you...",
            intensity: 0.5f,
            triggers: new[] { "observe", "wait" }
        );
    }

    public static async Task<GameEvent> GenerateContextualEvent(Room room, PlayerAnalysisProfile profile, string context)
    {
        try
        {
            // Get LLM-generated event description
            string description = await GameManager.Instance.LLMManager.GenerateEventDescription(context, profile);
            
            return new GameEvent(
                id: $"ctx_{System.Guid.NewGuid():N}",
                type: DetermineEventType(profile),
                description: description,
                intensity: CalculateEventIntensity(profile),
                triggers: GenerateEventTriggers(DetermineEventType(profile))
            );
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error generating contextual event: {ex.Message}");
            return GenerateFallbackEvent();
        }
    }
}