using System;
using UnityEngine;
using UnityEngine.UI;

public class RoomView : MonoBehaviour
{
    [SerializeField] CanvasGroup canvasGroup;
    [SerializeField] Image background;
    [SerializeField] Image foreground;
    [SerializeField] ColorSettings backgoundColorSettings;
    [SerializeField] ColorSettings foregroundColorSettings;

    public Room Room { get; private set; }
    
    bool isHidden;

    public void Initialize(Room room)
    {
        Room = room;
    }

    public void Hide()
    {
        canvasGroup.alpha = 0;
        isHidden = true;
    }

    public void Show()
    {
        canvasGroup.alpha = 1;
        background.color = backgoundColorSettings.defaultColor;
        foreground.color = foregroundColorSettings.defaultColor;
        isHidden = false;
    }

    public void Flash()
    {
        if (!isHidden)
        {
            return;
        }
        background.color = backgoundColorSettings.flashingColor;
        foreground.color = foregroundColorSettings.flashingColor;
    }
    
    [Serializable]
    class ColorSettings
    {
        public Color defaultColor;
        public Color flashingColor;
    }

    public void SetAlpha(float alpha)
    {
        canvasGroup.alpha = alpha;
    }
}