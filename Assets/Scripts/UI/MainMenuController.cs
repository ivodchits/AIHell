using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AIHell.UI
{
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] GameObject mainMenu;
        [SerializeField] GameObject settingsMenu;
        [SerializeField] GameObject loadGameMenu;
        [SerializeField] GameObject startButton;
        
        GameObject lastSelected;

        void Awake()
        {
            EventSystem.current.SetSelectedGameObject(startButton);
            startButton.GetComponent<Button>().Select();
            lastSelected = startButton;
        }

        void Update()
        {
            if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
            {
                EventSystem.current.SetSelectedGameObject(lastSelected);
            }
        }

        public void ShowMainMenu()
        {
            mainMenu.SetActive(true);
            settingsMenu.SetActive(false);
            loadGameMenu.SetActive(false);
        }

        public void ShowSettingsMenu()
        {
            mainMenu.SetActive(false);
            settingsMenu.SetActive(true);
            loadGameMenu.SetActive(false);
        }

        public void ShowLoadGameMenu()
        {
            mainMenu.SetActive(false);
            settingsMenu.SetActive(false);
            loadGameMenu.SetActive(true);
        }
        
        public void QuitGame()
        {
            Application.Quit();
        }
        
        public void StartGame()
        {
            SceneManager.LoadSceneAsync(1);
        }
    }
}