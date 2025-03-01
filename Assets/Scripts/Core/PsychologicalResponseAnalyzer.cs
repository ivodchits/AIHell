using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using AIHell.Core.Data;

namespace AIHell.Core
{
    public class PsychologicalResponseAnalyzer : MonoBehaviour
    {
        private LLMManager llmManager;
        private Dictionary<string, AIHell.Core.Data.ResponsePattern> observedPatterns;
        private Queue<PlayerResponse> responseQueue;
        private List<AIHell.Core.Data.PsychologicalInsight> insights;

        [System.Serializable]
        public class PlayerResponse
        {
            public string type;
            public float intensity;
            public string context;
            public float timestamp;
            public Dictionary<string, float> metrics;
            public string[] associatedElements;
        }

        private void Awake()
        {
            llmManager = GameManager.Instance.LLMManager;
            observedPatterns = new Dictionary<string, AIHell.Core.Data.ResponsePattern>();
            responseQueue = new Queue<PlayerResponse>();
            insights = new List<AIHell.Core.Data.PsychologicalInsight>();
        }

        public async Task RecordResponse(string responseType, float intensity, string context, string[] elements)
        {
            var response = new PlayerResponse
            {
                type = responseType,
                intensity = intensity,
                context = context,
                timestamp = Time.time,
                metrics = CalculateResponseMetrics(responseType, intensity),
                associatedElements = elements
            };

            responseQueue.Enqueue(response);

            // Process response queue when it reaches sufficient size
            if (responseQueue.Count >= 3)
            {
                await ProcessResponseQueue();
            }
        }

        private Dictionary<string, float> CalculateResponseMetrics(string responseType, float intensity)
        {
            var metrics = new Dictionary<string, float>();
            
            // Base psychological metrics
            metrics["immediate_impact"] = intensity;
            metrics["psychological_weight"] = CalculatePsychologicalWeight(responseType);
            metrics["persistence_factor"] = CalculatePersistenceFactor(responseType);
            
            // Response-specific metrics
            switch (responseType)
            {
                case "fear":
                    metrics["fight_flight"] = CalculateFightFlightResponse(intensity);
                    metrics["anxiety_level"] = intensity * 0.8f;
                    break;
                case "dread":
                    metrics["existential_impact"] = intensity * 1.2f;
                    metrics["psychological_tension"] = intensity * 0.9f;
                    break;
                case "confusion":
                    metrics["reality_distortion"] = intensity * 1.1f;
                    metrics["cognitive_dissonance"] = intensity * 0.7f;
                    break;
            }

            return metrics;
        }

        private float CalculatePsychologicalWeight(string responseType)
        {
            switch (responseType)
            {
                case "fear": return 0.8f;
                case "dread": return 0.9f;
                case "confusion": return 0.7f;
                default: return 0.5f;
            }
        }

        private float CalculatePersistenceFactor(string responseType)
        {
            switch (responseType)
            {
                case "fear": return 0.6f;
                case "dread": return 0.9f;
                case "confusion": return 0.7f;
                default: return 0.5f;
            }
        }

        private float CalculateFightFlightResponse(float intensity)
        {
            return Mathf.Clamp01(intensity * Random.Range(0.8f, 1.2f));
        }

        private async Task ProcessResponseQueue()
        {
            var responses = new List<PlayerResponse>();
            while (responseQueue.Count > 0)
            {
                responses.Add(responseQueue.Dequeue());
            }

            // Analyze response patterns
            await AnalyzeResponsePatterns(responses);
            
            // Generate psychological insights
            await GenerateInsights(responses);
            
            // Update player profile
            await UpdatePlayerProfile(responses);
        }

        private async Task AnalyzeResponsePatterns(List<PlayerResponse> responses)
        {
            string prompt = "Analyze these psychological responses for patterns:\n";
            foreach (var response in responses)
            {
                prompt += $"Type: {response.type}, Intensity: {response.intensity}\n";
                prompt += $"Context: {response.context}\n";
                prompt += $"Associated Elements: {string.Join(", ", response.associatedElements)}\n\n";
            }

            string analysis = await llmManager.GenerateResponse(prompt, "pattern_analysis");
            await ProcessPatternAnalysis(analysis);
        }

        private async Task ProcessPatternAnalysis(string analysis)
        {
            // Get structured format from LLM
            string structurePrompt = $"Structure these psychological response patterns:\n{analysis}";
            string structured = await llmManager.GenerateResponse(structurePrompt, "pattern_structuring");
            
            // Process and store patterns
            var newPatterns = ParsePatterns(structured);
            foreach (var pattern in newPatterns)
            {
                ProcessNewPattern(pattern);
            }
        }

        private void ProcessNewPattern(AIHell.Core.Data.ResponsePattern pattern)
        {
            if (!observedPatterns.ContainsKey(pattern.id))
            {
                observedPatterns[pattern.id] = pattern;
            }
            else
            {
                UpdateExistingPattern(observedPatterns[pattern.id], pattern);
            }
        }

