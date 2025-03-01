using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

public class LexicalVariationSystem : MonoBehaviour
{
    private LLMManager llmManager;
    private Dictionary<string, LexicalTheme> thematicLexicons;
    private Dictionary<string, float> intensityModifiers;
    private Queue<LexicalRequest> processingQueue;

    [System.Serializable]
    public class LexicalTheme
    {
        public string id;
        public string description;
        public Dictionary<string, List<string>> wordGroups;
        public Dictionary<string, float> emotionalWeights;
        public List<string> contextualTriggers;
        public int usageCount;
        public float effectivenessScore;
    }

    [System.Serializable]
    public class LexicalRequest
    {
        public string originalText;
        public string themeId;
        public float targetIntensity;
        public string[] requiredElements;
        public Dictionary<string, float> psychologicalWeights;
        public System.Action<LexicalResult> callback;
    }

    [System.Serializable]
    public class LexicalResult
    {
        public string variedText;
        public Dictionary<string, float> impacts;
        public string[] usedThemes;
        public float psychologicalResonance;
        public string[] suggestions;
    }

    private void Awake()
    {
        llmManager = GameManager.Instance.LLMManager;
        thematicLexicons = new Dictionary<string, LexicalTheme>();
        intensityModifiers = new Dictionary<string, float>();
        processingQueue = new Queue<LexicalRequest>();
        
        InitializeLexicons();
        InitializeModifiers();
    }

    private async void InitializeLexicons()
    {
        // Initialize core horror lexicons
        await CreateLexicon("cosmic_horror", new Dictionary<string, float> {
            { "existential", 0.9f },
            { "incomprehensible", 0.8f },
            { "infinite", 0.7f }
        });

        await CreateLexicon("psychological_horror", new Dictionary<string, float> {
            { "paranoia", 0.8f },
            { "intrusive", 0.7f },
            { "deterioration", 0.9f }
        });

        await CreateLexicon("personal_horror", new Dictionary<string, float> {
            { "intimate", 0.9f },
            { "identity", 0.8f },
            { "trauma", 0.7f }
        });

        await CreateLexicon("environmental_horror", new Dictionary<string, float> {
            { "atmospheric", 0.8f },
            { "sensory", 0.7f },
            { "spatial", 0.9f }
        });
    }

    private async Task CreateLexicon(string id, Dictionary<string, float> emotionalWeights)
    {
        string prompt = $"Generate psychological horror lexicon for theme: {id}\n" +
                       "Include word groups for various intensities and contexts.\n" +
                       $"Emotional weights: {string.Join(", ", emotionalWeights.Keys)}\n" +
                       "Focus on psychological impact and horror effectiveness.";

        string response = await llmManager.GenerateResponse(prompt, "lexicon_generation");
        var lexicon = await ParseLexiconResponse(response, id, emotionalWeights);
        
        thematicLexicons[id] = lexicon;
    }

    private async Task<LexicalTheme> ParseLexiconResponse(string response, string id, Dictionary<string, float> weights)
    {
        string structurePrompt = $"Structure this horror lexicon into clear categories:\n{response}";
        string structured = await llmManager.GenerateResponse(structurePrompt, "lexicon_structuring");

        return new LexicalTheme
        {
            id = id,
            description = ExtractDescription(structured),
            wordGroups = ParseWordGroups(structured),
            emotionalWeights = weights,
            contextualTriggers = ExtractTriggers(structured),
            usageCount = 0,
            effectivenessScore = 0.5f
        };
    }

    private void InitializeModifiers()
    {
        // Base intensity modifiers
        intensityModifiers["subtle"] = 0.3f;
        intensityModifiers["mounting"] = 0.5f;
        intensityModifiers["intense"] = 0.7f;
        intensityModifiers["overwhelming"] = 0.9f;
        
        // Psychological modifiers
        intensityModifiers["intrusive"] = 0.8f;
        intensityModifiers["haunting"] = 0.7f;
        intensityModifiers["destabilizing"] = 0.9f;
        intensityModifiers["shattering"] = 1.0f;
    }

