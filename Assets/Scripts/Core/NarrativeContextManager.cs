using UnityEngine;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using AIHell.Core.Data;

public class NarrativeContextManager : MonoBehaviour
{
    private LLMManager llmManager;
    private GameStateManager gameStateManager;
    private Dictionary<string, NarrativeMemory> narrativeMemories;
    private Dictionary<string, ThematicElement> thematicElements;
    private List<string> significantEvents;
    private const int MAX_SIGNIFICANT_EVENTS = 10;

    [System.Serializable]
    public class NarrativeMemory
    {
        public string id;
        public string description;
        public float psychologicalWeight;
        public string[] associatedThemes;
        public float emotionalImpact;
        public System.DateTime timestamp;
        public Dictionary<string, float> thematicLinks;
    }

    [System.Serializable]
    public class ThematicElement
    {
        public string theme;
        public float currentStrength;
        public string[] manifestations;
        public string[] psychologicalTriggers;
        public AnimationCurve developmentCurve;
    }

    private async void Awake()
    {
        llmManager = GameManager.Instance.LLMManager;
        gameStateManager = GameManager.Instance.StateManager;
        narrativeMemories = new Dictionary<string, NarrativeMemory>();
        thematicElements = new Dictionary<string, ThematicElement>();
        significantEvents = new List<string>();

        await InitializeThematicElements();
    }

    private async Task InitializeThematicElements()
    {
        // First, generate initial thematic elements
        string prompt = "Generate psychological horror themes that can evolve throughout the game. " +
                       "Each theme should have psychological triggers and possible manifestations. " +
                       "Focus on themes that can be personally unsettling and adapt to the player's state.";

        string response = await llmManager.GenerateResponse(prompt, "theme_generation");
        await ProcessThematicElements(response);
    }

    private async Task ProcessThematicElements(string llmResponse)
    {
        try
        {
            // Initialize base themes
            var baseThemes = new Dictionary<string, ThematicElement>
            {
                { "isolation", new ThematicElement {
                    theme = "isolation",
                    currentStrength = 0.3f,
                    manifestations = new[] { "empty rooms", "distant sounds", "abandoned spaces" },
                    psychologicalTriggers = new[] { "loneliness", "abandonment", "silence" },
                    developmentCurve = AnimationCurve.EaseInOut(0, 0, 1, 1)
                }},
                { "paranoia", new ThematicElement {
                    theme = "paranoia",
                    currentStrength = 0.2f,
                    manifestations = new[] { "watching eyes", "moving shadows", "whispered voices" },
                    psychologicalTriggers = new[] { "surveillance", "pursuit", "observation" },
                    developmentCurve = AnimationCurve.EaseInOut(0, 0, 1, 1)
                }},
                { "decay", new ThematicElement {
                    theme = "decay",
                    currentStrength = 0.1f,
                    manifestations = new[] { "rotting structures", "crumbling walls", "corrupted spaces" },
                    psychologicalTriggers = new[] { "deterioration", "entropy", "aging" },
                    developmentCurve = AnimationCurve.EaseInOut(0, 0, 1, 1)
                }}
            };

            // Process LLM response to enhance or modify base themes
            var enhancedThemes = ParseThemeResponse(llmResponse);
            foreach (var theme in enhancedThemes)
            {
                if (baseThemes.ContainsKey(theme.Key))
                {
                    // Merge with existing theme
                    baseThemes[theme.Key].manifestations = baseThemes[theme.Key].manifestations
                        .Concat(theme.Value.manifestations)
                        .Distinct()
                        .ToArray();
                    baseThemes[theme.Key].psychologicalTriggers = baseThemes[theme.Key].psychologicalTriggers
                        .Concat(theme.Value.psychologicalTriggers)
                        .Distinct()
                        .ToArray();
                }
                else
                {
                    // Add new theme
                    baseThemes.Add(theme.Key, theme.Value);
                }
            }

            thematicElements = baseThemes;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error processing thematic elements: {ex.Message}");
            InitializeFallbackThemes();
        }
    }

    private Dictionary<string, ThematicElement> ParseThemeResponse(string response)
    {
        try
        {
            var themes = new Dictionary<string, ThematicElement>();
            var lines = response.Split('\n');
            ThematicElement currentTheme = null;

            foreach (var line in lines)
            {
                if (line.StartsWith("Theme:"))
                {
                    if (currentTheme != null)
                    {
                        themes[currentTheme.theme] = currentTheme;
                    }
                    currentTheme = new ThematicElement
                    {
                        theme = line.Replace("Theme:", "").Trim().ToLower(),
                        currentStrength = 0.1f,
                        developmentCurve = AnimationCurve.EaseInOut(0, 0, 1, 1)
                    };
                }
                else if (line.StartsWith("Manifestations:") && currentTheme != null)
                {
                    currentTheme.manifestations = line.Replace("Manifestations:", "")
                        .Split(',')
                        .Select(m => m.Trim())
                        .Where(m => !string.IsNullOrEmpty(m))
                        .ToArray();
                }
                else if (line.StartsWith("Triggers:") && currentTheme != null)
                {
                    currentTheme.psychologicalTriggers = line.Replace("Triggers:", "")
                        .Split(',')
                        .Select(t => t.Trim())
                        .Where(t => !string.IsNullOrEmpty(t))
                        .ToArray();
                }
            }

            if (currentTheme != null)
            {
                themes[currentTheme.theme] = currentTheme;
            }

            return themes;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error parsing theme response: {ex.Message}");
            return new Dictionary<string, ThematicElement>();
        }
    }

