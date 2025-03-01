using UnityEngine;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AIHell.Core.Data;

public class GameStateManager : MonoBehaviour
{
    [System.Serializable]
    public class GameState
    {
        public float tensionLevel = 0f;
        public int recentEventCount = 0;
        public Dictionary<string, int> eventHistory = new Dictionary<string, int>();
        public float lastEventTime = 0f;
        public bool isInCriticalState = false;
        
        public GameState Clone()
        {
            return new GameState {
                tensionLevel = this.tensionLevel,
                recentEventCount = this.recentEventCount,
                eventHistory = new Dictionary<string, int>(this.eventHistory),
                lastEventTime = this.lastEventTime,
                isInCriticalState = this.isInCriticalState
            };
        }
    }

    private GameState currentState;
    private Stack<GameState> stateHistory;
    private const int MAX_HISTORY_SIZE = 10;
    private const float EVENT_DECAY_TIME = 30f;

    private void Awake()
    {
        InitializeState();
    }

    private void InitializeState()
    {
        currentState = new GameState();
        stateHistory = new Stack<GameState>();
    }

    public void RecordEvent(string eventType)
    {
        // Push current state to history
        PushState();

        // Update event count and history
        currentState.recentEventCount++;
        if (!currentState.eventHistory.ContainsKey(eventType))
        {
            currentState.eventHistory[eventType] = 0;
        }
        currentState.eventHistory[eventType]++;

        // Update timing
        currentState.lastEventTime = Time.time;

        // Check for critical state
        CheckCriticalState();
    }

    private void PushState()
    {
        stateHistory.Push(currentState.Clone());
        if (stateHistory.Count > MAX_HISTORY_SIZE)
        {
            var oldStates = new Stack<GameState>();
            while (stateHistory.Count > MAX_HISTORY_SIZE)
            {
                oldStates.Push(stateHistory.Pop());
            }
        }
    }

    public void UndoLastState()
    {
        if (stateHistory.Count > 0)
        {
            currentState = stateHistory.Pop();
        }
    }

    private void CheckCriticalState()
    {
        // Check various conditions for critical state
        bool hasTooManyEvents = currentState.recentEventCount >= 5;
        bool highTension = currentState.tensionLevel > 0.8f;
        bool rapidEvents = Time.time - currentState.lastEventTime < 10f;

        currentState.isInCriticalState = hasTooManyEvents || (highTension && rapidEvents);
    }

    public void ModifyTension(float amount, string source)
    {
        PushState();
        currentState.tensionLevel = Mathf.Clamp01(currentState.tensionLevel + amount);
        
        if (currentState.tensionLevel > 0.8f)
        {
            GameManager.Instance.TensionManager.OnHighTension(source);
        }
    }

    private void Update()
    {
        // Decay recent event count over time
        if (Time.time - currentState.lastEventTime > EVENT_DECAY_TIME)
        {
            currentState.recentEventCount = Mathf.Max(0, currentState.recentEventCount - 1);
        }

        // Natural tension decay
        if (currentState.tensionLevel > 0)
        {
            currentState.tensionLevel = Mathf.Max(0, currentState.tensionLevel - Time.deltaTime * 0.05f);
        }
    }

    public GameState GetCurrentState()
    {
        return currentState;
    }

    public void ResetState()
    {
        InitializeState();
    }
}