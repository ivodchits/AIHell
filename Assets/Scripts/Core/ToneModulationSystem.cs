using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;

public class ToneModulationSystem : MonoBehaviour
{
    private LLMManager llmManager;
    private GameStateManager gameStateManager;
    private Dictionary<string, ToneProfile> activeProfiles;
    private List<ToneTransition> pendingTransitions;
    private AnimationCurve defaultModulationCurve;

    [System.Serializable]
    public class ToneProfile
    {
        public string id;
        public string baseDescription;
        public float intensity;
        public float subtlety;
        public float psychologicalWeight;
        public string[] keywords;
        public Dictionary<string, float> emotionalWeights;
        public AnimationCurve modulationCurve;
    }

    [System.Serializable]
    public class ToneModulation
    {
        public string sourceProfile;
        public string targetProfile;
        public float progress;
        public float duration;
        public AnimationCurve transitionCurve;
        public Dictionary<string, float> modulationParams;
    }

    [System.Serializable]
    public class ToneTransition
    {
        public ToneProfile source;
        public ToneProfile target;
        public float startTime;
        public float duration;
        public bool isActive;
        public System.Action<ToneProfile> onComplete;
    }

    private void Awake()
    {
        llmManager = GameManager.Instance.LLMManager;
        gameStateManager = GameManager.Instance.StateManager;
        activeProfiles = new Dictionary<string, ToneProfile>();
        pendingTransitions = new List<ToneTransition>();
        
        InitializeDefaultCurve();
        InitializeBaseProfiles();
    }

    private void InitializeDefaultCurve()
    {
        defaultModulationCurve = new AnimationCurve();
        defaultModulationCurve.AddKey(0f, 0f);
        defaultModulationCurve.AddKey(0.3f, 0.4f);
        defaultModulationCurve.AddKey(0.7f, 0.8f);
        defaultModulationCurve.AddKey(1f, 1f);
    }

    private async void InitializeBaseProfiles()
    {
        await CreateProfile("subtle_dread", new Dictionary<string, float> {
            { "anxiety", 0.6f },
            { "unease", 0.7f },
            { "foreboding", 0.5f }
        });

        await CreateProfile("psychological_horror", new Dictionary<string, float> {
            { "fear", 0.7f },
            { "paranoia", 0.8f },
            { "isolation", 0.6f }
        });

        await CreateProfile("cosmic_horror", new Dictionary<string, float> {
            { "existential_dread", 0.9f },
            { "cosmic_insignificance", 0.8f },
            { "reality_distortion", 0.7f }
        });

        await CreateProfile("personal_horror", new Dictionary<string, float> {
            { "intimate_fear", 0.8f },
            { "self_doubt", 0.7f },
            { "personal_demons", 0.9f }
        });
    }

    private async Task CreateProfile(string id, Dictionary<string, float> emotionalWeights)
    {
        string prompt = $"Generate a psychological horror tone profile for: {id}\n" +
                       "Include base description and keywords that capture the psychological essence.\n" +
                       $"Emotional weights: {string.Join(", ", emotionalWeights.Keys)}";

        string response = await llmManager.GenerateResponse(prompt, "tone_profile_generation");
        var profile = await ParseProfileResponse(response, id, emotionalWeights);
        
        activeProfiles[id] = profile;
    }

    private async Task<ToneProfile> ParseProfileResponse(string response, string id, Dictionary<string, float> weights)
    {
        // Get structured format from LLM
        string structurePrompt = $"Structure this horror tone profile:\n{response}";
        string structured = await llmManager.GenerateResponse(structurePrompt, "profile_structuring");

        return new ToneProfile
        {
            id = id,
            baseDescription = ExtractDescription(structured),
            intensity = CalculateIntensity(weights),
            subtlety = CalculateSubtlety(weights),
            psychologicalWeight = CalculatePsychologicalWeight(weights),
            keywords = ExtractKeywords(structured),
            emotionalWeights = weights,
            modulationCurve = GenerateModulationCurve(weights)
        };
    }

