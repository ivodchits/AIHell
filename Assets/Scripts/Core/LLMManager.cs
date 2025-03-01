using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using System;
using AIHell.Core.Data;

public class LLMManager : MonoBehaviour
{
    private Queue<LLMRequest> requestQueue;
    private bool isProcessing;
    private Dictionary<string, float> temperatureModifiers;
    private Dictionary<string, string> contextCache;
    private List<ContextualMemory> shortTermMemory;
    private const int MAX_SHORT_TERM_MEMORIES = 10;
    private const float DEFAULT_TEMPERATURE = 0.7f;

    [System.Serializable]
    public class LLMRequest
    {
        public string prompt;
        public string type;
        public float temperature;
        public int maxTokens;
        public TaskCompletionSource<string> completionSource;
        public string[] requiredElements;
        public string contextKey;
    }

    [System.Serializable]
    public class ContextualMemory
    {
        public string type;
        public string content;
        public float relevance;
        public float emotionalImpact;
        public System.DateTime timestamp;
    }

    [System.Serializable]
    public class LLMConfigurationData
    {
        public string configType;
        public Dictionary<string, float> parameters;
        public Dictionary<string, string> templates;
        public float adaptationRate;
        public System.DateTime lastUpdate;
    }

    private void Awake()
    {
        InitializeManager();
    }

    private void InitializeManager()
    {
        requestQueue = new Queue<LLMRequest>();
        temperatureModifiers = new Dictionary<string, float>();
        contextCache = new Dictionary<string, string>();
        shortTermMemory = new List<ContextualMemory>();

        InitializeTemperatureModifiers();
    }

    private void InitializeTemperatureModifiers()
    {
        // Higher temperature for more creative/variable responses
        temperatureModifiers["event_generation"] = 0.8f;
        temperatureModifiers["manifestation"] = 0.85f;
        temperatureModifiers["pattern_recognition"] = 0.7f;
        temperatureModifiers["emotional_filter"] = 0.75f;
        temperatureModifiers["style_generation"] = 0.8f;
        temperatureModifiers["room_description"] = 0.75f;
        temperatureModifiers["character_dialogue"] = 0.8f;
        
        // Lower temperature for more consistent/focused responses
        temperatureModifiers["analysis"] = 0.5f;
        temperatureModifiers["parameter_generation"] = 0.4f;
        temperatureModifiers["psychological_impact"] = 0.6f;
        temperatureModifiers["validation"] = 0.3f;
        temperatureModifiers["coherence_check"] = 0.4f;
    }

    public async Task<string> GenerateResponse(string prompt, string type = "default", string[] requiredElements = null)
    {
        var request = new LLMRequest
        {
            prompt = prompt,
            type = type,
            temperature = GetTemperature(type),
            maxTokens = CalculateMaxTokens(prompt),
            completionSource = new TaskCompletionSource<string>(),
            requiredElements = requiredElements,
            contextKey = GenerateContextKey(prompt)
        };

        // Check cache for recent similar prompts
        if (contextCache.TryGetValue(request.contextKey, out string cachedResponse))
        {
            if (ValidateResponse(cachedResponse, requiredElements))
            {
                return cachedResponse;
            }
        }

        requestQueue.Enqueue(request);

        if (!isProcessing)
        {
            ProcessRequestQueue();
        }

        return await request.completionSource.Task;
    }

    public async Task<string> GenerateRoomDescription(Room room, Level level)
    {
        var promptBuilder = new StringBuilder();
        promptBuilder.AppendLine($"Generate a psychological horror description for a room in level {level.LevelNumber}.");
        promptBuilder.AppendLine($"Level theme: {level.Theme}");
        promptBuilder.AppendLine($"Room archetype: {room.Archetype}");
        
        var profile = GameManager.Instance.ProfileManager.CurrentProfile;
        if (profile != null)
        {
            promptBuilder.AppendLine("\nPsychological State:");
            promptBuilder.AppendLine($"Fear Level: {profile.FearLevel}");
            promptBuilder.AppendLine($"Obsession Level: {profile.ObsessionLevel}");
            promptBuilder.AppendLine($"Aggression Level: {profile.AggressionLevel}");
        }

        string[] requiredElements = { room.Archetype.ToLower(), level.Theme.ToLower() };
        return await GenerateResponse(promptBuilder.ToString(), "room_description", requiredElements);
    }

