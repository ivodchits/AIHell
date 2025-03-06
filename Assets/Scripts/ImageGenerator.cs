using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;

/// <summary>
/// Handles image generation requests to ComfyUI server
/// </summary>
public class ImageGenerator : MonoBehaviour
{
    [Header("ComfyUI Settings")]
    [SerializeField] string comfyUIServerURL = "http://127.0.0.1:8188";
    [SerializeField] bool useHighResolution = false;
    [SerializeField] string defaultCheckpoint = "v1-5-pruned-emaonly.safetensors";
    
    [Header("Default Parameters")]
    [SerializeField] int width = 512;
    [SerializeField] int height = 512;
    [SerializeField] int steps = 20;
    [SerializeField] float cfg = 7.0f;
    [SerializeField] float denoise = 1.0f;
    [SerializeField] string sampler = "euler";
    [SerializeField] string scheduler = "normal";
    [SerializeField] string defaultNegativePrompt = "bad hands, blurry, distorted, disfigured, poor quality";

    /// <summary>
    /// Inner class to represent simplified JSON handling since we can't use JObject directly
    /// </summary>
    [Serializable]
    public class SimpleJsonObject
    {
        public Dictionary<string, object> data = new Dictionary<string, object>();
        
        public SimpleJsonObject GetNode(string nodeId)
        {
            if (!data.ContainsKey(nodeId))
            {
                data[nodeId] = new SimpleJsonObject();
            }
            return (SimpleJsonObject)data[nodeId];
        }
        
        public void SetNodeClass(string nodeId, string className)
        {
            var node = GetNode(nodeId);
            node.data["class_type"] = className;
        }
        
        public SimpleJsonObject GetNodeInputs(string nodeId)
        {
            var node = GetNode(nodeId);
            if (!node.data.ContainsKey("inputs"))
            {
                node.data["inputs"] = new SimpleJsonObject();
            }
            return (SimpleJsonObject)node.data["inputs"];
        }
        
        public void SetNodeInputValue(string nodeId, string inputKey, object value)
        {
            var inputs = GetNodeInputs(nodeId);
            inputs.data[inputKey] = value;
        }
        
        public void SetNodeInputConnection(string nodeId, string inputKey, string sourceNodeId, int outputIndex)
        {
            var inputs = GetNodeInputs(nodeId);
            inputs.data[inputKey] = new object[] { sourceNodeId, outputIndex };
        }
        
        public string ToJson()
        {
            return JsonUtility.ToJson(this, true);
        }
        
        // Custom JSON serialization since Unity's JsonUtility doesn't handle Dictionary well
        public string SerializeToJson()
        {
            StringBuilder sb = new StringBuilder();
            SerializeObject(sb, this);
            return sb.ToString();
        }

        void SerializeObject(StringBuilder sb, SimpleJsonObject obj)
        {
            sb.Append("{");
            bool first = true;
            
            foreach (var kvp in obj.data)
            {
                if (!first) sb.Append(",");
                first = false;
                
                sb.Append("\"").Append(kvp.Key).Append("\":");
                SerializeValue(sb, kvp.Value);
            }
            
            sb.Append("}");
        }

        void SerializeValue(StringBuilder sb, object value)
        {
            if (value == null)
            {
                sb.Append("null");
                return;
            }
            
            if (value is string s)
            {
                sb.Append("\"").Append(s.Replace("\"", "\\\"")).Append("\"");
                return;
            }
            
            if (value is int || value is float || value is double || value is bool)
            {
                sb.Append(value.ToString().ToLower());
                return;
            }
            
            if (value is SimpleJsonObject obj)
            {
                SerializeObject(sb, obj);
                return;
            }
            
            if (value is object[] array)
            {
                sb.Append("[");
                for (int i = 0; i < array.Length; i++)
                {
                    if (i > 0) sb.Append(",");
                    SerializeValue(sb, array[i]);
                }
                sb.Append("]");
                return;
            }
            
            // Default case
            sb.Append("\"").Append(value.ToString()).Append("\"");
        }
    }