        private List<AIHell.Core.Data.ResponsePattern> ParsePatterns(string structured)
        {
            // Implementation would parse LLM response into pattern objects
            return new List<AIHell.Core.Data.ResponsePattern>();
        }

        private void UpdateExistingPattern(AIHell.Core.Data.ResponsePattern existing, AIHell.Core.Data.ResponsePattern newPattern)
        {
            // Update frequency
            existing.frequency = Mathf.Lerp(existing.frequency, newPattern.frequency, 0.3f);
            
            // Update significance
            existing.psychologicalSignificance = Mathf.Max(
                existing.psychologicalSignificance,
                newPattern.psychologicalSignificance
            );
            
            // Merge correlations
            foreach (var correlation in newPattern.correlations)
            {
                if (!existing.correlations.ContainsKey(correlation.Key))
                {
                    existing.correlations[correlation.Key] = correlation.Value;
                }
                else
                {
                    existing.correlations[correlation.Key] = Mathf.Lerp(
                        existing.correlations[correlation.Key],
                        correlation.Value,
                        0.2f
                    );
                }
            }
        }

        private async Task GenerateInsights(List<PlayerResponse> responses)
        {
            string prompt = "Generate psychological insights from these responses and patterns:\n";
            
            // Add response context
            foreach (var response in responses)
            {
                prompt += $"Response: {response.type} (Intensity: {response.intensity})\n";
                prompt += $"Context: {response.context}\n";
                prompt += $"Metrics: {string.Join(", ", response.metrics.Select(m => $"{m.Key}: {m.Value}"))}";
            }
            
            // Add pattern context
            prompt += "\nObserved Patterns:\n";
            foreach (var pattern in observedPatterns.Values)
            {
                prompt += $"- {pattern.description} (Frequency: {pattern.frequency})\n";
            }

            string insightResponse = await llmManager.GenerateResponse(prompt, "insight_generation");
            ProcessInsights(insightResponse);
        }

        private void ProcessInsights(string insightResponse)
        {
            // Implementation would process LLM response into insights
            // This is a placeholder implementation
            var insight = new AIHell.Core.Data.PsychologicalInsight
            {
                description = insightResponse,
                confidence = 0.8f,
                evidence = new string[] { "response_pattern", "psychological_state" },
                timestamp = Time.time,
                implications = new string[] { "adjust_horror_intensity", "modify_theme_focus" },
                isActive = true
            };
            
            insights.Add(insight);
        }

        private async Task UpdatePlayerProfile(List<PlayerResponse> responses)
        {
            var profile = GameManager.Instance.ProfileManager.CurrentProfile;
            
            // Build update context
            string prompt = "Analyze how these responses should affect the psychological profile:\n";
            foreach (var response in responses)
            {
                prompt += $"Response: {response.type} (Intensity: {response.intensity})\n";
                prompt += $"Psychological Weight: {response.metrics["psychological_weight"]}\n";
                prompt += $"Context: {response.context}\n\n";
            }

            string profileUpdate = await llmManager.GenerateResponse(prompt, "profile_update");
            await ApplyProfileUpdates(profileUpdate, profile);
        }

        private async Task ApplyProfileUpdates(string updates, PlayerAnalysisProfile profile)
        {
            // Get structured updates from LLM
            string structurePrompt = $"Structure these psychological profile updates:\n{updates}";
            string structured = await llmManager.GenerateResponse(structurePrompt, "update_structuring");
            
            // Parse and apply updates
            var modifications = ParseProfileModifications(structured);
            ApplyProfileModifications(modifications, profile);
        }

        private Dictionary<string, float> ParseProfileModifications(string structured)
        {
            // Implementation would parse LLM response into profile modifications
            return new Dictionary<string, float>();
        }

        private void ApplyProfileModifications(Dictionary<string, float> modifications, PlayerAnalysisProfile profile)
        {
            foreach (var mod in modifications)
            {
                switch (mod.Key)
                {
                    case "fear":
                        profile.FearLevel = Mathf.Clamp01(profile.FearLevel + mod.Value);
                        break;
                    case "obsession":
                        profile.ObsessionLevel = Mathf.Clamp01(profile.ObsessionLevel + mod.Value);
                        break;
                    case "aggression":
                        profile.AggressionLevel = Mathf.Clamp01(profile.AggressionLevel + mod.Value);
                        break;
                }
            }
        }

        public List<AIHell.Core.Data.ResponsePattern> GetSignificantPatterns(float minSignificance = 0.5f)
        {
            return observedPatterns.Values
                .Where(p => p.psychologicalSignificance >= minSignificance)
                .OrderByDescending(p => p.psychologicalSignificance)
                .ToList();
        }

        public List<AIHell.Core.Data.PsychologicalInsight> GetActiveInsights()
        {
            return insights.Where(i => i.isActive).ToList();
        }

        public void ClearResponseQueue()
        {
            responseQueue.Clear();
        }
    }
}