using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Added for UI components

public class LevelVisualizer : MonoBehaviour
{
    [SerializeField] LevelGenerator levelGenerator;
    [SerializeField] RectTransform contentParent; // Changed to RectTransform for UI
    [SerializeField] GameObject roomPrefab;
    [SerializeField] GameObject entrancePrefab;
    [SerializeField] GameObject exitPrefab;
    [SerializeField] GameObject doorPrefab;
    [SerializeField] GameObject playerMarkerPrefab; // Added: Player marker prefab
    [SerializeField] Color currentRoomHighlightColor = Color.yellow; // Added: Color to highlight current room
    [SerializeField] float roomSpacing = 100f; // Increased spacing for UI scale

    Dictionary<Vector2Int, GameObject> roomObjects = new Dictionary<Vector2Int, GameObject>();
    List<GameObject> doorObjects = new List<GameObject>();
    GameObject playerMarker; // Added: Reference to player marker object
    Vector2Int currentPlayerPosition; // Added: Tracks current player position
    Room currentPlayerRoom; // Added: Reference to current player room
    
    // Reference to the current level being visualized
    Level currentLevel;
    
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
        
        // Center the content after everything is instantiated
        CenterContent();
        
        // Set player at entrance room initially
        MovePlayerToRoom(currentLevel.GetEntranceRoom());
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
        
        // Destroy player marker if it exists
        if (playerMarker != null)
        {
            DestroyImmediate(playerMarker);
            playerMarker = null;
        }
        
        currentPlayerRoom = null;
    }
    
    // Create GameObjects for each room in the level
    void CreateRoomObjects()
    {
        foreach (Room room in currentLevel.GetAllRooms())
        {
            Vector2 roomPosition = new Vector2(room.Position.x * roomSpacing, room.Position.y * roomSpacing);
            GameObject roomObject = null;
            
            // Use appropriate prefab based on room type
            switch (room.Type)
            {
                case RoomType.Entrance:
                    roomObject = Instantiate(entrancePrefab != null ? entrancePrefab : roomPrefab, contentParent);
                    roomObject.name = $"Room_Entrance_{room.Position.x}_{room.Position.y}";
                    break;
                    
                case RoomType.Exit:
                    roomObject = Instantiate(exitPrefab != null ? exitPrefab : roomPrefab, contentParent);
                    roomObject.name = $"Room_Exit_{room.Position.x}_{room.Position.y}";
                    break;
                    
                default:
                    roomObject = Instantiate(roomPrefab, contentParent);
                    roomObject.name = $"Room_{room.Position.x}_{room.Position.y}";
                    break;
            }
            
            if (roomObject != null)
            {
                // Set the UI position using RectTransform
                RectTransform rectTransform = roomObject.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.anchoredPosition = roomPosition;
                }
                roomObjects[room.Position] = roomObject;
            }
        }
    }
    
    // Create door connections between rooms
    void CreateDoorConnections()
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
                        // Calculate door position (midpoint between rooms) - now using UI coordinates
                        Vector2 roomPos = new Vector2(room.Position.x * roomSpacing, room.Position.y * roomSpacing);
                        Vector2 connectedRoomPos = new Vector2(connectedRoom.Position.x * roomSpacing, connectedRoom.Position.y * roomSpacing);
                        Vector2 doorPosition = (roomPos + connectedRoomPos) / 2f;
                        
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
                        
                        GameObject doorObject = Instantiate(doorPrefab, contentParent);
                        RectTransform doorRectTransform = doorObject.GetComponent<RectTransform>();
                        if (doorRectTransform != null)
                        {
                            doorRectTransform.anchoredPosition = doorPosition;
                            doorRectTransform.localRotation = Quaternion.Euler(0f, 0f, rotationZ);
                        }
                        doorObject.name = $"Door_{room.Position.x}_{room.Position.y}_to_{connectedRoom.Position.x}_{connectedRoom.Position.y}";
                        doorObjects.Add(doorObject);
                    }
                }
            }
        }
    }
    
    // Center the content in the parent container
    void CenterContent()
    {
        if (roomObjects.Count == 0 || contentParent == null)
            return;
            
        // Calculate the bounds of all room objects
        Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 max = new Vector2(float.MinValue, float.MinValue);
        
        foreach (GameObject room in roomObjects.Values)
        {
            RectTransform rectTransform = room.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                Vector2 position = rectTransform.anchoredPosition;
                
                // Update min/max bounds
                min.x = Mathf.Min(min.x, position.x);
                min.y = Mathf.Min(min.y, position.y);
                max.x = Mathf.Max(max.x, position.x);
                max.y = Mathf.Max(max.y, position.y);
            }
        }
        
        // Calculate center point of the content
        Vector2 center = (min + max) / 2f;
        
        // Offset all objects to center them at (0,0)
        foreach (GameObject room in roomObjects.Values)
        {
            RectTransform rectTransform = room.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition -= center;
            }
        }
        
        // Also offset door objects
        foreach (GameObject door in doorObjects)
        {
            RectTransform rectTransform = door.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition -= center;
            }
        }
    }
    
    // Move player marker to a specific room
    public void MovePlayerToRoom(Room room)
    {
        if (room == null || currentLevel == null)
            return;
            
        // Update current player room reference
        currentPlayerRoom = room;
        currentPlayerPosition = room.Position;
        
        // Reset previous room highlight if any
        foreach (GameObject roomObj in roomObjects.Values)
        {
            Image roomImage = roomObj.GetComponent<Image>();
            if (roomImage != null)
            {
                roomImage.color = Color.white; // Reset to default color
            }
        }
        
        // Highlight the current room
        if (roomObjects.TryGetValue(currentPlayerPosition, out GameObject currentRoomObj))
        {
            Image roomImage = currentRoomObj.GetComponent<Image>();
            if (roomImage != null)
            {
                roomImage.color = currentRoomHighlightColor;
            }
            
            // Update or create player marker
            if (playerMarker == null && playerMarkerPrefab != null)
            {
                playerMarker = Instantiate(playerMarkerPrefab, contentParent);
                
                // Make sure player marker is rendered on top of room
                RectTransform markerTransform = playerMarker.GetComponent<RectTransform>();
                if (markerTransform != null)
                {
                    markerTransform.SetAsLastSibling();
                }
            }
            
            if (playerMarker != null)
            {
                RectTransform markerTransform = playerMarker.GetComponent<RectTransform>();
                RectTransform roomTransform = currentRoomObj.GetComponent<RectTransform>();
                
                if (markerTransform != null && roomTransform != null)
                {
                    markerTransform.anchoredPosition = roomTransform.anchoredPosition;
                }
            }
        }
    }
    
    // Move player to a room by grid position
    public void MovePlayerToPosition(Vector2Int position)
    {
        if (currentLevel == null)
            return;
            
        // Try to find room at position
        foreach (Room room in currentLevel.GetAllRooms())
        {
            if (room.Position == position)
            {
                MovePlayerToRoom(room);
                return;
            }
        }
    }
    
    // Get the current room the player is in
    public Room GetCurrentPlayerRoom()
    {
        return currentPlayerRoom;
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