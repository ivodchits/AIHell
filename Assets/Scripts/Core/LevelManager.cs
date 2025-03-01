using UnityEngine;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using AIHell.Core.Data;

public sealed class LevelManager : MonoBehaviour
{
    [Header("Level Generation")]
    public int baseRoomCount = 5;
    public float roomIncreasePerLevel = 2f;
    public float eventProbability = 0.3f;

    private LLMManager llmManager;
    private GameStateManager stateManager;
    private Level currentLevel;
    private Room currentRoom;
    private List<string> visitedRooms;
    private Dictionary<int, LevelTheme> levelThemes;
    private SceneManager sceneManager;

    [System.Serializable]
    public class LevelTheme
    {
        public string theme;
        public string tone;
        public List<string> keywords;
        public string[] roomArchetypes;
        public float baseIntensity;
        public AnimationCurve intensityProgression;
        public Dictionary<string, float> thematicWeights;

        public LevelTheme(string theme, string tone, List<string> keywords)
        {
            this.theme = theme;
            this.tone = tone;
            this.keywords = keywords;
            this.thematicWeights = new Dictionary<string, float>();
        }
    }

    private void Awake()
    {
        llmManager = GameManager.Instance.LLMManager;
        stateManager = GameManager.Instance.StateManager;
        visitedRooms = new List<string>();
        sceneManager = GetComponent<SceneManager>() ?? gameObject.AddComponent<SceneManager>();
        InitializeLevelThemes();
    }

    private void InitializeLevelThemes()
    {
        levelThemes = new Dictionary<int, LevelTheme>
        {
            { 1, new LevelTheme("Surface Anxiety", "Mundane Dread", 
                new List<string> { "isolation", "emptiness", "silence" }) {
                roomArchetypes = new[] { "empty_room", "corridor", "study", "bedroom" },
                baseIntensity = 0.3f,
                intensityProgression = AnimationCurve.EaseInOut(0, 0.3f, 1, 0.7f),
                thematicWeights = new Dictionary<string, float> {
                    { "psychological", 0.6f },
                    { "mundane", 0.3f },
                    { "supernatural", 0.1f }
                }
            }},
            { 2, new LevelTheme("Paranoid Distortion", "Growing Dread",
                new List<string> { "paranoia", "watching", "distortion" }) {
                roomArchetypes = new[] { "observation_room", "mirror_hall", "surveillance_area" },
                baseIntensity = 0.5f,
                intensityProgression = AnimationCurve.EaseInOut(0, 0.5f, 1, 0.9f),
                thematicWeights = new Dictionary<string, float> {
                    { "psychological", 0.4f },
                    { "supernatural", 0.4f },
                    { "existential", 0.2f }
                }
            }},
            { 3, new LevelTheme("Reality Breakdown", "Psychological Horror",
                new List<string> { "unreality", "madness", "transformation" }) {
                roomArchetypes = new[] { "twisted_room", "void_chamber", "memory_space" },
                baseIntensity = 0.7f,
                intensityProgression = AnimationCurve.EaseInOut(0, 0.7f, 1, 1f),
                thematicWeights = new Dictionary<string, float> {
                    { "psychological", 0.3f },
                    { "supernatural", 0.3f },
                    { "existential", 0.3f },
                    { "cosmic", 0.1f }
                }
            }},
            { 4, new LevelTheme("Existential Dread", "Meaninglessness",
                new List<string> { "void", "mirrors", "infinite", "decay" }) {
                roomArchetypes = new[] { "endless_corridor", "reflection_chamber", "void_space" },
                baseIntensity = 0.8f,
                intensityProgression = AnimationCurve.EaseInOut(0, 0.8f, 1, 1f),
                thematicWeights = new Dictionary<string, float> {
                    { "psychological", 0.2f },
                    { "existential", 0.5f },
                    { "cosmic", 0.3f }
                }
            }},
            { 5, new LevelTheme("Ultimate Despair", "Annihilation",
                new List<string> { "abyss", "darkness", "eternal", "unknowable" }) {
                roomArchetypes = new[] { "cosmic_void", "reality_tear", "nightmare_space" },
                baseIntensity = 0.9f,
                intensityProgression = AnimationCurve.EaseInOut(0, 0.9f, 1, 1f),
                thematicWeights = new Dictionary<string, float> {
                    { "psychological", 0.1f },
                    { "existential", 0.3f },
                    { "cosmic", 0.6f }
                }
            }}
        };
    }

