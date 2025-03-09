using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Handles the generation of different types of game content through LLM prompts
/// </summary>
public class ContentGenerator : MonoBehaviour
{
    [SerializeField] LLMManager llmManager;
    [Header("Prompt Templates")]
    [SerializeField]
    List<LLMPromptTemplate> promptTemplates = new List<LLMPromptTemplate>();
    
    /// <summary>
    /// Gets a prompt template by name
    /// </summary>
    /// <param name="templateName">Name of the template to retrieve</param>
    /// <returns>The prompt template</returns>
    public LLMPromptTemplate GetPromptTemplate(string templateName)
    {
        LLMPromptTemplate template = promptTemplates.Find(t => t.templateName == templateName);
        
        if (template != null)
        {
            return template;
        }
        
        Debug.LogWarning($"Prompt template '{templateName}' not found. Using default template.");
        return new LLMPromptTemplate
        {
            templateText = $"You are a text adventure game narrative generator. Generate a response based on the following context: {templateName}",
            modelType = ModelType.Lite
        };
    }

    /// <summary>
    /// Fills placeholder values in a prompt template
    /// </summary>
    /// <param name="template">The template to fill</param>
    /// <param name="context">Context values to insert</param>
    /// <param name="gameState">Game state values to insert</param>
    /// <returns>The filled prompt template</returns>
    public string FillPromptTemplate(string template, Dictionary<string, string> context, Dictionary<string, object> gameState)
    {
        string filledTemplate = template;
        
        // Replace context placeholders
        if (context != null)
        {
            foreach (var kvp in context)
            {
                string placeholder = "{" + kvp.Key + "}";
                filledTemplate = filledTemplate.Replace(placeholder, kvp.Value);
            }
        }
        
        // Replace game state placeholders
        if (gameState != null)
        {
            foreach (var kvp in gameState)
            {
                string placeholder = "{" + kvp.Key + "}";
                
                // Convert the value to string representation
                string valueStr = kvp.Value?.ToString() ?? "null";
                filledTemplate = filledTemplate.Replace(placeholder, valueStr);
            }
        }
        
        return filledTemplate;
    }
    
    #region Pre-Game Content Generation
    
    /// <summary>
    /// Generates the initial game setting and world description
    /// </summary>
    /// <param name="context">Context about the game world</param>
    /// <param name="gameState">Current game state</param>
    /// <returns>Game setting description</returns>
    public async Task<string> GenerateGameSetting(LLMChat chat)
    {
        var promptTemplate = GetPromptTemplate("GameSetting");
        
        return await llmManager.SendPromptToLLM(promptTemplate.templateText, chat);
    }
    
    /// <summary>
    /// Generates a summarized version of the game setting for context in other prompts
    /// </summary>
    /// <param name="fullSetting">The full game setting description</param>
    /// <returns>Summarized setting description</returns>
    public async Task<string> GenerateSettingSummary(string fullSetting, LLMChat chat)
    {
        var promptTemplate = GetPromptTemplate("SettingSummary");
        
        Dictionary<string, string> context = new Dictionary<string, string>
        {
            { "full_setting", fullSetting }
        };
        
        string finalPrompt = FillPromptTemplate(promptTemplate.templateText, context, null);
        
        var result = await llmManager.SendPromptToLLM(finalPrompt, chat);
        
        return result;
    }
    
    /// <summary>
    /// Generates a room description for an image generation prompt
    /// </summary>
    /// <param name="roomDescription">Full room description</param>
    /// <param name="levelNumber">Current level number</param>
    /// <returns>Image generation prompt</returns>
    public async Task<string> GenerateRoomImagePrompt(string roomDescription, int levelNumber, LLMChat chat)
    {
        var promptTemplate = GetPromptTemplate("RoomImageGeneration");
        
        Dictionary<string, string> context = new Dictionary<string, string>
        {
            { "room_description", roomDescription },
            { "level_number", levelNumber.ToString() }
        };
        
        string finalPrompt = FillPromptTemplate(promptTemplate.templateText, context, null);
        
        return await llmManager.SendPromptToLLM(finalPrompt, chat);
    }
    
    #endregion
    
    #region Room Generation
    