    public async Task<string> GenerateEventDescription(string context, PlayerAnalysisProfile profile)
    {
        var promptBuilder = new StringBuilder();
        promptBuilder.AppendLine("Generate a psychological horror event based on the following context:");
        promptBuilder.AppendLine(context);
        promptBuilder.AppendLine("\nPsychological State:");
        promptBuilder.AppendLine($"Fear Level: {profile.FearLevel}");
        promptBuilder.AppendLine($"Obsession Level: {profile.ObsessionLevel}");
        promptBuilder.AppendLine($"Aggression Level: {profile.AggressionLevel}");

        return await GenerateResponse(promptBuilder.ToString(), "event_generation");
    }

    private void ProcessRequestQueue()
    {
        isProcessing = true;
        ProcessNextRequest();
    }

    private async void ProcessNextRequest()
    {
        if (requestQueue.Count == 0)
        {
            isProcessing = false;
            return;
        }

        var request = requestQueue.Dequeue();
        
        try
        {
            string enhancedPrompt = await EnhancePrompt(request);
            string response = await SendToLLM(enhancedPrompt, request);

            if (ValidateResponse(response, request.requiredElements))
            {
                // Cache valid response
                contextCache[request.contextKey] = response;
                
                // Add to contextual memory if relevant
                if (IsSignificantResponse(response))
                {
                    AddToShortTermMemory(new ContextualMemory
                    {
                        type = request.type,
                        content = response,
                        relevance = CalculateRelevance(response),
                        emotionalImpact = CalculateEmotionalImpact(response),
                        timestamp = System.DateTime.Now
                    });
                }

                request.completionSource.SetResult(response);
            }
            else
            {
                // Retry with modified prompt
                await RetryRequest(request);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error processing LLM request: {e.Message}");
            request.completionSource.SetException(e);
        }

        // Process next request
        ProcessNextRequest();
    }

    private async Task<string> EnhancePrompt(LLMRequest request)
    {
        var promptBuilder = new StringBuilder(request.prompt);

        // Add relevant context from short-term memory
        var relevantMemories = await Task.Run(() => GetRelevantMemories(request.type));
        if (relevantMemories.Count > 0)
        {
            promptBuilder.AppendLine("\nRelevant Context:");
            foreach (var memory in relevantMemories)
            {
                promptBuilder.AppendLine($"- {memory.content}");
            }
        }

        // Add psychological context
        var profile = GameManager.Instance.ProfileManager.CurrentProfile;
        if (profile != null)
        {
            promptBuilder.AppendLine("\nPsychological State:");
            promptBuilder.AppendLine($"Fear Level: {profile.FearLevel}");
            promptBuilder.AppendLine($"Obsession Level: {profile.ObsessionLevel}");
            promptBuilder.AppendLine($"Aggression Level: {profile.AggressionLevel}");
        }

        // Add emotional context
        var emotionalSystem = GameManager.Instance.GetComponent<EmotionalResponseSystem>();
        if (emotionalSystem != null)
        {
            var emotions = emotionalSystem.GetEmotionalState();
            if (emotions != null && emotions.Count > 0)
            {
                promptBuilder.AppendLine("\nEmotional State:");
                foreach (var emotion in emotions)
                {
                    promptBuilder.AppendLine($"{emotion.Key}: {emotion.Value}");
                }
            }
        }

        return promptBuilder.ToString();
    }

    private async Task<string> SendToLLM(string prompt, LLMRequest request)
    {
        try
        {
            // Configure request parameters
            var parameters = new Dictionary<string, object>
            {
                { "prompt", prompt },
                { "temperature", request.temperature },
                { "max_tokens", request.maxTokens },
                { "model", GetModelForType(request.type) }
            };

            // Make API request to Gemini (or other LLM service)
            string apiKey = await GetApiKey();
            string endpoint = await GetApiEndpoint();

            using (var client = new System.Net.Http.HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
                
                var content = new System.Net.Http.StringContent(
                    UnityEngine.JsonUtility.ToJson(parameters),
                    System.Text.Encoding.UTF8,
                    "application/json"
                );

                var response = await client.PostAsync(endpoint, content);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();
                    var parsedResponse = UnityEngine.JsonUtility.FromJson<LLMResponse>(result);
                    return parsedResponse.choices[0].text;
                }
                else
                {
                    throw new Exception($"LLM API error: {response.StatusCode}");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error in SendToLLM: {ex.Message}");
            throw;
        }
    }

    private string GetModelForType(string type)
    {
        // Select appropriate model based on request type
        switch (type)
        {
            case "room_description":
            case "event_generation":
            case "manifestation":
                return "gemini-pro-vision"; // For rich descriptive content
            case "analysis":
            case "validation":
            case "coherence_check":
                return "gemini-pro"; // For logical processing
            default:
                return "gemini-pro";
        }
    }

    private async Task<string> GetApiKey()
    {
        // In production, this would be securely stored/retrieved
        return await Task.FromResult("YOUR_API_KEY");
    }

    private async Task<string> GetApiEndpoint()
    {
        return await Task.FromResult("https://api.gemini.ai/v1/chat/completions");
    }

    private bool ValidateResponse(string response, string[] requiredElements)
    {
        if (string.IsNullOrEmpty(response))
            return false;

        if (requiredElements == null || requiredElements.Length == 0)
            return true;

        foreach (var element in requiredElements)
        {
            if (!response.Contains(element))
                return false;
        }

        return true;
    }

    private async Task RetryRequest(LLMRequest request)
    {
        // Increase temperature slightly for more variety
        request.temperature = Mathf.Min(request.temperature + 0.1f, 1.0f);
        
        string modifiedPrompt = $"Previous attempt did not meet requirements. Please ensure the response includes: {string.Join(", ", request.requiredElements)}\n\n{request.prompt}";
        
        string response = await SendToLLM(modifiedPrompt, request);
        
        if (ValidateResponse(response, request.requiredElements))
        {
            request.completionSource.SetResult(response);
        }
        else
        {
            // If retry fails, return a failure message
            request.completionSource.SetResult($"Failed to generate valid response after retry. Required elements: {String.Join(", ", request.requiredElements)}");
        }
    }

    private float GetTemperature(string type)
    {
        return temperatureModifiers.TryGetValue(type, out float temp) ? temp : DEFAULT_TEMPERATURE;
    }

    private int CalculateMaxTokens(string prompt)
    {
        // Calculate appropriate max tokens based on prompt length and type
        // Base calculation: 2x prompt length, capped at 2048
        return Mathf.Min(2048, prompt.Length * 2);
    }

    private string GenerateContextKey(string prompt)
    {
        // Generate a context key for caching based on prompt content
        return System.Security.Cryptography.MD5.Create()
            .ComputeHash(System.Text.Encoding.UTF8.GetBytes(prompt))
            .ToString();
    }

    private bool IsSignificantResponse(string response)
    {
        // Determine if response should be added to short-term memory
        if (string.IsNullOrEmpty(response)) return false;
        
        // Check response length
        if (response.Length > 100) return true;
        
        // Check for significant keywords
        string[] significantKeywords = { "significant", "important", "crucial", "vital", "key", "critical" };
        foreach (var keyword in significantKeywords)
        {
            if (response.Contains(keyword)) return true;
        }
        
        return false;
    }

    private float CalculateRelevance(string response)
    {
        if (string.IsNullOrEmpty(response)) return 0f;

        float relevance = 0.5f; // Base relevance

        // Adjust based on response properties
        if (response.Length > 200) relevance += 0.2f;
        if (response.Contains("psychological")) relevance += 0.1f;
        if (response.Contains("horror")) relevance += 0.1f;
        
        return Mathf.Clamp01(relevance);
    }

    private float CalculateEmotionalImpact(string response)
    {
        if (string.IsNullOrEmpty(response)) return 0f;

        float impact = 0.5f; // Base impact

        // Emotional keywords and their weights
        var emotionalKeywords = new Dictionary<string, float>
        {
            { "fear", 0.2f },
            { "terror", 0.3f },
            { "dread", 0.25f },
            { "horror", 0.2f },
            { "panic", 0.25f },
            { "anxiety", 0.15f }
        };

        foreach (var keyword in emotionalKeywords)
        {
            if (response.Contains(keyword.Key))
            {
                impact += keyword.Value;
            }
        }

        return Mathf.Clamp01(impact);
    }

    private void AddToShortTermMemory(ContextualMemory memory)
    {
        shortTermMemory.Add(memory);
        
        // Remove oldest memories if we exceed the limit
        while (shortTermMemory.Count > MAX_SHORT_TERM_MEMORIES)
        {
            shortTermMemory.RemoveAt(0);
        }
    }

    private List<ContextualMemory> GetRelevantMemories(string type)
    {
        // Get memories relevant to the current request type
        return shortTermMemory.FindAll(m => 
            m.type == type || 
            m.relevance > 0.7f || 
            m.emotionalImpact > 0.7f
        );
    }

    public void ClearContextCache()
    {
        contextCache.Clear();
    }

    public void ClearShortTermMemory()
    {
        shortTermMemory.Clear();
    }

    [System.Serializable]
    private class LLMResponse
    {
        public Choice[] choices;

        [System.Serializable]
        public class Choice
        {
            public string text;
        }
    }
}