using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using AIHell.Core.Data;

[RequireComponent(typeof(HorrorCompositionSystem))]
public class EmotionalRenderingPipeline : MonoBehaviour
{
    private HorrorCompositionSystem compositionSystem;
    private LLMManager llmManager;
    private List<EmotionalFilter> exampleFilters;
    private Dictionary<string, EmotionalFilter> activeFilters;
    private RenderTexture renderBuffer;
    private Material compositionMaterial;

    [System.Serializable]
    public class EmotionalFilter
    {
        public string emotion;
        public string description;
        public AnimationCurve intensityCurve;
        public Color tint;
        public float distortionStrength;
        public float pulseRate;
        public float chromaticAberration;
        public float grainIntensity;
        public float vignetteIntensity;
        public string psychologicalContext;
    }

    [System.Serializable]
    public class RenderingState
    {
        public EmotionalFilter currentFilter;
        public float blendFactor;
        public float psychologicalIntensity;
        public Vector2 distortionOffset;
        public float timeScale;
    }

    private RenderingState currentState;

    private async void Awake()
    {
        await InitializePipeline();
    }

    private async Task InitializePipeline()
    {
        compositionSystem = GetComponent<HorrorCompositionSystem>();
        llmManager = GameManager.Instance.LLMManager;
        activeFilters = new Dictionary<string, EmotionalFilter>();
        currentState = new RenderingState();
        
        InitializeExampleFilters();
        await GenerateDynamicFilters();
        InitializeRenderResources();
    }

    private void InitializeExampleFilters()
    {
        exampleFilters = new List<EmotionalFilter>
        {
            new EmotionalFilter {
                emotion = "primal_fear",
                description = "Deep-seated survival instincts and primitive fears",
                tint = new Color(0.9f, 0.8f, 0.8f),
                distortionStrength = 0.3f,
                pulseRate = 1.2f,
                chromaticAberration = 0.02f,
                grainIntensity = 0.15f,
                vignetteIntensity = 0.4f,
                psychologicalContext = "Fight or flight response, survival horror"
            },
            new EmotionalFilter {
                emotion = "cosmic_dread",
                description = "Incomprehensible vastness and existential terror",
                tint = new Color(0.7f, 0.7f, 0.8f),
                distortionStrength = 0.2f,
                pulseRate = 0.8f,
                chromaticAberration = 0.03f,
                grainIntensity = 0.2f,
                vignetteIntensity = 0.5f,
                psychologicalContext = "Existential horror, cosmic insignificance"
            }
            // More examples for LLM reference
        };
    }

    private async Task GenerateDynamicFilters()
    {
        string prompt = "Using these example emotional filters as reference, generate new unique psychological horror filters:\n\n";
        
        foreach (var example in exampleFilters)
        {
            prompt += $"Example Filter:\n" +
                     $"Emotion: {example.emotion}\n" +
                     $"Description: {example.description}\n" +
                     $"Psychological Context: {example.psychologicalContext}\n" +
                     $"Visual Effects: Tint({example.tint}), Distortion({example.distortionStrength})\n\n";
        }

        string response = await llmManager.GenerateResponse(prompt);
        await ProcessGeneratedFilters(response);
    }

    private async Task ProcessGeneratedFilters(string llmResponse)
    {
        // Process LLM response into new emotional filters
        // Implementation would depend on LLM output format
        
        // For each generated filter, get specific parameters
        await GenerateFilterParameters(llmResponse);
    }

    private async Task GenerateFilterParameters(string filterDescription)
    {
        string prompt = "Generate specific visual effect parameters for this emotional filter:\n" +
                       filterDescription + "\n\n" +
                       "Include values for:\n" +
                       "- Color tint (RGB values)\n" +
                       "- Distortion strength (0-1)\n" +
                       "- Pulse rate (frequency)\n" +
                       "- Visual noise/grain amount\n" +
                       "- Vignette intensity";

        string response = await llmManager.GenerateResponse(prompt);
        // Parse response and create filter parameters
    }

    public async Task<EmotionalFilter> GenerateContextualFilter(string emotion, PlayerAnalysisProfile profile)
    {
        string prompt = $"Generate an emotional filter for the current psychological state:\n" +
                       $"Dominant Emotion: {emotion}\n" +
                       $"Fear Level: {profile.FearLevel}\n" +
                       $"Obsession Level: {profile.ObsessionLevel}\n" +
                       $"Aggression Level: {profile.AggressionLevel}\n\n" +
                       "Design visual effects that reflect this emotional state.";

        string response = await llmManager.GenerateResponse(prompt);
        return await CreateFilterFromResponse(response);
    }

    private async Task<EmotionalFilter> CreateFilterFromResponse(string response)
    {
        // Parse LLM response into filter parameters
        // Implementation would depend on LLM output format
        return new EmotionalFilter(); // Placeholder
    }

    public async void ProcessImage(Texture2D sourceImage, string dominantEmotion, float intensity, System.Action<Texture2D> callback)
    {
        // Generate or retrieve contextual filter
        var profile = GameManager.Instance.ProfileManager.CurrentProfile;
        var filter = await GenerateContextualFilter(dominantEmotion, profile);
        
        // Update rendering state
        UpdateRenderingState(filter, intensity);

        // Process through emotional filter
        StartCoroutine(ApplyEmotionalFilter(sourceImage, callback));
    }

