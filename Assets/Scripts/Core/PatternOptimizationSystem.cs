using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using AIHell.Core;

public class PatternOptimizationSystem : MonoBehaviour
{
    private LLMManager llmManager;
    private LLMConfigurationManager configManager;
    private Dictionary<string, PatternMetrics> patternMetrics;
    private List<OptimizationRule> optimizationRules;
    private Queue<PatternOptimizationRequest> optimizationQueue;

    [System.Serializable]
    public class PatternMetrics
    {
        public string patternId;
        public float effectivenessScore;
        public float psychologicalImpact;
        public float personalRelevance;
        public float narrativeCoherence;
        public Dictionary<string, float> contextualScores;
        public int successfulApplications;
        public int totalApplications;
    }

    [System.Serializable]
    public class OptimizationRule
    {
        public string id;
        public string description;
        public string[] triggers;
        public float threshold;
        public string optimizationType;
        public Dictionary<string, float> parameters;
        public bool isActive;
    }

    [System.Serializable]
    public class PatternOptimizationRequest
    {
        public string patternId;
        public string originalContent;
        public float currentEffectiveness;
        public string[] optimizationGoals;
        public Dictionary<string, float> constraints;
        public System.Action<string> onOptimized;
    }

    private void Awake()
    {
        llmManager = GameManager.Instance.LLMManager;
        configManager = GetComponent<LLMConfigurationManager>();
        patternMetrics = new Dictionary<string, PatternMetrics>();
        optimizationRules = new List<OptimizationRule>();
        optimizationQueue = new Queue<PatternOptimizationRequest>();
        
        InitializeOptimizationRules();
    }

    private void InitializeOptimizationRules()
    {
        optimizationRules.Add(new OptimizationRule
        {
            id = "psychological_depth",
            description = "Enhance psychological impact while maintaining subtlety",
            triggers = new[] { "low_impact", "surface_level" },
            threshold = 0.6f,
            optimizationType = "psychological_enhancement",
            parameters = new Dictionary<string, float>
            {
                { "depth_factor", 0.7f },
                { "subtlety_threshold", 0.8f }
            },
            isActive = true
        });

        optimizationRules.Add(new OptimizationRule
        {
            id = "personal_relevance",
            description = "Increase personal connection while preserving horror elements",
            triggers = new[] { "generic_horror", "low_engagement" },
            threshold = 0.7f,
            optimizationType = "personalization",
            parameters = new Dictionary<string, float>
            {
                { "relevance_factor", 0.8f },
                { "horror_preservation", 0.7f }
            },
            isActive = true
        });

        // More rules can be added based on optimization needs
    }

    public async Task<string> OptimizePattern(string patternId, string content, float currentEffectiveness)
    {
        var request = new PatternOptimizationRequest
        {
            patternId = patternId,
            originalContent = content,
            currentEffectiveness = currentEffectiveness,
            optimizationGoals = DetermineOptimizationGoals(patternId),
            constraints = GetOptimizationConstraints(patternId)
        };

        // Queue optimization request
        var optimizationResult = new TaskCompletionSource<string>();
        request.onOptimized = result => optimizationResult.SetResult(result);
        optimizationQueue.Enqueue(request);

        // Process queue if not already processing
        if (optimizationQueue.Count == 1)
        {
            ProcessOptimizationQueue();
        }

        return await optimizationResult.Task;
    }

    private string[] DetermineOptimizationGoals(string patternId)
    {
        var goals = new List<string>();
        
        if (patternMetrics.TryGetValue(patternId, out var metrics))
        {
            if (metrics.psychologicalImpact < 0.7f)
                goals.Add("increase_psychological_impact");
            
            if (metrics.personalRelevance < 0.6f)
                goals.Add("enhance_personal_relevance");
            
            if (metrics.narrativeCoherence < 0.8f)
                goals.Add("improve_coherence");
        }
        else
        {
            // Default goals for new patterns
            goals.AddRange(new[] {
                "establish_psychological_depth",
                "ensure_personal_relevance",
                "maintain_horror_effectiveness"
            });
        }

        return goals.ToArray();
    }

    private Dictionary<string, float> GetOptimizationConstraints(string patternId)
    {
        var constraints = new Dictionary<string, float>();
        
        // Base constraints
        constraints["min_effectiveness"] = 0.6f;
        constraints["max_complexity"] = 0.8f;
        
        // Pattern-specific constraints
        if (patternMetrics.TryGetValue(patternId, out var metrics))
        {
            constraints["preserve_coherence"] = metrics.narrativeCoherence;
            constraints["minimum_impact"] = metrics.psychologicalImpact;
        }

        return constraints;
    }

    private async void ProcessOptimizationQueue()
    {
        while (optimizationQueue.Count > 0)
        {
            var request = optimizationQueue.Peek();
            string optimizedContent = await OptimizeContent(request);
            
            // Validate optimization
            if (await ValidateOptimization(optimizedContent, request))
            {
                UpdateMetrics(request.patternId, optimizedContent);
                request.onOptimized(optimizedContent);
                optimizationQueue.Dequeue();
            }
            else
            {
                // Retry with adjusted parameters
                await RetryOptimization(request);
            }
        }
    }

    private async Task<string> OptimizeContent(PatternOptimizationRequest request)
    {
        // Build optimization context
        string context = await BuildOptimizationContext(request);
        
        // Get appropriate LLM configuration
        var config = await configManager.GetConfiguration("pattern_optimization", 
            GameManager.Instance.ProfileManager.CurrentProfile);
        
        // Generate optimized content
        string optimizationPrompt = BuildOptimizationPrompt(request, context);
        return await llmManager.GenerateResponse(optimizationPrompt, "pattern_optimization");
    }

