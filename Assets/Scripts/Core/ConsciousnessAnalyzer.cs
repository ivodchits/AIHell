using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using AIHell.Core.Data;

public class ConsciousnessAnalyzer : MonoBehaviour
{
    [System.Serializable]
    public class ConsciousnessPattern
    {
        public string id;
        public string archetype;
        public float intensity;
        public List<string> manifestations;
        public Dictionary<string, float> associations;
        public bool isActive;
        public float dominance;

        public ConsciousnessPattern(string id, string archetype)
        {
            this.id = id;
            this.archetype = archetype;
            intensity = 0f;
            manifestations = new List<string>();
            associations = new Dictionary<string, float>();
            isActive = false;
            dominance = 0f;
        }
    }

    private List<ConsciousnessPattern> activePatterns;
    private Dictionary<string, float> archetypeStrengths;
    private Queue<string> consciousnessStream;
    private const int STREAM_CAPACITY = 20;
    private float analyzedDepth = 0f;

    private void Awake()
    {
        InitializeAnalyzer();
    }

    private void InitializeAnalyzer()
    {
        activePatterns = new List<ConsciousnessPattern>();
        archetypeStrengths = new Dictionary<string, float>();
        consciousnessStream = new Queue<string>();
        InitializeArchetypes();
    }

    private void InitializeArchetypes()
    {
        // Initialize Jungian archetypes
        AddArchetype(new ConsciousnessPattern(
            "shadow_self",
            "Shadow"
        ));

        AddArchetype(new ConsciousnessPattern(
            "anima_animus",
            "Inner Self"
        ));

        AddArchetype(new ConsciousnessPattern(
            "wise_old_man",
            "Guide"
        ));

        AddArchetype(new ConsciousnessPattern(
            "trickster",
            "Deceiver"
        ));

        // Initialize psychological archetypes
        AddArchetype(new ConsciousnessPattern(
            "primal_fear",
            "Fear"
        ));

        AddArchetype(new ConsciousnessPattern(
            "collective_unconscious",
            "Shared Fear"
        ));
    }

    public void AnalyzeConsciousness(PlayerAnalysisProfile profile, string[] recentActions)
    {
        // Update consciousness stream
        foreach (string action in recentActions)
        {
            UpdateConsciousnessStream(action);
        }

        // Analyze current psychological state
        AnalyzePsychologicalState(profile);

        // Update archetype strengths
        UpdateArchetypeStrengths(profile);

        // Process manifestations
        ProcessArchetypalManifestations();

        // Update analyzed depth
        UpdateAnalyzedDepth(profile);
    }

    private void UpdateConsciousnessStream(string input)
    {
        consciousnessStream.Enqueue(input);
        while (consciousnessStream.Count > STREAM_CAPACITY)
        {
            consciousnessStream.Dequeue();
        }
    }

    private void AnalyzePsychologicalState(PlayerAnalysisProfile profile)
    {
        foreach (var pattern in activePatterns)
        {
            switch (pattern.archetype)
            {
                case "Shadow":
                    pattern.intensity = CalculateShadowIntensity(profile);
                    break;
                case "Inner Self":
                    pattern.intensity = CalculateInnerSelfIntensity(profile);
                    break;
                case "Fear":
                    pattern.intensity = CalculatePrimalFearIntensity(profile);
                    break;
                // Add more archetype calculations
            }

            pattern.isActive = pattern.intensity > 0.5f;
            UpdatePatternManifestations(pattern, profile);
        }
    }

    private float CalculateShadowIntensity(PlayerAnalysisProfile profile)
    {
        return Mathf.Clamp01(
            (profile.AggressionLevel * 0.5f) +
            (profile.ObsessionLevel * 0.3f) +
            (profile.FearLevel * 0.2f)
        );
    }

    private float CalculateInnerSelfIntensity(PlayerAnalysisProfile profile)
    {
        return Mathf.Clamp01(
            (profile.ObsessionLevel * 0.4f) +
            (profile.CuriosityLevel * 0.4f) +
            (profile.FearLevel * 0.2f)
        );
    }

    private float CalculatePrimalFearIntensity(PlayerAnalysisProfile profile)
    {
        return Mathf.Clamp01(
            (profile.FearLevel * 0.6f) +
            (profile.AggressionLevel * 0.2f) +
            (profile.ObsessionLevel * 0.2f)
        );
    }

    private void UpdatePatternManifestations(ConsciousnessPattern pattern, PlayerAnalysisProfile profile)
    {
        pattern.manifestations.Clear();

        if (pattern.intensity > 0.7f)
        {
            switch (pattern.archetype)
            {
                case "Shadow":
                    GenerateShadowManifestations(pattern, profile);
                    break;
                case "Inner Self":
                    GenerateInnerSelfManifestations(pattern, profile);
                    break;
                case "Fear":
                    GenerateFearManifestations(pattern, profile);
                    break;
            }
        }
    }

    private void GenerateShadowManifestations(ConsciousnessPattern pattern, PlayerAnalysisProfile profile)
    {
        if (profile.AggressionLevel > 0.6f)
        {
            pattern.manifestations.Add("Violent thoughts begin to take physical form...");
        }
        if (profile.ObsessionLevel > 0.6f)
        {
            pattern.manifestations.Add("Your darkest obsessions manifest in the shadows...");
        }
        if (profile.FearLevel > 0.6f)
        {
            pattern.manifestations.Add("Your fears coalesce into a dark reflection...");
        }
    }

