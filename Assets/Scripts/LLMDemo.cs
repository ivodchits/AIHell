using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Threading.Tasks;

public class LLMDemo : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text outputText;
    [SerializeField] private TMP_InputField playerInputField;
    [SerializeField] private Button generateRoomButton;
    [SerializeField] private Button generateEventButton;
    [SerializeField] private Button sendDialogueButton;
    [SerializeField] private TMP_Dropdown llmProviderDropdown;
    [SerializeField] private TMP_InputField apiKeyInput;
    [SerializeField] private Button saveApiKeyButton;
    
    [Header("Demo Settings")]
    [SerializeField] private int demoLevelNumber = 1;
    [SerializeField] private string demoLevelTheme = "Surface Anxiety";
    [SerializeField] private string demoLevelTone = "Mundane Dread";
    [SerializeField] private string[] demoRoomArchetypes = { 
        "Overcrowded Office", 
        "Empty Waiting Room", 
        "Silent Street Corner", 
        "Distorted Mirror" 
    };
    
    // References
    private LLMManager llmManager;
    private ContentGenerator contentGenerator;
    private string currentRoomDescription = "";
    
    // Mock player profile for demo
    private Dictionary<string, object> playerProfile = new Dictionary<string, object>
    {
        { "player_aggression_level", "low" },
        { "player_curiosity_level", "high" },
        { "player_fear_level", "moderate" },
        { "player_paranoia_level", "increasing" }
    };
    
    private void Start()
    {
        // Get the LLMManager instance
        llmManager = LLMManager.Instance;
        contentGenerator = ContentGenerator.Instance;
        
        if (llmManager == null)
        {
            Debug.LogError("LLMManager not found. Make sure it's in the scene.");
            outputText.text = "Error: LLMManager not found.";
            return;
        }
        
        // Setup UI
        SetupUI();
        
        outputText.text = "Welcome to AIHell LLM Demo!\n\nUse the buttons below to test the LLM functionality.";
    }
    
    private void SetupUI()
    {
        // Setup dropdown options
        llmProviderDropdown.ClearOptions();
        llmProviderDropdown.AddOptions(new List<string> { "Gemini", "Local LLM" });
        llmProviderDropdown.onValueChanged.AddListener(OnProviderDropdownChanged);
        
        // Setup buttons
        generateRoomButton.onClick.AddListener(OnGenerateRoomClicked);
        generateEventButton.onClick.AddListener(OnGenerateEventClicked);
        sendDialogueButton.onClick.AddListener(OnSendDialogueClicked);
        saveApiKeyButton.onClick.AddListener(OnSaveApiKeyClicked);
        
        // Setup input field
        playerInputField.onSubmit.AddListener(OnInputFieldSubmit);
        
        // Get any saved API key
        apiKeyInput.text = PlayerPrefs.GetString("GeminiApiKey", "");
    }
    
    private void OnProviderDropdownChanged(int index)
    {
        LLMProvider provider = (LLMProvider)index;
        llmManager.SetLLMProvider(provider);
        
        // Update UI based on selected provider
        bool isGemini = provider == LLMProvider.Gemini;
        apiKeyInput.gameObject.SetActive(isGemini);
        saveApiKeyButton.gameObject.SetActive(isGemini);
    }
    
    private void OnSaveApiKeyClicked()
    {
        llmManager.SetGeminiApiKey(apiKeyInput.text);
        StartCoroutine(ShowTemporaryMessage("API Key saved!"));
    }
    
    private void OnInputFieldSubmit(string input)
    {
        OnSendDialogueClicked();
    }
    
    private async void OnGenerateRoomClicked()
    {
        SetUIInteractable(false);
        outputText.text = "Generating room description...";
        
        try
        {
            // Create context for the room
            Dictionary<string, string> roomContext = new Dictionary<string, string>
            {
                { "level_number", demoLevelNumber.ToString() },
                { "level_theme", demoLevelTheme },
                { "level_tone", demoLevelTone },
                { "room_archetype", GetRandomRoomArchetype() },
                { "level_emotion", "intense anxiety" }
            };
            
            // Create game state dictionary
            Dictionary<string, object> gameState = new Dictionary<string, object>(playerProfile)
            {
                { "current_level", demoLevelNumber }
            };
            
            // Generate room description
            string description = await contentGenerator.GenerateRoomDescription(roomContext, gameState);
            currentRoomDescription = description;
            
            outputText.text = "Room Description:\n\n" + description;
        }
        catch (System.Exception e)
        {
            outputText.text = "Error generating room description: " + e.Message;
            Debug.LogError("Error generating room description: " + e);
        }
        finally
        {
            SetUIInteractable(true);
        }
    }
    
    private async void OnGenerateEventClicked()
    {
        if (string.IsNullOrEmpty(currentRoomDescription))
        {
            outputText.text = "Please generate a room description first.";
            return;
        }
        
        SetUIInteractable(false);
        outputText.text = currentRoomDescription + "\n\nGenerating event...";
        
        try
        {
            // Create context for the event
            Dictionary<string, string> eventContext = new Dictionary<string, string>
            {
                { "level_number", demoLevelNumber.ToString() },
                { "level_theme", demoLevelTheme },
                { "level_tone", demoLevelTone },
                { "room_description", currentRoomDescription }
            };
            
            // Create game state dictionary
            Dictionary<string, object> gameState = new Dictionary<string, object>(playerProfile)
            {
                { "current_level", demoLevelNumber }
            };
            
            // Generate event description
            string eventDescription = await contentGenerator.GenerateEventDescription(eventContext, gameState);
            
            outputText.text = currentRoomDescription + "\n\nEvent:\n" + eventDescription;
        }
        catch (System.Exception e)
        {
            outputText.text = "Error generating event: " + e.Message;
            Debug.LogError("Error generating event: " + e);
        }
        finally
        {
            SetUIInteractable(true);
        }
    }
    
    private async void OnSendDialogueClicked()
    {
        string playerInput = playerInputField.text.Trim();
        
        if (string.IsNullOrEmpty(playerInput))
        {
            return;
        }
        
        if (string.IsNullOrEmpty(currentRoomDescription))
        {
            outputText.text = "Please generate a room description first.";
            return;
        }
        
        SetUIInteractable(false);
        outputText.text += "\n\nYou: " + playerInput + "\n\nGenerating response...";
        
        try
        {
            // Create context for the character dialogue
            Dictionary<string, string> characterContext = new Dictionary<string, string>
            {
                { "level_number", demoLevelNumber.ToString() },
                { "level_theme", demoLevelTheme },
                { "level_tone", demoLevelTone },
                { "room_description", currentRoomDescription }
            };
            
            // Create game state dictionary
            Dictionary<string, object> gameState = new Dictionary<string, object>(playerProfile)
            {
                { "current_level", demoLevelNumber }
            };
            
            // Generate character dialogue
            string dialogue = await contentGenerator.GenerateCharacterDialogue(characterContext, playerInput, gameState);
            
            outputText.text = currentRoomDescription + "\n\nYou: " + playerInput + "\n\nCharacter: " + dialogue;
            
            // Clear input field
            playerInputField.text = "";
        }
        catch (System.Exception e)
        {
            outputText.text = "Error generating dialogue: " + e.Message;
            Debug.LogError("Error generating dialogue: " + e);
        }
        finally
        {
            SetUIInteractable(true);
            playerInputField.ActivateInputField();
        }
    }
    
    private string GetRandomRoomArchetype()
    {
        if (demoRoomArchetypes == null || demoRoomArchetypes.Length == 0)
        {
            return "Generic Room";
        }
        
        int index = Random.Range(0, demoRoomArchetypes.Length);
        return demoRoomArchetypes[index];
    }
    
    private void SetUIInteractable(bool interactable)
    {
        generateRoomButton.interactable = interactable;
        generateEventButton.interactable = interactable;
        sendDialogueButton.interactable = interactable;
        playerInputField.interactable = interactable;
        llmProviderDropdown.interactable = interactable;
        apiKeyInput.interactable = interactable;
        saveApiKeyButton.interactable = interactable;
    }
    
    private IEnumerator ShowTemporaryMessage(string message)
    {
        string originalText = outputText.text;
        outputText.text = message;
        
        yield return new WaitForSeconds(2f);
        
        outputText.text = originalText;
    }
}