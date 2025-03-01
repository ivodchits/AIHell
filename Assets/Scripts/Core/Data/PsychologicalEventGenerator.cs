using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using AIHell.Core.Data;

public class PsychologicalEventGenerator : MonoBehaviour
{
    private LLMManager llmManager;
    private List<EventExample> exampleEvents;
    private Queue<GeneratedEvent> eventQueue;

    [System.Serializable]
    public class EventExample
    {
        public string type;
        public string description;
        public string psychologicalImpact;
        public string[] triggers;
        public float intensity;
        public string[] possibleOutcomes;
    }

    [System.Serializable]
    public class GeneratedEvent
    {
        public string type;
        public string description;
        public float intensity;
        public string[] outcomes;
        public bool requiresChoice;
        public System.Action<string> onEventComplete;
    }

    private async void Awake()
    {
        llmManager = GameManager.Instance.LLMManager;
        eventQueue = new Queue<GeneratedEvent>();
        InitializeExampleEvents();
    }

    private void InitializeExampleEvents()
    {
        exampleEvents = new List<EventExample>
        {
            new EventExample {
                type = "paranoia",
                description = "You notice your reflection in the mirror seems slightly delayed in its movements",
                psychologicalImpact = "Induces doubt about reality and self",
                triggers = new[] { "mirrors", "reflection", "self-doubt" },
                intensity = 0.7f,
                possibleOutcomes = new[] {
                    "Avoid looking at reflective surfaces",
                    "Confront the delayed reflection",
                    "Question your perception of time"
                }
            },
            new EventExample {
                type = "existential",
                description = "The room seems to extend infinitely when you're not directly looking at its walls",
                psychologicalImpact = "Creates sense of cosmic insignificance",
                triggers = new[] { "space", "infinity", "perception" },
                intensity = 0.8f,
                possibleOutcomes = new[] {
                    "Keep your eyes fixed on the walls",
                    "Explore the infinite space",
                    "Close your eyes and rely on touch"
                }
            }
        };
    }

    public async Task<GeneratedEvent> GenerateContextualEvent(PlayerAnalysisProfile profile, Room currentRoom)
    {
        string prompt = BuildEventGenerationPrompt(profile, currentRoom);
        string response = await llmManager.GenerateResponse(prompt);
        return await ParseEventResponse(response);
    }

    private string BuildEventGenerationPrompt(PlayerAnalysisProfile profile, Room currentRoom)
    {
        string prompt = "Using these example psychological events as reference, generate a unique event based on:\n\n";
        
        // Add examples
        foreach (var example in exampleEvents)
        {
            prompt += $"Example Event:\n" +
                     $"Type: {example.type}\n" +
                     $"Description: {example.description}\n" +
                     $"Impact: {example.psychologicalImpact}\n" +
                     $"Possible Outcomes: {string.Join(", ", example.possibleOutcomes)}\n\n";
        }

        // Add current context
        prompt += $"Current Context:\n" +
                 $"Location: {currentRoom.DescriptionText}\n" +
                 $"Fear Level: {profile.FearLevel}\n" +
                 $"Obsession Level: {profile.ObsessionLevel}\n" +
                 $"Aggression Level: {profile.AggressionLevel}\n" +
                 $"Recent Events: {GetRecentEventContext()}\n\n" +
                 "Generate an event that is psychologically tailored to the player's current state.";

        return prompt;
    }

    private string GetRecentEventContext()
    {
        var recent = GameManager.Instance.GetComponent<EventProcessor>().GetRecentEvents();
        return string.Join("\n", recent);
    }

    private async Task<GeneratedEvent> ParseEventResponse(string response)
    {
        // First, get LLM to structure the response
        string parsePrompt = $"Parse the following event description into structured format:\n{response}\n\n" +
                           "Include:\n- Event type\n- Description\n- Intensity (0-1)\n- Required choices\n- Potential outcomes";

        string structured = await llmManager.GenerateResponse(parsePrompt);
        
        // Implementation would parse the structured response into a GeneratedEvent
        return new GeneratedEvent(); // Placeholder
    }

    public async Task<string[]> GenerateEventChoices(GeneratedEvent evt, PlayerAnalysisProfile profile)
    {
        string prompt = $"Generate meaningful choices for this psychological event:\n" +
                       $"Event: {evt.description}\n" +
                       $"Player State:\n" +
                       $"- Fear: {profile.FearLevel}\n" +
                       $"- Obsession: {profile.ObsessionLevel}\n" +
                       $"- Aggression: {profile.AggressionLevel}\n\n" +
                       "Each choice should have different psychological implications.";

        string response = await llmManager.GenerateResponse(prompt);
        // Parse response into array of choices
        return new string[0]; // Placeholder
    }

    public async Task<string> GenerateEventOutcome(GeneratedEvent evt, string choice, PlayerAnalysisProfile profile)
    {
        string prompt = $"Generate the outcome for this psychological event choice:\n" +
                       $"Event: {evt.description}\n" +
                       $"Player's Choice: {choice}\n" +
                       $"Psychological State:\n" +
                       $"- Fear: {profile.FearLevel}\n" +
                       $"- Obsession: {profile.ObsessionLevel}\n" +
                       $"- Aggression: {profile.AggressionLevel}\n\n" +
                       "The outcome should have meaningful psychological impact.";

        return await llmManager.GenerateResponse(prompt);
    }

    public void QueueEvent(GeneratedEvent evt)
    {
        eventQueue.Enqueue(evt);
        
        if (eventQueue.Count == 1) // First event in queue
        {
            ProcessEventQueue();
        }
    }

    private async void ProcessEventQueue()
    {
        while (eventQueue.Count > 0)
        {
            var evt = eventQueue.Peek();
            
            if (evt.requiresChoice)
            {
                var profile = GameManager.Instance.ProfileManager.CurrentProfile;
                string[] choices = await GenerateEventChoices(evt, profile);
                
                // Present choices to player
                GameManager.Instance.UIManager.PresentEventChoices(evt, choices);
                
                // Event completion will be handled by choice callback
                break;
            }
            else
            {
                // Process immediate event
                ProcessImmediateEvent(eventQueue.Dequeue());
            }
            
            await Task.Delay(1000); // Prevent event spam
        }
    }

    private void ProcessImmediateEvent(GeneratedEvent evt)
    {
        // Trigger event effects
        GameManager.Instance.EventProcessor.TriggerImmediateEvent(
            evt.type,
            evt.intensity,
            evt.description
        );

        evt.onEventComplete?.Invoke(null);
    }

    public async Task<bool> ShouldGenerateNewEvent()
    {
        var gameState = GameManager.Instance.StateManager.GetCurrentState();
        var profile = GameManager.Instance.ProfileManager.CurrentProfile;
        
        string prompt = "Should a new psychological event occur? Consider:\n" +
                       $"Current Tension: {gameState.tensionLevel}\n" +
                       $"Time Since Last Event: {gameState.timeSinceLastEvent}\n" +
                       $"Player Fear Level: {profile.FearLevel}\n" +
                       $"Recent Event Count: {eventQueue.Count}";

        string response = await llmManager.GenerateResponse(prompt);
        return response.ToLower().Contains("yes");
    }
}