using System.Collections;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AIHell.UI;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] Logger logger;
    [SerializeField] LevelGenerator levelGenerator;
    [SerializeField] LevelVisualizer levelVisualizer;
    [SerializeField] LLMManager llmManager;
    [SerializeField] ContentGenerator contentGenerator;
    [SerializeField] ImageGenerator imageGenerator;

    [Header("UI")]
    [SerializeField] GameUIController mainUI;
    [SerializeField] LoadingScreenController loadingScreen;

    [Header("Game Data")]
    [SerializeField] bool useDefaultData;
    [SerializeField] string[] defaultLevelThemes;
    [SerializeField] string[] defaultLevelTones;
    
    GameSession session = new ();

    IEnumerator Start()
    {
        mainUI.Hide();
        loadingScreen.Show(0);
        
        levelGenerator.GenerateAllLevels();
        loadingScreen.Show(5);
        
        yield return CreateSetting(5);
        yield return CreateLevel(session.CurrentLevel);
        loadingScreen.Show(52);
        
        session.CurrentRoomType = RoomType.Entrance;
        var room = levelGenerator.GetCurrentLevel().CurrentRoom;
        yield return CreateRoom(room, session.CurrentLevel, session.RoomsCleared, session.Setting.levels[0], session.CurrentRoomType);
        loadingScreen.Show(64);
        
        logger.Log($"<h1>Level 1</h1><br><h2>Room {session.RoomsCleared}</h2>");
        
        yield return CreateImagePrompt();
        loadingScreen.Show(79);
        
        yield return CreateImage();
        loadingScreen.Show(86);
        
        var gameFlowChat = new LLMChat("Game Flow", LLMProvider.Gemini, ModelType.Flash);
        yield return CreateIntroduction(gameFlowChat);
        loadingScreen.Show(99);
        
        levelVisualizer.VisualizeCurrentLevel();
        loadingScreen.Show(100);
        
        yield return new WaitForSeconds(1);

        if (loadingScreen.ShowingText)
        {
            while(loadingScreen.ShowingText)
            {
                yield return null;
            }
        }

        loadingScreen.Hide();
        
        mainUI.PlayShowingAnimation(gameFlowChat, skipFirstMessage: true);
    }

    IEnumerator CreateSetting(int loadingPercent)
    {
        var settingChat = new LLMChat("Setting", LLMProvider.Gemini, ModelType.Flash);
        var settingTask = contentGenerator.GenerateGameSetting(settingChat);
        yield return WaitForTask(settingTask);
        loadingPercent += 2;
        loadingScreen.Show(loadingPercent);
        session.Setting = new GameSetting { fullSetting = settingTask.Result };
        session.Setting.levels = new LevelSetting[defaultLevelThemes.Length];
        
        var settingForThePlayerTask = llmManager.SendPromptToLLM("Now create a philosophical text about this setting. First tell a little about the setting and the established world, and then vaguely talk about the philosophical concepts of it. Make it around 4 paragraphs. Don't mention Dr. Mire and make it interesting to read. Reply with just requested text.", settingChat);
        yield return WaitForTask(settingForThePlayerTask);
        session.Setting.forThePlayer = settingForThePlayerTask.Result;
        loadingPercent += 2;
        loadingScreen.Show(session.Setting.forThePlayer, loadingPercent);
        
        if (!useDefaultData)
        {
            var themesChat = new LLMChat("Setting", LLMProvider.LocalLLM, ModelType.Flash);
            themesChat.AddEntry(new ChatEntry(isUser: false, session.Setting.fullSetting));
            for (int i = 0; i < 5; i++)
            {
                var task = llmManager.SendPromptToLLM($"Generate a level theme. It can be an emotion, a trauma or something else. Answer with just the theme for the requested level, with no more than 6 words. Level {i + 1} theme is:", themesChat);
                yield return WaitForTask(task);
                var levelTheme = task.Result;
                loadingPercent += 2;
                loadingScreen.Show(loadingPercent);
                task = llmManager.SendPromptToLLM($"Generate a level tone. Level 1 should have the lightest tone, while level 5 - the darkest. Answer with just the tone for the requested level, with no more than 6 words. Level {i + 1} tone is:", themesChat);
                yield return WaitForTask(task);
                var levelTone = task.Result;
                session.Setting.levels[i] = new LevelSetting
                {
                    theme = levelTheme,
                    tone = levelTone
                };
                loadingPercent += 2;
                loadingScreen.Show(loadingPercent);
            }
        }
        else
        {
            for (int i = 0; i < defaultLevelThemes.Length; i++)
            {
                session.Setting.levels[i] = new LevelSetting
                {
                    theme = defaultLevelThemes[i],
                    tone = defaultLevelTones[i]
                };
            }
        }
        logger.LogExtra("Setting\n" + session.Setting.Print());
        
        var settingSummaryChat = new LLMChat("Setting Summary", LLMProvider.LocalLLM, ModelType.Flash);
        var settingSummaryTask = contentGenerator.GenerateSettingSummary(session.Setting.fullSetting, settingSummaryChat);
        yield return WaitForTask(settingSummaryTask);
        session.SettingSummary = settingSummaryTask.Result;
        session.Setting.briefSetting = session.SettingSummary;
        logger.LogExtra("Setting Summary\n" + session.SettingSummary);
    }

    IEnumerator CreateLevel(int index)
    {
        var levelChat = new LLMChat("Level Description", LLMProvider.Gemini, ModelType.Flash);
        var levelSetting = session.Setting.levels[index - 1];
        var levelTask = index == 1
            ? contentGenerator.GenerateFirstLevelDescription(session.SettingSummary, levelSetting, levelChat)
            : contentGenerator.GenerateLevelDescription(session.SettingSummary, levelSetting, levelChat);
        yield return WaitForTask(levelTask);
        session.CurrentLevelDescription = levelTask.Result;
        logger.LogExtra($"Level {index} Description\n{session.CurrentLevelDescription}");
        
        var levelSummaryChat = new LLMChat("Level Description Brief", LLMProvider.LocalLLM, ModelType.Flash);
        var levelSummaryTask = contentGenerator.GenerateLevelBrief(session.CurrentLevelDescription, levelSummaryChat);
        yield return WaitForTask(levelSummaryTask);
        session.CurrentLevelSummary = levelSummaryTask.Result;
        logger.LogExtra($"Level {index} Description Brief\n{session.CurrentLevelSummary}");
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
        logger.LogExtra($"Room {currentLevel}_{room.Position.x}.{room.Position.y} Description\n{session.CurrentRoomDescription}");
    }
    
    IEnumerator CreateIntroduction(LLMChat chat)
    {
        var introTask = contentGenerator.GenerateGameIntroduction(session.SettingSummary, session.CurrentLevelSummary, session.CurrentRoomDescription, chat);
        yield return WaitForTask(introTask);
    }
    
    IEnumerator CreateImagePrompt()
    {
        var chat = new LLMChat("Room Image Prompt", LLMProvider.Gemini, ModelType.Flash);
        var imagePromptTask = contentGenerator.GenerateRoomImagePrompt(session.CurrentRoomDescription, session.CurrentLevel, chat);
        yield return WaitForTask(imagePromptTask);
        session.CurrentRoomImagePrompt = RemoveAllSpecialCharacters(imagePromptTask.Result);
        logger.LogExtra($"Room Image Prompt\n{session.CurrentRoomImagePrompt}");
    }

    IEnumerator CreateImage()
    {
        var imageGenerationTask = imageGenerator.GenerateImage(session.CurrentRoomImagePrompt);
        yield return WaitForTask(imageGenerationTask);
        var image = imageGenerationTask.Result;
        mainUI.SetTexture(image);
        logger.LogImage(image);
    }

    IEnumerator WaitForTask(Task<string> task)
    {
        while (!task.IsCompleted)
        {
            yield return null;
        }
    }

    IEnumerator WaitForTask(Task<Texture2D> task)
    {
        while (!task.IsCompleted)
        {
            yield return null;
        }
    }

    string RemoveAllSpecialCharacters(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }
        
        // Remove escape sequences and special characters
        return Regex.Replace(input, @"[^\w\s,.]", "");
    }
}