    private string ExtractDescription(string structured)
    {
        // Implementation would extract description from LLM response
        return "Default description";
    }

    private float CalculateIntensity(Dictionary<string, float> weights)
    {
        float total = 0f;
        foreach (var weight in weights.Values)
        {
            total += weight;
        }
        return total / weights.Count;
    }

    private float CalculateSubtlety(Dictionary<string, float> weights)
    {
        if (weights.TryGetValue("anxiety", out float anxiety))
            return Mathf.Lerp(0.5f, 0.9f, anxiety);
        
        if (weights.TryGetValue("unease", out float unease))
            return Mathf.Lerp(0.4f, 0.8f, unease);
        
        return 0.5f;
    }

    private float CalculatePsychologicalWeight(Dictionary<string, float> weights)
    {
        float psychological = 0f;
        
        if (weights.TryGetValue("paranoia", out float paranoia))
            psychological += paranoia * 0.3f;
        
        if (weights.TryGetValue("isolation", out float isolation))
            psychological += isolation * 0.3f;
        
        if (weights.TryGetValue("self_doubt", out float selfDoubt))
            psychological += selfDoubt * 0.4f;
        
        return Mathf.Clamp01(psychological);
    }

    private string[] ExtractKeywords(string structured)
    {
        // Implementation would extract keywords from LLM response
        return new string[] { "default", "keywords" };
    }

    private AnimationCurve GenerateModulationCurve(Dictionary<string, float> weights)
    {
        AnimationCurve curve = new AnimationCurve();
        
        // Start subtle
        curve.AddKey(0f, 0f);
        
        // Build based on weights
        float midPoint = CalculateMidPoint(weights);
        curve.AddKey(0.3f, midPoint * 0.5f);
        curve.AddKey(0.7f, midPoint);
        
        // Peak intensity
        curve.AddKey(1f, CalculatePeak(weights));
        
        return curve;
    }

    private float CalculateMidPoint(Dictionary<string, float> weights)
    {
        float highest = 0f;
        foreach (var weight in weights.Values)
        {
            highest = Mathf.Max(highest, weight);
        }
        return highest * 0.7f;
    }

    private float CalculatePeak(Dictionary<string, float> weights)
    {
        float total = 0f;
        foreach (var weight in weights.Values)
        {
            total += weight;
        }
        return Mathf.Clamp01(total / weights.Count);
    }

    public async Task<string> ModulateContent(string content, string profileId, float intensity)
    {
        if (!activeProfiles.TryGetValue(profileId, out ToneProfile profile))
        {
            Debug.LogError($"Tone profile not found: {profileId}");
            return content;
        }

        // Build modulation context
        string context = await BuildModulationContext(profile, intensity);
        
        // Generate modulated content
        string prompt = $"Modulate this horror content maintaining psychological tone:\n" +
                       $"Content: {content}\n\n" +
                       $"Tone Profile: {profile.baseDescription}\n" +
                       $"Target Intensity: {intensity}\n" +
                       $"Context:\n{context}";

        return await llmManager.GenerateResponse(prompt, "content_modulation");
    }

    private async Task<string> BuildModulationContext(ToneProfile profile, float targetIntensity)
    {
        var contextBuilder = new System.Text.StringBuilder();
        
        // Add profile details
        contextBuilder.AppendLine($"Profile Keywords: {string.Join(", ", profile.keywords)}");
        contextBuilder.AppendLine($"Base Intensity: {profile.intensity}");
        contextBuilder.AppendLine($"Subtlety Level: {profile.subtlety}");
        
        // Add emotional weights
        contextBuilder.AppendLine("\nEmotional Weights:");
        foreach (var weight in profile.emotionalWeights)
        {
            contextBuilder.AppendLine($"- {weight.Key}: {weight.Value}");
        }
        
        // Add psychological state
        var playerProfile = GameManager.Instance.ProfileManager.CurrentProfile;
        contextBuilder.AppendLine("\nPsychological State:");
        contextBuilder.AppendLine($"Fear Level: {playerProfile.FearLevel}");
        contextBuilder.AppendLine($"Obsession Level: {playerProfile.ObsessionLevel}");
        contextBuilder.AppendLine($"Aggression Level: {playerProfile.AggressionLevel}");

        return contextBuilder.ToString();
    }

