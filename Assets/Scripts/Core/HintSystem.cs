using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using AIHell.Core.Data;

public class HintSystem : MonoBehaviour
{
    [System.Serializable]
    public class PsychologicalHint
    {
        public string hintId;
        public string hintText;
        public float psychologicalThreshold;
        public string triggerCondition;
        public bool wasShown;
        public float relevanceScore;
        public string[] associatedSymbols;

        public PsychologicalHint(string id, string text, float threshold)
        {
            hintId = id;
            hintText = text;
            psychologicalThreshold = threshold;
            wasShown = false;
            relevanceScore = 0f;
        }
    }

    private List<PsychologicalHint> availableHints;
    private Queue<string> recentHints;
    private const int MAX_RECENT_HINTS = 5;
    private float hintCooldown = 30f; // Seconds between hints
    private float lastHintTime;

    private void Awake()
    {
        InitializeHintSystem();
    }

    private void InitializeHintSystem()
    {
        availableHints = new List<PsychologicalHint>();
        recentHints = new Queue<string>();
        InitializeBaseHints();
    }

    private void InitializeBaseHints()
    {
        // Fear-based hints
        AddHint(new PsychologicalHint(
            "fear_escape",
            "The exit might not always be obvious... sometimes fear itself can guide you.",
            0.7f
        ) { 
            triggerCondition = "high_fear",
            associatedSymbols = new[] { "door", "shadow", "light" }
        });

        // Obsession-based hints
        AddHint(new PsychologicalHint(
            "obsession_pattern",
            "The patterns you see... they might be more than just your imagination.",
            0.6f
        ) {
            triggerCondition = "high_obsession",
            associatedSymbols = new[] { "mirror", "reflection", "symbol" }
        });

        // Paranoia-based hints
        AddHint(new PsychologicalHint(
            "paranoia_truth",
            "Just because you're paranoid doesn't mean the shadows aren't watching.",
            0.8f
        ) {
            triggerCondition = "high_paranoia",
            associatedSymbols = new[] { "eye", "darkness", "watcher" }
        });

        // General psychological hints
        AddHint(new PsychologicalHint(
            "mental_state",
            "Your mental state affects how you perceive this place... and how it perceives you.",
            0.5f
        ) {
            triggerCondition = "any_elevated",
            associatedSymbols = new[] { "mind", "perception", "reality" }
        });
    }

    public void AddHint(PsychologicalHint hint)
    {
        if (!availableHints.Any(h => h.hintId == hint.hintId))
        {
            availableHints.Add(hint);
        }
    }

    public void UpdateHints(PlayerAnalysisProfile profile, Room currentRoom)
    {
        if (Time.time - lastHintTime < hintCooldown)
            return;

        // Update hint relevance scores
        UpdateHintRelevance(profile, currentRoom);

        // Get most relevant hint
        var relevantHint = GetMostRelevantHint(profile);
        if (relevantHint != null && ShouldShowHint(relevantHint, profile))
        {
            DisplayHint(relevantHint);
            lastHintTime = Time.time;
        }
    }

    private void UpdateHintRelevance(PlayerAnalysisProfile profile, Room currentRoom)
    {
        foreach (var hint in availableHints)
        {
            float relevance = 0f;

            // Base relevance on psychological state
            switch (hint.triggerCondition)
            {
                case "high_fear":
                    relevance = profile.FearLevel;
                    break;
                case "high_obsession":
                    relevance = profile.ObsessionLevel;
                    break;
                case "high_paranoia":
                    relevance = (profile.FearLevel + profile.ObsessionLevel) / 2f;
                    break;
                case "any_elevated":
                    relevance = Mathf.Max(profile.FearLevel, profile.ObsessionLevel, profile.AggressionLevel);
                    break;
            }

            // Adjust relevance based on room context
            if (currentRoom != null && hint.associatedSymbols != null)
            {
                foreach (var symbol in hint.associatedSymbols)
                {
                    if (currentRoom.DescriptionText.ToLower().Contains(symbol.ToLower()))
                    {
                        relevance += 0.2f;
                    }
                }
            }

            // Reduce relevance if hint was recently shown
            if (recentHints.Contains(hint.hintId))
            {
                relevance *= 0.5f;
            }

            hint.relevanceScore = Mathf.Clamp01(relevance);
        }
    }

