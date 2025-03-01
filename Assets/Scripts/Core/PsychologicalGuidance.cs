using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using AIHell.Core.Data;

public class PsychologicalGuidance : MonoBehaviour
{
    private LLMManager llmManager;
    private SemanticHorrorAnalyzer semanticAnalyzer;
    private NarrativeContextManager narrativeContext;
    private GameStateManager gameStateManager;
    private List<ThematicThread> activeThreads;
    private Dictionary<string, float> psychologicalAnchors;

    [System.Serializable]
    public class ThematicThread
    {
        public string id;
        public string theme;
        public string psychologicalCore;
        public float intensity;
        public float consistency;
        public List<string> manifestations;
        public List<string> buildupPoints;
        public AnimationCurve progressionCurve;
    }

    [System.Serializable]
    public class GuidanceProfile
    {
        public string dominantFear;
        public string[] secondaryFears;
        public string coreTrauma;
        public float adaptability;
        public Dictionary<string, float> psychologicalThresholds;
        public List<string> avoidedThemes;
    }

    [System.Serializable]
    public class GuidanceRequest
    {
        public string contentType;
        public string currentContext;
        public float desiredIntensity;
        public string[] requiredThemes;
        public string[] buildupPoints;
        public bool allowRecursion;
    }

    private async void Awake()
    {
        InitializeComponents();
        await InitializeGuidance();
    }

    private void InitializeComponents()
    {
        llmManager = GameManager.Instance.LLMManager;
        semanticAnalyzer = GetComponent<SemanticHorrorAnalyzer>();
        narrativeContext = GetComponent<NarrativeContextManager>();
        gameStateManager = GameManager.Instance.StateManager;
        
        activeThreads = new List<ThematicThread>();
        psychologicalAnchors = new Dictionary<string, float>();
    }

    private async Task InitializeGuidance()
    {
        // Initialize psychological anchors
        await InitializePsychologicalAnchors();
        
        // Generate initial thematic threads
        await GenerateInitialThreads();
    }

    private async Task InitializePsychologicalAnchors()
    {
        string prompt = "Generate foundational psychological anchors for horror experience.\n" +
                       "Focus on deep-seated fears and psychological triggers that can evolve.\n" +
                       "Consider universal human fears that can be personalized.";

        string response = await llmManager.GenerateResponse(prompt, "anchor_generation");
        await ProcessPsychologicalAnchors(response);
    }

    private async Task ProcessPsychologicalAnchors(string response)
    {
        // Get structured format from LLM
        string structurePrompt = $"Structure these psychological anchors into clear key-value pairs:\n{response}";
        string structured = await llmManager.GenerateResponse(structurePrompt, "anchor_structuring");
        
        // Parse and store anchors
        ParseAndStoreAnchors(structured);
    }

    private void ParseAndStoreAnchors(string structured)
    {
        // Implementation would parse LLM response into anchors
        // This is a placeholder implementation
        psychologicalAnchors["existential_dread"] = 0.7f;
        psychologicalAnchors["isolation_fear"] = 0.8f;
        psychologicalAnchors["loss_of_self"] = 0.9f;
    }

    private async Task GenerateInitialThreads()
    {
        var profile = GameManager.Instance.ProfileManager.CurrentProfile;
        
        string prompt = "Generate initial psychological horror threads based on:\n" +
                       $"Fear Level: {profile.FearLevel}\n" +
                       $"Obsession Level: {profile.ObsessionLevel}\n" +
                       $"Aggression Level: {profile.AggressionLevel}\n" +
                       "Create evolving themes that can develop throughout the experience.";

        string response = await llmManager.GenerateResponse(prompt, "thread_generation");
        await ProcessInitialThreads(response);
    }

    private async Task ProcessInitialThreads(string response)
    {
        // Get structured format
        string structurePrompt = $"Structure these horror threads into clear themes with progression points:\n{response}";
        string structured = await llmManager.GenerateResponse(structurePrompt, "thread_structuring");
        
        // Create thread objects
        var threads = ParseThreads(structured);
        
        // Initialize threads
        foreach (var thread in threads)
        {
            await InitializeThread(thread);
            activeThreads.Add(thread);
        }
    }

    private List<ThematicThread> ParseThreads(string structured)
    {
        // Implementation would parse LLM response into thread objects
        return new List<ThematicThread>();
    }

    private async Task InitializeThread(ThematicThread thread)
    {
        // Generate psychological core
        string corePrompt = $"Generate psychological core for this horror theme:\n" +
                           $"Theme: {thread.theme}\n" +
                           "Focus on deep psychological impact and personal relevance.";

        thread.psychologicalCore = await llmManager.GenerateResponse(corePrompt, "core_generation");
        
        // Initialize progression curve
        thread.progressionCurve = GenerateProgressionCurve(thread);
    }