    public async Task<bool> GenerateLevel(int levelNumber)
    {
        try
        {
            if (!levelThemes.ContainsKey(levelNumber))
            {
                Debug.LogError($"Invalid level number: {levelNumber}");
                return false;
            }

            var theme = levelThemes[levelNumber];
            int roomCount = CalculateRoomCount();
            
            // Generate initial level structure
            currentLevel = new Level(levelNumber, theme.theme, theme.tone, theme.keywords);
            currentLevel.ThematicWeights = theme.thematicWeights;

            // Play level-specific ambience
            GameAudioManager.Instance.PlayLevelAmbience(levelNumber);

            // Generate rooms with psychological consideration
            await GenerateRoomsWithPsychologicalLayout(roomCount, theme);
            
            // Set up room connections
            ConnectRoomsBasedOnPsychology();
            
            // Generate initial manifestations
            await GenerateInitialManifestations();

            // Set up starting room
            currentRoom = currentLevel.GetStartRoom();
            visitedRooms.Clear();
            
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error generating level: {ex.Message}");
            return false;
        }
    }

    private int CalculateRoomCount()
    {
        float psychologicalModifier = GetPsychologicalRoomModifier();
        int baseCount = baseRoomCount + (int)(currentLevel.LevelNumber * roomIncreasePerLevel);
        return Mathf.RoundToInt(baseCount * (1 + psychologicalModifier));
    }

    private float GetPsychologicalRoomModifier()
    {
        var profile = GameManager.Instance.ProfileManager.CurrentProfile;
        float modifier = 0f;

        // Increase room count based on psychological state
        if (profile.ObsessionLevel > 0.7f) modifier += 0.3f;
        if (profile.FearLevel > 0.8f) modifier += 0.2f;
        if (profile.CuriosityLevel > 0.6f) modifier += 0.1f;

        return modifier;
    }

    private async Task GenerateRoomsWithPsychologicalLayout(int roomCount, LevelTheme theme)
    {
        var profile = GameManager.Instance.ProfileManager.CurrentProfile;
        List<string> generatedRooms = new List<string>();

        for (int i = 0; i < roomCount; i++)
        {
            string roomID = $"L{currentLevel.LevelNumber}R{i}";
            string archetype = GetPsychologicallyRelevantArchetype(profile, theme);
            
            // Generate room description using LLM
            string prompt = GenerateRoomPrompt(archetype, theme, profile);
            string description = await llmManager.GenerateResponse(prompt, "room_generation");
            
            Room room = new Room(roomID, archetype);
            room.SetDescription(description);
            
            // Add psychological effects based on profile
            AddPsychologicalEffects(room, profile);
            
            currentLevel.AddRoom(room);
            generatedRooms.Add(roomID);
        }

        // Add optional loops and dead ends based on psychological state
        if (profile.ObsessionLevel > 0.6f)
        {
            AddObsessiveLoops(generatedRooms);
        }
    }

    private string GetPsychologicallyRelevantArchetype(PlayerAnalysisProfile profile, LevelTheme theme)
    {
        if (profile.FearLevel > 0.7f)
            return GetHighFearArchetype(theme);
        else if (profile.ObsessionLevel > 0.6f)
            return GetObsessionArchetype(theme);
        else if (profile.AggressionLevel > 0.5f)
            return GetAggressionArchetype(theme);
        
        return theme.roomArchetypes[UnityEngine.Random.Range(0, theme.roomArchetypes.Length)];
    }

    private string GetHighFearArchetype(LevelTheme theme)
    {
        string[] fearArchetypes = {
            "Claustrophobic Corridor",
            "Shadow-Filled Chamber",
            "Echo Chamber",
            "Darkness-Shrouded Room"
        };
        return fearArchetypes[UnityEngine.Random.Range(0, fearArchetypes.Length)];
    }

    private string GetObsessionArchetype(LevelTheme theme)
    {
        string[] obsessionArchetypes = {
            "Mirror Room",
            "Repeating Chamber",
            "Pattern Room",
            "Recursive Space"
        };
        return obsessionArchetypes[UnityEngine.Random.Range(0, obsessionArchetypes.Length)];
    }

    private string GetAggressionArchetype(LevelTheme theme)
    {
        string[] aggressionArchetypes = {
            "Broken Room",
            "Shattered Chamber",
            "Violent Space",
            "Destructive Area"
        };
        return aggressionArchetypes[UnityEngine.Random.Range(0, aggressionArchetypes.Length)];
    }

    private string GenerateRoomPrompt(string archetype, LevelTheme theme, PlayerAnalysisProfile profile)
    {
        return $"Generate a psychological horror room description:\n" +
               $"Room Type: {archetype}\n" +
               $"Level Theme: {theme.theme}\n" +
               $"Tone: {theme.tone}\n" +
               $"Keywords: {string.Join(", ", theme.keywords)}\n" +
               $"Psychological State:\n" +
               $"- Fear: {profile.FearLevel}\n" +
               $"- Obsession: {profile.ObsessionLevel}\n" +
               $"- Aggression: {profile.AggressionLevel}\n" +
               "Generate a description that emphasizes psychological horror elements.";
    }

