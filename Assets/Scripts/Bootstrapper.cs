using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bootstrapper : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        InitializeGameSystems();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void InitializeGameSystems()
    {
        // Ensure GameManager exists
        if (GameManager.Instance == null)
        {
            GameObject gameManagerObj = new GameObject("GameManager");
            gameManagerObj.AddComponent<GameManager>();
            DontDestroyOnLoad(gameManagerObj);
        }
    }
}
