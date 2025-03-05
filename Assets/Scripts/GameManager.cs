using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] Logger logger;
    [SerializeField] LevelGenerator levelGenerator;
    [SerializeField] LevelVisualizer levelVisualizer;

    [Header("Game Data")]
    [SerializeField] bool useDefaultData;
    [SerializeField] string[] defaultLevelThemes;
    [SerializeField] string[] defaultLevelTones;
    
    LLMManager llmManager => LLMManager.Instance;
    ContentGenerator contentGenerator => ContentGenerator.Instance;
    
    GameSession session = new ();

    IEnumerator Start()
    {
        levelGenerator.GenerateAllLevels();
        
        yield return CreateSetting();
        yield return CreateLevel(session.CurrentLevel);
        
        session.CurrentRoomType = RoomType.Entrance;
        yield return CreateRoom(session.CurrentLevel, session.RoomsCleared, session.Setting.levels[0], session.CurrentRoomType);
        
        levelVisualizer.VisualizeCurrentLevel();
    }

    private IEnumerator CreateSetting()
    {
        var settingChat = new LLMChat("Setting", LLMProvider.Gemini, ModelType.Flash);
        var settingTask = contentGenerator.GenerateGameSetting(settingChat);
        yield return WaitForTask(settingTask);
        try
        {
            session.Setting = JsonUtility.FromJson<GameSetting>(settingTask.Result);
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            session.Setting = new GameSetting { full_setting = settingTask.Result };
        }
        if (session.Setting.levels == null || useDefaultData)
        {
            session.Setting.levels = new LevelSetting[defaultLevelThemes.Length];
            for (int i = 0; i < defaultLevelThemes.Length; i++)
            {
                session.Setting.levels[i] = new LevelSetting
                {
                    level_theme = defaultLevelThemes[i],
                    level_tone = defaultLevelTones[i]
                };
            }
        }
        logger.LogExtra("Setting:\n" + session.Setting.Print());
        
        var settingSummaryChat = new LLMChat("Setting Summary", LLMProvider.LocalLLM, ModelType.Flash);
        var settingSummaryTask = contentGenerator.GenerateSettingSummary(session.Setting.full_setting, settingSummaryChat);
        yield return WaitForTask(settingSummaryTask);
        session.SettingSummary = settingSummaryTask.Result;
        logger.LogExtra("Setting Summary:\n" + session.SettingSummary);
    }

    private IEnumerator CreateLevel(int index)
    {
        var levelChat = new LLMChat("Level Description", LLMProvider.Gemini, ModelType.Flash);
        var levelSetting = session.Setting.levels[index];
        var levelTask = index == 0
            ? contentGenerator.GenerateFirstLevelDescription(session.SettingSummary, levelSetting, levelChat)
            : contentGenerator.GenerateLevelDescription(session.SettingSummary, levelSetting, levelChat);
        yield return WaitForTask(levelTask);
        session.CurrentLevelDescription = levelTask.Result;
        logger.LogExtra($"Level {index + 1} Description:\n{session.CurrentLevelDescription}");
        
        var levelSummaryChat = new LLMChat("Level Description Brief", LLMProvider.LocalLLM, ModelType.Flash);
        var levelSummaryTask = contentGenerator.GenerateLevelBrief(session.CurrentLevelDescription, levelSummaryChat);
        yield return WaitForTask(levelSummaryTask);
        session.CurrentLevelSummary = levelSummaryTask.Result;
        logger.LogExtra($"Level {index + 1} Description Brief:\n{session.CurrentLevelSummary}");
    }

    private IEnumerator CreateRoom(int currentLevel, int clearedRooms, LevelSetting levelSetting, RoomType roomType)
    {
        var roomChat = new LLMChat("Room Description", LLMProvider.Gemini, ModelType.Flash);

        var roomTask = clearedRooms == 0
            ? contentGenerator.GenerateFirstRoomDescription(currentLevel, session.CurrentLevelDescription, levelSetting, roomChat)
            : roomType == RoomType.Standard
                ? contentGenerator.GenerateRoomDescription(currentLevel, session.CurrentLevelDescription, levelSetting, roomChat)
                : roomType == RoomType.Exit
                    ? contentGenerator.GenerateExitRoomDescription(null, null, roomChat)
                    : contentGenerator.GenerateDoctorInteraction(null, null, roomChat);
        yield return WaitForTask(roomTask);
        session.CurrentRoomDescription = roomTask.Result;
        logger.LogExtra($"Room {currentLevel + 1}_{clearedRooms} Description:\n{session.CurrentRoomDescription}");
    }

    private IEnumerator WaitForTask(Task<string> task)
    {
        while (!task.IsCompleted)
        {
            yield return null;
        }
    }
}