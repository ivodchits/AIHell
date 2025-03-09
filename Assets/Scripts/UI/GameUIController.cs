using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

namespace AIHell.UI
{
    public class GameUIController : UIControllerBase
    {
        [SerializeField] TMP_InputField inputField;
        [SerializeField] CanvasGroup inputFieldCanvasGroup;
        [SerializeField] CustomScrollView scrollView;
        [SerializeField] TMP_Text chatText;
        [SerializeField] CanvasGroup chatCanvasGroup;
        [SerializeField] RawImage image;
        [SerializeField] CanvasGroup imageCanvasGroup;
        [SerializeField] GameObject pressToContinueText;
        [SerializeField] float fadeinDuration = 2f;
        [SerializeField] float fadeoutDuration = 3f;
        [SerializeField] float typingSpeed = 20f;
        [SerializeField] float tvNoiseMax = 1f;
        [SerializeField] float tvNoiseMin = 0.1f;
        [SerializeField] float tvNoiseAdjustmentDuration = 2f;
        
        public event Action<string> OnUserInput;
        public event Action OnPressedToContinue;

        bool isCurrentlyTyping = false;
        bool isInPressToContinueState = false;
        string fullText = string.Empty;
        Coroutine typingCoroutine;
        
        int noiseMaterialPropertyId = Shader.PropertyToID("_NoiseIntensity");
        
        public void PlayShowingAnimation(LLMChat initialChat, bool skipFirstMessage)
        {
            fullText = string.Empty;
            chatText.SetText(string.Empty);
            inputField.text = string.Empty;
            
            inputFieldCanvasGroup.alpha = 0;
            chatCanvasGroup.alpha = 1;
            imageCanvasGroup.alpha = 0;
            
            canvasGroup.alpha = 1;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            var text = string.Empty;
            var startingIndex = skipFirstMessage ? 1 : 0;
            for (int i = startingIndex; i < initialChat.ChatHistory.Count; i++)
            {
                var c = initialChat.ChatHistory[i];
                text += "\n\n" + (c.IsUser ? "-You: " + c.Content : c.Content);
            }
            AddMessage(text);
            
            StartCoroutine(FadeIn());
        }
        
        public void PlayHidingAnimation(Action onFinished)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            inputField.text = string.Empty;
            
            StartCoroutine(FadeOut(onFinished));
        }
        
        public void AddMessage(ChatEntry entry)
        {
            if (entry.IsUser)
            {
                SetTextInstantly(fullText + "\n\n-You: " + PrepareText(entry.Content));
                scrollView.ScrollDown();
            }
            else
            {
                AddMessage(entry.Content);
            }
        }

        public void SetTexture(Texture2D texture)
        {
            image.texture = texture;
        }

        public void EnterPressToContinueState()
        {
            isInPressToContinueState = true;
            inputField.text = string.Empty;
            inputFieldCanvasGroup.alpha = 0;
            pressToContinueText.SetActive(true);
        }
        
        string PrepareText(string text)
        {
            return text.Replace("\\n", "\n").Replace("\\\"", "\"");
        }

        void AddMessage(string rawText)
        {
            var text = PrepareText(rawText);
            var startIndex = fullText.Length;
            fullText += "\n\n" + text;

            // Add the message with invisible text initially
            string invisibleText = $"<color=#00000000>{text}";
            
            chatText.SetText(chatText.text + invisibleText);
            chatText.ForceMeshUpdate();
            // Force the scroll view to update
            LayoutRebuilder.ForceRebuildLayoutImmediate(scrollView.transform as RectTransform);
            
            // Start the typing effect coroutine
            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
            }
            typingCoroutine = StartCoroutine(RevealTextGradually(startIndex));
        }
        
        void SetTextInstantly(string text)
        {
            fullText = text;
            chatText.SetText(text);
            chatText.ForceMeshUpdate();
            LayoutRebuilder.ForceRebuildLayoutImmediate(scrollView.transform as RectTransform);
        }

        IEnumerator RevealTextGradually(int startIndex)
        {
            isCurrentlyTyping = true;
        
            inputField.text = string.Empty;
            inputFieldCanvasGroup.alpha = 0;
        
            var typingCounter = 0f;
            var tag = "<color=#00000000>";
        
            while (startIndex < fullText.Length)
            {
                var symbolsPerFrame = typingSpeed * Time.deltaTime;
                typingCounter += symbolsPerFrame;
                if (typingCounter < 1)
                {
                    yield return null;
                    continue;
                }
        
                var symbolsToShow = Mathf.FloorToInt(typingCounter);
        
                // Ensure indices are within bounds
                if (startIndex + symbolsToShow > fullText.Length)
                {
                    symbolsToShow = fullText.Length - startIndex;
                }
        
                startIndex += symbolsToShow;

                // Replace the invisible text with gradually revealed text
                var visiblePart = fullText.Substring(0, startIndex);
                var invisiblePart = fullText.Substring(startIndex);
        
                // Update the chat text
                chatText.SetText(visiblePart + tag + invisiblePart);
        
                chatText.ForceMeshUpdate();
                yield return null;
            }
        
            inputField.text = string.Empty;
            inputFieldCanvasGroup.alpha = 1;
            isCurrentlyTyping = false;
        }

        IEnumerator FadeIn()
        {
            image.material.SetFloat(noiseMaterialPropertyId, 1);
            while (imageCanvasGroup.alpha < 1)
            {
                imageCanvasGroup.alpha += Time.deltaTime / fadeinDuration;
                yield return null;
            }
            
            imageCanvasGroup.alpha = 1;
            Show();

            while (image.texture == null)
            {
                yield return null;
            }
            
            var noiseIntensity = tvNoiseMax;
            while(noiseIntensity > tvNoiseMin)
            {
                noiseIntensity -= Time.deltaTime / tvNoiseAdjustmentDuration;
                image.material.SetFloat(noiseMaterialPropertyId, noiseIntensity);
                yield return null;
            }
            image.material.SetFloat(noiseMaterialPropertyId, tvNoiseMin);
        }

        IEnumerator FadeOut(Action onFinished)
        {
            while (canvasGroup.alpha > 0)
            {
                canvasGroup.alpha -= Time.deltaTime / fadeoutDuration;
                yield return null;
            }
            
            Hide();
            
            image.material.SetFloat(noiseMaterialPropertyId, tvNoiseMax);

            chatText.SetText(string.Empty);
            inputField.text = string.Empty;
            
            onFinished?.Invoke();
        }

        void Update()
        {
            if(canvasGroup.alpha == 0)
            {
                return;
            }
            
            if (Input.GetKeyDown(KeyCode.Return) && !isCurrentlyTyping && !isInPressToContinueState)
            {
                EnterPressed();
                inputField.ActivateInputField();
            }

            if (isInPressToContinueState && Input.GetKeyDown(KeyCode.Space))
            {
                isInPressToContinueState = false;
                pressToContinueText.SetActive(false);
                PlayHidingAnimation(() => OnPressedToContinue?.Invoke());
            }
            
            if (Input.mouseScrollDelta != Vector2.zero)
            {
                scrollView.Scroll(Input.mouseScrollDelta.y);
                LayoutRebuilder.ForceRebuildLayoutImmediate(scrollView.transform as RectTransform);
            }
            
            if (EventSystem.current.currentSelectedGameObject != inputField.gameObject)
            {
                EventSystem.current.SetSelectedGameObject(inputField.gameObject);
                inputField.Select();
            }
        }

        void EnterPressed()
        {
            OnUserInput?.Invoke(inputField.text);
            inputField.text = string.Empty;
        }
    }
}