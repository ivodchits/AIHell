using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using AIHell.Core.Data;

public class PlayerProfileManager : MonoBehaviour
{
    public PlayerAnalysisProfile CurrentProfile { get; private set; }
    private Queue<string> recentChoices;
    private const int MAX_RECENT_CHOICES = 10;
    private Dictionary<string, float> traitDecayRates;

    private void Awake()
    {
        InitializeManager();
    }

    private void InitializeManager()
    {
        recentChoices = new Queue<string>();
        traitDecayRates = new Dictionary<string, float>
        {
            { "fear", 0.05f },
            { "aggression", 0.03f },
            { "curiosity", 0.02f }
        };
        ResetPlayerProfile();
        StartCoroutine(TraitDecayCoroutine());
    }

    public void ResetPlayerProfile()
    {
        CurrentProfile = new PlayerAnalysisProfile();
        recentChoices.Clear();
    }

    public void TrackChoice(string choiceType, string target)
    {
        string fullChoice = $"{choiceType}:{target}";
        recentChoices.Enqueue(fullChoice);
        while (recentChoices.Count > MAX_RECENT_CHOICES)
        {
            recentChoices.Dequeue();
        }

        CurrentProfile.TrackChoice(choiceType, target);
        UpdatePsychologicalProfile();
    }

    private System.Collections.IEnumerator TraitDecayCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(10f); // Decay check every 10 seconds
            ApplyTraitDecay();
        }
    }

    private void ApplyTraitDecay()
    {
        foreach (var trait in traitDecayRates)
        {
            float currentValue = GetTraitLevel(trait.Key);
            float decay = trait.Value * Time.deltaTime;
            SetTraitLevel(trait.Key, Mathf.Max(0, currentValue - decay));
        }
    }

    private void UpdatePsychologicalProfile()
    {
        Dictionary<string, int> choices = CurrentProfile.GetChoiceFrequencies();
        
        // Update base psychological metrics
        UpdateAggressionLevel(choices);
        UpdateCuriosityLevel(choices);
        UpdateFearLevel(choices);

        // Analyze choice patterns
        AnalyzeChoicePatterns();
        
        // Update obsession metrics
        UpdateObsessionMetrics();
    }

    private void UpdateAggressionLevel(Dictionary<string, int> choices)
    {
        float aggressionScore = 0;
        string[] aggressiveActions = { "attack", "break", "destroy", "kill", "fight" };
        
        foreach (string action in aggressiveActions)
        {
            if (choices.TryGetValue(action, out int count))
            {
                aggressionScore += count * 0.2f;
            }
        }

        CurrentProfile.AggressionLevel = Mathf.Min(1f, aggressionScore);
    }

    private void UpdateCuriosityLevel(Dictionary<string, int> choices)
    {
        float curiosityScore = 0;
        string[] curiousActions = { "examine", "look", "inspect", "investigate", "search" };
        
        foreach (string action in curiousActions)
        {
            if (choices.TryGetValue(action, out int count))
            {
                curiosityScore += count * 0.15f;
            }
        }

        CurrentProfile.CuriosityLevel = Mathf.Min(1f, curiosityScore);
    }

    private void UpdateFearLevel(Dictionary<string, int> choices)
    {
        float fearScore = 0;
        string[] fearActions = { "run", "hide", "flee", "escape", "avoid" };
        
        foreach (string action in fearActions)
        {
            if (choices.TryGetValue(action, out int count))
            {
                fearScore += count * 0.25f;
            }
        }

        CurrentProfile.FearLevel = Mathf.Min(1f, fearScore);
    }

    private void AnalyzeChoicePatterns()
    {
        var recentChoicesList = recentChoices.ToList();
        
        // Look for repetitive behaviors
        var repetitions = recentChoicesList
            .GroupBy(x => x)
            .Where(g => g.Count() > 2)
            .ToDictionary(g => g.Key, g => g.Count());

        foreach (var repetition in repetitions)
        {
            CurrentProfile.AddObsessivePattern(repetition.Key, repetition.Value);
        }

        // Analyze choice sequences
        AnalyzeChoiceSequences(recentChoicesList);
    }

    private void AnalyzeChoiceSequences(List<string> choices)
    {
        if (choices.Count < 3) return;

        for (int i = 0; i < choices.Count - 2; i++)
        {
            string pattern = $"{choices[i]}>{choices[i + 1]}>{choices[i + 2]}";
            CurrentProfile.TrackBehaviorPattern(pattern);
        }
    }

    private void UpdateObsessionMetrics()
    {
        var patterns = CurrentProfile.GetBehaviorPatterns();
        float obsessionScore = 0;

        foreach (var pattern in patterns)
        {
            if (pattern.Value > 3) // Pattern repeated more than 3 times
            {
                obsessionScore += 0.2f;
            }
        }

        CurrentProfile.ObsessionLevel = Mathf.Min(1f, obsessionScore);
    }

    public float GetTraitLevel(string trait)
    {
        return trait.ToLower() switch
        {
            "aggression" => CurrentProfile.AggressionLevel,
            "curiosity" => CurrentProfile.CuriosityLevel,
            "fear" => CurrentProfile.FearLevel,
            "obsession" => CurrentProfile.ObsessionLevel,
            _ => 0f
        };
    }

    private void SetTraitLevel(string trait, float value)
    {
        switch (trait.ToLower())
        {
            case "aggression":
                CurrentProfile.AggressionLevel = value;
                break;
            case "curiosity":
                CurrentProfile.CuriosityLevel = value;
                break;
            case "fear":
                CurrentProfile.FearLevel = value;
                break;
            case "obsession":
                CurrentProfile.ObsessionLevel = value;
                break;
        }
    }

    public string[] GetDominantTraits()
    {
        var traits = new List<(string name, float value)>
        {
            ("Aggression", CurrentProfile.AggressionLevel),
            ("Curiosity", CurrentProfile.CuriosityLevel),
            ("Fear", CurrentProfile.FearLevel),
            ("Obsession", CurrentProfile.ObsessionLevel)
        };

        return traits
            .Where(t => t.value > 0.5f)
            .OrderByDescending(t => t.value)
            .Select(t => t.name)
            .ToArray();
    }

    public string[] GetRecentChoices()
    {
        return recentChoices.ToArray();
    }
}