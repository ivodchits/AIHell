using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

public class ThematicResonanceSystem : MonoBehaviour
{
    private LLMManager llmManager;
    private GameStateManager gameStateManager;
    private Dictionary<string, ThematicElement> thematicElements;
    private List<ResonanceNode> resonanceNetwork;
    private Dictionary<string, float> thematicWeights;
    private float minResonanceThreshold = 0.4f;

    [System.Serializable]
    public class ThematicElement
    {
        public string id;
        public string type;
        public string content;
        public float resonanceValue;
        public Dictionary<string, float> psychologicalWeights;
        public List<string> connections;
        public AnimationCurve developmentCurve;
        public System.DateTime timestamp;
    }

    [System.Serializable]
    public class ResonanceNode
    {
        public string sourceId;
        public string targetId;
        public float strength;
        public string resonanceType;
        public Dictionary<string, float> contextualFactors;
        public bool isActive;
    }

    [System.Serializable]
    public class ResonanceResult
    {
        public bool isResonant;
        public float overallStrength;
        public Dictionary<string, float> thematicScores;
        public string[] strongestConnections;
        public string[] suggestedEnhancements;
    }

    [System.Serializable]
    public class ThematicAnalysis
    {
        public Dictionary<string, float> thematicStrengths;
        public Dictionary<string, List<string>> connections;
        public float psychologicalDepth;
        public string[] coreThemes;
        public string[] potentialDevelopments;
    }

    private void Awake()
    {
        llmManager = GameManager.Instance.LLMManager;
        gameStateManager = GameManager.Instance.StateManager;
        thematicElements = new Dictionary<string, ThematicElement>();
        resonanceNetwork = new List<ResonanceNode>();
        thematicWeights = new Dictionary<string, float>();
        
        InitializeThematicWeights();
    }

    private void InitializeThematicWeights()
    {
        // Core psychological horror weights
        thematicWeights["existential_dread"] = 0.9f;
        thematicWeights["psychological_decay"] = 0.8f;
        thematicWeights["personal_horror"] = 0.85f;
        thematicWeights["reality_distortion"] = 0.75f;
        thematicWeights["cosmic_horror"] = 0.7f;
        thematicWeights["body_horror"] = 0.65f;
        thematicWeights["social_isolation"] = 0.8f;
        thematicWeights["paranoia"] = 0.85f;
    }

    public async Task<ResonanceResult> AnalyzeResonance(string content, string type)
    {
        // First, analyze the content
        var analysis = await AnalyzeContent(content);
        
        // Check existing thematic connections
        var connections = await FindThematicConnections(content, analysis);
        
        // Calculate resonance strength
        var strength = CalculateResonanceStrength(analysis, connections);
        
        // Generate result
        return await CreateResonanceResult(content, analysis, connections, strength);
    }

    private async Task<ThematicAnalysis> AnalyzeContent(string content)
    {
        string prompt = "Analyze this psychological horror content for thematic elements:\n" +
                       $"{content}\n\n" +
                       "Consider:\n" +
                       "- Psychological themes\n" +
                       "- Horror elements\n" +
                       "- Emotional resonance\n" +
                       "- Thematic depth";

        string response = await llmManager.GenerateResponse(prompt, "thematic_analysis");
        return await ProcessThematicAnalysis(response);
    }

    private async Task<ThematicAnalysis> ProcessThematicAnalysis(string analysis)
    {
        string structurePrompt = $"Structure this horror thematic analysis:\n{analysis}";
        string structured = await llmManager.GenerateResponse(structurePrompt, "analysis_structuring");

        return new ThematicAnalysis
        {
            thematicStrengths = ParseThematicStrengths(structured),
            connections = ParseThematicConnections(structured),
            psychologicalDepth = ExtractPsychologicalDepth(structured),
            coreThemes = ExtractCoreThemes(structured),
            potentialDevelopments = ExtractPotentialDevelopments(structured)
        };
    }

    private Dictionary<string, float> ParseThematicStrengths(string structured)
    {
        // Implementation would parse thematic strengths from LLM response
        return new Dictionary<string, float>();
    }

    private Dictionary<string, List<string>> ParseThematicConnections(string structured)
    {
        // Implementation would parse thematic connections from LLM response
        return new Dictionary<string, List<string>>();
    }

