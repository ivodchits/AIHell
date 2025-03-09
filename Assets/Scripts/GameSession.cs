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
    public GameSetting Setting { get; set; }
    public string SettingSummary { get; set; }
    
    // Current level data
    public string CurrentLevelDescription { get; set; }
    
    // Room tracking
    public string CurrentRoomDescription { get; set; }
    public Dictionary<Vector2Int, CompletedRoomData> VisitedRooms { get; private set; } = new ();
    public int RoomsCleared { get; set; } = 0;
    
    // Level progression data
    public List<string> LevelSummaries { get; set; } = new List<string>();
    public List<string> AppointmentSummaries { get; set; } = new List<string>();
    
    // Player data
    public string PlayerProfile { get; set; } = string.Empty;
    
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
    public Texture2D CurrentRoomImage { get; set; }
    public bool HasInitialIntroduction { get; set; } = false;

    public GameSession()
    {
        // Initialize with default values
        LevelSummaries = new List<string>();
        AppointmentSummaries = new List<string>();
        VisitedRooms = new();
        CurrentConversation = new List<string>();
    }
    
    public void AdvanceToNextLevel()
    {
        CurrentLevel++;
        RoomsCleared = 0;
        VisitedRooms.Clear();
        CurrentRoomDescription = string.Empty;
        IsInDoctorsOffice = false;
        IsInExitRoom = false;
        CurrentGameFlowState = GameFlowState.LevelGeneration;
        SetCurrentRoomType(RoomType.Entrance);
    }
    
    public void AddRoomSummary(Room room, string summary, Texture2D image)
    {
        VisitedRooms.Add(room.Position, new CompletedRoomData
        {
            RoomSummary = summary,
            RoomImage = image
        });
        RoomsCleared++;
    }
    
    public void SetRoomRevisitDescription(Room room, string revisitDescription)
    {
        if (VisitedRooms.TryGetValue(room.Position, out var visitedRoom))
        {
            visitedRoom.RevisitDescription = revisitDescription;
        }
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

    public string GetRoomSummaries()
    {
        var result = string.Empty;
        var counter = 1;
        foreach (var roomData in VisitedRooms.Values)
        {
            result += $"{counter++}. {roomData.RoomSummary}\n";
        }

        return result;
    }
}

[Serializable]
public class CompletedRoomData
{
    public string RoomSummary;
    public string RevisitDescription;
    public Texture2D RoomImage;
}

/// <summary>
/// Represents the current state of the game flow
/// </summary>
public enum GameFlowState
{
    PreGame,
    SettingCreation,
    LevelGeneration,
    RoomSelection,
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