using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

[Serializable]
public class StableDiffusionRequest
{
    public string prompt;
    public string negative_prompt;
    public int steps;
    public float cfg_scale;
    public int width;
    public int height;
    public string sampler_name;
    public bool enable_hr;
}

[Serializable]
public class StableDiffusionResponse
{
    public string[] images;
}

public class ImagePipeline : MonoBehaviour
{
    [System.Serializable]
    public class PipelineConfig
    {
        public string modelName;
        public int baseResolution = 512;
        public float aspectRatio = 1.0f;
        public bool useUpscaling = false;
        public float promptStrength = 7.0f;
        public int steps = 30;
        public string scheduler = "DPM++ 2M Karras";
    }

    [System.Serializable]
    public class GenerationMetadata
    {
        public string prompt;
        public string negativePrompt;
        public float psychologicalIntensity;
        public string dominantEmotion;
        public System.DateTime timestamp;
    }

    private Dictionary<string, PipelineConfig> modelConfigs;
    private Queue<GenerationRequest> requestQueue;
    private bool isProcessing;

    private class GenerationRequest
    {
        public string prompt;
        public PipelineConfig config;
        public GenerationMetadata metadata;
        public System.Action<Texture2D> callback;
    }

    private void Awake()
    {
        InitializePipeline();
    }

    private void InitializePipeline()
    {
        modelConfigs = new Dictionary<string, PipelineConfig>();
        requestQueue = new Queue<GenerationRequest>();

        // Initialize base models
        InitializeModelConfigs();
    }

    private void InitializeModelConfigs()
    {
        // Base horror model configuration
        modelConfigs["horror_base"] = new PipelineConfig
        {
            modelName = "dreamshaper_8.safetensors",
            baseResolution = 768,
            aspectRatio = 1.0f,
            useUpscaling = true,
            promptStrength = 7.5f,
            steps = 35
        };

        // Psychological model configuration
        modelConfigs["psychological"] = new PipelineConfig
        {
            modelName = "realisticVisionV51_v51VAE.safetensors",
            baseResolution = 512,
            aspectRatio = 1.0f,
            useUpscaling = true,
            promptStrength = 8.0f,
            steps = 40
        };

        // Surreal model configuration
        modelConfigs["surreal"] = new PipelineConfig
        {
            modelName = "deliberate_v3.safetensors",
            baseResolution = 640,
            aspectRatio = 1.0f,
            useUpscaling = true,
            promptStrength = 8.5f,
            steps = 45
        };
    }

    public void QueueImageGeneration(string prompt, string type, float psychologicalIntensity, string dominantEmotion, System.Action<Texture2D> callback)
    {
        var config = SelectModelConfig(type, psychologicalIntensity);
        var metadata = new GenerationMetadata
        {
            prompt = prompt,
            negativePrompt = GenerateNegativePrompt(type),
            psychologicalIntensity = psychologicalIntensity,
            dominantEmotion = dominantEmotion,
            timestamp = System.DateTime.Now
        };

        var request = new GenerationRequest
        {
            prompt = prompt,
            config = config,
            metadata = metadata,
            callback = callback
        };

        requestQueue.Enqueue(request);

        if (!isProcessing)
        {
            StartCoroutine(ProcessGenerationQueue());
        }
    }

    public async Task<bool> QueueImageGenerationAsync(string prompt, string type, float psychologicalIntensity, string dominantEmotion, Action<Texture2D> callback)
    {
        try
        {
            var config = SelectModelConfig(type, psychologicalIntensity);
            var metadata = new GenerationMetadata
            {
                prompt = prompt,
                negativePrompt = GenerateNegativePrompt(type),
                psychologicalIntensity = psychologicalIntensity,
                dominantEmotion = dominantEmotion,
                timestamp = DateTime.Now
            };

            var request = new GenerationRequest
            {
                prompt = prompt,
                config = config,
                metadata = metadata,
                callback = callback
            };

            requestQueue.Enqueue(request);

            if (!isProcessing)
            {
                _ = ProcessGenerationQueueAsync();
            }

            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error queueing image generation: {ex.Message}");
            return false;
        }
    }

