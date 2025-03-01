using UnityEngine;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using AIHell.Core.Data;

public class PhobiaManager : MonoBehaviour
{
    private LLMManager llmManager;
    private Dictionary<string, Phobia> activePhobias;
    private Dictionary<string, float> phobiaTriggers;
    private float updateInterval = 5f;
    private float lastUpdateTime;

    [System.Serializable]
    public class Phobia
    {
        public string id;
        public string type;
        public string description;
        public float intensity;
        public string[] triggers;
        public string[] manifestations;
        public float developmentRate;
        public float lastTriggerTime;
        public AnimationCurve intensityCurve;
    }

    private void Awake()
    {
        llmManager = GameManager.Instance.LLMManager;
        activePhobias = new Dictionary<string, Phobia>();
        phobiaTriggers = new Dictionary<string, float>();
        InitializeBasePhobias();
    }

    private void InitializeBasePhobias()
    {
        // Initialize with core psychological fears
        AddPhobia(new Phobia {
            id = "isolation",
            type = "environmental",
            description = "Fear of being alone in empty spaces",
            intensity = 0.3f,
            triggers = new[] { "empty_room", "silence", "darkness" },
            manifestations = new[] { "echoing sounds", "distant footsteps", "moving shadows" },
            developmentRate = 0.1f,
            intensityCurve = AnimationCurve.EaseInOut(0, 0, 1, 1)
        });

        AddPhobia(new Phobia {
            id = "observation",
            type = "paranoid",
            description = "Fear of being watched by unseen entities",
            intensity = 0.4f,
            triggers = new[] { "mirrors", "windows", "dark_corners" },
            manifestations = new[] { "reflective surfaces", "eye-like shapes", "movement glimpses" },
            developmentRate = 0.15f,
            intensityCurve = AnimationCurve.EaseInOut(0, 0, 1, 1)
        });

        AddPhobia(new Phobia {
            id = "unreality",
            type = "psychological",
            description = "Fear of reality breaking down",
            intensity = 0.2f,
            triggers = new[] { "distortion", "impossible_geometry", "time_anomaly" },
            manifestations = new[] { "warped spaces", "time loops", "reality glitches" },
            developmentRate = 0.2f,
            intensityCurve = AnimationCurve.EaseInOut(0, 0, 1, 1)
        });
    }

    private void Update()
    {
        if (Time.time - lastUpdateTime >= updateInterval)
        {
            UpdatePhobiaStates();
            lastUpdateTime = Time.time;
        }
    }

    public void AddPhobia(Phobia phobia)
    {
        if (!activePhobias.ContainsKey(phobia.id))
        {
            activePhobias[phobia.id] = phobia;
            foreach (var trigger in phobia.triggers)
            {
                if (!phobiaTriggers.ContainsKey(trigger))
                    phobiaTriggers[trigger] = 0f;
            }
        }
    }

    public async Task<bool> ProcessTrigger(string trigger, float intensity)
    {
        bool wasTriggered = false;
        foreach (var phobia in activePhobias.Values)
        {
            if (Array.Exists(phobia.triggers, t => t == trigger))
            {
                wasTriggered = true;
                await TriggerPhobia(phobia, intensity);
            }
        }
        return wasTriggered;
    }

    private async Task TriggerPhobia(Phobia phobia, float triggerIntensity)
    {
        try
        {
            phobia.lastTriggerTime = Time.time;
            phobia.intensity = Mathf.Min(1f, phobia.intensity + (triggerIntensity * phobia.developmentRate));

            // Generate manifestation description
            string prompt = $"Generate a psychological horror manifestation for this phobia:\n" +
                          $"Type: {phobia.type}\n" +
                          $"Description: {phobia.description}\n" +
                          $"Current Intensity: {phobia.intensity}\n" +
                          $"Previous Manifestations: {string.Join(", ", phobia.manifestations)}";

            string manifestation = await llmManager.GenerateResponse(prompt, "phobia_manifestation");

            // Trigger event processor
            GameManager.Instance.EventProcessor.TriggerPhobiaEvent(phobia);

            // Update psychological state
            var profile = GameManager.Instance.ProfileManager.CurrentProfile;
            profile.TrackEmotionalTrigger(phobia.type, phobia.intensity);

            // Update manifestations
            var manifestationsList = new List<string>(phobia.manifestations);
            manifestationsList.Add(manifestation);
            if (manifestationsList.Count > 5) manifestationsList.RemoveAt(0);
            phobia.manifestations = manifestationsList.ToArray();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error triggering phobia: {ex.Message}");
        }
    }

