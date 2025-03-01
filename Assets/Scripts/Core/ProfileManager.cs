using UnityEngine;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using AIHell.Core.Data;

public class ProfileManager : MonoBehaviour
{
    private LLMManager llmManager;
    private PlayerAnalysisProfile currentProfile;
    private Dictionary<string, float> choiceWeights;
    private List<PsychologicalEvent> eventHistory;
    private const int MAX_HISTORY = 20;
    private float analysisUpdateInterval = 5f;
    private float lastAnalysisTime;

    [System.Serializable]
    public class PsychologicalEvent
    {
        public string type;
        public string description;
        public float emotionalImpact;
        public float fearContribution;
        public float obsessionContribution;
        public float aggressionContribution;
        public DateTime timestamp;
        public Dictionary<string, float> psychologicalTags;
    }

    private void Awake()
    {
        llmManager = GameManager.Instance.LLMManager;
        InitializeProfile();
    }

    private void InitializeProfile()
    {
        currentProfile = new PlayerAnalysisProfile();
        choiceWeights = new Dictionary<string, float>();
        eventHistory = new List<PsychologicalEvent>();
        InitializeChoiceWeights();
    }

    private void InitializeChoiceWeights()
    {
        choiceWeights["exploration"] = 0.3f;
        choiceWeights["caution"] = 0.3f;
        choiceWeights["confrontation"] = 0.2f;
        choiceWeights["observation"] = 0.2f;
    }

    private void Update()
    {
        if (Time.time - lastAnalysisTime >= analysisUpdateInterval)
        {
            _ = UpdatePsychologicalAnalysis();
            lastAnalysisTime = Time.time;
        }
    }

    public async Task TrackChoice(string choiceType, string description)
    {
        try
        {
            string prompt = $"Analyze psychological impact of player choice:\n" +
                          $"Choice Type: {choiceType}\n" +
                          $"Description: {description}\n" +
                          $"Current Fear: {currentProfile.FearLevel}\n" +
                          $"Current Obsession: {currentProfile.ObsessionLevel}\n" +
                          $"Current Aggression: {currentProfile.AggressionLevel}";

            string analysis = await llmManager.GenerateResponse(prompt, "choice_analysis");
            var impact = ParseChoiceImpact(analysis);
            
            UpdateChoiceWeights(choiceType, impact);
            RecordPsychologicalEvent(choiceType, description, impact);
            
            await UpdatePsychologicalAnalysis();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error tracking choice: {ex.Message}");
        }
    }

    private ChoiceImpact ParseChoiceImpact(string analysis)
    {
        var impact = new ChoiceImpact();
        var lines = analysis.Split('\n');

        foreach (var line in lines)
        {
            if (line.StartsWith("Fear:"))
                float.TryParse(line.Replace("Fear:", "").Trim(), out impact.fearImpact);
            else if (line.StartsWith("Obsession:"))
                float.TryParse(line.Replace("Obsession:", "").Trim(), out impact.obsessionImpact);
            else if (line.StartsWith("Aggression:"))
                float.TryParse(line.Replace("Aggression:", "").Trim(), out impact.aggressionImpact);
            else if (line.StartsWith("Emotional:"))
                float.TryParse(line.Replace("Emotional:", "").Trim(), out impact.emotionalImpact);
        }

        return impact;
    }

    private void UpdateChoiceWeights(string choiceType, ChoiceImpact impact)
    {
        if (!choiceWeights.ContainsKey(choiceType))
            choiceWeights[choiceType] = 0.25f;

        // Adjust weight based on impact
        float adjustment = impact.emotionalImpact * 0.1f;
        choiceWeights[choiceType] = Mathf.Clamp01(choiceWeights[choiceType] + adjustment);

        // Normalize weights
        float total = choiceWeights.Values.Sum();
        foreach (var key in choiceWeights.Keys.ToList())
        {
            choiceWeights[key] /= total;
        }
    }

    private void RecordPsychologicalEvent(string type, string description, ChoiceImpact impact)
    {
        var psychEvent = new PsychologicalEvent
        {
            type = type,
            description = description,
            emotionalImpact = impact.emotionalImpact,
            fearContribution = impact.fearImpact,
            obsessionContribution = impact.obsessionImpact,
            aggressionContribution = impact.aggressionImpact,
            timestamp = DateTime.Now,
            psychologicalTags = new Dictionary<string, float>()
        };

        eventHistory.Add(psychEvent);
        if (eventHistory.Count > MAX_HISTORY)
            eventHistory.RemoveAt(0);
    }