    private AnimationCurve GenerateProgressionCurve(ThematicThread thread)
    {
        AnimationCurve curve = new AnimationCurve();
        
        // Start subtle
        curve.AddKey(0f, 0f);
        
        // Add buildup points
        float step = 1f / (thread.buildupPoints.Count + 1);
        for (int i = 0; i < thread.buildupPoints.Count; i++)
        {
            float time = step * (i + 1);
            float value = Mathf.Lerp(0.2f, 0.8f, (float)i / thread.buildupPoints.Count);
            curve.AddKey(time, value);
        }
        
        // Peak
        curve.AddKey(1f, 1f);
        
        return curve;
    }

    public async Task<string> GenerateGuidedContent(GuidanceRequest request)
    {
        // Get current psychological state
        var profile = GameManager.Instance.ProfileManager.CurrentProfile;
        var gameState = gameStateManager.GetCurrentState();
        
        // Build rich context
        string context = await BuildGuidanceContext(request, profile, gameState);
        
        // Generate content
        string response = await llmManager.GenerateResponse(context, "guided_generation");
        
        // Validate and enhance
        return await ValidateAndEnhanceContent(response, request);
    }

    private async Task<string> BuildGuidanceContext(GuidanceRequest request, PlayerAnalysisProfile profile, GameStateManager.GameState gameState)
    {
        var contextBuilder = new System.Text.StringBuilder();
        
        // Add base request
        contextBuilder.AppendLine($"Content Type: {request.contentType}");
        contextBuilder.AppendLine($"Current Context: {request.currentContext}");
        contextBuilder.AppendLine($"Desired Intensity: {request.desiredIntensity}");
        
        // Add psychological state
        contextBuilder.AppendLine("\nPsychological State:");
        contextBuilder.AppendLine($"Fear: {profile.FearLevel}");
        contextBuilder.AppendLine($"Obsession: {profile.ObsessionLevel}");
        contextBuilder.AppendLine($"Aggression: {profile.AggressionLevel}");
        
        // Add active threads
        contextBuilder.AppendLine("\nActive Psychological Threads:");
        foreach (var thread in activeThreads)
        {
            contextBuilder.AppendLine($"- {thread.theme} (Intensity: {thread.intensity})");
        }
        
        // Add psychological anchors
        contextBuilder.AppendLine("\nPsychological Anchors:");
        foreach (var anchor in psychologicalAnchors)
        {
            contextBuilder.AppendLine($"- {anchor.Key}: {anchor.Value}");
        }
        
        return contextBuilder.ToString();
    }

    private async Task<string> ValidateAndEnhanceContent(string content, GuidanceRequest request)
    {
        // Analyze psychological impact
        var analysis = await semanticAnalyzer.AnalyzeHorrorContent(
            content,
            GameManager.Instance.ProfileManager.CurrentProfile
        );
        
        // Validate against requirements
        if (!ValidateContent(content, analysis, request))
        {
            // Regenerate with more specific guidance
            return await RegenerateContent(content, analysis, request);
        }
        
        // Enhance psychological impact
        return await EnhanceContent(content, analysis);
    }

    private bool ValidateContent(string content, SemanticHorrorAnalyzer.AnalysisResult analysis, GuidanceRequest request)
    {
        // Check if required themes are present
        foreach (var theme in request.requiredThemes)
        {
            if (!analysis.primaryConcepts.Contains(theme))
                return false;
        }
        
        // Check psychological impact
        if (analysis.overallPsychologicalImpact < request.desiredIntensity * 0.8f)
            return false;
        
        return true;
    }

    private async Task<string> RegenerateContent(string content, SemanticHorrorAnalyzer.AnalysisResult analysis, GuidanceRequest request)
    {
        string prompt = "Enhance this horror content to meet requirements:\n" +
                       $"Original Content: {content}\n" +
                       $"Required Themes: {string.Join(", ", request.requiredThemes)}\n" +
                       $"Current Impact: {analysis.overallPsychologicalImpact}\n" +
                       $"Desired Impact: {request.desiredIntensity}\n" +
                       "Maintain psychological coherence while strengthening impact.";

        return await llmManager.GenerateResponse(prompt, "content_enhancement");
    }

    private async Task<string> EnhanceContent(string content, SemanticHorrorAnalyzer.AnalysisResult analysis)
    {
        // Generate psychological enhancements
        string enhancementPrompt = $"Enhance the psychological impact of:\n{content}\n\n" +
                                 "Focus on subtle psychological implications and personal relevance.";

        string enhanced = await llmManager.GenerateResponse(enhancementPrompt, "psychological_enhancement");
        
        // Integrate enhancements
        return await IntegrateEnhancements(content, enhanced);
    }

    private async Task<string> IntegrateEnhancements(string original, string enhancements)
    {
        string prompt = "Integrate these psychological enhancements naturally:\n" +
                       $"Original: {original}\n" +
                       $"Enhancements: {enhancements}\n" +
                       "Maintain narrative coherence and psychological impact.";

        return await llmManager.GenerateResponse(prompt, "enhancement_integration");
    }

