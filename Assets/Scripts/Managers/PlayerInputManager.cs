using UnityEngine;
using System.Collections.Generic;
using System;

public class PlayerInputManager : MonoBehaviour
{
    private Dictionary<string, Action<string>> commands;
    private HashSet<string> moveDirections;

    private void Awake()
    {
        InitializeCommands();
    }

    private void InitializeCommands()
    {
        moveDirections = new HashSet<string> { "north", "south", "east", "west" };
        
        commands = new Dictionary<string, Action<string>>(StringComparer.OrdinalIgnoreCase)
        {
            { "go", HandleMove },
            { "examine", HandleExamine },
            { "look", HandleExamine },
            { "help", _ => DisplayHelp() }
        };
    }

    public void ProcessInput(string input)
    {
        string[] parts = input.Trim().ToLower().Split(new[] { ' ' }, 2);
        string command = parts[0];
        string argument = parts.Length > 1 ? parts[1] : string.Empty;

        if (commands.TryGetValue(command, out Action<string> action))
        {
            action(argument);
            GameManager.Instance.ProfileManager.TrackChoice(command, argument);
        }
        else if (moveDirections.Contains(command))
        {
            HandleMove(command);
            GameManager.Instance.ProfileManager.TrackChoice("move", command);
        }
        else
        {
            GameManager.Instance.UIManager.DisplayMessage("I don't understand that command.");
        }
    }

    private void HandleMove(string direction)
    {
        if (string.IsNullOrEmpty(direction) || !moveDirections.Contains(direction))
        {
            GameManager.Instance.UIManager.DisplayMessage("Which direction do you want to go?");
            return;
        }

        if (!GameManager.Instance.LevelManager.TryMove(direction))
        {
            GameManager.Instance.UIManager.DisplayMessage($"You cannot go {direction}.");
        }
    }

    private void HandleExamine(string target)
    {
        if (string.IsNullOrEmpty(target))
        {
            // Look at the room in general
            Room currentRoom = GameManager.Instance.LevelManager.CurrentRoom;
            if (currentRoom != null)
            {
                GameManager.Instance.UIManager.DisplayRoomDescription(currentRoom);
            }
            return;
        }

        // TODO: Implement more detailed examination of specific objects or features
        GameManager.Instance.UIManager.DisplayMessage($"You examine the {target} more closely, but find nothing notable.");
    }

    private void DisplayHelp()
    {
        string helpText = @"Available commands:
- go [direction] (or just type: north, south, east, west)
- examine/look [target]
- help (shows this message)";
        
        GameManager.Instance.UIManager.DisplayMessage(helpText);
    }
}