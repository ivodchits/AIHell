using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

public class EmotionalStateManager : MonoBehaviour
{
    private LLMManager llmManager;
    private GameStateManager gameStateManager;
    private Dictionary<string, EmotionalState> emotionalStates;
    private Queue<EmotionalEvent> eventQueue;
    private List<EmotionalPattern> detectedPatterns;
    private EmotionalPredictionModel predictionModel;

    [System.Serializable]
    public class EmotionalState
    {
        public string id;
        public Dictionary<string, float> emotions;
        public float intensity;
        public float volatility;
        public string dominantEmotion;
        public List<string> activeModifiers;
        public AnimationCurve decayCurve;
        public System.DateTime timestamp;
    }

    [System.Serializable]
    public class EmotionalEvent
    {
        public string type;
        public Dictionary<string, float> emotionalChanges;
        public string context;
        public float impact;
        public string[] triggers;
        public System.DateTime timestamp;
    }

    [System.Serializable]
    public class EmotionalPattern
    {
        public string id;
        public List<string> emotionSequence;
        public float frequency;
        public float psychologicalSignificance;
        public Dictionary<string, float> triggerWeights;
        public AnimationCurve progressionCurve;
    }

    [System.Serializable]
    public class EmotionalPredictionModel
    {
        public Dictionary<string, float> baseWeights;
        public Dictionary<string, List<string>> triggers;
        public Dictionary<string, AnimationCurve> responseCurves;
        public float learningRate;
        public int predictionHorizon;
    }

    private void Awake()
    {
        llmManager = GameManager.Instance.LLMManager;
        gameStateManager = GameManager.Instance.StateManager;
        emotionalStates = new Dictionary<string, EmotionalState>();
        eventQueue = new Queue<EmotionalEvent>();
        detectedPatterns = new List<EmotionalPattern>();
        
        InitializeEmotionalStates();
        InitializePredictionModel();
    }

    private void InitializeEmotionalStates()
    {
        // Initialize core emotional states
        InitializeState("fear", new Dictionary<string, float> {
            { "dread", 0.3f },
            { "anxiety", 0.2f },
            { "terror", 0.1f }
        });

        InitializeState("paranoia", new Dictionary<string, float> {
            { "suspicion", 0.3f },
            { "distrust", 0.2f },
            { "hypervigilance", 0.2f }
        });

        InitializeState("despair", new Dictionary<string, float> {
            { "hopelessness", 0.3f },
            { "isolation", 0.2f },
            { "helplessness", 0.2f }
        });

        InitializeState("obsession", new Dictionary<string, float> {
            { "fixation", 0.3f },
            { "compulsion", 0.2f },
            { "intrusion", 0.2f }
        });
    }

    private void InitializeState(string id, Dictionary<string, float> emotions)
    {
        var state = new EmotionalState
        {
            id = id,
            emotions = emotions,
            intensity = 0.1f,
            volatility = 0.2f,
            dominantEmotion = emotions.OrderByDescending(e => e.Value).First().Key,
            activeModifiers = new List<string>(),
            decayCurve = GenerateDecayCurve(emotions),
            timestamp = System.DateTime.Now
        };

        emotionalStates[id] = state;
    }

    private AnimationCurve GenerateDecayCurve(Dictionary<string, float> emotions)
    {
        AnimationCurve curve = new AnimationCurve();
        
        // Initial impact
        curve.AddKey(0f, 1f);
        
        // Gradual decay based on emotion weights
        float totalWeight = emotions.Values.Sum();
        float decayMidpoint = Mathf.Lerp(0.3f, 0.7f, totalWeight);
        
        curve.AddKey(decayMidpoint, 0.5f);
        curve.AddKey(1f, 0.1f);
        
        return curve;
    }

    private void InitializePredictionModel()
    {
        predictionModel = new EmotionalPredictionModel
        {
            baseWeights = new Dictionary<string, float>(),
            triggers = new Dictionary<string, List<string>>(),
            responseCurves = new Dictionary<string, AnimationCurve>(),
            learningRate = 0.2f,
            predictionHorizon = 5
        };

        // Initialize base emotional weights
        foreach (var state in emotionalStates.Values)
        {
            predictionModel.baseWeights[state.id] = 0.5f;
            predictionModel.triggers[state.id] = new List<string>();
            predictionModel.responseCurves[state.id] = GenerateResponseCurve(state);
        }
    }

