using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using AIHell.Core.Data;

public class PatternRecognitionSystem : MonoBehaviour
{
    private LLMManager llmManager;
    private List<PatternProfile> examplePatterns;
    private List<PatternInstance> activePatterns;

    [System.Serializable]
    public class PatternProfile
    {
        public string id;
        public string type;
        public string description;
        public float psychologicalWeight;
        public string[] visualTriggers;
        public string[] psychologicalTriggers;
        public string interpretation;
    }

    [System.Serializable]
    public class PatternInstance
    {
        public string profileId;
        public Vector2 position;
        public float intensity;
        public float timestamp;
        public bool isProcessed;
    }

    [System.Serializable]
    public class PatternAnalysisResult
    {
        public List<PatternInstance> detectedPatterns;
        public float overallIntensity;
        public string dominantType;
        public Dictionary<string, float> psychologicalImpact;
    }

    private async void Awake()
    {
        llmManager = GameManager.Instance.LLMManager;
        InitializeSystem();
        await InitializeExamplePatterns();
    }

    private void InitializeSystem()
    {
        activePatterns = new List<PatternInstance>();
    }

    private async Task InitializeExamplePatterns()
    {
        examplePatterns = new List<PatternProfile>
        {
            new PatternProfile {
                id = "pareidolia_example",
                type = "psychological",
                description = "Faces emerging from random patterns",
                psychologicalWeight = 0.8f,
                visualTriggers = new[] { "face-like shapes", "watching eyes", "human silhouettes" },
                psychologicalTriggers = new[] { "paranoia", "being watched", "hidden presence" },
                interpretation = "The mind perceiving watching entities in innocuous patterns"
            },
            // More examples as reference for LLM
        };

        // Generate initial dynamic patterns
        await GenerateNewPatternSet();
    }

    private async Task GenerateNewPatternSet()
    {
        string prompt = "Using the following example patterns as reference, generate new unique psychological horror patterns " +
                       "that could emerge in the game. Each pattern should have a unique psychological impact and visual manifestation:\n\n";
        
        foreach (var example in examplePatterns)
        {
            prompt += $"Example Pattern:\n" +
                     $"Type: {example.type}\n" +
                     $"Description: {example.description}\n" +
                     $"Visual Triggers: {string.Join(", ", example.visualTriggers)}\n" +
                     $"Psychological Impact: {example.interpretation}\n\n";
        }

        string response = await llmManager.GenerateResponse(prompt);
        ParseAndAddNewPatterns(response);
    }

    private void ParseAndAddNewPatterns(string llmResponse)
    {
        // Parse LLM response and create new pattern profiles
        // This would need proper parsing logic based on LLM output format
    }

    public async Task<PatternAnalysisResult> AnalyzeImage(Texture2D image, PlayerAnalysisProfile profile)
    {
        var result = new PatternAnalysisResult
        {
            detectedPatterns = new List<PatternInstance>(),
            psychologicalImpact = new Dictionary<string, float>()
        };

        // Basic image analysis
        for (int y = 0; y < image.height; y += 32)
        {
            for (int x = 0; x < image.width; x += 32)
            {
                AnalyzeImageSection(
                    image, 
                    new Vector2(x, y), 
                    new Vector2(32, 32),
                    result
                );
            }
        }

        // Generate unique interpretations using LLM
        await GeneratePatternInterpretations(result, profile);

        return result;
    }

    private void AnalyzeImageSection(Texture2D image, Vector2 position, Vector2 size, PatternAnalysisResult result)
    {
        Color[] pixels = image.GetPixels(
            (int)position.x,
            (int)position.y,
            (int)size.x,
            (int)size.y
        );

        foreach (var profile in examplePatterns)
        {
            if (DetectPattern(pixels, profile))
            {
                var pattern = new PatternInstance
                {
                    profileId = profile.id,
                    position = position,
                    intensity = CalculatePatternIntensity(pixels, profile),
                    timestamp = Time.time,
                    isProcessed = false
                };

                result.detectedPatterns.Add(pattern);
            }
        }
    }

