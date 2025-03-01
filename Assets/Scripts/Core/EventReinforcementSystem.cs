using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

public class EventReinforcementSystem : MonoBehaviour
{
    private LLMManager llmManager;
    private GameStateManager gameStateManager;
    private Dictionary<string, ReinforcementProfile> reinforcementProfiles;
    private Queue<ReinforcementEvent> pendingEvents;
    private List<string> activeReinforcements;
    private float minReinforcementInterval = 30f;
    private float lastReinforcementTime;

    [System.Serializable]
    public class ReinforcementProfile
    {
        public string id;
        public string originalEventType;
        public float effectiveness;
        public float psychologicalImpact;
        public string[] triggers;
        public List<string> successfulVariations;
        public Dictionary<string, float> contextualWeights;
        public AnimationCurve reinforcementCurve;
    }

    [System.Serializable]
    public class ReinforcementEvent
    {
        public string type;
        public string originalContent;
        public float originalEffectiveness;
        public string context;
        public Dictionary<string, float> psychologicalMetrics;
        public System.DateTime timestamp;
    }

    [System.Serializable]
    public class ReinforcementResult
    {
        public string eventId;
        public string reinforcedContent;
        public float effectivenessGain;
        public Dictionary<string, float> impacts;
        public string[] appliedTechniques;
    }

    private void Awake()
    {
        llmManager = GameManager.Instance.LLMManager;
        gameStateManager = GameManager.Instance.StateManager;
        reinforcementProfiles = new Dictionary<string, ReinforcementProfile>();
        pendingEvents = new Queue<ReinforcementEvent>();
        activeReinforcements = new List<string>();
        
        InitializeProfiles();
    }

    private async void InitializeProfiles()
    {
        // Initialize core reinforcement profiles
        await CreateProfile("psychological_tension", new Dictionary<string, float> {
            { "anticipation", 0.7f },
            { "personal_relevance", 0.8f },
            { "psychological_impact", 0.9f }
        });

        await CreateProfile("environmental_horror", new Dictionary<string, float> {
            { "atmosphere", 0.8f },
            { "sensory_impact", 0.7f },
            { "spatial_discomfort", 0.6f }
        });

        await CreateProfile("narrative_dread", new Dictionary<string, float> {
            { "story_coherence", 0.7f },
            { "emotional_buildup", 0.8f },
            { "thematic_resonance", 0.9f }
        });

        await CreateProfile("personal_fear", new Dictionary<string, float> {
            { "intimacy", 0.9f },
            { "vulnerability", 0.8f },
            { "psychological_exposure", 0.7f }
        });
    }

    private async Task CreateProfile(string id, Dictionary<string, float> contextualWeights)
    {
        string prompt = $"Generate psychological horror reinforcement profile for: {id}\n" +
                       "Include effective triggers and psychological impact patterns.\n" +
                       $"Contextual weights: {string.Join(", ", contextualWeights.Keys)}";

        string response = await llmManager.GenerateResponse(prompt, "profile_generation");
        var profile = await ParseProfileResponse(response, id, contextualWeights);
        
        reinforcementProfiles[id] = profile;
    }

    private async Task<ReinforcementProfile> ParseProfileResponse(string response, string id, Dictionary<string, float> weights)
    {
        string structurePrompt = $"Structure this horror reinforcement profile:\n{response}";
        string structured = await llmManager.GenerateResponse(structurePrompt, "profile_structuring");

        return new ReinforcementProfile
        {
            id = id,
            originalEventType = id,
            effectiveness = 0.5f,
            psychologicalImpact = CalculateBaseImpact(weights),
            triggers = ExtractTriggers(structured),
            successfulVariations = new List<string>(),
            contextualWeights = weights,
            reinforcementCurve = GenerateReinforcementCurve(weights)
        };
    }

    private float CalculateBaseImpact(Dictionary<string, float> weights)
    {
        return weights.Values.Average();
    }

    private string[] ExtractTriggers(string structured)
    {
        // Implementation would extract triggers from LLM response
        return new string[] { "default_trigger" };
    }

    private AnimationCurve GenerateReinforcementCurve(Dictionary<string, float> weights)
    {
        AnimationCurve curve = new AnimationCurve();
        
        // Initial impact
        curve.AddKey(0f, 0.3f);
        
        // Build based on weights
        float midPoint = weights.Values.Average();
        curve.AddKey(0.3f, midPoint * 0.6f);
        curve.AddKey(0.7f, midPoint * 0.9f);
        
        // Peak effectiveness
        curve.AddKey(1f, Mathf.Min(1f, midPoint * 1.2f));
        
        return curve;
    }

