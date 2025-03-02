using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelVisualizer : MonoBehaviour
{
    [SerializeField] private LevelGenerator levelGenerator;
    [SerializeField] private Transform contentParent;
    [SerializeField] private GameObject roomPrefab;
    [SerializeField] private GameObject entrancePrefab;
    [SerializeField] private GameObject exitPrefab;
    [SerializeField] private GameObject doorPrefab;
    [SerializeField] private float roomSpacing = 5f;
    
    private Dictionary<Vector2Int, GameObject> roomObjects = new Dictionary<Vector2Int, GameObject>();
    private List<GameObject> doorObjects = new List<GameObject>();
    
    // Reference to the current level being visualized
    private Level currentLevel;
    
    // Call this to visualize the current level
    public void VisualizeCurrentLevel()
    {
        if (levelGenerator == null)
        {
            Debug.LogError("LevelVisualizer: LevelGenerator reference is missing!");
            return;
        }
        
        ClearVisualization();
        
        currentLevel = levelGenerator.GetCurrentLevel();
        if (currentLevel == null)
        {
            Debug.LogError("LevelVisualizer: Current level is null!");
            return;
        }
        
        CreateRoomObjects();
        CreateDoorConnections();
    }
    
    // Clear existing visualization objects
    public void ClearVisualization()
    {
        // Destroy all room objects
        foreach (GameObject room in roomObjects.Values)
        {
            if (room != null)
            {
                DestroyImmediate(room);
            }
        }
        roomObjects.Clear();
        
        // Destroy all door objects
        foreach (GameObject door in doorObjects)
        {
            if (door != null)
            {
                DestroyImmediate(door);
            }
        }
        doorObjects.Clear();
    }
    
    // Create GameObjects for each room in the level
    private void CreateRoomObjects()
    {
        foreach (Room room in currentLevel.GetAllRooms())
        {
            Vector3 roomPosition = new Vector3(room.Position.x * roomSpacing, room.Position.y * roomSpacing, 0f);
            GameObject roomObject = null;
            
            // Use appropriate prefab based on room type
            switch (room.Type)
            {
                case RoomType.Entrance:
                    roomObject = Instantiate(entrancePrefab != null ? entrancePrefab : roomPrefab, roomPosition, Quaternion.identity, contentParent);
                    roomObject.name = $"Room_Entrance_{room.Position.x}_{room.Position.y}";
                    break;
                    
                case RoomType.Exit:
                    roomObject = Instantiate(exitPrefab != null ? exitPrefab : roomPrefab, roomPosition, Quaternion.identity, contentParent);
                    roomObject.name = $"Room_Exit_{room.Position.x}_{room.Position.y}";
                    break;
                    
                default:
                    roomObject = Instantiate(roomPrefab, roomPosition, Quaternion.identity, contentParent);
                    roomObject.name = $"Room_{room.Position.x}_{room.Position.y}";
                    break;
            }
            
            if (roomObject != null)
            {
                roomObjects[room.Position] = roomObject;
            }
        }
    }
    
    // Create door connections between rooms
    private void CreateDoorConnections()
    {
        // Create doors for each room connection
        foreach (Room room in currentLevel.GetAllRooms())
        {
            foreach (var connection in room.Connections)
            {
                Direction direction = connection.Key;
                Room connectedRoom = connection.Value;
                
                // Create door only in one direction to avoid duplicates
                if (room.Position.x < connectedRoom.Position.x || 
                    (room.Position.x == connectedRoom.Position.x && room.Position.y < connectedRoom.Position.y))
                {
                    if (doorPrefab != null)
                    {
                        // Calculate door position (midpoint between rooms)
                        Vector3 roomPos = new Vector3(room.Position.x * roomSpacing, room.Position.y * roomSpacing, 0f);
                        Vector3 connectedRoomPos = new Vector3(connectedRoom.Position.x * roomSpacing, connectedRoom.Position.y * roomSpacing, 0f);
                        Vector3 doorPosition = (roomPos + connectedRoomPos) / 2f;
                        
                        // Calculate rotation based on the connection direction
                        float rotationZ = 0f;
                        switch (direction)
                        {
                            case Direction.North:
                            case Direction.South:
                                rotationZ = 0f;
                                break;
                            case Direction.East:
                            case Direction.West:
                                rotationZ = 90f;
                                break;
                        }
                        
                        GameObject doorObject = Instantiate(doorPrefab, doorPosition, Quaternion.Euler(0f, 0f, rotationZ), contentParent);
                        doorObject.name = $"Door_{room.Position.x}_{room.Position.y}_to_{connectedRoom.Position.x}_{connectedRoom.Position.y}";
                        doorObjects.Add(doorObject);
                    }
                }
            }
        }
    }
    
    // Visualize a specific level by index
    public void VisualizeLevel(int levelIndex)
    {
        if (levelGenerator == null)
        {
            Debug.LogError("LevelVisualizer: LevelGenerator reference is missing!");
            return;
        }
        
        Level level = levelGenerator.SetCurrentLevel(levelIndex);
        if (level != null)
        {
            VisualizeCurrentLevel();
        }
        else
        {
            Debug.LogError($"LevelVisualizer: Could not set level index to {levelIndex}!");
        }
    }
}