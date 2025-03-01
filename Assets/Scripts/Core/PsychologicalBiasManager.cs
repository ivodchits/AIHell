using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

public class PsychologicalBiasManager : MonoBehaviour
{
    private LLMManager llmManager;
    private GameStateManager gameStateManager;
    private Dictionary<string, BiasProfile> detectedBiases;
    private Queue<BiasObservation> observationQueue;
    private List<BiasPattern> recognizedPatterns;

    [System.Serializable]
    public class BiasProfile
    {
        public string id;
        public string description;
        public float strength;
        public float confidence;
        public string[] triggers;
        public Dictionary<string, float> influences;
        public AnimationCurve developmentCurve;
        public System.DateTime lastUpdate;
    }

    [System.Serializable]
    public class BiasObservation
    {
        public string type;
        public string context;
        public float intensity;
        public string[] relatedBiases;
        public Dictionary<string, object> metadata;
        public System.DateTime timestamp;
    }

    [System.Serializable]
    public class BiasPattern
    {
        public string id;
        public string[] biasSequence;
        public float frequency;
        public float psychologicalImpact;
        public string[] commonTriggers;
        public Dictionary<string, float> correlations;
    }

    [System.Serializable]
    public class BiasAdjustment
    {
        public string targetBias;
        public float adjustment;
        public string reason;
        public float confidence;
        public System.DateTime timestamp;
    }

    private void Awake()
    {
        llmManager = GameManager.Instance.LLMManager;
        gameStateManager = GameManager.Instance.StateManager;
        detectedBiases = new Dictionary<string, BiasProfile>();
        observationQueue = new Queue<BiasObservation>();
        recognizedPatterns = new List<BiasPattern>();

        InitializeCoreBiases();
    }

    private async void InitializeCoreBiases()
    {
        // Initialize fundamental psychological biases
        await InitializeBias("confirmation_bias", new Dictionary<string, float> {
            { "selective_attention", 0.7f },
            { "memory_distortion", 0.6f },
            { "belief_reinforcement", 0.8f }
        });

        await InitializeBias("negativity_bias", new Dictionary<string, float> {
            { "threat_detection", 0.8f },
            { "fear_amplification", 0.7f },
            { "anxiety_response", 0.6f }
        });

        await InitializeBias("pattern_recognition_bias", new Dictionary<string, float> {
            { "false_patterns", 0.7f },
            { "paranoia_enhancement", 0.6f },
            { "meaning_attribution", 0.8f }
        });

        await InitializeBias("emotional_bias", new Dictionary<string, float> {
            { "emotional_memory", 0.8f },
            { "fear_response", 0.7f },
            { "anxiety_sensitivity", 0.6f }
        });
    }

    private async Task InitializeBias(string id, Dictionary<string, float> influences)
    {
        string prompt = $"Generate psychological bias profile for: {id}\n" +
                       "Include description, triggers, and psychological implications.\n" +
                       $"Core influences: {string.Join(", ", influences.Keys)}";

        string response = await llmManager.GenerateResponse(prompt, "bias_profile_generation");
        var profile = await ParseBiasProfile(response, id, influences);
        
        detectedBiases[id] = profile;
    }

    private async Task<BiasProfile> ParseBiasProfile(string response, string id, Dictionary<string, float> influences)
    {
        string structurePrompt = $"Structure this psychological bias profile:\n{response}";
        string structured = await llmManager.GenerateResponse(structurePrompt, "profile_structuring");

        return new BiasProfile
        {
            id = id,
            description = ExtractDescription(structured),
            strength = 0.5f,
            confidence = 0.3f,
            triggers = ExtractTriggers(structured),
            influences = influences,
            developmentCurve = GenerateDevelopmentCurve(influences),
            lastUpdate = System.DateTime.Now
        };
    }

    private string ExtractDescription(string structured)
    {
        // Implementation would extract description from LLM response
        return "Default description";
    }

    private string[] ExtractTriggers(string structured)
    {
        // Implementation would extract triggers from LLM response
        return new string[] { "default_trigger" };
    }

