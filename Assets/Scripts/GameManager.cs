using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] Logger logger;
    [SerializeField] LevelGenerator levelGenerator;
    [SerializeField] LevelVisualizer levelVisualizer;
    [SerializeField] LLMManager llmManager;
    [SerializeField] ContentGenerator contentGenerator;

    [Header("Game Data")]
    [SerializeField] bool useDefaultData;
    [SerializeField] string[] defaultLevelThemes;
    [SerializeField] string[] defaultLevelTones;
    
    GameSession session = new ();

    IEnumerator Start()
    {
        levelGenerator.GenerateAllLevels();
        
        yield return CreateSetting();
        yield return CreateLevel(session.CurrentLevel);
        
        session.CurrentRoomType = RoomType.Entrance;
        var room = levelVisualizer.GetCurrentPlayerRoom();
        yield return CreateRoom(room, session.CurrentLevel, session.RoomsCleared, session.Setting.levels[0], session.CurrentRoomType);
        
        levelVisualizer.VisualizeCurrentLevel();
    }

    IEnumerator CreateSetting()
    {
        var settingChat = new LLMChat("Setting", LLMProvider.Gemini, ModelType.Flash);
        var settingTask = contentGenerator.GenerateGameSetting(settingChat);
        yield return WaitForTask(settingTask);
        session.Setting = new GameSetting { fullSetting = settingTask.Result };
        if (!useDefaultData)
        {
            settingChat.ConvertToLocal();
            for (int i = 0; i < 5; i++)
            {
                var task = llmManager.SendPromptToLLM($"Answer with 1 sentence. Level {i + 1} theme:", settingChat);
                yield return WaitForTask(task);
                var levelTheme = task.Result;
                task = llmManager.SendPromptToLLM($"Answer with 1 sentence. Level {i + 1} tone:", settingChat);
                yield return WaitForTask(task);
                var levelTone = task.Result;
                session.Setting.levels[i] = new LevelSetting
                {
                    theme = levelTheme,
                    tone = levelTone
                };
            }
        }
        else
        {
            session.Setting.levels = new LevelSetting[defaultLevelThemes.Length];
            for (int i = 0; i < defaultLevelThemes.Length; i++)
            {
                session.Setting.levels[i] = new LevelSetting
                {
                    theme = defaultLevelThemes[i],
                    tone = defaultLevelTones[i]
                };
            }
        }
        logger.LogExtra("<b>Setting</b>\n" + session.Setting.Print());
        
        var settingSummaryChat = new LLMChat("Setting Summary", LLMProvider.LocalLLM, ModelType.Flash);
        var settingSummaryTask = contentGenerator.GenerateSettingSummary(session.Setting.fullSetting, settingSummaryChat);
        yield return WaitForTask(settingSummaryTask);
        session.SettingSummary = settingSummaryTask.Result;
        session.Setting.briefSetting = session.SettingSummary;
        logger.LogExtra("<b>Setting Summary</b>\n" + session.SettingSummary);
    }

    IEnumerator CreateLevel(int index)
    {
        var levelChat = new LLMChat("Level Description", LLMProvider.Gemini, ModelType.Flash);
        var levelSetting = session.Setting.levels[index - 1];
        var levelTask = index == 0
            ? contentGenerator.GenerateFirstLevelDescription(session.SettingSummary, levelSetting, levelChat)
            : contentGenerator.GenerateLevelDescription(session.SettingSummary, levelSetting, levelChat);
        yield return WaitForTask(levelTask);
        session.CurrentLevelDescription = levelTask.Result;
        logger.LogExtra($"<b>Level {index} Description</b>\n{session.CurrentLevelDescription}");
        
        var levelSummaryChat = new LLMChat("Level Description Brief", LLMProvider.LocalLLM, ModelType.Flash);
        var levelSummaryTask = contentGenerator.GenerateLevelBrief(session.CurrentLevelDescription, levelSummaryChat);
        yield return WaitForTask(levelSummaryTask);
        session.CurrentLevelSummary = levelSummaryTask.Result;
        logger.LogExtra($"<b>Level {index} Description Brief</b>\n{session.CurrentLevelSummary}");
    }

    IEnumerator CreateRoom(Room room, int currentLevel, int clearedRooms, LevelSetting levelSetting,
        RoomType roomType)
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
        room.Visited = true;
        logger.LogExtra($"<b>Room {currentLevel}_{room.Position.x}.{room.Position.y} Description</b>\n{session.CurrentRoomDescription}");
    }

    IEnumerator WaitForTask(Task<string> task)
    {
        while (!task.IsCompleted)
        {
            yield return null;
        }
    }
}