    private string ExtractDescription(string structured)
    {
        // Implementation would extract description from LLM response
        return "Default description";
    }

    private Dictionary<string, List<string>> ParseWordGroups(string structured)
    {
        // Implementation would parse word groups from LLM response
        return new Dictionary<string, List<string>>();
    }

    private string[] ExtractTriggers(string structured)
    {
        // Implementation would extract triggers from LLM response
        return new string[0];
    }

    public async Task<LexicalResult> GenerateVariation(string text, string themeId, float intensity)
    {
        var request = new LexicalRequest
        {
            originalText = text,
            themeId = themeId,
            targetIntensity = intensity,
            requiredElements = await IdentifyRequiredElements(text),
            psychologicalWeights = await AnalyzePsychologicalWeights(text)
        };

        var completionSource = new TaskCompletionSource<LexicalResult>();
        request.callback = result => completionSource.SetResult(result);
        
        processingQueue.Enqueue(request);
        
        if (processingQueue.Count == 1)
        {
            ProcessQueue();
        }

        return await completionSource.Task;
    }

    private async Task<string[]> IdentifyRequiredElements(string text)
    {
        string prompt = "Identify essential psychological horror elements in:\n" +
                       $"{text}\n\n" +
                       "Focus on:\n" +
                       "- Core psychological themes\n" +
                       "- Key emotional triggers\n" +
                       "- Critical narrative elements";

        string response = await llmManager.GenerateResponse(prompt, "element_identification");
        return ParseElements(response);
    }

    private string[] ParseElements(string response)
    {
        // Implementation would parse elements from LLM response
        return new string[0];
    }

    private async Task<Dictionary<string, float>> AnalyzePsychologicalWeights(string text)
    {
        string prompt = "Analyze psychological weights in this horror text:\n" +
                       $"{text}\n\n" +
                       "Consider:\n" +
                       "- Emotional impact\n" +
                       "- Psychological depth\n" +
                       "- Horror effectiveness";

        string response = await llmManager.GenerateResponse(prompt, "weight_analysis");
        return ParseWeights(response);
    }

    private Dictionary<string, float> ParseWeights(string response)
    {
        // Implementation would parse weights from LLM response
        return new Dictionary<string, float>();
    }

    private async void ProcessQueue()
    {
        while (processingQueue.Count > 0)
        {
            var request = processingQueue.Peek();
            var result = await ProcessRequest(request);
            
            request.callback?.Invoke(result);
            processingQueue.Dequeue();
        }
    }

    private async Task<LexicalResult> ProcessRequest(LexicalRequest request)
    {
        // Get appropriate lexicon
        if (!thematicLexicons.TryGetValue(request.themeId, out LexicalTheme lexicon))
        {
            Debug.LogError($"Lexicon not found: {request.themeId}");
            return CreateErrorResult(request);
        }

        // Generate variation
        string variationPrompt = await BuildVariationPrompt(request, lexicon);
        string response = await llmManager.GenerateResponse(variationPrompt, "variation_generation");
        
        // Process and validate variation
        return await ProcessVariation(response, request, lexicon);
    }

    private async Task<string> BuildVariationPrompt(LexicalRequest request, LexicalTheme lexicon)
    {
        var contextBuilder = new System.Text.StringBuilder();
        
        // Add original text
        contextBuilder.AppendLine($"Original Text: {request.originalText}");
        
        // Add theme context
        contextBuilder.AppendLine($"Theme: {lexicon.description}");
        contextBuilder.AppendLine("Word Groups:");
        foreach (var group in lexicon.wordGroups)
        {
            contextBuilder.AppendLine($"- {group.Key}: {string.Join(", ", group.Value)}");
        }
        
        // Add psychological context
        var profile = GameManager.Instance.ProfileManager.CurrentProfile;
        contextBuilder.AppendLine("\nPsychological State:");
        contextBuilder.AppendLine($"Fear Level: {profile.FearLevel}");
        contextBuilder.AppendLine($"Obsession Level: {profile.ObsessionLevel}");
        contextBuilder.AppendLine($"Aggression Level: {profile.AggressionLevel}");
        
        // Add requirements
        contextBuilder.AppendLine("\nRequirements:");
        contextBuilder.AppendLine($"Target Intensity: {request.targetIntensity}");
        contextBuilder.AppendLine($"Required Elements: {string.Join(", ", request.requiredElements)}");

        return contextBuilder.ToString();
    }

