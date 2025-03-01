using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using AIHell.Core.Data;
using AIHell.Core;

[RequireComponent(typeof(StyleTransferProcessor))]
[RequireComponent(typeof(PatternRecognitionSystem))]
[RequireComponent(typeof(BlendingSystem))]
[RequireComponent(typeof(ShadowManifestationSystem))]
public class HorrorCompositionSystem : MonoBehaviour
{
    private StyleTransferProcessor styleProcessor;
    private PatternRecognitionSystem patternSystem;
    private BlendingSystem blendingSystem;
    private ShadowManifestationSystem shadowSystem;
    
    private Queue<CompositionRequest> compositionQueue;
    private bool isProcessing;

    [System.Serializable]
    public class CompositionRequest
    {
        public string prompt;
        public float psychologicalIntensity;
        public string dominantEmotion;
        public PlayerAnalysisProfile profile;
        public System.Action<Texture2D> callback;
    }

    [System.Serializable]
    public class CompositionResult
    {
        public Texture2D image;
        public List<string> detectedPatterns;
        public float psychologicalImpact;
        public string dominantStyle;
        public Dictionary<string, float> emotionalWeights;
    }

    private void Awake()
    {
        InitializeSystems();
    }

    private void InitializeSystems()
    {
        styleProcessor = GetComponent<StyleTransferProcessor>();
        patternSystem = GetComponent<PatternRecognitionSystem>();
        blendingSystem = GetComponent<BlendingSystem>();
        shadowSystem = GetComponent<ShadowManifestationSystem>();
        
        compositionQueue = new Queue<CompositionRequest>();
    }

    public void RequestHorrorComposition(string prompt, float intensity, string emotion, PlayerAnalysisProfile profile, System.Action<Texture2D> callback)
    {
        var request = new CompositionRequest
        {
            prompt = prompt,
            psychologicalIntensity = intensity,
            dominantEmotion = emotion,
            profile = profile,
            callback = callback
        };

        compositionQueue.Enqueue(request);

        if (!isProcessing)
        {
            StartCoroutine(ProcessCompositionQueue());
        }
    }

    private System.Collections.IEnumerator ProcessCompositionQueue()
    {
        isProcessing = true;

        while (compositionQueue.Count > 0)
        {
            var request = compositionQueue.Dequeue();
            yield return StartCoroutine(GenerateComposition(request));
        }

        isProcessing = false;
    }

    private System.Collections.IEnumerator GenerateComposition(CompositionRequest request)
    {
        // Initial image generation
        bool imageGenerated = false;
        Texture2D baseImage = null;

        GameManager.Instance.ImageGenerator.RequestContextualImage(
            GameManager.Instance.LevelManager.CurrentRoom,
            (texture) => {
                baseImage = texture;
                imageGenerated = true;
            }
        );

        // Wait for base image
        while (!imageGenerated || baseImage == null)
        {
            yield return null;
        }

        // Analyze psychological patterns
        var patternAnalysis = patternSystem.AnalyzeImage(baseImage, request.profile);

        // Determine optimal horror styles
        var styles = DetermineHorrorStyles(
            patternAnalysis,
            request.psychologicalIntensity,
            request.profile
        );

        // Apply horror style composition
        yield return StartCoroutine(ApplyHorrorComposition(
            baseImage,
            styles,
            request.psychologicalIntensity,
            (processedImage) => {
                // Process shadow manifestations
                ProcessShadowManifestations(processedImage, request.profile);
                
                // Final callback
                request.callback?.Invoke(processedImage);
            }
        ));
    }

    private HorrorStyleSet DetermineHorrorStyles(PatternRecognitionSystem.PatternAnalysisResult analysis, float intensity, PlayerAnalysisProfile profile)
    {
        var styles = new HorrorStyleSet();

        // Base style selection
        styles.primaryStyle = SelectPrimaryStyle(analysis, profile);
        styles.secondaryStyle = SelectSecondaryStyle(styles.primaryStyle, profile);
        styles.blendFactor = CalculateBlendFactor(analysis, profile);

        // Adjust based on psychological state
        AdjustStylesForPsychologicalState(styles, profile);

        // Modify based on pattern analysis
        ModifyStylesForPatterns(styles, analysis);

        return styles;
    }