    public async Task SubmitEvent(string type, string content, float effectiveness, Dictionary<string, float> metrics)
    {
        var reinforcementEvent = new ReinforcementEvent
        {
            type = type,
            originalContent = content,
            originalEffectiveness = effectiveness,
            context = await BuildEventContext(),
            psychologicalMetrics = metrics,
            timestamp = System.DateTime.Now
        };

        pendingEvents.Enqueue(reinforcementEvent);

        // Process if we have enough events or high effectiveness
        if (pendingEvents.Count >= 3 || effectiveness > 0.8f)
        {
            await ProcessPendingEvents();
        }
    }

    private async Task<string> BuildEventContext()
    {
        var gameState = gameStateManager.GetCurrentState();
        var profile = GameManager.Instance.ProfileManager.CurrentProfile;
        
        return $"Game State:\n" +
               $"Tension: {gameState.tensionLevel}\n" +
               $"Phase: {gameState.currentPhase}\n" +
               $"Psychological State:\n" +
               $"Fear: {profile.FearLevel}\n" +
               $"Obsession: {profile.ObsessionLevel}\n" +
               $"Aggression: {profile.AggressionLevel}";
    }

    private async Task ProcessPendingEvents()
    {
        var events = new List<ReinforcementEvent>();
        while (pendingEvents.Count > 0)
        {
            events.Add(pendingEvents.Dequeue());
        }

        // Analyze events
        await AnalyzeEvents(events);
        
        // Update profiles
        await UpdateProfiles(events);
        
        // Generate reinforcements
        foreach (var evt in events)
        {
            if (ShouldReinforce(evt))
            {
                await GenerateReinforcement(evt);
            }
        }
    }

    private async Task AnalyzeEvents(List<ReinforcementEvent> events)
    {
        string prompt = "Analyze these psychological horror events for reinforcement potential:\n";
        foreach (var evt in events)
        {
            prompt += $"Type: {evt.type}\n";
            prompt += $"Effectiveness: {evt.originalEffectiveness}\n";
            prompt += $"Content: {evt.originalContent}\n";
            prompt += $"Context: {evt.context}\n\n";
        }

        string response = await llmManager.GenerateResponse(prompt, "event_analysis");
        await ProcessEventAnalysis(response, events);
    }

    private async Task ProcessEventAnalysis(string analysis, List<ReinforcementEvent> events)
    {
        string structurePrompt = $"Structure this horror event analysis:\n{analysis}";
        string structured = await llmManager.GenerateResponse(structurePrompt, "analysis_structuring");
        
        var insights = ParseAnalysisInsights(structured);
        await ApplyAnalysisInsights(insights, events);
    }

    private Dictionary<string, float> ParseAnalysisInsights(string structured)
    {
        // Implementation would parse LLM response into insights
        return new Dictionary<string, float>();
    }

    private async Task ApplyAnalysisInsights(Dictionary<string, float> insights, List<ReinforcementEvent> events)
    {
        foreach (var evt in events)
        {
            if (reinforcementProfiles.TryGetValue(evt.type, out ReinforcementProfile profile))
            {
                // Update profile based on insights
                await UpdateProfileInsights(profile, insights);
            }
        }
    }

    private async Task UpdateProfileInsights(ReinforcementProfile profile, Dictionary<string, float> insights)
    {
        string prompt = "Update horror reinforcement profile with new insights:\n" +
                       $"Profile: {profile.id}\n" +
                       $"Current Effectiveness: {profile.effectiveness}\n" +
                       "New Insights:\n" +
                       string.Join("\n", insights.Select(i => $"- {i.Key}: {i.Value}"));

        string response = await llmManager.GenerateResponse(prompt, "profile_update");
        await ProcessProfileUpdate(profile, response);
    }

    private async Task ProcessProfileUpdate(ReinforcementProfile profile, string update)
    {
        string structurePrompt = $"Structure this profile update:\n{update}";
        string structured = await llmManager.GenerateResponse(structurePrompt, "update_structuring");
        
        var updates = ParseProfileUpdates(structured);
        ApplyProfileUpdates(profile, updates);
    }

    private Dictionary<string, float> ParseProfileUpdates(string structured)
    {
        // Implementation would parse LLM response into updates
        return new Dictionary<string, float>();
    }

    private void ApplyProfileUpdates(ReinforcementProfile profile, Dictionary<string, float> updates)
    {
        foreach (var update in updates)
        {
            if (profile.contextualWeights.ContainsKey(update.Key))
            {
                profile.contextualWeights[update.Key] = Mathf.Lerp(
                    profile.contextualWeights[update.Key],
                    update.Value,
                    0.3f
                );
            }
        }

        // Update overall effectiveness
        profile.effectiveness = Mathf.Lerp(
            profile.effectiveness,
            profile.contextualWeights.Values.Average(),
            0.2f
        );
    }

