using UnityEngine;
using System;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AIHell.Core.Data;

public class LevelManager : MonoBehaviour
{
    [Header("Level Generation")]
    public int baseRoomCount = 5;
    public float roomIncreasePerLevel = 2f;
    public float eventProbability = 0.3f;
    
    public Level CurrentLevel { get; private set; }
    public Room CurrentRoom { get; private set; }
    private Dictionary<int, LevelThemeData> levelThemes;
    private SceneManager sceneManager;

    private void Awake()
    {
        InitializeLevelThemes();
        sceneManager = GetComponent<SceneManager>();
        if (sceneManager == null)
        {
            sceneManager = gameObject.AddComponent<SceneManager>();
        }
    }

    private void InitializeLevelThemes()
    {
        levelThemes = new Dictionary<int, LevelThemeData>
        {
            { 1, new LevelThemeData("Surface Anxiety", "Mundane Dread", 
                new List<string> { "office", "street", "clock", "waiting", "silence" }) },
            { 2, new LevelThemeData("Isolation", "Paranoia", 
                new List<string> { "empty", "echo", "whispers", "watching", "shadows" }) },
            { 3, new LevelThemeData("Existential Dread", "Meaninglessness", 
                new List<string> { "void", "mirrors", "infinite", "decay", "time" }) },
            { 4, new LevelThemeData("Obsession", "Self-Destruction", 
                new List<string> { "repetition", "ritual", "blood", "machinery", "patterns" }) },
            { 5, new LevelThemeData("Ultimate Despair", "Annihilation", 
                new List<string> { "abyss", "darkness", "eternal", "unknowable", "cosmic" }) }
        };
    }

    public void LoadLevel(int levelNumber)
    {
        if (!levelThemes.ContainsKey(levelNumber))
        {
            Debug.LogError($"Invalid level number: {levelNumber}");
            return;
        }

        var themeData = levelThemes[levelNumber];
        CurrentLevel = new Level(levelNumber, themeData.Theme, themeData.Tone, themeData.Keywords);

        // Play level-specific ambience
        GameAudioManager.Instance.PlayLevelAmbience(levelNumber);

        GenerateLevel();
        LoadStartingRoom();
    }

    private void GenerateLevel()
    {
        int roomCount = CalculateRoomCount();
        var profile = GameManager.Instance.ProfileManager.CurrentProfile;
        
        // Generate rooms with psychological consideration
        GenerateRoomsWithPsychologicalLayout(roomCount, profile);
        
        ConnectRoomsBasedOnPsychology();
        SetExitRoom();
    }

    private int CalculateRoomCount()
    {
        float psychologicalModifier = GetPsychologicalRoomModifier();
        int baseCount = baseRoomCount + (int)(CurrentLevel.LevelNumber * roomIncreasePerLevel);
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

    private void GenerateRoomsWithPsychologicalLayout(int roomCount, PlayerAnalysisProfile profile)
    {
        List<string> generatedRooms = new List<string>();

        // Create initial rooms
        for (int i = 0; i < roomCount; i++)
        {
            string roomID = $"L{CurrentLevel.LevelNumber}R{i}";
            string archetype = GetPsychologicallyRelevantArchetype(profile);
            Room room = new Room(roomID, archetype);
            
            // Generate description using LLM
            GameManager.Instance.LLMManager.GenerateRoomDescription(room, CurrentLevel);
            
            // Add psychological effects based on profile
            AddPsychologicalEffects(room, profile);
            
            CurrentLevel.AddRoom(room);
            generatedRooms.Add(roomID);
        }

        // Add optional loops and dead ends based on psychological state
        if (profile.ObsessionLevel > 0.6f)
        {
            AddObsessiveLoops(generatedRooms);
        }
    }

    private string GetPsychologicallyRelevantArchetype(PlayerAnalysisProfile profile)
    {
        // Select room archetype based on psychological state
        if (profile.FearLevel > 0.7f)
        {
            return GetHighFearArchetype();
        }
        else if (profile.ObsessionLevel > 0.6f)
        {
            return GetObsessionArchetype();
        }
        else if (profile.AggressionLevel > 0.5f)
        {
            return GetAggressionArchetype();
        }
        
        return GetDefaultArchetype();
    }

    private string GetHighFearArchetype()
    {
        string[] fearArchetypes = {
            "Claustrophobic Corridor",
            "Shadow-Filled Chamber",
            "Echo Chamber",
            "Darkness-Shrouded Room"
        };
        return fearArchetypes[Random.Range(0, fearArchetypes.Length)];
    }

    private string GetObsessionArchetype()
    {
        string[] obsessionArchetypes = {
            "Mirror Room",
            "Repeating Chamber",
            "Pattern Room",
            "Recursive Space"
        };
        return obsessionArchetypes[Random.Range(0, obsessionArchetypes.Length)];
    }

    private string GetAggressionArchetype()
    {
        string[] aggressionArchetypes = {
            "Broken Room",
            "Shattered Chamber",
            "Violent Space",
            "Destructive Area"
        };
        return aggressionArchetypes[Random.Range(0, aggressionArchetypes.Length)];
    }

    private string GetDefaultArchetype()
    {
        string[] defaultArchetypes = {
            "Empty Room",
            "Standard Chamber",
            "Neutral Space",
            "Basic Room"
        };
        return defaultArchetypes[Random.Range(0, defaultArchetypes.Length)];
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
        if (Random.value < eventProbability)
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
            if (Random.value < 0.3f)
            {
                string direction = GetRandomDirection();
                CurrentLevel.ConnectRooms(roomIDs[i], roomIDs[i + 2], direction);
                CurrentLevel.ConnectRooms(roomIDs[i + 2], roomIDs[i], GetOppositeDirection(direction));
            }
        }
    }

