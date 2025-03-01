using UnityEngine;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using UnityEngine.Networking;
using AIHell.Core.Data;
using AIHell.Core;

[RequireComponent(typeof(LLMManager))]
[RequireComponent(typeof(ImagePostProcessor))]
public class ImageGenerator : MonoBehaviour
{
    public string STABLE_DIFFUSION_URL { get; private set; }
    public string IMAGE_SAVE_PATH { get; private set; }
    
    private LLMManager llmManager;
    private ImagePostProcessor postProcessor;
    private Queue<GenerationRequest> requestQueue;
    private bool isProcessing;
    private Dictionary<string, GenerationParameters> baseParameters;

    [System.Serializable]
    public class GenerationRequest
    {
        public Room contextRoom;
        public string prompt;
        public GenerationParameters parameters;
        public Action<Texture2D> callback;
        public float psychologicalIntensity;
    }

    [System.Serializable]
    public class GenerationParameters
    {
        public int width = 512;
        public int height = 512;
        public int steps = 30;
        public float cfgScale = 7.0f;
        public string scheduler = "DPM++ 2M Karras";
        public bool useUpscaling = false;
        public string negativePrompt;
        public string styleName;
        public string[] requiredElements;
    }

    private void Awake()
    {
        InitializeGenerator();
    }

    private void InitializeGenerator()
    {
        llmManager = GetComponent<LLMManager>();
        postProcessor = GetComponent<ImagePostProcessor>();
        requestQueue = new Queue<GenerationRequest>();
        baseParameters = new Dictionary<string, GenerationParameters>();

        // Initialize configuration
        STABLE_DIFFUSION_URL = "http://localhost:7860";
        IMAGE_SAVE_PATH = System.IO.Path.Combine(Application.persistentDataPath, "GeneratedImages");
        System.IO.Directory.CreateDirectory(IMAGE_SAVE_PATH);

        InitializeBaseParameters();
    }

    private void InitializeBaseParameters()
    {
        // Psychological horror parameters
        baseParameters["psychological"] = new GenerationParameters {
            width = 768,
            height = 768,
            steps = 35,
            cfgScale = 8.0f,
            useUpscaling = true,
            negativePrompt = "gore, blood, explicit violence, nsfw, cartoon, anime, watermark",
            styleName = "psychological_horror",
            requiredElements = new[] { "shadows", "atmosphere", "psychological" }
        };

        // Surreal horror parameters
        baseParameters["surreal"] = new GenerationParameters {
            width = 640,
            height = 640,
            steps = 40,
            cfgScale = 8.5f,
            useUpscaling = true,
            negativePrompt = "gore, blood, explicit violence, nsfw, photorealistic, plain",
            styleName = "surreal_horror",
            requiredElements = new[] { "surreal", "distortion", "dreamlike" }
        };

        // Cosmic horror parameters
        baseParameters["cosmic"] = new GenerationParameters {
            width = 896,
            height = 896,
            steps = 45,
            cfgScale = 9.0f,
            useUpscaling = true,
            negativePrompt = "gore, blood, explicit violence, nsfw, human, mundane",
            styleName = "cosmic_horror",
            requiredElements = new[] { "cosmic", "unknowable", "vast" }
        };
    }

    public void RequestContextualImage(Room room, Action<Texture2D> callback)
    {
        var request = new GenerationRequest {
            contextRoom = room,
            callback = callback,
            psychologicalIntensity = room.PsychologicalIntensity
        };

        requestQueue.Enqueue(request);

        if (!isProcessing)
        {
            _ = ProcessQueueAsync();
        }
    }

