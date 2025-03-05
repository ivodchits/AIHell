using System;
using System.Collections;
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
    [SerializeField] private ModelData[] localModels;
    
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
    public async Task<string> SendPromptToLLM(string prompt, LLMChat chat, int retryCount = 0)
    {
        var processedPrompt = EscapeJsonString(prompt); 
        Debug.Log($"Sending prompt to LLM (provider {chat.LLMProvider}, model type {chat.ModelType}):\n{processedPrompt}");
        
        chat.AddEntry(new ChatEntry(isUser: true, processedPrompt));
        return await SendPromptToLLM(chat, retryCount);
    }
    
    async Task<string> SendPromptToLLM(LLMChat chat, int retryCount = 0)
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
            
            switch (chat.LLMProvider)
            {
                case LLMProvider.Gemini:
                    // Using the coroutine-based method through a TaskCompletionSource
                    TaskCompletionSource<string> geminiTcs = new TaskCompletionSource<string>();
                    StartCoroutine(SendPromptToGeminiCoroutine(chat, (result) => geminiTcs.SetResult(result), 
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
                    StartCoroutine(SendPromptToLocalLLMCoroutine(chat, (result) => localTcs.SetResult(result), 
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
            
            var parsedResponse = ParseLLMResponse(response, chat);
            chat.AddEntry(new ChatEntry(isUser: false, parsedResponse));
            return parsedResponse;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error sending prompt to {chat.LLMProvider}: {e.Message}");
            
            // Implement fallback mechanism
            if (useLocalLLMFallback && chat.LLMProvider == LLMProvider.Gemini && retryCount < maxRetryAttempts)
            {
                Debug.Log("Falling back to Local LLM...");
                chat.ConvertToLocal();
                string result = await SendPromptToLLM(chat, retryCount + 1);
                return result;
            }
            else if (retryCount < maxRetryAttempts)
            {
                Debug.Log($"Retry attempt {retryCount + 1}...");
                await Task.Delay(1000); // Wait a second before retrying
                return await SendPromptToLLM(chat, retryCount + 1);
            }
            
            return "Error: Unable to generate content. Please try again later.";
        }
    }
    
    /// <summary>
    /// Coroutine version of sending prompt to Gemini
    /// </summary>
    private IEnumerator SendPromptToGeminiCoroutine(LLMChat chat, Action<string> onSuccess, Action<Exception> onError)
    {
        if (string.IsNullOrEmpty(geminiApiKey))
        {
            onError(new Exception("Gemini API Key is not set"));
            yield break;
        }
        
        // Construct the API URL with API key
        var model = Array.Find(models, m => m.modelType == chat.ModelType);
        var urlWithModel = geminiApiUrl + model.modelName;
        string apiUrlWithKey = $"{urlWithModel}:generateContent?key={geminiApiKey}";
        
        // Create the request payload
        string requestJson = $@"{{
            ""contents"": [
                {chat.GetChatHistoryAsString()}
            ],
            ""generationConfig"": {{
                ""temperature"": {geminiTemperature},
                ""maxOutputTokens"": {maxOutputTokens},
                ""topK"": 40,
                ""topP"": 0.95
            }}
        }}";
        
        // Create and send the request
        using (UnityWebRequest request = UnityWebRequest.Post(apiUrlWithKey, requestJson))
        {
            request.SetRequestHeader("Content-Type", "application/json");
            var jsonToSend = new UTF8Encoding().GetBytes(requestJson);
            request.uploadHandler = new UploadHandlerRaw(jsonToSend);
            request.downloadHandler = new DownloadHandlerBuffer();
            Debug.Log("Sending request to Gemini API:\n" + requestJson);
            
            // Send the request
            yield return request.SendWebRequest();
            
            if (request.result != UnityWebRequest.Result.Success)
            {
                onError(new Exception($"API Error: {request.error}"));
                yield break;
            }

            while (!request.isDone || !request.downloadHandler.isDone)
            {
                yield return null;
            }
            
            onSuccess(request.downloadHandler.text);
        }
    }
    
    /// <summary>
    /// Coroutine version of sending prompt to local LLM
    /// </summary>
    private IEnumerator SendPromptToLocalLLMCoroutine(LLMChat chat, Action<string> onSuccess,
        Action<Exception> onError)
    {
        // Create the request payload in the same format as Gemini
        var model = Array.Find(localModels, m => m.modelType == chat.ModelType);
        string requestJson = $@"{{
            ""model"": ""{model.modelName}"",
            ""messages"": [
                {chat.GetChatHistoryAsString()}
            ],
            ""temperature"": {localLLMTemperature},
            ""stream"": false
        }}";
        
        // Create and send the request
        using (UnityWebRequest request = UnityWebRequest.Post(localLLMApiUrl, requestJson))
        {
            byte[] jsonToSend = new UTF8Encoding().GetBytes(requestJson);
            request.uploadHandler = new UploadHandlerRaw(jsonToSend);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            Debug.Log("Sending request to LMStudio API:\n" + requestJson);

            // Send the request
            yield return request.SendWebRequest();
            
            if (request.result != UnityWebRequest.Result.Success)
            {
                onError(new Exception($"Local LLM Error: {request.error}"));
                yield break;
            }

            while (!request.downloadHandler.isDone)
            {
                yield return null;
            }
            
            onSuccess(request.downloadHandler.text);
        }
    }
    
    /// <summary>
    /// Parses the LLM response based on the provider
    /// </summary>
    private string ParseLLMResponse(string response, LLMChat chat)
    {
        try
        {
            switch (chat.LLMProvider)
            {
                case LLMProvider.Gemini:
                    // Parse Gemini response JSON (adjust the parsing based on the actual response structure)
                    Debug.Log($"Raw Gemini response:\n{response}");
                    try
                    {
                        var geminiResponse = JsonUtility.FromJson<GeminiResponse>(response);
                        StatisticsManager.Instance.Add(chat.ChatName, geminiResponse);
                        if (geminiResponse != null)
                        {
                            return geminiResponse.candidates[0].content.parts[0].text;
                        }
                    }
                    catch (Exception)
                    {
                        var geminiResponse = JsonUtility.FromJson<GeminiResponseAlternative>(response);
                        StatisticsManager.Instance.Add(chat.ChatName, geminiResponse);
                        if (geminiResponse != null)
                        {
                            return geminiResponse.candidates[0].parts[0].text;
                        }
                    }
                    break;
                    
                case LLMProvider.LocalLLM:
                    // Parse Local LLM response (adjust based on your local LLM API format)
                    Debug.Log($"Raw LMStudio response:\n{response}");
                    var localResponse = JsonUtility.FromJson<LocalLLMResponse>(response);
                    StatisticsManager.Instance.Add(chat.ChatName, localResponse);
                    if (localResponse != null)
                    {
                        return localResponse.choices[0].message.content;
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