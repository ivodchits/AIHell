using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using AIHell.Core.Data;
using AIHell.Core;

public class PsychologicalIntentManager : MonoBehaviour
{
    private LLMManager llmManager;
    private GameStateManager gameStateManager;
    private PsychologicalResponseAnalyzer responseAnalyzer;
    private AdaptiveFeedbackSystem feedbackSystem;
    private ToneModulationSystem toneSystem;
    private CoherenceValidationSystem coherenceSystem;
    private EmotionalStateManager emotionManager;
    private EventReinforcementSystem reinforcementSystem;
    private ThematicResonanceSystem resonanceSystem;
    private TimelineCoherenceSystem timelineSystem;
    private LexicalVariationSystem lexicalSystem;
    private PsychologicalBiasManager biasManager;

    private Queue<IntentRequest> intentQueue;
    private Dictionary<string, float> intentWeights;
    private List<string> activeIntents;
    private float minIntentInterval = 15f;
    private float lastIntentTime;

    [System.Serializable]
    public class IntentRequest
    {
        public string type;
        public string context;
        public Dictionary<string, object> parameters;
        public float priority;
        public System.Action<IntentResult> callback;
        public System.DateTime timestamp;
    }

    [System.Serializable]
    public class IntentResult
    {
        public string generatedContent;
        public Dictionary<string, float> psychologicalMetrics;
        public Dictionary<string, float> coherenceScores;
        public string[] appliedTechniques;
        public string[] suggestedFollowups;
    }

    private void Awake()
    {
        llmManager = GameManager.Instance.LLMManager;
        gameStateManager = GameManager.Instance.StateManager;
        responseAnalyzer = GetComponent<PsychologicalResponseAnalyzer>();
        feedbackSystem = GetComponent<AdaptiveFeedbackSystem>();
        toneSystem = GetComponent<ToneModulationSystem>();
        coherenceSystem = GetComponent<CoherenceValidationSystem>();
        emotionManager = GetComponent<EmotionalStateManager>();
        reinforcementSystem = GetComponent<EventReinforcementSystem>();
        resonanceSystem = GetComponent<ThematicResonanceSystem>();
        timelineSystem = GetComponent<TimelineCoherenceSystem>();
        lexicalSystem = GetComponent<LexicalVariationSystem>();
        biasManager = GetComponent<PsychologicalBiasManager>();

        intentQueue = new Queue<IntentRequest>();
        intentWeights = new Dictionary<string, float>();
        activeIntents = new List<string>();

        InitializeIntentWeights();
    }

    private void InitializeIntentWeights()
    {
        // Core psychological intent weights
        intentWeights["existential_dread"] = 0.9f;
        intentWeights["psychological_decay"] = 0.85f;
        intentWeights["personal_horror"] = 0.9f;
        intentWeights["reality_distortion"] = 0.8f;
        intentWeights["isolation"] = 0.85f;
        intentWeights["paranoia"] = 0.8f;
        intentWeights["obsession"] = 0.85f;
        intentWeights["cosmic_horror"] = 0.75f;
    }

    public async Task<IntentResult> ProcessIntent(string type, string context, Dictionary<string, object> parameters = null)
    {
        var request = new IntentRequest
        {
            type = type,
            context = context,
            parameters = parameters ?? new Dictionary<string, object>(),
            priority = CalculateIntentPriority(type, context),
            timestamp = System.DateTime.Now
        };

        var completionSource = new TaskCompletionSource<IntentResult>();
        request.callback = result => completionSource.SetResult(result);
        
        intentQueue.Enqueue(request);
        
        if (intentQueue.Count == 1)
        {
            ProcessIntentQueue();
        }

        return await completionSource.Task;
    }

    private float CalculateIntentPriority(string type, string context)
    {
        // Base priority from intent weight
        float priority = intentWeights.GetValueOrDefault(type, 0.5f);
        
        // Adjust based on current psychological state
        var profile = GameManager.Instance.ProfileManager.CurrentProfile;
        priority *= GetPsychologicalMultiplier(profile);
        
        // Adjust based on emotional state
        var emotionalState = emotionManager.GetDominantState();
        if (emotionalState != null)
        {
            priority *= emotionalState.intensity;
        }
        
        // Adjust based on active biases
        var bias = biasManager.GetDominantBias().Result;
        if (bias != null)
        {
            priority *= bias.strength;
        }
        
        return Mathf.Clamp01(priority);
    }

    private float GetPsychologicalMultiplier(PlayerAnalysisProfile profile)
    {
        float fearMultiplier = Mathf.Lerp(0.8f, 1.2f, profile.FearLevel);
        float obsessionMultiplier = Mathf.Lerp(0.9f, 1.3f, profile.ObsessionLevel);
        float aggressionMultiplier = Mathf.Lerp(0.7f, 1.1f, profile.AggressionLevel);
        
        return (fearMultiplier + obsessionMultiplier + aggressionMultiplier) / 3f;
    }