    /// <summary>
    /// Generates a room description using the LLM
    /// </summary>
    /// <param name="roomContext">Context about the room</param>
    /// <param name="gameState">Current game state</param>
    /// <returns>Room description text</returns>
    public async Task<string> GenerateRoomDescription(int levelNumber, string roomSummaries, string levelDescription, LevelSetting levelSetting, LLMChat chat)
    {
        var promptTemplate = GetPromptTemplate("RoomDescription");
        
        Dictionary<string, string> context = new Dictionary<string, string>
        {
            { "level_number", levelNumber.ToString() },
            { "previous_rooms_summary", roomSummaries },
            { "level_description", levelDescription },
            { "level_theme", levelSetting.theme },
            { "level_tone", levelSetting.tone }
        };
        
        string finalPrompt = FillPromptTemplate(promptTemplate.templateText, context, null);
        
        return await llmManager.SendPromptToLLM(finalPrompt, chat);
    }
    
    /// <summary>
    /// Generates the first room description of a level
    /// </summary>
    /// <param name="roomContext">Context about the room</param>
    /// <param name="gameState">Current game state</param>
    /// <returns>First room description text</returns>
    public async Task<string> GenerateFirstRoomDescription(int levelNumber, string levelDescription, LevelSetting levelSetting, LLMChat chat)
    {
        var promptTemplate = GetPromptTemplate("FirstRoom");
        
        Dictionary<string, string> context = new Dictionary<string, string>
        {
            { "level_number", levelNumber.ToString() },
            { "level_description", levelDescription },
            { "level_theme", levelSetting.theme },
            { "level_tone", levelSetting.tone }
        };
        
        string finalPrompt = FillPromptTemplate(promptTemplate.templateText, context, null);
        
        return await llmManager.SendPromptToLLM(finalPrompt, chat);
    }
    
    /// <summary>
    /// Generates the next room description in a level
    /// </summary>
    /// <param name="roomContext">Context about the room and previous rooms</param>
    /// <param name="gameState">Current game state</param>
    /// <returns>Next room description text</returns>
    public async Task<string> GenerateNextRoomDescription(Dictionary<string, string> roomContext, Dictionary<string, object> gameState, LLMChat chat)
    {
        var promptTemplate = GetPromptTemplate("NextRoom");
        string finalPrompt = FillPromptTemplate(promptTemplate.templateText, roomContext, gameState);
        
        return await llmManager.SendPromptToLLM(finalPrompt, chat);
    }
    
    /// <summary>
    /// Generates the exit room description for a level
    /// </summary>
    /// <param name="roomContext">Context about the room and previous rooms</param>
    /// <param name="gameState">Current game state</param>
    /// <returns>Exit room description text</returns>
    public async Task<string> GenerateExitRoomDescription(Dictionary<string, string> roomContext, Dictionary<string, object> gameState, LLMChat chat)
    {
        var promptTemplate = GetPromptTemplate("ExitRoom");
        string finalPrompt = FillPromptTemplate(promptTemplate.templateText, roomContext, gameState);
        
        return await llmManager.SendPromptToLLM(finalPrompt, chat);
    }
    
    /// <summary>
    /// Generates a description for revisiting a previously visited room
    /// </summary>
    /// <param name="roomContext">Context about the room including previous visit summary</param>
    /// <param name="gameState">Current game state</param>
    /// <returns>Revisited room description text</returns>
    public async Task<string> GenerateRevisitedRoomDescription(Dictionary<string, string> roomContext, Dictionary<string, object> gameState, LLMChat chat)
    {
        var promptTemplate = GetPromptTemplate("RevisitedRoom");
        string finalPrompt = FillPromptTemplate(promptTemplate.templateText, roomContext, gameState);
        
        return await llmManager.SendPromptToLLM(finalPrompt, chat);
    }
    
    /// <summary>
    /// Generates a summary of what happened in a room
    /// </summary>
    /// <param name="fullRoomConversation">The full conversation history in the room</param>
    /// <returns>Room summary text</returns>
    public async Task<string> GenerateRoomSummary(string fullRoomConversation, LLMChat chat)
    {
        var promptTemplate = GetPromptTemplate("RoomSummary");
        
        Dictionary<string, string> context = new Dictionary<string, string>
        {
            { "full_room_conversation", fullRoomConversation }
        };
        
        string finalPrompt = FillPromptTemplate(promptTemplate.templateText, context, null);
        
        var result = await llmManager.SendPromptToLLM(finalPrompt, chat);
        
        return result;
    }
    