    private bool DetectPattern(Color[] pixels, PatternProfile profile)
    {
        // Calculate pattern features
        float[] features = ExtractFeatures(pixels);
        
        // Compare against profile triggers
        return EvaluateFeatures(features, profile) > profile.psychologicalWeight;
    }

    private float[] ExtractFeatures(Color[] pixels)
    {
        float[] features = new float[5];

        // Edge density
        features[0] = CalculateEdgeDensity(pixels);
        
        // Color variation
        features[1] = CalculateColorVariation(pixels);
        
        // Pattern repetition
        features[2] = CalculatePatternRepetition(pixels);
        
        // Darkness level
        features[3] = CalculateDarknessLevel(pixels);
        
        // Texture complexity
        features[4] = CalculateTextureComplexity(pixels);

        return features;
    }

    private float CalculateEdgeDensity(Color[] pixels)
    {
        float edgeCount = 0;
        int width = 32;  // Assuming 32x32 sections

        for (int y = 1; y < width - 1; y++)
        {
            for (int x = 1; x < width - 1; x++)
            {
                int idx = y * width + x;
                float diff = Mathf.Abs(pixels[idx].grayscale - pixels[idx + 1].grayscale) +
                           Mathf.Abs(pixels[idx].grayscale - pixels[idx + width].grayscale);
                
                if (diff > 0.1f)
                    edgeCount++;
            }
        }

        return edgeCount / (width * width);
    }

    private float CalculateColorVariation(Color[] pixels)
    {
        Color average = Color.black;
        foreach (Color pixel in pixels)
        {
            average += pixel;
        }
        average /= pixels.Length;

        float variation = 0;
        foreach (Color pixel in pixels)
        {
            variation += (pixel - average).sqrMagnitude;
        }

        return Mathf.Sqrt(variation / pixels.Length);
    }

    private float CalculatePatternRepetition(Color[] pixels)
    {
        float repetition = 0;
        int width = 32;
        
        for (int y = 0; y < width - 8; y++)
        {
            for (int x = 0; x < width - 8; x++)
            {
                float similarity = CompareSections(
                    pixels, x, y,
                    pixels, x + 8, y + 8,
                    width
                );
                repetition = Mathf.Max(repetition, similarity);
            }
        }

        return repetition;
    }

    private float CompareSections(Color[] pixels, int x1, int y1, Color[] pixels2, int x2, int y2, int width)
    {
        float similarity = 0;
        int sampleSize = 4;

        for (int y = 0; y < sampleSize; y++)
        {
            for (int x = 0; x < sampleSize; x++)
            {
                int idx1 = (y1 + y) * width + (x1 + x);
                int idx2 = (y2 + y) * width + (x2 + x);
                
                similarity += 1f - (pixels[idx1] - pixels2[idx2]).magnitude;
            }
        }

        return similarity / (sampleSize * sampleSize);
    }

    private float CalculateDarknessLevel(Color[] pixels)
    {
        float darkness = 0;
        foreach (Color pixel in pixels)
        {
            darkness += 1f - pixel.grayscale;
        }
        return darkness / pixels.Length;
    }

