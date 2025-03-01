using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AIHell.Core.Data;

public sealed class TensionManager : MonoBehaviour
{
    // Core tension parameters
    private float currentTension;
    private float targetTension;
    private float tensionVelocity;
    private const float MAX_TENSION = 1.0f;
    private const float MIN_TENSION = 0.1f;
    private const float TENSION_SMOOTHING = 2f;
    private float baseDecayRate = 0.02f;
    private float tensionMultiplier = 1.0f;

    // Event timing
    private float lastEventTime;
    private float nextEventThreshold;
    private float intensityMultiplier = 1f;

    // Tension tracking
    private Dictionary<string, float> sourceTensions = new Dictionary<string, float>();
    private List<TensionEvent> recentEvents = new List<TensionEvent>();
    private Queue<float> tensionHistory = new Queue<float>();
    private float[] tensionPeaks = new float[3]; // Track last 3 peaks
    private int currentPeakIndex;

    private const int MAX_RECENT_EVENTS = 5;
    private const int HISTORY_LENGTH = 10;
    private const float MAX_SOURCE_TENSION = 1.0f;

    [System.Serializable]
    public class TensionEvent
    {
        public string source;
        public float amount;
        public float timestamp;
        public float duration;
        public AnimationCurve tensionCurve;
    }

    private void Awake()
    {
        InitializeTensionSystem();
    }

    private void InitializeTensionSystem()
    {
        currentTension = MIN_TENSION;
        targetTension = MIN_TENSION;
        RecalculateNextEventThreshold();
    }

    private void Update()
    {
        UpdateTensionLevel();
        CheckForTensionEvents();
        UpdateTensionHistory();
    }

    private void UpdateTensionLevel()
    {
        // Smoothly interpolate current tension toward target
        currentTension = Mathf.SmoothDamp(
            currentTension, 
            targetTension, 
            ref tensionVelocity, 
            TENSION_SMOOTHING
        );

        // Process source tensions
        float sourceTensionTotal = 0f;
        foreach (var source in sourceTensions.Keys.ToList())
        {
            // Apply decay
            sourceTensions[source] = Mathf.Max(0f, sourceTensions[source] - baseDecayRate * Time.deltaTime);
            sourceTensionTotal += sourceTensions[source];
        }

        // Add recent event impacts
        float eventTension = CalculateEventTension();

        // Combine tensions
        float combinedTension = Mathf.Clamp01((sourceTensionTotal + eventTension) * tensionMultiplier);
        targetTension = Mathf.Lerp(targetTension, combinedTension, Time.deltaTime);
    }

    private float CalculateEventTension()
    {
        float eventTension = 0f;
        float currentTime = Time.time;

        foreach (var evt in recentEvents.ToList())
        {
            float elapsed = currentTime - evt.timestamp;
            if (elapsed > evt.duration)
            {
                recentEvents.Remove(evt);
                continue;
            }

            float normalizedTime = elapsed / evt.duration;
            float impact = evt.tensionCurve.Evaluate(normalizedTime) * evt.amount;
            eventTension += impact;
        }

        return eventTension;
    }

    private void CheckForTensionEvents()
    {
        if (Time.time - lastEventTime >= nextEventThreshold)
        {
            TriggerTensionEvent();
            RecalculateNextEventThreshold();
            lastEventTime = Time.time;
        }
    }

    private void TriggerTensionEvent()
    {
        if (currentTension < 0.3f)
        {
            GenerateSubtleEvent();
        }
        else if (currentTension < 0.7f)
        {
            GenerateModerateEvent();
        }
        else
        {
            GenerateIntenseEvent();
        }
    }

    private void GenerateSubtleEvent()
    {
        var profile = GameManager.Instance.ProfileManager.CurrentProfile;
        var eventGen = new EventGenerator();
        
        GameEvent evt = eventGen.GenerateEventForProfile(
            GameManager.Instance.LevelManager.CurrentRoom,
            profile
        );

        GameManager.Instance.LevelManager.CurrentRoom.AddEvent(evt);
        evt.Trigger(GameManager.Instance.LevelManager.CurrentRoom);
    }