    private void ConnectRoomsBasedOnPsychology()
    {
        var profile = GameManager.Instance.ProfileManager.CurrentProfile;
        var roomIDs = new List<string>(CurrentLevel.Rooms.Keys);

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
    }

    private void CreateCircularConnections(List<string> roomIDs)
    {
        for (int i = 0; i < roomIDs.Count; i++)
        {
            string direction = GetRandomDirection();
            int nextIndex = (i + 1) % roomIDs.Count;
            
            CurrentLevel.ConnectRooms(roomIDs[i], roomIDs[nextIndex], direction);
            CurrentLevel.ConnectRooms(roomIDs[nextIndex], roomIDs[i], GetOppositeDirection(direction));
        }
    }

    private void CreateLinearPathWithDeadEnds(List<string> roomIDs)
    {
        // Create main path
        for (int i = 0; i < roomIDs.Count - 1; i++)
        {
            string direction = GetRandomDirection();
            CurrentLevel.ConnectRooms(roomIDs[i], roomIDs[i + 1], direction);
            CurrentLevel.ConnectRooms(roomIDs[i + 1], roomIDs[i], GetOppositeDirection(direction));

            // Add dead ends
            if (i > 0 && Random.value < 0.3f)
            {
                string deadEndDirection = GetRandomDirection();
                while (CurrentLevel.Rooms[roomIDs[i]].Exits.ContainsKey(deadEndDirection))
                {
                    deadEndDirection = GetRandomDirection();
                }
                
                // Create dead end room
                string deadEndID = $"L{CurrentLevel.LevelNumber}R{roomIDs.Count + i}";
                Room deadEnd = new Room(deadEndID, GetHighFearArchetype());
                GameManager.Instance.LLMManager.GenerateRoomDescription(deadEnd, CurrentLevel);
                CurrentLevel.AddRoom(deadEnd);
                
                CurrentLevel.ConnectRooms(roomIDs[i], deadEndID, deadEndDirection);
                CurrentLevel.ConnectRooms(deadEndID, roomIDs[i], GetOppositeDirection(deadEndDirection));
            }
        }
    }

    private void CreateBranchingPaths(List<string> roomIDs)
    {
        // Create main path
        for (int i = 0; i < roomIDs.Count - 1; i++)
        {
            string direction = GetRandomDirection();
            CurrentLevel.ConnectRooms(roomIDs[i], roomIDs[i + 1], direction);
            CurrentLevel.ConnectRooms(roomIDs[i + 1], roomIDs[i], GetOppositeDirection(direction));

            // Add branches
            if (i > 0 && i < roomIDs.Count - 2 && Random.value < 0.4f)
            {
                string branchDirection = GetRandomDirection();
                while (CurrentLevel.Rooms[roomIDs[i]].Exits.ContainsKey(branchDirection))
                {
                    branchDirection = GetRandomDirection();
                }
                
                // Connect to a room further ahead
                int targetIndex = Mathf.Min(i + 2, roomIDs.Count - 1);
                CurrentLevel.ConnectRooms(roomIDs[i], roomIDs[targetIndex], branchDirection);
                CurrentLevel.ConnectRooms(roomIDs[targetIndex], roomIDs[i], GetOppositeDirection(branchDirection));
            }
        }
    }

    public async Task ChangeRoom(string roomID)
    {
        if (CurrentLevel.Rooms.TryGetValue(roomID, out Room room))
        {
            try {
                bool useGlitchEffect = ShouldUseGlitchEffect();
                await sceneManager.TransitionToRoomAsync(room, useGlitchEffect);
                
                CurrentRoom = room;
                CheckForPsychologicalTriggers();
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
        if (profile.ObsessionLevel > 0.7f && Random.value < 0.4f)
        {
            var obsessions = profile.GetActiveObsessions();
            if (obsessions.Length > 0)
            {
                GameEvent evt = EventGenerator.GenerateEventForProfile(CurrentRoom, profile);
                CurrentRoom.AddEvent(evt);
                evt.Trigger(CurrentRoom);
            }
        }
    }

    private string GetRandomDirection()
    {
        string[] directions = { "north", "south", "east", "west" };
        return directions[Random.Range(0, directions.Length)];
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
        // Set last room as exit room for simplicity
        var roomIDs = new List<string>(CurrentLevel.Rooms.Keys);
        CurrentLevel.ExitRoomID = roomIDs[roomIDs.Count - 1];
    }

    private void LoadStartingRoom()
    {
        var roomIDs = new List<string>(CurrentLevel.Rooms.Keys);
        if (roomIDs.Count > 0)
        {
            ChangeRoom(roomIDs[0]);
        }
    }

    public bool TryMove(string direction)
    {
        if (CurrentRoom != null && CurrentRoom.Exits.TryGetValue(direction, out string targetRoomID))
        {
            ChangeRoom(targetRoomID);
            return true;
        }
        return false;
    }

    public bool IsCurrentRoomExit()
    {
        return CurrentRoom != null && CurrentRoom.RoomID == CurrentLevel.ExitRoomID;
    }

    private async Task<bool> GenerateInitialElements()
    {
        var profile = GameManager.Instance.ProfileManager.CurrentProfile;
        try {
            await Task.CompletedTask; // Placeholder for actual async work
            return true;
        }
        catch (Exception ex) {
            Debug.LogError($"Failed to generate initial elements: {ex.Message}");
            return false;
        }
    }
}

public class LevelThemeData
{
    public string Theme { get; private set; }
    public string Tone { get; private set; }
    public List<string> Keywords { get; private set; }

    public LevelThemeData(string theme, string tone, List<string> keywords)
    {
        Theme = theme;
        Tone = tone;
        Keywords = keywords;
    }
}