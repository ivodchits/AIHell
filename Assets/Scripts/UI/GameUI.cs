using UnityEngine;
using TMPro;
using UnityEngine.UI;

[RequireComponent(typeof(Canvas))]
public class GameUI : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text outputText;
    public TMP_InputField inputField;
    public ScrollRect scrollRect;
    public Button submitButton;

    private void Start()
    {
        ValidateComponents();
        SetupUI();
    }

    private void ValidateComponents()
    {
        if (outputText == null || inputField == null || scrollRect == null)
        {
            Debug.LogError("Missing required UI components!");
        }
    }

    private void SetupUI()
    {
        // Configure input field
        if (inputField != null)
        {
            inputField.onSubmit.AddListener(OnSubmitInput);
            inputField.ActivateInputField();
        }

        // Configure submit button
        if (submitButton != null)
        {
            submitButton.onClick.AddListener(() => OnSubmitInput(inputField.text));
        }

        // Set initial welcome message
        if (outputText != null)
        {
            outputText.text = "Welcome to AI Hell...\nType 'help' for available commands.";
        }
    }

    private void OnSubmitInput(string input)
    {
        if (string.IsNullOrEmpty(input))
            return;

        // Process input through GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.UIManager.DisplayMessage($"> {input}");
            GameManager.Instance.InputManager.ProcessInput(input);
        }

        // Clear and refocus input field
        inputField.text = string.Empty;
        inputField.ActivateInputField();
    }
}