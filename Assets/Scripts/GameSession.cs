using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents a game session and stores all game state data
/// </summary>
[Serializable]
public class GameSession
{
    // Core game data
    public int CurrentLevel { get; set; } = 1;
    public string Setting { get; set; }
    public string SettingSummary { get; set; }
    
    // Current level data
    public string CurrentLevelDescription { get; set; }
    public string CurrentLevelSummary { get; set; }
    
    // Room tracking
    public string CurrentRoomDescription { get; set; }
    public List<string> VisitedRooms { get; set; } = new List<string>();
    public List<string> RoomSummaries { get; set; } = new List<string>();
    public List<string> GameFlowHistory { get; set; } = new List<string>();
    public int RoomsCleared { get; set; } = 0;
    
    // Level progression data
    public List<string> LevelSummaries { get; set; } = new List<string>();
    public List<string> AppointmentSummaries { get; set; } = new List<string>();
    
    // Player data
    public PlayerProfile PlayerProfile { get; set; } = new PlayerProfile();
    
    // Game state flags
    public bool IsInDoctorsOffice { get; set; } = false;
    public bool IsGameOver { get; set; } = false;
    public bool IsGameWon { get; set; } = false;
    public bool IsInExitRoom { get; set; } = false;
    public bool IsInFinalConfrontation { get; set; } = false;
    
    // Current conversation context
    public string CurrentGameFlowPrompt { get; set; }
    public List<string> CurrentConversation { get; set; } = new List<string>();
    
    // Game flow state tracking
    public GameFlowState CurrentGameFlowState { get; set; } = GameFlowState.PreGame;
    public RoomType CurrentRoomType { get; set; } = RoomType.None;
    public string CurrentRoomImagePrompt { get; set; }
    public bool HasInitialIntroduction { get; set; } = false;
    
    public GameSession()
    {
        // Initialize with default values
        LevelSummaries = new List<string>();
        AppointmentSummaries = new List<string>();
        VisitedRooms = new List<string>();
        RoomSummaries = new List<string>();
        GameFlowHistory = new List<string>();
        CurrentConversation = new List<string>();
        PlayerProfile = new PlayerProfile();
    }
    
    public void ResetGameFlow()
    {
        CurrentConversation.Clear();
        CurrentGameFlowPrompt = string.Empty;
    }
    
    public void AdvanceToNextLevel()
    {
        CurrentLevel++;
        RoomsCleared = 0;
        VisitedRooms.Clear();
        RoomSummaries.Clear();
        GameFlowHistory.Clear();
        CurrentRoomDescription = string.Empty;
        ResetGameFlow();
        IsInDoctorsOffice = false;
        IsInExitRoom = false;
        CurrentGameFlowState = GameFlowState.LevelGeneration;
        CurrentRoomType = RoomType.None;
    }
    
    public void AddRoomSummary(string summary)
    {
        RoomSummaries.Add(summary);
        RoomsCleared++;
    }
    
    /// <summary>
    /// Sets the current room type based on the room description or game flow state
    /// </summary>
    /// <param name="roomType">The room type to set</param>
    public void SetCurrentRoomType(RoomType roomType)
    {
        CurrentRoomType = roomType;
        IsInExitRoom = (roomType == RoomType.Exit);
        IsInDoctorsOffice = (roomType == RoomType.DoctorOffice);
    }
}

/// <summary>
/// Represents the player's psychological profile
/// </summary>
[Serializable]
public class PlayerProfile
{
    // Core attributes
    public string AggressionLevel { get; set; } = "low";
    public string CuriosityLevel { get; set; } = "moderate";
    public string FearLevel { get; set; } = "low";
    public string ParanoiaLevel { get; set; } = "minimal";
    
    // Additional profiling data
    public List<string> IdentifiedFears { get; set; } = new List<string>();
    public List<string> IdentifiedWeaknesses { get; set; } = new List<string>();
    public List<string> IdentifiedTraits { get; set; } = new List<string>();
    
    // Character relationships
    public Dictionary<string, float> CharacterRelationships { get; set; } = new Dictionary<string, float>();
    
    // Full textual profile
    public string FullProfile { get; set; } = "Player is cautious but curious. No significant psychological issues identified.";
    
    public PlayerProfile()
    {
        IdentifiedFears = new List<string>();
        IdentifiedWeaknesses = new List<string>();
        IdentifiedTraits = new List<string>();
        CharacterRelationships = new Dictionary<string, float>();
    }
    
    public Dictionary<string, string> ToContextDictionary()
    {
        return new Dictionary<string, string>
        {
            { "player_aggression_level", AggressionLevel },
            { "player_curiosity_level", CuriosityLevel },
            { "player_fear_level", FearLevel },
            { "player_paranoia_level", ParanoiaLevel },
            { "player_profile", FullProfile }
        };
    }
}

/// <summary>
/// Represents the current state of the game flow
/// </summary>
public enum GameFlowState
{
    PreGame,
    SettingSummarization,
    LevelGeneration,
    LevelSummarization,
    RoomGeneration,
    RoomImageGeneration,
    GameFlow,
    RoomSummarization,
    DoctorsOffice,
    AppointmentSummarization,
    PlayerProfileUpdate,
    FinalConfrontation,
    GameOver
}