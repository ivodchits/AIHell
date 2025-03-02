using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelGenerator : MonoBehaviour
{
    [Header("Level Generation Settings")]
    [SerializeField] private int[] roomsPerLevel = new int[] { 20, 30, 40, 50, 60 }; // Default room counts per level
    [SerializeField] private int maxLevelWidth = 20;
    [SerializeField] private int maxLevelHeight = 20;
    
    // List of generated levels
    private List<Level> levels = new List<Level>();
    
    // Current level index
    private int currentLevelIndex = 0;
    
    private void Awake()
    {
        GenerateAllLevels();
    }
    
    public void GenerateAllLevels()
    {
        levels.Clear();
        
        // Generate each level
        for (int i = 0; i < 5; i++)
        {
            int roomCount = roomsPerLevel[i];
            
            // Determine level size based on room count
            int levelWidth = Mathf.Min(maxLevelWidth, Mathf.CeilToInt(Mathf.Sqrt(roomCount * 2)));
            int levelHeight = Mathf.Min(maxLevelHeight, Mathf.CeilToInt(Mathf.Sqrt(roomCount * 2)));
            
            // Create a new level
            Level level = new Level(i, roomCount, levelWidth, levelHeight);
            
            // Validate that the level is completable
            if (!level.IsLevelCompletable())
            {
                Debug.LogWarning($"Level {i} was not completable, regenerating...");
                i--; // Try again with this level
                continue;
            }
            
            levels.Add(level);
            
            Debug.Log($"Generated Level {i} with {level.GetRoomCount()} rooms");
        }
    }
    
    // Get the current level
    public Level GetCurrentLevel()
    {
        if (currentLevelIndex >= 0 && currentLevelIndex < levels.Count)
        {
            return levels[currentLevelIndex];
        }
        return null;
    }
    
    // Move to the next level
    public Level AdvanceToNextLevel()
    {
        if (currentLevelIndex < levels.Count - 1)
        {
            currentLevelIndex++;
            return GetCurrentLevel();
        }
        return null; // No more levels
    }
    
    // Set a specific level as current
    public Level SetCurrentLevel(int levelIndex)
    {
        if (levelIndex >= 0 && levelIndex < levels.Count)
        {
            currentLevelIndex = levelIndex;
            return GetCurrentLevel();
        }
        return null;
    }
    
    // Adjust room count for a specific level
    public void SetRoomsForLevel(int levelIndex, int roomCount)
    {
        if (levelIndex >= 0 && levelIndex < roomsPerLevel.Length)
        {
            roomsPerLevel[levelIndex] = Mathf.Max(10, roomCount); // Ensure at least 10 rooms
        }
    }
    
    // Get all generated levels
    public List<Level> GetAllLevels()
    {
        return levels;
    }
    
    // Regenerate a specific level
    public Level RegenerateLevel(int levelIndex)
    {
        if (levelIndex >= 0 && levelIndex < levels.Count)
        {
            int roomCount = roomsPerLevel[levelIndex];
            int levelWidth = Mathf.Min(maxLevelWidth, Mathf.CeilToInt(Mathf.Sqrt(roomCount * 2)));
            int levelHeight = Mathf.Min(maxLevelHeight, Mathf.CeilToInt(Mathf.Sqrt(roomCount * 2)));
            
            Level level = new Level(levelIndex, roomCount, levelWidth, levelHeight);
            
            // Validate that the level is completable
            if (!level.IsLevelCompletable())
            {
                Debug.LogWarning($"Regenerated Level {levelIndex} was not completable, attempting again...");
                return RegenerateLevel(levelIndex);
            }
            
            levels[levelIndex] = level;
            return level;
        }
        return null;
    }
}