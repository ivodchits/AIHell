using TMPro;
using UnityEngine;

public class LLMTest : MonoBehaviour
{
    [SerializeField] LLMManager llmManager;
    [SerializeField] LLMProvider llmProvider;
    [SerializeField] ModelType modelType;
    [SerializeField] TMP_InputField inputField;
    [SerializeField] TMP_Text outputText;

    LLMChat chat;

    void Awake()
    {
        chat = new LLMChat("Test", llmProvider, modelType);
    }

    public void SendInput()
    {
        string input = inputField.text;
        outputText.text = "Generating...";
        var task = llmManager.SendPromptToLLM(input, chat);
        var awaiter = task.GetAwaiter();
        awaiter.OnCompleted(() => ShowOutput(awaiter.GetResult()));
    }

    void ShowOutput(string output)
    {
        outputText.text = output;
        Debug.Log($"Output generated:\n{output}");
    }
}