    /// <summary>
    /// Generate an image using the ComfyUI API
    /// </summary>
    /// <param name="prompt">The text prompt for image generation</param>
    /// <param name="negativePrompt">Optional negative prompt to guide what to avoid</param>
    /// <returns>URL to the generated image on the ComfyUI server</returns>
    public async Task<Texture2D> GenerateImage(string prompt, string negativePrompt = null)
    {
        if (string.IsNullOrEmpty(negativePrompt))
        {
            negativePrompt = defaultNegativePrompt;
        }

        int actualSteps = steps; //Mathf.RoundToInt(steps * imageQuality);
        actualSteps = Mathf.Max(10, actualSteps); // Ensure minimum of 10 steps
        
        int actualWidth = useHighResolution ? width * 2 : width;
        int actualHeight = useHighResolution ? height * 2 : height;
        
        // Create a seed based on time
        int seed = Mathf.RoundToInt(UnityEngine.Random.Range(0, 1000000));
        
        // Create workflow nodes
        var workflow = new SimpleJsonObject();
        
        // Checkpoint Loader
        workflow.SetNodeClass("4", "CheckpointLoaderSimple");
        workflow.SetNodeInputValue("4", "ckpt_name", defaultCheckpoint);
        
        // Empty Latent Image
        workflow.SetNodeClass("5", "EmptyLatentImage");
        workflow.SetNodeInputValue("5", "batch_size", 1);
        workflow.SetNodeInputValue("5", "height", actualHeight);
        workflow.SetNodeInputValue("5", "width", actualWidth);
        
        // Positive CLIP Text Encode
        workflow.SetNodeClass("6", "CLIPTextEncode");
        workflow.SetNodeInputConnection("6", "clip", "4", 1);
        workflow.SetNodeInputValue("6", "text", prompt);
        
        // Negative CLIP Text Encode
        workflow.SetNodeClass("7", "CLIPTextEncode");
        workflow.SetNodeInputConnection("7", "clip", "4", 1);
        workflow.SetNodeInputValue("7", "text", negativePrompt);
        
        // KSampler
        workflow.SetNodeClass("3", "KSampler");
        workflow.SetNodeInputValue("3", "cfg", cfg);
        workflow.SetNodeInputValue("3", "denoise", denoise);
        workflow.SetNodeInputConnection("3", "latent_image", "5", 0);
        workflow.SetNodeInputConnection("3", "model", "4", 0);
        workflow.SetNodeInputConnection("3", "negative", "7", 0);
        workflow.SetNodeInputConnection("3", "positive", "6", 0);
        workflow.SetNodeInputValue("3", "sampler_name", sampler);
        workflow.SetNodeInputValue("3", "scheduler", scheduler);
        workflow.SetNodeInputValue("3", "seed", seed);
        workflow.SetNodeInputValue("3", "steps", actualSteps);
        
        // VAE Decode
        workflow.SetNodeClass("8", "VAEDecode");
        workflow.SetNodeInputConnection("8", "samples", "3", 0);
        workflow.SetNodeInputConnection("8", "vae", "4", 2);
        
        // Save Image
        workflow.SetNodeClass("9", "SaveImage");
        workflow.SetNodeInputValue("9", "filename_prefix", "AIHell");
        workflow.SetNodeInputConnection("9", "images", "8", 0);
        
        // Send prompt to ComfyUI
        string jsonWorkflow = workflow.SerializeToJson();
        Debug.Log("Sending workflow to ComfyUI: " + jsonWorkflow);
        
        // Create a task completion source to handle the async workflow
        TaskCompletionSource<Texture2D> tcs = new TaskCompletionSource<Texture2D>();
        
        StartCoroutine(SendComfyUIPromptCoroutine(jsonWorkflow, 
            (result) => tcs.SetResult(result), 
            (error) => tcs.SetException(new Exception(error))));
        
        return await tcs.Task;
    }
    
    /// <summary>
    /// Sends a prompt workflow to ComfyUI
    /// </summary>
    IEnumerator SendComfyUIPromptCoroutine(string promptJson, Action<Texture2D> onSuccess, Action<string> onError)
    {
        // Wrap the prompt in the expected format
        string jsonData = $"{{\"prompt\": {promptJson}}}";
        
        // Create the request
        using (UnityWebRequest request = new UnityWebRequest(comfyUIServerURL + "/prompt", "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            Debug.Log("Sending prompt to ComfyUI:\n" + jsonData);
            
            // Send the request
            yield return request.SendWebRequest();
            
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("ComfyUI Request Error: " + request.error);
                onError?.Invoke("Failed to send prompt to ComfyUI: " + request.error);
                yield break;
            }
            
            string responseText = request.downloadHandler.text;
            Debug.Log("ComfyUI Response: " + responseText);

            // Simple parsing since we can't use JObject
            int startIndex = responseText.IndexOf("\"prompt_id\": \"") + "\"prompt_id\": \"".Length;
            int endIndex = responseText.IndexOf("\"", startIndex);
            string promptId = responseText.Substring(startIndex, endIndex - startIndex);
            
            // Now we need to wait for the image to be generated
            yield return new WaitForSeconds(2f); // Initial wait
            
            // Polling for image completion
            bool isComplete = false;
            string imageUrl = null;
            int maxRetries = 30;
            int retryCount = 0;
            
            while (!isComplete && retryCount < maxRetries)
            {
                yield return StartCoroutine(CheckImageStatusCoroutine(promptId, 
                    (url) => { isComplete = true; imageUrl = url; }, 
                    () => { retryCount++; }));
                
                if (!isComplete)
                {
                    yield return new WaitForSeconds(1f);
                }
            }
            
            if (isComplete && !string.IsNullOrEmpty(imageUrl))
            {
                // Once we have the image URL, download it directly
                Texture2D downloadedImage = null;
                yield return StartCoroutine(DownloadImageCoroutine(imageUrl, (texture) => downloadedImage = texture));
                
                if (downloadedImage != null)
                {
                    Debug.Log("Image successfully downloaded directly");
                }
                
                // Still pass the URL back for compatibility with existing code
                onSuccess?.Invoke(downloadedImage);
            }
            else
            {
                onError?.Invoke("Image generation timed out or failed");
            }
        }
    }
    