    private float ExtractPsychologicalDepth(string structured)
    {
        // Implementation would extract psychological depth from LLM response
        return 0.5f;
    }

    private string[] ExtractCoreThemes(string structured)
    {
        // Implementation would extract core themes from LLM response
        return new string[0];
    }

    private string[] ExtractPotentialDevelopments(string structured)
    {
        // Implementation would extract potential developments from LLM response
        return new string[0];
    }

    private async Task<List<ResonanceNode>> FindThematicConnections(string content, ThematicAnalysis analysis)
    {
        var connections = new List<ResonanceNode>();
        
        foreach (var element in thematicElements.Values)
        {
            float resonance = await CalculateElementResonance(content, element, analysis);
            if (resonance >= minResonanceThreshold)
            {
                connections.Add(new ResonanceNode
                {
                    sourceId = System.Guid.NewGuid().ToString(),
                    targetId = element.id,
                    strength = resonance,
                    resonanceType = DetermineResonanceType(analysis, element),
                    contextualFactors = CalculateContextualFactors(analysis, element),
                    isActive = true
                });
            }
        }

        return connections;
    }

    private async Task<float> CalculateElementResonance(string content, ThematicElement element, ThematicAnalysis analysis)
    {
        string prompt = "Calculate thematic resonance between:\n" +
                       $"New Content: {content}\n" +
                       $"Existing Element: {element.content}\n\n" +
                       "Consider:\n" +
                       "- Thematic overlaps\n" +
                       "- Psychological connections\n" +
                       "- Horror elements\n" +
                       "- Emotional impact";

        string response = await llmManager.GenerateResponse(prompt, "resonance_calculation");
        return ParseResonanceValue(response);
    }

    private float ParseResonanceValue(string response)
    {
        // Implementation would parse resonance value from LLM response
        return 0.5f;
    }

    private string DetermineResonanceType(ThematicAnalysis analysis, ThematicElement element)
    {
        var sharedThemes = analysis.coreThemes.Intersect(
            element.connections
        ).ToList();

        if (sharedThemes.Contains("existential_dread"))
            return "existential";
        if (sharedThemes.Contains("psychological_decay"))
            return "psychological";
        if (sharedThemes.Contains("personal_horror"))
            return "personal";
            
        return "general";
    }

    private Dictionary<string, float> CalculateContextualFactors(ThematicAnalysis analysis, ThematicElement element)
    {
        var factors = new Dictionary<string, float>();
        
        // Calculate thematic overlap
        factors["thematic_overlap"] = analysis.coreThemes
            .Intersect(element.connections)
            .Count() / (float)analysis.coreThemes.Length;
        
        // Calculate psychological alignment
        factors["psychological_alignment"] = CalculatePsychologicalAlignment(
            analysis.thematicStrengths,
            element.psychologicalWeights
        );
        
        // Calculate temporal relevance
        factors["temporal_relevance"] = CalculateTemporalRelevance(element.timestamp);
        
        return factors;
    }

    private float CalculatePsychologicalAlignment(
        Dictionary<string, float> newWeights,
        Dictionary<string, float> existingWeights)
    {
        float alignment = 0f;
        int count = 0;
        
        foreach (var weight in newWeights)
        {
            if (existingWeights.TryGetValue(weight.Key, out float existingWeight))
            {
                alignment += 1f - Mathf.Abs(weight.Value - existingWeight);
                count++;
            }
        }
        
        return count > 0 ? alignment / count : 0f;
    }

    private float CalculateTemporalRelevance(System.DateTime timestamp)
    {
        var timeDifference = System.DateTime.Now - timestamp;
        return Mathf.Exp(-(float)timeDifference.TotalHours / 24f);
    }

    private float CalculateResonanceStrength(ThematicAnalysis analysis, List<ResonanceNode> connections)
    {
        if (connections.Count == 0)
            return analysis.psychologicalDepth;

        float totalStrength = connections.Sum(c => c.strength);
        float averageStrength = totalStrength / connections.Count;
        
        return Mathf.Lerp(
            analysis.psychologicalDepth,
            averageStrength,
            0.7f
        );
    }

