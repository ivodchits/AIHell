using UnityEngine;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using AIHell.Core.Data;

public class PatternAnalyzer : MonoBehaviour
{
    private LLMManager llmManager;
    private Dictionary<string, BehaviorPattern> activePatterns;
    private Queue<PatternObservation> recentObservations;
    private Dictionary<string, float> patternWeights;
    private const int MAX_OBSERVATIONS = 20;
    private float analysisInterval = 10f;
    private float lastAnalysisTime;

    [System.Serializable]
    public class BehaviorPattern
    {
        public string id;
        public string type;
        public string interpretation;
        public float psychologicalWeight;
        public int occurrences;
        public List<string> relatedTriggers;
        public List<string> manifestations;
        public DateTime lastOccurrence;
        public AnimationCurve developmentCurve;
    }

    [System.Serializable]
    public class PatternObservation
    {
        public string action;
        public string context;
        public float psychologicalIntensity;
        public Dictionary<string, float> emotionalState;
        public DateTime timestamp;
    }

    private void Awake()
    {
        llmManager = GameManager.Instance.LLMManager;
        activePatterns = new Dictionary<string, BehaviorPattern>();
        recentObservations = new Queue<PatternObservation>();
        patternWeights = new Dictionary<string, float>();
        InitializePatternWeights();
    }

    private void InitializePatternWeights()
    {
        patternWeights["repetitive"] = 0.3f;
        patternWeights["avoidance"] = 0.3f;
        patternWeights["obsessive"] = 0.4f;
        patternWeights["paranoid"] = 0.4f;
        patternWeights["ritualistic"] = 0.5f;
    }

    private void Update()
    {
        if (Time.time - lastAnalysisTime >= analysisInterval)
        {
            _ = AnalyzePatterns();
            lastAnalysisTime = Time.time;
        }
    }

    public void RecordObservation(string action, string context, PlayerAnalysisProfile profile)
    {
        var observation = new PatternObservation
        {
            action = action,
            context = context,
            psychologicalIntensity = CalculatePsychologicalIntensity(profile),
            emotionalState = new Dictionary<string, float>
            {
                { "fear", profile.FearLevel },
                { "obsession", profile.ObsessionLevel },
                { "aggression", profile.AggressionLevel }
            },
            timestamp = DateTime.Now
        };

        recentObservations.Enqueue(observation);
        while (recentObservations.Count > MAX_OBSERVATIONS)
        {
            recentObservations.Dequeue();
        }

        // Check for immediate pattern recognition
        _ = CheckImmediatePatterns(observation);
    }

    private float CalculatePsychologicalIntensity(PlayerAnalysisProfile profile)
    {
        return (profile.FearLevel + profile.ObsessionLevel + profile.AggressionLevel) / 3f;
    }

    private async Task CheckImmediatePatterns(PatternObservation observation)
    {
        try
        {
            // Check for rapid repetition
            var recentActions = recentObservations
                .TakeLast(5)
                .Select(o => o.action)
                .ToList();

            if (recentActions.Count(a => a == observation.action) >= 3)
            {
                await RecognizePattern("repetitive", observation);
            }

            // Check for high-intensity actions
            if (observation.psychologicalIntensity > 0.7f)
            {
                await RecognizePattern("intense", observation);
            }

            // Check emotional state patterns
            CheckEmotionalPatterns(observation);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error checking immediate patterns: {ex.Message}");
        }
    }

    private void CheckEmotionalPatterns(PatternObservation observation)
    {
        if (observation.emotionalState["fear"] > 0.7f)
        {
            _ = RecognizePattern("fear_driven", observation);
        }
        if (observation.emotionalState["obsession"] > 0.7f)
        {
            _ = RecognizePattern("obsessive", observation);
        }
    }

    private async Task AnalyzePatterns()
    {
        if (recentObservations.Count < 5) return;

        try
        {
            string prompt = GenerateAnalysisPrompt();
            string response = await llmManager.GenerateResponse(prompt, "pattern_analysis");
            var patterns = ParsePatternResponse(response);

            foreach (var pattern in patterns)
            {
                if (!activePatterns.ContainsKey(pattern.id))
                {
                    activePatterns[pattern.id] = pattern;
                }
                else
                {
                    UpdateExistingPattern(activePatterns[pattern.id], pattern);
                }
            }

            // Trigger significant patterns
            await TriggerSignificantPatterns();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error analyzing patterns: {ex.Message}");
        }
    }

    private string GenerateAnalysisPrompt()
    {
        var recentObs = recentObservations.TakeLast(5);
        var promptBuilder = new System.Text.StringBuilder();
        
        promptBuilder.AppendLine("Analyze recent psychological behavior patterns:");
        foreach (var obs in recentObs)
        {
            promptBuilder.AppendLine($"Action: {obs.action}");
            promptBuilder.AppendLine($"Context: {obs.context}");
            promptBuilder.AppendLine($"Intensity: {obs.psychologicalIntensity}");
            promptBuilder.AppendLine("Emotional State:");
            foreach (var emotion in obs.emotionalState)
            {
                promptBuilder.AppendLine($"- {emotion.Key}: {emotion.Value}");
            }
            promptBuilder.AppendLine();
        }

        return promptBuilder.ToString();
    }