    private AnimationCurve GenerateDevelopmentCurve(Dictionary<string, float> influences)
    {
        AnimationCurve curve = new AnimationCurve();
        
        // Start conservative
        curve.AddKey(0f, 0.2f);
        
        // Build based on influences
        float midPoint = CalculateMidPoint(influences);
        curve.AddKey(0.3f, midPoint * 0.5f);
        curve.AddKey(0.7f, midPoint);
        
        // Peak based on strongest influence
        curve.AddKey(1f, influences.Values.Max());
        
        return curve;
    }

    private float CalculateMidPoint(Dictionary<string, float> influences)
    {
        return influences.Values.Average();
    }

    public async Task RecordObservation(string type, string context, float intensity)
    {
        var observation = new BiasObservation
        {
            type = type,
            context = context,
            intensity = intensity,
            relatedBiases = await IdentifyRelatedBiases(context),
            metadata = new Dictionary<string, object>(),
            timestamp = System.DateTime.Now
        };

        observationQueue.Enqueue(observation);

        if (observationQueue.Count >= 3)
        {
            await ProcessObservationQueue();
        }
    }

    private async Task<string[]> IdentifyRelatedBiases(string context)
    {
        string prompt = "Identify psychological biases in this context:\n" +
                       $"{context}\n\n" +
                       "Consider:\n" +
                       "- Cognitive biases\n" +
                       "- Emotional biases\n" +
                       "- Perceptual biases";

        string response = await llmManager.GenerateResponse(prompt, "bias_identification");
        return ParseBiasIdentification(response);
    }

    private string[] ParseBiasIdentification(string response)
    {
        // Implementation would parse LLM response into bias identifiers
        return new string[0];
    }

    private async Task ProcessObservationQueue()
    {
        var observations = new List<BiasObservation>();
        while (observationQueue.Count > 0)
        {
            observations.Add(observationQueue.Dequeue());
        }

        // Analyze observation patterns
        await AnalyzeObservations(observations);
        
        // Update bias profiles
        await UpdateBiasProfiles(observations);
        
        // Identify new patterns
        await IdentifyNewPatterns(observations);
    }

    private async Task AnalyzeObservations(List<BiasObservation> observations)
    {
        string prompt = "Analyze these psychological bias observations:\n";
        foreach (var obs in observations)
        {
            prompt += $"Type: {obs.type}\n";
            prompt += $"Context: {obs.context}\n";
            prompt += $"Intensity: {obs.intensity}\n\n";
        }
        prompt += "Identify patterns and psychological implications.";

        string response = await llmManager.GenerateResponse(prompt, "observation_analysis");
        await ProcessAnalysis(response, observations);
    }

    private async Task ProcessAnalysis(string analysis, List<BiasObservation> observations)
    {
        string structurePrompt = $"Structure this bias analysis into clear patterns:\n{analysis}";
        string structured = await llmManager.GenerateResponse(structurePrompt, "analysis_structuring");
        
        // Update recognized patterns
        var newPatterns = ParsePatterns(structured);
        foreach (var pattern in newPatterns)
        {
            if (!HasExistingPattern(pattern))
            {
                recognizedPatterns.Add(pattern);
            }
        }
    }

    private List<BiasPattern> ParsePatterns(string structured)
    {
        // Implementation would parse LLM response into patterns
        return new List<BiasPattern>();
    }

    private bool HasExistingPattern(BiasPattern pattern)
    {
        return recognizedPatterns.Any(p => 
            p.biasSequence.SequenceEqual(pattern.biasSequence)
        );
    }

    private async Task UpdateBiasProfiles(List<BiasObservation> observations)
    {
        foreach (var observation in observations)
        {
            foreach (var biasId in observation.relatedBiases)
            {
                if (detectedBiases.TryGetValue(biasId, out BiasProfile profile))
                {
                    await UpdateBiasProfile(profile, observation);
                }
            }
        }
    }

