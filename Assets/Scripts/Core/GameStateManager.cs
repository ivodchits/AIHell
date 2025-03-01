using UnityEngine;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using AIHell.Core.Data;

namespace AIHell.Core
{
    public sealed class GameStateManager : MonoBehaviour
    {
        private LLMManager llmManager;
        private UnityEventBridge eventBridge;
        private readonly GameState currentState = new GameState();
        private readonly List<StateTransition> stateHistory = new List<StateTransition>();
        private readonly Stack<GameState> stateUndoStack = new Stack<GameState>();
        private float stateUpdateInterval = 0.5f;
        private float lastStateUpdate;
        private const int MAX_UNDO_SIZE = 10;

        [System.Serializable]
        private class GameState
        {
            public float tensionLevel;
            public float timeInCurrentArea;
            public float timeInCurrentPhase;
            public float timeSinceLastEvent;
            public int recentEventCount;
            public string currentPhase;
            public Dictionary<string, float> narrativeWeights = new Dictionary<string, float>();
            public Dictionary<string, object> dynamicVariables = new Dictionary<string, object>();
            public Dictionary<string, int> eventHistory = new Dictionary<string, int>();
            public bool isInCriticalState;

            public GameState Clone()
            {
                var clone = new GameState {
                    tensionLevel = tensionLevel,
                    timeInCurrentArea = timeInCurrentArea,
                    timeInCurrentPhase = timeInCurrentPhase,
                    timeSinceLastEvent = timeSinceLastEvent,
                    recentEventCount = recentEventCount,
                    currentPhase = currentPhase,
                    isInCriticalState = isInCriticalState
                };

                foreach (var pair in narrativeWeights)
                    clone.narrativeWeights[pair.Key] = pair.Value;
                
                foreach (var pair in dynamicVariables)
                    clone.dynamicVariables[pair.Key] = pair.Value;
                
                foreach (var pair in eventHistory)
                    clone.eventHistory[pair.Key] = pair.Value;

                return clone;
            }
        }

        [System.Serializable]
        public class StateTransition
        {
            public GameState previousState;
            public GameState newState;
            public string trigger;
            public float timestamp;
            public float psychologicalSignificance;
        }

        private void Awake()
        {
            llmManager = GameManager.Instance.LLMManager;
            eventBridge = GameManager.Instance.GetComponent<UnityEventBridge>();
            InitializeState();
        }

        private void InitializeState()
        {
            currentState.tensionLevel = 0f;
            currentState.timeInCurrentArea = 0f;
            currentState.timeInCurrentPhase = 0f;
            currentState.timeSinceLastEvent = 0f;
            currentState.recentEventCount = 0;
            currentState.currentPhase = "introduction";
            currentState.narrativeWeights = new Dictionary<string, float>();
            currentState.dynamicVariables = new Dictionary<string, object>();
            currentState.eventHistory = new Dictionary<string, int>();
            currentState.isInCriticalState = false;

            InitializeNarrativeWeights();
            LoadGameState();
        }

        private void InitializeNarrativeWeights()
        {
            currentState.narrativeWeights["psychological"] = 0.4f;
            currentState.narrativeWeights["supernatural"] = 0.3f;
            currentState.narrativeWeights["existential"] = 0.2f;
            currentState.narrativeWeights["cosmic"] = 0.1f;
        }

        private void Update()
        {
            if (Time.time - lastStateUpdate >= stateUpdateInterval)
            {
                UpdateGameState();
                lastStateUpdate = Time.time;
            }

            // Decay recent event count
            if (Time.time - currentState.timeSinceLastEvent > 30f)
            {
                currentState.recentEventCount = Mathf.Max(0, currentState.recentEventCount - 1);
            }

            // Natural tension decay if not in critical state
            if (!currentState.isInCriticalState && currentState.tensionLevel > 0)
            {
                currentState.tensionLevel = Mathf.Max(0, currentState.tensionLevel - Time.deltaTime * 0.05f);
            }
        }

        private async void UpdateGameState()
        {
            var previousState = currentState;
            
            // Update time-based variables
            currentState.timeInCurrentArea += stateUpdateInterval;
            currentState.timeInCurrentPhase += stateUpdateInterval;
            currentState.timeSinceLastEvent += stateUpdateInterval;

            // Update tension based on psychological state
            UpdateTensionLevel();

            // Check for significant state changes
            if (HasSignificantStateChange(previousState))
            {
                await ProcessStateTransition(previousState);
            }

            // Update narrative weights based on player state
            await UpdateNarrativeWeights();
        }