    private async Task UpdatePsychologicalAnalysis()
    {
        if (eventHistory.Count == 0) return;

        try
        {
            string prompt = GenerateAnalysisPrompt();
            string analysis = await llmManager.GenerateResponse(prompt, "psychological_analysis");
            
            var newState = ParsePsychologicalState(analysis);
            UpdateProfileState(newState);
            
            // Notify systems of significant changes
            if (HasSignificantChange(newState))
            {
                await OnSignificantStateChange(newState);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error updating psychological analysis: {ex.Message}");
        }
    }

    private string GenerateAnalysisPrompt()
    {
        var recentEvents = eventHistory.TakeLast(5);
        var promptBuilder = new System.Text.StringBuilder();
        
        promptBuilder.AppendLine("Analyze psychological state based on recent events:");
        foreach (var evt in recentEvents)
        {
            promptBuilder.AppendLine($"Event: {evt.type}");
            promptBuilder.AppendLine($"Description: {evt.description}");
            promptBuilder.AppendLine($"Impact: {evt.emotionalImpact}");
            promptBuilder.AppendLine();
        }

        promptBuilder.AppendLine($"Current Fear: {currentProfile.FearLevel}");
        promptBuilder.AppendLine($"Current Obsession: {currentProfile.ObsessionLevel}");
        promptBuilder.AppendLine($"Current Aggression: {currentProfile.AggressionLevel}");
        
        return promptBuilder.ToString();
    }

    private PlayerAnalysisProfile ParsePsychologicalState(string analysis)
    {
        var newState = new PlayerAnalysisProfile();
        var lines = analysis.Split('\n');

        foreach (var line in lines)
        {
            if (line.StartsWith("Fear="))
                float.TryParse(line.Replace("Fear=", "").Trim(), out newState.FearLevel);
            else if (line.StartsWith("Obsession="))
                float.TryParse(line.Replace("Obsession=", "").Trim(), out newState.ObsessionLevel);
            else if (line.StartsWith("Aggression="))
                float.TryParse(line.Replace("Aggression=", "").Trim(), out newState.AggressionLevel);
        }

        return newState;
    }

    private void UpdateProfileState(PlayerAnalysisProfile newState)
    {
        // Smoothly interpolate to new state
        float lerpFactor = 0.3f;
        currentProfile.FearLevel = Mathf.Lerp(currentProfile.FearLevel, newState.FearLevel, lerpFactor);
        currentProfile.ObsessionLevel = Mathf.Lerp(currentProfile.ObsessionLevel, newState.ObsessionLevel, lerpFactor);
        currentProfile.AggressionLevel = Mathf.Lerp(currentProfile.AggressionLevel, newState.AggressionLevel, lerpFactor);
    }

    private bool HasSignificantChange(PlayerAnalysisProfile newState)
    {
        float threshold = 0.2f;
        return Mathf.Abs(newState.FearLevel - currentProfile.FearLevel) > threshold ||
               Mathf.Abs(newState.ObsessionLevel - currentProfile.ObsessionLevel) > threshold ||
               Mathf.Abs(newState.AggressionLevel - currentProfile.AggressionLevel) > threshold;
    }

    private async Task OnSignificantStateChange(PlayerAnalysisProfile newState)
    {
        // Generate manifestation for significant change
        await GameManager.Instance.GetComponent<ShadowManifestationSystem>()
            .ProcessPsychologicalState(newState, GameManager.Instance.LevelManager.GetCurrentRoom());

        // Update tension
        float tensionModifier = Mathf.Max(
            Mathf.Abs(newState.FearLevel - currentProfile.FearLevel),
            Mathf.Abs(newState.ObsessionLevel - currentProfile.ObsessionLevel),
            Mathf.Abs(newState.AggressionLevel - currentProfile.AggressionLevel)
        );

        GameManager.Instance.TensionManager.ModifyTension(tensionModifier, "psychological_shift");
    }

    private class ChoiceImpact
    {
        public float fearImpact;
        public float obsessionImpact;
        public float aggressionImpact;
        public float emotionalImpact;
    }

    public PlayerAnalysisProfile GetCurrentProfile()
    {
        return currentProfile;
    }

    public List<PsychologicalEvent> GetRecentEvents(int count = 5)
    {
        return eventHistory.TakeLast(count).ToList();
    }

    public Dictionary<string, float> GetChoiceWeights()
    {
        return new Dictionary<string, float>(choiceWeights);
    }

    public void ResetProfile()
    {
        currentProfile = new PlayerAnalysisProfile();
        eventHistory.Clear();
        InitializeChoiceWeights();
    }

    public void SaveProfile()
    {
        PlayerPrefs.SetFloat("FearLevel", currentProfile.FearLevel);
        PlayerPrefs.SetFloat("ObsessionLevel", currentProfile.ObsessionLevel);
        PlayerPrefs.SetFloat("AggressionLevel", currentProfile.AggressionLevel);
        PlayerPrefs.Save();
    }

    public void LoadProfile()
    {
        currentProfile.FearLevel = PlayerPrefs.GetFloat("FearLevel", 0f);
        currentProfile.ObsessionLevel = PlayerPrefs.GetFloat("ObsessionLevel", 0f);
        currentProfile.AggressionLevel = PlayerPrefs.GetFloat("AggressionLevel", 0f);
    }
}