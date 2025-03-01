using UnityEngine;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using AIHell.Core.Data;

public class ImagePromptGenerator : MonoBehaviour
{
    // Example templates that will be used to train the LLM
    private List<PromptTemplate> exampleTemplates;
    private LLMManager llmManager;
    
    [System.Serializable]
    public class PromptTemplate
    {
        public string basePrompt;
        public string psychologicalContext;
        public string emotionalTone;
        public string[] suggestedElements;
        public float psychologicalIntensity;
    }

    private void Awake()
    {
        llmManager = GameManager.Instance.LLMManager;
        InitializeExampleTemplates();
    }

    private void InitializeExampleTemplates()
    {
        exampleTemplates = new List<PromptTemplate>
        {
            new PromptTemplate {
                basePrompt = "A dimly lit Victorian corridor with shadows that seem to move",
                psychologicalContext = "Growing paranoia and fear of being watched",
                emotionalTone = "Dread and unease",
                suggestedElements = new[] { "dancing shadows", "distorted reflections", "twisted perspectives" },
                psychologicalIntensity = 0.7f
            },
            new PromptTemplate {
                basePrompt = "An impossibly vast cosmic void with non-euclidean geometry",
                psychologicalContext = "Overwhelming cosmic dread and insignificance",
                emotionalTone = "Existential horror",
                suggestedElements = new[] { "incomprehensible shapes", "infinite depth", "reality-bending patterns" },
                psychologicalIntensity = 0.9f
            }
            // More examples can be added as reference material for the LLM
        };
    }

    public async Task<string> GeneratePrompt(string baseContext, PlayerAnalysisProfile profile, EmotionalResponseSystem emotional)
    {
        // Create LLM context from current state
        var context = BuildGenerationContext(baseContext, profile, emotional);
        
        // Generate unique prompt using LLM
        string generatedPrompt = await GenerateUniquePrompt(context);
        
        // Enhance with psychological elements
        string enhancedPrompt = await EnhanceWithPsychologicalElements(generatedPrompt, profile);
        
        return enhancedPrompt;
    }

    private string BuildGenerationContext(string baseContext, PlayerAnalysisProfile profile, EmotionalResponseSystem emotional)
    {
        var contextBuilder = new StringBuilder();
        
        // Add base context
        contextBuilder.AppendLine("Scene Context: " + baseContext);
        
        // Add psychological state
        contextBuilder.AppendLine($"Fear Level: {profile.FearLevel}");
        contextBuilder.AppendLine($"Obsession Level: {profile.ObsessionLevel}");
        contextBuilder.AppendLine($"Aggression Level: {profile.AggressionLevel}");
        
        // Add emotional state
        var emotions = emotional.GetEmotionalState();
        contextBuilder.AppendLine("Emotional State:");
        foreach (var emotion in emotions)
        {
            contextBuilder.AppendLine($"- {emotion.Key}: {emotion.Value}");
        }
        
        // Add example templates for reference
        contextBuilder.AppendLine("\nExample Templates for Reference:");
        foreach (var template in exampleTemplates)
        {
            contextBuilder.AppendLine($"Example:");
            contextBuilder.AppendLine($"Base: {template.basePrompt}");
            contextBuilder.AppendLine($"Context: {template.psychologicalContext}");
            contextBuilder.AppendLine($"Tone: {template.emotionalTone}");
        }
        
        return contextBuilder.ToString();
    }

    private async Task<string> GenerateUniquePrompt(string context)
    {
        string prompt = "Based on the following context and example templates, generate a unique and psychologically impactful image prompt. " +
                       "The prompt should be deeply unsettling and personal, while avoiding generic horror tropes. " +
                       "Focus on psychological horror elements that reflect the current emotional and psychological state.\n\n" + context;

        return await llmManager.GenerateResponse(prompt);
    }

    private async Task<string> EnhanceWithPsychologicalElements(string basePrompt, PlayerAnalysisProfile profile)
    {
        string enhancementPrompt = $"Enhance the following image prompt with subtle psychological elements based on these psychological traits:\n" +
                                 $"Fear: {profile.FearLevel}\n" +
                                 $"Obsession: {profile.ObsessionLevel}\n" +
                                 $"Aggression: {profile.AggressionLevel}\n\n" +
                                 $"Base Prompt: {basePrompt}\n\n" +
                                 "Add unsettling details that specifically target these psychological states without being too obvious.";

        return await llmManager.GenerateResponse(enhancementPrompt);
    }

    public async Task<string> GenerateNegativePrompt(PlayerAnalysisProfile profile)
    {
        string prompt = "Generate a negative prompt for stable diffusion that will help avoid generic horror elements while " +
                       "maintaining psychological impact. Consider these psychological states:\n" +
                       $"Fear: {profile.FearLevel}\n" +
                       $"Obsession: {profile.ObsessionLevel}\n" +
                       $"Aggression: {profile.AggressionLevel}";

        return await llmManager.GenerateResponse(prompt);
    }
}