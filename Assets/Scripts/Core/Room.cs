using UnityEngine;
using System;
using System.Collections.Generic;
using AIHell.Core.Data;

public class Room
{
    public string RoomID { get; private set; }
    public string Archetype { get; private set; }
    public string BaseDescription { get; set; }
    public string CurrentDescription { get; set; }
    public Dictionary<string, string> Exits { get; private set; }
    public List<GameEvent> Events { get; private set; }
    public List<PsychologicalEffect> ActiveEffects { get; private set; }
    public bool IsVisited { get; set; }
    public float PsychologicalIntensity { get; set; }
    
    public Room(string roomID, string archetype)
    {
        RoomID = roomID;
        Archetype = archetype;
        Exits = new Dictionary<string, string>();
        Events = new List<GameEvent>();
        ActiveEffects = new List<PsychologicalEffect>();
        IsVisited = false;
        PsychologicalIntensity = 0f;
    }

    public void SetDescription(string description)
    {
        BaseDescription = description;
        CurrentDescription = description;
        UpdateDescription();
    }

    private void UpdateDescription()
    {
        CurrentDescription = BaseDescription;
        foreach (var effect in ActiveEffects)
        {
            CurrentDescription = effect.ModifyDescription(CurrentDescription);
        }
    }

    public void AddEvent(GameEvent evt)
    {
        if (evt != null)
        {
            Events.Add(evt);
            UpdatePsychologicalIntensity();
        }
    }

    public void AddEffect(PsychologicalEffect effect)
    {
        if (effect != null)
        {
            ActiveEffects.Add(effect);
            UpdatePsychologicalIntensity();
        }
    }

    public void RemoveEffect(PsychologicalEffect effect)
    {
        if (effect != null)
        {
            ActiveEffects.Remove(effect);
            UpdatePsychologicalIntensity();
        }
    }

    private void UpdatePsychologicalIntensity()
    {
        float eventIntensity = 0f;
        float effectIntensity = 0f;

        // Calculate event-based intensity
        foreach (var evt in Events)
        {
            eventIntensity = Mathf.Max(eventIntensity, evt.Intensity);
        }

        // Calculate effect-based intensity
        foreach (var effect in ActiveEffects)
        {
            effectIntensity = Mathf.Max(effectIntensity, effect.Intensity);
        }

        // Combine intensities with higher weight on active effects
        PsychologicalIntensity = Mathf.Clamp01((eventIntensity * 0.4f) + (effectIntensity * 0.6f));
    }

    public void ConnectTo(string direction, string targetRoomID)
    {
        if (!string.IsNullOrEmpty(direction) && !string.IsNullOrEmpty(targetRoomID))
        {
            Exits[direction.ToLower()] = targetRoomID;
        }
    }

    public bool HasExit(string direction)
    {
        return Exits.ContainsKey(direction.ToLower());
    }

    public string GetConnectedRoom(string direction)
    {
        return Exits.TryGetValue(direction.ToLower(), out string roomID) ? roomID : null;
    }

    public void OnPlayerEnter(PlayerAnalysisProfile profile)
    {
        IsVisited = true;

        // Trigger any untriggered events
        foreach (var evt in Events)
        {
            if (!evt.HasTriggered)
            {
                evt.Trigger(this);
            }
        }

        // Apply psychological effects
        foreach (var effect in ActiveEffects)
        {
            effect.Apply(this, profile);
        }

        // Update room description with current effects
        UpdateDescription();
    }

    public List<GameEvent> GetActiveEvents()
    {
        return Events.FindAll(e => !e.HasTriggered);
    }

    public void UpdateState(PlayerAnalysisProfile profile)
    {
        bool needsDescriptionUpdate = false;

        // Update active effects
        for (int i = ActiveEffects.Count - 1; i >= 0; i--)
        {
            var effect = ActiveEffects[i];
            if (!effect.IsActive)
            {
                ActiveEffects.RemoveAt(i);
                needsDescriptionUpdate = true;
            }
            else
            {
                effect.Update(profile);
            }
        }

        if (needsDescriptionUpdate)
        {
            UpdateDescription();
        }

        UpdatePsychologicalIntensity();
    }
}