    private async Task UpdateProfiles(List<ReinforcementEvent> events)
    {
        foreach (var evt in events)
        {
            if (reinforcementProfiles.TryGetValue(evt.type, out ReinforcementProfile profile))
            {
                // Update effectiveness
                float newEffectiveness = Mathf.Lerp(
                    profile.effectiveness,
                    evt.originalEffectiveness,
                    0.2f
                );
                profile.effectiveness = newEffectiveness;

                // Update psychological impact
                float impact = CalculatePsychologicalImpact(evt.psychologicalMetrics);
                profile.psychologicalImpact = Mathf.Lerp(
                    profile.psychologicalImpact,
                    impact,
                    0.2f
                );

                // Store successful variation if effective
                if (evt.originalEffectiveness > 0.7f)
                {
                    profile.successfulVariations.Add(evt.originalContent);
                    if (profile.successfulVariations.Count > 10)
                    {
                        profile.successfulVariations.RemoveAt(0);
                    }
                }

                // Update reinforcement curve
                await UpdateReinforcementCurve(profile, evt);
            }
        }
    }

    private float CalculatePsychologicalImpact(Dictionary<string, float> metrics)
    {
        if (metrics.Count == 0) return 0f;
        return metrics.Values.Average();
    }

    private async Task UpdateReinforcementCurve(ReinforcementProfile profile, ReinforcementEvent evt)
    {
        string prompt = "Generate optimal reinforcement curve based on:\n" +
                       $"Event Type: {evt.type}\n" +
                       $"Effectiveness: {evt.originalEffectiveness}\n" +
                       $"Psychological Impact: {profile.psychologicalImpact}\n" +
                       "Consider psychological progression and timing.";

        string response = await llmManager.GenerateResponse(prompt, "curve_generation");
        profile.reinforcementCurve = ParseReinforcementCurve(response);
    }

    private AnimationCurve ParseReinforcementCurve(string response)
    {
        // Implementation would parse LLM response into animation curve
        return new AnimationCurve(
            new Keyframe(0f, 0.3f),
            new Keyframe(0.5f, 0.6f),
            new Keyframe(1f, 0.9f)
        );
    }

    private bool ShouldReinforce(ReinforcementEvent evt)
    {
        // Check minimum interval
        if (Time.time - lastReinforcementTime < minReinforcementInterval)
            return false;

        // Check effectiveness threshold
        if (evt.originalEffectiveness < 0.5f)
            return false;

        // Check psychological metrics
        if (evt.psychologicalMetrics.Values.Average() < 0.4f)
            return false;

        return true;
    }

    private async Task GenerateReinforcement(ReinforcementEvent evt)
    {
        if (!reinforcementProfiles.TryGetValue(evt.type, out ReinforcementProfile profile))
            return;

        string prompt = "Generate psychological horror reinforcement based on:\n" +
                       $"Original Content: {evt.originalContent}\n" +
                       $"Effectiveness: {evt.originalEffectiveness}\n" +
                       $"Context: {evt.context}\n" +
                       "Focus on psychological impact and horror escalation.";

        string response = await llmManager.GenerateResponse(prompt, "reinforcement_generation");
        var result = await ProcessReinforcement(response, evt, profile);
        
        if (result.effectivenessGain > 0.2f)
        {
            ApplyReinforcement(result);
        }
    }

    private async Task<ReinforcementResult> ProcessReinforcement(string reinforcement, ReinforcementEvent evt, ReinforcementProfile profile)
    {
        string structurePrompt = $"Structure this horror reinforcement:\n{reinforcement}";
        string structured = await llmManager.GenerateResponse(structurePrompt, "reinforcement_structuring");
        
        return new ReinforcementResult
        {
            eventId = System.Guid.NewGuid().ToString(),
            reinforcedContent = ExtractReinforcedContent(structured),
            effectivenessGain = CalculateEffectivenessGain(structured, evt.originalEffectiveness),
            impacts = ParseImpacts(structured),
            appliedTechniques = ExtractTechniques(structured)
        };
    }

    private string ExtractReinforcedContent(string structured)
    {
        // Implementation would extract content from LLM response
        return structured;
    }

    private float CalculateEffectivenessGain(string structured, float originalEffectiveness)
    {
        // Implementation would calculate effectiveness gain
        return 0.2f;
    }

    private Dictionary<string, float> ParseImpacts(string structured)
    {
        // Implementation would parse impacts from LLM response
        return new Dictionary<string, float>();
    }

    private string[] ExtractTechniques(string structured)
    {
        // Implementation would extract techniques from LLM response
        return new string[0];
    }

    private void ApplyReinforcement(ReinforcementResult result)
    {
        activeReinforcements.Add(result.eventId);
        lastReinforcementTime = Time.time;
        
        // Notify game systems of reinforcement
        GameManager.Instance.EventBridge.TriggerEvent(
            "horror_reinforcement",
            result
        );
    }

    public ReinforcementProfile GetProfile(string type)
    {
        return reinforcementProfiles.GetValueOrDefault(type);
    }

    public List<string> GetActiveReinforcements()
    {
        return new List<string>(activeReinforcements);
    }

    public void ClearStaleReinforcements()
    {
        activeReinforcements.Clear();
    }
}