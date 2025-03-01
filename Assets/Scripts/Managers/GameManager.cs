using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AIHell.Core.Data;
using AIHell.Core;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // Core Managers
    public GameStateManager StateManager { get; private set; }
    public LevelManager LevelManager { get; private set; }
    public UIManager UIManager { get; private set; }
    public PlayerInputManager InputManager { get; private set; }
    public LLMManager LLMManager { get; private set; }
    public PlayerProfileManager ProfileManager { get; private set; }
    
    // Core Systems
    public SaveSystem SaveSystem { get; private set; }
    public GameAudioManager AudioManager { get; private set; }
    public SceneManager SceneManager { get; private set; }
    public AchievementSystem AchievementSystem { get; private set; }
    public UIEffects UIEffects { get; private set; }

    // Horror Systems
    public NarrativeManager NarrativeManager { get; private set; }
    public EmotionalResponseSystem EmotionalResponseSystem { get; private set; }
    public ConsciousnessAnalyzer ConsciousnessAnalyzer { get; private set; }
    public PhobiaManager PhobiaManager { get; private set; }
    public PatternAnalyzer PatternAnalyzer { get; private set; }
    public RealityFilter RealityFilter { get; private set; }
    public MetaphysicalEventsSystem MetaphysicalEvents { get; private set; }
    public TensionManager TensionManager { get; private set; }
    public SoundManager SoundManager { get; private set; }
    public DifficultyManager DifficultyManager { get; private set; }
    public ImageGenerationSystem ImageGenerator { get; private set; }

    [Header("Game State")]
    private bool isGameInitialized;
    private float psychologicalUpdateInterval = 10f;
    private float lastPsychologicalUpdate;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        InitializeManagers();
    }

    private void InitializeManagers()
    {
        // Initialize core managers
        StateManager = gameObject.AddComponent<GameStateManager>();
        LevelManager = gameObject.AddComponent<LevelManager>();
        UIManager = gameObject.AddComponent<UIManager>();
        InputManager = gameObject.AddComponent<PlayerInputManager>();
        LLMManager = gameObject.AddComponent<LLMManager>();
        ProfileManager = gameObject.AddComponent<PlayerProfileManager>();

        // Initialize core systems
        SaveSystem = gameObject.AddComponent<SaveSystem>();
        AudioManager = gameObject.AddComponent<GameAudioManager>();
        SceneManager = gameObject.AddComponent<SceneManager>();
        AchievementSystem = gameObject.AddComponent<AchievementSystem>();
        UIEffects = gameObject.AddComponent<UIEffects>();

        // Initialize horror systems
        NarrativeManager = gameObject.AddComponent<NarrativeManager>();
        EmotionalResponseSystem = gameObject.AddComponent<EmotionalResponseSystem>();
        ConsciousnessAnalyzer = gameObject.AddComponent<ConsciousnessAnalyzer>();
        PhobiaManager = gameObject.AddComponent<PhobiaManager>();
        PatternAnalyzer = gameObject.AddComponent<PatternAnalyzer>();
        RealityFilter = gameObject.AddComponent<RealityFilter>();
        MetaphysicalEvents = gameObject.AddComponent<MetaphysicalEventsSystem>();
        TensionManager = gameObject.AddComponent<TensionManager>();
        SoundManager = gameObject.AddComponent<SoundManager>();
        DifficultyManager = gameObject.AddComponent<DifficultyManager>();
        ImageGenerator = gameObject.AddComponent<ImageGenerationSystem>();

        isGameInitialized = true;
    }

    private void Update()
    {
        if (!isGameInitialized) return;

        // Update psychological systems
        if (Time.time - lastPsychologicalUpdate >= psychologicalUpdateInterval)
        {
            UpdatePsychologicalSystems();
            lastPsychologicalUpdate = Time.time;
        }
    }

    private void UpdatePsychologicalSystems()
    {
        var profile = ProfileManager.CurrentProfile;
        var currentRoom = LevelManager.CurrentRoom;

        // Update narrative context
        NarrativeManager.UpdateNarrativeContext(profile, StateManager.CurrentLevel);

        // Analyze consciousness patterns
        ConsciousnessAnalyzer.AnalyzeConsciousness(
            profile,
            InputManager.GetRecentActions()
        );

        // Update phobias
        PhobiaManager.UpdatePhobias(profile, currentRoom);

        // Update metaphysical events
        MetaphysicalEvents.UpdateRealityState(profile, currentRoom);

        // Update tension
        TensionManager.AdjustPacing(profile);

        // Update difficulty
        DifficultyManager.UpdateDifficulty(profile, StateManager.CurrentLevel);

        // Check achievements
        AchievementSystem.CheckPsychologicalAchievements(profile);

        // Save game state periodically
        SaveSystem.SaveGame();
    }

    public string ProcessRoomDescription(string description)
    {
        var profile = ProfileManager.CurrentProfile;

        // Filter reality based on psychological state
        description = RealityFilter.FilterReality(description, profile);

        // Apply narrative enhancements
        description = NarrativeManager.GenerateRoomNarrative(
            LevelManager.CurrentRoom,
            profile
        );

        // Generate scene imagery if psychological state is significant
        if (ShouldGenerateImage(profile))
        {
            string emotionalTone = DetermineEmotionalTone(profile);
            ImageGenerator.RequestImage(
                description,
                GetPsychologicalStateDescription(profile),
                emotionalTone,
                OnImageGenerated
            );
        }

        // Apply consciousness patterns
        var patterns = ConsciousnessAnalyzer.GetActivePatterns();
        foreach (var pattern in patterns)
        {
            if (pattern.intensity > 0.7f && pattern.manifestations.Count > 0)
            {
                description += "\n" + pattern.manifestations[
                    Random.Range(0, pattern.manifestations.Count)
                ];
            }
        }

        return description;
    }

    private bool ShouldGenerateImage(PlayerAnalysisProfile profile)
    {
        // Generate images at significant psychological moments
        return profile.FearLevel > 0.6f ||
               profile.ObsessionLevel > 0.7f ||
               profile.AggressionLevel > 0.8f ||
               TensionManager.GetCurrentTension() > 0.7f;
    }

    private string GetPsychologicalStateDescription(PlayerAnalysisProfile profile)
    {
        List<string> states = new List<string>();
        
        if (profile.FearLevel > 0.6f)
            states.Add("deeply fearful");
        if (profile.ObsessionLevel > 0.7f)
            states.Add("obsessively focused");
        if (profile.AggressionLevel > 0.8f)
            states.Add("internally violent");
        
        string baseState = string.Join(" and ", states);
        
        var phobias = PhobiaManager.GetActivePhobias();
        if (phobias.Count > 0)
        {
            baseState += $", triggered by {phobias[0].name.ToLower()}";
        }
        
        return baseState;
    }

    private string DetermineEmotionalTone(PlayerAnalysisProfile profile)
    {
        var emotionalState = EmotionalResponseSystem.GetEmotionalState();
        string dominantEmotion = EmotionalResponseSystem.GetDominantEmotion();
        
        if (emotionalState["anxiety"] > 0.7f)
            return "tense and paranoid";
        if (emotionalState["dread"] > 0.7f)
            return "existentially horrifying";
        if (emotionalState["despair"] > 0.7f)
            return "deeply unsettling";
            
        return $"psychologically {dominantEmotion.ToLower()}";
    }

    private void OnImageGenerated(Texture2D texture)
    {
        if (texture != null)
        {
            UIManager.DisplayGeneratedImage(texture);
        }
    }

    public void ProcessPlayerAction(string action)
    {
        var profile = ProfileManager.CurrentProfile;

        // Analyze patterns
        PatternAnalyzer.AnalyzeAction(action, profile);

        // Process emotional response
        EmotionalResponseSystem.ProcessEmotionalStimulus(
            "player_action",
            0.5f,
            profile
        );

        // Update tension
        TensionManager.ModifyTension(0.1f, "player_action");

        // Check for metaphysical triggers
        if (action.Contains("examine") || action.Contains("look"))
        {
            MetaphysicalEvents.UpdateRealityState(profile, LevelManager.CurrentRoom);
        }
    }

    public void StartGame()
    {
        if (!isGameInitialized)
        {
            Debug.LogError("Game not properly initialized!");
            return;
        }

        ResetGameState();
        StartCoroutine(ShowIntroduction());
    }

    private void ResetGameState()
    {
        StateManager.ResetGameState();
        ProfileManager.ResetPlayerProfile();
        NarrativeManager.ResetNarrativeSystem();
        ConsciousnessAnalyzer.ResetAnalyzer();
        PhobiaManager.ResetPhobias();
        RealityFilter.ResetFilter();
        MetaphysicalEvents.ResetSystem();
        TensionManager.ResetTension();
        PatternAnalyzer.ResetAnalyzer();
    }

    private IEnumerator ShowIntroduction()
    {
        string[] introLines = {
            "Welcome to the depths of your psyche...",
            "Each choice you make will shape the horror that awaits.",
            "Your fears, your obsessions, your deepest anxieties...",
            "They will all manifest in the darkness ahead.",
            "Type 'help' for available commands, if you dare to proceed..."
        };

        foreach (string line in introLines)
        {
            UIManager.DisplayMessage(line);
            yield return new WaitForSeconds(2f);
        }

        // Start first level with base psychological state
        LevelManager.LoadLevel(1);
    }

    public void GameOver(string reason)
    {
        StartCoroutine(ShowGameOver(reason));
    }

    private IEnumerator ShowGameOver(string reason)
    {
        // Stop all effects
        UIEffects.StopAllEffects();
        SoundManager.StopAllSounds();

        // Display game over message with psychological analysis
        UIManager.DisplayMessage("\n<color=red>═══════════════════════════</color>");
        yield return new WaitForSeconds(1f);
        
        UIManager.DisplayMessage(reason);
        yield return new WaitForSeconds(2f);

        // Show psychological profile summary
        var profile = ProfileManager.CurrentProfile;
        var patterns = ConsciousnessAnalyzer.GetActivePatterns();
        var phobias = PhobiaManager.GetActivePhobias();

        string[] summary = {
            $"Fear Level: {profile.FearLevel:P0}",
            $"Obsession Level: {profile.ObsessionLevel:P0}",
            $"Aggression Level: {profile.AggressionLevel:P0}",
            $"Reality Coherence: {RealityFilter.GetGlobalDistortion():P0}",
            $"Dominant Patterns: {patterns.Count}",
            $"Manifested Phobias: {phobias.Count}"
        };

        foreach (string stat in summary)
        {
            UIManager.DisplayMessage(stat);
            yield return new WaitForSeconds(0.5f);
        }

        // Show achievements
        var unlockedAchievements = AchievementSystem.GetUnlockedAchievements();
        if (unlockedAchievements.Length > 0)
        {
            UIManager.DisplayMessage("\nAchievements Unlocked:");
            foreach (var achievement in unlockedAchievements)
            {
                UIManager.DisplayMessage($"- {achievement.title}");
                yield return new WaitForSeconds(0.5f);
            }
        }

        UIManager.DisplayMessage("\n<color=red>═══════════════════════════</color>");
        UIManager.DisplayMessage("\nType 'restart' to begin anew...");
    }
}