using System.Collections.Generic;
using UnityEngine;

public class Room
{
    public string RoomID { get; private set; }
    public string DescriptionText { get; set; }
    public string Archetype { get; private set; }
    public Dictionary<string, string> Exits { get; private set; }
    public List<GameEvent> Events { get; private set; }
    public List<Character> Characters { get; private set; }
    public List<string> Keywords { get; set; }
    
    private List<PsychologicalEffect> activeEffects;
    private string baseDescription;
    private bool isDistorted;

    public Room(string roomID, string archetype)
    {
        RoomID = roomID;
        Archetype = archetype;
        Exits = new Dictionary<string, string>();
        Events = new List<GameEvent>();
        Characters = new List<Character>();
        Keywords = new List<string>();
        activeEffects = new List<PsychologicalEffect>();
        isDistorted = false;
    }

    public void SetDescription(string description)
    {
        baseDescription = description;
        DescriptionText = description;
    }

    public void AddEffect(PsychologicalEffect effect)
    {
        activeEffects.Add(effect);
        ApplyEffects();
    }

    public void RemoveEffect(PsychologicalEffect effect)
    {
        activeEffects.Remove(effect);
        ResetAndApplyEffects();
    }

    private void ApplyEffects()
    {
        if (activeEffects.Count > 0)
        {
            // Reset to base description first
            DescriptionText = baseDescription;

            // Apply all active effects in order
            foreach (var effect in activeEffects)
            {
                effect.Apply(this, GameManager.Instance.ProfileManager.CurrentProfile);
            }
            isDistorted = true;
        }
    }

    private void ResetAndApplyEffects()
    {
        DescriptionText = baseDescription;
        isDistorted = false;
        ApplyEffects();
    }

    public void AddExit(string direction, string targetRoomID)
    {
        if (!Exits.ContainsKey(direction))
        {
            Exits.Add(direction, targetRoomID);
        }
    }

    public void AddEvent(GameEvent gameEvent)
    {
        Events.Add(gameEvent);
    }

    public void AddCharacter(Character character)
    {
        Characters.Add(character);
    }

    public List<string> GetAvailableExits()
    {
        return new List<string>(Exits.Keys);
    }

    public void TriggerEvents()
    {
        foreach (var gameEvent in Events.ToArray())
        {
            gameEvent.Trigger(this);
        }

        // Chance to apply psychological effects based on player profile
        TryApplyRandomPsychologicalEffect();
    }

    private void TryApplyRandomPsychologicalEffect()
    {
        var profile = GameManager.Instance.ProfileManager.CurrentProfile;
        float effectChance = 0.2f + (profile.FearLevel * 0.3f);

        if (!isDistorted && Random.value < effectChance)
        {
            PsychologicalEffect effect = GenerateRandomEffect();
            AddEffect(effect);
        }
    }

    private PsychologicalEffect GenerateRandomEffect()
    {
        var effectType = (PsychologicalEffect.EffectType)Random.Range(0, 
            System.Enum.GetValues(typeof(PsychologicalEffect.EffectType)).Length);
        
        float intensity = Random.Range(0.3f, 0.8f);
        string description = "The room begins to shift...";
        
        return new PsychologicalEffect(effectType, intensity, description, "distortionSound");
    }
}