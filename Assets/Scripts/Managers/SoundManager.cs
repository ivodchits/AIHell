using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using AIHell.Core.Data;

[System.Serializable]
public class SoundLayer
{
    public string name;
    public AudioClip[] clips;
    public float baseVolume = 1f;
    public bool loop = true;
    public float fadeTime = 1f;
    [Range(0f, 1f)]
    public float randomizationFactor = 0.1f;
    public bool spatialize = false;
}

public class SoundManager : MonoBehaviour
{
    [Header("Ambient Layers")]
    public SoundLayer[] ambientLayers;
    
    [Header("Psychological Effects")]
    public AudioClip[] fearSounds;
    public AudioClip[] obsessionSounds;
    public AudioClip[] paranoiaSounds;
    
    [Header("Event Sounds")]
    public AudioClip[] roomTransitions;
    public AudioClip[] textGlitches;
    public AudioClip[] achievements;

    private Dictionary<string, AudioSource> layerSources;
    private List<AudioSource> effectSources;
    private const int MAX_EFFECT_SOURCES = 5;
    private float masterVolume = 1f;

    private void Awake()
    {
        InitializeAudioSources();
    }

    private void InitializeAudioSources()
    {
        layerSources = new Dictionary<string, AudioSource>();
        effectSources = new List<AudioSource>();

        // Initialize ambient layer sources
        foreach (var layer in ambientLayers)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.loop = layer.loop;
            source.volume = 0;
            source.spatialize = layer.spatialize;
            layerSources.Add(layer.name, source);
        }

        // Initialize effect sources pool
        for (int i = 0; i < MAX_EFFECT_SOURCES; i++)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.loop = false;
            source.volume = 0;
            effectSources.Add(source);
        }
    }

    public void UpdatePsychologicalAudio(PlayerAnalysisProfile profile)
    {
        // Adjust ambient layers based on psychological state
        float fearIntensity = profile.FearLevel;
        float obsessionIntensity = profile.ObsessionLevel;
        
        // Update base ambient layer
        if (layerSources.TryGetValue("BaseAmbient", out AudioSource baseAmbient))
        {
            StartCoroutine(FadeLayer("BaseAmbient", 0.3f + (fearIntensity * 0.4f)));
        }

        // Update fear layer
        if (layerSources.TryGetValue("FearLayer", out AudioSource fearLayer))
        {
            StartCoroutine(FadeLayer("FearLayer", fearIntensity * 0.5f));
        }

        // Update obsession layer
        if (layerSources.TryGetValue("ObsessionLayer", out AudioSource obsessionLayer))
        {
            StartCoroutine(FadeLayer("ObsessionLayer", obsessionIntensity * 0.5f));
        }

        // Trigger random psychological sound effects
        if (fearIntensity > 0.7f && Random.value < 0.1f)
        {
            PlayPsychologicalEffect(PsychologicalEffect.EffectType.ParanoiaInduction);
        }
    }

    public void PlayLayeredAmbience(int levelNumber)
    {
        StopAllCoroutines();

        foreach (var layer in ambientLayers)
        {
            if (layerSources.TryGetValue(layer.name, out AudioSource source))
            {
                // Select appropriate clip for the level
                if (layer.clips != null && layer.clips.Length > levelNumber - 1)
                {
                    source.clip = layer.clips[levelNumber - 1];
                    source.Play();
                    StartCoroutine(FadeLayer(layer.name, layer.baseVolume));
                }
            }
        }
    }

    public void PlayPsychologicalEffect(PsychologicalEffect.EffectType effectType)
    {
        AudioClip[] clipArray = effectType switch
        {
            PsychologicalEffect.EffectType.ParanoiaInduction => paranoiaSounds,
            PsychologicalEffect.EffectType.RoomDistortion => fearSounds,
            _ => obsessionSounds
        };

        if (clipArray != null && clipArray.Length > 0)
        {
            AudioClip clip = clipArray[Random.Range(0, clipArray.Length)];
            PlayEffect(clip, GetRandomPosition(), Random.Range(0.8f, 1.2f));
        }
    }

    public void PlayRoomTransition()
    {
        if (roomTransitions != null && roomTransitions.Length > 0)
        {
            AudioClip clip = roomTransitions[Random.Range(0, roomTransitions.Length)];
            PlayEffect(clip, Vector3.zero, 1f);
        }
    }

    public void PlayTextGlitch()
    {
        if (textGlitches != null && textGlitches.Length > 0)
        {
            AudioClip clip = textGlitches[Random.Range(0, textGlitches.Length)];
            PlayEffect(clip, Vector3.zero, Random.Range(0.8f, 1.2f));
        }
    }

    public void PlayAchievement()
    {
        if (achievements != null && achievements.Length > 0)
        {
            AudioClip clip = achievements[Random.Range(0, achievements.Length)];
            PlayEffect(clip, Vector3.zero, 1f);
        }
    }

    private IEnumerator FadeLayer(string layerName, float targetVolume)
    {
        if (layerSources.TryGetValue(layerName, out AudioSource source))
        {
            SoundLayer layer = System.Array.Find(ambientLayers, l => l.name == layerName);
            if (layer == null) yield break;

            float startVolume = source.volume;
            float elapsed = 0f;

            while (elapsed < layer.fadeTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / layer.fadeTime;
                source.volume = Mathf.Lerp(startVolume, targetVolume * masterVolume, t);
                yield return null;
            }

            source.volume = targetVolume * masterVolume;
        }
    }

    private void PlayEffect(AudioClip clip, Vector3 position, float pitch)
    {
        AudioSource source = GetAvailableEffectSource();
        if (source != null)
        {
            source.clip = clip;
            source.pitch = pitch;
            source.spatialize = true;
            source.transform.position = position;
            source.volume = masterVolume;
            source.Play();
        }
    }

    private AudioSource GetAvailableEffectSource()
    {
        return effectSources.Find(source => !source.isPlaying);
    }

    private Vector3 GetRandomPosition()
    {
        return new Vector3(
            Random.Range(-10f, 10f),
            0f,
            Random.Range(-10f, 10f)
        );
    }

    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        foreach (var source in layerSources.Values)
        {
            source.volume *= masterVolume;
        }
    }

    public void StopAllSounds()
    {
        foreach (var source in layerSources.Values)
        {
            source.Stop();
        }

        foreach (var source in effectSources)
        {
            source.Stop();
        }
    }
}