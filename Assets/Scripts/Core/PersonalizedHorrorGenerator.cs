using UnityEngine;
using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;
using AIHell.Core.Data;

[RequireComponent(typeof(LLMManager))]
[RequireComponent(typeof(GameStateManager))]
[RequireComponent(typeof(NarrativeContextManager))]
public class PersonalizedHorrorGenerator : MonoBehaviour
{
    private LLMManager llmManager;
    private GameStateManager gameStateManager;
    private NarrativeContextManager narrativeContext;
    private UnityEventBridge eventBridge;
    private Dictionary<string, float> personalizedWeights;
    private List<GeneratedHorrorElement> activeElements;

    [System.Serializable]
    public class GeneratedHorrorElement
    {
        public string id;
        public string type;
        public string description;
        public float personalRelevance;
        public float psychologicalImpact;
        public string[] triggers;
        public AnimationCurve intensityCurve;
        public bool isActive;
        public float lifetime;
        public Dictionary<string, object> dynamicProperties;
    }

    [System.Serializable]
    public class GenerationParameters
    {
        public float psychologicalIntensity;
        public string[] requiredThemes;
        public string[] avoidedThemes;
        public float personalRelevanceThreshold;
        public bool allowRecursion;
        public Dictionary<string, float> elementWeights;
    }

    private async void Awake()
    {
        InitializeComponents();
        await InitializePersonalization();
    }

    private void InitializeComponents()
    {
        llmManager = GetComponent<LLMManager>();
        gameStateManager = GetComponent<GameStateManager>();
        narrativeContext = GetComponent<NarrativeContextManager>();
        eventBridge = GameManager.Instance.EventBridge;
        
        personalizedWeights = new Dictionary<string, float>();
        activeElements = new List<GeneratedHorrorElement>();
    }

    private async Task InitializePersonalization()
    {
        // Initialize base weights
        personalizedWeights["psychological"] = 0.4f;
        personalizedWeights["environmental"] = 0.3f;
        personalizedWeights["narrative"] = 0.3f;

        // Generate initial personalized elements
        await GenerateInitialElements();
    }

    private async Task GenerateInitialElements()
    {
        var profile = GameManager.Instance.ProfileManager.CurrentProfile;
        
        string prompt = "Generate initial psychological horror elements based on:\n" +
                       $"Fear Level: {profile.FearLevel}\n" +
                       $"Obsession Level: {profile.ObsessionLevel}\n" +
                       $"Aggression Level: {profile.AggressionLevel}\n" +
                       "Create unique elements that can evolve with the player's psychological state.";

        string response = await llmManager.GenerateResponse(prompt, "initial_horror_generation");
        await ProcessGeneratedElements(response);
    }

    public async Task<GeneratedHorrorElement> GenerateHorrorElement(GenerationParameters parameters)
    {
        // Build rich context for generation
        string context = await BuildGenerationContext(parameters);
        
        // Generate personalized horror element
        string response = await llmManager.GenerateResponse(
            context,
            "horror_generation",
            parameters.requiredThemes
        );

        // Process and validate the generated element
        var element = await ProcessGeneratedElement(response, parameters);
        
        // Add to active elements if valid
        if (ValidateElement(element, parameters))
        {
            activeElements.Add(element);
            await IntegrateElement(element);
        }

        return element;
    }

    private async Task<string> BuildGenerationContext(GenerationParameters parameters)
    {
        var contextBuilder = new StringBuilder();

        // Add narrative context
        string narrativeContext = await this.narrativeContext.GenerateContextualPrompt(
            "Generate a psychological horror element",
            NarrativeContextManager.ContextType.Combined
        );
        contextBuilder.AppendLine(narrativeContext);

        // Add generation parameters
        contextBuilder.AppendLine("\nGeneration Parameters:");
        contextBuilder.AppendLine($"Psychological Intensity: {parameters.psychologicalIntensity}");
        contextBuilder.AppendLine($"Required Themes: {string.Join(", ", parameters.requiredThemes)}");
        contextBuilder.AppendLine($"Personal Relevance Threshold: {parameters.personalRelevanceThreshold}");

        // Add active elements context
        if (activeElements.Count > 0)
        {
            contextBuilder.AppendLine("\nActive Horror Elements:");
            foreach (var element in activeElements.Take(3))
            {
                contextBuilder.AppendLine($"- {element.description} (Impact: {element.psychologicalImpact})");
            }
        }

        return contextBuilder.ToString();
    }

