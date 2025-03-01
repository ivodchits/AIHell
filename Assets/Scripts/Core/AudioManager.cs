using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using AIHell.Core.Data;

public class GameAudioManager : MonoBehaviour
{
    [System.Serializable]
    public class SoundEffect
    {
        public string name;
        public AudioClip clip;
        [Range(0f, 1f)]
        public float volume = 1f;
        [Range(0.1f, 3f)]
        public float pitch = 1f;
        public bool loop = false;
        [HideInInspector]
        public AudioSource source;
    }

    private Dictionary<string, AudioSource> audioSources;
    private Dictionary<string, float> baseVolumes;
    private AudioSource ambienceSource;
    private AudioSource tensionSource;
    private AudioLowPassFilter lowPassFilter;
    private AudioHighPassFilter highPassFilter;
    private AudioReverbFilter reverbFilter;
    private float masterVolume = 1f;
    private float currentTensionBlend = 0f;

    [Header("Sound Effects")]
    public SoundEffect[] soundEffects;

    [Header("Random Ambience")]
    private AudioSource[] randomAmbienceSources;
    private float minRandomTime = 10f;
    private float maxRandomTime = 30f;

    public static GameAudioManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAudio();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeAudio()
    {
        try
        {
            audioSources = new Dictionary<string, AudioSource>();
            baseVolumes = new Dictionary<string, float>();

            // Setup ambience source
            ambienceSource = gameObject.AddComponent<AudioSource>();
            ambienceSource.loop = true;
            ambienceSource.spatialBlend = 0f;
            ambienceSource.priority = 0;
            ambienceSource.volume = 0.3f;

            // Setup tension source
            tensionSource = gameObject.AddComponent<AudioSource>();
            tensionSource.loop = true;
            tensionSource.spatialBlend = 0f;
            tensionSource.priority = 1;

            // Initialize random ambience sources
            randomAmbienceSources = new AudioSource[3];
            for (int i = 0; i < randomAmbienceSources.Length; i++)
            {
                randomAmbienceSources[i] = gameObject.AddComponent<AudioSource>();
            }

            // Initialize sound effects
            foreach (var sound in soundEffects)
            {
                sound.source = gameObject.AddComponent<AudioSource>();
                sound.source.clip = sound.clip;
                sound.source.volume = sound.volume;
                sound.source.pitch = sound.pitch;
                sound.source.loop = sound.loop;
            }

            // Add audio filters
            lowPassFilter = gameObject.AddComponent<AudioLowPassFilter>();
            highPassFilter = gameObject.AddComponent<AudioHighPassFilter>();
            reverbFilter = gameObject.AddComponent<AudioReverbFilter>();

            InitializeAudioFilters();
            StartCoroutine(PlayRandomAmbience());
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error initializing audio system: {ex.Message}");
        }
    }

    private void InitializeAudioFilters()
    {
        // Set default filter values
        lowPassFilter.enabled = true;
        lowPassFilter.cutoffFrequency = 22000f;

        highPassFilter.enabled = true;
        highPassFilter.cutoffFrequency = 10f;

        reverbFilter.enabled = true;
        reverbFilter.reverbPreset = AudioReverbPreset.PaddedCell;
        reverbFilter.dryLevel = 0f;
        reverbFilter.room = -1000f;
    }

