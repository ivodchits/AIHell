using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro;

public class SceneManager : MonoBehaviour
{
    [Header("Transition Effects")]
    public Image fadePanel;
    public float fadeSpeed = 1f;
    public float transitionDelay = 0.5f;

    [Header("Text Effects")]
    public float textFadeSpeed = 2f;
    public float glitchInterval = 0.1f;
    private string[] glitchCharacters = { "█", "▓", "▒", "░", "®", "¥", "€" };

    private bool isTransitioning;
    private TMP_Text outputText;

    private void Awake()
    {
        if (fadePanel != null)
        {
            fadePanel.gameObject.SetActive(true);
            Color startColor = fadePanel.color;
            startColor.a = 0;
            fadePanel.color = startColor;
        }
        
        outputText = GameManager.Instance.UIManager.outputText;
    }

    public IEnumerator TransitionToRoom(Room targetRoom, bool useGlitchEffect = false)
    {
        if (isTransitioning) yield break;
        isTransitioning = true;

        // Start fade out
        if (fadePanel != null)
        {
            yield return StartCoroutine(FadeScreen(true));
        }

        // Apply psychological effects to transition
        yield return StartCoroutine(ApplyPsychologicalTransitionEffects(targetRoom));

        // Change room
        GameManager.Instance.LevelManager.ChangeRoom(targetRoom.RoomID);

        // Optional glitch effect based on psychological state
        if (useGlitchEffect)
        {
            yield return StartCoroutine(ApplyGlitchEffect(targetRoom.DescriptionText));
        }
        else
        {
            // Normal text reveal
            yield return StartCoroutine(RevealTextGradually(targetRoom.DescriptionText));
        }

        // Fade back in
        if (fadePanel != null)
        {
            yield return StartCoroutine(FadeScreen(false));
        }

        isTransitioning = false;
    }

    private IEnumerator FadeScreen(bool fadeOut)
    {
        float targetAlpha = fadeOut ? 1f : 0f;
        Color currentColor = fadePanel.color;
        
        while (Mathf.Abs(currentColor.a - targetAlpha) > 0.01f)
        {
            currentColor.a = Mathf.MoveTowards(currentColor.a, targetAlpha, Time.deltaTime * fadeSpeed);
            fadePanel.color = currentColor;
            yield return null;
        }

        if (fadeOut)
        {
            yield return new WaitForSeconds(transitionDelay);
        }
    }

    private IEnumerator ApplyPsychologicalTransitionEffects(Room targetRoom)
    {
        var profile = GameManager.Instance.ProfileManager.CurrentProfile;
        
        // Apply different transition effects based on psychological state
        if (profile.FearLevel > 0.7f)
        {
            GameAudioManager.Instance.PlaySound("heartbeat");
            yield return StartCoroutine(PulseScreen());
        }
        else if (profile.ObsessionLevel > 0.8f)
        {
            GameAudioManager.Instance.PlaySound("whispers");
            yield return StartCoroutine(SpinScreen());
        }
        
        yield return null;
    }

    private IEnumerator PulseScreen()
    {
        if (fadePanel == null) yield break;

        for (int i = 0; i < 3; i++)
        {
            yield return StartCoroutine(FadeScreen(true));
            yield return StartCoroutine(FadeScreen(false));
            yield return new WaitForSeconds(0.2f);
        }
    }

    private IEnumerator SpinScreen()
    {
        if (fadePanel == null) yield break;

        RectTransform panelRect = fadePanel.GetComponent<RectTransform>();
        float startRotation = panelRect.rotation.eulerAngles.z;
        float targetRotation = startRotation + 360f;
        float duration = 1f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float currentRotation = Mathf.Lerp(startRotation, targetRotation, elapsed / duration);
            panelRect.rotation = Quaternion.Euler(0, 0, currentRotation);
            yield return null;
        }
    }

    private IEnumerator ApplyGlitchEffect(string finalText)
    {
        if (outputText == null) yield break;

        string originalText = finalText;
        int length = originalText.Length;
        
        for (int i = 0; i < 3; i++)
        {
            // Generate glitch text
            string glitchText = GenerateGlitchText(length);
            outputText.text = glitchText;
            yield return new WaitForSeconds(glitchInterval);
        }

        // Reveal final text
        outputText.text = originalText;
    }

    private string GenerateGlitchText(int length)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 0; i < length; i++)
        {
            if (Random.value > 0.7f)
            {
                sb.Append(glitchCharacters[Random.Range(0, glitchCharacters.Length)]);
            }
            else
            {
                sb.Append(" ");
            }
        }
        return sb.ToString();
    }

    private IEnumerator RevealTextGradually(string text)
    {
        if (outputText == null) yield break;

        outputText.maxVisibleCharacters = 0;
        outputText.text = text;

        while (outputText.maxVisibleCharacters < text.Length)
        {
            outputText.maxVisibleCharacters++;
            yield return new WaitForSeconds(1f / (textFadeSpeed * text.Length));
        }
    }
}