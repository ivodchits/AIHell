using UnityEngine;
using System.Text;

public static class TextFormatter
{
    public static string StyleRoomDescription(string description)
    {
        return $"<color=#E8E8E8><size=120%>{description}</size></color>";
    }

    public static string StyleEventText(string eventText)
    {
        return $"<color=#FF6B6B><i>{eventText}</i></color>";
    }

    public static string StyleCharacterName(string name)
    {
        return $"<color=#6BB5FF><b>{name}</b></color>";
    }

    public static string StyleCharacterDialogue(string dialogue)
    {
        return $"<color=#6BFF6B>\"{dialogue}\"</color>";
    }

    public static string StyleSystemMessage(string message)
    {
        return $"<color=#888888>{message}</color>";
    }

    public static string StyleErrorMessage(string error)
    {
        return $"<color=#FF4444>{error}</color>";
    }

    public static string StyleExits(string[] exits)
    {
        if (exits == null || exits.Length == 0)
            return StyleSystemMessage("There are no visible exits.");

        StringBuilder sb = new StringBuilder("Available exits: ");
        for (int i = 0; i < exits.Length; i++)
        {
            sb.Append($"<color=#FFBB44>{exits[i]}</color>");
            if (i < exits.Length - 1)
                sb.Append(", ");
        }
        return sb.ToString();
    }

    public static string StyleHelp(string helpText)
    {
        return $"<color=#44FF44>{helpText}</color>";
    }

    public static string StylePlayerInput(string input)
    {
        return $"<color=#888888>> </color><color=#FFFFFF>{input}</color>";
    }
}