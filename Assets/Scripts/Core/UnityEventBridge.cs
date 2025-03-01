using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using AIHell.Core.Data;

public class UnityEventBridge : MonoBehaviour
{
    private Queue<Action> mainThreadActions;
    private Dictionary<string, TaskCompletionSource<object>> pendingTasks;
    private Dictionary<string, System.Action<object>> eventCallbacks;
    private HashSet<string> activeCoroutines;

    private void Awake()
    {
        mainThreadActions = new Queue<Action>();
        pendingTasks = new Dictionary<string, TaskCompletionSource<object>>();
        eventCallbacks = new Dictionary<string, System.Action<object>>();
        activeCoroutines = new HashSet<string>();
    }

    private void Update()
    {
        // Process all queued actions on the main thread
        while (mainThreadActions.Count > 0)
        {
            try
            {
                mainThreadActions.Dequeue()?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogError($"Error executing main thread action: {e}");
            }
        }
    }

    public void QueueMainThreadAction(Action action)
    {
        lock (mainThreadActions)
        {
            mainThreadActions.Enqueue(action);
        }
    }

    public Task<T> ExecuteOnMainThread<T>(Func<T> function)
    {
        var tcs = new TaskCompletionSource<T>();

        QueueMainThreadAction(() =>
        {
            try
            {
                var result = function();
                tcs.SetResult(result);
            }
            catch (Exception e)
            {
                tcs.SetException(e);
            }
        });

        return tcs.Task;
    }

    public async Task WaitForEvent(string eventId, float timeout = 5f)
    {
        var tcs = new TaskCompletionSource<object>();
        string timeoutKey = $"{eventId}_timeout_{DateTime.Now.Ticks}";

        pendingTasks[eventId] = tcs;

        try
        {
            using (var cts = new System.Threading.CancellationTokenSource())
            {
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(timeout), cts.Token);
                var completionTask = tcs.Task;

                var completedTask = await Task.WhenAny(completionTask, timeoutTask);
                if (completedTask == timeoutTask)
                {
                    throw new TimeoutException($"Event {eventId} timed out after {timeout} seconds");
                }

                cts.Cancel(); // Cancel the timeout task if the event completed
            }
        }
        finally
        {
            pendingTasks.Remove(eventId);
        }
    }

    public void RegisterEventCallback(string eventId, System.Action<object> callback)
    {
        eventCallbacks[eventId] = callback;
    }

    public void UnregisterEventCallback(string eventId)
    {
        eventCallbacks.Remove(eventId);
    }

    public void TriggerEvent(string eventId, object data = null)
    {
        QueueMainThreadAction(() =>
        {
            if (pendingTasks.TryGetValue(eventId, out var tcs))
            {
                tcs.TrySetResult(data);
            }

            if (eventCallbacks.TryGetValue(eventId, out var callback))
            {
                try
                {
                    callback(data);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error in event callback for {eventId}: {e}");
                }
            }
        });
    }

    public async Task<bool> StartPsychologicalCoroutine(string coroutineId, Func<Task> action)
    {
        if (activeCoroutines.Contains(coroutineId))
        {
            return false;
        }

        activeCoroutines.Add(coroutineId);

        try
        {
            await action();
            return true;
        }
        finally
        {
            activeCoroutines.Remove(coroutineId);
        }
    }

    public bool IsCoroutineActive(string coroutineId)
    {
        return activeCoroutines.Contains(coroutineId);
    }

    public void CancelAllCoroutines()
    {
        activeCoroutines.Clear();
    }

    #region LLM Integration Helpers

    public async Task<bool> ProcessLLMResponse(string response, string eventType)
    {
        var success = true;
        
        try
        {
            // Queue the response processing on the main thread
            await ExecuteOnMainThread(() =>
            {
                // Notify relevant systems
                TriggerEvent($"llm_response_{eventType}", response);
                
                // Update psychological state if needed
                var profile = GameManager.Instance.ProfileManager.CurrentProfile;
                UpdatePsychologicalState(response, profile);
                
                return true;
            });
        }
        catch (Exception e)
        {
            Debug.LogError($"Error processing LLM response: {e}");
            success = false;
        }

        return success;
    }

    private void UpdatePsychologicalState(string response, PlayerAnalysisProfile profile)
    {
        // Extract emotional impact from response
        if (response.Contains("fear") || response.Contains("terror"))
        {
            profile.FearLevel = Mathf.Min(profile.FearLevel + 0.1f, 1f);
        }
        
        if (response.Contains("obsession") || response.Contains("compulsion"))
        {
            profile.ObsessionLevel = Mathf.Min(profile.ObsessionLevel + 0.1f, 1f);
        }
        
        if (response.Contains("anger") || response.Contains("rage"))
        {
            profile.AggressionLevel = Mathf.Min(profile.AggressionLevel + 0.1f, 1f);
        }
    }

    #endregion

    #region Async Event Handlers

    public async Task<T> WaitForEventData<T>(string eventId, float timeout = 5f)
    {
        var tcs = new TaskCompletionSource<T>();
        
        void Handler(object data)
        {
            if (data is T typedData)
            {
                tcs.TrySetResult(typedData);
            }
            else
            {
                tcs.TrySetException(new InvalidCastException($"Event data is not of type {typeof(T)}"));
            }
        }

        try
        {
            RegisterEventCallback(eventId, Handler);
            
            using (var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(timeout)))
            {
                return await tcs.Task;
            }
        }
        finally
        {
            UnregisterEventCallback(eventId);
        }
    }

    public async Task WaitForMultipleEvents(string[] eventIds, float timeout = 5f)
    {
        var tasks = new List<Task>();
        
        foreach (var eventId in eventIds)
        {
            tasks.Add(WaitForEvent(eventId, timeout));
        }

        await Task.WhenAll(tasks);
    }

    #endregion

    #region Error Handling

    public class EventTimeoutException : Exception
    {
        public string EventId { get; }
        
        public EventTimeoutException(string eventId, string message) : base(message)
        {
            EventId = eventId;
        }
    }

    public class EventProcessingException : Exception
    {
        public string EventId { get; }
        public object EventData { get; }
        
        public EventProcessingException(string eventId, object data, string message) : base(message)
        {
            EventId = eventId;
            EventData = data;
        }
    }

    #endregion
}