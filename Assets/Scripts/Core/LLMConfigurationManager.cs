using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using AIHell.Core.Data;

namespace AIHell.Core
{
    public class LLMConfigurationManager : MonoBehaviour
    {
        private Dictionary<string, LLMConfig> configurations;
        private Dictionary<string, float> performanceMetrics;
        private PsychologicalResponseAnalyzer responseAnalyzer;
        private float adaptationRate = 0.2f;
        private List<LLMConfig> cachedConfigs = new List<LLMConfig>();

        [System.Serializable]
        public class LLMConfig
        {
            public string id;
            public float temperature;
            public int maxTokens;
            public float topP;
            public float presencePenalty;
            public float frequencyPenalty;
            public string[] stopSequences;
            public Dictionary<string, float> weightings;
            public AnimationCurve adaptationCurve;
        }

        [System.Serializable]
        public class ConfigurationResult
        {
            public string configId;
            public float effectiveness;
            public Dictionary<string, float> metrics;
            public string[] observations;
        }

        private void Awake()
        {
            configurations = new Dictionary<string, LLMConfig>();
            performanceMetrics = new Dictionary<string, float>();
            responseAnalyzer = GetComponent<PsychologicalResponseAnalyzer>();
            
            InitializeConfigurations();
        }

        private void InitializeConfigurations()
        {
            // Initialize base configurations for different psychological contexts
            InitializeConfig("fear", 0.7f, new Dictionary<string, float> {
                {"immediacy", 0.8f},
                {"visceral", 0.7f},
                {"personal", 0.6f}
            });

            InitializeConfig("dread", 0.6f, new Dictionary<string, float> {
                {"atmospheric", 0.8f},
                {"psychological", 0.9f},
                {"anticipation", 0.7f}
            });

            InitializeConfig("madness", 0.8f, new Dictionary<string, float> {
                {"surreal", 0.8f},
                {"cognitive", 0.7f},
                {"reality", 0.9f}
            });

            InitializeConfig("psychological", 0.65f, new Dictionary<string, float> {
                {"subtlety", 0.8f},
                {"personal", 0.9f},
                {"emotional", 0.7f}
            });
        }

        private void InitializeConfig(string id, float baseTemp, Dictionary<string, float> weights)
        {
            var config = new LLMConfig
            {
                id = id,
                temperature = baseTemp,
                maxTokens = 2048,
                topP = 0.9f,
                presencePenalty = 0.1f,
                frequencyPenalty = 0.1f,
                stopSequences = new string[] { "\n\n", "END" },
                weightings = weights,
                adaptationCurve = GenerateAdaptationCurve()
            };

            configurations[id] = config;
        }

        private AnimationCurve GenerateAdaptationCurve()
        {
            AnimationCurve curve = new AnimationCurve();
            
            // Start conservative
            curve.AddKey(0f, 0.2f);
            
            // Gradually increase adaptation
            curve.AddKey(0.3f, 0.4f);
            curve.AddKey(0.7f, 0.8f);
            
            // Max adaptation
            curve.AddKey(1f, 1f);
            
            return curve;
        }

        public async Task<LLMConfig> GetConfiguration(string contextType, PlayerAnalysisProfile profile)
        {
            var config = cachedConfigs.Find(c => c.id == contextType);
            if (config == null)
            {
                config = await GenerateNewConfiguration(contextType, profile);
            }
            await AdaptConfiguration(config, profile);
            return config;
        }

        private async Task<LLMConfig> GenerateNewConfiguration(string contextType, PlayerAnalysisProfile profile)
        {
            string prompt = $"Generate LLM configuration parameters for context type: {contextType}\n" +
                           $"Current Fear Level: {profile.FearLevel}\n" +
                           $"Current Obsession Level: {profile.ObsessionLevel}\n" +
                           $"Current Aggression Level: {profile.AggressionLevel}\n" +
                           "Focus on psychological impact and horror effectiveness.";

            string response = await GameManager.Instance.LLMManager.GenerateResponse(prompt, "config_generation");
            var config = ParseConfiguration(response, contextType);
            cachedConfigs.Add(config);
            return config;
        }

        private LLMConfig ParseConfiguration(string response, string configId)
        {
            // Implementation would parse LLM response into configuration
            // This is a placeholder implementation
            return new LLMConfig
            {
                id = configId,
                temperature = 0.7f,
                maxTokens = 2048,
                topP = 0.9f,
                presencePenalty = 0.1f,
                frequencyPenalty = 0.1f,
                stopSequences = new string[] { "\n\n", "END" },
                weightings = new Dictionary<string, float>(),
                adaptationCurve = GenerateAdaptationCurve()
            };
        }

        private async Task AdaptConfiguration(LLMConfig config, PlayerAnalysisProfile profile)
        {
            var analyzer = GameManager.Instance.GetComponent<PsychologicalResponseAnalyzer>();
            var patterns = analyzer.GetSignificantPatterns();
            var insights = analyzer.GetActiveInsights();
            await GenerateAdaptationParameters(config, patterns, insights, profile);
        }

