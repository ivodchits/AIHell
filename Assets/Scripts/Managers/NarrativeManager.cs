using UnityEngine;
using System.Collections.Generic;
using AIHell.Core.Data;

public class NarrativeManager : MonoBehaviour
{
    [System.Serializable]
    public class NarrativeContext
    {
        public string currentTheme;
        public string dominantEmotion;
        public List<string> activeObsessions;
        public Dictionary<string, int> significantChoices;
        public float narrativeTension;
    }

    private NarrativeContext context;
    private float tensionDecayRate = 0.05f;
    private float maxTension = 1.0f;
    private List<string> narrativeHistory;

    private void Awake()
    {
        InitializeNarrativeSystem();
    }

    private void InitializeNarrativeSystem()
    {
        context = new NarrativeContext
        {
            currentTheme = "Surface Anxiety",
            dominantEmotion = "Unease",
            activeObsessions = new List<string>(),
            significantChoices = new Dictionary<string, int>(),
            narrativeTension = 0.2f
        };
        
        narrativeHistory = new List<string>();
    }

    public void UpdateNarrativeContext(PlayerAnalysisProfile profile, int currentLevel)
    {
        // Update theme based on level and psychological state
        context.currentTheme = DetermineCurrentTheme(currentLevel, profile);
        
        // Update dominant emotion
        context.dominantEmotion = CalculateDominantEmotion(profile);
        
        // Update active obsessions
        context.activeObsessions = new List<string>();
        foreach (var obsession in profile.GetActiveObsessions())
        {
            context.activeObsessions.Add(obsession.Keyword);
        }
        
        // Update narrative tension
        UpdateNarrativeTension(profile);
    }

    public string GenerateRoomNarrative(Room room, PlayerAnalysisProfile profile)
    {
        string baseDescription = room.DescriptionText;
        string enhancedNarrative = EnhanceWithPsychologicalContext(baseDescription, profile);
        
        // Add to narrative history
        narrativeHistory.Add(enhancedNarrative);
        if (narrativeHistory.Count > 10)
        {
            narrativeHistory.RemoveAt(0);
        }
        
        return enhancedNarrative;
    }

    private string EnhanceWithPsychologicalContext(string baseDescription, PlayerAnalysisProfile profile)
    {
        string enhanced = baseDescription;

        // Add psychological layers based on player state
        if (profile.FearLevel > 0.7f)
        {
            enhanced = AddFearLayer(enhanced);
        }
        
        if (profile.ObsessionLevel > 0.6f)
        {
            enhanced = AddObsessionLayer(enhanced, profile);
        }
        
        if (context.narrativeTension > 0.8f)
        {
            enhanced = AddTensionLayer(enhanced);
        }

        return enhanced;
    }

    private string AddFearLayer(string description)
    {
        string[] fearElements = {
            "\nYour heart pounds against your chest...",
            "\nShadows seem to move with malicious intent...",
            "\nThe air grows thick with dread..."
        };

        return description + fearElements[Random.Range(0, fearElements.Length)];
    }

    private string AddObsessionLayer(string description, PlayerAnalysisProfile profile)
    {
        var obsessions = profile.GetActiveObsessions();
        if (obsessions.Length > 0)
        {
            var obsession = obsessions[Random.Range(0, obsessions.Length)];
            string[] obsessionElements = {
                $"\nYou can't help but notice {obsession.Keyword} everywhere...",
                $"\nThe {obsession.Keyword} seems to call to you...",
                $"\nYour thoughts keep returning to {obsession.Keyword}..."
            };

            return description + obsessionElements[Random.Range(0, obsessionElements.Length)];
        }
        return description;
    }

    private string AddTensionLayer(string description)
    {
        string[] tensionElements = {
            "\nThe tension is almost unbearable...",
            "\nYour nerves are stretched to breaking point...",
            "\nReality itself seems to strain under the pressure..."
        };

        return description + tensionElements[Random.Range(0, tensionElements.Length)];
    }

    private string DetermineCurrentTheme(int level, PlayerAnalysisProfile profile)
    {
        switch (level)
        {
            case 1:
                return "Surface Anxiety";
            case 2:
                return profile.FearLevel > 0.7f ? "Deep Isolation" : "Growing Paranoia";
            case 3:
                return profile.ObsessionLevel > 0.7f ? "Obsessive Patterns" : "Existential Dread";
            case 4:
                return profile.AggressionLevel > 0.7f ? "Inner Violence" : "Self Dissolution";
            case 5:
                return "Ultimate Void";
            default:
                return "Unknown Depths";
        }
    }

    private string CalculateDominantEmotion(PlayerAnalysisProfile profile)
    {
        if (profile.FearLevel > 0.8f)
            return "Terror";
        if (profile.ObsessionLevel > 0.8f)
            return "Obsession";
        if (profile.FearLevel > 0.6f)
            return "Fear";
        if (profile.ObsessionLevel > 0.6f)
            return "Fixation";
        if (profile.AggressionLevel > 0.6f)
            return "Rage";
        return "Unease";
    }

    private void UpdateNarrativeTension(PlayerAnalysisProfile profile)
    {
        // Calculate new tension based on psychological state
        float targetTension = Mathf.Max(
            profile.FearLevel,
            profile.ObsessionLevel,
            profile.AggressionLevel
        );

        // Gradually adjust tension
        context.narrativeTension = Mathf.Lerp(
            context.narrativeTension,
            targetTension,
            0.2f
        );

        // Apply decay
        context.narrativeTension = Mathf.Max(
            0.1f,
            context.narrativeTension - (tensionDecayRate * Time.deltaTime)
        );
    }

    public void RecordSignificantChoice(string choice)
    {
        if (!context.significantChoices.ContainsKey(choice))
        {
            context.significantChoices[choice] = 0;
        }
        context.significantChoices[choice]++;
        
        // Increase tension for significant choices
        context.narrativeTension = Mathf.Min(
            maxTension,
            context.narrativeTension + 0.1f
        );
    }

    public NarrativeContext GetCurrentContext()
    {
        return context;
    }

    public List<string> GetRecentNarrativeHistory()
    {
        return new List<string>(narrativeHistory);
    }
}