    #endregion
    
    #region Level Generation
    
    /// <summary>
    /// Generates a level description using the LLM
    /// </summary>
    /// <param name="levelContext">Context about the level, including difficulty, horror score, and theme</param>
    /// <param name="gameState">Current game state</param>
    /// <returns>Level description text</returns>
    public async Task<string> GenerateLevelDescription(string settingSummary, LevelSetting levelSetting, LLMChat chat)
    {
        var promptTemplate = GetPromptTemplate("LevelDescription");
        
        Dictionary<string, string> context = new Dictionary<string, string>
        {
            { "setting_summary", settingSummary },
            { "level_theme", levelSetting.theme },
            { "level_tone", levelSetting.tone }
        };
        
        //TODO: Add game state to context
        string finalPrompt = FillPromptTemplate(promptTemplate.templateText, context, null);
        
        return await llmManager.SendPromptToLLM(finalPrompt, chat);
    }
    
    /// <summary>
    /// Generates the first level description
    /// </summary>
    /// <param name="levelContext">Context about the first level</param>
    /// <param name="gameState">Current game state</param>
    /// <returns>First level description text</returns>
    public async Task<string> GenerateFirstLevelDescription(string setting, LevelSetting levelSetting, LLMChat chat)
    {
        var promptTemplate = GetPromptTemplate("FirstLevel");
        
        Dictionary<string, string> context = new Dictionary<string, string>
        {
            { "setting_summary", setting },
            { "level_theme", levelSetting.theme },
            { "level_tone", levelSetting.tone }
        };
        
        string finalPrompt = FillPromptTemplate(promptTemplate.templateText, context, null);
        
        return await llmManager.SendPromptToLLM(finalPrompt, chat);
    }
    
    /// <summary>
    /// Generates a summarized version of a level description
    /// </summary>
    /// <param name="fullLevelDescription">The full level description</param>
    /// <returns>Summarized level description</returns>
    public async Task<string> GenerateLevelBrief(string fullLevelDescription, LLMChat chat)
    {
        var promptTemplate = GetPromptTemplate("LevelBrief");
        
        Dictionary<string, string> context = new Dictionary<string, string>
        {
            { "full_level_description", fullLevelDescription }
        };
        
        string finalPrompt = FillPromptTemplate(promptTemplate.templateText, context, null);
        
        var result = await llmManager.SendPromptToLLM(finalPrompt, chat);
        
        return result;
    }
    
    /// <summary>
    /// Generates the next level description after completing a level
    /// </summary>
    /// <param name="levelContext">Context about the next level, previous levels, and player profile</param>
    /// <param name="gameState">Current game state</param>
    /// <returns>Next level description text</returns>
    public async Task<string> GenerateNextLevelDescription(string settingSummary, string previousLevelSummary, string playerProfile, int levelNumber, LevelSetting levelSetting, LLMChat chat)
    {
        var promptTemplate = GetPromptTemplate("NextLevel");
        
        Dictionary<string, string> context = new Dictionary<string, string>
        {
            { "setting_summary", settingSummary },
            { "previous_level_summary", previousLevelSummary },
            { "player_profile", playerProfile },
            { "level_number", levelNumber.ToString() },
            { "level_theme", levelSetting.theme },
            { "level_tone", levelSetting.tone }
        };
        string finalPrompt = FillPromptTemplate(promptTemplate.templateText, context, null);
        
        return await llmManager.SendPromptToLLM(finalPrompt, chat);
    }
    
    /// <summary>
    /// Generates a summary of the entire level experience
    /// </summary>
    /// <param name="levelContext">Context including all room summaries and doctor appointment</param>
    /// <returns>Full level summary text</returns>
    public async Task<string> GenerateFullLevelSummary(string roomSummaries, string appointmentSummary, LLMChat chat)
    {
        var promptTemplate = GetPromptTemplate("FullLevelSummary");
        
        Dictionary<string, string> context = new Dictionary<string, string>
        {
            { "room_summaries", roomSummaries },
            { "appointment_summary", appointmentSummary }
        };
        string finalPrompt = FillPromptTemplate(promptTemplate.templateText, context, null);
        
        var result = await llmManager.SendPromptToLLM(finalPrompt, chat);
        
        return result;
    }
    