    private void GenerateModerateEvent()
    {
        var profile = GameManager.Instance.ProfileManager.CurrentProfile;
        PsychologicalEffect effect;

        if (profile.FearLevel > profile.ObsessionLevel)
        {
            effect = new PsychologicalEffect(
                PsychologicalEffect.EffectType.ParanoiaInduction,
                currentTension,
                "The atmosphere grows heavy with tension...",
                "moderate_tension"
            );
        }
        else
        {
            effect = new PsychologicalEffect(
                PsychologicalEffect.EffectType.RoomDistortion,
                currentTension,
                "Reality begins to waver...",
                "reality_distortion"
            );
        }

        GameManager.Instance.UIManager.ApplyPsychologicalEffect(effect);
    }

    private void GenerateIntenseEvent()
    {
        var profile = GameManager.Instance.ProfileManager.CurrentProfile;
        StartCoroutine(IntenseEventSequence(profile));
    }

    private IEnumerator IntenseEventSequence(PlayerAnalysisProfile profile)
    {
        // Visual distortion
        var effect1 = new PsychologicalEffect(
            PsychologicalEffect.EffectType.RoomDistortion,
            1f,
            "The room twists impossibly...",
            "intense_distortion"
        );
        
        GameManager.Instance.UIManager.ApplyPsychologicalEffect(effect1);

        yield return new WaitForSeconds(2f);

        // Psychological manifestation
        var effect2 = new PsychologicalEffect(
            PsychologicalEffect.EffectType.Hallucination,
            1f,
            "Your fears take physical form...",
            "manifestation"
        );
        
        GameManager.Instance.UIManager.ApplyPsychologicalEffect(effect2);

        // Process psychological state
        await GameManager.Instance.GetComponent<ShadowManifestationSystem>()
            .ProcessPsychologicalState(profile, GameManager.Instance.LevelManager.CurrentRoom);

        // Update audio
        GameAudioManager.Instance.UpdateTensionAmbience(currentTension);

        // Reduce tension after intense sequence
        ModifyTension(-0.3f, "post_intense_event");
    }

    private void RecalculateNextEventThreshold()
    {
        // Base threshold on current tension
        float baseTime = Mathf.Lerp(45f, 15f, currentTension);
        
        // Add randomness
        nextEventThreshold = baseTime * UnityEngine.Random.Range(0.8f, 1.2f);
    }

    private void UpdateTensionHistory()
    {
        tensionHistory.Enqueue(currentTension);
        while (tensionHistory.Count > HISTORY_LENGTH)
        {
            tensionHistory.Dequeue();
        }

        // Check for new tension peak
        if (currentTension > GetAverageTension() + 0.2f)
        {
            RecordTensionPeak(currentTension);
        }
    }

    private void RecordTensionPeak(float peakValue)
    {
        tensionPeaks[currentPeakIndex] = peakValue;
        currentPeakIndex = (currentPeakIndex + 1) % tensionPeaks.Length;
    }

    private float GetAverageTension()
    {
        if (tensionHistory.Count == 0) return MIN_TENSION;
        
        float sum = 0f;
        foreach (float tension in tensionHistory)
        {
            sum += tension;
        }
        return sum / tensionHistory.Count;
    }