        private void UpdateTensionLevel()
        {
            var profile = GameManager.Instance.ProfileManager.CurrentProfile;
            
            // Base tension from psychological state
            float psychologicalTension = (
                profile.FearLevel * 0.4f +
                profile.ObsessionLevel * 0.3f +
                profile.AggressionLevel * 0.3f
            );

            // Modify based on recent events
            float eventModifier = Mathf.Min(currentState.recentEventCount * 0.1f, 0.5f);
            
            // Time-based tension
            float timeTension = Mathf.Min(currentState.timeInCurrentArea / 300f, 0.3f);

            // Combine tensions
            currentState.tensionLevel = Mathf.Lerp(
                currentState.tensionLevel,
                Mathf.Clamp01(psychologicalTension + eventModifier + timeTension),
                0.1f
            );
        }

        private bool HasSignificantStateChange(GameState previousState)
        {
            // Check for significant changes in tension
            if (Mathf.Abs(currentState.tensionLevel - previousState.tensionLevel) > 0.2f)
                return true;

            // Check for phase changes
            if (currentState.currentPhase != previousState.currentPhase)
                return true;

            // Check for significant narrative weight changes
            foreach (var weight in currentState.narrativeWeights)
            {
                if (Mathf.Abs(weight.Value - previousState.narrativeWeights[weight.Key]) > 0.15f)
                    return true;
            }

            return false;
        }

        private async Task ProcessStateTransition(GameState previousState)
        {
            var transition = new StateTransition
            {
                previousState = previousState,
                newState = currentState,
                trigger = DetermineTrigger(previousState),
                timestamp = Time.time,
                psychologicalSignificance = CalculateSignificance(previousState)
            };

            stateHistory.Add(transition);

            // Generate LLM interpretation of state change
            await GenerateStateInterpretation(transition);

            // Notify systems of state change
            eventBridge.TriggerEvent("state_changed", transition);
        }

        private string DetermineTrigger(GameState previousState)
        {
            if (currentState.currentPhase != previousState.currentPhase)
                return "phase_change";
            
            if (currentState.tensionLevel > previousState.tensionLevel + 0.2f)
                return "tension_spike";
            
            if (currentState.tensionLevel < previousState.tensionLevel - 0.2f)
                return "tension_release";
            
            return "gradual_change";
        }

        private float CalculateSignificance(GameState previousState)
        {
            float significance = 0f;
            
            // Phase change significance
            if (currentState.currentPhase != previousState.currentPhase)
                significance += 0.5f;
            
            // Tension change significance
            significance += Mathf.Abs(currentState.tensionLevel - previousState.tensionLevel);
            
            // Narrative weight change significance
            float maxWeightChange = 0f;
            foreach (var weight in currentState.narrativeWeights)
            {
                float change = Mathf.Abs(weight.Value - previousState.narrativeWeights[weight.Key]);
                maxWeightChange = Mathf.Max(maxWeightChange, change);
            }
            significance += maxWeightChange;

            return Mathf.Clamp01(significance);
        }

        private async Task GenerateStateInterpretation(StateTransition transition)
        {
            string prompt = $"Interpret this psychological state transition:\n" +
                           $"Trigger: {transition.trigger}\n" +
                           $"Previous Tension: {transition.previousState.tensionLevel}\n" +
                           $"New Tension: {transition.newState.tensionLevel}\n" +
                           $"Psychological Significance: {transition.psychologicalSignificance}\n" +
                           $"Time in Current Phase: {currentState.timeInCurrentPhase}\n\n" +
                           "Generate a psychological interpretation that can inform future content generation.";

            string interpretation = await llmManager.GenerateResponse(prompt, "state_analysis");
            
            // Store interpretation in dynamic variables for future reference
            currentState.dynamicVariables["last_state_interpretation"] = interpretation;
        }

        private async Task UpdateNarrativeWeights()
        {
            var profile = GameManager.Instance.ProfileManager.CurrentProfile;
            
            string prompt = $"Analyze the current psychological state to determine narrative focus:\n" +
                           $"Fear Level: {profile.FearLevel}\n" +
                           $"Obsession Level: {profile.ObsessionLevel}\n" +
                           $"Aggression Level: {profile.AggressionLevel}\n" +
                           $"Current Phase: {currentState.currentPhase}\n" +
                           $"Tension: {currentState.tensionLevel}\n\n" +
                           "Suggest weight adjustments for these narrative aspects: psychological, supernatural, existential, cosmic";

            string response = await llmManager.GenerateResponse(prompt, "narrative_weight_analysis");
            
            // Parse response and update weights
            ParseAndUpdateWeights(response);
        }

