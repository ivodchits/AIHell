using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class LLMManager : MonoBehaviour
{
    // Singleton instance
    private static LLMManager _instance;
    public static LLMManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<LLMManager>();
                if (_instance == null)
                {
                    GameObject obj = new GameObject("LLMManager");
                    _instance = obj.AddComponent<LLMManager>();
                }
            }
            return _instance;
        }
    }

    // Current LLM provider
    [SerializeField]
    private LLMProvider currentProvider = LLMProvider.Gemini;
    
    // API settings
    [Header("Gemini API Settings")]
    [SerializeField] private string geminiApiKey = "AIzaSyAO3eBNDEw4gVi58sDnbDWAX03vTMMzBw8";
    [SerializeField] private string geminiApiUrl = "https://generativelanguage.googleapis.com/v1beta/models/{0}:generateContent";
    [SerializeField] private float geminiTemperature = 0.7f;
    [SerializeField] private int maxOutputTokens = 1024;
    [SerializeField] private ModelData[] models;
    
    [Header("Local LLM Settings")]
    [SerializeField] private string localLLMApiUrl = "http://localhost:8000/api/generate";
    [SerializeField] private float localLLMTemperature = 0.7f;
    
    [Header("Rate Limiting")]
    [SerializeField] private float minTimeBetweenRequests = 1.0f;
    private float lastRequestTime = 0f;
    
    // Fallback mechanism
    [SerializeField] private bool useLocalLLMFallback = true;
    [SerializeField] private int maxRetryAttempts = 3;
    
    // For task completion and callbacks
    private class TaskCompletionSource<T>
    {
        private bool isCompleted = false;
        private T result;
        private Exception exception;
        
        public bool IsCompleted => isCompleted;
        
        public void SetResult(T value)
        {
            isCompleted = true;
            result = value;
        }
        
        public void SetException(Exception ex)
        {
            isCompleted = true;
            exception = ex;
        }
        
        public T GetResult()
        {
            if (!isCompleted)
                throw new InvalidOperationException("Task not completed");
                
            if (exception != null)
                throw exception;
                
            return result;
        }
    }
    
    private void Awake()
    {
        // Ensure singleton behavior
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        
        _instance = this;
        DontDestroyOnLoad(this.gameObject);
        
        // Try to load API key from PlayerPrefs if not set in inspector
        if (string.IsNullOrEmpty(geminiApiKey))
        {
            geminiApiKey = PlayerPrefs.GetString("GeminiApiKey", "");
        }
    }
    
    public LLMProvider CurrentProvider => currentProvider;

    /// <summary>
    /// Set the active LLM provider
    /// </summary>
    /// <param name="provider">The LLM provider to use</param>
    public void SetLLMProvider(LLMProvider provider)
    {
        currentProvider = provider;
        Debug.Log($"LLM Provider set to: {provider}");
    }
    
    /// <summary>
    /// Set the Gemini API key (can be used at runtime)
    /// </summary>
    /// <param name="apiKey">The API key to use</param>
    public void SetGeminiApiKey(string apiKey)
    {
        geminiApiKey = apiKey;
        PlayerPrefs.SetString("GeminiApiKey", apiKey);
    }
    
    /// <summary>
    /// Sends a prompt to the current LLM provider
    /// </summary>
    /// <param name="prompt">The prompt to send</param>
    /// <param name="modelType">The model to use</param>
    /// <returns>LLM response text</returns>
    public async Task<string> SendPromptToLLM(string prompt, ModelType modelType, int retryCount = 0)
    {
        // Rate limiting
        float timeSinceLastRequest = Time.time - lastRequestTime;
        if (timeSinceLastRequest < minTimeBetweenRequests)
        {
            await Task.Delay((int)((minTimeBetweenRequests - timeSinceLastRequest) * 1000));
        }
        
        lastRequestTime = Time.time;
        
        try
        {
            string response = "";
            
            switch (currentProvider)
            {
                case LLMProvider.Gemini:
                    // Using the coroutine-based method through a TaskCompletionSource
                    TaskCompletionSource<string> geminiTcs = new TaskCompletionSource<string>();
                    StartCoroutine(SendPromptToGeminiCoroutine(prompt, modelType, (result) => geminiTcs.SetResult(result), 
                                                              (exception) => geminiTcs.SetException(exception)));
                    response = await Task.Run(() => {
                        // Poll until the task is completed
                        while (!geminiTcs.IsCompleted)
                        {
                            Task.Delay(10).Wait();  // Small delay to prevent CPU thrashing
                        }
                        return geminiTcs.GetResult();
                    });
                    break;
                    
                case LLMProvider.LocalLLM:
                    // Using the coroutine-based method through a TaskCompletionSource
                    TaskCompletionSource<string> localTcs = new TaskCompletionSource<string>();
                    StartCoroutine(SendPromptToLocalLLMCoroutine(prompt, (result) => localTcs.SetResult(result), 
                                                                (exception) => localTcs.SetException(exception)));
                    response = await Task.Run(() => {
                        // Poll until the task is completed
                        while (!localTcs.IsCompleted)
                        {
                            Task.Delay(10).Wait();  // Small delay to prevent CPU thrashing
                        }
                        return localTcs.GetResult();
                    });
                    break;
            }
            
            return ParseLLMResponse(response, currentProvider);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error sending prompt to {currentProvider}: {e.Message}");
            
            // Implement fallback mechanism
            if (useLocalLLMFallback && currentProvider == LLMProvider.Gemini && retryCount < maxRetryAttempts)
            {
                Debug.Log("Falling back to Local LLM...");
                LLMProvider originalProvider = currentProvider;
                currentProvider = LLMProvider.LocalLLM;
                string result = await SendPromptToLLM(prompt, modelType, retryCount + 1);
                currentProvider = originalProvider;
                return result;
            }
            else if (retryCount < maxRetryAttempts)
            {
                Debug.Log($"Retry attempt {retryCount + 1}...");
                await Task.Delay(1000); // Wait a second before retrying
                return await SendPromptToLLM(prompt, modelType, retryCount + 1);
            }
            
            return "Error: Unable to generate content. Please try again later.";
        }
    }
    
    /// <summary>
    /// Coroutine version of sending prompt to Gemini
    /// </summary>
    private IEnumerator SendPromptToGeminiCoroutine(string prompt, ModelType modelType, Action<string> onSuccess, Action<Exception> onError)
    {
        if (string.IsNullOrEmpty(geminiApiKey))
        {
            onError(new Exception("Gemini API Key is not set"));
            yield break;
        }
        
        // Construct the API URL with API key
        var model = Array.Find(models, m => m.modelType == modelType);
        var urlWithModel = string.Format(geminiApiUrl, model.modelName);
        string apiUrlWithKey = $"{urlWithModel}?key={geminiApiKey}";
        
        // Create the request payload
        string requestJson = $@"{{
            ""contents"": [
                {{
                    ""role"": ""user"",
                    ""parts"": [
                        {{
                            ""text"": ""{EscapeJsonString(prompt)}""
                        }}
                    ]
                }}
            ],
            ""generationConfig"": {{
                ""temperature"": {geminiTemperature},
                ""maxOutputTokens"": {maxOutputTokens},
                ""topK"": 40,
                ""topP"": 0.95
            }}
        }}";
        
        // Create and send the request
        using (UnityWebRequest request = new UnityWebRequest(apiUrlWithKey, "POST"))
        {
            byte[] jsonToSend = new UTF8Encoding().GetBytes(requestJson);
            request.uploadHandler = new UploadHandlerRaw(jsonToSend);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            
            // Send the request
            yield return request.SendWebRequest();
            
            if (request.result != UnityWebRequest.Result.Success)
            {
                onError(new Exception($"API Error: {request.error}"));
                yield break;
            }
            
            onSuccess(request.downloadHandler.text);
        }
    }
    
    /// <summary>
    /// Coroutine version of sending prompt to local LLM
    /// </summary>
    private IEnumerator SendPromptToLocalLLMCoroutine(string prompt, Action<string> onSuccess, Action<Exception> onError)
    {
        // Create the request payload - adjust based on your local LLM API format
        string requestJson = $@"{{
            ""prompt"": ""{EscapeJsonString(prompt)}"",
            ""temperature"": {localLLMTemperature},
            ""max_tokens"": {maxOutputTokens}
        }}";
        
        // Create and send the request
        using (UnityWebRequest request = new UnityWebRequest(localLLMApiUrl, "POST"))
        {
            byte[] jsonToSend = new UTF8Encoding().GetBytes(requestJson);
            request.uploadHandler = new UploadHandlerRaw(jsonToSend);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            
            // Send the request
            yield return request.SendWebRequest();
            
            if (request.result != UnityWebRequest.Result.Success)
            {
                onError(new Exception($"Local LLM Error: {request.error}"));
                yield break;
            }
            
            onSuccess(request.downloadHandler.text);
        }
    }
    
    /// <summary>
    /// Parses the LLM response based on the provider
    /// </summary>
    private string ParseLLMResponse(string response, LLMProvider provider)
    {
        try
        {
            switch (provider)
            {
                case LLMProvider.Gemini:
                    // Parse Gemini response JSON (adjust the parsing based on the actual response structure)
                    var geminiJson = JsonUtility.FromJson<GeminiResponse>(response);
                    if (geminiJson != null && geminiJson.candidates != null && geminiJson.candidates.Length > 0)
                    {
                        return geminiJson.candidates[0].content.parts[0].text;
                    }
                    break;
                    
                case LLMProvider.LocalLLM:
                    // Parse Local LLM response (adjust based on your local LLM API format)
                    var localJson = JsonUtility.FromJson<LocalLLMResponse>(response);
                    if (localJson != null)
                    {
                        return localJson.generated_text;
                    }
                    break;
            }
            
            Debug.LogWarning("Failed to parse LLM response: " + response);
            return "Error parsing response";
        }
        catch (Exception e)
        {
            Debug.LogError($"Error parsing LLM response: {e.Message}");
            return "Error parsing response";
        }
    }
    
    /// <summary>
    /// Escapes a string for use in JSON
    /// </summary>
    private string EscapeJsonString(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }
        
        StringBuilder sb = new StringBuilder(input.Length + 10);
        foreach (char c in input)
        {
            switch (c)
            {
                case '\\': sb.Append("\\\\"); break;
                case '\"': sb.Append("\\\""); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                case '\b': sb.Append("\\b"); break;
                case '\f': sb.Append("\\f"); break;
                default: sb.Append(c); break;
            }
        }
        
        return sb.ToString();
    }
}

// Enum to define different LLM providers
public enum LLMProvider
{
    Gemini,
    LocalLLM
}

public enum ModelType
{
    Flash,
    Lite,
    Pro
}

[Serializable]
public class ModelData
{
    public ModelType modelType;
    public string modelName;
}

// Classes to deserialize LLM responses
[Serializable]
public class GeminiResponse
{
    public GeminiCandidate[] candidates;
}

[Serializable]
public class GeminiCandidate
{
    public GeminiContent content;
}

[Serializable]
public class GeminiContent
{
    public GeminiPart[] parts;
}

[Serializable]
public class GeminiPart
{
    public string text;
}

[Serializable]
public class LocalLLMResponse
{
    public string generated_text;
}