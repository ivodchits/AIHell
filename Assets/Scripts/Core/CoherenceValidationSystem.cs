using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

public class CoherenceValidationSystem : MonoBehaviour
{
    private LLMManager llmManager;
    private GameStateManager gameStateManager;
    private Dictionary<string, NarrativeElement> trackedElements;
    private List<CoherenceRule> validationRules;
    private Queue<ValidationRequest> validationQueue;

    [System.Serializable]
    public class NarrativeElement
    {
        public string id;
        public string type;
        public string content;
        public float coherenceScore;
        public Dictionary<string, float> relationships;
        public List<string> dependencies;
        public System.DateTime timestamp;
        public bool isValid;
    }

    [System.Serializable]
    public class CoherenceRule
    {
        public string id;
        public string description;
        public string[] applicableTypes;
        public float threshold;
        public string[] requiredElements;
        public Dictionary<string, float> weights;
        public bool isRequired;
    }

    [System.Serializable]
    public class ValidationRequest
    {
        public string elementId;
        public string content;
        public string[] context;
        public Dictionary<string, object> metadata;
        public System.Action<ValidationResult> callback;
    }

    [System.Serializable]
    public class ValidationResult
    {
        public bool isValid;
        public float coherenceScore;
        public string[] violations;
        public Dictionary<string, float> metrics;
        public string[] suggestions;
    }

    private void Awake()
    {
        llmManager = GameManager.Instance.LLMManager;
        gameStateManager = GameManager.Instance.StateManager;
        trackedElements = new Dictionary<string, NarrativeElement>();
        validationRules = new List<CoherenceRule>();
        validationQueue = new Queue<ValidationRequest>();
        
        InitializeValidationRules();
    }

    private void InitializeValidationRules()
    {
        // Core psychological coherence rules
        validationRules.Add(new CoherenceRule
        {
            id = "psychological_continuity",
            description = "Maintains consistent psychological themes and character states",
            applicableTypes = new[] { "character_state", "psychological_event" },
            threshold = 0.7f,
            requiredElements = new[] { "psychological_context", "emotional_state" },
            weights = new Dictionary<string, float>
            {
                { "theme_consistency", 0.4f },
                { "character_coherence", 0.3f },
                { "emotional_logic", 0.3f }
            },
            isRequired = true
        });

        // Narrative progression rules
        validationRules.Add(new CoherenceRule
        {
            id = "horror_escalation",
            description = "Ensures proper psychological horror progression and build-up",
            applicableTypes = new[] { "horror_event", "manifestation" },
            threshold = 0.6f,
            requiredElements = new[] { "previous_events", "tension_state" },
            weights = new Dictionary<string, float>
            {
                { "tension_curve", 0.4f },
                { "psychological_impact", 0.3f },
                { "build_up", 0.3f }
            },
            isRequired = false
        });

        // Personal relevance rules
        validationRules.Add(new CoherenceRule
        {
            id = "personal_consistency",
            description = "Maintains consistency with player's psychological profile",
            applicableTypes = new[] { "personal_event", "psychological_trigger" },
            threshold = 0.8f,
            requiredElements = new[] { "player_profile", "psychological_history" },
            weights = new Dictionary<string, float>
            {
                { "personal_relevance", 0.5f },
                { "psychological_accuracy", 0.3f },
                { "emotional_resonance", 0.2f }
            },
            isRequired = true
        });
    }

    public async Task<ValidationResult> ValidateElement(string content, string type, string[] context = null)
    {
        var request = new ValidationRequest
        {
            elementId = System.Guid.NewGuid().ToString(),
            content = content,
            context = context ?? new string[0],
            metadata = new Dictionary<string, object>()
        };

        var completionSource = new TaskCompletionSource<ValidationResult>();
        request.callback = result => completionSource.SetResult(result);
        
        validationQueue.Enqueue(request);
        
        if (validationQueue.Count == 1)
        {
            ProcessValidationQueue();
        }

        return await completionSource.Task;
    }