    private async Task UpdateBiasProfile(BiasProfile profile, BiasObservation observation)
    {
        string prompt = "Update psychological bias profile based on observation:\n" +
                       $"Profile: {profile.description}\n" +
                       $"Observation Type: {observation.type}\n" +
                       $"Context: {observation.context}\n" +
                       $"Intensity: {observation.intensity}";

        string response = await llmManager.GenerateResponse(prompt, "profile_update");
        await ProcessProfileUpdate(profile, response);
    }

    private async Task ProcessProfileUpdate(BiasProfile profile, string update)
    {
        string structurePrompt = $"Structure this bias profile update:\n{update}";
        string structured = await llmManager.GenerateResponse(structurePrompt, "update_structuring");
        
        var adjustments = ParseAdjustments(structured);
        ApplyAdjustments(profile, adjustments);
    }

    private List<BiasAdjustment> ParseAdjustments(string structured)
    {
        // Implementation would parse LLM response into adjustments
        return new List<BiasAdjustment>();
    }

    private void ApplyAdjustments(BiasProfile profile, List<BiasAdjustment> adjustments)
    {
        foreach (var adjustment in adjustments)
        {
            // Update strength
            profile.strength = Mathf.Clamp01(
                profile.strength + adjustment.adjustment * adjustment.confidence
            );

            // Update confidence
            profile.confidence = Mathf.Lerp(
                profile.confidence,
                adjustment.confidence,
                0.3f
            );

            profile.lastUpdate = System.DateTime.Now;
        }
    }

    private async Task IdentifyNewPatterns(List<BiasObservation> observations)
    {
        if (observations.Count < 3)
            return;

        string prompt = "Identify new psychological bias patterns in these observations:\n";
        foreach (var obs in observations)
        {
            prompt += $"Type: {obs.type}\n";
            prompt += $"Related Biases: {string.Join(", ", obs.relatedBiases)}\n";
            prompt += $"Context: {obs.context}\n\n";
        }

        string response = await llmManager.GenerateResponse(prompt, "pattern_identification");
        await ProcessNewPatterns(response);
    }

    private async Task ProcessNewPatterns(string patterns)
    {
        string structurePrompt = $"Structure these bias patterns:\n{patterns}";
        string structured = await llmManager.GenerateResponse(structurePrompt, "pattern_structuring");
        
        var newPatterns = ParsePatterns(structured);
        foreach (var pattern in newPatterns)
        {
            if (!HasExistingPattern(pattern))
            {
                recognizedPatterns.Add(pattern);
            }
        }
    }

    public async Task<BiasProfile> GetDominantBias()
    {
        var activeBiases = detectedBiases.Values
            .Where(b => b.strength > 0.5f)
            .OrderByDescending(b => b.strength * b.confidence)
            .ToList();

        if (activeBiases.Count == 0)
            return null;

        // Analyze current psychological state
        var profile = GameManager.Instance.ProfileManager.CurrentProfile;
        
        string prompt = "Determine dominant psychological bias considering:\n" +
                       $"Fear Level: {profile.FearLevel}\n" +
                       $"Obsession Level: {profile.ObsessionLevel}\n" +
                       $"Aggression Level: {profile.AggressionLevel}\n\n" +
                       "Active Biases:\n";
        
        foreach (var bias in activeBiases)
        {
            prompt += $"- {bias.description} (Strength: {bias.strength})\n";
        }

        string response = await llmManager.GenerateResponse(prompt, "dominant_bias_analysis");
        return SelectDominantBias(response, activeBiases);
    }

    private BiasProfile SelectDominantBias(string analysis, List<BiasProfile> candidates)
    {
        // Implementation would select dominant bias based on LLM analysis
        return candidates.FirstOrDefault();
    }

    public List<BiasPattern> GetActivePatterns()
    {
        return recognizedPatterns
            .Where(p => p.frequency > 0.3f)
            .OrderByDescending(p => p.psychologicalImpact)
            .ToList();
    }

    public void ClearStalePatterns()
    {
        recognizedPatterns.RemoveAll(p => p.frequency < 0.1f);
    }
}