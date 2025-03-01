using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using AIHell.Core.Data;

public class StyleTransferProcessor : MonoBehaviour
{
    private Dictionary<string, StyleDefinition> activeStyles;
    private Dictionary<string, ComputeShader> styleShaders;
    private LLMManager llmManager;
    private List<StyleDefinition> exampleStyles;

    [System.Serializable]
    public class StyleDefinition
    {
        public string id;
        public string name;
        public string description;
        public string psychologicalImpact;
        public string visualCharacteristics;
        public float intensity;
        public TextAsset styleNetwork;
        public AnimationCurve transferCurve;

        public StyleDefinition(string id, string name)
        {
            this.id = id;
            this.name = name;
            intensity = 1f;
            transferCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        }
    }

    private async void Awake()
    {
        await InitializeProcessor();
    }

    private async Task InitializeProcessor()
    {
        llmManager = GameManager.Instance.LLMManager;
        activeStyles = new Dictionary<string, StyleDefinition>();
        styleShaders = new Dictionary<string, ComputeShader>();
        
        InitializeExampleStyles();
        await GenerateDynamicStyles();
    }

    private void InitializeExampleStyles()
    {
        exampleStyles = new List<StyleDefinition>
        {
            new StyleDefinition("dark_romanticism_example", "Gothic Horror") {
                description = "Victorian-era psychological darkness",
                psychologicalImpact = "Emphasizes deep-seated fears and emotional turmoil",
                visualCharacteristics = "Deep shadows, architectural distortion, baroque elements",
                intensity = 0.8f
            },
            new StyleDefinition("surreal_horror_example", "Psychological Surrealism") {
                description = "Reality-bending psychological horror",
                psychologicalImpact = "Creates sense of unreality and mental instability",
                visualCharacteristics = "Impossible geometry, fluid transitions, dreamlike distortions",
                intensity = 0.9f
            }
            // More examples for LLM reference
        };
    }

    private async Task GenerateDynamicStyles()
    {
        string prompt = "Using these example horror styles as reference, generate new unique psychological horror visual styles:\n\n";
        
        foreach (var example in exampleStyles)
        {
            prompt += $"Example Style:\n" +
                     $"Name: {example.name}\n" +
                     $"Description: {example.description}\n" +
                     $"Psychological Impact: {example.psychologicalImpact}\n" +
                     $"Visual Characteristics: {example.visualCharacteristics}\n\n";
        }

        string response = await llmManager.GenerateResponse(prompt);
        await ProcessGeneratedStyles(response);
    }

    private async Task ProcessGeneratedStyles(string llmResponse)
    {
        // Parse LLM response into new style definitions
        var styles = await ParseStyleDefinitions(llmResponse);
        
        foreach (var style in styles)
        {
            await GenerateStyleParameters(style);
            AddStyle(style);
        }
    }

