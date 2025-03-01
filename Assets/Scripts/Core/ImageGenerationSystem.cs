using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Networking;
using System.IO;
using AIHell.Core.Data;  // Add the missing namespace

namespace AIHell.Core
{
    public class ImageGenerationSystem : MonoBehaviour
    {
        private const string STABLE_DIFFUSION_URL = "http://localhost:7860"; // Default Stable Diffusion WebUI port
        private const string IMAGE_SAVE_PATH = "Assets/GeneratedImages/";
        private LLMManager llmManager;
        private Queue<ImageGenerationRequest> requestQueue;
        private bool isProcessing;
        private ImagePromptGenerator promptGenerator;

        [System.Serializable]
        public class ImageGenerationRequest
        {
            public string context;
            public string psychologicalState;
            public string emotionalTone;
            public System.Action<Texture2D> onImageGenerated;
        }

        [System.Serializable]
        private class StableDiffusionRequest
        {
            public string prompt;
            public string negative_prompt;
            public int steps = 30;
            public int width = 512;
            public int height = 512;
            public float cfg_scale = 7.0f;
            public int batch_size = 1;
            public bool enable_hr = false;
        }

        private void Awake()
        {
            llmManager = GameManager.Instance.LLMManager;
            promptGenerator = gameObject.AddComponent<ImagePromptGenerator>();
            requestQueue = new Queue<ImageGenerationRequest>();
            Directory.CreateDirectory(IMAGE_SAVE_PATH);
        }

        public async Task RequestContextualImage(Room room, System.Action<Texture2D> callback)
        {
            var profile = GameManager.Instance.ProfileManager.CurrentProfile;
            var emotional = GameManager.Instance.EmotionalResponseSystem;

            string prompt = await promptGenerator.GeneratePrompt(
                room.DescriptionText,
                profile,
                emotional
            );

            GenerateImageFromPrompt(prompt, callback);
        }

        // public void RequestPhobiaImage(PhobiaManager.Phobia phobia, float intensity, System.Action<Texture2D> callback)
        // {
        //     string prompt = promptGenerator.GeneratePhobiaPrompt(phobia, intensity);
        //     GenerateImageFromPrompt(prompt, callback);
        // }

        // public void RequestArchetypalImage(ConsciousnessAnalyzer.ConsciousnessPattern pattern, System.Action<Texture2D> callback)
        // {
        //     string prompt = promptGenerator.GenerateArchetypalPrompt(pattern);
        //     GenerateImageFromPrompt(prompt, callback);
        // }

        private void GenerateImageFromPrompt(string prompt, System.Action<Texture2D> callback)
        {
            var request = new ImageGenerationRequest
            {
                context = prompt,
                psychologicalState = "",  // Already incorporated in the prompt
                emotionalTone = "",       // Already incorporated in the prompt
                onImageGenerated = callback
            };

            requestQueue.Enqueue(request);

            if (!isProcessing)
            {
                StartCoroutine(ProcessImageQueue());
            }
        }

        private IEnumerator ProcessImageQueue()
        {
            isProcessing = true;

            while (requestQueue.Count > 0)
            {
                var request = requestQueue.Dequeue();
                
                yield return StartCoroutine(GenerateImageDescriptionAsync(request, (description) => {
                    if (!string.IsNullOrEmpty(description))
                    {
                        StartCoroutine(GenerateImage(description, request.onImageGenerated));
                    }
                }));

                yield return new WaitForSeconds(1f); // Rate limiting
            }

            isProcessing = false;
        }

        private IEnumerator GenerateImageDescriptionAsync(ImageGenerationRequest request, System.Action<string> callback)
        {
            // Format prompt for LLM
            string prompt = FormatLLMPrompt(request);
            
            // Get response from LLM
            Task<string> responseTask = llmManager.GenerateResponse(prompt);
            
            while (!responseTask.IsCompleted)
            {
                yield return null;
            }

            string description = ProcessLLMResponse(responseTask.Result);
            callback(description);
        }

        private string FormatLLMPrompt(ImageGenerationRequest request)
        {
            return $@"Generate a detailed image description for a psychological horror game scene.
Context: {request.context}
Psychological State: {request.psychologicalState}
Emotional Tone: {request.emotionalTone}

Requirements:
- Focus on creating an unsettling, psychologically disturbing atmosphere
- Use specific visual details that convey psychological horror
- Include lighting, shadows, and atmosphere
- Avoid explicit gore or violence
- Keep the description clear enough for an AI image generator

Format your response as a detailed image prompt that could be used by Stable Diffusion.";
        }

        private string ProcessLLMResponse(string response)
        {
            // Add style keywords and modifiers for better Stable Diffusion results
            string enhancedPrompt = $"{response}, psychological horror, dark atmosphere, unsettling, cinematic lighting, highly detailed, surreal";
            
            // Add negative prompt elements
            string negativePrompt = "gore, blood, explicit violence, nsfw, low quality, blurry";

            return $"{enhancedPrompt} ### {negativePrompt}";
        }

        private IEnumerator GenerateImage(string imageDescription, System.Action<Texture2D> callback)
        {
            string[] promptParts = imageDescription.Split(new[] { "###" }, System.StringSplitOptions.RemoveEmptyEntries);
            string mainPrompt = promptParts[0].Trim();
            string negativePrompt = promptParts.Length > 1 ? promptParts[1].Trim() : "";

            var requestData = new StableDiffusionRequest
            {
                prompt = mainPrompt,
                negative_prompt = negativePrompt,
                steps = DetermineSamplingSteps(mainPrompt),
                cfg_scale = DetermineCFGScale(mainPrompt)
            };

            string jsonData = JsonUtility.ToJson(requestData);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);

            using (UnityWebRequest www = new UnityWebRequest($"{STABLE_DIFFUSION_URL}/sdapi/v1/txt2img", "POST"))
            {
                www.uploadHandler = new UploadHandlerRaw(bodyRaw);
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
                            SaveGeneratedImage(texture);
                            callback?.Invoke(texture);
                        }
                    }
                }
                else
                {
                    Debug.LogError($"Error generating image: {www.error}");
                }
            }
        }

        private int DetermineSamplingSteps(string prompt)
        {
            // Increase steps for more complex or important scenes
            if (prompt.Contains("psychological horror") || prompt.Contains("archetype"))
                return 40;
            if (prompt.Contains("phobia") || prompt.Contains("fear"))
                return 35;
            return 30;
        }

        private float DetermineCFGScale(string prompt)
        {
            // Adjust CFG scale based on desired reality/surrealism balance
            if (prompt.Contains("surreal") || prompt.Contains("impossible"))
                return 8.5f;
            if (prompt.Contains("realistic") || prompt.Contains("hyperrealistic"))
                return 7.0f;
            return 7.5f;
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

        private void SaveGeneratedImage(Texture2D texture)
        {
            try
            {
                string filename = $"horror_scene_{System.DateTime.Now:yyyyMMddHHmmss}.png";
                string filepath = Path.Combine(IMAGE_SAVE_PATH, filename);
                
                byte[] bytes = texture.EncodeToPNG();
                File.WriteAllBytes(filepath, bytes);
                
                Debug.Log($"Saved generated image to: {filepath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error saving generated image: {e.Message}");
            }
        }

        [System.Serializable]
        private class StableDiffusionResponse
        {
            public string[] images;
        }
    }
}