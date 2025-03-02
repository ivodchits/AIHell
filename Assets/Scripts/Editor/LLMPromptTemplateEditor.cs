using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

[CustomEditor(typeof(LLMManager))]
public class LLMPromptTemplateEditor : Editor
{
    private bool showTemplateEditor = false;
    private string newTemplateName = "";
    private string newTemplateText = "";
    private Vector2 templateTextScrollPos;
    private int selectedTemplateIndex = -1;
    
    // Default templates to offer
    private readonly Dictionary<string, string> defaultTemplates = new Dictionary<string, string>
    {
        {
            "RoomDescription",
            @"You are a text adventure game narrative generator specializing in psychological horror.
The player is entering a new room in Level {level_number}, themed ""{level_theme}"".
The overall tone of this level is ""{level_tone}"".
The room archetype is ""{room_archetype}"".

Considering the player's psychological profile, which currently indicates:
- Aggression Level: {player_aggression_level}
- Curiosity Level: {player_curiosity_level}

Describe this room in vivid detail, focusing on sensory details and unsettling atmosphere.
The description should evoke feelings of {level_emotion}.
Keep the description concise yet impactful, approximately 3-5 sentences.
Highlight potentially disturbing or unusual elements within the room.

Generate the room description:"
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
        }
    };
    
    public override void OnInspectorGUI()
    {
        // Draw the default inspector properties
        DrawDefaultInspector();
        
        // Get the target as LLMManager
        LLMManager llmManager = (LLMManager)target;
        
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
                        EditorUtility.SetDirty(llmManager);
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
                            EditorUtility.SetDirty(llmManager);
                            
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
                            
                            nameProperty.stringValue = newTemplateName;
                            textProperty.stringValue = newTemplateText;
                            
                            serializedObject.ApplyModifiedProperties();
                            EditorUtility.SetDirty(llmManager);
                            
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
                            EditorUtility.SetDirty(llmManager);
                        }
                    }
                    else
                    {
                        // Add as a new template
                        promptTemplatesProperty.arraySize++;
                        SerializedProperty newTemplate = promptTemplatesProperty.GetArrayElementAtIndex(promptTemplatesProperty.arraySize - 1);
                        SerializedProperty nameProperty = newTemplate.FindPropertyRelative("templateName");
                        SerializedProperty textProperty = newTemplate.FindPropertyRelative("templateText");
                        
                        nameProperty.stringValue = defaultTemplate.Key;
                        textProperty.stringValue = defaultTemplate.Value;
                        
                        serializedObject.ApplyModifiedProperties();
                        EditorUtility.SetDirty(llmManager);
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
                        
                        templates.Add(new LLMPromptTemplate 
                        { 
                            templateName = nameProperty.stringValue, 
                            templateText = textProperty.stringValue 
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
                                        textProperty.stringValue = importedTemplate.templateText;
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
                                    
                                    nameProperty.stringValue = importedTemplate.templateName;
                                    textProperty.stringValue = importedTemplate.templateText;
                                }
                            }
                            
                            serializedObject.ApplyModifiedProperties();
                            EditorUtility.SetDirty(llmManager);
                            
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
    private class TemplateCollection
    {
        public List<LLMPromptTemplate> templates;
    }
}