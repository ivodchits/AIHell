using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

public class AdaptiveFeedbackSystem : MonoBehaviour
{
    private LLMManager llmManager;
    private GameStateManager gameStateManager;
    private Dictionary<string, FeedbackMetric> feedbackMetrics;
    private Queue<FeedbackEvent> pendingFeedback;
    private List<AdaptationRule> adaptationRules;
    private float feedbackProcessInterval = 5f;
    private float lastProcessTime;

    [System.Serializable]
    public class FeedbackMetric
    {
        public string id;
        public float value;
        public float confidence;
        public float timeWeight;
        public AnimationCurve decayCurve;
        public Dictionary<string, float> correlations;
        public List<string> relatedEvents;
    }

    [System.Serializable]
    public class FeedbackEvent
    {
        public string type;
        public float intensity;
        public string context;
        public float timestamp;
        public Dictionary<string, object> metadata;
        public List<string> relatedMetrics;
    }

    [System.Serializable]
    public class AdaptationRule
    {
        public string id;
        public string condition;
        public string[] requiredMetrics;
        public float threshold;
        public Dictionary<string, float> adjustments;
        public bool isActive;
        public float lastTriggerTime;
        public float cooldown;
    }

    private void Awake()
    {
        llmManager = GameManager.Instance.LLMManager;
        gameStateManager = GameManager.Instance.StateManager;
        feedbackMetrics = new Dictionary<string, FeedbackMetric>();
        pendingFeedback = new Queue<FeedbackEvent>();
        adaptationRules = new List<AdaptationRule>();
        
        InitializeMetrics();
        InitializeAdaptationRules();
    }

    private void InitializeMetrics()
    {
        // Initialize core feedback metrics
        AddMetric("horror_effectiveness", new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(300f, 0.5f),
            new Keyframe(600f, 0.2f)
        ));