    private PipelineConfig SelectModelConfig(string type, float intensity)
    {
        // Select appropriate model based on type and intensity
        string modelKey = type switch
        {
            "psychological" when intensity > 0.8f => "psychological",
            "surreal" when intensity > 0.7f => "surreal",
            _ => "horror_base"
        };

        var config = modelConfigs[modelKey];

        // Adjust configuration based on intensity
        config.steps = Mathf.RoundToInt(config.steps * (1 + intensity * 0.2f));
        config.promptStrength = config.promptStrength * (1 + intensity * 0.1f);

        return config;
    }

    private string GenerateNegativePrompt(string type)
    {
        List<string> negatives = new List<string>
        {
            "gore", "blood", "explicit violence", "nsfw",
            "low quality", "blurry", "bad anatomy",
            "watermark", "signature", "text", "logo"
        };

        switch (type)
        {
            case "psychological":
                negatives.AddRange(new[] { "cartoon", "anime", "3d", "painting" });
                break;
            case "surreal":
                negatives.AddRange(new[] { "photorealistic", "boring", "plain" });
                break;
            default:
                negatives.AddRange(new[] { "bright", "cheerful", "happy" });
                break;
        }

        return string.Join(", ", negatives);
    }

    private System.Collections.IEnumerator ProcessGenerationQueue()
    {
        isProcessing = true;

        while (requestQueue.Count > 0)
        {
            var request = requestQueue.Dequeue();
            yield return StartCoroutine(GenerateImage(request));
            yield return new WaitForSeconds(0.5f); // Rate limiting
        }

        isProcessing = false;
    }

    private async Task ProcessGenerationQueueAsync()
    {
        isProcessing = true;

        while (requestQueue.Count > 0)
        {
            try
            {
                var request = requestQueue.Dequeue();
                await GenerateImageAsync(request);
                await Task.Delay(500); // Rate limiting
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error processing generation queue: {ex.Message}");
            }
        }

        isProcessing = false;
    }

