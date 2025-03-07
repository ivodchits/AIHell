using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

[CustomEditor(typeof(ContentGenerator))]
public class LLMPromptTemplateEditor : Editor
{
    bool showTemplateEditor = false;
    string newTemplateName = "";
    string newTemplateText = "";
    Vector2 templateTextScrollPos;
    int selectedTemplateIndex = -1;
    
    // Default templates to offer
    readonly Dictionary<string, string> defaultTemplates = new Dictionary<string, string>
    {
        {
            "GameSetting",
            @"You are a text adventure game narrative generator specializing in psychological horror.
Create the setting and world description for a deeply unsettling psychological horror game.
In the game the player is in hell but doesn't know it yet. They travel through a series of levels, each with its own theme and tone.
These levels are an illusion created by Dr. Cassius Mire, a psychiatrist who is actually Lucifer in disguise.
The purpose is to explore the player's psyche and exploit their fears.
Get creative with the overall setting, it can be anything from an industrial complex to a space station.

The setting should be vague enough to allow for personalization through gameplay, but specific enough to establish a cohesive world.
Keep the description to 3-5 paragraphs. The player won't see this description, so it can be more technical and specific.

Each consecutive level will be progressively more disturbing and psychologically challenging.
Reply with just the text of the setting description.

The game setting:"
        },
        {
            "SettingSummary",
            @"Summarize the following game setting description for use as brief context in other prompts:

{full_setting}

Create a concise summary (maximum 4 sentences) that captures the essential elements of the setting while maintaining the ominous tone."
        },
        {
            "RoomDescription",
            @"You are a text adventure game narrative generator specializing in psychological horror.
The player is entering a new room in Level {level_number}, themed ""{level_theme}"".
The overall tone of this level is ""{level_tone}"".

Here is the level description:
{level_description_brief}

Here is what happened in the rooms before:
{previous_rooms_summary}

Considering the player's psychological profile, which currently indicates:
- Aggression Level: {player_aggression_level}
- Curiosity Level: {player_curiosity_level}

Describe this room in detail, focusing on contents and events happening in the room.
Keep the description concise yet impactful, approximately 5-10 sentences.
Highlight potentially disturbing or unusual elements within the room. The player may meet new or previously met characters here.

Important: The player MUST solve something or complete some action to leave this room. Make this condition clear in the description.
The player won't see this description, so it can be more technical and specific. Get creative with room's content and goals.

Generate the room description:"
        },
        {
            "FirstRoom",
            @"You are a text adventure game narrative generator specializing in psychological horror.
Create the first room description for Level {level_number} of a psychological horror game.

Level Theme: ""{level_theme}""
Level Tone: ""{level_tone}""
Level Description: ""{level_description}""

The player has just entered Level {level_number} and this is the very first room they encounter.

Create a detailed room description that:
1. Sets the tone for the entire level
2. Introduces elements that reflect the level theme
3. Contains subtle psychological horror elements
4. Includes a specific condition or puzzle that must be solved to proceed further

The room should establish the atmosphere while suggesting that darker elements await deeper in the level.
The condition for proceeding should be clear but not immediately obvious - the player should need to interact with the environment.

Important: The player MUST solve something or complete some action to leave this room. Make this condition clear in the description.
The player won't see this description, so it can be more technical and specific.

Keep the description to 4-10 sentences, focusing on contents and events happening in the room.

Generate the first room description:"
        },
        {
            "NextRoom",
            @"You are a text adventure game narrative generator specializing in psychological horror.
Create the next room description for Level {level_number} of a psychological horror game.

Level Theme: ""{level_theme}""
Level Tone: ""{level_tone}""
Room Archetype: ""{room_archetype}""

Previous rooms in this level:
{previous_rooms_summary}

The player's psychological profile currently indicates:
- Fear Level: {player_fear_level}
- Paranoia Level: {player_paranoia_level}
- Aggression Level: {player_aggression_level}

Create a detailed room description that:
1. Builds upon the established atmosphere of the level
2. Contains new disturbing elements that escalate the psychological tension
3. Optionally includes characters or events that challenge the player
4. Includes a condition that must be fulfilled to proceed further

The condition for proceeding should relate to the room's contents and possibly to the player's psychological profile.
If this is room {room_number} out of {total_rooms} in the level, adjust the intensity accordingly.

Important: The player MUST complete some action or solve something to leave this room. End with ""ROOM CLEAR"" when they've completed the condition, or ""PLAYER DEAD"" if their actions lead to death.

Keep the description to 4-6 sentences, focusing on sensory details and psychological impact.

Generate the next room description:"
        },
        {
            "ExitRoom",
            @"You are a text adventure game narrative generator specializing in psychological horror.
Create the exit room description for Level {level_number} of a psychological horror game.

Level Theme: ""{level_theme}""
Level Tone: ""{level_tone}""

Previous rooms in this level:
{previous_rooms_summary}

The player's psychological profile currently indicates:
- Fear Level: {player_fear_level}
- Paranoia Level: {player_paranoia_level}
- Aggression Level: {player_aggression_level}

Create a detailed description for the exit room that:
1. Serves as a climax to the themes explored in this level
2. Contains disturbing elements that represent a culmination of the level's horror
3. Features Dr. Cassius Mire's office door prominently in the room
4. Includes a final challenge or revelation before the player can exit

IMPORTANT: The room MUST contain Dr. Cassius Mire's office door as the way to exit this level.
The door should be described in an unsettling way that hints at Dr. Mire's true nature (Lucifer in disguise).

Keep the description to 4-6 sentences, focusing on sensory details and psychological impact.
End with ""ROOM CLEAR"" when they've completed the condition, or ""PLAYER DEAD"" if their actions lead to death.

Generate the exit room description:"
        },
        {
            "RevisitedRoom",
            @"You are a text adventure game narrative generator specializing in psychological horror.
The player is revisiting a room they've already explored in Level {level_number}.

Original room description summary:
""{room_summary}""

What happened during their previous visit:
""{previous_visit_summary}""

Create a brief description of the revisited room that:
1. Acknowledges that the player has been here before
2. Notes any changes that might have occurred since their last visit
3. Indicates that there is nothing new to discover here
4. Suggests they should choose another direction

The description should be brief (2-3 sentences) but maintain the psychological horror atmosphere.
The player cannot do anything meaningful in this room now - they must leave.

End the description with ""ROOM CLEAR"" to indicate they should move on.

Generate the revisited room description:"
        },
        {
            "RoomSummary",
            @"Summarize the following interaction that took place in a room in the psychological horror game:

{full_room_conversation}

Create a concise summary (3-4 sentences) that captures:
1. The key events that happened
2. Important actions the player took
3. Any significant acquaintances or revelations that occurred

The summary will be used for context in future game prompts, so include essential psychological elements."
        },
        {
            "LevelDescription",
            @"You are a text adventure game narrative generator specializing in psychological horror.
Create a description for Act {level_number} of a psychological horror game about submerging deeper and deeper into the player's subconsciousness. Come up with a vague narrative for this act and outline the main events that will occur.

- Theme of the act should be ""{level_theme}""
- Tone of the act is ""{level_tone}""

The player has just completed act {previous_level_number} which was themed ""{previous_level_theme}"".
The overall setting of the game is ""{setting_summary}"".

Create a brief, atmospheric description that:
1. Introduces the act's setting and psychological atmosphere
2. Hints at what horrors might await the player
3. Establishes the overall mood appropriate to the difficulty and horror rating
4. Provides subtle clues about what the player might encounter

Keep the description concise yet evocative, approximately 5-10 sentences.
As the difficulty and the act number increase, the descriptions should become progressively more unsettling and foreboding.
Reply with just the requested text of the act description.

Act description:"
        },
        {
            "FirstLevel",
            @"You are a text adventure game narrative generator specializing in psychological horror.
Create the first act description for a psychological horror game about submerging deeper and deeper into the player's subconsciousness. Come up with a vague narrative for this act and outline the main events that will occur.

Game Setting:
{setting_summary}

- Theme of the act is ""{level_theme}""
- Tone of the act is ""{level_tone}""

Create a brief, atmospheric description that:
1. Introduces the first act's setting and psychological atmosphere
2. Establishes a sense of disorientation appropriate for the beginning of the journey
3. Hints at what horrors might await the player
4. Establishes the overall mood appropriate to the difficulty and horror rating
5. Provides subtle clues about what the player might encounter

Keep the description concise yet evocative, approximately 5-10 sentences.
The description should be mildly unsettling, creating a feeling that something in this world is off.
Reply with just the text of the act description.

Level description:"
        },
        {
            "LevelBrief",
            @"Summarize the following level description for a psychological horror game:

{full_level_description}

Create a concise summary (2-3 sentences) that captures the essential theme, tone, and psychological elements of the level while maintaining the ominous atmosphere."
        },
        {
            "NextLevel",
            @"Here is what happened in the previous level:
{previous_level_summary}

At the end of the level the player was confronted by Dr. Cassius Mire, who is actually Lucifer in disguise.
Here is the summary of their appointment:
{previous_appointment_summary}

Player's Psychological Profile:
{player_profile}

Consider these level parameters:
- Level Number: {level_number}
- Difficulty: {difficulty_level}/10 (increases with each level)
- Horror Rating: {horror_score}/10 (increases with each level)
- Theme: ""{level_theme}""
- Tone: ""{level_tone}""

Create a brief, atmospheric description of the next level that:
1. Introduces new level's setting and psychological atmosphere
2. Builds upon the psychological journey established in previous levels
3. Incorporates elements from the player's psychological profile, especially their fears and weaknesses
4. Establishes a more intense challenge appropriate to the increased difficulty

The level should exploit the weaknesses and fears identified in the player's profile.
As this is level {level_number}, the description should be progressively more disturbing than previous levels.

Keep the description concise yet evocative, approximately 5-10 sentences.
Reply with just the text of the level description.

Next level description:"
        },
        {
            "FullLevelSummary",
            @"Create a comprehensive summary of the player's experience in Level {level_number}.

Room Summaries:
{room_summaries}

Doctor's Appointment Summary:
{appointment_summary}

Create a concise but complete summary (10-30 sentences) that captures:
1. The key psychological themes explored in this level
2. Important choices or actions the player made
3. Significant revelations or character encounters
4. How the doctor's appointment concluded the level

This summary will be used to inform future game content, so highlight elements that reflect the player's psychological state and potential weaknesses."
        },
        {
            "DoctorOffice",
            @"You are a text adventure game narrative generator specializing in psychological horror.
Create a doctor's office interaction for a psychological horror game.

The player has just completed Level {level_number}.

Level Theme: ""{level_theme}""
Level Tone: ""{level_tone}""

Room Summaries from this level:
{room_summaries}

Player's Psychological Profile:
{player_profile}

The player is now in Dr. Cassius Mire's office for their appointment after completing the level.
Dr. Mire is actually Lucifer in disguise, manipulating the player's journey through their personal hell.

Important context:
- For Level 1-2 appointments: Dr. Mire appears friendly but subtly manipulative
- For Level 3-4 appointments: Dr. Mire becomes progressively more aggressive and targets player weaknesses

Create a doctor's office scene where:
1. Dr. Mire analyzes the player's actions during the level
2. He probes into the player's fears and insecurities based on their choices
3. The conversation becomes more unsettling as it progresses
4. The level of hostility matches the current level number (higher = more aggressive)

The session should end with Dr. Mire dismissing the patient to the next level.
Make the dialogue realistic, psychologically unsettling, and reflective of the level theme.

Generate the first message of the doctor's office interaction:"
        },
        {
            "AppointmentSummary",
            @"Summarize the following interaction from the psychological horror game:

{full_appointment}

Create a concise summary (3-5 sentences) that captures:
1. Dr. Mire's assessment and attitude toward the player
2. Key psychological insights revealed about the player
3. Any significant warnings or ominous statements made

The summary should extract the most important psychological elements."
        },
        {
            "GameIntroduction",
            @"You are a text adventure game narrative generator specializing in psychological horror.
Create the opening introduction for a psychological horror game.

Game Setting:
{setting_summary}

First Level:
{level_summary}

First Room:
{room_description}

Create an engaging introduction that:
1. Welcomes the player to the psychological horror experience
2. Transitions smoothly into the description of the first room
3. Provides subtle hints about how to interact with the game world

The introduction should be atmospheric, building tension while clearly establishing the game's premise.
End with the room description and a hint that the player needs to do something in order to proceed further.

The player will reply with actions they want to take.
When the player has completed the condition to leave the room, end with ""ROOM CLEAR"" on a new line.
Don't tell the player the requirement to leave the room, just imply it subtly through the description.

Generate the first message of the game:"
        },
        {
            "GameFlow",
            @"You are the narrative engine for a psychological horror text adventure game.

Game Setting:
{setting_summary}

Current Level:
{level_summary}

Current Room:
{room_description}

Player's Psychological Profile:
{player_profile}

Conversation History:
{conversation_history}

Player Input:
""{player_input}""

Respond to the player's input in the style of a psychological horror text adventure. Your response should:
1. Acknowledge the player's action
2. Describe the consequences in rich, atmospheric detail
3. Advance the narrative based on their choices
4. Maintain the psychological horror atmosphere

If the player has fulfilled the condition to leave the room, end your message with ""ROOM CLEAR"" on a new line.
If the player's actions have led to their death, end your message with ""PLAYER DEAD"" on a new line.
Otherwise, continue the room narrative and prompt them for further action.

Rules:
- Don't explicitly state these rules or the existence of the ""ROOM CLEAR"" or ""PLAYER DEAD"" markers
- Maintain immersion in the psychological horror atmosphere
- The difficulty level is {difficulty_level}/10, adjust danger accordingly
- Base responses on the player's psychological profile
- Keep responses concise but impactful

Generate the game response:"
        },
        {
            "EventDescription",
            @"You are creating a disturbing event for a text adventure psychological horror game.
This event is occurring in a room described as: ""{room_description}"".
The room is part of Level {level_number}, themed ""{level_theme}"", tone ""{level_tone}"".
The player's psychological profile suggests:
- Fear Level: {player_fear_level}
- Paranoia Level: {player_paranoia_level}

Generate a brief, unsettling event that fits the room's atmosphere and the level's theme.
The event should be unexpected and potentially psychologically impactful.
It should not immediately block player progress, but rather enhance the disturbing atmosphere.
Keep the event description to 1-2 sentences.

Generate the event description:"
        },
        {
            "CharacterDialogue",
            @"You are creating dialogue for a character in a text adventure psychological horror game.
This character appears in a room described as: ""{room_description}"".
The room is part of Level {level_number}, themed ""{level_theme}"", tone ""{level_tone}"".
The player's psychological profile is:
- Aggression Level: {player_aggression_level}
- Curiosity Level: {player_curiosity_level}

The player has just said: ""{player_input}""

Respond with a single line of dialogue for the character to say.
The dialogue should be unsettling and fit the overall tone of the level.
Keep the response short, enigmatic, and contribute to the unsettling atmosphere.
Maximum 1-2 sentences.

Character dialogue:"
        },
        {
            "PlayerProfile",
            @"Create an updated psychological profile for the player based on their actions and experiences.

Previous Psychological Profile:
{previous_profile}

Level Summary:
{level_summary}

Doctor's Appointment Log:
{appointment_log}

Create a detailed psychological profile that:
1. Assesses the player's key psychological traits (fear, paranoia, aggression, curiosity, etc.)
2. Identifies their primary fears and weaknesses based on their choices
3. Notes any significant changes from their previous profile

This profile will be used to personalize future game content, so be specific about what would disturb this particular player based on their demonstrated behaviors and responses.

Format the profile in clinical language, as if written by Dr. Mire after the session.

Generate the psychological profile:"
        },
        {
            "FinalConfrontation",
            @"You are a text adventure game narrative generator specializing in psychological horror.
Create the final confrontation scene for a psychological horror game.

Game Setting:
{setting_summary}

Level Summaries:
{all_level_summaries}

Doctor Appointment Summaries:
{all_appointment_summaries}

Player's Final Psychological Profile:
{player_profile}

Allied Characters (if any):
{allied_characters}

The player has just entered the final area after completing Level 5. Instead of finding Dr. Mire's office door, they've discovered a gateway into the void. Upon entering, they face Dr. Mire, who now reveals himself as Lucifer in all his glory.

Create an intense final confrontation scene where:
1. Dr. Mire transforms into his true form as Lucifer
2. He confronts the player about their journey through his crafted hell
3. He exploits the specific fears and weaknesses identified in their profile
4. The player must find a way to overcome Lucifer based on their choices throughout the game
5. Any allies they've made can assist them if they've bonded with characters

The tone should be terrifying, revelatory, and climactic.
This is the final battle for the player's soul.

Generate the final confrontation scene:"
        },
        {
            "RoomImageGeneration",
            @"Create a detailed image generation prompt based on this room description from a psychological horror game:

Room Description:
{room_description}

Current Level: {level_number} (higher levels should be progressively more disturbing).

Create a detailed prompt for an AI image generator that:
1. Describes the key visual elements of the room in concrete detail
2. Captures the psychological horror atmosphere
3. Specifies lighting, color palette, and mood appropriate to the level
4. Includes any important objects or features mentioned

The prompt should be detailed enough to generate a consistent image that matches the text description.
Focus on creating an unsettling, disturbing atmosphere appropriate to the level number.

Write the image generation prompt in a format optimal for image AI (without mentioning AI). It should consist of short precise phrases that describe the room.
For example:
""dark disturbing photo, horror atmosphere, dimly lit, unsettling shadows, eerie silence, cracked walls, flickering lights, blood stains on the floor, broken furniture, a single flickering bulb hanging from the ceiling, a mirror reflecting something ominous.""

Answer with just the suggested prompt."
        }
    };
    
