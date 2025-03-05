using System.Collections;
using TMPro;
using UnityEngine;

namespace AIHell.UI
{
    public class LoadingScreenController : MonoBehaviour
    {
        [SerializeField] CanvasGroup canvasGroup;
        [SerializeField] TMP_Text loadingText;
        [SerializeField] TMP_Text progressText;

        Coroutine showingCoroutine;
        
        public void Show(int percent = -1)
        {
            if (showingCoroutine == null)
            {
                StartCoroutine(ShowCoroutine());
            }
            UpdatePercentage(percent);
            
            canvasGroup.alpha = 1;
        }
        
        public void Hide()
        {
            if (showingCoroutine != null)
            {
                StopCoroutine(showingCoroutine);
                showingCoroutine = null;
            }
            canvasGroup.alpha = 0;
        }

        IEnumerator ShowCoroutine()
        {
            var timer = 0f;
            while (true)
            {
                yield return null;
                timer += Time.deltaTime;
                loadingText.text = $"Loading{new string('.', Mathf.FloorToInt(timer) % 3 + 1)}";
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