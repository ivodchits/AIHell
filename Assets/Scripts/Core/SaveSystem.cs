using UnityEngine;
using System.IO;
using System;
using System.Linq;
using System.Collections.Generic;
using AIHell.Core.Data;

[System.Serializable]
public class GameSaveData
{
    public int currentLevel;
    public PlayerProfileData profileData;
    public Dictionary<string, bool> globalFlags;
    public List<string> unlockedAchievements;
    public DateTime lastPlayTime;
    public float totalPlayTime;
    public int deathCount;
    public float averageFearLevel;

    public GameSaveData()
    {
        profileData = new PlayerProfileData();
        globalFlags = new Dictionary<string, bool>();
        unlockedAchievements = new List<string>();
        lastPlayTime = DateTime.Now;
        totalPlayTime = 0f;
        deathCount = 0;
        averageFearLevel = 0f;
    }
}

[System.Serializable]
public class PlayerProfileData
{
    public float aggressionLevel;
    public float curiosityLevel;
    public float fearLevel;
    public float obsessionLevel;
    public Dictionary<string, int> choiceFrequencies;
    public List<string> recentChoices;
    public Dictionary<string, float> traitTrends;
    public List<string> commonObsessions;

    public PlayerProfileData()
    {
        choiceFrequencies = new Dictionary<string, int>();
        recentChoices = new List<string>();
        traitTrends = new Dictionary<string, float>();
        commonObsessions = new List<string>();
    }
}

public class SaveSystem : MonoBehaviour
{
    private const string SAVE_FILE_NAME = "AIHell_SaveData.json";
    private const string SAVE_FOLDER = "SaveData";
    private const int AUTO_SAVE_INTERVAL = 300; // 5 minutes in seconds
    private float timeSinceLastSave;

    private void Start()
    {
        CreateSaveDirectory();
        LoadGame();
        StartAutoSave();
    }

    private void Update()
    {
        timeSinceLastSave += Time.deltaTime;
        if (timeSinceLastSave >= AUTO_SAVE_INTERVAL)
        {
            SaveGame();
            timeSinceLastSave = 0f;
        }
    }

    private void CreateSaveDirectory()
    {
        string dir = Path.Combine(Application.persistentDataPath, SAVE_FOLDER);
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
    }

    public void SaveGame()
    {
        GameSaveData saveData = CreateSaveData();
        string json = JsonUtility.ToJson(saveData, true);
        string filePath = GetSaveFilePath();

        try
        {
            File.WriteAllText(filePath, json);
            Debug.Log("Game saved successfully");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error saving game: {e.Message}");
        }
    }

    public void LoadGame()
    {
        string filePath = GetSaveFilePath();
        if (!File.Exists(filePath))
        {
            Debug.Log("No save file found. Starting new game.");
            return;
        }

        try
        {
            string json = File.ReadAllText(filePath);
            GameSaveData saveData = JsonUtility.FromJson<GameSaveData>(json);
            ApplySaveData(saveData);
            Debug.Log("Game loaded successfully");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error loading game: {e.Message}");
        }
    }

    private GameSaveData CreateSaveData()
    {
        GameSaveData saveData = new GameSaveData();
        
        var gameState = GameManager.Instance.StateManager.GetCurrentState();
        saveData.currentLevel = (int)gameState.dynamicVariables["currentLevel"];
        
        // Save psychological profile
        var profile = GameManager.Instance.ProfileManager.CurrentProfile;
        saveData.profileData = new PlayerProfileData
        {
            aggressionLevel = profile.AggressionLevel,
            curiosityLevel = profile.CuriosityLevel,
            fearLevel = profile.FearLevel,
            obsessionLevel = profile.ObsessionLevel,
            choiceFrequencies = profile.GetChoiceFrequencies(),
            recentChoices = new List<string>(GameManager.Instance.ProfileManager.GetRecentChoices()),
            commonObsessions = ExtractCommonObsessions(profile)
        };

        // Save global flags and stats
        saveData.globalFlags = new Dictionary<string, bool>();
        foreach (var variable in gameState.dynamicVariables)
        {
            if (variable.Value is bool)
            {
                saveData.globalFlags[variable.Key] = (bool)variable.Value;
            }
        }

        saveData.lastPlayTime = DateTime.Now;
        saveData.totalPlayTime += Time.time;
        saveData.deathCount = (int)gameState.dynamicVariables["deathCount"];
        
        // Calculate average fear level
        float totalFear = (float)gameState.dynamicVariables["totalFearLevel"];
        int fearReadings = (int)gameState.dynamicVariables["fearReadings"];
        saveData.averageFearLevel = fearReadings > 0 ? totalFear / fearReadings : 0f;

        return saveData;
    }

    private void ApplySaveData(GameSaveData saveData)
    {
        var gameState = GameManager.Instance.StateManager.GetCurrentState();
        gameState.dynamicVariables["currentLevel"] = saveData.currentLevel;
        
        // Restore psychological profile
        var profile = GameManager.Instance.ProfileManager.CurrentProfile;
        profile.AggressionLevel = saveData.profileData.aggressionLevel;
        profile.CuriosityLevel = saveData.profileData.curiosityLevel;
        profile.FearLevel = saveData.profileData.fearLevel;
        profile.ObsessionLevel = saveData.profileData.obsessionLevel;

        // Restore choice history
        foreach (var choice in saveData.profileData.choiceFrequencies)
        {
            for (int i = 0; i < choice.Value; i++)
            {
                GameManager.Instance.ProfileManager.TrackChoice(choice.Key, "");
            }
        }

        // Restore global flags
        foreach (var flag in saveData.globalFlags)
        {
            gameState.dynamicVariables[flag.Key] = flag.Value;
        }

        // Update play time statistics
        gameState.dynamicVariables["totalPlayTime"] = Mathf.RoundToInt(saveData.totalPlayTime);
        gameState.dynamicVariables["deathCount"] = saveData.deathCount;
    }

    private List<string> ExtractCommonObsessions(PlayerAnalysisProfile profile)
    {
        var obsessions = profile.GetActiveObsessions();
        return Array.ConvertAll(obsessions, o => o.Keyword).ToList();
    }

    private string GetSaveFilePath()
    {
        return Path.Combine(Application.persistentDataPath, SAVE_FOLDER, SAVE_FILE_NAME);
    }

    private void StartAutoSave()
    {
        InvokeRepeating(nameof(SaveGame), AUTO_SAVE_INTERVAL, AUTO_SAVE_INTERVAL);
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            SaveGame();
        }
    }

    private void OnApplicationQuit()
    {
        SaveGame();
    }
}