    public async Task UpdateGuidance()
    {
        // Update active threads
        foreach (var thread in activeThreads.ToList())
        {
            await UpdateThread(thread);
        }
        
        // Generate new threads if needed
        if (ShouldGenerateNewThread())
        {
            await GenerateNewThread();
        }
        
        // Update psychological anchors
        await UpdatePsychologicalAnchors();
    }

    private async Task UpdateThread(ThematicThread thread)
    {
        // Get current progression
        float progress = CalculateThreadProgress(thread);
        
        // Update intensity based on progression
        thread.intensity = thread.progressionCurve.Evaluate(progress);
        
        // Check for next buildup point
        if (ShouldTriggerBuildupPoint(thread, progress))
        {
            await TriggerBuildupPoint(thread);
        }
        
        // Update consistency
        thread.consistency = CalculateThreadConsistency(thread);
    }

    private float CalculateThreadProgress(ThematicThread thread)
    {
        // Calculate based on manifestations and time
        return Mathf.Clamp01(thread.manifestations.Count / 10f);
    }

    private bool ShouldTriggerBuildupPoint(ThematicThread thread, float progress)
    {
        if (thread.buildupPoints.Count == 0)
            return false;

        float nextBuildupProgress = (float)(thread.manifestations.Count + 1) / thread.buildupPoints.Count;
        return progress >= nextBuildupProgress;
    }

    private async Task TriggerBuildupPoint(ThematicThread thread)
    {
        string prompt = $"Generate psychological buildup for horror thread:\n" +
                       $"Theme: {thread.theme}\n" +
                       $"Core: {thread.psychologicalCore}\n" +
                       $"Current Intensity: {thread.intensity}\n" +
                       "Create a significant psychological development.";

        string buildup = await llmManager.GenerateResponse(prompt, "buildup_generation");
        thread.buildupPoints.Add(buildup);
        
        // Trigger manifestation
        await GameManager.Instance.ShadowManifestationSystem.GenerateContextualManifestation(
            GameManager.Instance.ProfileManager.CurrentProfile,
            GameManager.Instance.LevelManager.CurrentRoom
        );
    }

    private float CalculateThreadConsistency(ThematicThread thread)
    {
        // Calculate based on manifestation coherence
        float consistency = 1f;
        
        for (int i = 1; i < thread.manifestations.Count; i++)
        {
            // Compare adjacent manifestations for thematic consistency
            consistency *= 0.9f;
        }
        
        return consistency;
    }

    private bool ShouldGenerateNewThread()
    {
        if (activeThreads.Count >= 5)
            return false;

        var gameState = gameStateManager.GetCurrentState();
        var profile = GameManager.Instance.ProfileManager.CurrentProfile;
        
        // Calculate probability based on state
        float probability = 0.1f;
        probability += gameState.tensionLevel * 0.2f;
        probability += profile.FearLevel * 0.15f;
        probability += profile.ObsessionLevel * 0.15f;
        
        return Random.value < probability;
    }

    private async Task GenerateNewThread()
    {
        var profile = GameManager.Instance.ProfileManager.CurrentProfile;
        
        string prompt = $"Generate new psychological horror thread based on:\n" +
                       $"Fear Level: {profile.FearLevel}\n" +
                       $"Obsession Level: {profile.ObsessionLevel}\n" +
                       $"Aggression Level: {profile.AggressionLevel}\n" +
                       $"Active Threads: {string.Join(", ", activeThreads.Select(t => t.theme))}\n" +
                       "Create a complementary psychological theme.";

        string response = await llmManager.GenerateResponse(prompt, "thread_generation");
        var thread = ParseThread(response);
        
        if (thread != null)
        {
            await InitializeThread(thread);
            activeThreads.Add(thread);
        }
    }

    private ThematicThread ParseThread(string response)
    {
        // Implementation would parse LLM response into thread object
        return null;
    }

    private async Task UpdatePsychologicalAnchors()
    {
        var profile = GameManager.Instance.ProfileManager.CurrentProfile;
        
        foreach (var anchor in psychologicalAnchors.ToList())
        {
            // Update anchor strength based on psychological state
            float newStrength = CalculateAnchorStrength(anchor.Key, profile);
            psychologicalAnchors[anchor.Key] = Mathf.Lerp(anchor.Value, newStrength, 0.1f);
        }
    }

    private float CalculateAnchorStrength(string anchor, PlayerAnalysisProfile profile)
    {
        switch (anchor)
        {
            case "existential_dread":
                return Mathf.Max(profile.FearLevel, profile.ObsessionLevel);
            case "isolation_fear":
                return profile.FearLevel * 0.8f + profile.ObsessionLevel * 0.2f;
            case "loss_of_self":
                return profile.ObsessionLevel * 0.7f + profile.AggressionLevel * 0.3f;
            default:
                return 0.5f;
        }
    }

    public List<ThematicThread> GetActiveThreads()
    {
        return new List<ThematicThread>(activeThreads);
    }

    public void ClearThreads()
    {
        activeThreads.Clear();
    }
}