    public void PlayLevelAmbience(int levelNumber)
    {
        try
        {
            string ambiencePath = $"Ambience/Level{levelNumber}";
            var ambienceClip = Resources.Load<AudioClip>(ambiencePath);
            
            if (ambienceClip != null)
            {
                StartCoroutine(CrossfadeAmbience(ambienceClip));
            }
            else
            {
                Debug.LogWarning($"Ambience clip not found: {ambiencePath}");
            }

            // Load tension layer
            string tensionPath = $"Ambience/Tension{levelNumber}";
            var tensionClip = Resources.Load<AudioClip>(tensionPath);
            
            if (tensionClip != null)
            {
                tensionSource.clip = tensionClip;
                tensionSource.volume = 0f;
                tensionSource.Play();
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error playing level ambience: {ex.Message}");
        }
    }

    private IEnumerator CrossfadeAmbience(AudioClip newAmbience)
    {
        float fadeTime = 2f;
        float startVolume = ambienceSource.volume;

        // Fade out current ambience
        while (ambienceSource.volume > 0)
        {
            ambienceSource.volume -= startVolume * Time.deltaTime / fadeTime;
            yield return null;
        }

        ambienceSource.clip = newAmbience;
        ambienceSource.Play();

        // Fade in new ambience
        while (ambienceSource.volume < startVolume)
        {
            ambienceSource.volume += startVolume * Time.deltaTime / fadeTime;
            yield return null;
        }
    }

    private IEnumerator PlayRandomAmbience()
    {
        while (true)
        {
            yield return new WaitForSeconds(UnityEngine.Random.Range(minRandomTime, maxRandomTime));
            
            AudioSource source = GetAvailableRandomSource();
            if (source != null)
            {
                PlayRandomSound(source);
            }
        }
    }

    private AudioSource GetAvailableRandomSource()
    {
        return System.Array.Find(randomAmbienceSources, source => !source.isPlaying);
    }

    private void PlayRandomSound(AudioSource source)
    {
        if (soundEffects.Length == 0) return;

        var sound = soundEffects[UnityEngine.Random.Range(0, soundEffects.Length)];
        source.clip = sound.clip;
        source.volume = sound.volume * UnityEngine.Random.Range(0.6f, 1f) * masterVolume;
        source.pitch = sound.pitch * UnityEngine.Random.Range(0.8f, 1.2f);
        
        // Randomize position for 3D audio effect
        float randomX = UnityEngine.Random.Range(-10f, 10f);
        float randomZ = UnityEngine.Random.Range(-10f, 10f);
        source.transform.position = new Vector3(randomX, 0, randomZ);
        
        source.Play();
    }

    public void PlaySound(string name)
    {
        try
        {
            // First check predefined sound effects
            SoundEffect sound = System.Array.Find(soundEffects, s => s.name == name);
            if (sound != null)
            {
                sound.source.Play();
                return;
            }

            // Fall back to dynamic loading if not found in predefined effects
            if (!audioSources.TryGetValue(name, out AudioSource source))
            {
                var clip = Resources.Load<AudioClip>($"Sounds/{name}");
                if (clip == null)
                {
                    Debug.LogWarning($"Sound clip not found: {name}");
                    return;
                }

                source = gameObject.AddComponent<AudioSource>();
                source.clip = clip;
                source.spatialBlend = 0f;
                source.volume = GetBaseVolume(name);
                
                audioSources[name] = source;
            }

            source.Play();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error playing sound: {ex.Message}");
        }
    }

    public void StopSound(string name)
    {
        // Check predefined sound effects
        SoundEffect sound = System.Array.Find(soundEffects, s => s.name == name);
        if (sound != null)
        {
            sound.source.Stop();
            return;
        }

        // Check dynamic sources
        if (audioSources.TryGetValue(name, out AudioSource source))
        {
            source.Stop();
        }
    }

    public void UpdateTensionAmbience(float tension)
    {
        try
        {
            // Smoothly blend in tension layer
            currentTensionBlend = Mathf.Lerp(currentTensionBlend, tension, Time.deltaTime);
            
            tensionSource.volume = currentTensionBlend * masterVolume;
            
            // Modify audio filters based on tension
            UpdateAudioFilters(tension);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error updating tension ambience: {ex.Message}");
        }
    }

    private void UpdateAudioFilters(float tension)
    {
        // Increase filter effects with tension
        lowPassFilter.cutoffFrequency = Mathf.Lerp(22000f, 5000f, tension);
        highPassFilter.cutoffFrequency = Mathf.Lerp(10f, 200f, tension);
        
        // Increase reverb with tension
        reverbFilter.dryLevel = Mathf.Lerp(0f, -600f, tension);
        reverbFilter.room = Mathf.Lerp(-1000f, 0f, tension);
    }

    private float GetBaseVolume(string soundName)
    {
        if (baseVolumes.TryGetValue(soundName, out float volume))
            return volume;
        
        // Default volume if not specified
        return 1.0f;
    }

    public void SetSourceVolume(string soundName, float volume)
    {
        if (audioSources.TryGetValue(soundName, out AudioSource source))
        {
            source.volume = Mathf.Clamp01(volume) * masterVolume;
        }

        // Store base volume
        baseVolumes[soundName] = volume;
    }

    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        
        // Update all source volumes
        foreach (var source in audioSources.Values)
        {
            if (source != null)
            {
                source.volume *= masterVolume;
            }
        }

        // Update ambience volumes
        if (ambienceSource != null)
            ambienceSource.volume *= masterVolume;
        if (tensionSource != null)
            tensionSource.volume *= masterVolume;
    }

    public void StopAllSounds()
    {
        foreach (var source in audioSources.Values)
        {
            if (source != null)
                source.Stop();
        }

        if (ambienceSource != null)
            ambienceSource.Stop();
        if (tensionSource != null)
            tensionSource.Stop();
    }

    public void PlayPsychologicalEffect(PsychologicalEffect effect)
    {
        if (!string.IsNullOrEmpty(effect.AssociatedSound))
        {
            PlaySound(effect.AssociatedSound);
            
            // Modify audio environment based on effect type
            switch (effect.Type)
            {
                case PsychologicalEffect.EffectType.ParanoiaInduction:
                    ApplyParanoiaAudioEffect(effect.Intensity);
                    break;
                case PsychologicalEffect.EffectType.RoomDistortion:
                    ApplyDistortionAudioEffect(effect.Intensity);
                    break;
                case PsychologicalEffect.EffectType.TimeDistortion:
                    ApplyTimeDistortionAudioEffect(effect.Intensity);
                    break;
            }
        }
    }

    private void ApplyParanoiaAudioEffect(float intensity)
    {
        lowPassFilter.cutoffFrequency = Mathf.Lerp(22000f, 3000f, intensity);
        reverbFilter.dryLevel = Mathf.Lerp(0f, -800f, intensity);
        reverbFilter.room = Mathf.Lerp(-1000f, 200f, intensity);
    }

    private void ApplyDistortionAudioEffect(float intensity)
    {
        highPassFilter.cutoffFrequency = Mathf.Lerp(10f, 300f, intensity);
        reverbFilter.dryLevel = Mathf.Lerp(0f, -400f, intensity);
        reverbFilter.room = Mathf.Lerp(-1000f, -200f, intensity);
    }

    private void ApplyTimeDistortionAudioEffect(float intensity)
    {
        // Pitch shift for time distortion
        ambienceSource.pitch = Mathf.Lerp(1f, 0.8f, intensity);
        tensionSource.pitch = Mathf.Lerp(1f, 1.2f, intensity);
        
        reverbFilter.dryLevel = Mathf.Lerp(0f, -200f, intensity);
        reverbFilter.room = Mathf.Lerp(-1000f, -600f, intensity);
    }

    public void ResetAudioEffects()
    {
        // Reset audio filters
        lowPassFilter.cutoffFrequency = 22000f;
        highPassFilter.cutoffFrequency = 10f;
        reverbFilter.dryLevel = 0f;
        reverbFilter.room = -1000f;

        // Reset source pitches
        ambienceSource.pitch = 1f;
        tensionSource.pitch = 1f;
    }
}