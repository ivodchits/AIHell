using System.Collections;
using TMPro;
using UnityEngine;

namespace AIHell.UI
{
    public class LoadingScreenController : UIControllerBase
    {
        [SerializeField] GameObject pressToContinueText;
        [SerializeField] GameObject settingTextGameObject;
        [SerializeField] GameObject gradientGameObject;
        [SerializeField] TMP_Text settingText;
        [SerializeField] TMP_Text loadingText;
        [SerializeField] TMP_Text progressText;
        [SerializeField] RectTransform settingTextRectTransform;
        [SerializeField] float scrollingSpeed = 5f;
        
        public event System.Action OnPressedToContinue;
        
        bool inPressToContinueState;

        Coroutine showingCoroutine;
        Coroutine settingTextCoroutine;
        
        public bool ShowingText { get; private set; }
        
        public override void Show()
        {
            Show(percent: -1);
        }

        public void Show(int percent)
        {
            if (showingCoroutine == null)
            {
                StartCoroutine(ShowCoroutine());
            }
            UpdatePercentage(percent);
            
            canvasGroup.alpha = 1;
            
            if (settingTextCoroutine == null)
            {
                settingTextGameObject.SetActive(false);
            }

            if (percent == 100)
            {
                UpdatePercentage(-1);
                loadingText.text = string.Empty;
                pressToContinueText.SetActive(true);
                inPressToContinueState = true;
            }
        }

        public void Show(string text, int percent)
        {
            if (settingTextCoroutine != null)
            {
                StopCoroutine(settingTextCoroutine);
            }

            settingTextGameObject.SetActive(true);
            settingText.SetText(text.Replace("\\n", "\n"));
            settingTextCoroutine = StartCoroutine(ScrollTextCoroutine());
            
            if (showingCoroutine == null)
            {
                StartCoroutine(ShowCoroutine());
            }
            UpdatePercentage(percent);
            
            canvasGroup.alpha = 1;
        }
        
        public override void Hide()
        {
            if (showingCoroutine != null)
            {
                StopCoroutine(showingCoroutine);
                showingCoroutine = null;
            }

            if (settingTextCoroutine != null)
            {
                StopCoroutine(settingTextCoroutine);
                settingTextCoroutine = null;
            }
            
            pressToContinueText.SetActive(false);
            settingTextGameObject.SetActive(false);
            canvasGroup.alpha = 0;
        }

        IEnumerator ShowCoroutine()
        {
            var timer = 0f;
            while (true)
            {
                yield return null;
                if (inPressToContinueState)
                {
                    loadingText.text = string.Empty;
                    yield break;
                }
                timer += Time.deltaTime;
                loadingText.text = $"Loading{new string('.', Mathf.FloorToInt(timer) % 3 + 1)}";
            }
        }
        
        IEnumerator ScrollTextCoroutine()
        {
            ShowingText = true;
            gradientGameObject.SetActive(true);
            settingTextRectTransform.anchoredPosition = new Vector2(0, -settingTextRectTransform.rect.height);
            while (settingTextRectTransform.anchoredPosition.y < 0)
            {
                yield return null;
                settingTextRectTransform.anchoredPosition =
                    Vector2.MoveTowards(settingTextRectTransform.anchoredPosition, Vector2.zero, scrollingSpeed * Time.deltaTime);
            }
            settingTextRectTransform.anchoredPosition = Vector2.zero;
            gradientGameObject.SetActive(false);
            ShowingText = false;
        }

        void Update()
        {
            if (inPressToContinueState && Input.GetKeyDown(KeyCode.Space))   
            {
                inPressToContinueState = false;
                pressToContinueText.SetActive(false);
                OnPressedToContinue?.Invoke();
            }
        }

        void UpdatePercentage(int percent)
        {
            if (percent >= 0)
            {
                progressText.enabled = true;
                progressText.text = $"({percent}%)";
            }
            else
            {
                progressText.enabled = false;
            }
        }
    }
}