    private async Task<LexicalResult> ProcessVariation(string variation, LexicalRequest request, LexicalTheme lexicon)
    {
        // Analyze variation
        var analysis = await AnalyzeVariation(variation, request.originalText);
        
        // Update lexicon effectiveness
        UpdateLexiconEffectiveness(lexicon, analysis);
        
        return new LexicalResult
        {
            variedText = variation,
            impacts = analysis,
            usedThemes = new[] { lexicon.id },
            psychologicalResonance = CalculateResonance(analysis),
            suggestions = await GenerateSuggestions(variation, analysis)
        };
    }

    private async Task<Dictionary<string, float>> AnalyzeVariation(string variation, string original)
    {
        string prompt = "Analyze this horror text variation:\n" +
                       $"Original: {original}\n" +
                       $"Variation: {variation}\n\n" +
                       "Evaluate:\n" +
                       "- Psychological impact\n" +
                       "- Horror effectiveness\n" +
                       "- Emotional resonance\n" +
                       "- Thematic coherence";

        string response = await llmManager.GenerateResponse(prompt, "variation_analysis");
        return ParseAnalysis(response);
    }

    private Dictionary<string, float> ParseAnalysis(string response)
    {
        // Implementation would parse analysis from LLM response
        return new Dictionary<string, float>();
    }

    private void UpdateLexiconEffectiveness(LexicalTheme lexicon, Dictionary<string, float> analysis)
    {
        // Update usage count
        lexicon.usageCount++;
        
        // Update effectiveness score
        float analysisScore = analysis.Values.Average();
        lexicon.effectivenessScore = Mathf.Lerp(
            lexicon.effectivenessScore,
            analysisScore,
            0.2f
        );
    }

    private float CalculateResonance(Dictionary<string, float> analysis)
    {
        if (analysis.Count == 0) return 0f;
        
        float psychological = analysis.GetValueOrDefault("psychological_impact", 0f);
        float emotional = analysis.GetValueOrDefault("emotional_resonance", 0f);
        float horror = analysis.GetValueOrDefault("horror_effectiveness", 0f);
        
        return (psychological * 0.4f + emotional * 0.3f + horror * 0.3f);
    }

    private async Task<string[]> GenerateSuggestions(string variation, Dictionary<string, float> analysis)
    {
        if (CalculateResonance(analysis) > 0.7f)
            return new string[0];

        string prompt = "Generate improvement suggestions for this horror text:\n" +
                       $"{variation}\n\n" +
                       "Current Metrics:\n" +
                       string.Join("\n", analysis.Select(a => $"- {a.Key}: {a.Value}"));

        string response = await llmManager.GenerateResponse(prompt, "suggestion_generation");
        return ParseSuggestions(response);
    }

    private string[] ParseSuggestions(string response)
    {
        // Implementation would parse suggestions from LLM response
        return new string[0];
    }

    private LexicalResult CreateErrorResult(LexicalRequest request)
    {
        return new LexicalResult
        {
            variedText = request.originalText,
            impacts = new Dictionary<string, float>(),
            usedThemes = new string[0],
            psychologicalResonance = 0f,
            suggestions = new[] { "Error: Lexicon not found" }
        };
    }

    public List<string> GetEffectiveLexicons(float threshold = 0.7f)
    {
        return thematicLexicons.Values
            .Where(l => l.effectivenessScore >= threshold)
            .OrderByDescending(l => l.effectivenessScore)
            .Select(l => l.id)
            .ToList();
    }

    public void ClearIneffectiveLexicons(float threshold = 0.3f)
    {
        var ineffectiveKeys = thematicLexicons.Keys
            .Where(k => thematicLexicons[k].effectivenessScore < threshold)
            .ToList();

        foreach (var key in ineffectiveKeys)
        {
            thematicLexicons.Remove(key);
        }
    }
}