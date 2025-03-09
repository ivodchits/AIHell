using System;
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
    LLMChat gameFlowChat;

    IEnumerator Start()
    {
        mainUI.Hide();
        loadingScreen.Show(0);
        
        levelGenerator.GenerateAllLevels();
        loadingScreen.Show(5);
        
        session.CurrentGameFlowState = GameFlowState.SettingCreation;
        yield return CreateSetting(5);
        session.CurrentGameFlowState = GameFlowState.LevelGeneration;
        yield return CreateLevel(session.CurrentLevel);
        loadingScreen.Show(52);
        
        session.SetCurrentRoomType(RoomType.Entrance);
        var room = levelGenerator.GetCurrentLevel().CurrentRoom;
        session.CurrentGameFlowState = GameFlowState.RoomGeneration;
        yield return CreateRoom(room, session.CurrentLevel, session.Setting.levels[0]);
        loadingScreen.Show(64);
        
        logger.Log($"Level 1. Room {session.RoomsCleared}.");
        
        session.CurrentGameFlowState = GameFlowState.RoomImageGeneration;
        yield return CreateImagePrompt();
        loadingScreen.Show(79);
        
        yield return CreateImage();
        loadingScreen.Show(86);
        
        session.CurrentGameFlowState = GameFlowState.GameFlow;
        gameFlowChat = new LLMChat("Game Flow", LLMProvider.Gemini, ModelType.Flash);
        yield return CreateIntroduction(gameFlowChat);
        loadingScreen.Show(99);
        
        levelVisualizer.VisualizeCurrentLevel();
        loadingScreen.Show(100);
        loadingScreen.OnPressedToContinue += StartGame;
    }

    void StartGame()
    {
        llmManager.ActivateRequestLock();
        loadingScreen.Hide();
        
        var gameFlowStartTemplate = contentGenerator.GetGameFlowStartMessage(session.SettingSummary, session.CurrentRoomDescription);
        var gameFlowStartChat = new ChatEntry(isUser: true, gameFlowStartTemplate);
        gameFlowChat = CreateGameFlowChat(gameFlowStartChat, gameFlowChat.ChatHistory[1]);
        mainUI.PlayShowingAnimation(gameFlowChat, skipFirstMessage: true);

        mainUI.OnUserInput += OnUserInput;
    }

    void OnUserInput(string input)
    {
        var escapedInput = llmManager.EscapeJsonString(input);
        var chatEntry = new ChatEntry(isUser: true, escapedInput);
        var task = llmManager.SendPromptToLLM(chatEntry, gameFlowChat);
        StartCoroutine(WaitForTask(task, OnGameFlowResponse));
        mainUI.AddMessage(chatEntry);
        logger.Log($"-You: {escapedInput}");
    }

    void StartGameFlow(string firstModelMessage)
    {
        loadingScreen.Hide();
        mainUI.PlayShowingAnimation(gameFlowChat, skipFirstMessage: true);
    }
    
    void OnGameFlowResponse(string response)
    {
        if (string.IsNullOrEmpty(response))
        {
            Debug.LogError("Game flow response is empty");
            return;
        }
        logger.Log(response);

        var processedResponse = contentGenerator.ProcessGameFlowResponse(response, session);
        var responseChatEntry = new ChatEntry(isUser: false, processedResponse);
        mainUI.AddMessage(responseChatEntry);

        if (session.CurrentGameFlowState == GameFlowState.GameOver)
        {
            GameOver();
            return;
        }

        if (session.CurrentGameFlowState == GameFlowState.RoomSummarization)
        {
            mainUI.EnterPressToContinueState();
            var roomSummaryChat = new LLMChat("Room Summary", LLMProvider.LocalLLM, ModelType.Flash);
            if (session.CurrentRoomType == RoomType.DoctorOffice)
            {
                mainUI.OnPressedToContinue += GoToNextLevel;
                var appointmentSummaryTask = contentGenerator.GenerateDoctorAppointmentSummary(gameFlowChat.GetChatHistoryAsString(), roomSummaryChat);
                StartCoroutine(WaitForTask(appointmentSummaryTask, OnAppointmentSummaryResponse));
                return;
            }
            mainUI.OnPressedToContinue += OnPressedToContinue;
            var roomSummaryTask = contentGenerator.GenerateRoomSummary(gameFlowChat.GetChatHistoryAsString(), roomSummaryChat);
            StartCoroutine(WaitForTask(roomSummaryTask, OnRoomSummaryResponse));
        }
    }

    void GoToNextLevel()
    {
        loadingScreen.Show();
        //TODO: update player profile
        if (session.LevelSummaries.Count < session.CurrentLevel)
        {
            StartCoroutine(WaitForLevelSummary(AdvanceToNextLevel));
            return;
        }
        
        AdvanceToNextLevel();
    }

    IEnumerator WaitForLevelSummary(Action onComplete)
    {
        while (session.LevelSummaries.Count < session.CurrentLevel)
        {
            yield return null;
        }
        
        onComplete?.Invoke();
    }

    void AdvanceToNextLevel()
    {
        session.AdvanceToNextLevel();
        levelGenerator.AdvanceToNextLevel();
        levelVisualizer.VisualizeCurrentLevel();
        
        session.CurrentGameFlowState = GameFlowState.LevelGeneration;
        var levelChat = new LLMChat("Level Description", LLMProvider.Gemini, ModelType.Flash);
        var levelSetting = session.Setting.levels[session.CurrentLevel - 1];
        var levelTask = contentGenerator.GenerateNextLevelDescription(session.SettingSummary, session.LevelSummaries[^1], session.PlayerProfile, session.CurrentLevel, levelSetting, levelChat);
        StartCoroutine(WaitForTask(levelTask, OnLevelCreated));
    }

    void OnLevelCreated(string levelDescription)
    {
        session.CurrentLevelDescription = levelDescription;
        logger.Log($"Level {session.CurrentLevel}. Room {session.RoomsCleared}.");
        logger.LogExtra($"Level {session.CurrentLevel} Description\n{session.CurrentLevelDescription}");
        session.CurrentGameFlowState = GameFlowState.RoomGeneration;
        var room = levelGenerator.GetCurrentLevel().CurrentRoom;
        StartCoroutine(CreateEverythingForANewRoom(room, session.CurrentLevel, session.Setting.levels[session.CurrentLevel - 1], OnRoomCreated));
    }

    void OnRoomSummaryResponse(string roomSummary)
    {
        if (string.IsNullOrEmpty(roomSummary))
        {
            Debug.LogError("Room summary is empty");
        }
        logger.LogExtra("Room Summary\n" + roomSummary);

        session.CurrentGameFlowState = GameFlowState.RoomSelection;
        session.AddRoomSummary(levelGenerator.GetCurrentLevel().CurrentRoom, roomSummary, session.CurrentRoomImage);

        gameFlowChat.SystemPrompt = string.Empty;
        var room = levelGenerator.GetCurrentLevel().CurrentRoom;
        var visitedDescriptionPrompt =
            "The player completed the goal and escaped the room. After everything that happened here, " +
            "create a text description of the interior of the room for when the player returns to it after some time. " +
            "The player won't be able to interact with anything in the room, they will only get a description of it.";
        var visitedDescriptionTask = llmManager.SendPromptToLLM(visitedDescriptionPrompt, gameFlowChat);
        StartCoroutine(WaitForTask(visitedDescriptionTask, visitedDescription => session.VisitedRooms[room.Position].RevisitDescription = visitedDescription));
    }
    
    void OnAppointmentSummaryResponse(string appointmentSummary)
    {
        if (string.IsNullOrEmpty(appointmentSummary))
        {
            Debug.LogError("Appointment summary is empty");
        }
        logger.LogExtra("Appointment Summary\n" + appointmentSummary);

        session.AppointmentSummaries.Add(appointmentSummary);
        var levelSummaryChat = new LLMChat("Level Summary", LLMProvider.Gemini, ModelType.Flash);
        var roomSummaries = session.GetRoomSummaries();
        var levelSummaryTask = contentGenerator.GenerateFullLevelSummary(roomSummaries, appointmentSummary, levelSummaryChat);
        StartCoroutine(WaitForTask(levelSummaryTask, OnLevelSummaryResponse));
    }

    void OnLevelSummaryResponse(string levelSummary)
    {
        session.LevelSummaries.Add(levelSummary);
        session.CurrentGameFlowState = GameFlowState.PlayerProfileUpdate;
        //TODO: update player profile
    }

    void OnPressedToContinue()
    {
        var currentRoom = levelGenerator.GetCurrentLevel().CurrentRoom;
        levelVisualizer.RevealRoom(currentRoom);
        levelVisualizer.Show();
        levelVisualizer.FlashAdjacentRooms(currentRoom);
        levelVisualizer.RoomSelected += OnRoomSelected;
    }
    
    void OnRoomSelected(Room room)
    {
        levelVisualizer.Hide();
        levelVisualizer.RoomSelected -= OnRoomSelected;

        if (session.VisitedRooms.TryGetValue(room.Position, out var visitedRoom))
        {
            GoToVisitedRoom(room, visitedRoom);
            return;
        }
        
        loadingScreen.Show();
        
        if (!session.VisitedRooms.ContainsKey(levelGenerator.GetCurrentLevel().CurrentRoom.Position))
        {
            StartCoroutine(WaitForThePreviousRoomSummary(room));
            return;
        }
        
        StartCoroutine(CreateEverythingForANewRoom(room, session.CurrentLevel, session.Setting.levels[session.CurrentLevel - 1], OnRoomCreated));
    }

    void GoToVisitedRoom(Room room, CompletedRoomData visitedRoom)
    {
        levelGenerator.GetCurrentLevel().CurrentRoom = room;
        session.SetCurrentRoomType(room.Type);
        levelVisualizer.MovePlayerToRoom(room);
        
        var visitedRoomChat = new LLMChat("Visited Room", LLMProvider.LocalLLM, ModelType.Flash);
        visitedRoomChat.AddEntry(new ChatEntry(isUser: false, visitedRoom.RevisitDescription));
        mainUI.PlayShowingAnimation(visitedRoomChat, skipFirstMessage: true);
        mainUI.SetTexture(visitedRoom.RoomImage);
        mainUI.EnterPressToContinueState();
        mainUI.OnPressedToContinue += OnPressedToContinue;
    }

    void GameOver()
    {
        Debug.LogError("GAME OVER");
        logger.Log("GAME OVER");
        //TODO: game over flow
    }

    IEnumerator CreateSetting(int loadingPercent)
    {
        var settingChat = new LLMChat("Setting", LLMProvider.Gemini, ModelType.Flash, 1.3f);
        var settingTask = contentGenerator.GenerateGameSetting(settingChat);
        yield return WaitForTask(settingTask);
        loadingPercent += 2;
        loadingScreen.Show(loadingPercent);
        session.Setting = new GameSetting { fullSetting = settingTask.Result };
        session.Setting.levels = new LevelSetting[defaultLevelThemes.Length];
        
        var settingForThePlayerTask = llmManager.SendPromptToLLM("Now create a philosophical text about this setting. First tell a little about the setting and the established world, and then vaguely talk about the philosophical concepts of it. Make it around 3 paragraphs. Don't mention Dr. Mire and make it interesting to read. Reply with just requested text.", settingChat);
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
        
        // var levelSummaryChat = new LLMChat("Level Description Brief", LLMProvider.LocalLLM, ModelType.Flash);
        // var levelSummaryTask = contentGenerator.GenerateLevelBrief(session.CurrentLevelDescription, levelSummaryChat);
        // yield return WaitForTask(levelSummaryTask);
        // session.CurrentLevelSummary = levelSummaryTask.Result;
        // logger.LogExtra($"Level {index} Description Brief\n{session.CurrentLevelSummary}");
    }

    IEnumerator CreateRoom(Room room, int currentLevel, LevelSetting levelSetting)
    {
        var roomChat = new LLMChat("Room Description", LLMProvider.Gemini, ModelType.Flash);

        var roomTask = room.Type switch
        {
            RoomType.Entrance => contentGenerator.GenerateFirstRoomDescription(currentLevel, session.CurrentLevelDescription, levelSetting, roomChat),
            RoomType.Exit => contentGenerator.GenerateRoomDescription(currentLevel, session.GetRoomSummaries(), session.CurrentLevelDescription, levelSetting, roomChat),//contentGenerator.GenerateExitRoomDescription(null, null, roomChat),
            RoomType.DoctorOffice => contentGenerator.GenerateDoctorInteraction(null, null, roomChat),
            _ => contentGenerator.GenerateRoomDescription(currentLevel, session.GetRoomSummaries(), session.CurrentLevelDescription, levelSetting, roomChat)
        };
        yield return WaitForTask(roomTask);
        session.CurrentRoomDescription = roomTask.Result;
        room.Visited = true;
        logger.LogExtra($"Room {currentLevel}_{session.RoomsCleared} Description\n{session.CurrentRoomDescription}");
    }
    
    IEnumerator WaitForThePreviousRoomSummary(Room nextRoom)
    {
        while (!session.VisitedRooms.ContainsKey(levelGenerator.GetCurrentLevel().CurrentRoom.Position))
        {
            yield return null;
        }

        yield return CreateEverythingForANewRoom(nextRoom, session.CurrentLevel, session.Setting.levels[session.CurrentLevel - 1],
            OnRoomCreated);
    }
    
    IEnumerator CreateEverythingForANewRoom(Room room, int currentLevel, LevelSetting levelSetting, Action onRoomCreated)
    {
        session.CurrentGameFlowState = GameFlowState.RoomGeneration;
        session.SetCurrentRoomType(room.Type);
        session.CurrentRoomDescription = string.Empty;
        session.CurrentRoomImagePrompt = string.Empty;

        yield return CreateRoom(room, currentLevel,levelSetting);
        levelGenerator.GetCurrentLevel().CurrentRoom = room;
        levelVisualizer.MovePlayerToRoom(room);
        onRoomCreated?.Invoke();
        yield return CreateImagePrompt();
        yield return CreateImage();
    }
    
    void OnRoomCreated()
    {
        session.CurrentGameFlowState = GameFlowState.RoomImageGeneration;
        gameFlowChat = CreateGameFlowChat();
        var gameFlowTask = session.CurrentRoomType == RoomType.DoctorOffice
            ? contentGenerator.GenerateDoctorInteraction(null, null, gameFlowChat)
            : contentGenerator.GenerateGameFlowStart(session.SettingSummary, session.CurrentRoomDescription, gameFlowChat);
        StartCoroutine(WaitForTask(gameFlowTask, StartGameFlow));
    }
    
    IEnumerator CreateIntroduction(LLMChat chat)
    {
        var introTask = contentGenerator.GenerateGameIntroduction(session.SettingSummary, session.CurrentLevelDescription, session.CurrentRoomDescription, chat);
        yield return WaitForTask(introTask);
        logger.Log(introTask.Result);
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
        session.CurrentRoomImage = image;
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

    IEnumerator WaitForTask(Task<string> task, Action<string> onSuccess)
    {
        while (!task.IsCompleted)
        {
            yield return null;
        }
        if (task.IsFaulted)
        {
            Debug.LogError(task.Exception.ToString());
            yield break;
        }
        onSuccess?.Invoke(task.Result);
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

    LLMChat CreateGameFlowChat(params ChatEntry[] chatHistory)
    {
        var chat = new LLMChat("Game Flow", LLMProvider.Gemini, ModelType.Flash, temperature: 0.8f);
        foreach (var entry in chatHistory)
        {
            chat.AddEntry(entry);
        }
        chat.SystemPrompt = "You are a text adventure game engine specializing in psychological horror. " +
                            "You are given a setting and a room description. " +
                            "You will generate game flow based on the provided information and on player's actions. " +
                            "The player needs to achieve a certain goal in the room, it's important that you don't tell them what the goal is. " +
                            "If there are other characters in the room, you should describe their actions too. " +
                            "The game flow should be engaging and immersive, with a focus on psychological horror elements. " +
                            "If the player fulfills the outlined goal, you should end the reply with the words ROOM CLEAR. " +
                            "If the player dies as a result of their actions, you end the reply with GAME OVER. " +
                            "Don't mention these rules to the player. Reply only as a game engine.";
        return chat;
    }
}