    #endregion
    
    #region Doctor/Endgame Interactions
    
    /// <summary>
    /// Generates a doctor's office interaction
    /// </summary>
    /// <param name="doctorContext">Context about the doctor interaction, level number, and room summaries</param>
    /// <param name="gameState">Current game state</param>
    /// <returns>Doctor dialogue text</returns>
    public async Task<string> GenerateDoctorInteraction(Dictionary<string, string> doctorContext, Dictionary<string, object> gameState, LLMChat chat)
    {
        var promptTemplate = GetPromptTemplate("DoctorOffice");
        string finalPrompt = FillPromptTemplate(promptTemplate.templateText, doctorContext, gameState);
        
        return await llmManager.SendPromptToLLM(finalPrompt, chat);
    }
    
    /// <summary>
    /// Generates a summary of the doctor's appointment
    /// </summary>
    /// <param name="fullAppointment">The full appointment conversation</param>
    /// <returns>Appointment summary text</returns>
    public async Task<string> GenerateDoctorAppointmentSummary(string fullAppointment, LLMChat chat)
    {
        var promptTemplate = GetPromptTemplate("AppointmentSummary");
        
        Dictionary<string, string> context = new Dictionary<string, string>
        {
            { "full_appointment", fullAppointment }
        };
        
        string finalPrompt = FillPromptTemplate(promptTemplate.templateText, context, null);
        
        var result = await llmManager.SendPromptToLLM(finalPrompt, chat);
        
        return result;
    }
    
    /// <summary>
    /// Generates the final confrontation with Lucifer
    /// </summary>
    /// <param name="endgameContext">Context including all level summaries and player profile</param>
    /// <param name="gameState">Current game state</param>
    /// <returns>Final confrontation text</returns>
    public async Task<string> GenerateFinalConfrontation(Dictionary<string, string> endgameContext, Dictionary<string, object> gameState, LLMChat chat)
    {
        var promptTemplate = GetPromptTemplate("FinalConfrontation");
        string finalPrompt = FillPromptTemplate(promptTemplate.templateText, endgameContext, gameState);
        
        return await llmManager.SendPromptToLLM(finalPrompt, chat);
    }
    
    #endregion
    
    #region Game Flow and Player Profile
    
    /// <summary>
    /// Generates the initial game introduction and room description
    /// </summary>
    /// <param name="context">Context including setting and level summaries, and room description</param>
    /// <param name="gameState">Current game state</param>
    /// <returns>Game introduction text</returns>
    public async Task<string> GenerateGameIntroduction(string settingSummary, string levelSummary, string roomDescription, LLMChat chat)
    {
        var promptTemplate = GetPromptTemplate("GameIntroduction");
        
        Dictionary<string, string> context = new Dictionary<string, string>();
        context["setting_summary"] = settingSummary;
        context["level_summary"] = levelSummary;
        context["room_description"] = roomDescription;
        
        string finalPrompt = FillPromptTemplate(promptTemplate.templateText, context, null);
        
        return await llmManager.SendPromptToLLM(finalPrompt, chat);
    }
    
    public async Task<string> GenerateGameFlowStart(string settingSummary, string roomDescription, LLMChat chat)
    {
        var finalPrompt = GetGameFlowStartMessage(settingSummary, roomDescription);
        
        return await llmManager.SendPromptToLLM(finalPrompt, chat);
    }
    
    public string GetGameFlowStartMessage(string settingSummary, string roomDescription)
    {
        var promptTemplate = GetPromptTemplate("GameFlow");
        
        Dictionary<string, string> context = new Dictionary<string, string>();
        context["setting_summary"] = settingSummary;
        context["room_description"] = roomDescription;
        
        string finalPrompt = FillPromptTemplate(promptTemplate.templateText, context, null);
        
        return finalPrompt;
    }
    
    /// <summary>
    /// Generates an updated psychological profile for the player
    /// </summary>
    /// <param name="profileContext">Context including level summary, appointment log, and previous profile</param>
    /// <returns>Updated player profile text</returns>
    public async Task<string> GeneratePlayerProfile(Dictionary<string, string> profileContext, LLMChat chat)
    {
        var promptTemplate = GetPromptTemplate("PlayerProfile");
        string finalPrompt = FillPromptTemplate(promptTemplate.templateText, profileContext, null);
        
        return await llmManager.SendPromptToLLM(finalPrompt, chat);
    }
    
