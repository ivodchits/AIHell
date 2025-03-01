using UnityEngine;
using System;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(MaterialManager))]
[RequireComponent(typeof(ImagePostProcessor))]
public class EventProcessor : MonoBehaviour
{
    private MaterialManager materialManager;
    private ImagePostProcessor imageProcessor;
    private Queue<PsychologicalEvent> eventQueue;
    private List<PsychologicalEvent> recentEvents;
    private const int MAX_RECENT_EVENTS = 5;
    private bool isProcessing;

    [System.Serializable]
    public class PsychologicalEvent
    {
        public string type;
        public float intensity;
        public string trigger;
        public string description;
        public bool requiresImageGeneration;
        public System.Action<Texture2D> onImageGenerated;
    }

    private void Awake()
    {
        materialManager = GetComponent<MaterialManager>();
        imageProcessor = GetComponent<ImagePostProcessor>();
        eventQueue = new Queue<PsychologicalEvent>();
        recentEvents = new List<PsychologicalEvent>();
    }

    public void QueuePsychologicalEvent(PsychologicalEvent psychEvent)
    {
        eventQueue.Enqueue(psychEvent);
        if (!isProcessing)
        {
            StartCoroutine(ProcessEventQueue());
        }
    }

    public List<PsychologicalEvent> GetRecentEvents()
    {
        return recentEvents.ToList();
    }

    private IEnumerator ProcessEventQueue()
    {
        isProcessing = true;

        while (eventQueue.Count > 0)
        {
            var currentEvent = eventQueue.Dequeue();
            yield return StartCoroutine(ProcessEvent(currentEvent));
            yield return new WaitForSeconds(0.5f); // Prevent event overlap
        }

        isProcessing = false;
    }

    private IEnumerator ProcessEvent(PsychologicalEvent psychEvent)
    {
        // Add to recent events
        recentEvents.Add(psychEvent);
        if (recentEvents.Count > MAX_RECENT_EVENTS)
        {
            recentEvents.RemoveAt(0);
        }

        // Log event for analysis
        Debug.Log($"Processing psychological event: {psychEvent.type} - Intensity: {psychEvent.intensity}");

        // Generate imagery if required
        if (psychEvent.requiresImageGeneration)
        {
            yield return StartCoroutine(GenerateEventImagery(psychEvent));
        }

        // Apply visual effects
        ApplyEventEffects(psychEvent);

        // Trigger psychological response
        TriggerPsychologicalResponse(psychEvent);

        yield return new WaitForSeconds(psychEvent.intensity * 2f); // Scale duration with intensity
    }

    private IEnumerator GenerateEventImagery(PsychologicalEvent psychEvent)
    {
        bool imageGenerated = false;
        
        // Request image generation
        GameManager.Instance.ImageGenerator.RequestContextualImage(
            GameManager.Instance.LevelManager.CurrentRoom,
            (texture) => {
                if (texture != null)
                {
                    // Apply psychological post-processing
                    imageProcessor.ApplyPsychologicalEffect(
                        texture,
                        new ImagePostProcessor.PostProcessEffect {
                            name = DetermineEffectType(psychEvent),
                            intensity = psychEvent.intensity,
                            duration = psychEvent.intensity * 3f
                        }
                    );

                    // Invoke callback
                    psychEvent.onImageGenerated?.Invoke(texture);
                }
                imageGenerated = true;
            }
        );

        // Wait for image generation
        while (!imageGenerated)
        {
            yield return null;
        }
    }

    private void ApplyEventEffects(PsychologicalEvent psychEvent)
    {
        string effectName = DetermineEffectType(psychEvent);
        float duration = CalculateEffectDuration(psychEvent);

        materialManager.ApplyPsychologicalEffect(
            effectName,
            psychEvent.intensity,
            duration
        );
    }

    private string DetermineEffectType(PsychologicalEvent psychEvent)
    {
        switch (psychEvent.type.ToLower())
        {
            case "paranoia":
            case "fear":
                return "ParanoiaEffect";
            case "reality_break":
            case "distortion":
                return "RealityBreak";
            case "psychological":
            case "mental":
                return "PsychologicalEffect";
            case "temporal":
            case "time":
                return "TimeDistortion";
            default:
                return "PsychologicalEffect";
        }
    }

    private float CalculateEffectDuration(PsychologicalEvent psychEvent)
    {
        float baseDuration = 3f;
        
        // Modify duration based on event type
        switch (psychEvent.type.ToLower())
        {
            case "paranoia":
                return baseDuration * 1.5f;
            case "reality_break":
                return baseDuration * 2f;
            case "temporal":
                return baseDuration * 1.2f;
            default:
                return baseDuration;
        }
    }

