using UnityEngine;
using UnityEngine.UI;

namespace AIHell.UI
{
    /// <summary>
    /// A custom scroll view that displays different images based on the scroll position.
    /// Shows up indicators when content can be scrolled up, and down indicators when content can be scrolled down.
    /// </summary>
    public class CustomScrollView : MonoBehaviour
    {
        [Header("Scroll Components")]
        [SerializeField]
        ScrollRect scrollRect;

        [Header("Up Scroll Indicators")]
        [SerializeField] GameObject upIndicatorContainer;

        [Header("Down Scroll Indicators")]
        [SerializeField] GameObject downIndicatorContainer;

        [Header("Settings")]
        [SerializeField, Range(0.01f, 0.2f)] float scrollThreshold = 0.05f;

        float currentScrollPosition = 0f;

        void OnEnable()
        {
            // Subscribe to scroll changes
            if (scrollRect != null)
            {
                scrollRect.onValueChanged.AddListener(OnScrollPositionChanged);
            }
        }

        void OnDisable()
        {
            // Unsubscribe from scroll changes
            if (scrollRect != null)
            {
                scrollRect.onValueChanged.RemoveListener(OnScrollPositionChanged);
            }
        }

        void Start()
        {
            // Make sure all components are assigned
            if (scrollRect == null)
            {
                scrollRect = GetComponent<ScrollRect>();
                if (scrollRect == null)
                {
                    Debug.LogError("No ScrollRect assigned or found on CustomScrollView!");
                }
            }

            // Ensure indicator containers start hidden
            if (upIndicatorContainer != null)
                upIndicatorContainer.SetActive(false);
            
            if (downIndicatorContainer != null)
                downIndicatorContainer.SetActive(false);
                
            // Initialize scroll indicators
            UpdateScrollIndicators(scrollRect.normalizedPosition.y);
        }

        void OnScrollPositionChanged(Vector2 position)
        {
            currentScrollPosition = position.y;
        }

        void Update()
        {
            UpdateScrollIndicators(currentScrollPosition);
        }

        void UpdateScrollIndicators(float normalizedY)
        {
            var threshold = scrollThreshold / scrollRect.content.rect.height;
            
            // Check if we can scroll up (content is below the view)
            bool canScrollUp = normalizedY < (1.0f - threshold);
            
            // Check if we can scroll down (content is above the view)
            bool canScrollDown = normalizedY > threshold;

            // Update visibility of indicator containers
            if (upIndicatorContainer != null)
            {
                upIndicatorContainer.SetActive(canScrollUp);
            }
                
            if (downIndicatorContainer != null)
            {
                downIndicatorContainer.SetActive(canScrollDown);
            }
        }
        
        public void Scroll(float value)
        {
            if (scrollRect != null)
            {
                scrollRect.normalizedPosition += new Vector2(0, value / scrollRect.content.rect.height * scrollRect.scrollSensitivity);
            }
        }

        public void ScrollDown()
        {
            if (scrollRect != null)
            {
                scrollRect.normalizedPosition = Vector2.zero;
            }
        }
    }
}