    private void GenerateInnerSelfManifestations(ConsciousnessPattern pattern, PlayerAnalysisProfile profile)
    {
        if (profile.ObsessionLevel > 0.6f)
        {
            pattern.manifestations.Add("Your inner voice grows louder, more insistent...");
        }
        if (profile.CuriosityLevel > 0.6f)
        {
            pattern.manifestations.Add("Reality bends to reveal deeper truths...");
        }
    }

    private void GenerateFearManifestations(ConsciousnessPattern pattern, PlayerAnalysisProfile profile)
    {
        if (profile.FearLevel > 0.7f)
        {
            pattern.manifestations.Add("Primal terrors emerge from your unconscious...");
        }
        if (profile.AggressionLevel > 0.6f)
        {
            pattern.manifestations.Add("Your fear transforms into a defensive rage...");
        }
    }

    private void UpdateArchetypeStrengths(PlayerAnalysisProfile profile)
    {
        foreach (var pattern in activePatterns)
        {
            if (!archetypeStrengths.ContainsKey(pattern.archetype))
            {
                archetypeStrengths[pattern.archetype] = 0f;
            }

            // Update strength based on pattern intensity and profile
            float newStrength = Mathf.Lerp(
                archetypeStrengths[pattern.archetype],
                pattern.intensity,
                Time.deltaTime
            );

            archetypeStrengths[pattern.archetype] = newStrength;
            pattern.dominance = CalculateArchetypeDominance(pattern);
        }
    }

    private float CalculateArchetypeDominance(ConsciousnessPattern pattern)
    {
        float totalStrength = archetypeStrengths.Values.Sum();
        return totalStrength > 0 ? 
            archetypeStrengths[pattern.archetype] / totalStrength : 
            0f;
    }

    private void ProcessArchetypalManifestations()
    {
        var dominantPattern = activePatterns
            .Where(p => p.isActive)
            .OrderByDescending(p => p.dominance)
            .FirstOrDefault();

        if (dominantPattern != null && dominantPattern.manifestations.Count > 0)
        {
            string manifestation = dominantPattern.manifestations[
                Random.Range(0, dominantPattern.manifestations.Count)
            ];

            var effect = new PsychologicalEffect(
                PsychologicalEffect.EffectType.Hallucination,
                dominantPattern.intensity,
                manifestation,
                $"archetype_{dominantPattern.archetype.ToLower()}"
            );

            GameManager.Instance.UIManager.GetComponent<UIEffects>()
                .ApplyPsychologicalEffect(effect);
        }
    }

    private void UpdateAnalyzedDepth(PlayerAnalysisProfile profile)
    {
        // Increase analyzed depth based on psychological engagement
        float depthIncrease = Time.deltaTime * (
            profile.ObsessionLevel * 0.4f +
            profile.CuriosityLevel * 0.3f +
            profile.FearLevel * 0.3f
        );

        analyzedDepth = Mathf.Clamp01(analyzedDepth + depthIncrease);

        // Trigger deep consciousness events at certain thresholds
        if (analyzedDepth > 0.8f && Random.value < 0.1f)
        {
            TriggerDeepConsciousnessEvent(profile);
        }
    }

    private void TriggerDeepConsciousnessEvent(PlayerAnalysisProfile profile)
    {
        var dominantArchetype = activePatterns
            .OrderByDescending(p => p.dominance)
            .First();

        string eventDescription = GenerateDeepEventDescription(dominantArchetype, profile);

        var effect = new PsychologicalEffect(
            PsychologicalEffect.EffectType.MemoryAlter,
            analyzedDepth,
            eventDescription,
            "deep_consciousness"
        );

        GameManager.Instance.UIManager.GetComponent<UIEffects>()
            .ApplyPsychologicalEffect(effect);

        // Reset depth after significant event
        analyzedDepth *= 0.5f;
    }

    private string GenerateDeepEventDescription(ConsciousnessPattern pattern, PlayerAnalysisProfile profile)
    {
        string[] deepDescriptions = {
            $"Your {pattern.archetype} self emerges from the depths...",
            "The boundaries between conscious and unconscious blur...",
            "Ancient psychological patterns surface in your mind...",
            "Your deepest fears and desires merge into one..."
        };

        return deepDescriptions[Random.Range(0, deepDescriptions.Length)];
    }

    private void AddArchetype(ConsciousnessPattern pattern)
    {
        if (!activePatterns.Any(p => p.id == pattern.id))
        {
            activePatterns.Add(pattern);
            archetypeStrengths[pattern.archetype] = 0f;
        }
    }

    public List<ConsciousnessPattern> GetActivePatterns()
    {
        return activePatterns.Where(p => p.isActive).ToList();
    }

    public float GetAnalyzedDepth()
    {
        return analyzedDepth;
    }

    public void ResetAnalyzer()
    {
        activePatterns.Clear();
        archetypeStrengths.Clear();
        consciousnessStream.Clear();
        analyzedDepth = 0f;
        InitializeArchetypes();
    }
}