using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using AIHell.Core.Data;

public class MusicManager : MonoBehaviour
{
    [System.Serializable]
    public class MusicLayer
    {
        public string name;
        public AudioClip[] variations;
        public float baseVolume = 1f;
        public bool isLooping = true;
        [Range(0f, 1f)]
        public float psychologicalInfluence = 0.5f;
    }

    [System.Serializable]
    public class StingerSound
    {
        public string name;
        public AudioClip[] clips;
        public float minInterval = 10f;
        public float maxInterval = 30f;
        public float lastPlayTime;
    }

    private Dictionary<string, AudioSource> musicLayers;
    private Dictionary<string, AudioSource> stingerSources;
    private List<StingerSound> stingers;
    private float crossFadeTime = 2f;
    private bool isTransitioning;

    private const int LAYER_COUNT = 4;
    private readonly string[] layerNames = {
        "Ambient",
        "Tension",
        "Horror",
        "Psychological"
    };

    private void Awake()
    {
        InitializeAudioSources();
        StartCoroutine(StingerLoop());
    }

    private void InitializeAudioSources()
    {
        musicLayers = new Dictionary<string, AudioSource>();
        stingerSources = new Dictionary<string, AudioSource>();
        stingers = new List<StingerSound>();

        // Initialize music layers
        foreach (string layerName in layerNames)
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            source.volume = 0f;
            musicLayers.Add(layerName, source);
        }

        // Initialize stinger sources
        for (int i = 0; i < 3; i++)
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            stingerSources.Add($"Stinger_{i}", source);
        }
    }

    public void UpdateMusicState(PlayerAnalysisProfile profile, EmotionalResponseSystem emotional)
    {
        if (isTransitioning) return;

        var emotionalState = emotional.GetEmotionalState();
        
        // Update layer volumes based on psychological state
        float tensionLevel = CalculateTensionLevel(profile, emotionalState);
        float horrorLevel = CalculateHorrorLevel(profile, emotionalState);
        float psychLevel = CalculatePsychologicalLevel(profile, emotionalState);

        StartCoroutine(TransitionLayerVolumes(new Dictionary<string, float> {
            { "Ambient", 1f - (tensionLevel * 0.5f) },
            { "Tension", tensionLevel },
            { "Horror", horrorLevel },
            { "Psychological", psychLevel }
        }));
    }

    private float CalculateTensionLevel(PlayerAnalysisProfile profile, Dictionary<string, float> emotionalState)
    {
        float baseLevel = Mathf.Max(
            profile.FearLevel,
            emotionalState["anxiety"],
            emotionalState["paranoia"] * 0.8f
        );

        return Mathf.Lerp(0.2f, 1f, baseLevel);
    }

    private float CalculateHorrorLevel(PlayerAnalysisProfile profile, Dictionary<string, float> emotionalState)
    {
        float baseLevel = Mathf.Max(
            emotionalState["dread"],
            profile.FearLevel * 1.2f,
            emotionalState["despair"] * 0.9f
        );

        return Mathf.Lerp(0f, 1f, baseLevel);
    }

    private float CalculatePsychologicalLevel(PlayerAnalysisProfile profile, Dictionary<string, float> emotionalState)
    {
        float baseLevel = Mathf.Max(
            profile.ObsessionLevel,
            emotionalState["paranoia"],
            profile.AggressionLevel * 0.8f
        );

        return Mathf.Lerp(0f, 1f, baseLevel);
    }

    private IEnumerator TransitionLayerVolumes(Dictionary<string, float> targetVolumes)
    {
        isTransitioning = true;
        Dictionary<string, float> startVolumes = new Dictionary<string, float>();

        foreach (var layer in musicLayers)
        {
            startVolumes[layer.Key] = layer.Value.volume;
        }

        float elapsed = 0f;
        while (elapsed < crossFadeTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / crossFadeTime;

            foreach (var layer in musicLayers)
            {
                float startVol = startVolumes[layer.Key];
                float targetVol = targetVolumes[layer.Key];
                layer.Value.volume = Mathf.Lerp(startVol, targetVol, t);
            }

            yield return null;
        }

        isTransitioning = false;
    }

    public void PlayPsychologicalStinger(PsychologicalEffect effect)
    {
        var availableSource = GetAvailableStingerSource();
        if (availableSource == null) return;

        string stingerType = effect.Type switch
        {
            PsychologicalEffect.EffectType.ParanoiaInduction => "paranoia",
            PsychologicalEffect.EffectType.RoomDistortion => "distortion",
            PsychologicalEffect.EffectType.Hallucination => "hallucination",
            _ => "generic"
        };

        PlayStinger(stingerType, availableSource, effect.Intensity);
    }

    private AudioSource GetAvailableStingerSource()
    {
        return stingerSources.Values.FirstOrDefault(s => !s.isPlaying);
    }

    private void PlayStinger(string type, AudioSource source, float intensity)
    {
        // Find appropriate stinger sound
        var stinger = stingers.Find(s => s.name == type);
        if (stinger == null || stinger.clips.Length == 0) return;

        // Select random variation
        var clip = stinger.clips[Random.Range(0, stinger.clips.Length)];
        
        // Play with intensity-based parameters
        source.clip = clip;
        source.volume = Mathf.Lerp(0.5f, 1f, intensity);
        source.pitch = Random.Range(0.9f, 1.1f);
        source.Play();

        stinger.lastPlayTime = Time.time;
    }

    private IEnumerator StingerLoop()
    {
        while (true)
        {
            foreach (var stinger in stingers)
            {
                if (Time.time - stinger.lastPlayTime >= stinger.minInterval)
                {
                    // Check if we should play based on psychological state
                    if (ShouldPlayStinger(stinger))
                    {
                        var source = GetAvailableStingerSource();
                        if (source != null)
                        {
                            PlayStinger(stinger.name, source, 1f);
                        }
                    }
                }
            }
            yield return new WaitForSeconds(1f);
        }
    }

    private bool ShouldPlayStinger(StingerSound stinger)
    {
        var profile = GameManager.Instance.ProfileManager.CurrentProfile;
        float chance = 0.1f; // Base chance

        // Increase chance based on psychological state
        if (profile.FearLevel > 0.7f) chance += 0.2f;
        if (profile.ObsessionLevel > 0.6f) chance += 0.15f;

        return Random.value < chance;
    }

    public void StartLevel(int levelNumber)
    {
        StopAllCoroutines();
        StartCoroutine(TransitionToLevel(levelNumber));
    }

    private IEnumerator TransitionToLevel(int levelNumber)
    {
        // Fade out current music
        yield return StartCoroutine(TransitionLayerVolumes(new Dictionary<string, float> {
            { "Ambient", 0f },
            { "Tension", 0f },
            { "Horror", 0f },
            { "Psychological", 0f }
        }));

        // Set up new level music
        foreach (var layer in musicLayers)
        {
            // TODO: Load appropriate clips for level
            // layer.Value.clip = GetLevelClip(levelNumber, layer.Key);
            layer.Value.Play();
        }

        // Fade in new music
        yield return StartCoroutine(TransitionLayerVolumes(new Dictionary<string, float> {
            { "Ambient", 1f },
            { "Tension", 0.2f },
            { "Horror", 0f },
            { "Psychological", 0f }
        }));

        StartCoroutine(StingerLoop());
    }

    public void StopAllAudio()
    {
        StopAllCoroutines();
        foreach (var source in musicLayers.Values)
        {
            source.Stop();
        }
        foreach (var source in stingerSources.Values)
        {
            source.Stop();
        }
    }
}