    private void TriggerPsychologicalResponse(PsychologicalEvent psychEvent)
    {
        var profile = GameManager.Instance.ProfileManager.CurrentProfile;
        var emotional = GameManager.Instance.EmotionalResponseSystem;

        // Update psychological state
        switch (psychEvent.type.ToLower())
        {
            case "paranoia":
            case "fear":
                profile.FearLevel += psychEvent.intensity * 0.2f;
                emotional.ProcessEmotionalStimulus("fear_response", psychEvent.intensity, profile);
                break;
            case "reality_break":
            case "distortion":
                profile.ObsessionLevel += psychEvent.intensity * 0.15f;
                emotional.ProcessEmotionalStimulus("reality_distortion", psychEvent.intensity, profile);
                break;
            case "psychological":
            case "mental":
                profile.AggressionLevel += psychEvent.intensity * 0.1f;
                emotional.ProcessEmotionalStimulus("psychological_pressure", psychEvent.intensity, profile);
                break;
        }

        // Update tension
        GameManager.Instance.TensionManager.ModifyTension(
            psychEvent.intensity * 0.3f,
            psychEvent.type
        );
    }

    public void TriggerImmediateEvent(string type, float intensity, string description)
    {
        QueuePsychologicalEvent(new PsychologicalEvent {
            type = type,
            intensity = intensity,
            description = description,
            requiresImageGeneration = true,
            trigger = "immediate"
        });
    }

    public void TriggerPhobiaEvent(PhobiaManager.Phobia phobia)
    {
        QueuePsychologicalEvent(new PsychologicalEvent {
            type = "paranoia",
            intensity = phobia.intensity,
            description = phobia.description,
            requiresImageGeneration = true,
            trigger = "phobia"
        });
    }

    public void TriggerRealityEvent(MetaphysicalEventsSystem.RealityDistortion distortion)
    {
        QueuePsychologicalEvent(new PsychologicalEvent {
            type = "reality_break",
            intensity = distortion.intensity,
            description = distortion.description,
            requiresImageGeneration = true,
            trigger = "reality_distortion"
        });
    }

    public void TriggerPatternEvent(PatternAnalyzer.BehaviorPattern pattern)
    {
        QueuePsychologicalEvent(new PsychologicalEvent {
            type = "psychological",
            intensity = pattern.psychologicalWeight,
            description = pattern.interpretation,
            requiresImageGeneration = true,
            trigger = "pattern_recognition"
        });
    }

    private async Task ProcessEventQueueAsync()
    {
        isProcessing = true;

        while (eventQueue.Count > 0)
        {
            try {
                var currentEvent = eventQueue.Dequeue();
                await ProcessEventAsync(currentEvent);
                await Task.Delay(500); // Prevent event overlap
            }
            catch (Exception ex) {
                Debug.LogError($"Error processing event: {ex.Message}");
            }
        }

        isProcessing = false;
    }

    private async Task ProcessEventAsync(PsychologicalEvent psychEvent)
    {
        try {
            // Add to recent events
            recentEvents.Add(psychEvent);
            if (recentEvents.Count > MAX_RECENT_EVENTS)
            {
                recentEvents.RemoveAt(0);
            }

            // Log event for analysis
            Debug.Log($"Processing psychological event: {psychEvent.type} - Intensity: {psychEvent.intensity}");

            // Generate imagery if required
            if (psychEvent.requiresImageGeneration)
            {
                await GenerateEventImageryAsync(psychEvent);
            }

            // Apply visual effects
            ApplyEventEffects(psychEvent);

            // Trigger psychological response
            TriggerPsychologicalResponse(psychEvent);

            await Task.Delay(Mathf.RoundToInt(psychEvent.intensity * 2000f));
        }
        catch (Exception ex) {
            Debug.LogError($"Error in ProcessEventAsync: {ex.Message}");
            throw;
        }
    }

    private async Task GenerateEventImageryAsync(PsychologicalEvent psychEvent)
    {
        var tcs = new TaskCompletionSource<bool>();
        
        try {
            // Request image generation
            GameManager.Instance.ImageGenerator.RequestContextualImage(
                GameManager.Instance.LevelManager.CurrentRoom,
                (texture) => {
                    if (texture != null)
                    {
                        try {
                            // Apply psychological post-processing
                            imageProcessor.ApplyPsychologicalEffect(
                                texture,
                                new ImagePostProcessor.PostProcessEffect {
                                    name = DetermineEffectType(psychEvent),
                                    intensity = psychEvent.intensity,
                                    duration = psychEvent.intensity * 3f
                                }
                            );

                            // Invoke callback
                            psychEvent.onImageGenerated?.Invoke(texture);
                            tcs.SetResult(true);
                        }
                        catch (Exception ex) {
                            tcs.SetException(ex);
                        }
                    }
                    else {
                        tcs.SetResult(false);
                    }
                }
            );

            await tcs.Task;
        }
        catch (Exception ex) {
            Debug.LogError($"Error in GenerateEventImageryAsync: {ex.Message}");
            throw;
        }
    }
}