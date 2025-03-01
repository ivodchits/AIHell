using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using AIHell.Core.Data;

public class EmotionalResponseSystem : MonoBehaviour
{
    private class EmotionalState
    {
        public float anxiety;
        public float dread;
        public float paranoia;
        public float despair;
        public Dictionary<string, float> triggers;
        public List<string> recentStimuli;

        public EmotionalState()
        {
            anxiety = 0f;
            dread = 0f;
            paranoia = 0f;
            despair = 0f;
            triggers = new Dictionary<string, float>();
            recentStimuli = new List<string>();
        }
    }

    private EmotionalState currentState;
    private Queue<string> emotionalMemory;
    private const int MEMORY_CAPACITY = 10;
    private float stateDecayRate = 0.05f;
    private Dictionary<string, System.Action> emotionalResponses;

    private void Awake()
    {
        InitializeSystem();
    }

    private void InitializeSystem()
    {
        currentState = new EmotionalState();
        emotionalMemory = new Queue<string>();
        InitializeEmotionalResponses();
    }

    private void InitializeEmotionalResponses()
    {
        emotionalResponses = new Dictionary<string, System.Action>
        {
            { "anxiety_spike", () => TriggerAnxietyResponse() },
            { "paranoia_trigger", () => TriggerParanoiaResponse() },
            { "dread_increase", () => TriggerDreadResponse() },
            { "despair_onset", () => TriggerDespairResponse() }
        };
    }

    public void ProcessEmotionalStimulus(string stimulus, float intensity, PlayerAnalysisProfile profile)
    {
        // Update emotional state based on stimulus
        UpdateEmotionalState(stimulus, intensity, profile);

        // Record in emotional memory
        RecordEmotionalMemory(stimulus);

        // Check for emotional thresholds and trigger responses
        CheckEmotionalThresholds();

        // Apply psychological effects based on current state
        ApplyPsychologicalEffects(profile);

        // Generate imagery for significant emotional events
        CheckForImageGeneration(stimulus, intensity);
    }

    private void UpdateEmotionalState(string stimulus, float intensity, PlayerAnalysisProfile profile)
    {
        // Base impact modified by player's psychological state
        float impact = intensity * (1f + profile.FearLevel);

        // Update relevant emotional components
        switch (stimulus.ToLower())
        {
            case "isolation":
            case "loneliness":
                currentState.anxiety += impact * 0.3f;
                currentState.despair += impact * 0.2f;
                break;

            case "being_watched":
            case "stalked":
                currentState.paranoia += impact * 0.4f;
                currentState.anxiety += impact * 0.2f;
                break;

            case "cosmic_horror":
            case "existential":
                currentState.dread += impact * 0.5f;
                currentState.despair += impact * 0.3f;
                break;
        }

        // Cap emotional values
        ClampEmotionalValues();

        // Add to triggers if significant
        if (intensity > 0.5f)
        {
            if (!currentState.triggers.ContainsKey(stimulus))
                currentState.triggers[stimulus] = 0f;
            currentState.triggers[stimulus] += intensity;
        }

        // Add to recent stimuli
        currentState.recentStimuli.Add(stimulus);
        if (currentState.recentStimuli.Count > 5)
            currentState.recentStimuli.RemoveAt(0);
    }

    private void ClampEmotionalValues()
    {
        currentState.anxiety = Mathf.Clamp01(currentState.anxiety);
        currentState.dread = Mathf.Clamp01(currentState.dread);
        currentState.paranoia = Mathf.Clamp01(currentState.paranoia);
        currentState.despair = Mathf.Clamp01(currentState.despair);
    }

    private void RecordEmotionalMemory(string stimulus)
    {
        emotionalMemory.Enqueue(stimulus);
        if (emotionalMemory.Count > MEMORY_CAPACITY)
            emotionalMemory.Dequeue();
    }