    private void UpdatePhobiaStates()
    {
        var profile = GameManager.Instance.ProfileManager.CurrentProfile;
        float decayRate = 0.05f * Time.deltaTime;

        foreach (var phobia in activePhobias.Values)
        {
            // Natural decay over time
            float timeSinceLastTrigger = Time.time - phobia.lastTriggerTime;
            if (timeSinceLastTrigger > 30f) // 30 seconds cooldown
            {
                phobia.intensity = Mathf.Max(0.1f, phobia.intensity - decayRate);
            }

            // Modify based on psychological state
            if (profile.FearLevel > 0.7f || profile.ObsessionLevel > 0.7f)
            {
                phobia.developmentRate = Mathf.Min(0.3f, phobia.developmentRate + 0.01f);
            }
            else
            {
                phobia.developmentRate = Mathf.Max(0.1f, phobia.developmentRate - 0.01f);
            }
        }
    }

    public async Task<bool> GenerateNewPhobia(PlayerAnalysisProfile profile)
    {
        try
        {
            string prompt = $"Generate a new psychological fear based on current state:\n" +
                          $"Fear Level: {profile.FearLevel}\n" +
                          $"Obsession Level: {profile.ObsessionLevel}\n" +
                          $"Aggression Level: {profile.AggressionLevel}\n" +
                          $"Existing Phobias: {string.Join(", ", activePhobias.Keys)}";

            string response = await llmManager.GenerateResponse(prompt, "phobia_generation");
            var newPhobia = ParsePhobiaResponse(response);

            if (newPhobia != null)
            {
                AddPhobia(newPhobia);
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error generating new phobia: {ex.Message}");
            return false;
        }
    }

    private Phobia ParsePhobiaResponse(string response)
    {
        try
        {
            var lines = response.Split('\n');
            var phobia = new Phobia
            {
                id = $"phobia_{Guid.NewGuid():N}",
                intensity = 0.1f,
                developmentRate = 0.1f,
                intensityCurve = AnimationCurve.EaseInOut(0, 0, 1, 1)
            };

            foreach (var line in lines)
            {
                if (line.StartsWith("Type:"))
                    phobia.type = line.Replace("Type:", "").Trim();
                else if (line.StartsWith("Description:"))
                    phobia.description = line.Replace("Description:", "").Trim();
                else if (line.StartsWith("Triggers:"))
                    phobia.triggers = line.Replace("Triggers:", "")
                        .Split(',')
                        .Select(t => t.Trim())
                        .ToArray();
                else if (line.StartsWith("Manifestations:"))
                    phobia.manifestations = line.Replace("Manifestations:", "")
                        .Split(',')
                        .Select(m => m.Trim())
                        .ToArray();
            }

            if (string.IsNullOrEmpty(phobia.type) || string.IsNullOrEmpty(phobia.description))
                return null;

            return phobia;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error parsing phobia response: {ex.Message}");
            return null;
        }
    }

    public Phobia GetActivePhobia(string id)
    {
        return activePhobias.TryGetValue(id, out Phobia phobia) ? phobia : null;
    }

    public List<Phobia> GetActivePhobias()
    {
        return activePhobias.Values
            .Where(p => p.intensity > 0.3f)
            .OrderByDescending(p => p.intensity)
            .ToList();
    }

    public void ResetPhobias()
    {
        foreach (var phobia in activePhobias.Values)
        {
            phobia.intensity = 0.1f;
            phobia.lastTriggerTime = 0f;
            phobia.developmentRate = 0.1f;
        }
    }
}