    /// <summary>
    /// Downloads the generated image directly from the ComfyUI server
    /// </summary>
    /// <param name="imageUrl">URL to the generated image</param>
    /// <param name="onComplete">Callback when the download is complete, returns the downloaded texture</param>
    /// <returns>Coroutine IEnumerator</returns>
    IEnumerator DownloadImageCoroutine(string imageUrl, Action<Texture2D> onComplete)
    {
        Debug.Log("Downloading image from: " + imageUrl);
        
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(imageUrl))
        {
            yield return request.SendWebRequest();
            
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Failed to download image: " + request.error);
                onComplete?.Invoke(null);
                yield break;
            }
            
            Debug.Log("Image download complete");
            Texture2D texture = ((DownloadHandlerTexture)request.downloadHandler).texture;
            onComplete?.Invoke(texture);
        }
    }
    
    /// <summary>
    /// Checks the status of an image generation request
    /// </summary>
    IEnumerator CheckImageStatusCoroutine(string promptId, Action<string> onComplete, Action onNotReady)
    {
        // First check the history endpoint to see if our prompt has been processed
        using (UnityWebRequest historyRequest = UnityWebRequest.Get(comfyUIServerURL + "/history"))
        {
            yield return historyRequest.SendWebRequest();
            
            if (historyRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("ComfyUI History Request Error: " + historyRequest.error);
                onNotReady?.Invoke();
                yield break;
            }
            
            string historyResponse = historyRequest.downloadHandler.text;
            
            // Check if our promptId is in the history
            if (historyResponse.Contains(promptId))
            {
                // Now get the image output from node 9
                string nodeId = "9";
                
                // Find the image filename (simple parsing since we can't use JObject)
                int startIndex = historyResponse.IndexOf(promptId);
                int outputsIndex = historyResponse.IndexOf("\"outputs\"", startIndex);
                int nodeIndex = historyResponse.IndexOf("\"" + nodeId + "\"", outputsIndex);
                int imagesIndex = historyResponse.IndexOf("\"images\"", nodeIndex);
                int filenameStartIndex = historyResponse.IndexOf("\"filename\": \"", imagesIndex) + "\"filename\": \"".Length;
                int filenameEndIndex = historyResponse.IndexOf("\"", filenameStartIndex);
                
                if (filenameStartIndex > 0 && filenameEndIndex > filenameStartIndex)
                {
                    string filename = historyResponse.Substring(filenameStartIndex, filenameEndIndex - filenameStartIndex);
                    string imageUrl = comfyUIServerURL + "/view?filename=" + filename;
                    
                    onComplete?.Invoke(imageUrl);
                }
                else
                {
                    onNotReady?.Invoke();
                }
            }
            else
            {
                onNotReady?.Invoke();
            }
        }
    }
    
    /// <summary>
    /// Helper class for task-based asynchronous operations
    /// </summary>
    class TaskCompletionSource<T>
    {
        bool isCompleted;
        T result;
        Exception exception;
        
        public TaskCompletionSource()
        {
            isCompleted = false;
            result = default(T);
            exception = null;
        }
        
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
        
        public Task<T> Task
        {
            get
            {
                return System.Threading.Tasks.Task.Run(() => {
                    while (!isCompleted)
                    {
                        System.Threading.Thread.Sleep(10);
                    }
                    
                    if (exception != null)
                    {
                        throw exception;
                    }
                    
                    return result;
                });
            }
        }
    }
}