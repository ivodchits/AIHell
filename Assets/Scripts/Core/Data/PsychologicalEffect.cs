using UnityEngine;
using System;
using AIHell.Core.Data;

public class PsychologicalEffect
{
    public enum EffectType
    {
        RoomDistortion,
        MemoryAlter,
        Hallucination,
        ParanoiaInduction,
        TimeDistortion
    }

    public EffectType Type { get; private set; }
    public float Intensity { get; private set; }
    public string Description { get; private set; }
    public bool IsPermanent { get; private set; }
    public string AssociatedSound { get; private set; }
    public bool IsActive { get; private set; }
    public float Duration { get; private set; }
    public float ElapsedTime { get; private set; }

    private Action<Room> effectAction;
    private Func<PlayerAnalysisProfile, float> intensityModifier;

    public PsychologicalEffect(EffectType type, float intensity, string description, string trigger = null, bool permanent = false, float duration = 0f)
    {
        Type = type;
        Intensity = intensity;
        Description = description;
        IsPermanent = permanent;
        AssociatedSound = trigger;
        IsActive = true;
        Duration = duration;
        ElapsedTime = 0f;
        
        SetupEffectBehavior();
    }

    private void SetupEffectBehavior()
    {
        switch (Type)
        {
            case EffectType.RoomDistortion:
                effectAction = (room) => {
                    string distortion = GenerateDistortionText(room.DescriptionText);
                    room.DescriptionText = distortion;
                };
                intensityModifier = (profile) => Intensity * (1 + profile.FearLevel);
                break;

            case EffectType.ParanoiaInduction:
                effectAction = (room) => {
                    AddParanoiaElements(room);
                };
                intensityModifier = (profile) => Intensity * (1 + profile.AggressionLevel);
                break;

            case EffectType.TimeDistortion:
                effectAction = (room) => {
                    AddTimeDistortionElements(room);
                };
                intensityModifier = (profile) => Intensity;
                break;

            // Add more effect types as needed
        }
    }

    public string ModifyDescription(string originalDescription)
    {
        if (!IsActive) return originalDescription;

        switch (Type)
        {
            case EffectType.RoomDistortion:
                return GenerateDistortionText(originalDescription);
            case EffectType.ParanoiaInduction:
                return AddParanoiaElements(originalDescription);
            case EffectType.TimeDistortion:
                return AddTimeDistortionElements(originalDescription);
            default:
                return originalDescription;
        }
    }

    public void Apply(Room room, PlayerAnalysisProfile profile)
    {
        if (!IsActive) return;

        try
        {
            float modifiedIntensity = CalculateModifiedIntensity(profile);
            
            if (!string.IsNullOrEmpty(AssociatedSound))
            {
                GameAudioManager.Instance.PlaySound(AssociatedSound);
            }

            effectAction?.Invoke(room);
            
            // Apply psychological impact
            ApplyPsychologicalImpact(profile);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error applying psychological effect: {ex.Message}");
        }
    }

    public void Update(PlayerAnalysisProfile profile)
    {
        if (!IsActive || IsPermanent) return;

        ElapsedTime += Time.deltaTime;
        
        if (Duration > 0 && ElapsedTime >= Duration)
        {
            IsActive = false;
            return;
        }

        // Update intensity based on time
        float timeRatio = Duration > 0 ? ElapsedTime / Duration : 0;
        Intensity = Mathf.Lerp(Intensity, 0, timeRatio);
    }

    private float CalculateModifiedIntensity(PlayerAnalysisProfile profile)
    {
        return intensityModifier?.Invoke(profile) ?? Intensity;
    }

    private void ApplyPsychologicalImpact(PlayerAnalysisProfile profile)
    {
        switch (Type)
        {
            case EffectType.ParanoiaInduction:
                profile.FearLevel = Mathf.Clamp01(profile.FearLevel + (Intensity * 0.1f));
                break;
            case EffectType.RoomDistortion:
                profile.ObsessionLevel = Mathf.Clamp01(profile.ObsessionLevel + (Intensity * 0.1f));
                break;
            case EffectType.TimeDistortion:
                profile.FearLevel = Mathf.Clamp01(profile.FearLevel + (Intensity * 0.05f));
                profile.ObsessionLevel = Mathf.Clamp01(profile.ObsessionLevel + (Intensity * 0.05f));
                break;
        }
    }

    private string GenerateDistortionText(string originalText)
    {
        if (Intensity <= 0.3f)
            return originalText + "\nSomething feels slightly off about this room.";
            
        string distorted = originalText;
        
        if (Intensity > 0.7f)
        {
            distorted = distorted.Replace("wall", "membrane")
                               .Replace("door", "portal")
                               .Replace("window", "void")
                               .Replace("ceiling", "sky")
                               .Replace("floor", "ground");
            distorted += "\nReality itself seems to warp and twist here.";
        }
        else
        {
            distorted += "\nThe geometry of this space feels increasingly wrong.";
        }

        return distorted;
    }

    private string AddParanoiaElements(string originalText)
    {
        string[] paranoiaElements = {
            "Did something just move in the corner?",
            "You can't shake the feeling of being watched.",
            "The shadows seem to follow your movements.",
            "You hear whispers, but can't make out the words.",
            "Every reflection shows something slightly... different."
        };

        int count = Mathf.CeilToInt(Intensity * 2);
        string modified = originalText;

        for (int i = 0; i < count && i < paranoiaElements.Length; i++)
        {
            modified += $"\n{paranoiaElements[UnityEngine.Random.Range(0, paranoiaElements.Length)]}";
        }

        return modified;
    }

    private string AddTimeDistortionElements(string originalText)
    {
        string[] timeDistortions = {
            "Time seems to flow differently here.",
            "Your watch shows impossible times.",
            "Seconds stretch into eternities.",
            "The air feels thick with temporal displacement.",
            "Past and future blur together in this space."
        };

        int count = Mathf.CeilToInt(Intensity * 2);
        string modified = originalText;

        for (int i = 0; i < count && i < timeDistortions.Length; i++)
        {
            modified += $"\n{timeDistortions[UnityEngine.Random.Range(0, timeDistortions.Length)]}";
        }

        return modified;
    }
}