    private void AddPsychologicalEffects(Room room, PlayerAnalysisProfile profile)
    {
        // Add effects based on psychological state
        if (profile.FearLevel > 0.6f)
        {
            room.AddEffect(new PsychologicalEffect(
                PsychologicalEffect.EffectType.ParanoiaInduction,
                profile.FearLevel,
                "The shadows seem to watch you...",
                "paranoia"
            ));
        }

        if (profile.ObsessionLevel > 0.7f)
        {
            room.AddEffect(new PsychologicalEffect(
                PsychologicalEffect.EffectType.RoomDistortion,
                profile.ObsessionLevel,
                "The room seems to pulse with your thoughts...",
                "obsession"
            ));
        }

        // Add random events
        if (UnityEngine.Random.value < eventProbability)
        {
            GameEvent evt = EventGenerator.GenerateEventForProfile(room, profile);
            room.AddEvent(evt);
        }
    }

    private void AddObsessiveLoops(List<string> roomIDs)
    {
        // Create loops in the layout for high obsession levels
        for (int i = 0; i < roomIDs.Count - 2; i++)
        {
            if (UnityEngine.Random.value < 0.3f)
            {
                string direction = GetRandomDirection();
                currentLevel.ConnectRooms(roomIDs[i], roomIDs[i + 2], direction);
                currentLevel.ConnectRooms(roomIDs[i + 2], roomIDs[i], GetOppositeDirection(direction));
            }
        }
    }

    private void ConnectRoomsBasedOnPsychology()
    {
        var profile = GameManager.Instance.ProfileManager.CurrentProfile;
        var roomIDs = new List<string>(currentLevel.Rooms.Keys);

        // Different connection patterns based on psychological state
        if (profile.ObsessionLevel > 0.7f)
        {
            CreateCircularConnections(roomIDs);
        }
        else if (profile.FearLevel > 0.6f)
        {
            CreateLinearPathWithDeadEnds(roomIDs);
        }
        else
        {
            CreateBranchingPaths(roomIDs);
        }

        SetExitRoom();
    }

    private void CreateCircularConnections(List<string> roomIDs)
    {
        for (int i = 0; i < roomIDs.Count; i++)
        {
            string direction = GetRandomDirection();
            int nextIndex = (i + 1) % roomIDs.Count;
            
            currentLevel.ConnectRooms(roomIDs[i], roomIDs[nextIndex], direction);
            currentLevel.ConnectRooms(roomIDs[nextIndex], roomIDs[i], GetOppositeDirection(direction));
        }
    }

    private void CreateLinearPathWithDeadEnds(List<string> roomIDs)
    {
        // Create main path
        for (int i = 0; i < roomIDs.Count - 1; i++)
        {
            string direction = GetRandomDirection();
            currentLevel.ConnectRooms(roomIDs[i], roomIDs[i + 1], direction);
            currentLevel.ConnectRooms(roomIDs[i + 1], roomIDs[i], GetOppositeDirection(direction));

            // Add dead ends
            if (i > 0 && UnityEngine.Random.value < 0.3f)
            {
                string deadEndDirection = GetRandomDirection();
                while (currentLevel.Rooms[roomIDs[i]].Exits.ContainsKey(deadEndDirection))
                {
                    deadEndDirection = GetRandomDirection();
                }
                
                // Create dead end room
                string deadEndID = $"L{currentLevel.LevelNumber}R{roomIDs.Count + i}";
                Room deadEnd = new Room(deadEndID, GetHighFearArchetype(levelThemes[currentLevel.LevelNumber]));
                llmManager.GenerateRoomDescription(deadEnd, currentLevel);
                currentLevel.AddRoom(deadEnd);
                
                currentLevel.ConnectRooms(roomIDs[i], deadEndID, deadEndDirection);
                currentLevel.ConnectRooms(deadEndID, roomIDs[i], GetOppositeDirection(deadEndDirection));
            }
        }
    }

    private void CreateBranchingPaths(List<string> roomIDs)
    {
        // Create main path
        for (int i = 0; i < roomIDs.Count - 1; i++)
        {
            string direction = GetRandomDirection();
            currentLevel.ConnectRooms(roomIDs[i], roomIDs[i + 1], direction);
            currentLevel.ConnectRooms(roomIDs[i + 1], roomIDs[i], GetOppositeDirection(direction));

            // Add branches
            if (i > 0 && i < roomIDs.Count - 2 && UnityEngine.Random.value < 0.4f)
            {
                string branchDirection = GetRandomDirection();
                while (currentLevel.Rooms[roomIDs[i]].Exits.ContainsKey(branchDirection))
                {
                    branchDirection = GetRandomDirection();
                }
                
                int targetIndex = Mathf.Min(i + 2, roomIDs.Count - 1);
                currentLevel.ConnectRooms(roomIDs[i], roomIDs[targetIndex], branchDirection);
                currentLevel.ConnectRooms(roomIDs[targetIndex], roomIDs[i], GetOppositeDirection(branchDirection));
            }
        }
    }