    private async Task<ResonanceResult> CreateResonanceResult(
        string content,
        ThematicAnalysis analysis,
        List<ResonanceNode> connections,
        float strength)
    {
        var result = new ResonanceResult
        {
            isResonant = strength >= minResonanceThreshold,
            overallStrength = strength,
            thematicScores = analysis.thematicStrengths,
            strongestConnections = connections
                .OrderByDescending(c => c.strength)
                .Take(3)
                .Select(c => c.targetId)
                .ToArray()
        };

        // Generate enhancement suggestions if needed
        if (strength < 0.7f)
        {
            result.suggestedEnhancements = await GenerateEnhancements(content, analysis);
        }

        return result;
    }

    private async Task<string[]> GenerateEnhancements(string content, ThematicAnalysis analysis)
    {
        string prompt = "Generate psychological horror enhancements for:\n" +
                       $"{content}\n\n" +
                       "Current Analysis:\n" +
                       $"Psychological Depth: {analysis.psychologicalDepth}\n" +
                       $"Core Themes: {string.Join(", ", analysis.coreThemes)}\n\n" +
                       "Suggest improvements for:\n" +
                       "- Thematic resonance\n" +
                       "- Psychological impact\n" +
                       "- Horror effectiveness";

        string response = await llmManager.GenerateResponse(prompt, "enhancement_generation");
        return ParseEnhancements(response);
    }

    private string[] ParseEnhancements(string response)
    {
        // Implementation would parse enhancements from LLM response
        return new string[0];
    }

    public async Task RegisterThematicElement(string content, string type)
    {
        var analysis = await AnalyzeContent(content);
        var element = new ThematicElement
        {
            id = System.Guid.NewGuid().ToString(),
            type = type,
            content = content,
            resonanceValue = analysis.psychologicalDepth,
            psychologicalWeights = analysis.thematicStrengths,
            connections = analysis.coreThemes.ToList(),
            developmentCurve = GenerateDevelopmentCurve(analysis),
            timestamp = System.DateTime.Now
        };

        thematicElements[element.id] = element;
        await UpdateResonanceNetwork(element);
    }

    private AnimationCurve GenerateDevelopmentCurve(ThematicAnalysis analysis)
    {
        AnimationCurve curve = new AnimationCurve();
        
        // Initial impact
        curve.AddKey(0f, analysis.psychologicalDepth);
        
        // Development points
        float developmentPoints = analysis.potentialDevelopments.Length;
        if (developmentPoints > 0)
        {
            float step = 1f / (developmentPoints + 1);
            for (int i = 0; i < developmentPoints; i++)
            {
                float time = step * (i + 1);
                float value = Mathf.Lerp(
                    analysis.psychologicalDepth,
                    1f,
                    (i + 1) / developmentPoints
                );
                curve.AddKey(time, value);
            }
        }
        
        // Final state
        curve.AddKey(1f, 1f);
        
        return curve;
    }

    private async Task UpdateResonanceNetwork(ThematicElement newElement)
    {
        foreach (var element in thematicElements.Values)
        {
            if (element.id == newElement.id)
                continue;

            float resonance = await CalculateElementResonance(
                newElement.content,
                element,
                await AnalyzeContent(element.content)
            );

            if (resonance >= minResonanceThreshold)
            {
                resonanceNetwork.Add(new ResonanceNode
                {
                    sourceId = newElement.id,
                    targetId = element.id,
                    strength = resonance,
                    resonanceType = "bidirectional",
                    contextualFactors = new Dictionary<string, float>(),
                    isActive = true
                });
            }
        }
    }

    public List<ThematicElement> GetResonantElements(float threshold = 0.6f)
    {
        return thematicElements.Values
            .Where(e => e.resonanceValue >= threshold)
            .OrderByDescending(e => e.resonanceValue)
            .ToList();
    }

    public void UpdateThematicWeight(string theme, float weight)
    {
        thematicWeights[theme] = Mathf.Clamp01(weight);
    }

    public void ClearStaleElements(float maxAge = 24f)
    {
        var now = System.DateTime.Now;
        var staleElements = thematicElements.Values
            .Where(e => (now - e.timestamp).TotalHours > maxAge)
            .ToList();

        foreach (var element in staleElements)
        {
            thematicElements.Remove(element.id);
            resonanceNetwork.RemoveAll(n => 
                n.sourceId == element.id || n.targetId == element.id
            );
        }
    }
}