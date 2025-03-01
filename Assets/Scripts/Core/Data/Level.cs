using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

public class Level
{
    public int LevelNumber { get; private set; }
    public string Theme { get; private set; }
    public string Tone { get; private set; }
    public Dictionary<string, Room> Rooms { get; private set; }
    public List<string> Keywords { get; private set; }
    public string ExitRoomID { get; set; }
    public float PsychologicalIntensity { get; private set; }
    public Dictionary<string, float> ThematicWeights { get; private set; }
    public string StartRoomID { get; private set; }

    private Dictionary<string, List<string>> roomConnections;
    private HashSet<string> visitedRooms;
    private Queue<string> roomProgression;

    public Level(int levelNumber, string theme, string tone, List<string> keywords)
    {
        LevelNumber = levelNumber;
        Theme = theme;
        Tone = tone;
        Keywords = keywords;
        Rooms = new Dictionary<string, Room>();
        roomConnections = new Dictionary<string, List<string>>();
        visitedRooms = new HashSet<string>();
        roomProgression = new Queue<string>();
        ThematicWeights = new Dictionary<string, float>();
        InitializeThematicWeights();
    }

    private void InitializeThematicWeights()
    {
        ThematicWeights["isolation"] = 0.3f;
        ThematicWeights["paranoia"] = 0.3f;
        ThematicWeights["psychological"] = 0.4f;
        ThematicWeights["surreal"] = 0.2f;
        ThematicWeights["cosmic"] = 0.1f;
    }

    public void AddRoom(Room room)
    {
        if (!Rooms.ContainsKey(room.RoomID))
        {
            Rooms.Add(room.RoomID, room);
            roomConnections[room.RoomID] = new List<string>();
            if (Rooms.Count == 1)
            {
                StartRoomID = room.RoomID;
            }
        }
    }

    public void ConnectRooms(string roomID1, string roomID2, string direction)
    {
        if (Rooms.ContainsKey(roomID1) && Rooms.ContainsKey(roomID2))
        {
            if (!roomConnections[roomID1].Contains(roomID2))
            {
                roomConnections[roomID1].Add(roomID2);
                Rooms[roomID1].ConnectTo(direction, roomID2);
            }

            // Add reverse connection with opposite direction
            string oppositeDirection = GetOppositeDirection(direction);
            if (!roomConnections[roomID2].Contains(roomID1))
            {
                roomConnections[roomID2].Add(roomID1);
                Rooms[roomID2].ConnectTo(oppositeDirection, roomID1);
            }
        }
    }

    private string GetOppositeDirection(string direction)
    {
        return direction.ToLower() switch
        {
            "north" => "south",
            "south" => "north",
            "east" => "west",
            "west" => "east",
            "up" => "down",
            "down" => "up",
            _ => direction
        };
    }

    public void OnRoomVisited(string roomID)
    {
        if (Rooms.TryGetValue(roomID, out Room room))
        {
            visitedRooms.Add(roomID);
            roomProgression.Enqueue(roomID);
            if (roomProgression.Count > 5) // Keep track of last 5 rooms
            {
                roomProgression.Dequeue();
            }

            UpdatePsychologicalIntensity();
            UpdateThematicWeights(room);
        }
    }

    private void UpdatePsychologicalIntensity()
    {
        float baseIntensity = (float)visitedRooms.Count / Rooms.Count;
        float progressionModifier = CalculateProgressionModifier();
        PsychologicalIntensity = Mathf.Clamp01(baseIntensity * progressionModifier);
    }

    private float CalculateProgressionModifier()
    {
        // Calculate based on room progression pattern
        var recentRooms = roomProgression.ToList();
        if (recentRooms.Count < 2) return 1f;

        float modifier = 1f;

        // Check for backtracking (revisiting previous rooms)
        var uniqueRecent = new HashSet<string>(recentRooms);
        if (uniqueRecent.Count < recentRooms.Count)
        {
            modifier += 0.2f; // Increase intensity when backtracking
        }

        // Check for distance from exit
        if (!string.IsNullOrEmpty(ExitRoomID) && recentRooms.Contains(ExitRoomID))
        {
            modifier += 0.3f; // Increase intensity near exit
        }

        return modifier;
    }

    private void UpdateThematicWeights(Room room)
    {
        // Update weights based on room psychological state
        if (room.PsychologicalIntensity > 0.7f)
        {
            ThematicWeights["psychological"] = Mathf.Min(1f, ThematicWeights["psychological"] + 0.1f);
            ThematicWeights["surreal"] = Mathf.Min(1f, ThematicWeights["surreal"] + 0.05f);
        }

        // Update based on active effects
        foreach (var effect in room.ActiveEffects)
        {
            switch (effect.Type)
            {
                case PsychologicalEffect.EffectType.ParanoiaInduction:
                    ThematicWeights["paranoia"] = Mathf.Min(1f, ThematicWeights["paranoia"] + 0.15f);
                    break;
                case PsychologicalEffect.EffectType.RoomDistortion:
                    ThematicWeights["surreal"] = Mathf.Min(1f, ThematicWeights["surreal"] + 0.1f);
                    break;
                case PsychologicalEffect.EffectType.TimeDistortion:
                    ThematicWeights["cosmic"] = Mathf.Min(1f, ThematicWeights["cosmic"] + 0.1f);
                    break;
            }
        }

        NormalizeThematicWeights();
    }

    private void NormalizeThematicWeights()
    {
        float total = ThematicWeights.Values.Sum();
        if (total > 0)
        {
            foreach (var theme in ThematicWeights.Keys.ToList())
            {
                ThematicWeights[theme] /= total;
            }
        }
    }

    public List<string> GetConnectedRooms(string roomID)
    {
        return roomConnections.ContainsKey(roomID) ? roomConnections[roomID] : new List<string>();
    }

    public List<string> GetUnvisitedConnections(string roomID)
    {
        return GetConnectedRooms(roomID).Where(r => !visitedRooms.Contains(r)).ToList();
    }

    public Room GetStartRoom()
    {
        return Rooms.TryGetValue(StartRoomID, out Room room) ? room : null;
    }

    public Room GetExitRoom()
    {
        return Rooms.TryGetValue(ExitRoomID, out Room room) ? room : null;
    }

    public float GetCompletionPercentage()
    {
        return (float)visitedRooms.Count / Rooms.Count;
    }

    public bool IsComplete()
    {
        return visitedRooms.Contains(ExitRoomID);
    }

    public void ResetProgress()
    {
        visitedRooms.Clear();
        roomProgression.Clear();
        PsychologicalIntensity = 0f;
        InitializeThematicWeights();
        
        foreach (var room in Rooms.Values)
        {
            room.IsVisited = false;
        }
    }
}