    private void UpdateRenderingState(EmotionalFilter filter, float intensity)
    {
        if (filter != null)
        {
            currentState.currentFilter = filter;
            currentState.psychologicalIntensity = intensity;
            currentState.timeScale = 1f + intensity * 0.5f;
            
            // Update distortion offset based on psychological intensity
            float time = Time.time * currentState.timeScale;
            currentState.distortionOffset = new Vector2(
                Mathf.Sin(time * filter.pulseRate) * filter.distortionStrength,
                Mathf.Cos(time * filter.pulseRate * 1.3f) * filter.distortionStrength
            ) * intensity;
        }
    }

    private System.Collections.IEnumerator ApplyEmotionalFilter(Texture2D sourceImage, System.Action<Texture2D> callback)
    {
        if (compositionMaterial == null || currentState.currentFilter == null)
        {
            callback?.Invoke(sourceImage);
            yield break;
        }

        // Set shader parameters
        compositionMaterial.SetTexture("_MainTex", sourceImage);
        compositionMaterial.SetFloat("_Intensity", currentState.psychologicalIntensity);
        compositionMaterial.SetColor("_EmotionalTint", currentState.currentFilter.tint);
        compositionMaterial.SetVector("_DistortionOffset", currentState.distortionOffset);
        compositionMaterial.SetFloat("_ChromaticAberration", currentState.currentFilter.chromaticAberration * currentState.psychologicalIntensity);
        compositionMaterial.SetFloat("_GrainIntensity", currentState.currentFilter.grainIntensity * currentState.psychologicalIntensity);
        compositionMaterial.SetFloat("_VignetteIntensity", currentState.currentFilter.vignetteIntensity * currentState.psychologicalIntensity);
        compositionMaterial.SetFloat("_Time", Time.time * currentState.timeScale);

        // Render to buffer
        Graphics.Blit(sourceImage, renderBuffer, compositionMaterial);

        // Convert back to Texture2D
        Texture2D resultTexture = new Texture2D(renderBuffer.width, renderBuffer.height);
        RenderTexture.active = renderBuffer;
        resultTexture.ReadPixels(new Rect(0, 0, renderBuffer.width, renderBuffer.height), 0, 0);
        resultTexture.Apply();

        // Post-process emotional effects
        ApplyEmotionalPostProcess(resultTexture);

        callback?.Invoke(resultTexture);
    }

    private void ApplyEmotionalPostProcess(Texture2D texture)
    {
        Color[] pixels = texture.GetPixels();
        EmotionalFilter filter = currentState.currentFilter;
        float intensity = currentState.psychologicalIntensity;

        for (int i = 0; i < pixels.Length; i++)
        {
            // Apply emotional color modulation
            Color pixel = pixels[i];
            
            // Emotional tinting
            pixel = Color.Lerp(pixel, pixel * filter.tint, intensity * 0.5f);
            
            // Psychological contrast
            float luminance = pixel.grayscale;
            pixel = Color.Lerp(pixel, pixel * (1f + (luminance - 0.5f) * 0.5f), intensity * 0.3f);
            
            // Subtle color shifting based on emotional state
            float shift = Mathf.Sin(Time.time * filter.pulseRate) * 0.1f * intensity;
            pixel.r += shift;
            pixel.b -= shift * 0.5f;
            
            pixels[i] = pixel;
        }

        texture.SetPixels(pixels);
        texture.Apply();
    }

    private async Task<bool> ShouldGenerateNewFilters()
    {
        var gameState = GameManager.Instance.StateManager.GetCurrentState();
        string prompt = "Based on the current game state, should new emotional filters be introduced?\n" +
                       $"Current Emotional State: {GetCurrentEmotionalState()}\n" +
                       $"Active Filters: {activeFilters.Count}\n" +
                       $"Game Phase: {gameState.currentPhase}";

        string response = await llmManager.GenerateResponse(prompt);
        return response.ToLower().Contains("yes");
    }

    private string GetCurrentEmotionalState()
    {
        var emotional = GameManager.Instance.EmotionalResponseSystem;
        var emotions = emotional.GetEmotionalState();
        return string.Join(", ", emotions);
    }

    private void InitializeRenderResources()
    {
        renderBuffer = new RenderTexture(
            Screen.width,
            Screen.height,
            0,
            RenderTextureFormat.ARGBFloat
        );
        renderBuffer.enableRandomWrite = true;
        renderBuffer.Create();

        // Load emotional composition shader
        Shader compositionShader = Shader.Find("Hidden/EmotionalComposition");
        if (compositionShader != null)
        {
            compositionMaterial = new Material(compositionShader);
        }
    }

    private void OnDestroy()
    {
        if (renderBuffer != null)
        {
            renderBuffer.Release();
        }

        if (compositionMaterial != null)
        {
            Destroy(compositionMaterial);
        }
    }

    public EmotionalFilter GetCurrentFilter()
    {
        return currentState?.currentFilter;
    }

    public void ResetPipeline()
    {
        currentState = new RenderingState();
    }
}