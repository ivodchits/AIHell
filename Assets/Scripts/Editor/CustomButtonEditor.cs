using AIHell.UI;
using UnityEditor;
using UnityEditor.UI;

[CustomEditor(typeof(CustomButton))]
public class CustomButtonEditor : ButtonEditor
{
    SerializedProperty secondaryGraphicProperty;
    SerializedProperty secondaryDefaultColorProperty;
    SerializedProperty secondarySelectedColorProperty;

    protected override void OnEnable()
    {
        base.OnEnable();
        
        secondaryGraphicProperty = serializedObject.FindProperty("secondaryGraphic");
        secondaryDefaultColorProperty = serializedObject.FindProperty("secondaryDefaultColor");
        secondarySelectedColorProperty = serializedObject.FindProperty("secondarySelectedColor");
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Secondary Graphic Properties", EditorStyles.boldLabel);
        
        serializedObject.Update();
        
        EditorGUILayout.PropertyField(secondaryGraphicProperty);
        EditorGUILayout.PropertyField(secondaryDefaultColorProperty);
        EditorGUILayout.PropertyField(secondarySelectedColorProperty);
        
        serializedObject.ApplyModifiedProperties();
    }
}