    private float CalculateTextureComplexity(Color[] pixels)
    {
        float complexity = 0;
        int width = 32;

        for (int y = 1; y < width - 1; y++)
        {
            for (int x = 1; x < width - 1; x++)
            {
                int idx = y * width + x;
                float localVariation = 0;

                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        
                        int neighborIdx = (y + dy) * width + (x + dx);
                        localVariation += (pixels[idx] - pixels[neighborIdx]).magnitude;
                    }
                }

                complexity += localVariation;
            }
        }

        return complexity / (width * width);
    }

    private float EvaluateFeatures(float[] features, PatternProfile profile)
    {
        float score = 0;

        // Weight features based on pattern type
        switch (profile.type)
        {
            case "psychological":
                score = features[0] * 0.3f +  // Edge density
                        features[1] * 0.2f +  // Color variation
                        features[2] * 0.1f +  // Pattern repetition
                        features[3] * 0.2f +  // Darkness
                        features[4] * 0.2f;   // Complexity
                break;

            case "cosmic":
                score = features[0] * 0.2f +
                        features[1] * 0.1f +
                        features[2] * 0.4f +  // Emphasize repetition
                        features[3] * 0.2f +
                        features[4] * 0.1f;
                break;

            case "surreal":
                score = features[0] * 0.2f +
                        features[1] * 0.3f +  // Emphasize color variation
                        features[2] * 0.2f +
                        features[3] * 0.1f +
                        features[4] * 0.2f;
                break;

            case "dark_romanticism":
                score = features[0] * 0.2f +
                        features[1] * 0.1f +
                        features[2] * 0.1f +
                        features[3] * 0.4f +  // Emphasize darkness
                        features[4] * 0.2f;
                break;
        }

        return score;
    }

    private float CalculatePatternIntensity(Color[] pixels, PatternProfile profile)
    {
        float[] features = ExtractFeatures(pixels);
        float baseIntensity = EvaluateFeatures(features, profile);
        
        // Apply profile-specific intensity curve
        return baseIntensity;
    }

    private async Task GeneratePatternInterpretations(PatternAnalysisResult result, PlayerAnalysisProfile profile)
    {
        string prompt = $"Analyze the following pattern detections in the context of the player's psychological state:\n" +
                       $"Fear Level: {profile.FearLevel}\n" +
                       $"Obsession Level: {profile.ObsessionLevel}\n" +
                       $"Aggression Level: {profile.AggressionLevel}\n\n" +
                       "Detected Patterns:\n";

        foreach (var pattern in result.detectedPatterns)
        {
            prompt += $"- Pattern at position {pattern.position} with intensity {pattern.intensity}\n";
        }

        string interpretation = await llmManager.GenerateResponse(prompt);
        ProcessPatternInterpretation(interpretation, result);
    }

    private void ProcessPatternInterpretation(string interpretation, PatternAnalysisResult result)
    {
        // Update psychological impact based on LLM interpretation
        // This would need proper parsing logic based on LLM output format
    }

    private void ProcessAnalysisResults(PatternAnalysisResult result, PlayerAnalysisProfile profile)
    {
        float totalIntensity = 0;
        Dictionary<string, float> typeIntensities = new Dictionary<string, float>();

        foreach (var pattern in result.detectedPatterns)
        {
            if (examplePatterns.Find(p => p.id == pattern.profileId) is PatternProfile profile)
            {
                string type = profile.type;
                float impact = pattern.intensity * profile.psychologicalWeight;

                if (!typeIntensities.ContainsKey(type))
                    typeIntensities[type] = 0;

                typeIntensities[type] += impact;
                totalIntensity += impact;
            }
        }

        // Find dominant type
        string dominantType = "";
        float maxIntensity = 0;
        foreach (var pair in typeIntensities)
        {
            if (pair.Value > maxIntensity)
            {
                maxIntensity = pair.Value;
                dominantType = pair.Key;
            }
        }

        // Calculate psychological impact
        foreach (var pair in typeIntensities)
        {
            result.psychologicalImpact[pair.Key] = pair.Value / totalIntensity;
        }

        result.overallIntensity = totalIntensity;
        result.dominantType = dominantType;
    }

    public void UpdateActivePatterns()
    {
        float currentTime = Time.time;
        activePatterns.RemoveAll(p => currentTime - p.timestamp > 10f);
    }

    public List<PatternInstance> GetActivePatterns()
    {
        return activePatterns;
    }

    public void ResetSystem()
    {
        activePatterns.Clear();
    }

    public async Task<bool> ShouldGenerateNewPatterns()
    {
        // Ask LLM if it's time to introduce new patterns based on game state
        var gameState = GameManager.Instance.StateManager.GetCurrentState();
        string prompt = "Based on the current game state, should new psychological patterns be introduced?\n" +
                       $"Current Tension: {gameState.tensionLevel}\n" +
                       $"Time in Current Area: {gameState.timeInCurrentArea}\n" +
                       $"Recent Event Count: {gameState.recentEventCount}";

        string response = await llmManager.GenerateResponse(prompt);
        return response.ToLower().Contains("yes");
    }
}