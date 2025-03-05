using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(LevelVisualizer))]
public class LevelVisualizerEditor : Editor
{
    int selectedLevelIndex = 0;
    
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        LevelVisualizer visualizer = (LevelVisualizer)target;
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Level Visualization Controls", EditorStyles.boldLabel);
        
        // Level selection
        selectedLevelIndex = EditorGUILayout.IntSlider("Level Index", selectedLevelIndex, 0, 4);
        
        EditorGUILayout.BeginHorizontal();
        
        // Visualize selected level
        if (GUILayout.Button("Visualize Level"))
        {
            visualizer.VisualizeLevel(selectedLevelIndex);
        }
        
        // Clear visualization
        if (GUILayout.Button("Clear Visualization"))
        {
            visualizer.ClearVisualization();
        }
        
        EditorGUILayout.EndHorizontal();
    }
}