using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Level
{
    // Grid dimensions
    private int width;
    private int height;
    
    // Rooms in this level
    private Dictionary<Vector2Int, Room> rooms = new Dictionary<Vector2Int, Room>();
    
    // Special rooms
    private Room entranceRoom;
    private Room exitRoom;
    
    // Level index
    public int LevelIndex { get; private set; }
    
    public Level(int index, int roomCount, int width, int height)
    {
        LevelIndex = index;
        this.width = width;
        this.height = height;
        
        GenerateLevel(roomCount);
    }
    
    private void GenerateLevel(int roomCount)
    {
        // Clear any existing rooms
        rooms.Clear();
        
        // Create and place the entrance room randomly
        Vector2Int entrancePos = new Vector2Int(Random.Range(0, width), Random.Range(0, height));
        entranceRoom = CreateRoom(entrancePos);
        entranceRoom.Type = RoomType.Entrance;
        
        // Use a modified depth-first search algorithm to create a maze-like structure
        Stack<Room> roomStack = new Stack<Room>();
        roomStack.Push(entranceRoom);
        entranceRoom.Visited = true;
        
        int createdRooms = 1; // Start with 1 for the entrance room
        
        while (roomStack.Count > 0 && createdRooms < roomCount)
        {
            Room currentRoom = roomStack.Peek();
            
            // Get unvisited neighbors
            List<Direction> availableDirections = GetAvailableDirections(currentRoom.Position);
            
            if (availableDirections.Count > 0)
            {
                // Choose a random direction
                Direction direction = availableDirections[Random.Range(0, availableDirections.Count)];
                Vector2Int newPosition = currentRoom.Position + direction.GetOffset();
                
                // Create a new room
                Room newRoom = CreateRoom(newPosition);
                newRoom.Visited = true;
                
                // Connect the rooms
                ConnectRooms(currentRoom, newRoom, direction);
                
                // Add to stack for next iteration
                roomStack.Push(newRoom);
                createdRooms++;
            }
            else
            {
                // Backtrack
                roomStack.Pop();
            }
        }
        
        // Create the exit room far from entrance
        exitRoom = FindFarthestRoom(entranceRoom);
        exitRoom.Type = RoomType.Exit;
        
        // Final pass to add some loops to make it more interesting
        AddRandomConnections(roomCount / 10);
    }
    
    private List<Direction> GetAvailableDirections(Vector2Int position)
    {
        List<Direction> availableDirections = new List<Direction>();
        
        foreach (Direction dir in System.Enum.GetValues(typeof(Direction)))
        {
            Vector2Int neighborPos = position + dir.GetOffset();
            
            // Check if the position is within bounds and not already occupied
            if (IsWithinBounds(neighborPos) && !rooms.ContainsKey(neighborPos))
            {
                availableDirections.Add(dir);
            }
        }
        
        return availableDirections;
    }
    
    private bool IsWithinBounds(Vector2Int position)
    {
        return position.x >= 0 && position.x < width && 
               position.y >= 0 && position.y < height;
    }
    
    private Room CreateRoom(Vector2Int position)
    {
        Room room = new Room(position);
        rooms[position] = room;
        return room;
    }
    
    private void ConnectRooms(Room room1, Room room2, Direction direction)
    {
        room1.AddConnection(direction, room2);
        room2.AddConnection(direction.GetOpposite(), room1);
    }
    
    private Room FindFarthestRoom(Room startRoom)
    {
        // Reset visited status for all rooms
        foreach (Room room in rooms.Values)
        {
            room.Visited = false;
        }
        
        // Use BFS to find the farthest room
        Queue<Room> queue = new Queue<Room>();
        Dictionary<Room, int> distances = new Dictionary<Room, int>();
        
        queue.Enqueue(startRoom);
        startRoom.Visited = true;
        distances[startRoom] = 0;
        
        Room farthestRoom = startRoom;
        int maxDistance = 0;
        
        while (queue.Count > 0)
        {
            Room current = queue.Dequeue();
            int currentDistance = distances[current];
            
            // Check if this is the farthest room so far
            if (currentDistance > maxDistance)
            {
                maxDistance = currentDistance;
                farthestRoom = current;
            }
            
            // Visit all connected rooms
            foreach (KeyValuePair<Direction, Room> connection in current.Connections)
            {
                Room neighbor = connection.Value;
                if (!neighbor.Visited)
                {
                    neighbor.Visited = true;
                    queue.Enqueue(neighbor);
                    distances[neighbor] = currentDistance + 1;
                }
            }
        }
        
        // Make sure the exit is not too close to entrance (at least 50% of the total room count away)
        int minimumDistance = rooms.Count / 2;
        if (maxDistance < minimumDistance)
        {
            // If not enough rooms, just return the farthest one we found
            Debug.LogWarning("Could not find a room far enough away for the exit. Using the farthest available.");
        }
        
        return farthestRoom;
    }
    
    private void AddRandomConnections(int connectionCount)
    {
        // Add a few random connections to create loops
        for (int i = 0; i < connectionCount; i++)
        {
            // Get random room
            Room room = rooms.Values.ElementAt(Random.Range(0, rooms.Count));
            
            // Try to find a neighboring position that has a room but is not connected
            List<Direction> potentialDirections = new List<Direction>();
            
            foreach (Direction dir in System.Enum.GetValues(typeof(Direction)))
            {
                Vector2Int neighborPos = room.Position + dir.GetOffset();
                
                // Check if there's a room there and not already connected
                if (IsWithinBounds(neighborPos) && 
                    rooms.ContainsKey(neighborPos) && 
                    !room.HasConnection(dir))
                {
                    potentialDirections.Add(dir);
                }
            }
            
            // If there's at least one potential connection
            if (potentialDirections.Count > 0)
            {
                Direction randomDir = potentialDirections[Random.Range(0, potentialDirections.Count)];
                Room neighborRoom = rooms[room.Position + randomDir.GetOffset()];
                
                // Don't add too many connections to a single room
                if (room.GetConnectionCount() < 4 && neighborRoom.GetConnectionCount() < 4)
                {
                    ConnectRooms(room, neighborRoom, randomDir);
                }
            }
        }
    }
    
    public Room GetEntranceRoom()
    {
        return entranceRoom;
    }
    
    public Room GetExitRoom()
    {
        return exitRoom;
    }
    
    public IEnumerable<Room> GetAllRooms()
    {
        return rooms.Values;
    }
    
    public int GetRoomCount()
    {
        return rooms.Count;
    }
    
    public bool IsLevelCompletable()
    {
        // Reset visited status for all rooms
        foreach (Room room in rooms.Values)
        {
            room.Visited = false;
        }
        
        // Use BFS to check if there's a path from entrance to exit
        Queue<Room> queue = new Queue<Room>();
        queue.Enqueue(entranceRoom);
        entranceRoom.Visited = true;
        
        while (queue.Count > 0)
        {
            Room current = queue.Dequeue();
            
            if (current == exitRoom)
            {
                return true;
            }
            
            foreach (KeyValuePair<Direction, Room> connection in current.Connections)
            {
                Room neighbor = connection.Value;
                if (!neighbor.Visited)
                {
                    neighbor.Visited = true;
                    queue.Enqueue(neighbor);
                }
            }
        }
        
        return false;
    }
}