    private async void ProcessIntentQueue()
    {
        while (intentQueue.Count > 0)
        {
            if (Time.time - lastIntentTime < minIntentInterval)
            {
                await Task.Delay(1000); // Wait a second before checking again
                continue;
            }

            var request = intentQueue.Peek();
            var result = await GenerateIntentContent(request);
            
            // Validate and enhance content
            result = await ValidateAndEnhanceContent(result, request);
            
            request.callback?.Invoke(result);
            intentQueue.Dequeue();
            
            lastIntentTime = Time.time;
            activeIntents.Add(request.type);
        }
    }

    private async Task<IntentResult> GenerateIntentContent(IntentRequest request)
    {
        // Build psychological context
        var context = await BuildPsychologicalContext(request);
        
        // Generate base content
        string baseContent = await GenerateBaseContent(request, context);
        
        // Create initial result
        return new IntentResult
        {
            generatedContent = baseContent,
            psychologicalMetrics = await AnalyzePsychologicalMetrics(baseContent),
            coherenceScores = new Dictionary<string, float>(),
            appliedTechniques = new string[0],
            suggestedFollowups = new string[0]
        };
    }

    private async Task<string> BuildPsychologicalContext(IntentRequest request)
    {
        var contextBuilder = new System.Text.StringBuilder();
        
        // Add psychological profile
        var profile = GameManager.Instance.ProfileManager.CurrentProfile;
        contextBuilder.AppendLine("Psychological State:");
        contextBuilder.AppendLine($"Fear Level: {profile.FearLevel}");
        contextBuilder.AppendLine($"Obsession Level: {profile.ObsessionLevel}");
        contextBuilder.AppendLine($"Aggression Level: {profile.AggressionLevel}");
        
        // Add emotional state
        var emotionalState = emotionManager.GetDominantState();
        if (emotionalState != null)
        {
            contextBuilder.AppendLine("\nEmotional State:");
            contextBuilder.AppendLine($"Dominant Emotion: {emotionalState.dominantEmotion}");
            contextBuilder.AppendLine($"Intensity: {emotionalState.intensity}");
        }
        
        // Add active biases
        var bias = await biasManager.GetDominantBias();
        if (bias != null)
        {
            contextBuilder.AppendLine("\nActive Bias:");
            contextBuilder.AppendLine($"Type: {bias.id}");
            contextBuilder.AppendLine($"Strength: {bias.strength}");
        }
        
        // Add thematic elements
        var resonantElements = resonanceSystem.GetResonantElements();
        if (resonantElements.Any())
        {
            contextBuilder.AppendLine("\nActive Themes:");
            foreach (var element in resonantElements.Take(3))
            {
                contextBuilder.AppendLine($"- {element.content}");
            }
        }

        return contextBuilder.ToString();
    }

    private async Task<string> GenerateBaseContent(IntentRequest request, string context)
    {
        string prompt = $"Generate psychological horror content for intent: {request.type}\n" +
                       $"Context: {request.context}\n\n" +
                       $"Psychological Context:\n{context}\n\n" +
                       "Focus on:\n" +
                       "- Deep psychological impact\n" +
                       "- Personal resonance\n" +
                       "- Horror effectiveness\n" +
                       "- Thematic coherence";

        return await llmManager.GenerateResponse(prompt, "intent_generation");
    }

    private async Task<Dictionary<string, float>> AnalyzePsychologicalMetrics(string content)
    {
        string prompt = "Analyze psychological metrics for this horror content:\n" +
                       $"{content}\n\n" +
                       "Evaluate:\n" +
                       "- Psychological impact\n" +
                       "- Personal resonance\n" +
                       "- Horror effectiveness\n" +
                       "- Emotional depth";

        string response = await llmManager.GenerateResponse(prompt, "metric_analysis");
        return ParseMetrics(response);
    }

    private Dictionary<string, float> ParseMetrics(string response)
    {
        // Implementation would parse metrics from LLM response
        return new Dictionary<string, float>();
    }

    private async Task<IntentResult> ValidateAndEnhanceContent(IntentResult result, IntentRequest request)
    {
        // Validate coherence
        var coherence = await coherenceSystem.ValidateElement(result.generatedContent, request.type);
        result.coherenceScores = coherence.metrics;

        // Check if enhancement needed
        if (ShouldEnhanceContent(result, coherence))
        {
            result = await EnhanceContent(result, request, coherence);
        }

        // Apply appropriate tone
        result.generatedContent = await ApplyTone(result.generatedContent, request);
        
        // Validate thematic resonance
        var resonance = await resonanceSystem.AnalyzeResonance(result.generatedContent, request.type);
        if (resonance.suggestedEnhancements != null && resonance.suggestedEnhancements.Length > 0)
        {
            result.suggestedFollowups = resonance.suggestedEnhancements;
        }
        
        // Record for timeline coherence
        await timelineSystem.RegisterTimelineNode(result.generatedContent, Time.time);
        
        // Update psychological state
        await UpdatePsychologicalState(result, request);

        return result;
    }