    private async void ProcessValidationQueue()
    {
        while (validationQueue.Count > 0)
        {
            var request = validationQueue.Peek();
            var result = await ValidateRequest(request);
            
            request.callback?.Invoke(result);
            validationQueue.Dequeue();
        }
    }

    private async Task<ValidationResult> ValidateRequest(ValidationRequest request)
    {
        // First, analyze the content
        var analysis = await AnalyzeContent(request);
        
        // Validate against rules
        var ruleResults = await ValidateAgainstRules(request, analysis);
        
        // Check relationships with existing elements
        var relationshipResults = await ValidateRelationships(request, analysis);
        
        // Combine results
        return CombineValidationResults(analysis, ruleResults, relationshipResults);
    }

    private async Task<Dictionary<string, float>> AnalyzeContent(ValidationRequest request)
    {
        string prompt = "Analyze this psychological horror content for coherence:\n" +
                       $"Content: {request.content}\n\n" +
                       "Context:\n" +
                       string.Join("\n", request.context) + "\n\n" +
                       "Evaluate:\n" +
                       "- Psychological consistency\n" +
                       "- Thematic coherence\n" +
                       "- Personal relevance\n" +
                       "- Horror effectiveness";

        string response = await llmManager.GenerateResponse(prompt, "coherence_analysis");
        return ParseAnalysisResponse(response);
    }

    private Dictionary<string, float> ParseAnalysisResponse(string response)
    {
        // Implementation would parse LLM response into metrics
        return new Dictionary<string, float>();
    }

    private async Task<List<ValidationResult>> ValidateAgainstRules(ValidationRequest request, Dictionary<string, float> analysis)
    {
        var results = new List<ValidationResult>();
        
        foreach (var rule in validationRules)
        {
            if (!ShouldApplyRule(rule, request))
                continue;

            var ruleResult = await ValidateRule(rule, request, analysis);
            results.Add(ruleResult);
        }

        return results;
    }

    private bool ShouldApplyRule(CoherenceRule rule, ValidationRequest request)
    {
        // Check if type is applicable
        if (rule.applicableTypes != null && 
            rule.applicableTypes.Length > 0 && 
            !rule.applicableTypes.Contains(request.GetType().Name))
            return false;

        // Check if required elements are available
        if (rule.requiredElements != null)
        {
            foreach (var required in rule.requiredElements)
            {
                if (!request.metadata.ContainsKey(required))
                    return false;
            }
        }

        return true;
    }

    private async Task<ValidationResult> ValidateRule(CoherenceRule rule, ValidationRequest request, Dictionary<string, float> analysis)
    {
        string prompt = $"Validate against coherence rule: {rule.description}\n" +
                       $"Content: {request.content}\n" +
                       "Context:\n" +
                       string.Join("\n", request.context) + "\n\n" +
                       $"Required Elements: {string.Join(", ", rule.requiredElements)}\n" +
                       $"Threshold: {rule.threshold}";

        string response = await llmManager.GenerateResponse(prompt, "rule_validation");
        return ParseValidationResponse(response, rule);
    }

    private ValidationResult ParseValidationResponse(string response, CoherenceRule rule)
    {
        // Implementation would parse LLM response into validation result
        return new ValidationResult();
    }

    private async Task<Dictionary<string, float>> ValidateRelationships(ValidationRequest request, Dictionary<string, float> analysis)
    {
        var relationships = new Dictionary<string, float>();
        
        // Get recent related elements
        var relatedElements = GetRecentRelatedElements(request);
        
        foreach (var element in relatedElements)
        {
            float relationship = await CalculateRelationship(request, element, analysis);
            relationships[element.id] = relationship;
        }

        return relationships;
    }

    private List<NarrativeElement> GetRecentRelatedElements(ValidationRequest request)
    {
        return trackedElements.Values
            .Where(e => IsRelatedElement(e, request))
            .OrderByDescending(e => e.timestamp)
            .Take(5)
            .ToList();
    }

    private bool IsRelatedElement(NarrativeElement element, ValidationRequest request)
    {
        // Check timestamp (within last hour)
        if ((System.DateTime.Now - element.timestamp).TotalHours > 1)
            return false;

        // Check for shared context
        return request.context.Any(c => element.content.Contains(c));
    }