    private async Task GenerateInitialManifestations()
    {
        foreach (var room in currentLevel.Rooms.Values)
        {
            if (UnityEngine.Random.value < 0.3f) // 30% chance for initial manifestation
            {
                string prompt = $"Generate a psychological manifestation for this room:\n" +
                              $"Room Type: {room.Archetype}\n" +
                              $"Level Theme: {currentLevel.Theme}\n" +
                              $"Description: {room.BaseDescription}";

                string response = await llmManager.GenerateResponse(prompt, "manifestation_generation");
                await GameManager.Instance.GetComponent<ShadowManifestationSystem>()
                    .ProcessNewManifestations(currentLevel.Theme, new[] { response });
            }
        }
    }

    public async Task ChangeRoom(string roomID)
    {
        if (currentLevel.Rooms.TryGetValue(roomID, out Room room))
        {
            try {
                bool useGlitchEffect = ShouldUseGlitchEffect();
                await sceneManager.TransitionToRoomAsync(room, useGlitchEffect);
                
                currentRoom = room;
                visitedRooms.Add(roomID);
                
                // Update level state
                currentLevel.OnRoomVisited(roomID);
                stateManager.UpdateFromLevelState(currentLevel);

                CheckForPsychologicalTriggers();

                // Check for level progression
                if (await stateManager.ShouldProgressLevel(currentLevel))
                {
                    await OnLevelProgressionTriggered();
                }
            }
            catch (Exception ex) {
                Debug.LogError($"Error transitioning to room {roomID}: {ex.Message}");
            }
        }
    }

    private bool ShouldUseGlitchEffect()
    {
        var profile = GameManager.Instance.ProfileManager.CurrentProfile;
        return profile.FearLevel > 0.7f || profile.ObsessionLevel > 0.8f;
    }

    private void CheckForPsychologicalTriggers()
    {
        var profile = GameManager.Instance.ProfileManager.CurrentProfile;
        
        // Generate dynamic events based on psychological state
        if (profile.ObsessionLevel > 0.7f && UnityEngine.Random.value < 0.4f)
        {
            GameEvent evt = EventGenerator.GenerateEventForProfile(currentRoom, profile);
            currentRoom.AddEvent(evt);
            evt.Trigger(currentRoom);
        }
    }

    private string GetRandomDirection()
    {
        string[] directions = { "north", "south", "east", "west" };
        return directions[UnityEngine.Random.Range(0, directions.Length)];
    }

    private string GetOppositeDirection(string direction)
    {
        return direction switch
        {
            "north" => "south",
            "south" => "north",
            "east" => "west",
            "west" => "east",
            _ => direction
        };
    }

    private void SetExitRoom()
    {
        var roomIDs = new List<string>(currentLevel.Rooms.Keys);
        currentLevel.ExitRoomID = roomIDs[roomIDs.Count - 1];
    }

    private async Task OnLevelProgressionTriggered()
    {
        // Generate transition description
        string description = await stateManager.GeneratePhaseTransitionDescription(
            stateManager.GetCurrentState().currentPhase,
            "progression"
        );

        // Display transition
        GameManager.Instance.UIManager.DisplayMessage(description);

        // Update psychological state
        var profile = GameManager.Instance.ProfileManager.CurrentProfile;
        profile.FearLevel = Mathf.Min(1f, profile.FearLevel + 0.1f);
        profile.ObsessionLevel = Mathf.Min(1f, profile.ObsessionLevel + 0.1f);

        // Trigger manifestation
        await GameManager.Instance.GetComponent<ShadowManifestationSystem>()
            .ProcessPsychologicalState(profile, currentRoom);
    }

    // Public interface methods
    public Room GetCurrentRoom() => currentRoom;
    public Level GetCurrentLevel() => currentLevel;
    
    public bool TryMove(string direction)
    {
        if (currentRoom != null && currentRoom.Exits.TryGetValue(direction, out string targetRoomID))
        {
            ChangeRoom(targetRoomID);
            return true;
        }
        return false;
    }

    public bool IsCurrentRoomExit()
    {
        return currentRoom != null && currentRoom.RoomID == currentLevel.ExitRoomID;
    }

    public void ResetLevel()
    {
        if (currentLevel != null)
        {
            currentLevel.ResetProgress();
            currentRoom = currentLevel.GetStartRoom();
            visitedRooms.Clear();
            stateManager.ResetState();
        }
    }
}