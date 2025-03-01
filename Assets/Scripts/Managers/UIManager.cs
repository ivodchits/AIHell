using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Linq;

// UIManager: Handles all UI-related functionality
public sealed class UIManager : MonoBehaviour  // Made class sealed
{
    // Text UI components
    public TMP_Text outputText;
    public TMP_InputField inputField;
    public ScrollRect scrollRect;

    // Image display components
    [SerializeField] private RawImage _generatedImageDisplay;  // Renamed with underscore prefix
    [SerializeField] private CanvasGroup _imageCanvasGroup;    // Renamed with underscore prefix
    
    private readonly float _imageFadeDuration = 2f;           // Made readonly and renamed
    private readonly float _imageDisplayDuration = 5f;        // Made readonly and renamed
    private Coroutine _currentImageCoroutine;                // Renamed with underscore prefix

    private void Awake()
    {
        // Ensure we have the necessary UI components
        if (outputText == null || inputField == null)
        {
            Debug.LogError("UI components not assigned to UIManager!");
        }

        SetupInputField();
    }

    // Text UI Methods
    private void SetupInputField()
    {
        if (inputField != null)
        {
            inputField.onSubmit.AddListener(OnInputSubmitted);
            inputField.onEndEdit.AddListener(OnInputSubmitted);
        }
    }

    private void OnInputSubmitted(string input)
    {
        if (string.IsNullOrEmpty(input) || !Input.GetKeyDown(KeyCode.Return))
            return;

        DisplayMessage($"> {input}");
        GameManager.Instance.InputManager.ProcessInput(input);
        inputField.text = string.Empty;
        inputField.ActivateInputField();
    }

    public void DisplayRoomDescription(Room room)
    {
        string description = TextFormatter.StyleRoomDescription(room.DescriptionText);
        
        var exits = room.GetAvailableExits();
        if (exits.Count > 0)
        {
            description += "\n\n" + TextFormatter.StyleExits(exits.ToArray());
        }

        if (room.Characters.Count > 0)
        {
            description += "\n\nPresent:";
            foreach (var character in room.Characters)
            {
                description += $"\n{TextFormatter.StyleCharacterName(character.Name)}";
            }
        }

        DisplayMessage(description);
    }

    public void DisplayMessage(string message)
    {
        if (outputText != null)
        {
            if (message.StartsWith(">"))
            {
                message = TextFormatter.StylePlayerInput(message.Substring(1).Trim());
            }
            
            outputText.text += $"\n{message}";
            StartCoroutine(ScrollToBottom());
        }
    }

    public void DisplaySystemMessage(string message) => DisplayMessage(TextFormatter.StyleSystemMessage(message));
    public void DisplayErrorMessage(string error) => DisplayMessage(TextFormatter.StyleErrorMessage(error));
    public void DisplayEventMessage(string eventText) => DisplayMessage(TextFormatter.StyleEventText(eventText));

    public void DisplayCharacterDialogue(string characterName, string dialogue)
    {
        string formattedDialogue = $"{TextFormatter.StyleCharacterName(characterName)}: {TextFormatter.StyleCharacterDialogue(dialogue)}";
        DisplayMessage(formattedDialogue);
    }

    public void DisplayHelpText()
    {
        string helpText = @"Available Commands:
- go [direction] or simply type: north, south, east, west
- examine/look [target]
- help (shows this message)";
        
        DisplayMessage(TextFormatter.StyleHelp(helpText));
    }

    private IEnumerator ScrollToBottom()
    {
        yield return new WaitForEndOfFrame();
        if (scrollRect != null)
        {
            scrollRect.normalizedPosition = new Vector2(0, 0);
        }
    }

    public void ClearOutput()
    {
        if (outputText != null)
        {
            outputText.text = string.Empty;
        }
    }

    // Image Display Methods
    public void DisplayGeneratedImage(Texture2D texture)
    {
        if (_currentImageCoroutine != null)
        {
            StopCoroutine(_currentImageCoroutine);
        }
        _currentImageCoroutine = StartCoroutine(ShowImageSequence(texture));
    }

    private IEnumerator ShowImageSequence(Texture2D texture)
    {
        _generatedImageDisplay.texture = texture;
        _generatedImageDisplay.color = Color.white;
        
        yield return StartCoroutine(FadeImage(0f, 1f, _imageFadeDuration));
        yield return new WaitForSeconds(_imageDisplayDuration);
        yield return StartCoroutine(FadeImage(1f, 0f, _imageFadeDuration));
        
        _generatedImageDisplay.texture = null;
    }

    private IEnumerator FadeImage(float startAlpha, float targetAlpha, float duration)
    {
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = elapsed / duration;
            
            _imageCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, normalizedTime);
            
            yield return null;
        }
        
        _imageCanvasGroup.alpha = targetAlpha;
    }

    public void ApplyImageEffect(PsychologicalEffect effect)
    {
        if (_generatedImageDisplay.texture != null)
        {
            switch (effect.Type)
            {
                case PsychologicalEffect.EffectType.RoomDistortion:
                    StartCoroutine(ApplyDistortionEffect(effect.Intensity));
                    break;
                case PsychologicalEffect.EffectType.Hallucination:
                    StartCoroutine(ApplyHallucinationEffect(effect.Intensity));
                    break;
                case PsychologicalEffect.EffectType.ParanoiaInduction:
                    StartCoroutine(ApplyParanoiaEffect(effect.Intensity));
                    break;
            }
        }
    }

    private IEnumerator ApplyDistortionEffect(float intensity)
    {
        float duration = intensity * 2f;
        float elapsed = 0f;
        Vector3 originalScale = _generatedImageDisplay.transform.localScale;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float wave = Mathf.Sin(elapsed * 5f) * intensity * 0.1f;
            
            _generatedImageDisplay.transform.localScale = originalScale + new Vector3(wave, wave, 0f);
            
            yield return null;
        }
        
        _generatedImageDisplay.transform.localScale = originalScale;
    }

    private IEnumerator ApplyHallucinationEffect(float intensity)
    {
        float duration = intensity * 3f;
        float elapsed = 0f;
        Color originalColor = _generatedImageDisplay.color;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float colorShift = Mathf.Sin(elapsed * 3f) * intensity * 0.2f;
            
            _generatedImageDisplay.color = new Color(
                originalColor.r + colorShift,
                originalColor.g - colorShift,
                originalColor.b + colorShift,
                originalColor.a
            );
            
            yield return null;
        }
        
        _generatedImageDisplay.color = originalColor;
    }

    private IEnumerator ApplyParanoiaEffect(float intensity)
    {
        float duration = intensity * 2.5f;
        float elapsed = 0f;
        Vector3 originalPosition = _generatedImageDisplay.transform.localPosition;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            Vector3 shake = Random.insideUnitSphere * intensity * 5f;
            
            _generatedImageDisplay.transform.localPosition = originalPosition + shake;
            
            yield return new WaitForSeconds(0.05f);
        }
        
        _generatedImageDisplay.transform.localPosition = originalPosition;
    }
}