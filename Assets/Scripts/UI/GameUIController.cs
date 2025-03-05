using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace AIHell.UI
{
    public class GameUIController : MonoBehaviour
    {
        [SerializeField] TMP_InputField inputField;
        [SerializeField] CustomScrollView scrollView;
        [SerializeField] TMP_Text chatText;

        void Update()
        {
            if (EventSystem.current.currentSelectedGameObject != inputField.gameObject)
            {
                EventSystem.current.SetSelectedGameObject(inputField.gameObject);
                inputField.Select();
            }

            if (Input.GetKeyDown(KeyCode.Return))
            {
                EnterPressed();
                inputField.ActivateInputField();
            }
            
            if (Input.mouseScrollDelta != Vector2.zero)
            {
                scrollView.Scroll(Input.mouseScrollDelta.y);
            }
        }

        void EnterPressed()
        {
            chatText.text += "\n" + inputField.text;
            inputField.text = "";
        }
    }
}