    private async Task<List<StyleDefinition>> ParseStyleDefinitions(string response)
    {
        var styles = new List<StyleDefinition>();
        try
        {
            var lines = response.Split('\n');
            StyleDefinition currentStyle = null;

            foreach (var line in lines)
            {
                if (line.StartsWith("Name:"))
                {
                    if (currentStyle != null)
                    {
                        styles.Add(currentStyle);
                    }
                    string styleName = line.Replace("Name:", "").Trim();
                    currentStyle = new StyleDefinition($"style_{Guid.NewGuid():N}", styleName);
                }
                else if (currentStyle != null)
                {
                    if (line.StartsWith("Description:"))
                        currentStyle.description = line.Replace("Description:", "").Trim();
                    else if (line.StartsWith("Psychological Impact:"))
                        currentStyle.psychologicalImpact = line.Replace("Psychological Impact:", "").Trim();
                    else if (line.StartsWith("Visual Characteristics:"))
                        currentStyle.visualCharacteristics = line.Replace("Visual Characteristics:", "").Trim();
                }
            }

            if (currentStyle != null)
            {
                styles.Add(currentStyle);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error parsing style definitions: {ex.Message}");
        }

        return styles;
    }

    private async Task GenerateStyleParameters(StyleDefinition style)
    {
        string prompt = $"Generate specific visual parameters for this horror style:\n" +
                       $"Style: {style.name}\n" +
                       $"Description: {style.description}\n" +
                       $"Visual Characteristics: {style.visualCharacteristics}\n\n" +
                       "Include specific values for:\n" +
                       "- Base intensity (0-1)\n" +
                       "- Color manipulation approach\n" +
                       "- Distortion patterns\n" +
                       "- Shadow/light interaction";

        string response = await llmManager.GenerateResponse(prompt);
        await UpdateStyleParameters(style, response);
    }

    private async Task UpdateStyleParameters(StyleDefinition style, string parameters)
    {
        try
        {
            var lines = parameters.Split('\n');
            foreach (var line in lines)
            {
                if (line.StartsWith("Base intensity:"))
                {
                    if (float.TryParse(line.Replace("Base intensity:", "").Trim(), out float intensity))
                    {
                        style.intensity = Mathf.Clamp01(intensity);
                    }
                }
                else if (line.StartsWith("Color manipulation:"))
                {
                    // Store color manipulation approach in visual characteristics
                    style.visualCharacteristics += "\nColor: " + line.Replace("Color manipulation:", "").Trim();
                }
                else if (line.StartsWith("Distortion:"))
                {
                    // Add distortion pattern to characteristics
                    style.visualCharacteristics += "\nDistortion: " + line.Replace("Distortion:", "").Trim();
                }
                else if (line.StartsWith("Shadow/light:"))
                {
                    // Add lighting info to characteristics
                    style.visualCharacteristics += "\nLighting: " + line.Replace("Shadow/light:", "").Trim();
                }
            }

            // Generate curve based on parameters
            style.transferCurve = GenerateTransferCurve(style);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error updating style parameters: {ex.Message}");
        }
    }

    private AnimationCurve GenerateTransferCurve(StyleDefinition style)
    {
        var curve = new AnimationCurve();
        
        if (style.visualCharacteristics.Contains("sudden") || style.visualCharacteristics.Contains("sharp"))
        {
            // Sharp transition curve
            curve.AddKey(new Keyframe(0, 0, 0, 0));
            curve.AddKey(new Keyframe(0.4f, 0.1f, 0.5f, 0.5f));
            curve.AddKey(new Keyframe(0.6f, 0.9f, 0.5f, 0.5f));
            curve.AddKey(new Keyframe(1, 1, 0, 0));
        }
        else if (style.visualCharacteristics.Contains("gradual") || style.visualCharacteristics.Contains("subtle"))
        {
            // Smooth transition curve
            curve.AddKey(new Keyframe(0, 0, 1, 1));
            curve.AddKey(new Keyframe(1, 1, 1, 1));
        }
        else
        {
            // Default curve
            curve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        }
        
        return curve;
    }

    public void ApplyHorrorStyle(Texture2D sourceTexture, string styleId, float intensity, System.Action<Texture2D> callback)
    {
        if (!activeStyles.TryGetValue(styleId, out StyleDefinition style))
        {
            Debug.LogError($"Horror style not found: {styleId}");
            return;
        }

        StartCoroutine(ProcessStyle(sourceTexture, style, intensity, callback));
    }

    private IEnumerator ProcessStyle(Texture2D sourceTexture, StyleDefinition style, float intensity, System.Action<Texture2D> callback)
    {
        // Create render texture for processing
        RenderTexture renderTexture = new RenderTexture(
            sourceTexture.width,
            sourceTexture.height,
            0,
            RenderTextureFormat.ARGBFloat
        );
        renderTexture.enableRandomWrite = true;
        renderTexture.Create();

        // Apply style transfer
        yield return StartCoroutine(ApplyStyleTransfer(sourceTexture, renderTexture, style, intensity));

        // Convert result back to Texture2D
        Texture2D resultTexture = new Texture2D(sourceTexture.width, sourceTexture.height);
        RenderTexture.active = renderTexture;
        resultTexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        resultTexture.Apply();

        // Apply post-processing effects
        ApplyHorrorPostProcessing(resultTexture, style, intensity);

        // Cleanup
        RenderTexture.active = null;
        renderTexture.Release();

        callback?.Invoke(resultTexture);
    }

    private IEnumerator ApplyStyleTransfer(Texture2D source, RenderTexture target, StyleDefinition style, float intensity)
    {
        ComputeShader shader = GetStyleShader(style.id);
        if (shader == null)
        {
            Debug.LogError($"Style shader not found for: {style.id}");
            yield break;
        }

        // Set shader parameters
        int kernelIndex = shader.FindKernel("StyleTransfer");
        shader.SetTexture(kernelIndex, "_SourceTex", source);
        shader.SetTexture(kernelIndex, "_ResultTex", target);
        shader.SetFloat("_Intensity", intensity * style.intensity);
        shader.SetFloat("_Time", Time.time);

        // Dispatch compute shader
        int threadGroupsX = Mathf.CeilToInt(source.width / 8.0f);
        int threadGroupsY = Mathf.CeilToInt(source.height / 8.0f);
        shader.Dispatch(kernelIndex, threadGroupsX, threadGroupsY, 1);

        yield return null;
    }

    private void ApplyHorrorPostProcessing(Texture2D texture, StyleDefinition style, float intensity)
    {
        switch (style.id)
        {
            case "dark_romanticism":
                ApplyDarkRomanticismEffects(texture, intensity);
                break;
            case "surreal_horror":
                ApplySurrealHorrorEffects(texture, intensity);
                break;
            case "cosmic_horror":
                ApplyCosmicHorrorEffects(texture, intensity);
                break;
            case "psychological_horror":
                ApplyPsychologicalHorrorEffects(texture, intensity);
                break;
        }
    }

    private void ApplyDarkRomanticismEffects(Texture2D texture, float intensity)
    {
        Color[] pixels = texture.GetPixels();
        
        for (int i = 0; i < pixels.Length; i++)
        {
            // Enhance shadows and contrast
            float luminance = pixels[i].grayscale;
            float darkened = Mathf.Pow(luminance, 1 + intensity * 0.5f);
            pixels[i] = Color.Lerp(pixels[i], new Color(darkened, darkened, darkened, 1), intensity);
            
            // Add subtle color tinting
            pixels[i] *= new Color(0.9f, 0.85f, 1f, 1f);
        }
        
        texture.SetPixels(pixels);
        texture.Apply();
    }

    private void ApplySurrealHorrorEffects(Texture2D texture, float intensity)
    {
        Color[] pixels = texture.GetPixels();
        int width = texture.width;
        int height = texture.height;
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;
                
                // Apply wave distortion
                float wave = Mathf.Sin((float)y / height * 10f + Time.time) * intensity * 0.1f;
                int offsetX = Mathf.RoundToInt(x + wave * width);
                offsetX = Mathf.Clamp(offsetX, 0, width - 1);
                
                // Color manipulation
                Color color = pixels[y * width + offsetX];
                color.r = Mathf.Sin(color.r * Mathf.PI) * intensity + color.r * (1 - intensity);
                color.b = Mathf.Sin(color.b * Mathf.PI) * intensity + color.b * (1 - intensity);
                
                pixels[index] = color;
            }
        }
        
