using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AIHell.UI
{
    public class GameUIController : MonoBehaviour
    {
        [SerializeField] TMP_InputField inputField;
        [SerializeField] CustomScrollView scrollView;
        [SerializeField] TMP_Text chatText;

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Return))
            {
                EnterPressed();
                inputField.ActivateInputField();
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
            chatText.text += "\n" + inputField.text;
            inputField.text = "";
        }
    }
}