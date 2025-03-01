using UnityEngine;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using AIHell.Core.Data;

public class RealityFilter : MonoBehaviour
{
    private Dictionary<string, float> realityAspects;
    private Dictionary<string, string> distortionPatterns;
    private List<string> activeFilters;
    private float globalDistortionLevel;
    private const float REALITY_THRESHOLD = 0.3f;

    private void Awake()
    {
        InitializeFilter();
    }

    private void InitializeFilter()
    {
        realityAspects = new Dictionary<string, float>
        {
            { "coherence", 1.0f },
            { "continuity", 1.0f },
            { "causality", 1.0f },
            { "physicality", 1.0f }
        };

        InitializeDistortionPatterns();
        activeFilters = new List<string>();
        globalDistortionLevel = 0f;
    }

    private void InitializeDistortionPatterns()
    {
        distortionPatterns = new Dictionary<string, string>
        {
            { "time_loop", @"(moments?|time|seconds?) (repeat|loop|cycle)" },
            { "space_warp", @"(walls?|room|space) (shift|move|change)" },
            { "identity_blur", @"(you|your|self) (change|blur|fade)" },
            { "reality_break", @"(reality|world|existence) (break|tear|split)" }
        };
    }

    public string FilterReality(string input, PlayerAnalysisProfile profile)
    {
        // Update reality aspects based on psychological state
        UpdateRealityAspects(profile);

        // Apply active distortions
        string filteredText = ApplyDistortions(input, profile);

        // Add psychological overlay
        filteredText = AddPsychologicalLayer(filteredText, profile);

        return filteredText;
    }

    private void UpdateRealityAspects(PlayerAnalysisProfile profile)
    {
        // Update coherence based on fear level
        realityAspects["coherence"] = Mathf.Lerp(
            1.0f, 0.2f,
            profile.FearLevel
        );

        // Update continuity based on obsession level
        realityAspects["continuity"] = Mathf.Lerp(
            1.0f, 0.3f,
            profile.ObsessionLevel
        );

        // Update causality based on overall psychological state
        realityAspects["causality"] = Mathf.Lerp(
            1.0f, 0.4f,
            (profile.FearLevel + profile.ObsessionLevel) / 2f
        );

        // Update physicality based on aggression level
        realityAspects["physicality"] = Mathf.Lerp(
            1.0f, 0.5f,
            profile.AggressionLevel
        );

        // Calculate global distortion
        globalDistortionLevel = 1f - realityAspects.Values.Average();
    }

    private string ApplyDistortions(string input, PlayerAnalysisProfile profile)
    {
        string result = input;

        // Apply temporal distortions
        if (realityAspects["continuity"] < REALITY_THRESHOLD)
        {
            result = ApplyTemporalDistortion(result);
        }

        // Apply spatial distortions
        if (realityAspects["physicality"] < REALITY_THRESHOLD)
        {
            result = ApplySpatialDistortion(result);
        }

        // Apply causal distortions
        if (realityAspects["causality"] < REALITY_THRESHOLD)
        {
            result = ApplyCausalDistortion(result);
        }

        // Apply coherence distortions
        if (realityAspects["coherence"] < REALITY_THRESHOLD)
        {
            result = ApplyCoherenceDistortion(result);
        }

        return result;
    }

    private string ApplyTemporalDistortion(string input)
    {
        // Add time-related anomalies
        string[] temporalMarkers = {
            "\nTime seems to stutter...",
            "\nMoments overlap impossibly...",
            "\nThe present feels uncertain..."
        };

        if (Random.value < globalDistortionLevel)
        {
            input += temporalMarkers[Random.Range(0, temporalMarkers.Length)];
        }

        // Replace temporal references
        input = Regex.Replace(input, 
            @"\b(now|then|before|after)\b",
            match => GetTemporalDistortion(match.Value)
        );

        return input;
    }

    private string GetTemporalDistortion(string temporal)
    {
        return temporal switch
        {
            "now" => "then-now-soon",
            "then" => "now-then-never",
            "before" => "after-before-during",
            "after" => "before-after-always",
            _ => temporal
        };
    }

    private string ApplySpatialDistortion(string input)
    {
        // Add spatial anomalies
        string[] spatialMarkers = {
            "\nThe geometry of the room defies understanding...",
            "\nSpace bends in impossible ways...",
            "\nDistances seem to shift with each glance..."
        };

        if (Random.value < globalDistortionLevel)
        {
            input += spatialMarkers[Random.Range(0, spatialMarkers.Length)];
        }

        // Replace spatial references
        input = Regex.Replace(input,
            @"\b(here|there|near|far)\b",
            match => GetSpatialDistortion(match.Value)
        );

        return input;
    }