    private AnimationCurve GenerateResponseCurve(EmotionalState state)
    {
        AnimationCurve curve = new AnimationCurve();
        
        // Base response
        curve.AddKey(0f, 0f);
        
        // Peak based on state intensity
        curve.AddKey(0.4f, state.intensity * 0.7f);
        
        // Decay based on volatility
        float decayPoint = Mathf.Lerp(0.6f, 0.8f, 1f - state.volatility);
        curve.AddKey(decayPoint, state.intensity * 0.3f);
        
        // Final state
        curve.AddKey(1f, 0.1f);
        
        return curve;
    }

    public async Task RecordEmotionalEvent(string type, Dictionary<string, float> changes, string context)
    {
        var emotionalEvent = new EmotionalEvent
        {
            type = type,
            emotionalChanges = changes,
            context = context,
            impact = CalculateEventImpact(changes),
            triggers = await IdentifyEventTriggers(context),
            timestamp = System.DateTime.Now
        };

        eventQueue.Enqueue(emotionalEvent);

        // Process queue if sufficient events
        if (eventQueue.Count >= 3)
        {
            await ProcessEventQueue();
        }
    }

    private float CalculateEventImpact(Dictionary<string, float> changes)
    {
        float totalImpact = 0f;
        foreach (var change in changes.Values)
        {
            totalImpact += Mathf.Abs(change);
        }
        return Mathf.Clamp01(totalImpact / changes.Count);
    }

    private async Task<string[]> IdentifyEventTriggers(string context)
    {
        string prompt = "Identify emotional triggers in this context:\n" +
                       $"{context}\n\n" +
                       "Consider:\n" +
                       "- Direct psychological triggers\n" +
                       "- Environmental factors\n" +
                       "- Personal relevance\n" +
                       "- Horror elements";

        string response = await llmManager.GenerateResponse(prompt, "trigger_identification");
        return ParseTriggers(response);
    }

    private string[] ParseTriggers(string response)
    {
        // Implementation would parse LLM response into triggers
        return new string[0];
    }

    private async Task ProcessEventQueue()
    {
        var events = new List<EmotionalEvent>();
        while (eventQueue.Count > 0)
        {
            events.Add(eventQueue.Dequeue());
        }

        // Update emotional states
        await UpdateEmotionalStates(events);
        
        // Detect patterns
        await DetectPatterns(events);
        
        // Update prediction model
        UpdatePredictionModel(events);
    }

    private async Task UpdateEmotionalStates(List<EmotionalEvent> events)
    {
        foreach (var emotionalEvent in events)
        {
            foreach (var change in emotionalEvent.emotionalChanges)
            {
                if (emotionalStates.TryGetValue(change.Key, out EmotionalState state))
                {
                    await UpdateState(state, change.Value, emotionalEvent);
                }
            }
        }
    }

    private async Task UpdateState(EmotionalState state, float change, EmotionalEvent triggeringEvent)
    {
        string prompt = "Update emotional state based on event:\n" +
                       $"Current State: {state.id} (Intensity: {state.intensity})\n" +
                       $"Change: {change}\n" +
                       $"Event Type: {triggeringEvent.type}\n" +
                       $"Context: {triggeringEvent.context}";

        string response = await llmManager.GenerateResponse(prompt, "state_update");
        await ProcessStateUpdate(state, response, change);
    }

    private async Task ProcessStateUpdate(EmotionalState state, string update, float change)
    {
        string structurePrompt = $"Structure this emotional state update:\n{update}";
        string structured = await llmManager.GenerateResponse(structurePrompt, "update_structuring");
        
        var updates = ParseStateUpdates(structured);
        ApplyStateUpdates(state, updates, change);
    }

    private Dictionary<string, float> ParseStateUpdates(string structured)
    {
        // Implementation would parse LLM response into state updates
        return new Dictionary<string, float>();
    }

    private void ApplyStateUpdates(EmotionalState state, Dictionary<string, float> updates, float change)
    {
        // Update intensity
        state.intensity = Mathf.Clamp01(state.intensity + change);
        
        // Update emotions
        foreach (var update in updates)
        {
            if (state.emotions.ContainsKey(update.Key))
            {
                state.emotions[update.Key] = Mathf.Clamp01(
                    state.emotions[update.Key] + update.Value
                );
            }
        }
        
        // Update dominant emotion
        state.dominantEmotion = state.emotions
            .OrderByDescending(e => e.Value)
            .First().Key;
        
        state.timestamp = System.DateTime.Now;
    }

