using UnityEngine;

namespace AIHell.UI
{
    public abstract class UIControllerBase : MonoBehaviour
    {
        [SerializeField] protected CanvasGroup canvasGroup;

        public virtual void Show()
        {
            canvasGroup.alpha = 1;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
        
        public virtual void Hide()
        {
            canvasGroup.alpha = 0;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }
}