    /// <summary>
    /// Processes game flow to detect special ending conditions like ROOM CLEAR or PLAYER DEAD
    /// </summary>
    /// <param name="responseText">The LLM response text</param>
    /// <param name="gameSession">The current game session</param>
    /// <returns>The processed response with any special conditions handled</returns>
    public string ProcessGameFlowResponse(string responseText, GameSession gameSession)
    {
        string processedText = responseText;
        
        // Check for room clear condition
        if (processedText.EndsWith("ROOM CLEAR") || processedText.Contains("ROOM CLEAR"))
        {
            processedText = processedText.Replace("ROOM CLEAR", string.Empty);
            gameSession.CurrentGameFlowState = GameFlowState.RoomSummarization;
            Debug.Log("Room cleared detected, advancing to room summarization");
        }
        
        // Check for player death
        if (processedText.EndsWith("GAME OVER") || processedText.Contains("GAME OVER"))
        {
            processedText = processedText.Replace("GAME OVER", string.Empty);
            gameSession.IsGameOver = true;
            gameSession.CurrentGameFlowState = GameFlowState.GameOver;
        }
        
        return processedText.Trim();
    }
    
    #endregion
    
    /// <summary>
    /// Generates an event description using the LLM
    /// </summary>
    /// <param name="eventContext">Context about the event</param>
    /// <param name="gameState">Current game state</param>
    /// <returns>Event description text</returns>
    public async Task<string> GenerateEventDescription(Dictionary<string, string> eventContext, Dictionary<string, object> gameState, LLMChat chat)
    {
        var promptTemplate = GetPromptTemplate("EventDescription");
        string finalPrompt = FillPromptTemplate(promptTemplate.templateText, eventContext, gameState);
        
        return await llmManager.SendPromptToLLM(finalPrompt, chat);
    }
    
    /// <summary>
    /// Generates character dialogue using the LLM
    /// </summary>
    /// <param name="characterContext">Context about the character</param>
    /// <param name="playerInput">The player's input</param>
    /// <param name="gameState">Current game state</param>
    /// <returns>Character dialogue text</returns>
    public async Task<string> GenerateCharacterDialogue(Dictionary<string, string> characterContext, string playerInput, Dictionary<string, object> gameState, LLMChat chat)
    {
        var promptTemplate = GetPromptTemplate("CharacterDialogue");
        
        // Merge character context with player input
        Dictionary<string, string> fullContext = new Dictionary<string, string>(characterContext);
        fullContext["player_input"] = playerInput;
        
        string finalPrompt = FillPromptTemplate(promptTemplate.templateText, fullContext, gameState);
        
        return await llmManager.SendPromptToLLM(finalPrompt, chat);
    }
    
    /// <summary>
    /// Handles the game over sequence with appropriate messaging based on how the game ended
    /// </summary>
    /// <param name="context">Context about how the game ended</param>
    /// <param name="gameState">Current game state</param>
    /// <returns>Game over message</returns>
    public async Task<string> GenerateGameOverSequence(Dictionary<string, string> context, Dictionary<string, object> gameState, LLMChat chat)
    {
        var promptTemplate = GetPromptTemplate("GameOver");
        string finalPrompt = FillPromptTemplate(promptTemplate.templateText, context, gameState);
        
        return await llmManager.SendPromptToLLM(finalPrompt, chat);
    }
    
    /// <summary>
    /// Generates a victory sequence for when the player successfully defeats Lucifer
    /// </summary>
    /// <param name="context">Context about the player's journey</param>
    /// <param name="gameState">Current game state</param>
    /// <returns>Victory message</returns>
    public async Task<string> GenerateVictorySequence(Dictionary<string, string> context, Dictionary<string, object> gameState, LLMChat chat)
    {
        var promptTemplate = GetPromptTemplate("Victory");
        string finalPrompt = FillPromptTemplate(promptTemplate.templateText, context, gameState);
        
        return await llmManager.SendPromptToLLM(finalPrompt, chat);
    }
}

/// <summary>
/// Container for room content including description and image
/// </summary>
public class RoomContent
{
    public string description;
    public string imageUrl;
}