    private string SelectPrimaryStyle(PatternRecognitionSystem.PatternAnalysisResult analysis, PlayerAnalysisProfile profile)
    {
        // Weight different factors
        var weights = new Dictionary<string, float>();

        // Pattern-based weights
        foreach (var impact in analysis.psychologicalImpact)
        {
            weights[impact.Key] = impact.Value;
        }

        // Profile-based weights
        weights["psychological_horror"] = profile.FearLevel * 1.2f;
        weights["surreal_horror"] = profile.ObsessionLevel * 1.1f;
        weights["cosmic_horror"] = (profile.FearLevel + profile.ObsessionLevel) * 0.5f;
        weights["dark_romanticism"] = profile.AggressionLevel;

        // Return style with highest weight
        return weights.OrderByDescending(w => w.Value).First().Key;
    }

    private string SelectSecondaryStyle(string primaryStyle, PlayerAnalysisProfile profile)
    {
        switch (primaryStyle)
        {
            case "psychological_horror":
                return profile.ObsessionLevel > 0.6f ? "surreal_horror" : "dark_romanticism";
            case "surreal_horror":
                return profile.FearLevel > 0.6f ? "cosmic_horror" : "psychological_horror";
            case "cosmic_horror":
                return profile.AggressionLevel > 0.6f ? "dark_romanticism" : "surreal_horror";
            default:
                return "psychological_horror";
        }
    }

    private float CalculateBlendFactor(PatternRecognitionSystem.PatternAnalysisResult analysis, PlayerAnalysisProfile profile)
    {
        // Base blend on psychological intensity
        float baseFactor = (profile.FearLevel + profile.ObsessionLevel) * 0.5f;
        
        // Modify based on pattern strength
        baseFactor *= analysis.overallIntensity;
        
        return Mathf.Clamp01(baseFactor);
    }

    private void AdjustStylesForPsychologicalState(HorrorStyleSet styles, PlayerAnalysisProfile profile)
    {
        // Intensify primary style based on psychological state
        if (profile.FearLevel > 0.8f)
        {
            styles.primaryIntensity *= 1.2f;
        }
        
        if (profile.ObsessionLevel > 0.7f)
        {
            styles.blendFactor *= 1.3f;
        }
        
        // Add subtle variations
        if (profile.AggressionLevel > 0.6f)
        {
            styles.secondaryIntensity *= 1.1f;
        }
    }

    private void ModifyStylesForPatterns(HorrorStyleSet styles, PatternRecognitionSystem.PatternAnalysisResult analysis)
    {
        // Adjust based on detected patterns
        foreach (var pattern in analysis.detectedPatterns)
        {
            if (pattern.intensity > 0.7f)
            {
                if (pattern.profileId == "pareidolia")
                {
                    styles.primaryIntensity *= 1.2f;
                }
                else if (pattern.profileId == "fractal")
                {
                    styles.blendFactor *= 1.1f;
                }
            }
        }
    }

    private System.Collections.IEnumerator ApplyHorrorComposition(Texture2D baseImage, HorrorStyleSet styles, float intensity, System.Action<Texture2D> callback)
    {
        // Request style transition
        bool transitionComplete = false;
        Texture2D processedImage = null;

        blendingSystem.RequestStyleTransition(
            styles.primaryStyle,
            styles.secondaryStyle,
            3f, // Duration
            intensity,
            (result) => {
                processedImage = result;
                transitionComplete = true;
            }
        );

        // Wait for transition
        while (!transitionComplete)
        {
            yield return null;
        }

        // Apply final composition
        if (processedImage != null)
        {
            callback?.Invoke(processedImage);
        }
    }

    private void ProcessShadowManifestations(Texture2D image, PlayerAnalysisProfile profile)
    {
        if (profile.FearLevel > 0.7f || profile.ObsessionLevel > 0.7f)
        {
            shadowSystem.ProcessPsychologicalState(profile, GameManager.Instance.LevelManager.CurrentRoom);
        }
    }

    private class HorrorStyleSet
    {
        public string primaryStyle;
        public string secondaryStyle;
        public float primaryIntensity = 1f;
        public float secondaryIntensity = 0.7f;
        public float blendFactor = 0.5f;
    }

    public bool IsProcessing()
    {
        return isProcessing || compositionQueue.Count > 0;
    }

    public void CancelAllCompositions()
    {
        compositionQueue.Clear();
        blendingSystem.CancelAllTransitions();
        isProcessing = false;
    }
}