    private void CheckEmotionalThresholds()
    {
        if (currentState.anxiety > 0.8f)
            TriggerEmotionalResponse("anxiety_spike");
        
        if (currentState.paranoia > 0.7f)
            TriggerEmotionalResponse("paranoia_trigger");
        
        if (currentState.dread > 0.9f)
            TriggerEmotionalResponse("dread_increase");
        
        if (currentState.despair > 0.85f)
            TriggerEmotionalResponse("despair_onset");
    }

    private void TriggerEmotionalResponse(string responseType)
    {
        if (emotionalResponses.ContainsKey(responseType))
            emotionalResponses[responseType].Invoke();
    }

    private void TriggerAnxietyResponse()
    {
        var effect = new PsychologicalEffect(
            PsychologicalEffect.EffectType.ParanoiaInduction,
            currentState.anxiety,
            "Your anxiety manifests in the environment...",
            "anxiety_sound"
        );
        
        GameManager.Instance.UIManager.GetComponent<UIEffects>()
            .ApplyPsychologicalEffect(effect);
    }

    private void TriggerParanoiaResponse()
    {
        var effect = new PsychologicalEffect(
            PsychologicalEffect.EffectType.RoomDistortion,
            currentState.paranoia,
            "The room shifts with your paranoid thoughts...",
            "paranoia_sound"
        );
        
        GameManager.Instance.UIManager.GetComponent<UIEffects>()
            .ApplyPsychologicalEffect(effect);
    }

    private void TriggerDreadResponse()
    {
        var effect = new PsychologicalEffect(
            PsychologicalEffect.EffectType.TimeDistortion,
            currentState.dread,
            "Time itself seems to warp with your dread...",
            "dread_sound"
        );
        
        GameManager.Instance.UIManager.GetComponent<UIEffects>()
            .ApplyPsychologicalEffect(effect);
    }

    private void TriggerDespairResponse()
    {
        var effect = new PsychologicalEffect(
            PsychologicalEffect.EffectType.Hallucination,
            currentState.despair,
            "Your despair takes physical form...",
            "despair_sound"
        );
        
        GameManager.Instance.UIManager.GetComponent<UIEffects>()
            .ApplyPsychologicalEffect(effect);
    }

    private void ApplyPsychologicalEffects(PlayerAnalysisProfile profile)
    {
        // Calculate dominant emotion
        string dominantEmotion = GetDominantEmotion();
        
        // Apply effects based on dominant emotion and intensity
        float totalIntensity = GetTotalEmotionalIntensity();
        
        if (totalIntensity > 0.7f)
        {
            // Update game atmosphere
            GameManager.Instance.UIManager.DisplayMessage(
                GenerateAtmosphericResponse(dominantEmotion, totalIntensity)
            );
            
            // Trigger sound effects
            string soundEffect = GetEmotionalSoundEffect(dominantEmotion);
            if (!string.IsNullOrEmpty(soundEffect))
            {
                GameAudioManager.Instance.PlaySound(soundEffect);
            }
        }
    }

    private string GetDominantEmotion()
    {
        var emotions = new Dictionary<string, float>
        {
            { "anxiety", currentState.anxiety },
            { "dread", currentState.dread },
            { "paranoia", currentState.paranoia },
            { "despair", currentState.despair }
        };

        return emotions.OrderByDescending(e => e.Value).First().Key;
    }

    private float GetTotalEmotionalIntensity()
    {
        return (currentState.anxiety + currentState.dread + 
                currentState.paranoia + currentState.despair) / 4f;
    }

    private string GenerateAtmosphericResponse(string emotion, float intensity)
    {
        Dictionary<string, string[]> responses = new Dictionary<string, string[]>
        {
            { "anxiety", new[] {
                "Your heart races as shadows dance...",
                "The air grows thick with tension...",
                "Every sound makes you jump..."
            }},
            { "dread", new[] {
                "An overwhelming sense of doom looms...",
                "The future seems to collapse into darkness...",
                "Time itself feels heavy with foreboding..."
            }},
            { "paranoia", new[] {
                "Eyes watch from every corner...",
                "The walls themselves seem to spy on you...",
                "Nothing is as it seems..."
            }},
            { "despair", new[] {
                "Hope feels like a distant memory...",
                "The darkness within manifests without...",
                "Reality bends under the weight of your despair..."
            }}
        };

        if (responses.TryGetValue(emotion, out string[] possibleResponses))
        {
            return possibleResponses[Random.Range(0, possibleResponses.Length)];
        }

        return "The atmosphere grows heavier...";
    }