    public override void OnInspectorGUI()
    {
        // Draw the default inspector properties
        DrawDefaultInspector();
        
        // Get the target as ContentGenerator
        ContentGenerator contentGenerator = (ContentGenerator)target;
        
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Prompt Templates Editor", EditorStyles.boldLabel);
        
        // Button to show/hide template editor
        if (GUILayout.Button(showTemplateEditor ? "Hide Template Editor" : "Show Template Editor"))
        {
            showTemplateEditor = !showTemplateEditor;
        }
        
        if (showTemplateEditor)
        {
            EditorGUILayout.Space(5);
            
            // Get the template list via SerializedProperty
            SerializedProperty promptTemplatesProperty = serializedObject.FindProperty("promptTemplates");
            
            // Current templates
            EditorGUILayout.LabelField("Current Templates", EditorStyles.boldLabel);
            
            // Check if there are templates
            if (promptTemplatesProperty.arraySize > 0)
            {
                // Create a dropdown of template names
                string[] templateNames = new string[promptTemplatesProperty.arraySize];
                for (int i = 0; i < promptTemplatesProperty.arraySize; i++)
                {
                    SerializedProperty template = promptTemplatesProperty.GetArrayElementAtIndex(i);
                    SerializedProperty nameProperty = template.FindPropertyRelative("templateName");
                    templateNames[i] = nameProperty.stringValue;
                }
                
                // Dropdown to select template
                int newSelectedIndex = EditorGUILayout.Popup("Select Template", selectedTemplateIndex, templateNames);
                
                // If selection changed
                if (newSelectedIndex != selectedTemplateIndex)
                {
                    if (selectedTemplateIndex != -1)
                    {
                        // Save changes to the previously selected template
                        SerializedProperty oldTemplate = promptTemplatesProperty.GetArrayElementAtIndex(selectedTemplateIndex);
                        SerializedProperty oldTextProperty = oldTemplate.FindPropertyRelative("templateText");
                        oldTextProperty.stringValue = newTemplateText;
                    }
                    
                    selectedTemplateIndex = newSelectedIndex;
                    
                    if (selectedTemplateIndex != -1)
                    {
                        // Load the newly selected template
                        SerializedProperty template = promptTemplatesProperty.GetArrayElementAtIndex(selectedTemplateIndex);
                        SerializedProperty nameProperty = template.FindPropertyRelative("templateName");
                        SerializedProperty textProperty = template.FindPropertyRelative("templateText");
                        
                        newTemplateName = nameProperty.stringValue;
                        newTemplateText = textProperty.stringValue;
                    }
                }
                
                // If a template is selected, show the edit fields
                if (selectedTemplateIndex != -1)
                {
                    EditorGUILayout.Space(5);
                    
                    // Template name field
                    EditorGUI.BeginChangeCheck();
                    newTemplateName = EditorGUILayout.TextField("Template Name", newTemplateName);
                    
                    EditorGUILayout.LabelField("Template Text");
                    
                    // Scrollable text area for template text
                    templateTextScrollPos = EditorGUILayout.BeginScrollView(templateTextScrollPos, GUILayout.Height(200));
                    newTemplateText = EditorGUILayout.TextArea(newTemplateText, GUILayout.ExpandHeight(true));
                    EditorGUILayout.EndScrollView();
                    
                    // Save template edits
                    if (GUILayout.Button("Save Template Changes"))
                    {
                        SerializedProperty template = promptTemplatesProperty.GetArrayElementAtIndex(selectedTemplateIndex);
                        SerializedProperty nameProperty = template.FindPropertyRelative("templateName");
                        SerializedProperty textProperty = template.FindPropertyRelative("templateText");
                        
                        nameProperty.stringValue = newTemplateName;
                        textProperty.stringValue = newTemplateText;
                        
                        serializedObject.ApplyModifiedProperties();
                        EditorUtility.SetDirty(contentGenerator);
                    }
                    
                    EditorGUILayout.Space(5);
                    
                    // Delete template button
                    if (GUILayout.Button("Delete Template", GUILayout.Width(120)))
                    {
                        if (EditorUtility.DisplayDialog("Confirm Delete", 
                            $"Are you sure you want to delete the template '{newTemplateName}'?", 
                            "Yes", "No"))
                        {
                            promptTemplatesProperty.DeleteArrayElementAtIndex(selectedTemplateIndex);
                            serializedObject.ApplyModifiedProperties();
                            EditorUtility.SetDirty(contentGenerator);
                            
                            selectedTemplateIndex = -1;
                            newTemplateName = "";
                            newTemplateText = "";
                        }
                    }
                }
            }
            else
            {
                EditorGUILayout.LabelField("No templates found. Create a new template below.");
            }
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Create New Template", EditorStyles.boldLabel);
            
            // Add new template section
            if (selectedTemplateIndex == -1) // Only allow creating new templates if no template is being edited
            {
                newTemplateName = EditorGUILayout.TextField("Template Name", newTemplateName);
                
                EditorGUILayout.LabelField("Template Text");
                
                // Scrollable text area for template text
                templateTextScrollPos = EditorGUILayout.BeginScrollView(templateTextScrollPos, GUILayout.Height(200));
                newTemplateText = EditorGUILayout.TextArea(newTemplateText, GUILayout.ExpandHeight(true));
                EditorGUILayout.EndScrollView();
                
                EditorGUILayout.Space(5);
                
                // Create template button
                if (GUILayout.Button("Create Template"))
                {
                    if (!string.IsNullOrEmpty(newTemplateName) && !string.IsNullOrEmpty(newTemplateText))
                    {
                        // Check for duplicate template names
                        bool isDuplicate = false;
                        for (int i = 0; i < promptTemplatesProperty.arraySize; i++)
                        {
                            SerializedProperty template = promptTemplatesProperty.GetArrayElementAtIndex(i);
                            SerializedProperty nameProperty = template.FindPropertyRelative("templateName");
                            
                            if (nameProperty.stringValue == newTemplateName)
                            {
                                isDuplicate = true;
                                break;
                            }
                        }
                        
                        if (isDuplicate)
                        {
                            EditorUtility.DisplayDialog("Error", 
                                $"A template with the name '{newTemplateName}' already exists.", 
                                "OK");
                        }
                        else
                        {
                            // Add the new template
                            promptTemplatesProperty.arraySize++;
                            SerializedProperty newTemplate = promptTemplatesProperty.GetArrayElementAtIndex(promptTemplatesProperty.arraySize - 1);
                            SerializedProperty nameProperty = newTemplate.FindPropertyRelative("templateName");
                            SerializedProperty textProperty = newTemplate.FindPropertyRelative("templateText");
                            SerializedProperty modelProperty = newTemplate.FindPropertyRelative("modelType");
                            
                            nameProperty.stringValue = newTemplateName;
                            textProperty.stringValue = newTemplateText;
                            modelProperty.enumValueIndex = (int)ModelType.Lite; // Default to Lite model
                            
                            serializedObject.ApplyModifiedProperties();
                            EditorUtility.SetDirty(contentGenerator);
                            
                            // Clear the fields
                            newTemplateName = "";
                            newTemplateText = "";
                        }
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Error", 
                            "Template name and text cannot be empty.", 
                            "OK");
                    }
                }
            }
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Import Default Templates", EditorStyles.boldLabel);
            
            // Clear and add all default templates
            if (GUILayout.Button("Clear and Add All Default Templates"))
            {
                if (EditorUtility.DisplayDialog("Confirm Clear and Replace", 
                    "This will CLEAR ALL existing templates and add all default templates. This action cannot be undone. Are you sure?", 
                    "Yes, Clear and Replace", "Cancel"))
                {
                    // Clear all existing templates
                    promptTemplatesProperty.ClearArray();
                    
                    // Add all default templates
                    foreach (var defaultTemplate in defaultTemplates)
                    {
                        promptTemplatesProperty.arraySize++;
                        SerializedProperty newTemplate = promptTemplatesProperty.GetArrayElementAtIndex(promptTemplatesProperty.arraySize - 1);
                        SerializedProperty nameProperty = newTemplate.FindPropertyRelative("templateName");
                        SerializedProperty textProperty = newTemplate.FindPropertyRelative("templateText");
                        SerializedProperty modelProperty = newTemplate.FindPropertyRelative("modelType");
                        
                        nameProperty.stringValue = defaultTemplate.Key;
                        textProperty.stringValue = defaultTemplate.Value;
                        modelProperty.enumValueIndex = (int)ModelType.Lite; // Default to Lite model
                    }
                    
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(contentGenerator);
                    
                    // Reset editor state
                    selectedTemplateIndex = -1;
                    newTemplateName = "";
                    newTemplateText = "";
                    
                    EditorUtility.DisplayDialog("Templates Replaced", 
                        $"All templates have been cleared and replaced with {defaultTemplates.Count} default templates.", 
                        "OK");
                }
            }
            
            // Add default templates section
            foreach (var defaultTemplate in defaultTemplates)
            {
                if (GUILayout.Button($"Import '{defaultTemplate.Key}' Template"))
                {
                    // Check if the template already exists
                    bool exists = false;
                    for (int i = 0; i < promptTemplatesProperty.arraySize; i++)
                    {
                        SerializedProperty template = promptTemplatesProperty.GetArrayElementAtIndex(i);
                        SerializedProperty nameProperty = template.FindPropertyRelative("templateName");
                        
                        if (nameProperty.stringValue == defaultTemplate.Key)
                        {
                            exists = true;
                            break;
                        }
                    }
                    
                    if (exists)
                    {
                        if (EditorUtility.DisplayDialog("Template Exists", 
                            $"A template named '{defaultTemplate.Key}' already exists. Do you want to overwrite it?", 
                            "Yes", "No"))
                        {
                            // Find and update the existing template
                            for (int i = 0; i < promptTemplatesProperty.arraySize; i++)
                            {
                                SerializedProperty template = promptTemplatesProperty.GetArrayElementAtIndex(i);
                                SerializedProperty nameProperty = template.FindPropertyRelative("templateName");
                                
                                if (nameProperty.stringValue == defaultTemplate.Key)
                                {
                                    SerializedProperty textProperty = template.FindPropertyRelative("templateText");
                                    textProperty.stringValue = defaultTemplate.Value;
                                    break;
                                }
                            }
                            
                            serializedObject.ApplyModifiedProperties();
                            EditorUtility.SetDirty(contentGenerator);
                        }
                    }
                    else
                    {
                        // Add as a new template
                        promptTemplatesProperty.arraySize++;
                        SerializedProperty newTemplate = promptTemplatesProperty.GetArrayElementAtIndex(promptTemplatesProperty.arraySize - 1);
                        SerializedProperty nameProperty = newTemplate.FindPropertyRelative("templateName");
                        SerializedProperty textProperty = newTemplate.FindPropertyRelative("templateText");
                        SerializedProperty modelProperty = newTemplate.FindPropertyRelative("modelType");
                        
                        nameProperty.stringValue = defaultTemplate.Key;
                        textProperty.stringValue = defaultTemplate.Value;
                        modelProperty.enumValueIndex = (int)ModelType.Lite; // Default to Lite model
                        
                        serializedObject.ApplyModifiedProperties();
                        EditorUtility.SetDirty(contentGenerator);
                    }
                }
            }
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Import/Export Templates", EditorStyles.boldLabel);
            
            // Export all templates
            if (GUILayout.Button("Export All Templates"))
            {
                string path = EditorUtility.SaveFilePanel("Export Templates", "", "AIHell_Templates.json", "json");
                if (!string.IsNullOrEmpty(path))
                {
                    List<LLMPromptTemplate> templates = new List<LLMPromptTemplate>();
                    
                    for (int i = 0; i < promptTemplatesProperty.arraySize; i++)
                    {
                        SerializedProperty template = promptTemplatesProperty.GetArrayElementAtIndex(i);
                        SerializedProperty nameProperty = template.FindPropertyRelative("templateName");
                        SerializedProperty textProperty = template.FindPropertyRelative("templateText");
                        SerializedProperty modelProperty = template.FindPropertyRelative("modelType");
                        
                        templates.Add(new LLMPromptTemplate 
                        { 
                            templateName = nameProperty.stringValue, 
                            templateText = textProperty.stringValue,
                            modelType = (ModelType) modelProperty.enumValueIndex
                        });
                    }
                    
                    string json = JsonUtility.ToJson(new TemplateCollection { templates = templates }, true);
                    File.WriteAllText(path, json);
                    
                    EditorUtility.DisplayDialog("Export Successful", 
                        $"Templates exported to: {path}", 
                        "OK");
                }
            }
            
            // Import templates
            if (GUILayout.Button("Import Templates"))
            {
                string path = EditorUtility.OpenFilePanel("Import Templates", "", "json");
                if (!string.IsNullOrEmpty(path))
                {
                    string json = File.ReadAllText(path);
                    TemplateCollection templateCollection = JsonUtility.FromJson<TemplateCollection>(json);
                    
                    if (templateCollection != null && templateCollection.templates != null && templateCollection.templates.Count > 0)
                    {
                        if (EditorUtility.DisplayDialog("Import Templates", 
                            $"Import {templateCollection.templates.Count} templates? This will merge with existing templates, overwriting duplicates.", 
                            "Yes", "No"))
                        {
                            foreach (var importedTemplate in templateCollection.templates)
                            {
                                // Check if template with this name already exists
                                bool found = false;
                                for (int i = 0; i < promptTemplatesProperty.arraySize; i++)
                                {
                                    SerializedProperty template = promptTemplatesProperty.GetArrayElementAtIndex(i);
                                    SerializedProperty nameProperty = template.FindPropertyRelative("templateName");
                                    
                                    if (nameProperty.stringValue == importedTemplate.templateName)
                                    {
                                        // Update existing template
                                        SerializedProperty textProperty = template.FindPropertyRelative("templateText");
                                        SerializedProperty modelProperty = template.FindPropertyRelative("modelType");
                                        textProperty.stringValue = importedTemplate.templateText;
                                        modelProperty.enumValueIndex = (int)importedTemplate.modelType;
                                        found = true;
                                        break;
                                    }
                                }
                                
                                if (!found)
                                {
                                    // Add as new template
                                    promptTemplatesProperty.arraySize++;
                                    SerializedProperty newTemplate = promptTemplatesProperty.GetArrayElementAtIndex(promptTemplatesProperty.arraySize - 1);
                                    SerializedProperty nameProperty = newTemplate.FindPropertyRelative("templateName");
                                    SerializedProperty textProperty = newTemplate.FindPropertyRelative("templateText");
                                    SerializedProperty modelProperty = newTemplate.FindPropertyRelative("modelType");
                                    
                                    nameProperty.stringValue = importedTemplate.templateName;
                                    textProperty.stringValue = importedTemplate.templateText;
                                    modelProperty.enumValueIndex = (int)importedTemplate.modelType;
                                }
                            }
                            
                            serializedObject.ApplyModifiedProperties();
                            EditorUtility.SetDirty(contentGenerator);
                            
                            EditorUtility.DisplayDialog("Import Successful", 
                                $"Imported {templateCollection.templates.Count} templates.", 
                                "OK");
                        }
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Import Error", 
                            "Failed to import templates. The file may be invalid or corrupted.", 
                            "OK");
                    }
                }
            }
        }
    }
    
    [System.Serializable]
    class TemplateCollection
    {
        public List<LLMPromptTemplate> templates;
    }
}