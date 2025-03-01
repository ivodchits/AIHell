using UnityEngine;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using AIHell.Core.Data;

public class MetaphysicalEventsSystem : MonoBehaviour
{
    private LLMManager llmManager;
    private Dictionary<string, RealityDistortion> activeDistortions;
    private List<RealityEvent> eventHistory;
    private float realityCoherence = 1.0f;
    private const int MAX_HISTORY = 10;
    private const float REALITY_THRESHOLD = 0.3f;

    [System.Serializable]
    public class RealityDistortion
    {
        public string id;
        public string type;
        public string description;
        public float intensity;
        public float duration;
        public float realityDamage;
        public AnimationCurve distortionCurve;
        public Dictionary<string, float> dimensionalEffects;
        public bool isPermanent;
        public float lastUpdateTime;
    }

    [System.Serializable]
    public class RealityEvent
    {
        public string type;
        public string description;
        public float coherenceImpact;
        public DateTime timestamp;
        public Dictionary<string, float> dimensionalStates;
    }

    private void Awake()
    {
        llmManager = GameManager.Instance.LLMManager;
        activeDistortions = new Dictionary<string, RealityDistortion>();
        eventHistory = new List<RealityEvent>();
        InitializeSystem();
    }

    private void InitializeSystem()
    {
        InitializeBaseDistortions();
    }

    private void InitializeBaseDistortions()
    {
        // Initialize with core reality distortions
        AddDistortion(new RealityDistortion {
            id = "time_loop",
            type = "temporal",
            description = "Time begins to fold and repeat",
            intensity = 0.3f,
            duration = 15f,
            realityDamage = 0.2f,
            distortionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1),
            dimensionalEffects = new Dictionary<string, float> {
                { "temporal", 0.4f },
                { "spatial", 0.2f }
            }
        });

