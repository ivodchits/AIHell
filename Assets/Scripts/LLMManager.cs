using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class LLMManager : MonoBehaviour
{
    [SerializeField] StatisticsManager statisticsManager;
    [SerializeField] ImageGenerator imageGenerator;
    
    // API settings
    [Header("Gemini API Settings")]
    [SerializeField]
    string geminiApiKey = "AIzaSyAO3eBNDEw4gVi58sDnbDWAX03vTMMzBw8";
    [SerializeField] string geminiApiUrl = "https://generativelanguage.googleapis.com/v1beta/models/{0}:generateContent";
    [SerializeField] float geminiTemperature = 0.7f;
    [SerializeField] int maxOutputTokens = 1024;
    [SerializeField] ModelData[] models;
    
    [Header("Local LLM Settings")]
    [SerializeField]
    string localLLMApiUrl = "http://localhost:8000/api/generate";
    [SerializeField] float localLLMTemperature = 0.7f;
    [SerializeField] ModelData[] localModels;
    
    [Header("Rate Limiting")]
    [SerializeField]
    float minTimeBetweenRequests = 1.0f;

    float lastRequestTime = 0f;
    
    // Fallback mechanism
    [SerializeField] bool useLocalLLMFallback = true;
    [SerializeField] int maxRetryAttempts = 3;
    
    //TODO: a mechanism to restrict too frequent requests to Gemini
    
    // For task completion and callbacks
    class TaskCompletionSource<T>
    {
        bool isCompleted = false;
        T result;
        Exception exception;
        
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
            var formattedResponse = EscapeJsonString(parsedResponse);;
            chat.AddEntry(new ChatEntry(isUser: false, formattedResponse));
            return formattedResponse;
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
    IEnumerator SendPromptToGeminiCoroutine(LLMChat chat, Action<string> onSuccess, Action<Exception> onError)
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
                ""maxOutputTokens"": {maxOutputTokens}
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
    IEnumerator SendPromptToLocalLLMCoroutine(LLMChat chat, Action<string> onSuccess,
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
    string ParseLLMResponse(string response, LLMChat chat)
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
                        statisticsManager.Add(chat.ChatName, geminiResponse);
                        if (geminiResponse != null)
                        {
                            return geminiResponse.candidates[0].content.parts[0].text;
                        }
                    }
                    catch (Exception)
                    {
                        var geminiResponse = JsonUtility.FromJson<GeminiResponseAlternative>(response);
                        statisticsManager.Add(chat.ChatName, geminiResponse);
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
                    statisticsManager.Add(chat.ChatName, localResponse);
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
    string EscapeJsonString(string input)
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