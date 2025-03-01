using UnityEngine;
using TMPro;

public class GameInitializer : MonoBehaviour
{
    [Header("UI References")]
    public GameUI gameUI;
    public TMP_Text outputText;
    public TMP_InputField inputField;

    [Header("Audio")]
    public AudioClip[] levelAmbienceSounds;
    public AudioClip[] atmosphericSounds;

    private void Start()
    {
        InitializeGame();
    }

    private void InitializeGame()
    {
        // Create AudioManager if it doesn't exist
        if (FindObjectOfType<GameAudioManager>() == null)
        {
            GameObject audioManagerObj = new GameObject("AudioManager");
            GameAudioManager audioManager = audioManagerObj.AddComponent<GameAudioManager>();
            
            // Set up audio clips
            if (levelAmbienceSounds != null && levelAmbienceSounds.Length > 0)
            {
                // Add ambient sounds to AudioManager
                audioManager.levelAmbience = levelAmbienceSounds;
            }
        }

        // Set up UI references in UIManager
        if (GameManager.Instance != null && GameManager.Instance.UIManager != null)
        {
            GameManager.Instance.UIManager.outputText = outputText;
            GameManager.Instance.UIManager.inputField = inputField;
        }

        // Start the game
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartGame();
        }
        else
        {
            Debug.LogError("GameManager instance not found!");
        }
    }
}