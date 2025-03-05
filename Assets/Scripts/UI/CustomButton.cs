using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AIHell.UI
{
    public class CustomButton : Button
    {
        [SerializeField] Graphic secondaryGraphic;
        [SerializeField] Color secondaryDefaultColor;
        [SerializeField] Color secondarySelectedColor;

        public override void OnSelect(BaseEventData eventData)
        {
            base.OnSelect(eventData);
            secondaryGraphic.color = secondarySelectedColor;
        }
        
        public override void OnDeselect(BaseEventData eventData)
        {
            base.OnDeselect(eventData);
            secondaryGraphic.color = secondaryDefaultColor;
        }
    }
}