using System;
using System.Collections.Generic;
using AIHell.UI;
using UnityEngine;

public class LevelVisualizer : UIControllerBase
{
    [SerializeField] LevelGenerator levelGenerator;
    [SerializeField] RectTransform contentParent;
    [SerializeField] RoomView roomPrefab;
    [SerializeField] RoomView entrancePrefab;
    [SerializeField] RoomView exitPrefab;
    [SerializeField] GameObject doorPrefab;
    [SerializeField] GameObject playerMarkerPrefab;
    [SerializeField] GameObject roomSelection;
    [SerializeField] float roomSpacing = 40f; // Increased spacing for UI scale
    [SerializeField] float flashingFrequency = 1f; // Frequency of flashing effect
    
    public event Action<Room> RoomSelected;
    
    const float maxFlashingAlpha = 0.6f; // Maximum alpha for flashing effect

    Dictionary<Vector2Int, RoomView> roomObjects = new ();
    List<GameObject> doorObjects = new ();
    List<RoomView> flashingRooms = new ();
    GameObject playerMarker;
    Vector2Int currentPlayerPosition;
    Room currentPlayerRoom;
    RoomView currentlySelectedRoom;
    float currentAlpha = maxFlashingAlpha;
    bool doctorOfficeAvailable = false;
    
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
        foreach (var room in roomObjects.Values)
        {
            if (room != null)
            {
                DestroyImmediate(room.gameObject);
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
            RoomView roomObject = null;
            
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
                roomObject.Initialize(room);
                roomObject.Hide();
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
        
        foreach (var room in roomObjects.Values)
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
        foreach (var room in roomObjects.Values)
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
        
        // Highlight the current room
        if (roomObjects.TryGetValue(currentPlayerPosition, out var currentRoomObj))
        {
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

    public void RevealRoom(Room room)
    {
        roomObjects[room.Position].Show();
    }

    public void FlashAdjacentRooms(Room currentRoom)
    {
        doctorOfficeAvailable = currentRoom.Type == RoomType.Exit;
        var connections = currentRoom.Connections;
        foreach (var room in connections.Values)
        {
            if (roomObjects.TryGetValue(room.Position, out var roomView))
            {
                roomView.Flash();
                flashingRooms.Add(roomView);
            }
        }
    }

    public override void Show()
    {
        base.Show();
        roomSelection.SetActive(false);
    }

    void Update()
    {
        if (flashingRooms.Count == 0)
        {
            return;
        }
        
        var newAlpha = Mathf.PingPong(currentAlpha + Time.deltaTime * flashingFrequency, maxFlashingAlpha);
        currentAlpha = newAlpha;
        foreach (var roomView in flashingRooms)
        {
            roomView.SetAlpha(currentAlpha + 0.2f);
        }
        
        var horizontalInput = Input.GetAxis("Horizontal");
        var verticalInput = Input.GetAxis("Vertical");
        if (horizontalInput > Mathf.Epsilon || horizontalInput < -Mathf.Epsilon)
        {
            var direction = horizontalInput > 0 ? Direction.East : Direction.West;
            if (currentPlayerRoom.Connections.TryGetValue(direction, out var nextRoom))
            {
                MoveSelection(nextRoom.Position);
            }
        }
        else if (verticalInput > Mathf.Epsilon || verticalInput < -Mathf.Epsilon)
        {
            var direction = verticalInput > 0 ? Direction.North : Direction.South;
            if (currentPlayerRoom.Connections.TryGetValue(direction, out var nextRoom))
            {
                MoveSelection(nextRoom.Position);
            }
        }

        if (Input.GetKeyDown(KeyCode.Space) && doctorOfficeAvailable)
        {
            var room = new Room(Vector2Int.zero);
            room.Type = RoomType.DoctorOffice;
            SelectRoom(room);
        }

        if (Input.GetKeyDown(KeyCode.Return))
        {
            SelectRoom(currentlySelectedRoom.Room);
        }
    }

    void SelectRoom(Room room)
    {
        foreach (var flashingRoom in flashingRooms)
        {
            flashingRoom.SetAlpha(1);
            if (!flashingRoom.Room.Visited)
            {
                flashingRoom.Hide();
            }
        }
        flashingRooms.Clear();
        currentlySelectedRoom = null;
        roomSelection.SetActive(false);
        
        RoomSelected?.Invoke(room);
    }

    void MoveSelection(Vector2Int selectedRoomPosition)
    {
        if (roomObjects.TryGetValue(selectedRoomPosition, out var roomView))
        {
            currentlySelectedRoom = roomView;
            roomSelection.transform.position = roomView.transform.position;
            if (!roomSelection.activeSelf)
            {
                roomSelection.SetActive(true);
            }
        }
        else
        {
            roomSelection.SetActive(false);
        }
    }
}