    public void ModifyTension(float amount, string source)
    {
        try
        {
            // Create tension event
            var tensionEvent = new TensionEvent
            {
                source = source,
                amount = amount * intensityMultiplier,
                timestamp = Time.time,
                duration = CalculateEventDuration(amount),
                tensionCurve = GenerateTensionCurve(amount)
            };

            recentEvents.Add(tensionEvent);
            if (recentEvents.Count > MAX_RECENT_EVENTS)
                recentEvents.RemoveAt(0);

            // Update source tensions
            if (!sourceTensions.ContainsKey(source))
                sourceTensions[source] = 0f;
            
            sourceTensions[source] = Mathf.Clamp01(sourceTensions[source] + amount);

            // Check for high tension
            if (currentTension > 0.8f)
            {
                OnHighTension(source);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error modifying tension: {ex.Message}");
        }
    }

    private float CalculateEventDuration(float amount)
    {
        // Larger tension changes last longer
        return Mathf.Lerp(5f, 15f, Mathf.Abs(amount));
    }

    private AnimationCurve GenerateTensionCurve(float amount)
    {
        AnimationCurve curve = new AnimationCurve();

        if (amount > 0)
        {
            // Quick rise, slow fall for positive tension
            curve.AddKey(new Keyframe(0, 0, 2, 2));
            curve.AddKey(new Keyframe(0.3f, 0.8f));
            curve.AddKey(new Keyframe(0.7f, 0.9f));
            curve.AddKey(new Keyframe(1, 0, -0.5f, -0.5f));
        }
        else
        {
            // Gradual tension relief
            curve.AddKey(new Keyframe(0, 0, 0.5f, 0.5f));
            curve.AddKey(new Keyframe(0.5f, -0.5f));
            curve.AddKey(new Keyframe(1, 0, 0.5f, 0.5f));
        }

        return curve;
    }

    private void UpdateTotalTension()
    {
        // Calculate base tension from sources
        float baseTension = sourceTensions.Values.Sum();
        baseTension = Mathf.Clamp01(baseTension);

        // Add recent event impacts
        float eventTension = 0f;
        float currentTime = Time.time;

        foreach (var evt in recentEvents.ToList())
        {
            float elapsed = currentTime - evt.timestamp;
            if (elapsed > evt.duration)
            {
                recentEvents.Remove(evt);
                continue;
            }

            float normalizedTime = elapsed / evt.duration;
            float impact = evt.tensionCurve.Evaluate(normalizedTime) * evt.amount;
            eventTension += impact;
        }

        // Combine tensions
        currentTension = Mathf.Clamp01(baseTension + eventTension);
        currentTension *= tensionMultiplier;
    }

    private void UpdateTensionMultiplier()
    {
        var profile = GameManager.Instance.ProfileManager.CurrentProfile;
        
        // Higher psychological states increase tension impact
        float psychologicalFactor = Mathf.Max(
            profile.FearLevel,
            profile.ObsessionLevel,
            profile.AggressionLevel
        );

        tensionMultiplier = 1f + (psychologicalFactor * 0.5f);
    }

    public void OnHighTension(string source)
    {
        try
        {
            var profile = GameManager.Instance.ProfileManager.CurrentProfile;

            // Generate high tension event
            GameManager.Instance.EventProcessor.TriggerImmediateEvent(
                "high_tension",
                currentTension,
                GenerateHighTensionDescription(source)
            );

            // Affect psychological state
            profile.FearLevel = Mathf.Min(1f, profile.FearLevel + 0.1f);

            // Update music system
            GameManager.Instance.GetComponent<MusicManager>()
                .UpdateMusicState(profile, GameManager.Instance.EmotionalResponseSystem);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error handling high tension: {ex.Message}");
        }
    }

    private string GenerateHighTensionDescription(string source)
    {
        switch (source.ToLower())
        {
            case "paranoia":
                return "The air grows thick with paranoid energy...";
            case "fear":
                return "Terror seeps into your bones...";
            case "psychological":
                return "Your mind strains against reality...";
            case "manifestation":
                return "The darkness itself seems to watch...";
            default:
                return "Tension reaches a breaking point...";
        }
    }

    public void AdjustPacing(PlayerAnalysisProfile profile)
    {
        // Calculate ideal pacing based on psychological state
        float idealTension = CalculateIdealTension(profile);
        
        // Adjust intensity multiplier based on difference from ideal
        float tensionDiff = Mathf.Abs(currentTension - idealTension);
        if (tensionDiff > 0.3f)
        {
            intensityMultiplier = currentTension < idealTension ? 1.5f : 0.7f;
        }
        else
        {
            intensityMultiplier = 1f;
        }

        // Recalculate event timing
        RecalculateNextEventThreshold();
    }

    private float CalculateIdealTension(PlayerAnalysisProfile profile)
    {
        // Base ideal tension on psychological state
        float fearComponent = profile.FearLevel * 0.4f;
        float obsessionComponent = profile.ObsessionLevel * 0.3f;
        float aggressionComponent = profile.AggressionLevel * 0.3f;

        return Mathf.Clamp(
            fearComponent + obsessionComponent + aggressionComponent,
            0.2f,
            0.8f
        );
    }

    // Public interface methods
    public float GetCurrentTension() => currentTension;
    public float GetSourceTension(string source) => sourceTensions.TryGetValue(source, out float tension) ? tension : 0f;
    public List<TensionEvent> GetRecentEvents() => new List<TensionEvent>(recentEvents);

    public void ResetTension()
    {
        currentTension = MIN_TENSION;
        targetTension = MIN_TENSION;
        tensionVelocity = 0f;
        sourceTensions.Clear();
        recentEvents.Clear();
        tensionHistory.Clear();
        tensionMultiplier = 1.0f;
        lastEventTime = Time.time;
        RecalculateNextEventThreshold();
    }

    public void SetTensionDecayRate(float rate)
    {
        baseDecayRate = Mathf.Clamp(rate, 0.01f, 0.1f);
    }
}