    public async Task<ToneProfile> TransitionToProfile(string targetProfileId, float duration)
    {
        if (!activeProfiles.TryGetValue(targetProfileId, out ToneProfile targetProfile))
        {
            Debug.LogError($"Target profile not found: {targetProfileId}");
            return null;
        }

        var currentProfile = GetCurrentProfile();
        var transition = new ToneTransition
        {
            source = currentProfile,
            target = targetProfile,
            startTime = Time.time,
            duration = duration,
            isActive = true
        };

        // Generate transition profile
        var transitionProfile = await GenerateTransitionProfile(currentProfile, targetProfile, duration);
        
        // Add to pending transitions
        pendingTransitions.Add(transition);
        
        return transitionProfile;
    }

    private ToneProfile GetCurrentProfile()
    {
        // Get current active profile based on game state
        var gameState = gameStateManager.GetCurrentState();
        
        if (gameState.tensionLevel > 0.8f)
            return activeProfiles["psychological_horror"];
        
        if (gameState.tensionLevel > 0.5f)
            return activeProfiles["personal_horror"];
        
        return activeProfiles["subtle_dread"];
    }

    private async Task<ToneProfile> GenerateTransitionProfile(ToneProfile source, ToneProfile target, float duration)
    {
        string prompt = "Generate transitional horror tone profile between:\n" +
                       $"Source: {source.baseDescription}\n" +
                       $"Target: {target.baseDescription}\n" +
                       $"Duration: {duration}s\n" +
                       "Create smooth psychological progression.";

        string response = await llmManager.GenerateResponse(prompt, "transition_generation");
        
        // Create blended weights
        var blendedWeights = BlendEmotionalWeights(source.emotionalWeights, target.emotionalWeights, 0.5f);
        
        return await ParseProfileResponse(response, "transition", blendedWeights);
    }

    private Dictionary<string, float> BlendEmotionalWeights(
        Dictionary<string, float> source,
        Dictionary<string, float> target,
        float blend)
    {
        var blended = new Dictionary<string, float>();
        
        // Combine all keys
        var allKeys = new HashSet<string>(source.Keys.Concat(target.Keys));
        
        foreach (var key in allKeys)
        {
            float sourceWeight = source.GetValueOrDefault(key, 0f);
            float targetWeight = target.GetValueOrDefault(key, 0f);
            
            blended[key] = Mathf.Lerp(sourceWeight, targetWeight, blend);
        }
        
        return blended;
    }

    private void Update()
    {
        // Update active transitions
        UpdateTransitions();
    }

    private void UpdateTransitions()
    {
        for (int i = pendingTransitions.Count - 1; i >= 0; i--)
        {
            var transition = pendingTransitions[i];
            
            if (!transition.isActive)
            {
                pendingTransitions.RemoveAt(i);
                continue;
            }

            float progress = (Time.time - transition.startTime) / transition.duration;
            
            if (progress >= 1f)
            {
                CompleteTransition(transition);
                pendingTransitions.RemoveAt(i);
            }
        }
    }

    private void CompleteTransition(ToneTransition transition)
    {
        transition.isActive = false;
        transition.onComplete?.Invoke(transition.target);
    }

    public Dictionary<string, float> GetCurrentToneWeights()
    {
        var currentProfile = GetCurrentProfile();
        return new Dictionary<string, float>(currentProfile.emotionalWeights);
    }

    public void ResetProfile(string profileId)
    {
        if (activeProfiles.ContainsKey(profileId))
        {
            activeProfiles[profileId].modulationCurve = defaultModulationCurve;
        }
    }
}