    private async Task<GeneratedHorrorElement> ProcessGeneratedElement(string response, GenerationParameters parameters)
    {
        // First, get LLM to structure the element properly
        string structurePrompt = $"Structure this horror element response into a clear format:\n{response}";
        string structured = await llmManager.GenerateResponse(structurePrompt, "element_structuring");

        var element = new GeneratedHorrorElement
        {
            id = System.Guid.NewGuid().ToString(),
            type = DetermineElementType(structured),
            description = ExtractDescription(structured),
            personalRelevance = await CalculatePersonalRelevance(structured),
            psychologicalImpact = CalculatePsychologicalImpact(structured, parameters),
            triggers = ExtractTriggers(structured),
            intensityCurve = GenerateIntensityCurve(parameters.psychologicalIntensity),
            isActive = true,
            lifetime = 0f,
            dynamicProperties = new Dictionary<string, object>()
        };

        return element;
    }

    private string DetermineElementType(string structured)
    {
        // Analyze the structured response to determine the horror element type
        if (structured.Contains("psychological") || structured.Contains("mental"))
            return "psychological";
        if (structured.Contains("environment") || structured.Contains("physical"))
            return "environmental";
        if (structured.Contains("narrative") || structured.Contains("story"))
            return "narrative";
        
        return "general";
    }

    private async Task<float> CalculatePersonalRelevance(string elementDescription)
    {
        var profile = GameManager.Instance.ProfileManager.CurrentProfile;
        
        string prompt = $"Analyze the personal relevance of this horror element:\n" +
                       $"Element: {elementDescription}\n" +
                       $"Fear Level: {profile.FearLevel}\n" +
                       $"Obsession Level: {profile.ObsessionLevel}\n" +
                       $"Aggression Level: {profile.AggressionLevel}\n" +
                       "Rate the personal relevance from 0 to 1.";

        string response = await llmManager.GenerateResponse(prompt, "relevance_analysis");
        return ParseRelevanceScore(response);
    }

    private float CalculatePsychologicalImpact(string elementDescription, GenerationParameters parameters)
    {
        float impact = 0f;

        // Base impact from intensity parameter
        impact += parameters.psychologicalIntensity * 0.4f;

        // Impact from psychological keywords
        if (elementDescription.Contains("fear") || elementDescription.Contains("terror"))
            impact += 0.2f;
        if (elementDescription.Contains("trauma") || elementDescription.Contains("psychological"))
            impact += 0.3f;
        if (elementDescription.Contains("personal") || elementDescription.Contains("intimate"))
            impact += 0.2f;

        return Mathf.Clamp01(impact);
    }

    private AnimationCurve GenerateIntensityCurve(float baseIntensity)
    {
        AnimationCurve curve = new AnimationCurve();
        
        // Start subtle
        curve.AddKey(0f, 0f);
        
        // Build to peak
        curve.AddKey(0.3f, baseIntensity * 0.5f);
        curve.AddKey(0.7f, baseIntensity);
        
        // Gradual fade
        curve.AddKey(1f, baseIntensity * 0.3f);
        
        return curve;
    }

    private bool ValidateElement(GeneratedHorrorElement element, GenerationParameters parameters)
    {
        // Check personal relevance threshold
        if (element.personalRelevance < parameters.personalRelevanceThreshold)
            return false;

        // Check for required themes
        if (parameters.requiredThemes != null && parameters.requiredThemes.Length > 0)
        {
            bool hasRequiredTheme = false;
            foreach (var theme in parameters.requiredThemes)
            {
                if (element.description.ToLower().Contains(theme.ToLower()))
                {
                    hasRequiredTheme = true;
                    break;
                }
            }
            if (!hasRequiredTheme) return false;
        }

        // Check for avoided themes
        if (parameters.avoidedThemes != null)
        {
            foreach (var theme in parameters.avoidedThemes)
            {
                if (element.description.ToLower().Contains(theme.ToLower()))
                    return false;
            }
        }

        return true;
    }

    private async Task IntegrateElement(GeneratedHorrorElement element)
    {
        // Record in narrative context
        await narrativeContext.RecordSignificantEvent(
            element.description,
            element.psychologicalImpact,
            element.triggers
        );

        // Update game state
        gameStateManager.RecordEvent(element.type);

        // Notify event system
        eventBridge.TriggerEvent("horror_element_generated", element);

        // Generate manifestation if needed
        if (ShouldManifest(element))
        {
            await GenerateManifestation(element);
        }
    }

    private bool ShouldManifest(GeneratedHorrorElement element)
    {
        return element.psychologicalImpact > 0.7f || 
               element.personalRelevance > 0.8f ||
               gameStateManager.GetCurrentState().tensionLevel > 0.6f;
    }

