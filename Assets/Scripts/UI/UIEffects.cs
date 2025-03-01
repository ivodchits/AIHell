using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI;

public class UIEffects : MonoBehaviour
{
    [Header("Text Effects")]
    public float glitchInterval = 0.05f;
    public float distortionAmount = 0.1f;
    public float shakeAmount = 2f;
    
    [Header("Visual Effects")]
    public Image vignette;
    public float vignetteIntensity = 0.5f;
    public float pulseSpeed = 1f;
    
    private TMP_Text outputText;
    private string baseText;
    private bool isGlitching;
    private Vector3 originalPosition;
    private Coroutine currentTextEffect;
    private readonly string[] glitchChars = { "█", "▓", "▒", "░", "▄", "▀", "█", "▌", "▐" };

    private void Start()
    {
        outputText = GameManager.Instance.UIManager.outputText;
        if (outputText != null)
        {
            originalPosition = outputText.transform.localPosition;
        }
        
        if (vignette != null)
        {
            SetVignetteIntensity(0f);
        }
    }

    public void ApplyPsychologicalEffect(PsychologicalEffect effect)
    {
        float intensity = effect.Intensity;
        
        switch (effect.Type)
        {
            case PsychologicalEffect.EffectType.ParanoiaInduction:
                StartVignettePulse(intensity);
                ApplyTextDistortion(intensity);
                break;
                
            case PsychologicalEffect.EffectType.TimeDistortion:
                ApplyTimeDistortionEffect(intensity);
                break;
                
            case PsychologicalEffect.EffectType.RoomDistortion:
                ApplyRoomDistortionEffect(intensity);
                break;
        }
    }

    public void ApplyTextGlitch(string text, float duration)
    {
        if (currentTextEffect != null)
        {
            StopCoroutine(currentTextEffect);
        }
        
        baseText = text;
        currentTextEffect = StartCoroutine(GlitchTextEffect(duration));
    }

    private IEnumerator GlitchTextEffect(float duration)
    {
        if (outputText == null) yield break;
        
        float elapsed = 0f;
        isGlitching = true;
        
        while (elapsed < duration)
        {
            if (Random.value < 0.3f)
            {
                outputText.text = DistortText(baseText);
            }
            else
            {
                outputText.text = baseText;
            }
            
            elapsed += glitchInterval;
            yield return new WaitForSeconds(glitchInterval);
        }
        
        outputText.text = baseText;
        isGlitching = false;
    }

    private string DistortText(string input)
    {
        char[] chars = input.ToCharArray();
        int distortions = Mathf.FloorToInt(chars.Length * distortionAmount);
        
        for (int i = 0; i < distortions; i++)
        {
            int index = Random.Range(0, chars.Length);
            chars[index] = glitchChars[Random.Range(0, glitchChars.Length)][0];
        }
        
        return new string(chars);
    }

    private void ApplyTextDistortion(float intensity)
    {
        if (currentTextEffect != null)
        {
            StopCoroutine(currentTextEffect);
        }
        currentTextEffect = StartCoroutine(TextDistortionEffect(intensity));
    }

    private IEnumerator TextDistortionEffect(float intensity)
    {
        if (outputText == null) yield break;
        
        Vector3 basePosition = originalPosition;
        float duration = 2f * intensity;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            if (outputText != null)
            {
                Vector3 randomOffset = Random.insideUnitSphere * shakeAmount * intensity;
                outputText.transform.localPosition = basePosition + randomOffset;
            }
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        if (outputText != null)
        {
            outputText.transform.localPosition = basePosition;
        }
    }

    private void ApplyTimeDistortionEffect(float intensity)
    {
        if (vignette != null)
        {
            StartCoroutine(TimeDistortionEffect(intensity));
        }
    }

    private IEnumerator TimeDistortionEffect(float intensity)
    {
        float duration = 3f * intensity;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            float vignetteValue = Mathf.PingPong(Time.time * pulseSpeed, vignetteIntensity) * intensity;
            SetVignetteIntensity(vignetteValue);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        SetVignetteIntensity(0f);
    }

    private void ApplyRoomDistortionEffect(float intensity)
    {
        StartCoroutine(RoomDistortionEffect(intensity));
    }

    private IEnumerator RoomDistortionEffect(float intensity)
    {
        if (outputText == null) yield break;
        
        string originalText = outputText.text;
        float duration = 2f * intensity;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            if (Random.value < intensity * 0.3f)
            {
                outputText.text = DistortText(originalText);
                yield return new WaitForSeconds(0.1f);
                outputText.text = originalText;
            }
            
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private void StartVignettePulse(float intensity)
    {
        if (vignette != null)
        {
            StartCoroutine(VignettePulseEffect(intensity));
        }
    }

    private IEnumerator VignettePulseEffect(float intensity)
    {
        float duration = 3f * intensity;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            float pulseValue = Mathf.Sin(Time.time * pulseSpeed * 2f) * 0.5f + 0.5f;
            SetVignetteIntensity(pulseValue * vignetteIntensity * intensity);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        SetVignetteIntensity(0f);
    }

    private void SetVignetteIntensity(float intensity)
    {
        if (vignette != null)
        {
            Color color = vignette.color;
            color.a = intensity;
            vignette.color = color;
        }
    }

    public void StopAllEffects()
    {
        StopAllCoroutines();
        if (outputText != null)
        {
            outputText.transform.localPosition = originalPosition;
        }
        SetVignetteIntensity(0f);
    }
}