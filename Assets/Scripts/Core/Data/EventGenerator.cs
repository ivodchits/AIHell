using UnityEngine;
using System.Collections.Generic;
using AIHell.Core.Data;

public class EventGenerator
{
    private static readonly Dictionary<PsychologicalEffect.EffectType, string[]> EffectSpecificEvents = new Dictionary<PsychologicalEffect.EffectType, string[]>
    {
        { PsychologicalEffect.EffectType.ParanoiaInduction, new[] {
            "You hear footsteps matching your own, but slightly out of sync.",
            "Your reflection in a surface seems to move a fraction of a second too late.",
            "The shadows in the room appear to be cast from impossible angles.",
            "You distinctly feel eyes watching you, but cannot locate their source."
        }},
        { PsychologicalEffect.EffectType.TimeDistortion, new[] {
            "The clock on the wall ticks backwards for three beats, then forwards for two.",
            "You watch dust particles freeze in mid-air, suspended in an impossible moment.",
            "Your own movements seem to leave lingering afterimages in the air.",
            "Time feels thick here, like moving through cooling glass."
        }},
        { PsychologicalEffect.EffectType.RoomDistortion, new[] {
            "The walls pulse slowly, like the breathing of some vast creature.",
            "The room's geometry shifts subtly when viewed from different angles.",
            "Furniture casts shadows that don't match their physical forms.",
            "The ceiling seems to rise and fall with your breathing."
        }},
        { PsychologicalEffect.EffectType.Hallucination, new[] {
            "Writing appears on the wall, spelling out words you'd rather not read.",
            "Objects in your peripheral vision change shape when directly observed.",
            "You hear whispers that seem to respond to your thoughts.",
            "The air shimmers with half-formed figures that disperse when noticed."
        }},
        { PsychologicalEffect.EffectType.MemoryAlter, new[] {
            "You recognize this room, but the memory feels impossible.",
            "Personal items you've never owned before lie scattered about.",
            "Photos on the wall show memories you both do and don't remember.",
            "You find notes written in your handwriting that you never wrote."
        }}
    };

    private static readonly string[] ObsessionEvents = {
        "You can't stop thinking about {0}, it's everywhere you look.",
        "The word '{0}' seems to repeat endlessly in your mind.",
        "Every surface seems to reflect your obsession with {0}.",
        "You see patterns of {0} forming in the mundane details of the room."
    };

    private static readonly string[] HighFearEvents = {
        "Your heart races as shadows dart between the corners of your vision.",
        "The air grows thick with the weight of your mounting dread.",
        "Every sound seems to carry a hint of threat.",
        "Your muscles tense involuntarily, preparing for flight."
    };

    public static GameEvent GenerateEventForProfile(Room room, PlayerAnalysisProfile profile)
    {
        string eventDescription;
        string soundEffect = null;
        GameEvent.EventType eventType;

        // Check for active obsessions first
        var obsessions = profile.GetActiveObsessions();
        if (obsessions.Length > 0 && Random.value < 0.4f)
        {
            var obsession = obsessions[Random.Range(0, obsessions.Length)];
            eventDescription = string.Format(
                ObsessionEvents[Random.Range(0, ObsessionEvents.Length)],
                obsession.Keyword
            );
            eventType = GameEvent.EventType.Psychological;
            soundEffect = "obsessionWhisper";
        }
        // High fear level events
        else if (profile.FearLevel > 0.7f && Random.value < 0.5f)
        {
            eventDescription = HighFearEvents[Random.Range(0, HighFearEvents.Length)];
            eventType = GameEvent.EventType.Psychological;
            soundEffect = "heartbeat";
        }
        // Effect-specific events
        else
        {
            var effectType = DetermineEffectType(profile);
            var events = EffectSpecificEvents[effectType];
            eventDescription = events[Random.Range(0, events.Length)];
            eventType = GameEvent.EventType.Atmospheric;
            soundEffect = GetSoundEffectForEventType(effectType);
        }

        return new GameEvent(
            $"evt_{room.RoomID}_{System.Guid.NewGuid().ToString().Substring(0, 8)}",
            eventType,
            eventDescription,
            soundEffect,
            Random.Range(0.5f, 2f)
        );
    }

    private static PsychologicalEffect.EffectType DetermineEffectType(PlayerAnalysisProfile profile)
    {
        if (profile.FearLevel > 0.6f)
            return PsychologicalEffect.EffectType.ParanoiaInduction;
        if (profile.ObsessionLevel > 0.7f)
            return PsychologicalEffect.EffectType.Hallucination;
        if (profile.CuriosityLevel > 0.8f)
            return PsychologicalEffect.EffectType.RoomDistortion;
        if (profile.AggressionLevel > 0.7f)
            return PsychologicalEffect.EffectType.MemoryAlter;
        
        return PsychologicalEffect.EffectType.TimeDistortion;
    }

    private static string GetSoundEffectForEventType(PsychologicalEffect.EffectType effectType)
    {
        return effectType switch
        {
            PsychologicalEffect.EffectType.ParanoiaInduction => "footsteps",
            PsychologicalEffect.EffectType.TimeDistortion => "clockDistortion",
            PsychologicalEffect.EffectType.RoomDistortion => "spatialDistortion",
            PsychologicalEffect.EffectType.Hallucination => "whispers",
            PsychologicalEffect.EffectType.MemoryAlter => "memoryEcho",
            _ => "ambient"
        };
    }
}