    private List<BehaviorPattern> ParsePatternResponse(string response)
    {
        var patterns = new List<BehaviorPattern>();
        try
        {
            var lines = response.Split('\n');
            BehaviorPattern currentPattern = null;

            foreach (var line in lines)
            {
                if (line.StartsWith("Pattern:"))
                {
                    if (currentPattern != null) patterns.Add(currentPattern);
                    currentPattern = new BehaviorPattern
                    {
                        id = $"pattern_{Guid.NewGuid():N}",
                        relatedTriggers = new List<string>(),
                        manifestations = new List<string>(),
                        lastOccurrence = DateTime.Now,
                        developmentCurve = AnimationCurve.EaseInOut(0, 0, 1, 1)
                    };
                }
                else if (currentPattern != null)
                {
                    if (line.StartsWith("Type:"))
                        currentPattern.type = line.Replace("Type:", "").Trim();
                    else if (line.StartsWith("Interpretation:"))
                        currentPattern.interpretation = line.Replace("Interpretation:", "").Trim();
                    else if (line.StartsWith("Weight:"))
                        float.TryParse(line.Replace("Weight:", "").Trim(), out currentPattern.psychologicalWeight);
                }
            }

            if (currentPattern != null) patterns.Add(currentPattern);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error parsing pattern response: {ex.Message}");
        }
        return patterns;
    }

    private void UpdateExistingPattern(BehaviorPattern existing, BehaviorPattern updated)
    {
        existing.occurrences++;
        existing.lastOccurrence = DateTime.Now;
        existing.psychologicalWeight = Mathf.Lerp(
            existing.psychologicalWeight,
            updated.psychologicalWeight,
            0.3f
        );
        
        if (!string.IsNullOrEmpty(updated.interpretation))
        {
            existing.interpretation = updated.interpretation;
        }

        // Merge triggers and manifestations
        existing.relatedTriggers.AddRange(updated.relatedTriggers ?? new List<string>());
        existing.manifestations.AddRange(updated.manifestations ?? new List<string>());
        
        // Keep lists manageable
        if (existing.relatedTriggers.Count > 5)
            existing.relatedTriggers = existing.relatedTriggers.Take(5).ToList();
        if (existing.manifestations.Count > 5)
            existing.manifestations = existing.manifestations.Take(5).ToList();
    }

    private async Task RecognizePattern(string type, PatternObservation observation)
    {
        try
        {
            string prompt = $"Interpret psychological pattern:\n" +
                          $"Type: {type}\n" +
                          $"Action: {observation.action}\n" +
                          $"Context: {observation.context}\n" +
                          $"Intensity: {observation.psychologicalIntensity}";

            string response = await llmManager.GenerateResponse(prompt, "pattern_interpretation");
            var pattern = new BehaviorPattern
            {
                id = $"pattern_{type}_{Guid.NewGuid():N}",
                type = type,
                interpretation = response,
                psychologicalWeight = patternWeights.GetValueOrDefault(type, 0.3f) * observation.psychologicalIntensity,
                occurrences = 1,
                relatedTriggers = new List<string> { observation.action },
                manifestations = new List<string>(),
                lastOccurrence = DateTime.Now,
                developmentCurve = AnimationCurve.EaseInOut(0, 0, 1, 1)
            };

            if (!activePatterns.ContainsKey(pattern.id))
            {
                activePatterns[pattern.id] = pattern;
            }
            else
            {
                UpdateExistingPattern(activePatterns[pattern.id], pattern);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error recognizing pattern: {ex.Message}");
        }
    }

    private async Task TriggerSignificantPatterns()
    {
        var significantPatterns = activePatterns.Values
            .Where(p => p.psychologicalWeight > 0.6f && 
                   (DateTime.Now - p.lastOccurrence).TotalMinutes < 5)
            .OrderByDescending(p => p.psychologicalWeight)
            .Take(2);

        foreach (var pattern in significantPatterns)
        {
            GameManager.Instance.EventProcessor.TriggerPatternEvent(pattern);

            // Update psychological state
            var profile = GameManager.Instance.ProfileManager.CurrentProfile;
            switch (pattern.type.ToLower())
            {
                case "obsessive":
                    profile.ObsessionLevel = Mathf.Min(1f, profile.ObsessionLevel + 0.1f);
                    break;
                case "paranoid":
                    profile.FearLevel = Mathf.Min(1f, profile.FearLevel + 0.1f);
                    break;
                case "aggressive":
                    profile.AggressionLevel = Mathf.Min(1f, profile.AggressionLevel + 0.1f);
                    break;
            }

            // Generate manifestation if pattern is highly significant
            if (pattern.psychologicalWeight > 0.8f)
            {
                await GeneratePatternManifestation(pattern);
            }
        }
    }

    private async Task GeneratePatternManifestation(BehaviorPattern pattern)
    {
        try
        {
            string prompt = $"Generate psychological manifestation for pattern:\n" +
                          $"Type: {pattern.type}\n" +
                          $"Interpretation: {pattern.interpretation}\n" +
                          $"Weight: {pattern.psychologicalWeight}\n" +
                          $"Previous Manifestations: {string.Join(", ", pattern.manifestations)}";

            string manifestation = await llmManager.GenerateResponse(prompt, "pattern_manifestation");
            pattern.manifestations.Add(manifestation);

            // Trigger metaphysical event if pattern is extremely significant
            if (pattern.psychologicalWeight > 0.9f)
            {
                await GameManager.Instance.GetComponent<MetaphysicalEventsSystem>()
                    .GenerateMetaphysicalEvent(GameManager.Instance.LevelManager.GetCurrentRoom());
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error generating pattern manifestation: {ex.Message}");
        }
    }

    public List<BehaviorPattern> GetActivePatterns()
    {
        return activePatterns.Values
            .Where(p => p.psychologicalWeight > 0.3f)
            .OrderByDescending(p => p.psychologicalWeight)
            .ToList();
    }

    public void ResetPatterns()
    {
        activePatterns.Clear();
        recentObservations.Clear();
        InitializePatternWeights();
    }
}