    private async Task<string> BuildOptimizationContext(PatternOptimizationRequest request)
    {
        var contextBuilder = new System.Text.StringBuilder();
        
        // Add pattern metrics
        if (patternMetrics.TryGetValue(request.patternId, out var metrics))
        {
            contextBuilder.AppendLine("Pattern Performance:");
            contextBuilder.AppendLine($"Effectiveness: {metrics.effectivenessScore}");
            contextBuilder.AppendLine($"Psychological Impact: {metrics.psychologicalImpact}");
            contextBuilder.AppendLine($"Personal Relevance: {metrics.personalRelevance}");
        }
        
        // Add optimization goals
        contextBuilder.AppendLine("\nOptimization Goals:");
        foreach (var goal in request.optimizationGoals)
        {
            contextBuilder.AppendLine($"- {goal}");
        }
        
        // Add relevant rules
        var applicableRules = GetApplicableRules(request);
        contextBuilder.AppendLine("\nApplicable Rules:");
        foreach (var rule in applicableRules)
        {
            contextBuilder.AppendLine($"- {rule.description}");
        }

        return contextBuilder.ToString();
    }

    private string BuildOptimizationPrompt(PatternOptimizationRequest request, string context)
    {
        return $"Optimize this psychological horror pattern:\n\n" +
               $"Original Content:\n{request.originalContent}\n\n" +
               $"Context:\n{context}\n\n" +
               "Generate an optimized version that:\n" +
               "1. Enhances psychological impact\n" +
               "2. Increases personal relevance\n" +
               "3. Maintains horror effectiveness\n" +
               "4. Preserves narrative coherence";
    }

    private List<OptimizationRule> GetApplicableRules(PatternOptimizationRequest request)
    {
        return optimizationRules.Where(rule => 
            rule.isActive && 
            rule.threshold <= request.currentEffectiveness &&
            rule.triggers.Any(trigger => request.originalContent.Contains(trigger))
        ).ToList();
    }

    private async Task<bool> ValidateOptimization(string optimizedContent, PatternOptimizationRequest request)
    {
        // Analyze optimized content
        var analysis = await AnalyzeOptimizedContent(optimizedContent, request);
        
        // Check if optimization goals were met
        foreach (var goal in request.optimizationGoals)
        {
            if (!IsGoalMet(goal, analysis, request.constraints))
                return false;
        }
        
        // Ensure horror effectiveness is maintained
        if (analysis.GetValueOrDefault("horror_effectiveness", 0f) < 
            request.currentEffectiveness * 0.9f)
            return false;

        return true;
    }

    private async Task<Dictionary<string, float>> AnalyzeOptimizedContent(string content, PatternOptimizationRequest request)
    {
        string prompt = "Analyze this optimized horror pattern:\n" +
                       $"{content}\n\n" +
                       "Evaluate:\n" +
                       "1. Psychological impact (0-1)\n" +
                       "2. Personal relevance (0-1)\n" +
                       "3. Horror effectiveness (0-1)\n" +
                       "4. Narrative coherence (0-1)";

        string response = await llmManager.GenerateResponse(prompt, "optimization_analysis");
        return ParseAnalysisResponse(response);
    }

    private Dictionary<string, float> ParseAnalysisResponse(string response)
    {
        // Implementation would parse LLM response into metrics
        return new Dictionary<string, float>();
    }

    private bool IsGoalMet(string goal, Dictionary<string, float> analysis, Dictionary<string, float> constraints)
    {
        switch (goal)
        {
            case "increase_psychological_impact":
                return analysis.GetValueOrDefault("psychological_impact", 0f) >= 
                       constraints.GetValueOrDefault("minimum_impact", 0.7f);
            
            case "enhance_personal_relevance":
                return analysis.GetValueOrDefault("personal_relevance", 0f) >= 0.6f;
            
            case "improve_coherence":
                return analysis.GetValueOrDefault("narrative_coherence", 0f) >= 
                       constraints.GetValueOrDefault("preserve_coherence", 0.8f);
            
            default:
                return true;
        }
    }

    private async Task RetryOptimization(PatternOptimizationRequest request)
    {
        // Adjust constraints for retry
        foreach (var constraint in request.constraints.Keys.ToList())
        {
            request.constraints[constraint] *= 0.9f;
        }
        
        // Add more specific guidance
        var adjustedGoals = new List<string>(request.optimizationGoals);
        adjustedGoals.Add("maintain_core_elements");
        request.optimizationGoals = adjustedGoals.ToArray();
        
        // Retry optimization
        await OptimizeContent(request);
    }

    private void UpdateMetrics(string patternId, string optimizedContent)
    {
        if (!patternMetrics.ContainsKey(patternId))
        {
            patternMetrics[patternId] = new PatternMetrics
            {
                patternId = patternId,
                contextualScores = new Dictionary<string, float>()
            };
        }

        var metrics = patternMetrics[patternId];
        metrics.totalApplications++;
        metrics.successfulApplications++;
        
        // Update effectiveness score
        metrics.effectivenessScore = Mathf.Lerp(
            metrics.effectivenessScore,
            CalculateEffectiveness(optimizedContent),
            0.3f
        );
    }

    private float CalculateEffectiveness(string content)
    {
        // Implementation would calculate effectiveness score
        // This is a placeholder implementation
        return 0.8f;
    }

    public void AddOptimizationRule(OptimizationRule rule)
    {
        optimizationRules.Add(rule);
    }

    public PatternMetrics GetPatternMetrics(string patternId)
    {
        return patternMetrics.GetValueOrDefault(patternId);
    }

    public void ClearOptimizationQueue()
    {
        optimizationQueue.Clear();
    }
}