    private async Task DetectPatterns(List<EmotionalEvent> events)
    {
        if (events.Count < 3) return;

        string prompt = "Analyze these emotional events for patterns:\n";
        foreach (var evt in events)
        {
            prompt += $"Type: {evt.type}\n";
            prompt += $"Impact: {evt.impact}\n";
            prompt += $"Changes: {string.Join(", ", evt.emotionalChanges.Select(c => $"{c.Key}: {c.Value}"))}\n\n";
        }

        string response = await llmManager.GenerateResponse(prompt, "pattern_analysis");
        await ProcessPatternAnalysis(response);
    }

    private async Task ProcessPatternAnalysis(string analysis)
    {
        string structurePrompt = $"Structure these emotional patterns:\n{analysis}";
        string structured = await llmManager.GenerateResponse(structurePrompt, "pattern_structuring");
        
        var patterns = ParsePatterns(structured);
        UpdateDetectedPatterns(patterns);
    }

    private List<EmotionalPattern> ParsePatterns(string structured)
    {
        // Implementation would parse LLM response into patterns
        return new List<EmotionalPattern>();
    }

    private void UpdateDetectedPatterns(List<EmotionalPattern> newPatterns)
    {
        foreach (var pattern in newPatterns)
        {
            var existing = detectedPatterns.FirstOrDefault(p => p.id == pattern.id);
            if (existing != null)
            {
                // Update existing pattern
                existing.frequency = Mathf.Lerp(existing.frequency, pattern.frequency, 0.3f);
                existing.psychologicalSignificance = Mathf.Max(
                    existing.psychologicalSignificance,
                    pattern.psychologicalSignificance
                );
            }
            else
            {
                // Add new pattern
                detectedPatterns.Add(pattern);
            }
        }
    }

    private void UpdatePredictionModel(List<EmotionalEvent> events)
    {
        foreach (var evt in events)
        {
            // Update base weights
            foreach (var change in evt.emotionalChanges)
            {
                if (predictionModel.baseWeights.ContainsKey(change.Key))
                {
                    predictionModel.baseWeights[change.Key] = Mathf.Lerp(
                        predictionModel.baseWeights[change.Key],
                        change.Value,
                        predictionModel.learningRate
                    );
                }
            }

            // Update triggers
            foreach (var trigger in evt.triggers)
            {
                foreach (var state in evt.emotionalChanges.Keys)
                {
                    if (!predictionModel.triggers[state].Contains(trigger))
                    {
                        predictionModel.triggers[state].Add(trigger);
                    }
                }
            }

            // Update response curves
            foreach (var state in evt.emotionalChanges.Keys)
            {
                predictionModel.responseCurves[state] = UpdateResponseCurve(
                    predictionModel.responseCurves[state],
                    evt.impact
                );
            }
        }
    }

    private AnimationCurve UpdateResponseCurve(AnimationCurve curve, float impact)
    {
        for (int i = 0; i < curve.keys.Length; i++)
        {
            var key = curve.keys[i];
            key.value = Mathf.Lerp(key.value, impact, predictionModel.learningRate);
            curve.MoveKey(i, key);
        }
        return curve;
    }

    public async Task<Dictionary<string, float>> PredictEmotionalResponse(string context, string[] triggers)
    {
        var predictions = new Dictionary<string, float>();
        
        // Get current psychological state
        var profile = GameManager.Instance.ProfileManager.CurrentProfile;
        
        string prompt = "Predict emotional response to:\n" +
                       $"Context: {context}\n" +
                       $"Triggers: {string.Join(", ", triggers)}\n" +
                       $"Fear Level: {profile.FearLevel}\n" +
                       $"Obsession Level: {profile.ObsessionLevel}\n" +
                       $"Aggression Level: {profile.AggressionLevel}";

        string response = await llmManager.GenerateResponse(prompt, "response_prediction");
        return ParsePredictions(response);
    }

    private Dictionary<string, float> ParsePredictions(string response)
    {
        // Implementation would parse LLM response into predictions
        return new Dictionary<string, float>();
    }

    public EmotionalState GetDominantState()
    {
        return emotionalStates.Values
            .OrderByDescending(s => s.intensity)
            .FirstOrDefault();
    }

    public List<EmotionalPattern> GetActivePatterns()
    {
        return detectedPatterns
            .Where(p => p.frequency > 0.3f)
            .OrderByDescending(p => p.psychologicalSignificance)
            .ToList();
    }

    public void ClearStalePatterns()
    {
        detectedPatterns.RemoveAll(p => p.frequency < 0.1f);
    }
}