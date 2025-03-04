using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum RoomType
{
    None,
    Entrance,
    Standard,
    Exit,
    DoctorOffice
}

public class Room
{
    // Room position in the grid
    public Vector2Int Position { get; private set; }
    
    // Connections to other rooms
    public Dictionary<Direction, Room> Connections { get; private set; }
    
    // Type of room (normal, entrance, exit)
    public RoomType Type { get; set; }
    
    // Additional properties
    public bool Visited { get; set; }
    
    public Room(Vector2Int position)
    {
        Position = position;
        Connections = new Dictionary<Direction, Room>();
        Type = RoomType.Standard;
        Visited = false;
    }
    
    public bool HasConnection(Direction direction)
    {
        return Connections.ContainsKey(direction);
    }
    
    public void AddConnection(Direction direction, Room connectedRoom)
    {
        if (!HasConnection(direction))
        {
            Connections[direction] = connectedRoom;
        }
    }
    
    public int GetConnectionCount()
    {
        return Connections.Count;
    }
}

// Direction enum for room connections
public enum Direction
{
    North,
    East,
    South,
    West
}

// Extension methods for Direction
public static class DirectionExtensions
{
    // Get the opposite direction
    public static Direction GetOpposite(this Direction direction)
    {
        switch (direction)
        {
            case Direction.North: return Direction.South;
            case Direction.East: return Direction.West; 
            case Direction.South: return Direction.North;
            case Direction.West: return Direction.East;
            default: return Direction.North; // Should never happen
        }
    }
    
    // Get the position offset for a direction
    public static Vector2Int GetOffset(this Direction direction)
    {
        switch (direction)
        {
            case Direction.North: return new Vector2Int(0, 1);
            case Direction.East: return new Vector2Int(1, 0);
            case Direction.South: return new Vector2Int(0, -1);
            case Direction.West: return new Vector2Int(-1, 0);
            default: return Vector2Int.zero; // Should never happen
        }
    }
}