    private void InitializeFallbackThemes()
    {
        thematicElements = new Dictionary<string, ThematicElement>
        {
            { "psychological", new ThematicElement {
                theme = "psychological",
                currentStrength = 0.3f,
                manifestations = new[] { "unsettling atmosphere", "mental strain", "psychological pressure" },
                psychologicalTriggers = new[] { "stress", "anxiety", "fear" },
                developmentCurve = AnimationCurve.EaseInOut(0, 0, 1, 1)
            }}
        };
    }

    public async Task<string> GenerateContextualPrompt(string basePrompt, ContextType contextType)
    {
        var contextBuilder = new StringBuilder(basePrompt);
        contextBuilder.AppendLine("\nNarrative Context:");

        // Add relevant narrative memories
        var relevantMemories = GetRelevantMemories(contextType);
        foreach (var memory in relevantMemories)
        {
            contextBuilder.AppendLine($"- {memory.description}");
        }

        // Add active themes
        var activeThemes = GetActiveThemes();
        contextBuilder.AppendLine("\nActive Themes:");
        foreach (var theme in activeThemes)
        {
            contextBuilder.AppendLine($"- {theme.theme} (Strength: {theme.currentStrength})");
        }

        // Add psychological context
        await AddPsychologicalContext(contextBuilder);

        return contextBuilder.ToString();
    }

    private List<NarrativeMemory> GetRelevantMemories(ContextType contextType)
    {
        // Get current psychological state
        var profile = GameManager.Instance.ProfileManager.CurrentProfile;
        var gameState = gameStateManager.GetCurrentState();

        // Filter and sort memories based on relevance
        return narrativeMemories.Values
            .Where(m => IsMemoryRelevant(m, contextType, profile, gameState))
            .OrderByDescending(m => CalculateMemoryRelevance(m, contextType, profile))
            .Take(5)
            .ToList();
    }

    private bool IsMemoryRelevant(NarrativeMemory memory, ContextType contextType, PlayerAnalysisProfile profile, GameStateManager.GameState gameState)
    {
        switch (contextType)
        {
            case ContextType.Psychological:
                return memory.psychologicalWeight > 0.5f;
            
            case ContextType.Environmental:
                return memory.thematicLinks.ContainsKey("environment");
            
            case ContextType.Narrative:
                return memory.emotionalImpact > 0.3f;
            
            default:
                return true;
        }
    }

    private float CalculateMemoryRelevance(NarrativeMemory memory, ContextType contextType, PlayerAnalysisProfile profile)
    {
        float relevance = 0f;

        // Base relevance from psychological weight
        relevance += memory.psychologicalWeight * 0.4f;

        // Emotional impact
        relevance += memory.emotionalImpact * 0.3f;

        // Thematic relevance
        foreach (var theme in memory.associatedThemes)
        {
            if (thematicElements.TryGetValue(theme, out var thematicElement))
            {
                relevance += thematicElement.currentStrength * 0.2f;
            }
        }

        // Time decay
        float timeSinceMemory = (float)(System.DateTime.Now - memory.timestamp).TotalMinutes;
        float timeDecay = Mathf.Exp(-timeSinceMemory / 30f); // 30-minute half-life
        relevance *= timeDecay;

        return relevance;
    }

    private List<ThematicElement> GetActiveThemes()
    {
        return thematicElements.Values
            .Where(t => t.currentStrength > 0.3f)
            .OrderByDescending(t => t.currentStrength)
            .ToList();
    }

    private async Task AddPsychologicalContext(StringBuilder contextBuilder)
    {
        var profile = GameManager.Instance.ProfileManager.CurrentProfile;
        var gameState = gameStateManager.GetCurrentState();

        contextBuilder.AppendLine("\nPsychological Context:");
        contextBuilder.AppendLine($"Fear Level: {profile.FearLevel}");
        contextBuilder.AppendLine($"Obsession Level: {profile.ObsessionLevel}");
        contextBuilder.AppendLine($"Aggression Level: {profile.AggressionLevel}");
        contextBuilder.AppendLine($"Current Tension: {gameState.tensionLevel}");

        // Add psychological interpretation
        string interpretation = await GeneratePsychologicalInterpretation(profile, gameState);
        contextBuilder.AppendLine($"\nPsychological State Interpretation: {interpretation}");
    }