        texture.SetPixels(pixels);
        texture.Apply();
    }

    private void ApplyCosmicHorrorEffects(Texture2D texture, float intensity)
    {
        Color[] pixels = texture.GetPixels();
        int width = texture.width;
        int height = texture.height;
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;
                
                // Create void-like effect
                float distance = Vector2.Distance(
                    new Vector2(x, y),
                    new Vector2(width/2, height/2)
                ) / (width * 0.5f);
                
                float voidEffect = Mathf.Pow(distance, 2) * intensity;
                
                // Color manipulation
                Color color = pixels[index];
                color = Color.Lerp(color, Color.black, voidEffect);
                color.r += Mathf.Sin(Time.time + distance) * intensity * 0.2f;
                color.b += Mathf.Cos(Time.time + distance) * intensity * 0.2f;
                
                pixels[index] = color;
            }
        }
        
        texture.SetPixels(pixels);
        texture.Apply();
    }

    private void ApplyPsychologicalHorrorEffects(Texture2D texture, float intensity)
    {
        Color[] pixels = texture.GetPixels();
        int width = texture.width;
        int height = texture.height;
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;
                
                // Create subtle pattern distortions
                float noise = Mathf.PerlinNoise(
                    (float)x / width * 5f + Time.time,
                    (float)y / height * 5f + Time.time
                );
                
                // Color manipulation based on psychological intensity
                Color color = pixels[index];
                float luminance = color.grayscale;
                
                color = Color.Lerp(
                    color,
                    new Color(
                        luminance + noise * 0.2f,
                        luminance - noise * 0.1f,
                        luminance + noise * 0.3f,
                        1
                    ),
                    intensity
                );
                
                pixels[index] = color;
            }
        }
        
        texture.SetPixels(pixels);
        texture.Apply();
    }

    private ComputeShader GetStyleShader(string styleId)
    {
        if (styleShaders.TryGetValue(styleId, out ComputeShader shader))
        {
            return shader;
        }

        // Load shader based on style
        string shaderName = $"StyleTransfer_{styleId}";
        shader = Resources.Load<ComputeShader>(shaderName);
        
        if (shader != null)
        {
            styleShaders[styleId] = shader;
        }
        
        return shader;
    }

    private void AddStyle(StyleDefinition style)
    {
        if (!activeStyles.ContainsKey(style.id))
        {
            activeStyles.Add(style.id, style);
        }
    }

    public StyleDefinition GetStyle(string styleId)
    {
        return activeStyles.TryGetValue(styleId, out StyleDefinition style) ? style : null;
    }

    public string[] GetAvailableStyles()
    {
        return new List<string>(activeStyles.Keys).ToArray();
    }

    public async Task<bool> ShouldGenerateNewStyles()
    {
        var gameState = GameManager.Instance.StateManager.GetCurrentState();
        string prompt = "Based on the current game state, should new horror styles be introduced?\n" +
                       $"Current Tension: {gameState.tensionLevel}\n" +
                       $"Active Styles: {activeStyles.Count}\n" +
                       $"Player State: {GetPlayerStateDescription()}";

        string response = await llmManager.GenerateResponse(prompt);
        return response.ToLower().Contains("yes");
    }

    private string GetPlayerStateDescription()
    {
        var profile = GameManager.Instance.ProfileManager.CurrentProfile;
        return $"Fear: {profile.FearLevel}, Obsession: {profile.ObsessionLevel}, Aggression: {profile.AggressionLevel}";
    }

    public async Task<StyleDefinition> GenerateContextualStyle(PlayerAnalysisProfile profile, Room currentRoom)
    {
        string prompt = $"Generate a horror style based on the current context:\n" +
                       $"Psychological State:\n" +
                       $"- Fear: {profile.FearLevel}\n" +
                       $"- Obsession: {profile.ObsessionLevel}\n" +
                       $"- Aggression: {profile.AggressionLevel}\n" +
                       $"Current Environment: {currentRoom.description}\n\n" +
                       "Design a unique visual style that reflects this psychological state and environment.";

        string response = await llmManager.GenerateResponse(prompt);
        var style = await CreateStyleFromResponse(response);
        await GenerateStyleParameters(style);
        
        return style;
    }

    private async Task<StyleDefinition> CreateStyleFromResponse(string response)
    {
        try
        {
            var style = new StyleDefinition($"style_{Guid.NewGuid():N}", "Generated Style");
            
            var lines = response.Split('\n');
            foreach (var line in lines)
            {
                if (line.StartsWith("Name:"))
                    style.name = line.Replace("Name:", "").Trim();
                else if (line.StartsWith("Description:"))
                    style.description = line.Replace("Description:", "").Trim();
                else if (line.StartsWith("Impact:"))
                    style.psychologicalImpact = line.Replace("Impact:", "").Trim();
                else if (line.StartsWith("Visual:"))
                    style.visualCharacteristics = line.Replace("Visual:", "").Trim();
            }

            // Set default values if missing
            if (string.IsNullOrEmpty(style.name))
                style.name = "Dynamic Horror Style";
            if (string.IsNullOrEmpty(style.description))
                style.description = "A dynamically generated horror style";
            
            style.intensity = CalculateInitialIntensity(style);
            style.transferCurve = GenerateTransferCurve(style);

            return style;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error creating style from response: {ex.Message}");
            return null;
        }
    }

    private float CalculateInitialIntensity(StyleDefinition style)
    {
        float intensity = 0.5f; // Base intensity

        // Adjust based on characteristics
        if (style.visualCharacteristics != null)
        {
            if (style.visualCharacteristics.Contains("strong") || 
                style.visualCharacteristics.Contains("intense") ||
                style.visualCharacteristics.Contains("dramatic"))
            {
                intensity += 0.2f;
            }
            if (style.visualCharacteristics.Contains("subtle") || 
                style.visualCharacteristics.Contains("gentle") ||
                style.visualCharacteristics.Contains("mild"))
            {
                intensity -= 0.2f;
            }
        }

        return Mathf.Clamp01(intensity);
    }
}