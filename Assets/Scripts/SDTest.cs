using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SDTest : MonoBehaviour
{
    [SerializeField] ImageGenerator imageGenerator;
    [SerializeField] TMP_InputField inputField;
    [SerializeField] RawImage image;

    public void SendInput()
    {
        string input = inputField.text;
        var task = imageGenerator.GenerateImage(input);
        var awaiter = task.GetAwaiter();
        awaiter.OnCompleted(() => ShowOutput(awaiter.GetResult()));
    }

    void ShowOutput(Texture2D output)
    {
        image.texture = output;
    }
}