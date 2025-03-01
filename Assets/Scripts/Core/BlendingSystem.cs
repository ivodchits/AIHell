using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BlendingSystem : MonoBehaviour
{
    private Dictionary<string, BlendState> activeBlends;
    private Queue<BlendTransition> transitionQueue;
    private bool isProcessing;

    [System.Serializable]
    public class BlendState
    {
        public string styleA;
        public string styleB;
        public float blendFactor;
        public AnimationCurve blendCurve;
        public float duration;
        public float elapsedTime;
        public bool isActive;
    }

    [System.Serializable]
    public class BlendTransition
    {
        public string fromStyle;
        public string toStyle;
        public float duration;
        public float intensity;
        public AnimationCurve transitionCurve;
        public System.Action<Texture2D> onComplete;
    }

    private void Awake()
    {
        InitializeSystem();
    }

    private void InitializeSystem()
    {
        activeBlends = new Dictionary<string, BlendState>();
        transitionQueue = new Queue<BlendTransition>();
    }

    public void RequestStyleTransition(string fromStyle, string toStyle, float duration, float intensity, System.Action<Texture2D> callback)
    {
        var transition = new BlendTransition
        {
            fromStyle = fromStyle,
            toStyle = toStyle,
            duration = duration,
            intensity = intensity,
            transitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f),
            onComplete = callback
        };

        transitionQueue.Enqueue(transition);

        if (!isProcessing)
        {
            StartCoroutine(ProcessTransitionQueue());
        }
    }

    private IEnumerator ProcessTransitionQueue()
    {
        isProcessing = true;

        while (transitionQueue.Count > 0)
        {
            var transition = transitionQueue.Dequeue();
            yield return StartCoroutine(ExecuteTransition(transition));
        }

        isProcessing = false;
    }

    private IEnumerator ExecuteTransition(BlendTransition transition)
    {
        float elapsed = 0f;
        Texture2D currentTexture = null;
        var styleProcessor = GetComponent<StyleTransferProcessor>();

        // Create initial blend state
        var blendState = new BlendState
        {
            styleA = transition.fromStyle,
            styleB = transition.toStyle,
            duration = transition.duration,
            blendCurve = transition.transitionCurve,
            isActive = true
        };

        string blendKey = $"{transition.fromStyle}_{transition.toStyle}";
        activeBlends[blendKey] = blendState;

        while (elapsed < transition.duration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = elapsed / transition.duration;
            blendState.blendFactor = blendState.blendCurve.Evaluate(normalizedTime);

            // Process style blending
            yield return StartCoroutine(ProcessStyleBlend(
                blendState,
                transition.intensity,
                (resultTexture) => {
                    currentTexture = resultTexture;
                }
            ));

            // Apply psychological effects based on blend
            ApplyPsychologicalEffects(blendState, transition.intensity);

            yield return null;
        }

        // Cleanup and callback
        activeBlends.Remove(blendKey);
        transition.onComplete?.Invoke(currentTexture);
    }

    private IEnumerator ProcessStyleBlend(BlendState blend, float intensity, System.Action<Texture2D> callback)
    {
        var styleProcessor = GetComponent<StyleTransferProcessor>();
        Texture2D resultA = null;
        Texture2D resultB = null;
        bool styleAComplete = false;
        bool styleBComplete = false;

        // Process both styles
        styleProcessor.ApplyHorrorStyle(
            GameManager.Instance.ImageGenerator.GetCurrentTexture(),
            blend.styleA,
            intensity * (1f - blend.blendFactor),
            (textureA) => {
                resultA = textureA;
                styleAComplete = true;
            }
        );

        styleProcessor.ApplyHorrorStyle(
            GameManager.Instance.ImageGenerator.GetCurrentTexture(),
            blend.styleB,
            intensity * blend.blendFactor,
            (textureB) => {
                resultB = textureB;
                styleBComplete = true;
            }
        );

        // Wait for both styles to complete
        while (!styleAComplete || !styleBComplete)
        {
            yield return null;
        }

        // Blend the results
        Texture2D blendedTexture = BlendTextures(resultA, resultB, blend.blendFactor);
        callback?.Invoke(blendedTexture);

        // Cleanup
        if (resultA != null) Destroy(resultA);
        if (resultB != null) Destroy(resultB);
    }

    private Texture2D BlendTextures(Texture2D textureA, Texture2D textureB, float blendFactor)
    {
        int width = textureA.width;
        int height = textureA.height;
        Texture2D blendedTexture = new Texture2D(width, height);

        Color[] pixelsA = textureA.GetPixels();
        Color[] pixelsB = textureB.GetPixels();
        Color[] blendedPixels = new Color[pixelsA.Length];

        for (int i = 0; i < pixelsA.Length; i++)
        {
            // Apply custom blending based on psychological impact
            blendedPixels[i] = BlendPixels(pixelsA[i], pixelsB[i], blendFactor);
        }

        blendedTexture.SetPixels(blendedPixels);
        blendedTexture.Apply();

        return blendedTexture;
    }

    private Color BlendPixels(Color a, Color b, float t)
    {
        // Custom blending function for psychological impact
        float blendedR = Mathf.Lerp(a.r, b.r, t);
        float blendedG = Mathf.Lerp(a.g, b.g, t);
        float blendedB = Mathf.Lerp(a.b, b.b, t);

        // Add subtle distortion at blend boundaries
        float edgeEffect = Mathf.Abs(t - 0.5f) * 2f;
        float distortion = Mathf.Sin(Time.time * 2f) * 0.1f * (1f - edgeEffect);

        blendedR += distortion;
        blendedB -= distortion;

        return new Color(
            Mathf.Clamp01(blendedR),
            Mathf.Clamp01(blendedG),
            Mathf.Clamp01(blendedB),
            Mathf.Lerp(a.a, b.a, t)
        );
    }

    private void ApplyPsychologicalEffects(BlendState blend, float intensity)
    {
        var profile = GameManager.Instance.ProfileManager.CurrentProfile;
        var emotional = GameManager.Instance.EmotionalResponseSystem;

        // Calculate psychological impact of the blend
        float blendImpact = CalculateBlendImpact(blend);

        // Apply psychological effects
        profile.FearLevel += blendImpact * 0.05f * Time.deltaTime;
        profile.ObsessionLevel += blendImpact * 0.03f * Time.deltaTime;

        // Trigger emotional response
        emotional.ProcessEmotionalStimulus(
            "style_transition",
            blendImpact * intensity,
            profile
        );

        // Modify tension
        GameManager.Instance.TensionManager.ModifyTension(
            blendImpact * 0.1f,
            "style_blend"
        );
    }

    private float CalculateBlendImpact(BlendState blend)
    {
        // Calculate psychological impact based on style combination
        float baseImpact = blend.blendFactor * (1f - blend.blendFactor) * 4f; // Peak at 0.5

        // Modify impact based on style compatibility
        float compatibilityMultiplier = GetStyleCompatibility(blend.styleA, blend.styleB);
        
        return baseImpact * compatibilityMultiplier;
    }

    private float GetStyleCompatibility(string styleA, string styleB)
    {
        // Define psychological compatibility between different horror styles
        if (styleA == "psychological_horror" && styleB == "surreal_horror")
            return 1.5f; // High compatibility
        if (styleA == "cosmic_horror" && styleB == "dark_romanticism")
            return 1.3f; // Good compatibility
        if (styleA == "psychological_horror" && styleB == "cosmic_horror")
            return 1.2f; // Moderate compatibility
        
        return 1f; // Base compatibility
    }

    public void CancelAllTransitions()
    {
        transitionQueue.Clear();
        activeBlends.Clear();
        isProcessing = false;
    }

    public bool HasActiveTransitions()
    {
        return isProcessing || transitionQueue.Count > 0;
    }

    public BlendState GetActiveBlend(string styleA, string styleB)
    {
        string key = $"{styleA}_{styleB}";
        return activeBlends.TryGetValue(key, out BlendState blend) ? blend : null;
    }
}