    private string GetSpatialDistortion(string spatial)
    {
        return spatial switch
        {
            "here" => "here-there-everywhere",
            "there" => "nowhere-there-everywhere",
            "near" => "far-near-inside",
            "far" => "near-far-beyond",
            _ => spatial
        };
    }

    private string ApplyCausalDistortion(string input)
    {
        // Add causality anomalies
        string[] causalMarkers = {
            "\nCause and effect become meaningless...",
            "\nEvents occur in impossible sequences...",
            "\nReality's logic breaks down..."
        };

        if (Random.value < globalDistortionLevel)
        {
            input += causalMarkers[Random.Range(0, causalMarkers.Length)];
        }

        // Invert causal relationships
        input = Regex.Replace(input,
            @"(\w+) causes (\w+)",
            "$2 precedes $1"
        );

        return input;
    }

    private string ApplyCoherenceDistortion(string input)
    {
        // Add coherence breaks
        string[] coherenceMarkers = {
            "\nReality fragments into contradictions...",
            "\nLogic dissolves into chaos...",
            "\nMeaning slips away like water..."
        };

        if (Random.value < globalDistortionLevel)
        {
            input += coherenceMarkers[Random.Range(0, coherenceMarkers.Length)];
        }

        // Fragment sentences
        if (globalDistortionLevel > 0.7f)
        {
            input = FragmentText(input);
        }

        return input;
    }

    private string FragmentText(string input)
    {
        string[] sentences = input.Split('.');
        System.Text.StringBuilder result = new System.Text.StringBuilder();

        foreach (string sentence in sentences)
        {
            if (Random.value < globalDistortionLevel)
            {
                result.Append(FragmentSentence(sentence.Trim()));
            }
            else
            {
                result.Append(sentence);
            }
            result.Append(". ");
        }

        return result.ToString();
    }

    private string FragmentSentence(string sentence)
    {
        string[] words = sentence.Split(' ');
        if (words.Length <= 3) return sentence;

        System.Text.StringBuilder fragmented = new System.Text.StringBuilder();
        int breakPoint = Random.Range(2, words.Length - 1);

        for (int i = 0; i < words.Length; i++)
        {
            if (i == breakPoint)
            {
                fragmented.Append("... ");
            }
            fragmented.Append(words[i] + " ");
        }

        return fragmented.ToString().Trim();
    }

    private string AddPsychologicalLayer(string input, PlayerAnalysisProfile profile)
    {
        if (profile.FearLevel > 0.7f)
        {
            input = AddFearLayer(input);
        }

        if (profile.ObsessionLevel > 0.6f)
        {
            input = AddObsessionLayer(input);
        }

        if (profile.AggressionLevel > 0.5f)
        {
            input = AddAggressionLayer(input);
        }

        return input;
    }

    private string AddFearLayer(string input)
    {
        string[] fearPatterns = {
            @"\b(dark|shadow|night)\b",
            @"\b(quiet|silence|still)\b",
            @"\b(alone|empty|void)\b"
        };

        foreach (string pattern in fearPatterns)
        {
            input = Regex.Replace(input, pattern,
                match => $"{match.Value} (your fear magnifies its presence)");
        }

        return input;
    }

    private string AddObsessionLayer(string input)
    {
        string[] obsessionPatterns = {
            @"\b(pattern|repeat|cycle)\b",
            @"\b(watch|observe|see)\b",
            @"\b(think|remember|know)\b"
        };

        foreach (string pattern in obsessionPatterns)
        {
            input = Regex.Replace(input, pattern,
                match => $"{match.Value} (it draws your obsessive attention)");
        }

        return input;
    }

    private string AddAggressionLayer(string input)
    {
        string[] aggressionPatterns = {
            @"\b(break|destroy|damage)\b",
            @"\b(fight|resist|struggle)\b",
            @"\b(anger|rage|fury)\b"
        };

        foreach (string pattern in aggressionPatterns)
        {
            input = Regex.Replace(input, pattern,
                match => $"{match.Value} (your aggression resonates with it)");
        }

        return input;
    }

    public void AddActiveFilter(string filterType)
    {
        if (!activeFilters.Contains(filterType))
        {
            activeFilters.Add(filterType);
        }
    }

    public void RemoveActiveFilter(string filterType)
    {
        activeFilters.Remove(filterType);
    }

    public float GetRealityAspect(string aspect)
    {
        return realityAspects.TryGetValue(aspect, out float value) ? value : 1f;
    }

    public float GetGlobalDistortion()
    {
        return globalDistortionLevel;
    }

    public void ResetFilter()
    {
        foreach (var aspect in realityAspects.Keys.ToList())
        {
            realityAspects[aspect] = 1f;
        }
        activeFilters.Clear();
        globalDistortionLevel = 0f;
    }
}