        private void ParseAndUpdateWeights(string response)
        {
            try
            {
                var lines = response.Split('\n');
                var newWeights = new Dictionary<string, float>();
                float totalWeight = 0f;

                foreach (var line in lines)
                {
                    if (line.Contains(":"))
                    {
                        var parts = line.Split(':');
                        string aspect = parts[0].Trim().ToLower();
                        if (float.TryParse(parts[1].Trim(), out float weight))
                        {
                            newWeights[aspect] = weight;
                            totalWeight += weight;
                        }
                    }
                }

                if (totalWeight > 0)
                {
                    // Normalize and update weights
                    foreach (var weight in newWeights)
                    {
                        currentState.narrativeWeights[weight.Key] = weight.Value / totalWeight;
                    }

                    // Update dynamic variables for tracking
                    currentState.dynamicVariables["dominant_narrative"] = 
                        newWeights.OrderByDescending(w => w.Value).First().Key;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error parsing narrative weights: {ex.Message}");
            }
        }

        public async Task<bool> ShouldProgressLevel(Level currentLevel)
        {
            try
            {
                float completion = currentLevel.GetCompletionPercentage();
                var profile = GameManager.Instance.ProfileManager.CurrentProfile;
                
                string prompt = $"Analyze if psychological state warrants level progression:\n" +
                              $"Level Completion: {completion}\n" +
                              $"Current Phase: {currentState.currentPhase}\n" +
                              $"Tension: {currentState.tensionLevel}\n" +
                              $"Fear: {profile.FearLevel}\n" +
                              $"Obsession: {profile.ObsessionLevel}\n" +
                              $"Aggression: {profile.AggressionLevel}\n" +
                              $"Level Theme: {currentLevel.Theme}";

                string response = await llmManager.GenerateResponse(prompt, "progression_analysis");
                return response.ToLower().Contains("progress") || response.ToLower().Contains("advance");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error analyzing level progression: {ex.Message}");
                return false;
            }
        }

        public async Task<string> GeneratePhaseTransitionDescription(string oldPhase, string newPhase)
        {
            string prompt = $"Generate psychological description for phase transition:\n" +
                           $"From: {oldPhase}\n" +
                           $"To: {newPhase}\n" +
                           $"Current Tension: {currentState.tensionLevel}\n" +
                           $"Time in Previous Phase: {currentState.timeInCurrentPhase}\n" +
                           $"Dominant Narrative: {GetDynamicVariable<string>("dominant_narrative", "psychological")}";

            return await llmManager.GenerateResponse(prompt, "phase_transition");
        }

        public void UpdateFromLevelState(Level level)
        {
            // Update tension based on level psychological intensity
            currentState.tensionLevel = Mathf.Lerp(
                currentState.tensionLevel,
                level.PsychologicalIntensity,
                0.3f
            );

            // Merge thematic weights
            foreach (var weight in level.ThematicWeights)
            {
                if (currentState.narrativeWeights.ContainsKey(weight.Key))
                {
                    currentState.narrativeWeights[weight.Key] = Mathf.Lerp(
                        currentState.narrativeWeights[weight.Key],
                        weight.Value,
                        0.2f
                    );
                }
            }

            // Update dynamic variables
            currentState.dynamicVariables["current_level_theme"] = level.Theme;
            currentState.dynamicVariables["level_completion"] = level.GetCompletionPercentage();
            currentState.dynamicVariables["visited_rooms"] = level.Rooms.Count(r => r.Value.IsVisited);
        }

        public GameState GetCurrentState()
        {
            return currentState;
        }

        public void RecordEvent(string eventType)
        {
            // Push current state to undo stack
            PushState();

            // Update core state
            currentState.recentEventCount++;
            currentState.timeSinceLastEvent = 0f;

            // Update event history
            if (!currentState.eventHistory.ContainsKey(eventType))
            {
                currentState.eventHistory[eventType] = 0;
            }
            currentState.eventHistory[eventType]++;

            // Update dynamic variables
            currentState.dynamicVariables[$"last_{eventType}_event_time"] = Time.time;
            if (!currentState.dynamicVariables.ContainsKey($"{eventType}_event_count"))
                currentState.dynamicVariables[$"{eventType}_event_count"] = 0;
            currentState.dynamicVariables[$"{eventType}_event_count"] = 
                ((int)currentState.dynamicVariables[$"{eventType}_event_count"]) + 1;

            CheckCriticalState();
        }

        private void PushState()
        {
            stateUndoStack.Push(currentState.Clone());
            if (stateUndoStack.Count > MAX_UNDO_SIZE)
            {
                var oldStates = new Stack<GameState>();
                while (stateUndoStack.Count > MAX_UNDO_SIZE)
                {
                    oldStates.Push(stateUndoStack.Pop());
                }
            }
        }

        public void UndoLastState()
        {
            if (stateUndoStack.Count > 0)
            {
                currentState = stateUndoStack.Pop();
            }
        }

        private void CheckCriticalState()
        {
            // Check various conditions for critical state
            bool hasTooManyEvents = currentState.recentEventCount >= 5;
            bool highTension = currentState.tensionLevel > 0.8f;
            bool rapidEvents = Time.time - currentState.timeSinceLastEvent < 10f;

            currentState.isInCriticalState = hasTooManyEvents || (highTension && rapidEvents);
            
            if (currentState.isInCriticalState)
            {
                GameManager.Instance.TensionManager.OnHighTension("critical_state");
            }
        }

        public void TransitionToPhase(string newPhase)
        {
            string oldPhase = currentState.currentPhase;
            currentState.currentPhase = newPhase;
            currentState.timeInCurrentPhase = 0f;
            
            eventBridge.TriggerEvent("phase_changed", new { oldPhase, newPhase });
        }

        public List<StateTransition> GetRecentHistory(int count = 5)
        {
            return stateHistory.GetRange(
                Mathf.Max(0, stateHistory.Count - count),
                Mathf.Min(count, stateHistory.Count)
            );
        }

        public void SetDynamicVariable(string key, object value)
        {
            currentState.dynamicVariables[key] = value;
        }

        public T GetDynamicVariable<T>(string key, T defaultValue = default)
        {
            if (currentState.dynamicVariables.TryGetValue(key, out object value))
            {
                if (value is T typedValue)
                    return typedValue;
            }
            return defaultValue;
        }

        public void ResetState()
        {
            InitializeState();
        }

        #region State Persistence

        // Save game state to PlayerPrefs
        public void SaveGameState()
        {
            // Save basic state
            PlayerPrefs.SetString("CurrentPhase", currentState.currentPhase);
            PlayerPrefs.SetFloat("TensionLevel", currentState.tensionLevel);
            PlayerPrefs.SetFloat("TimeInCurrentArea", currentState.timeInCurrentArea);
            PlayerPrefs.SetFloat("TimeInCurrentPhase", currentState.timeInCurrentPhase);
            
            // Save narrative weights
            string weightsJson = JsonUtility.ToJson(new SerializableDictionary<float>(currentState.narrativeWeights));
            PlayerPrefs.SetString("NarrativeWeights", weightsJson);
            
            // Save dynamic variables (only serializable ones)
            var serializableVars = new Dictionary<string, string>();
            foreach (var pair in currentState.dynamicVariables)
            {
                if (pair.Value != null)
                {
                    serializableVars[pair.Key] = JsonUtility.ToJson(pair.Value);
                }
            }
            string varsJson = JsonUtility.ToJson(new SerializableDictionary<string>(serializableVars));
            PlayerPrefs.SetString("DynamicVariables", varsJson);
            
            PlayerPrefs.Save();
        }

        // Load game state from PlayerPrefs
        public void LoadGameState()
        {
            // Load basic state
            currentState.currentPhase = PlayerPrefs.GetString("CurrentPhase", "introduction");
            currentState.tensionLevel = PlayerPrefs.GetFloat("TensionLevel", 0f);
            currentState.timeInCurrentArea = PlayerPrefs.GetFloat("TimeInCurrentArea", 0f);
            currentState.timeInCurrentPhase = PlayerPrefs.GetFloat("TimeInCurrentPhase", 0f);
            
            // Load narrative weights
            string weightsJson = PlayerPrefs.GetString("NarrativeWeights", "{}");
            var loadedWeights = JsonUtility.FromJson<SerializableDictionary<float>>(weightsJson);
            if (loadedWeights != null && loadedWeights.dictionary != null)
            {
                foreach (var pair in loadedWeights.dictionary)
                {
                    currentState.narrativeWeights[pair.Key] = pair.Value;
                }
            }
            
            // Load dynamic variables
            string varsJson = PlayerPrefs.GetString("DynamicVariables", "{}");
            var loadedVars = JsonUtility.FromJson<SerializableDictionary<string>>(varsJson);
            if (loadedVars != null && loadedVars.dictionary != null)
            {
                foreach (var pair in loadedVars.dictionary)
                {
                    try
                    {
                        currentState.dynamicVariables[pair.Key] = JsonUtility.FromJson<object>(pair.Value);
                    }
                    catch
                    {
                        Debug.LogWarning($"Failed to deserialize dynamic variable: {pair.Key}");
                    }
                }
            }
        }

        #endregion

        #region Serialization Helper

        [System.Serializable]
        private class SerializableDictionary<T>
        {
            public Dictionary<string, T> dictionary;

            public SerializableDictionary(Dictionary<string, T> dict)
            {
                dictionary = dict;
            }
        }

        #endregion
    }
}