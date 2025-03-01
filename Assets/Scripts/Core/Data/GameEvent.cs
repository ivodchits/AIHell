using System;
using UnityEngine;

public class GameEvent
{
    public string EventID { get; private set; }
    public string Type { get; private set; }
    public string Description { get; set; }
    public bool HasTriggered { get; private set; }
    public string SoundEffectName { get; set; }
    public float DelayBeforeSound { get; set; }
    public float Intensity { get; private set; }
    public string[] Triggers { get; private set; }

    private Action<Room> onTrigger;

    public GameEvent(string id, string type, string description, float intensity, string[] triggers, string soundEffect = null, float soundDelay = 0f, Action<Room> onTrigger = null)
    {
        EventID = id;
        Type = type;
        Description = description;
        Intensity = intensity;
        Triggers = triggers;
        SoundEffectName = soundEffect;
        DelayBeforeSound = soundDelay;
        this.onTrigger = onTrigger;
        HasTriggered = false;
    }

    // Support for legacy constructor
    public GameEvent(string eventID, EventType legacyType, string description, string soundEffect = null, float soundDelay = 0f, Action<Room> onTrigger = null)
        : this(eventID, legacyType.ToString(), description, 0.5f, new[] { "observe" }, soundEffect, soundDelay, onTrigger)
    {
    }

    public void Trigger(Room room)
    {
        if (!HasTriggered)
        {
            // Display the event text with appropriate formatting
            GameManager.Instance.UIManager.DisplayEventMessage(Description);

            // Play sound effect if specified
            if (!string.IsNullOrEmpty(SoundEffectName))
            {
                if (DelayBeforeSound > 0)
                {
                    TriggerDelayedSound();
                }
                else
                {
                    GameAudioManager.Instance.PlaySound(SoundEffectName);
                }
            }

            // Execute any additional event logic
            onTrigger?.Invoke(room);

            // Update psychological profile based on event type
            UpdatePlayerProfile();

            HasTriggered = true;
        }
    }

    private async void TriggerDelayedSound()
    {
        await System.Threading.Tasks.Task.Delay((int)(DelayBeforeSound * 1000));
        if (!string.IsNullOrEmpty(SoundEffectName))
        {
            GameAudioManager.Instance.PlaySound(SoundEffectName);
        }
    }

    private void UpdatePlayerProfile()
    {
        var profileManager = GameManager.Instance.ProfileManager;
        
        switch (Type.ToLower())
        {
            case "psychological":
                profileManager.TrackChoice("psychological_event", Description);
                break;
            case "paranoia":
                profileManager.TrackChoice("paranoia_event", Description);
                UpdateFearLevel();
                break;
            case "obsession":
                profileManager.TrackChoice("obsession_event", Description);
                UpdateObsessionLevel();
                break;
            case "aggression":
                profileManager.TrackChoice("aggression_event", Description);
                UpdateAggressionLevel();
                break;
            default:
                profileManager.TrackChoice("general_event", Description);
                break;
        }
    }

    private void UpdateFearLevel()
    {
        var profile = GameManager.Instance.ProfileManager.CurrentProfile;
        profile.FearLevel = Mathf.Clamp01(profile.FearLevel + (Intensity * 0.2f));
    }

    private void UpdateObsessionLevel()
    {
        var profile = GameManager.Instance.ProfileManager.CurrentProfile;
        profile.ObsessionLevel = Mathf.Clamp01(profile.ObsessionLevel + (Intensity * 0.15f));
    }

    private void UpdateAggressionLevel()
    {
        var profile = GameManager.Instance.ProfileManager.CurrentProfile;
        profile.AggressionLevel = Mathf.Clamp01(profile.AggressionLevel + (Intensity * 0.1f));
    }
}