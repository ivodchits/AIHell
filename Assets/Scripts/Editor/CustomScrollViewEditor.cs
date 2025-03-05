using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using AIHell.UI;

namespace AIHell.Editor
{
    [CustomEditor(typeof(CustomScrollView))]
    public class CustomScrollViewEditor : UnityEditor.Editor
    {
        SerializedProperty scrollRect;
        SerializedProperty upIndicatorContainer;
        SerializedProperty downIndicatorContainer;
        SerializedProperty scrollThreshold;

        void OnEnable()
        {
            // Get all serialized properties
            scrollRect = serializedObject.FindProperty("scrollRect");
            upIndicatorContainer = serializedObject.FindProperty("upIndicatorContainer");
            downIndicatorContainer = serializedObject.FindProperty("downIndicatorContainer");
            scrollThreshold = serializedObject.FindProperty("scrollThreshold");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            CustomScrollView scrollView = (CustomScrollView)target;

            EditorGUILayout.Space();
            
            // Scroll rect component
            EditorGUILayout.PropertyField(scrollRect);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Up Indicators", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(upIndicatorContainer);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Down Indicators", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(downIndicatorContainer);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(scrollThreshold);

            EditorGUILayout.Space();
            if (GUILayout.Button("Auto-Setup Scroll View"))
            {
                AutoSetupScrollView(scrollView);
            }

            serializedObject.ApplyModifiedProperties();
        }

        void AutoSetupScrollView(CustomScrollView scrollView)
        {
            if (scrollView == null) return;

            // Try to find a ScrollRect if not assigned
            if (serializedObject.FindProperty("scrollRect").objectReferenceValue == null)
            {
                ScrollRect sr = scrollView.GetComponent<ScrollRect>();
                if (sr != null)
                {
                    serializedObject.FindProperty("scrollRect").objectReferenceValue = sr;
                    Debug.Log("Automatically assigned ScrollRect component.");
                }
                else
                {
                    Debug.LogWarning("No ScrollRect component found on this GameObject. Please add one first.");
                    return;
                }
            }

            // Ask if user wants to create indicator containers
            bool createContainers = EditorUtility.DisplayDialog(
                "Create Indicator Containers",
                "Do you want to automatically create indicator containers and images?",
                "Yes", "No");

            if (createContainers)
            {
                CreateIndicatorContainers(scrollView);
            }

            serializedObject.ApplyModifiedProperties();
        }

        void CreateIndicatorContainers(CustomScrollView scrollView)
        {
            GameObject parent = scrollView.gameObject;
            
            // Create up indicator container if it doesn't exist
            if (upIndicatorContainer.objectReferenceValue == null)
            {
                GameObject upContainer = new GameObject("UpScrollIndicators");
                upContainer.transform.SetParent(parent.transform, false);
                RectTransform upRectTransform = upContainer.AddComponent<RectTransform>();
                
                // Position at top of scroll view
                upRectTransform.anchorMin = new Vector2(0, 1);
                upRectTransform.anchorMax = new Vector2(1, 1);
                upRectTransform.pivot = new Vector2(0.5f, 1);
                upRectTransform.anchoredPosition = new Vector2(0, 0);
                upRectTransform.sizeDelta = new Vector2(0, 30);
                
                // Assign to properties
                upIndicatorContainer.objectReferenceValue = upContainer;
            }
            
            // Create down indicator container if it doesn't exist
            if (downIndicatorContainer.objectReferenceValue == null)
            {
                GameObject downContainer = new GameObject("DownScrollIndicators");
                downContainer.transform.SetParent(parent.transform, false);
                RectTransform downRectTransform = downContainer.AddComponent<RectTransform>();
                
                // Position at bottom of scroll view
                downRectTransform.anchorMin = new Vector2(0, 0);
                downRectTransform.anchorMax = new Vector2(1, 0);
                downRectTransform.pivot = new Vector2(0.5f, 0);
                downRectTransform.anchoredPosition = new Vector2(0, 0);
                downRectTransform.sizeDelta = new Vector2(0, 30);
                
                // Assign to properties
                downIndicatorContainer.objectReferenceValue = downContainer;
            }
            
            Debug.Log("Scroll indicator containers and images created successfully.");
        }

        GameObject CreateIndicatorImage(Transform parent, string name, Vector2 anchorPosition)
        {
            GameObject imgObj = new GameObject(name);
            imgObj.transform.SetParent(parent, false);
            
            RectTransform rectTransform = imgObj.AddComponent<RectTransform>();
            rectTransform.anchorMin = anchorPosition;
            rectTransform.anchorMax = anchorPosition;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(20, 20);
            
            Image img = imgObj.AddComponent<Image>();
            img.color = new Color(1, 1, 1, 0.8f);
            
            return imgObj;
        }
    }
}