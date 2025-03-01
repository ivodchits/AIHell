using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public sealed class UIManager : MonoBehaviour
{
    // Text UI components
    public TMP_Text outputText;
    public TMP_InputField inputField;
    public ScrollRect scrollRect;

    // Image display components
    [SerializeField] private RawImage generatedImageDisplay;
    [SerializeField] private CanvasGroup imageCanvasGroup;
    
    // Effect components
    private CanvasGroup fadeGroup;
    private Material postProcessMaterial;

    // Effect parameters
    private Queue<string> messageQueue = new Queue<string>();
    private readonly float messageDisplayTime = 4f;
    private readonly float messageFadeTime = 1f;
    private readonly float imageFadeDuration = 2f;
    private readonly float imageDisplayDuration = 5f;
    private bool isDisplayingMessage;
    private Dictionary<UIEffects.EffectType, Material> effectMaterials;
    private List<UIEffects.Effect> activeEffects;
    private Coroutine currentImageCoroutine;

    private void Awake()
    {
        InitializeUI();
    }

    private void InitializeUI()
    {
        try
        {
            // Initialize effect components
            activeEffects = new List<UIEffects.Effect>();
            effectMaterials = new Dictionary<UIEffects.EffectType, Material>();

            // Set up UI references
            SetupUIReferences();

            // Initialize effect materials
            InitializeEffectMaterials();

            // Set up input handling
            SetupInputField();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error initializing UI: {ex.Message}");
        }
    }

    private void SetupUIReferences()
    {
        // Find or create canvas if not assigned
        var canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            var canvasObj = new GameObject("UICanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // Set up UI elements if not assigned in inspector
        if (outputText == null)
        {
            var textObj = CreateUIElement("MessageText", canvas.transform);
            outputText = textObj.AddComponent<TextMeshProUGUI>();
            outputText.fontSize = 24;
            outputText.alignment = TextAlignmentOptions.Center;
        }

        if (inputField == null)
        {
            var inputObj = CreateUIElement("InputField", canvas.transform);
            inputField = inputObj.AddComponent<TMP_InputField>();
            SetupInputFieldComponents(inputObj);
        }

        if (scrollRect == null)
        {
            var scrollObj = CreateUIElement("ScrollView", canvas.transform);
            scrollRect = scrollObj.AddComponent<ScrollRect>();
            SetupScrollRect(scrollObj);
        }

        // Set up effect components
        if (generatedImageDisplay == null)
        {
            var imageObj = CreateUIElement("GeneratedImage", canvas.transform);
            generatedImageDisplay = imageObj.AddComponent<RawImage>();
            generatedImageDisplay.color = new Color(1, 1, 1, 0);
        }

        if (imageCanvasGroup == null)
        {
            imageCanvasGroup = generatedImageDisplay.gameObject.AddComponent<CanvasGroup>();
            imageCanvasGroup.alpha = 0;
        }

        // Fade group for transitions
        var fadeObj = CreateUIElement("FadeGroup", canvas.transform);
        fadeGroup = fadeObj.AddComponent<CanvasGroup>();
        fadeGroup.alpha = 0;
    }

    private GameObject CreateUIElement(string name, Transform parent)
    {
        var obj = new GameObject(name);
        var rt = obj.AddComponent<RectTransform>();
        rt.SetParent(parent);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        rt.localPosition = Vector3.zero;
        return obj;
    }

    private void SetupInputFieldComponents(GameObject inputObj)
    {
        // Create text area and placeholder
        var textArea = new GameObject("Text Area", typeof(RectTransform));
        textArea.transform.SetParent(inputObj.transform);
        var textComponent = textArea.AddComponent<TextMeshProUGUI>();
        inputField.textComponent = textComponent;

        var placeholder = new GameObject("Placeholder", typeof(RectTransform));
        placeholder.transform.SetParent(inputObj.transform);
        var placeholderText = placeholder.AddComponent<TextMeshProUGUI>();
        placeholderText.text = "Enter command...";
        placeholderText.color = new Color(1, 1, 1, 0.5f);
        inputField.placeholder = placeholderText;
    }

    private void SetupScrollRect(GameObject scrollObj)
    {
        // Create viewport and content
        var viewport = CreateUIElement("Viewport", scrollObj.transform);
        var content = CreateUIElement("Content", viewport.transform);
        
        scrollRect.viewport = viewport.GetComponent<RectTransform>();
        scrollRect.content = content.GetComponent<RectTransform>();
        viewport.AddComponent<Mask>();
        viewport.AddComponent<Image>().color = new Color(0, 0, 0, 0.5f);
    }

    private void InitializeEffectMaterials()
    {
        // Load and initialize effect shaders
        effectMaterials[UIEffects.EffectType.Paranoia] = new Material(Shader.Find("Hidden/ParanoiaEffect"));
        effectMaterials[UIEffects.EffectType.RealityDistortion] = new Material(Shader.Find("Hidden/RealityDistortion"));
        effectMaterials[UIEffects.EffectType.VoidManifestation] = new Material(Shader.Find("Hidden/VoidManifestation"));
        effectMaterials[UIEffects.EffectType.ShadowConvergence] = new Material(Shader.Find("Hidden/ShadowConvergence"));

        // Set up post-process material
        postProcessMaterial = new Material(Shader.Find("Hidden/PostProcess"));
    }

    private void SetupInputField()
    {
        if (inputField != null)
        {
            inputField.onSubmit.AddListener(OnInputSubmitted);
            inputField.onEndEdit.AddListener(OnInputSubmitted);
        }
    }

    public void DisplayMessage(string message)
    {
        messageQueue.Enqueue(message);
        if (!isDisplayingMessage)
        {
            StartCoroutine(DisplayNextMessage());
        }
    }

    private IEnumerator DisplayNextMessage()
    {
        isDisplayingMessage = true;

        while (messageQueue.Count > 0)
        {
            string message = messageQueue.Dequeue();
            
            // Format message if needed
            if (message.StartsWith(">"))
            {
                message = TextFormatter.StylePlayerInput(message.Substring(1).Trim());
            }

            // Update output text
            if (outputText != null)
            {
                outputText.text += $"\n{message}";
                StartCoroutine(ScrollToBottom());
            }

            // Message display animation
            float elapsed = 0f;
            while (elapsed < messageFadeTime)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Display duration
            yield return new WaitForSeconds(messageDisplayTime);
        }

        isDisplayingMessage = false;
    }

    private IEnumerator ScrollToBottom()
    {
        yield return new WaitForEndOfFrame();
        if (scrollRect != null)
        {
            scrollRect.normalizedPosition = new Vector2(0, 0);
        }
    }

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
        generatedImageDisplay.texture = texture;
        generatedImageDisplay.color = Color.white;
        
        yield return StartCoroutine(FadeImage(0f, 1f, imageFadeDuration));
        yield return new WaitForSeconds(imageDisplayDuration);
        yield return StartCoroutine(FadeImage(1f, 0f, imageFadeDuration));
        
        generatedImageDisplay.texture = null;
    }

    private IEnumerator FadeImage(float startAlpha, float targetAlpha, float duration)
    {
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = elapsed / duration;
            
            imageCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, normalizedTime);
            
            yield return null;
        }
        
        imageCanvasGroup.alpha = targetAlpha;
    }

    public void ApplyPsychologicalEffect(PsychologicalEffect effect)
    {
        var uiEffect = new UIEffects.Effect
        {
            type = DetermineEffectType(effect),
            intensity = effect.Intensity,
            duration = effect.Duration
        };

        activeEffects.Add(uiEffect);
        StartCoroutine(ProcessEffect(uiEffect));
    }

    private UIEffects.EffectType DetermineEffectType(PsychologicalEffect effect)
    {
        switch (effect.Type)
        {
            case PsychologicalEffect.EffectType.ParanoiaInduction:
                return UIEffects.EffectType.Paranoia;
            case PsychologicalEffect.EffectType.RoomDistortion:
                return UIEffects.EffectType.RealityDistortion;
            case PsychologicalEffect.EffectType.TimeDistortion:
                return UIEffects.EffectType.VoidManifestation;
            default:
                return UIEffects.EffectType.ShadowConvergence;
        }
    }

    private IEnumerator ProcessEffect(UIEffects.Effect effect)
    {
        float elapsed = 0f;
        Material material = effectMaterials[effect.type];

        while (elapsed < effect.duration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = elapsed / effect.duration;

            // Update effect parameters
            UpdateEffectParameters(material, effect.type, effect.intensity * (1 - normalizedTime));

            yield return null;
        }

        activeEffects.Remove(effect);
    }

    private void UpdateEffectParameters(Material material, UIEffects.EffectType type, float intensity)
    {
        switch (type)
        {
            case UIEffects.EffectType.Paranoia:
                material.SetFloat("_Intensity", intensity);
                material.SetFloat("_DistortionAmount", intensity * 0.2f);
                break;
            case UIEffects.EffectType.RealityDistortion:
                material.SetFloat("_DistortionStrength", intensity);
                material.SetFloat("_WaveSpeed", 1f + intensity);
                break;
            case UIEffects.EffectType.VoidManifestation:
                material.SetFloat("_VoidStrength", intensity);
                material.SetFloat("_PulseRate", 0.5f + intensity);
                break;
            case UIEffects.EffectType.ShadowConvergence:
                material.SetFloat("_ShadowIntensity", intensity);
                material.SetFloat("_ConvergenceRate", intensity * 2f);
                break;
        }
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (activeEffects.Count == 0)
        {
            Graphics.Blit(source, destination);
            return;
        }

        // Apply active effects
        var tempRT = RenderTexture.GetTemporary(source.width, source.height);
        Graphics.Blit(source, tempRT);

        foreach (var effect in activeEffects)
        {
            var material = effectMaterials[effect.type];
            Graphics.Blit(tempRT, destination, material);
            Graphics.Blit(destination, tempRT);
        }

        // Final post-process
        Graphics.Blit(tempRT, destination, postProcessMaterial);
        RenderTexture.ReleaseTemporary(tempRT);
    }

    // Helper methods for formatted messages
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

    public void ClearOutput()
    {
        if (outputText != null)
        {
            outputText.text = string.Empty;
        }
    }

    private void OnDestroy()
    {
        // Cleanup materials
        foreach (var material in effectMaterials.Values)
        {
            if (material != null)
                Destroy(material);
        }

        if (postProcessMaterial != null)
            Destroy(postProcessMaterial);
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
}