        AddMetric("psychological_impact", new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(180f, 0.7f),
            new Keyframe(600f, 0.3f)
        ));

        AddMetric("personal_resonance", new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(400f, 0.6f),
            new Keyframe(800f, 0.2f)
        ));

        AddMetric("narrative_coherence", new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(500f, 0.8f),
            new Keyframe(1000f, 0.4f)
        ));
    }

    private void AddMetric(string id, AnimationCurve decayCurve)
    {
        feedbackMetrics[id] = new FeedbackMetric
        {
            id = id,
            value = 0.5f,
            confidence = 0f,
            timeWeight = 1f,
            decayCurve = decayCurve,
            correlations = new Dictionary<string, float>(),
            relatedEvents = new List<string>()
        };
    }

    private void InitializeAdaptationRules()
    {
        adaptationRules.Add(new AdaptationRule
        {
            id = "intensity_adjustment",
            condition = "horror_effectiveness < 0.6 && psychological_impact > 0.7",
            requiredMetrics = new[] { "horror_effectiveness", "psychological_impact" },
            threshold = 0.6f,
            adjustments = new Dictionary<string, float>
            {
                { "content_intensity", 0.2f },
                { "psychological_depth", 0.1f }
            },
            isActive = true,
            cooldown = 60f
        });

        adaptationRules.Add(new AdaptationRule
        {
            id = "personal_enhancement",
            condition = "personal_resonance < 0.5 && narrative_coherence > 0.7",
            requiredMetrics = new[] { "personal_resonance", "narrative_coherence" },
            threshold = 0.5f,
            adjustments = new Dictionary<string, float>
            {
                { "personal_context_weight", 0.3f },
                { "emotional_specificity", 0.2f }
            },
            isActive = true,
            cooldown = 120f
        });
    }

    private void Update()
    {
        if (Time.time - lastProcessTime >= feedbackProcessInterval)
        {
            ProcessPendingFeedback();
            lastProcessTime = Time.time;
        }

        UpdateMetricDecay();
    }

    public async Task SubmitFeedback(string type, float intensity, string context, Dictionary<string, object> metadata = null)
    {
        var feedbackEvent = new FeedbackEvent
        {
            type = type,
            intensity = intensity,
            context = context,
            timestamp = Time.time,
            metadata = metadata ?? new Dictionary<string, object>(),
            relatedMetrics = DetermineRelatedMetrics(type)
        };

        pendingFeedback.Enqueue(feedbackEvent);

        // Process immediately if high intensity
        if (intensity > 0.8f)
        {
            await ProcessFeedbackEvent(feedbackEvent);
        }
    }

    private List<string> DetermineRelatedMetrics(string eventType)
    {
        var related = new List<string>();
        
        switch (eventType)
        {
            case "fear_response":
                related.AddRange(new[] { "horror_effectiveness", "psychological_impact" });
                break;
            case "personal_connection":
                related.AddRange(new[] { "personal_resonance", "narrative_coherence" });
                break;
            case "psychological_state":
                related.AddRange(new[] { "psychological_impact", "personal_resonance" });
                break;
            default:
                related.Add("horror_effectiveness");
                break;
        }

        return related;
    }

    private async void ProcessPendingFeedback()
    {
        while (pendingFeedback.Count > 0)
        {
            var feedback = pendingFeedback.Dequeue();
            await ProcessFeedbackEvent(feedback);
        }

        // Check for adaptation triggers
        await CheckAdaptationRules();
    }

    private async Task ProcessFeedbackEvent(FeedbackEvent feedback)
    {
        // Analyze feedback context
        var analysis = await AnalyzeFeedback(feedback);
        
        // Update related metrics
        UpdateMetrics(feedback, analysis);
        
        // Record correlations
        UpdateCorrelations(feedback);
        
        // Trigger immediate adaptations if needed
        if (ShouldTriggerImmediateAdaptation(feedback))
        {
            await TriggerImmediateAdaptation(feedback);
        }
    }

    private async Task<Dictionary<string, float>> AnalyzeFeedback(FeedbackEvent feedback)
    {
        string prompt = "Analyze this psychological horror feedback:\n" +
                       $"Type: {feedback.type}\n" +
                       $"Intensity: {feedback.intensity}\n" +
                       $"Context: {feedback.context}\n\n" +
                       "Evaluate impact on:\n" +
                       "- Horror effectiveness\n" +
                       "- Psychological impact\n" +
                       "- Personal resonance\n" +
                       "- Narrative coherence";

        string response = await llmManager.GenerateResponse(prompt, "feedback_analysis");
        return ParseAnalysisResponse(response);
    }

    private Dictionary<string, float> ParseAnalysisResponse(string response)
    {
        // Implementation would parse LLM response into metric impacts
        return new Dictionary<string, float>();
    }

    private void UpdateMetrics(FeedbackEvent feedback, Dictionary<string, float> analysis)
    {
        foreach (var metricId in feedback.relatedMetrics)
        {
            if (feedbackMetrics.TryGetValue(metricId, out var metric))
            {
                // Get analysis value for this metric
                if (analysis.TryGetValue(metricId, out float impact))
                {
                    // Update metric value with time-weighted impact
                    float timeWeight = metric.decayCurve.Evaluate(
                        Time.time - feedback.timestamp
                    );
                    
                    metric.value = Mathf.Lerp(
                        metric.value,
                        impact,
                        timeWeight * 0.3f
                    );
                    
                    // Update confidence
                    metric.confidence = Mathf.Min(
                        metric.confidence + 0.1f,
                        1f
                    );
                    
                    // Add to related events
                    metric.relatedEvents.Add(feedback.type);
                    if (metric.relatedEvents.Count > 10)
                    {
                        metric.relatedEvents.RemoveAt(0);
                    }
                }
            }
        }
    }

    private void UpdateCorrelations(FeedbackEvent feedback)
    {
        foreach (var metricId in feedback.relatedMetrics)
        {
            if (feedbackMetrics.TryGetValue(metricId, out var metric))
            {
                foreach (var otherId in feedback.relatedMetrics)
                {
                    if (otherId != metricId)
                    {
                        if (!metric.correlations.ContainsKey(otherId))
                        {
                            metric.correlations[otherId] = 0f;
                        }
                        
                        metric.correlations[otherId] = Mathf.Lerp(
                            metric.correlations[otherId],
                            CalculateCorrelation(metricId, otherId),
                            0.2f
                        );
                    }
                }
            }
        }
    }

    private float CalculateCorrelation(string metric1, string metric2)
    {
        if (!feedbackMetrics.TryGetValue(metric1, out var m1) ||
            !feedbackMetrics.TryGetValue(metric2, out var m2))
            return 0f;

        // Simple correlation based on recent events
        int commonEvents = m1.relatedEvents.Intersect(m2.relatedEvents).Count();
        return (float)commonEvents / Mathf.Max(m1.relatedEvents.Count, m2.relatedEvents.Count);
    }

    private void UpdateMetricDecay()
    {
        float currentTime = Time.time;
        
        foreach (var metric in feedbackMetrics.Values)
        {
            // Apply time decay
            float decayFactor = metric.decayCurve.Evaluate(
                currentTime - lastProcessTime
            );
            
            metric.value *= decayFactor;
            metric.confidence *= decayFactor;
        }
    }

    private bool ShouldTriggerImmediateAdaptation(FeedbackEvent feedback)
    {
        return feedback.intensity > 0.8f || 
               feedback.type == "critical_response" ||
               feedback.metadata.ContainsKey("immediate_adapt");
    }

    private async Task TriggerImmediateAdaptation(FeedbackEvent feedback)
    {
        string prompt = "Generate immediate adaptation for:\n" +
                       $"Feedback Type: {feedback.type}\n" +
                       $"Intensity: {feedback.intensity}\n" +
                       $"Context: {feedback.context}\n\n" +
                       "Focus on maintaining psychological impact while adjusting horror elements.";

        string response = await llmManager.GenerateResponse(prompt, "immediate_adaptation");
        await ApplyImmediateAdaptation(response, feedback);
    }

    private async Task ApplyImmediateAdaptation(string adaptation, FeedbackEvent feedback)
    {
        // Parse adaptation response
        var adjustments = ParseAdaptationResponse(adaptation);
        
        // Apply adjustments to relevant systems
        foreach (var adjustment in adjustments)
        {
            await ApplySystemAdjustment(adjustment.Key, adjustment.Value, feedback);
        }
    }

    private Dictionary<string, float> ParseAdaptationResponse(string response)
    {
        // Implementation would parse LLM response into system adjustments
        return new Dictionary<string, float>();
    }

    private async Task ApplySystemAdjustment(string system, float adjustment, FeedbackEvent feedback)
    {
        switch (system)
        {
            case "horror_generation":
                await AdjustHorrorGeneration(adjustment);
                break;
            case "psychological_impact":
                await AdjustPsychologicalImpact(adjustment);
                break;
            case "narrative_coherence":
                await AdjustNarrativeCoherence(adjustment);
                break;
        }
    }

    private async Task CheckAdaptationRules()
    {
        foreach (var rule in adaptationRules.Where(r => r.isActive))
        {
            if (Time.time - rule.lastTriggerTime < rule.cooldown)
                continue;

            if (ShouldTriggerRule(rule))
            {
                await ApplyAdaptationRule(rule);
                rule.lastTriggerTime = Time.time;
            }
        }
    }

    private bool ShouldTriggerRule(AdaptationRule rule)
    {
        // Check if all required metrics are available
        foreach (var metricId in rule.requiredMetrics)
        {
            if (!feedbackMetrics.ContainsKey(metricId))
                return false;
        }

        // Evaluate rule condition
        return EvaluateCondition(rule.condition);
    }

    private bool EvaluateCondition(string condition)
    {
        // Simple condition evaluation
        // This could be enhanced with a proper expression evaluator
        string[] parts = condition.Split(new[] { "&&" }, System.StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var part in parts)
        {
            string[] comparison = part.Trim().Split(' ');
            if (comparison.Length != 3) continue;

            string metricId = comparison[0];
            string op = comparison[1];
            float value = float.Parse(comparison[2]);

            if (!feedbackMetrics.TryGetValue(metricId, out var metric))
                return false;

            switch (op)
            {
                case "<":
                    if (!(metric.value < value)) return false;
                    break;
                case ">":
                    if (!(metric.value > value)) return false;
                    break;
                case "<=":
                    if (!(metric.value <= value)) return false;
                    break;
                case ">=":
                    if (!(metric.value >= value)) return false;
                    break;
            }
        }

        return true;
    }

    private async Task ApplyAdaptationRule(AdaptationRule rule)
    {
        foreach (var adjustment in rule.adjustments)
        {
            await ApplySystemAdjustment(adjustment.Key, adjustment.Value, null);
        }
    }

    private async Task AdjustHorrorGeneration(float adjustment)
    {
        // Implementation would adjust horror generation parameters
        await Task.CompletedTask;
    }

    private async Task AdjustPsychologicalImpact(float adjustment)
    {
        // Implementation would adjust psychological impact parameters
        await Task.CompletedTask;
    }

    private async Task AdjustNarrativeCoherence(float adjustment)
    {
        // Implementation would adjust narrative coherence parameters
        await Task.CompletedTask;
    }

    public Dictionary<string, float> GetCurrentMetrics()
    {
        return feedbackMetrics.ToDictionary(
            m => m.Key,
            m => m.Value.value
        );
    }

    public void AddAdaptationRule(AdaptationRule rule)
    {
        adaptationRules.Add(rule);
    }

    public void ClearFeedbackQueue()
    {
        pendingFeedback.Clear();
    }
}