    private async Task<float> CalculateRelationship(ValidationRequest request, NarrativeElement element, Dictionary<string, float> analysis)
    {
        string prompt = "Calculate psychological relationship between:\n" +
                       $"New Content: {request.content}\n" +
                       $"Existing Element: {element.content}\n\n" +
                       "Consider:\n" +
                       "- Thematic connections\n" +
                       "- Psychological progression\n" +
                       "- Emotional resonance";

        string response = await llmManager.GenerateResponse(prompt, "relationship_calculation");
        return ParseRelationshipScore(response);
    }

    private float ParseRelationshipScore(string response)
    {
        // Implementation would parse LLM response into relationship score
        return 0.5f;
    }

    private ValidationResult CombineValidationResults(
        Dictionary<string, float> analysis,
        List<ValidationResult> ruleResults,
        Dictionary<string, float> relationships)
    {
        var combined = new ValidationResult
        {
            metrics = new Dictionary<string, float>(),
            violations = new List<string>().ToArray(),
            suggestions = new List<string>().ToArray()
        };

        // Combine metrics
        foreach (var metric in analysis)
        {
            combined.metrics[metric.Key] = metric.Value;
        }

        // Add relationship metrics
        foreach (var relationship in relationships)
        {
            combined.metrics[$"relationship_{relationship.Key}"] = relationship.Value;
        }

        // Calculate overall coherence
        combined.coherenceScore = CalculateOverallCoherence(combined.metrics);
        
        // Determine validity
        combined.isValid = IsValidResult(combined, ruleResults);
        
        // Collect violations and suggestions
        CollectViolationsAndSuggestions(combined, ruleResults);

        return combined;
    }

    private float CalculateOverallCoherence(Dictionary<string, float> metrics)
    {
        if (metrics.Count == 0)
            return 0f;

        float total = 0f;
        foreach (var metric in metrics.Values)
        {
            total += metric;
        }
        return total / metrics.Count;
    }

    private bool IsValidResult(ValidationResult combined, List<ValidationResult> ruleResults)
    {
        // Check if coherence score meets minimum threshold
        if (combined.coherenceScore < 0.6f)
            return false;

        // Check if any required rules were violated
        foreach (var result in ruleResults)
        {
            if (!result.isValid)
                return false;
        }

        return true;
    }

    private void CollectViolationsAndSuggestions(ValidationResult combined, List<ValidationResult> ruleResults)
    {
        var violations = new List<string>();
        var suggestions = new List<string>();

        foreach (var result in ruleResults)
        {
            if (result.violations != null)
                violations.AddRange(result.violations);
            
            if (result.suggestions != null)
                suggestions.AddRange(result.suggestions);
        }

        combined.violations = violations.Distinct().ToArray();
        combined.suggestions = suggestions.Distinct().ToArray();
    }

    public void TrackElement(string id, string content, string type)
    {
        var element = new NarrativeElement
        {
            id = id,
            type = type,
            content = content,
            coherenceScore = 0f,
            relationships = new Dictionary<string, float>(),
            dependencies = new List<string>(),
            timestamp = System.DateTime.Now,
            isValid = false
        };

        trackedElements[id] = element;
    }

    public void UpdateElementCoherence(string id, float coherenceScore)
    {
        if (trackedElements.TryGetValue(id, out var element))
        {
            element.coherenceScore = coherenceScore;
            element.isValid = coherenceScore >= 0.6f;
        }
    }

    public List<NarrativeElement> GetIncoherentElements()
    {
        return trackedElements.Values
            .Where(e => !e.isValid)
            .OrderByDescending(e => e.timestamp)
            .ToList();
    }

    public void ClearStaleElements()
    {
        var staleThreshold = System.DateTime.Now.AddHours(-1);
        
        var staleKeys = trackedElements.Keys
            .Where(k => trackedElements[k].timestamp < staleThreshold)
            .ToList();

        foreach (var key in staleKeys)
        {
            trackedElements.Remove(key);
        }
    }
}