    private async Task<string> GeneratePsychologicalInterpretation(PlayerAnalysisProfile profile, GameStateManager.GameState gameState)
    {
        string prompt = "Interpret the current psychological state considering:\n" +
                       $"Fear: {profile.FearLevel}\n" +
                       $"Obsession: {profile.ObsessionLevel}\n" +
                       $"Aggression: {profile.AggressionLevel}\n" +
                       $"Tension: {gameState.tensionLevel}\n" +
                       $"Recent Events: {string.Join(", ", significantEvents.Take(3))}";

        return await llmManager.GenerateResponse(prompt, "psychological_interpretation");
    }

    public async Task RecordSignificantEvent(string description, float psychologicalWeight, string[] themes)
    {
        var memory = new NarrativeMemory
        {
            id = System.Guid.NewGuid().ToString(),
            description = description,
            psychologicalWeight = psychologicalWeight,
            associatedThemes = themes,
            emotionalImpact = CalculateEmotionalImpact(description),
            timestamp = System.DateTime.Now,
            thematicLinks = await GenerateThematicLinks(description, themes)
        };

        narrativeMemories[memory.id] = memory;
        significantEvents.Insert(0, description);
        
        if (significantEvents.Count > MAX_SIGNIFICANT_EVENTS)
        {
            significantEvents.RemoveAt(significantEvents.Count - 1);
        }

        // Update thematic elements
        await UpdateThematicElements(memory);
    }

    private float CalculateEmotionalImpact(string description)
    {
        // Calculate based on emotional keywords and intensity
        float impact = 0f;
        
        if (description.Contains("fear") || description.Contains("terror"))
            impact += 0.3f;
        
        if (description.Contains("horror") || description.Contains("dread"))
            impact += 0.4f;
        
        if (description.Contains("trauma") || description.Contains("psychological"))
            impact += 0.5f;

        return Mathf.Clamp01(impact);
    }

    private async Task<Dictionary<string, float>> GenerateThematicLinks(string description, string[] themes)
    {
        string prompt = $"Analyze these thematic connections:\nEvent: {description}\nThemes: {string.Join(", ", themes)}\n" +
                       "Generate strength values (0-1) for each thematic connection.";

        string response = await llmManager.GenerateResponse(prompt, "thematic_analysis");
        return ParseThematicLinks(response);
    }

    private Dictionary<string, float> ParseThematicLinks(string response)
    {
        var links = new Dictionary<string, float>();
        try
        {
            var lines = response.Split('\n');
            foreach (var line in lines)
            {
                var parts = line.Split(':');
                if (parts.Length == 2)
                {
                    string theme = parts[0].Trim().ToLower();
                    if (float.TryParse(parts[1].Trim(), out float strength))
                    {
                        links[theme] = Mathf.Clamp01(strength);
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error parsing thematic links: {ex.Message}");
        }
        return links;
    }

    private async Task UpdateThematicElements(NarrativeMemory memory)
    {
        foreach (var theme in memory.associatedThemes)
        {
            if (thematicElements.TryGetValue(theme, out var element))
            {
                // Update theme strength
                float oldStrength = element.currentStrength;
                element.currentStrength = Mathf.Lerp(
                    element.currentStrength,
                    element.currentStrength + memory.psychologicalWeight * 0.2f,
                    0.3f
                );

                // Generate new manifestations if theme significantly strengthened
                if (element.currentStrength > oldStrength + 0.2f)
                {
                    await GenerateNewManifestations(element);
                }
            }
        }
    }

    private async Task GenerateNewManifestations(ThematicElement element)
    {
        try
        {
            string prompt = $"Generate new manifestations for this strengthened theme:\n" +
                           $"Theme: {element.theme}\n" +
                           $"Current Strength: {element.currentStrength}\n" +
                           $"Current Manifestations: {string.Join(", ", element.manifestations)}\n" +
                           "Generate subtle but psychologically impactful new manifestations.";

            string response = await llmManager.GenerateResponse(prompt, "manifestation_generation");
            var newManifestations = ParseManifestations(response);

            if (newManifestations.Length > 0)
            {
                // Merge new manifestations with existing ones
                element.manifestations = element.manifestations
                    .Concat(newManifestations)
                    .Distinct()
                    .ToArray();

                // Notify the manifestation system
                await GameManager.Instance.ShadowManifestationSystem
                    .ProcessNewManifestations(element.theme, newManifestations);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error generating manifestations: {ex.Message}");
        }
    }

    private string[] ParseManifestations(string response)
    {
        try
        {
            return response.Split('\n')
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Select(line => line.Trim())
                .Where(line => !line.StartsWith("//") && !line.StartsWith("#"))
                .ToArray();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error parsing manifestations: {ex.Message}");
            return System.Array.Empty<string>();
        }
    }

    public void ClearNarrativeMemories()
    {
        narrativeMemories.Clear();
        significantEvents.Clear();
        
        foreach (var element in thematicElements.Values)
        {
            element.currentStrength = 0.1f;
        }
    }

    public enum ContextType
    {
        Psychological,
        Environmental,
        Narrative,
        Combined
    }
}