        private async Task GenerateAdaptationParameters(LLMConfig config, List<AIHell.Core.Data.ResponsePattern> patterns, List<AIHell.Core.Data.PsychologicalInsight> insights, PlayerAnalysisProfile profile)
        {
            // Build adaptation prompt
            string prompt = $"Generate adaptation parameters for context type: {config.id}\n" +
                        $"Current Fear Level: {profile.FearLevel}\n" +
                        $"Current Obsession Level: {profile.ObsessionLevel}\n" +
                        $"Current Aggression Level: {profile.AggressionLevel}\n\n" +
                        "Active Patterns:\n";

            foreach (var pattern in patterns)
            {
                prompt += $"- {pattern.description} (Significance: {pattern.psychologicalSignificance})\n";
            }

            prompt += "\nPsychological Insights:\n";
            foreach (var insight in insights)
            {
                prompt += $"- {insight.description} (Confidence: {insight.confidence})\n";
            }

            string response = await GameManager.Instance.LLMManager.GenerateResponse(prompt, "adaptation_generation");
            ProcessAdaptationResponse(response, config);
        }

        private void ProcessAdaptationResponse(string response, LLMConfig config)
        {
            // Implementation would process LLM response into configuration adaptations
            // This is a placeholder implementation
            
            // Adapt temperature based on psychological state
            config.temperature = Mathf.Lerp(config.temperature, 0.8f, adaptationRate);
            
            // Adjust penalties
            config.presencePenalty = Mathf.Lerp(config.presencePenalty, 0.2f, adaptationRate);
            config.frequencyPenalty = Mathf.Lerp(config.frequencyPenalty, 0.15f, adaptationRate);
        }

        private void ApplyAdaptations(LLMConfig config)
        {
            // Ensure values stay within safe bounds
            config.temperature = Mathf.Clamp(config.temperature, 0.1f, 1f);
            config.topP = Mathf.Clamp(config.topP, 0.1f, 1f);
            config.presencePenalty = Mathf.Clamp(config.presencePenalty, 0f, 2f);
            config.frequencyPenalty = Mathf.Clamp(config.frequencyPenalty, 0f, 2f);

            // Update configuration in dictionary
            configurations[config.id] = config;
        }

        public async Task<ConfigurationResult> EvaluateConfiguration(string configId, string generatedContent)
        {
            if (!configurations.TryGetValue(configId, out LLMConfig config))
            {
                return null;
            }

            // Analyze content effectiveness
            var result = new ConfigurationResult
            {
                configId = configId,
                metrics = await AnalyzeEffectiveness(config, generatedContent),
                observations = await GenerateObservations(config, generatedContent)
            };

            // Calculate overall effectiveness
            result.effectiveness = CalculateOverallEffectiveness(result.metrics);

            // Update performance metrics
            UpdatePerformanceMetrics(configId, result);

            return result;
        }

        private async Task<Dictionary<string, float>> AnalyzeEffectiveness(LLMConfig config, string content)
        {
            string prompt = "Analyze the effectiveness of this horror content:\n" +
                           $"{content}\n\n" +
                           "Generate metrics for:\n" +
                           "- Psychological impact (0-1)\n" +
                           "- Narrative coherence (0-1)\n" +
                           "- Personal relevance (0-1)\n" +
                           "- Horror effectiveness (0-1)";

            string response = await GameManager.Instance.LLMManager.GenerateResponse(prompt, "effectiveness_analysis");
            return ParseEffectivenessMetrics(response);
        }

        private Dictionary<string, float> ParseEffectivenessMetrics(string response)
        {
            // Implementation would parse LLM response into metrics
            return new Dictionary<string, float>();
        }

        private async Task<string[]> GenerateObservations(LLMConfig config, string content)
        {
            string prompt = "Generate observations about the effectiveness of these LLM parameters:\n" +
                           $"Temperature: {config.temperature}\n" +
                           $"Presence Penalty: {config.presencePenalty}\n" +
                           $"Frequency Penalty: {config.frequencyPenalty}\n\n" +
                           $"Based on generated content:\n{content}";

            string response = await GameManager.Instance.LLMManager.GenerateResponse(prompt, "observation_generation");
            return response.Split('\n');
        }

        private float CalculateOverallEffectiveness(Dictionary<string, float> metrics)
        {
            float total = 0f;
            foreach (var metric in metrics.Values)
            {
                total += metric;
            }
            return total / metrics.Count;
        }

        private void UpdatePerformanceMetrics(string configId, ConfigurationResult result)
        {
            if (!performanceMetrics.ContainsKey(configId))
            {
                performanceMetrics[configId] = result.effectiveness;
            }
            else
            {
                performanceMetrics[configId] = Mathf.Lerp(
                    performanceMetrics[configId],
                    result.effectiveness,
                    0.2f
                );
            }
        }

        public Dictionary<string, float> GetPerformanceMetrics()
        {
            return new Dictionary<string, float>(performanceMetrics);
        }

        public void ResetConfiguration(string configId)
        {
            if (configurations.ContainsKey(configId))
            {
                InitializeConfig(
                    configId,
                    0.7f,
                    new Dictionary<string, float>()
                );
            }
        }
    }
}