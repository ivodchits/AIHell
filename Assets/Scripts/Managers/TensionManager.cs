using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using AIHell.Core.Data;

public class TensionManager : MonoBehaviour
{
    private float currentTension;
    private float targetTension;
    private float tensionVelocity;
    private const float MAX_TENSION = 1.0f;
    private const float MIN_TENSION = 0.1f;
    private const float TENSION_SMOOTHING = 2f;

    private float lastEventTime;
    private float nextEventThreshold;
    private float intensityMultiplier = 1f;

    private Queue<float> tensionHistory;
    private const int HISTORY_LENGTH = 10;
    private float[] tensionPeaks;
    private int currentPeakIndex;

    private void Awake()
    {
        InitializeTensionSystem();
    }

    private void InitializeTensionSystem()
    {
        currentTension = MIN_TENSION;
        targetTension = MIN_TENSION;
        tensionHistory = new Queue<float>();
        tensionPeaks = new float[3]; // Track last 3 peaks
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

        // Apply natural tension decay
        ApplyTensionDecay();
    }

    private void ApplyTensionDecay()
    {
        float decayRate = 0.05f * Time.deltaTime;
        targetTension = Mathf.Max(MIN_TENSION, targetTension - decayRate);
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

    public void ModifyTension(float amount, string reason)
    {
        float modifiedAmount = amount * intensityMultiplier;
        targetTension = Mathf.Clamp(targetTension + modifiedAmount, MIN_TENSION, MAX_TENSION);

        if (Mathf.Abs(modifiedAmount) > 0.2f)
        {
            OnSignificantTensionChange(modifiedAmount, reason);
        }
    }

    private void OnSignificantTensionChange(float change, string reason)
    {
        var profile = GameManager.Instance.ProfileManager.CurrentProfile;
        
        // Trigger psychological effects based on tension change
        if (change > 0.3f && currentTension > 0.7f)
        {
            var effect = new PsychologicalEffect(
                PsychologicalEffect.EffectType.ParanoiaInduction,
                Mathf.Abs(change),
                "The tension becomes palpable...",
                "tension_spike"
            );
            
            GameManager.Instance.UIManager.GetComponent<UIEffects>()
                .ApplyPsychologicalEffect(effect);
        }

        // Update music system
        GameManager.Instance.GetComponent<MusicManager>()
            .UpdateMusicState(profile, GameManager.Instance.EmotionalResponseSystem);
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
        // Create a psychological effect based on current state
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

        GameManager.Instance.UIManager.GetComponent<UIEffects>()
            .ApplyPsychologicalEffect(effect);
    }

    private void GenerateIntenseEvent()
    {
        var profile = GameManager.Instance.ProfileManager.CurrentProfile;
        
        // Trigger multiple effects for intense moments
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
        
        GameManager.Instance.UIManager.GetComponent<UIEffects>()
            .ApplyPsychologicalEffect(effect1);

        yield return new WaitForSeconds(2f);

        // Psychological manifestation
        var effect2 = new PsychologicalEffect(
            PsychologicalEffect.EffectType.Hallucination,
            1f,
            "Your fears take physical form...",
            "manifestation"
        );
        
        GameManager.Instance.UIManager.GetComponent<UIEffects>()
            .ApplyPsychologicalEffect(effect2);

        // Reduce tension after intense sequence
        ModifyTension(-0.3f, "post_intense_event");
    }

    private void RecalculateNextEventThreshold()
    {
        // Base threshold on current tension
        float baseTime = Mathf.Lerp(45f, 15f, currentTension);
        
        // Add randomness
        nextEventThreshold = baseTime * Random.Range(0.8f, 1.2f);
    }

    public float GetCurrentTension()
    {
        return currentTension;
    }

    public void ResetTension()
    {
        currentTension = MIN_TENSION;
        targetTension = MIN_TENSION;
        tensionVelocity = 0f;
        tensionHistory.Clear();
        lastEventTime = Time.time;
        RecalculateNextEventThreshold();
    }
}