        AddDistortion(new RealityDistortion {
            id = "spatial_warping",
            type = "spatial",
            description = "Space bends and twists unnaturally",
            intensity = 0.4f,
            duration = 20f,
            realityDamage = 0.3f,
            distortionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1),
            dimensionalEffects = new Dictionary<string, float> {
                { "spatial", 0.5f },
                { "perceptual", 0.3f }
            }
        });

        AddDistortion(new RealityDistortion {
            id = "cognitive_dissolution",
            type = "perceptual",
            description = "Reality fragments into conflicting perceptions",
            intensity = 0.5f,
            duration = 25f,
            realityDamage = 0.4f,
            distortionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1),
            dimensionalEffects = new Dictionary<string, float> {
                { "perceptual", 0.6f },
                { "temporal", 0.2f }
            }
        });
    }

    private void Update()
    {
        UpdateDistortions();
        CheckRealityState();
    }

    public void AddDistortion(RealityDistortion distortion)
    {
        if (!activeDistortions.ContainsKey(distortion.id))
        {
            activeDistortions[distortion.id] = distortion;
            UpdateRealityCoherence(distortion.realityDamage);
        }
    }

    private void UpdateDistortions()
    {
        foreach (var distortion in activeDistortions.Values.ToList())
        {
            float elapsed = Time.time - distortion.lastUpdateTime;
            if (!distortion.isPermanent && elapsed > distortion.duration)
            {
                RemoveDistortion(distortion.id);
                continue;
            }

            float normalizedTime = elapsed / distortion.duration;
            float currentIntensity = distortion.distortionCurve.Evaluate(normalizedTime) * distortion.intensity;

            ProcessDistortionEffects(distortion, currentIntensity);
        }
    }

    private void ProcessDistortionEffects(RealityDistortion distortion, float currentIntensity)
    {
        // Apply dimensional effects
        foreach (var effect in distortion.dimensionalEffects)
        {
            ApplyDimensionalEffect(effect.Key, effect.Value * currentIntensity);
        }

        // Trigger events based on intensity thresholds
        if (currentIntensity > 0.7f)
        {
            GameManager.Instance.EventProcessor.TriggerRealityEvent(distortion);
        }

        // Update psychological state
        var profile = GameManager.Instance.ProfileManager.CurrentProfile;
        float psychologicalImpact = currentIntensity * 0.2f;
        profile.ObsessionLevel = Mathf.Min(1f, profile.ObsessionLevel + psychologicalImpact);
    }

    private void ApplyDimensionalEffect(string dimension, float intensity)
    {
        switch (dimension)
        {
            case "temporal":
                ApplyTemporalDistortion(intensity);
                break;
            case "spatial":
                ApplySpatialDistortion(intensity);
                break;
            case "perceptual":
                ApplyPerceptualDistortion(intensity);
                break;
        }
    }

    private void ApplyTemporalDistortion(float intensity)
    {
        Time.timeScale = Mathf.Lerp(1f, 0.5f, intensity);
        // Apply time-based visual effects through MaterialManager
        GameManager.Instance.GetComponent<MaterialManager>()
            .ApplyPsychologicalEffect("TimeDistortion", intensity, 1f);
    }

    private void ApplySpatialDistortion(float intensity)
    {
        // Apply spatial warping effects
        GameManager.Instance.GetComponent<MaterialManager>()
            .ApplyPsychologicalEffect("RealityBreak", intensity, 1f);
    }

    private void ApplyPerceptualDistortion(float intensity)
    {
        // Apply perception-altering effects
        GameManager.Instance.GetComponent<MaterialManager>()
            .ApplyPsychologicalEffect("PsychologicalEffect", intensity, 1f);
    }

    private void RemoveDistortion(string id)
    {
        if (activeDistortions.TryGetValue(id, out RealityDistortion distortion))
        {
            activeDistortions.Remove(id);
            UpdateRealityCoherence(-distortion.realityDamage * 0.5f); // Partial recovery
        }
    }

    private void UpdateRealityCoherence(float change)
    {
        realityCoherence = Mathf.Clamp01(realityCoherence - change);
        if (realityCoherence < REALITY_THRESHOLD)
        {
            OnRealityBreakdown();
        }
    }

    private void CheckRealityState()
    {
        if (realityCoherence < REALITY_THRESHOLD)
        {
            // Natural reality recovery over time
            realityCoherence = Mathf.Min(1f, realityCoherence + Time.deltaTime * 0.01f);
        }
    }

    private async void OnRealityBreakdown()
    {
        try
        {
            string prompt = $"Generate reality breakdown event:\n" +
                          $"Coherence Level: {realityCoherence}\n" +
                          $"Active Distortions: {string.Join(", ", activeDistortions.Keys)}\n" +
                          $"Recent Events: {string.Join(", ", eventHistory.Take(3).Select(e => e.type))}";

            string response = await llmManager.GenerateResponse(prompt, "reality_breakdown");
            var newDistortion = ParseDistortionResponse(response);

            if (newDistortion != null)
            {
                AddDistortion(newDistortion);
                RecordRealityEvent(newDistortion);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error handling reality breakdown: {ex.Message}");
        }
    }

    private RealityDistortion ParseDistortionResponse(string response)
    {
        try
        {
            var lines = response.Split('\n');
            var distortion = new RealityDistortion
            {
                id = $"distortion_{Guid.NewGuid():N}",
                intensity = UnityEngine.Random.Range(0.6f, 0.9f),
                duration = UnityEngine.Random.Range(15f, 30f),
                realityDamage = UnityEngine.Random.Range(0.2f, 0.4f),
                distortionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1),
                dimensionalEffects = new Dictionary<string, float>(),
                lastUpdateTime = Time.time
            };

            foreach (var line in lines)
            {
                if (line.StartsWith("Type:"))
                    distortion.type = line.Replace("Type:", "").Trim();
                else if (line.StartsWith("Description:"))
                    distortion.description = line.Replace("Description:", "").Trim();
                else if (line.StartsWith("Effects:"))
                {
                    var effects = line.Replace("Effects:", "").Split(',');
                    foreach (var effect in effects)
                    {
                        var parts = effect.Trim().Split(':');
                        if (parts.Length == 2 && float.TryParse(parts[1], out float value))
                        {
                            distortion.dimensionalEffects[parts[0]] = value;
                        }
                    }
                }
            }

            return distortion;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error parsing distortion response: {ex.Message}");
            return null;
        }
    }

    private void RecordRealityEvent(RealityDistortion distortion)
    {
        var realityEvent = new RealityEvent
        {
            type = distortion.type,
            description = distortion.description,
            coherenceImpact = distortion.realityDamage,
            timestamp = DateTime.Now,
            dimensionalStates = new Dictionary<string, float>(distortion.dimensionalEffects)
        };

        eventHistory.Add(realityEvent);
        if (eventHistory.Count > MAX_HISTORY)
            eventHistory.RemoveAt(0);
    }

    public async Task<bool> GenerateMetaphysicalEvent(Room currentRoom)
    {
        try
        {
            string prompt = $"Generate metaphysical event for current room:\n" +
                          $"Room Type: {currentRoom.Archetype}\n" +
                          $"Description: {currentRoom.BaseDescription}\n" +
                          $"Reality Coherence: {realityCoherence}\n" +
                          $"Current Distortions: {string.Join(", ", activeDistortions.Keys)}";

            string response = await llmManager.GenerateResponse(prompt, "metaphysical_event");
            var newDistortion = ParseDistortionResponse(response);

            if (newDistortion != null)
            {
                AddDistortion(newDistortion);
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error generating metaphysical event: {ex.Message}");
            return false;
        }
    }

    public List<RealityEvent> GetRecentEvents()
    {
        return new List<RealityEvent>(eventHistory);
    }

    public float GetRealityCoherence()
    {
        return realityCoherence;
    }

    public void ResetReality()
    {
        activeDistortions.Clear();
        eventHistory.Clear();
        realityCoherence = 1.0f;
        Time.timeScale = 1.0f;
    }
}