    private async Task ProcessQueueAsync()
    {
        isProcessing = true;

        while (requestQueue.Count > 0)
        {
            try
            {
                var request = requestQueue.Dequeue();
                string prompt = await GenerateImagePrompt(request);
                var parameters = SelectGenerationParameters(request);
                
                Texture2D generatedImage = await GenerateImage(prompt, parameters);
                if (generatedImage != null)
                {
                    // Apply post-processing
                    generatedImage = await ApplyPsychologicalEffects(generatedImage, request);
                    request.callback?.Invoke(generatedImage);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error processing image request: {ex.Message}");
            }

            await Task.Delay(500); // Rate limiting
        }

        isProcessing = false;
    }

    private async Task<string> GenerateImagePrompt(GenerationRequest request)
    {
        var promptBuilder = new StringBuilder();
        promptBuilder.AppendLine($"Generate image prompt for room: {request.contextRoom.BaseDescription}");
        
        // Add psychological context
        var profile = GameManager.Instance.ProfileManager.CurrentProfile;
        promptBuilder.AppendLine("\nPsychological State:");
        promptBuilder.AppendLine($"Fear: {profile.FearLevel}");
        promptBuilder.AppendLine($"Obsession: {profile.ObsessionLevel}");
        promptBuilder.AppendLine($"Aggression: {profile.AggressionLevel}");

        // Add room context
        promptBuilder.AppendLine("\nRoom Context:");
        promptBuilder.AppendLine($"Archetype: {request.contextRoom.Archetype}");
        if (request.contextRoom.Events.Count > 0)
        {
            promptBuilder.AppendLine("Recent Events:");
            foreach (var evt in request.contextRoom.Events.Take(2))
            {
                promptBuilder.AppendLine($"- {evt.Description}");
            }
        }

        string response = await llmManager.GenerateResponse(promptBuilder.ToString(), "image_prompt");
        return response;
    }

    private GenerationParameters SelectGenerationParameters(GenerationRequest request)
    {
        string parameterType = DetermineParameterType(request);
        var parameters = baseParameters[parameterType].Clone() as GenerationParameters;

        // Adjust parameters based on psychological intensity
        parameters.steps = Mathf.RoundToInt(parameters.steps * (1 + request.psychologicalIntensity * 0.2f));
        parameters.cfgScale = parameters.cfgScale * (1 + request.psychologicalIntensity * 0.1f);

        return parameters;
    }

    private string DetermineParameterType(GenerationRequest request)
    {
        var profile = GameManager.Instance.ProfileManager.CurrentProfile;

        if (profile.FearLevel > 0.7f)
            return "psychological";
        if (profile.ObsessionLevel > 0.7f)
            return "surreal";
        if (request.psychologicalIntensity > 0.8f)
            return "cosmic";

        return "psychological";
    }

    private async Task<Texture2D> GenerateImage(string prompt, GenerationParameters parameters)
    {
        try
        {
            var requestData = new Dictionary<string, object>
            {
                { "prompt", prompt },
                { "negative_prompt", parameters.negativePrompt },
                { "steps", parameters.steps },
                { "cfg_scale", parameters.cfgScale },
                { "width", parameters.width },
                { "height", parameters.height },
                { "sampler_name", parameters.scheduler },
                { "enable_hr", parameters.useUpscaling }
            };

            using (var client = new UnityWebRequest($"{STABLE_DIFFUSION_URL}/sdapi/v1/txt2img", "POST"))
            {
                string jsonData = JsonUtility.ToJson(requestData);
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
                client.uploadHandler = new UploadHandlerRaw(bodyRaw);
                client.downloadHandler = new DownloadHandlerBuffer();
                client.SetRequestHeader("Content-Type", "application/json");

                var operation = client.SendWebRequest();
                while (!operation.isDone)
                    await Task.Yield();

                if (client.result == UnityWebRequest.Result.Success)
                {
                    var response = JsonUtility.FromJson<StableDiffusionResponse>(client.downloadHandler.text);
                    if (response?.images != null && response.images.Length > 0)
                    {
                        return ProcessGeneratedImage(response.images[0]);
                    }
                }
                else
                {
                    throw new Exception($"Image generation failed: {client.error}");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error generating image: {ex.Message}");
        }

        return null;
    }

    private Texture2D ProcessGeneratedImage(string base64Image)
    {
        try
        {
            byte[] imageBytes = Convert.FromBase64String(base64Image);
            Texture2D texture = new Texture2D(2, 2);
            
            if (texture.LoadImage(imageBytes))
            {
                return texture;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error processing generated image: {ex.Message}");
        }

        return null;
    }

    private async Task<Texture2D> ApplyPsychologicalEffects(Texture2D texture, GenerationRequest request)
    {
        try
        {
            // Get the style definition
            var styleProcessor = GameManager.Instance.GetComponent<StyleTransferProcessor>();
            var style = styleProcessor.GetStyle(request.parameters.styleName);

            if (style != null)
            {
                var processedTexture = new Texture2D(texture.width, texture.height);
                Graphics.CopyTexture(texture, processedTexture);

                // Apply psychological post-processing
                postProcessor.ApplyPsychologicalDistortion(
                    processedTexture,
                    GameManager.Instance.ProfileManager.CurrentProfile
                );

                // Apply style transfer
                var tcs = new TaskCompletionSource<Texture2D>();
                styleProcessor.ApplyHorrorStyle(
                    processedTexture,
                    style.id,
                    request.psychologicalIntensity,
                    (result) => tcs.SetResult(result)
                );

                return await tcs.Task;
            }

            return texture;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error applying psychological effects: {ex.Message}");
            return texture;
        }
    }

    [System.Serializable]
    private class StableDiffusionResponse
    {
        public string[] images;
    }
}