    private System.Collections.IEnumerator GenerateImage(GenerationRequest request)
    {
        // Prepare API request data
        var requestData = new StableDiffusionRequest
        {
            prompt = request.prompt,
            negative_prompt = request.metadata.negativePrompt,
            steps = request.config.steps,
            cfg_scale = request.config.promptStrength,
            width = request.config.baseResolution,
            height = Mathf.RoundToInt(request.config.baseResolution * request.config.aspectRatio),
            sampler_name = request.config.scheduler,
            enable_hr = request.config.useUpscaling
        };

        // Send to Stable Diffusion API
        using (UnityWebRequest www = new UnityWebRequest($"{GameManager.Instance.ImageGenerator.STABLE_DIFFUSION_URL}/sdapi/v1/txt2img", "POST"))
        {
            byte[] jsonData = System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(requestData));
            www.uploadHandler = new UploadHandlerRaw(jsonData);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                var response = JsonUtility.FromJson<StableDiffusionResponse>(www.downloadHandler.text);
                if (response != null && response.images != null && response.images.Length > 0)
                {
                    string base64Image = response.images[0];
                    Texture2D texture = ProcessGeneratedImage(base64Image);
                    
                    if (texture != null)
                    {
                        SaveImageWithMetadata(texture, request.metadata);
                        request.callback?.Invoke(texture);
                    }
                }
            }
            else
            {
                Debug.LogError($"Image generation error: {www.error}");
            }
        }
    }

    private async Task GenerateImageAsync(GenerationRequest request)
    {
        // Prepare API request data
        var requestData = new StableDiffusionRequest
        {
            prompt = request.prompt,
            negative_prompt = request.metadata.negativePrompt,
            steps = request.config.steps,
            cfg_scale = request.config.promptStrength,
            width = request.config.baseResolution,
            height = Mathf.RoundToInt(request.config.baseResolution * request.config.aspectRatio),
            sampler_name = request.config.scheduler,
            enable_hr = request.config.useUpscaling
        };

        try
        {
            using (UnityWebRequest www = new UnityWebRequest($"{GameManager.Instance.ImageGenerator.STABLE_DIFFUSION_URL}/sdapi/v1/txt2img", "POST"))
            {
                byte[] jsonData = System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(requestData));
                www.uploadHandler = new UploadHandlerRaw(jsonData);
                www.downloadHandler = new DownloadHandlerBuffer();
                www.SetRequestHeader("Content-Type", "application/json");

                var operation = www.SendWebRequest();
                while (!operation.isDone)
                    await Task.Yield();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    var response = JsonUtility.FromJson<StableDiffusionResponse>(www.downloadHandler.text);
                    if (response?.images != null && response.images.Length > 0)
                    {
                        string base64Image = response.images[0];
                        Texture2D texture = await ProcessGeneratedImageAsync(base64Image);
                        
                        if (texture != null)
                        {
                            await SaveImageWithMetadataAsync(texture, request.metadata);
                            request.callback?.Invoke(texture);
                        }
                    }
                }
                else
                {
                    throw new Exception($"Image generation failed: {www.error}");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error in GenerateImageAsync: {ex.Message}");
            throw;
        }
    }

    private void SaveImageWithMetadata(Texture2D texture, GenerationMetadata metadata)
    {
        try
        {
            string filename = $"horror_{metadata.timestamp:yyyyMMddHHmmss}.png";
            string filepath = System.IO.Path.Combine(GameManager.Instance.ImageGenerator.IMAGE_SAVE_PATH, filename);
            
            // Save image
            byte[] bytes = texture.EncodeToPNG();
            System.IO.File.WriteAllBytes(filepath, bytes);
            
            // Save metadata
            string metadataPath = filepath.Replace(".png", "_metadata.json");
            string metadataJson = JsonUtility.ToJson(metadata, true);
            System.IO.File.WriteAllText(metadataPath, metadataJson);
            
            Debug.Log($"Saved image and metadata to: {filepath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error saving image and metadata: {e.Message}");
        }
    }

    private async Task SaveImageWithMetadataAsync(Texture2D texture, GenerationMetadata metadata)
    {
        try
        {
            string filename = $"horror_{metadata.timestamp:yyyyMMddHHmmss}.png";
            string filepath = System.IO.Path.Combine(GameManager.Instance.ImageGenerator.IMAGE_SAVE_PATH, filename);
            
            byte[] bytes = texture.EncodeToPNG();
            await Task.Run(() => {
                System.IO.File.WriteAllBytes(filepath, bytes);
                
                string metadataPath = filepath.Replace(".png", "_metadata.json");
                string metadataJson = JsonUtility.ToJson(metadata, true);
                System.IO.File.WriteAllText(metadataPath, metadataJson);
            });
            
            Debug.Log($"Saved image and metadata to: {filepath}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error saving image and metadata: {ex.Message}");
            throw;
        }
    }

    private Texture2D ProcessGeneratedImage(string base64Image)
    {
        try
        {
            byte[] imageBytes = System.Convert.FromBase64String(base64Image);
            Texture2D texture = new Texture2D(2, 2);
            
            if (texture.LoadImage(imageBytes))
            {
                return texture;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error processing generated image: {e.Message}");
        }

        return null;
    }

    private async Task<Texture2D> ProcessGeneratedImageAsync(string base64Image)
    {
        try
        {
            byte[] imageBytes = Convert.FromBase64String(base64Image);
            var texture = new Texture2D(2, 2);
            
            await Task.Run(() => {
                if (!texture.LoadImage(imageBytes))
                {
                    throw new Exception("Failed to load image data");
                }
            });

            return texture;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error processing generated image: {ex.Message}");
            return null;
        }
    }

    public void UpdateModelConfig(string modelKey, PipelineConfig config)
    {
        if (modelConfigs.ContainsKey(modelKey))
        {
            modelConfigs[modelKey] = config;
        }
        else
        {
            modelConfigs.Add(modelKey, config);
        }
    }

    public PipelineConfig GetModelConfig(string modelKey)
    {
        return modelConfigs.TryGetValue(modelKey, out var config) ? config : null;
    }
}