    private async Task GenerateManifestation(GeneratedHorrorElement element)
    {
        // Generate visual manifestation
        var manifestation = await GameManager.Instance.ShadowManifestationSystem
            .GenerateContextualManifestation(element);

        // Process through horror composition system
        await GameManager.Instance.HorrorCompositionSystem
            .ProcessManifestation(manifestation, element);
    }

    public async Task UpdateElements()
    {
        foreach (var element in activeElements.ToList())
        {
            element.lifetime += Time.deltaTime;

            // Update element state
            await UpdateElementState(element);

            // Remove inactive elements
            if (!element.isActive)
            {
                activeElements.Remove(element);
            }
        }

        // Generate new elements if needed
        if (ShouldGenerateNewElement())
        {
            await GenerateNewElement();
        }
    }

    private async Task UpdateElementState(GeneratedHorrorElement element)
    {
        // Get normalized lifetime
        float normalizedTime = Mathf.Clamp01(element.lifetime / 60f); // 60-second base lifetime
        
        // Get current intensity
        float currentIntensity = element.intensityCurve.Evaluate(normalizedTime);
        
        // Update psychological impact
        var profile = GameManager.Instance.ProfileManager.CurrentProfile;
        await UpdatePsychologicalImpact(element, currentIntensity, profile);

        // Check if element should remain active
        element.isActive = currentIntensity > 0.1f;
    }

    private async Task UpdatePsychologicalImpact(GeneratedHorrorElement element, float intensity, PlayerAnalysisProfile profile)
    {
        string prompt = $"Analyze the ongoing psychological impact of:\n" +
                       $"Element: {element.description}\n" +
                       $"Current Intensity: {intensity}\n" +
                       $"Lifetime: {element.lifetime}s\n" +
                       $"Current Fear: {profile.FearLevel}\n" +
                       "Generate psychological state modifications.";

        string response = await llmManager.GenerateResponse(prompt, "impact_analysis");
        ApplyPsychologicalModifications(response, profile);
    }

    private bool ShouldGenerateNewElement()
    {
        var gameState = gameStateManager.GetCurrentState();
        
        // Check basic conditions
        if (activeElements.Count >= 5) return false;
        if (gameState.recentEventCount > 3) return false;
        
        // Calculate generation probability
        float probability = 0.1f; // Base probability
        
        probability += gameState.tensionLevel * 0.2f;
        probability += (1f - (activeElements.Count / 5f)) * 0.3f;
        
        return Random.value < probability;
    }

    private async Task GenerateNewElement()
    {
        var parameters = new GenerationParameters
        {
            psychologicalIntensity = CalculateDesiredIntensity(),
            personalRelevanceThreshold = 0.6f,
            allowRecursion = false,
            elementWeights = new Dictionary<string, float>(personalizedWeights)
        };

        await GenerateHorrorElement(parameters);
    }

    private float CalculateDesiredIntensity()
    {
        var gameState = gameStateManager.GetCurrentState();
        var profile = GameManager.Instance.ProfileManager.CurrentProfile;

        // Base intensity from tension
        float intensity = gameState.tensionLevel * 0.5f;

        // Modify based on psychological state
        intensity += profile.FearLevel * 0.2f;
        intensity += profile.ObsessionLevel * 0.2f;
        intensity += profile.AggressionLevel * 0.1f;

        // Reduce if too many active elements
        intensity *= 1f - (activeElements.Count / 10f);

        return Mathf.Clamp01(intensity);
    }

    private float ParseRelevanceScore(string response)
    {
        // Parse LLM response to extract relevance score
        // This would be implemented based on the LLM's output format
        return 0.5f; // Placeholder
    }

    private void ApplyPsychologicalModifications(string response, PlayerAnalysisProfile profile)
    {
        if (string.IsNullOrEmpty(response)) return;

        if (response.Contains("fear") || response.Contains("terror"))
            profile.FearLevel += 0.1f;
        
        if (response.Contains("obsession") || response.Contains("compulsion"))
            profile.ObsessionLevel += 0.1f;
        
        if (response.Contains("anger") || response.Contains("aggression"))
            profile.AggressionLevel += 0.1f;
        
        // Clamp values
        profile.FearLevel = Mathf.Clamp01(profile.FearLevel);
        profile.ObsessionLevel = Mathf.Clamp01(profile.ObsessionLevel);
        profile.AggressionLevel = Mathf.Clamp01(profile.AggressionLevel);
    }

    public List<GeneratedHorrorElement> GetActiveElements()
    {
        return new List<GeneratedHorrorElement>(activeElements);
    }

    public void ClearElements()
    {
        activeElements.Clear();
    }
}