    private bool ShouldEnhanceContent(IntentResult result, CoherenceValidationSystem.ValidationResult coherence)
    {
        // Check coherence threshold
        if (!coherence.isValid || coherence.coherenceScore < 0.7f)
            return true;
        
        // Check psychological impact
        if (result.psychologicalMetrics.GetValueOrDefault("psychological_impact", 0f) < 0.7f)
            return true;
        
        // Check horror effectiveness
        if (result.psychologicalMetrics.GetValueOrDefault("horror_effectiveness", 0f) < 0.6f)
            return true;
        
        return false;
    }

    private async Task<IntentResult> EnhanceContent(
        IntentResult result,
        IntentRequest request,
        CoherenceValidationSystem.ValidationResult coherence)
    {
        // Get bias-aware enhancements
        var biasEnhancements = await biasManager.GetDominantBias();
        
        // Get emotional enhancements
        var emotionalEnhancements = await emotionManager.PredictEmotionalResponse(
            result.generatedContent,
            coherence.violations
        );
        
        // Get lexical enhancements
        var lexicalResult = await lexicalSystem.GenerateVariation(
            result.generatedContent,
            request.type,
            CalculateTargetIntensity(result)
        );

        // Build enhanced content
        string enhancedPrompt = "Enhance psychological horror content using:\n" +
                              $"Original: {result.generatedContent}\n" +
                              $"Bias Considerations: {biasEnhancements?.description}\n" +
                              $"Emotional Response: {string.Join(", ", emotionalEnhancements)}\n" +
                              $"Lexical Variations: {lexicalResult.variedText}";

        string enhancedContent = await llmManager.GenerateResponse(enhancedPrompt, "content_enhancement");
        
        // Update result
        result.generatedContent = enhancedContent;
        result.psychologicalMetrics = await AnalyzePsychologicalMetrics(enhancedContent);
        result.appliedTechniques = lexicalResult.usedThemes;
        
        return result;
    }

    private float CalculateTargetIntensity(IntentResult result)
    {
        float baseIntensity = result.psychologicalMetrics.GetValueOrDefault("psychological_impact", 0.5f);
        
        // Adjust based on current psychological state
        var profile = GameManager.Instance.ProfileManager.CurrentProfile;
        float psychologicalMultiplier = GetPsychologicalMultiplier(profile);
        
        // Adjust based on emotional state
        var emotionalState = emotionManager.GetDominantState();
        float emotionalMultiplier = emotionalState != null ? 
            Mathf.Lerp(0.8f, 1.2f, emotionalState.intensity) : 1f;
        
        return Mathf.Clamp01(
            baseIntensity * psychologicalMultiplier * emotionalMultiplier
        );
    }

    private async Task<string> ApplyTone(string content, IntentRequest request)
    {
        var profile = GameManager.Instance.ProfileManager.CurrentProfile;
        string toneId;
        
        if (profile.FearLevel > 0.8f)
            toneId = "psychological_horror";
        else if (profile.ObsessionLevel > 0.7f)
            toneId = "obsessive_horror";
        else if (profile.AggressionLevel > 0.6f)
            toneId = "aggressive_horror";
        else
            toneId = "subtle_dread";

        return await toneSystem.ModulateContent(
            content,
            toneId,
            CalculateTargetIntensity(new IntentResult { generatedContent = content })
        );
    }

    private async Task UpdatePsychologicalState(IntentResult result, IntentRequest request)
    {
        // Record psychological response
        await responseAnalyzer.RecordResponse(
            request.type,
            result.psychologicalMetrics.GetValueOrDefault("psychological_impact", 0.5f),
            request.context,
            result.appliedTechniques
        );
        
        // Submit feedback
        await feedbackSystem.SubmitFeedback(
            request.type,
            result.psychologicalMetrics.GetValueOrDefault("horror_effectiveness", 0.5f),
            request.context
        );
        
        // Record emotional event
        await emotionManager.RecordEmotionalEvent(
            request.type,
            result.psychologicalMetrics,
            request.context
        );
        
        // Submit for reinforcement analysis
        await reinforcementSystem.SubmitEvent(
            request.type,
            result.generatedContent,
            result.psychologicalMetrics.GetValueOrDefault("effectiveness", 0.5f),
            result.psychologicalMetrics
        );
    }

    public void UpdateIntentWeight(string intent, float weight)
    {
        intentWeights[intent] = Mathf.Clamp01(weight);
    }

    public List<string> GetActiveIntents()
    {
        return new List<string>(activeIntents);
    }

    public void ClearStaleIntents()
    {
        activeIntents.Clear();
    }
}