    private PsychologicalHint GetMostRelevantHint(PlayerAnalysisProfile profile)
    {
        return availableHints
            .Where(h => !h.wasShown || IsHintRepeatable(h))
            .Where(h => h.relevanceScore >= h.psychologicalThreshold)
            .OrderByDescending(h => h.relevanceScore)
            .FirstOrDefault();
    }

    private bool IsHintRepeatable(PsychologicalHint hint)
    {
        // Some hints can be shown multiple times if they're important enough
        return hint.relevanceScore > 0.8f && !recentHints.Contains(hint.hintId);
    }

    private bool ShouldShowHint(PsychologicalHint hint, PlayerAnalysisProfile profile)
    {
        // Check if player's state warrants the hint
        if (hint.psychologicalThreshold > 0)
        {
            float relevantState = hint.triggerCondition switch
            {
                "high_fear" => profile.FearLevel,
                "high_obsession" => profile.ObsessionLevel,
                "high_paranoia" => (profile.FearLevel + profile.ObsessionLevel) / 2f,
                _ => Mathf.Max(profile.FearLevel, profile.ObsessionLevel, profile.AggressionLevel)
            };

            if (relevantState < hint.psychologicalThreshold)
                return false;
        }

        return true;
    }

    private void DisplayHint(PsychologicalHint hint)
    {
        // Format hint based on current psychological state
        string formattedHint = FormatHint(hint);

        // Display the hint through UI
        GameManager.Instance.UIManager.DisplayMessage($"\n<color=#8A2BE2>{formattedHint}</color>");

        // Update hint tracking
        hint.wasShown = true;
        recentHints.Enqueue(hint.hintId);
        if (recentHints.Count > MAX_RECENT_HINTS)
        {
            recentHints.Dequeue();
        }

        // Play subtle audio cue
        GameAudioManager.Instance.PlaySound("hint_whisper");
    }

    private string FormatHint(PsychologicalHint hint)
    {
        var profile = GameManager.Instance.ProfileManager.CurrentProfile;
        string formattedText = hint.hintText;

        // Add psychological flavor based on player state
        if (profile.FearLevel > 0.7f)
        {
            formattedText = $"*Your trembling mind whispers*: {formattedText}";
        }
        else if (profile.ObsessionLevel > 0.7f)
        {
            formattedText = $"*The thought repeats endlessly*: {formattedText}";
        }
        else if (profile.AggressionLevel > 0.7f)
        {
            formattedText = $"*Your inner rage reveals*: {formattedText}";
        }
        else
        {
            formattedText = $"*A distant thought emerges*: {formattedText}";
        }

        return formattedText;
    }

    public void GenerateContextualHint(Room room, PlayerAnalysisProfile profile)
    {
        // Generate a hint based on current room and psychological state
        string hintText = GenerateHintText(room, profile);
        
        var contextualHint = new PsychologicalHint(
            $"context_{System.Guid.NewGuid().ToString().Substring(0, 8)}",
            hintText,
            0.4f
        );

        AddHint(contextualHint);
    }

    private string GenerateHintText(Room room, PlayerAnalysisProfile profile)
    {
        if (profile.FearLevel > 0.7f)
        {
            return "In this room, your fears take on a life of their own...";
        }
        else if (profile.ObsessionLevel > 0.7f)
        {
            return "The patterns in this room seem to respond to your thoughts...";
        }
        else if (profile.AggressionLevel > 0.7f)
        {
            return "Your inner violence resonates with this space...";
        }
        
        return "There's something more to this room than meets the eye...";
    }

    public void ResetHints()
    {
        foreach (var hint in availableHints)
        {
            hint.wasShown = false;
            hint.relevanceScore = 0f;
        }
        recentHints.Clear();
        lastHintTime = 0f;
    }
}