    private string GetEmotionalSoundEffect(string emotion)
    {
        return emotion switch
        {
            "anxiety" => "heartbeat",
            "dread" => "ambient_dread",
            "paranoia" => "whispers",
            "despair" => "void_sound",
            _ => string.Empty
        };
    }

    public void DecayEmotionalState()
    {
        currentState.anxiety = Mathf.Max(0f, currentState.anxiety - stateDecayRate * Time.deltaTime);
        currentState.dread = Mathf.Max(0f, currentState.dread - stateDecayRate * Time.deltaTime);
        currentState.paranoia = Mathf.Max(0f, currentState.paranoia - stateDecayRate * Time.deltaTime);
        currentState.despair = Mathf.Max(0f, currentState.despair - stateDecayRate * Time.deltaTime);
    }

    public Dictionary<string, float> GetEmotionalState()
    {
        return new Dictionary<string, float>
        {
            { "anxiety", currentState.anxiety },
            { "dread", currentState.dread },
            { "paranoia", currentState.paranoia },
            { "despair", currentState.despair }
        };
    }

    public List<string> GetRecentStimuli()
    {
        return new List<string>(currentState.recentStimuli);
    }

    public void ResetEmotionalState()
    {
        currentState = new EmotionalState();
        emotionalMemory.Clear();
    }

    private void CheckForImageGeneration(string stimulus, float intensity)
    {
        bool shouldGenerateImage = false;
        System.Action<Texture2D> callback = null;

        // Check emotional state combinations that warrant imagery
        if (currentState.anxiety > 0.8f && currentState.paranoia > 0.7f)
        {
            shouldGenerateImage = true;
            callback = OnParanoidImageGenerated;
        }
        else if (currentState.dread > 0.85f && currentState.despair > 0.7f)
        {
            shouldGenerateImage = true;
            callback = OnDreadImageGenerated;
        }
        else if (intensity > 0.9f)
        {
            shouldGenerateImage = true;
            callback = OnIntenseEmotionImageGenerated;
        }

        if (shouldGenerateImage)
        {
            GameManager.Instance.ImageGenerator.RequestContextualImage(
                GameManager.Instance.LevelManager.CurrentRoom,
                callback
            );
        }
    }

    private void OnParanoidImageGenerated(Texture2D texture)
    {
        var effect = new PsychologicalEffect(
            PsychologicalEffect.EffectType.ParanoiaInduction,
            currentState.paranoia,
            "Your paranoid visions take form...",
            "paranoid_vision"
        );
        
        GameManager.Instance.UIManager.GetComponent<UIEffects>()
            .ApplyPsychologicalEffect(effect);
    }

    private void OnDreadImageGenerated(Texture2D texture)
    {
        var effect = new PsychologicalEffect(
            PsychologicalEffect.EffectType.Hallucination,
            currentState.dread,
            "Your deepest dreads manifest...",
            "dread_vision"
        );
        
        GameManager.Instance.UIManager.GetComponent<UIEffects>()
            .ApplyPsychologicalEffect(effect);
    }

    private void OnIntenseEmotionImageGenerated(Texture2D texture)
    {
        string dominantEmotion = GetDominantEmotion();
        
        var effect = new PsychologicalEffect(
            PsychologicalEffect.EffectType.TimeDistortion,
            GetTotalEmotionalIntensity(),
            $"Your {dominantEmotion} warps reality...",
            "emotional_vision"
        );
        
        GameManager.Instance.UIManager.GetComponent<UIEffects>()
            .ApplyPsychologicalEffect(effect);
    }
}