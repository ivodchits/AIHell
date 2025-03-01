using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AIHell.Core.Data
{
    public class PlayerAnalysisProfile
    {
        // Core psychological traits
        public virtual float AggressionLevel { get; set; }
        public virtual float CuriosityLevel { get; set; }
        public virtual float FearLevel { get; set; }
        public virtual float ObsessionLevel { get; set; }

        // Advanced psychological metrics
        public float ParanoiaIndex { get; private set; }
        public float RealityDistortionLevel { get; private set; }
        public float EmotionalInstability { get; private set; }
        public Dictionary<string, float> PsychologicalTriggers { get; private set; }

        // Tracking data
        private Dictionary<string, int> choiceFrequencies;
        private Dictionary<string, int> behaviorPatterns;
        private Dictionary<string, ObsessivePattern> obsessivePatterns;
        private List<string> keywordUsage;
        private Queue<BehaviorSnapshot> behaviorHistory;
        private Dictionary<string, EmotionalTrigger> emotionalTriggers;

        // Thresholds and constants
        private const float OBSESSION_THRESHOLD = 0.7f;
        private const float PARANOIA_THRESHOLD = 0.6f;
        private const float REALITY_DISTORTION_THRESHOLD = 0.8f;
        private const int PATTERN_THRESHOLD = 3;
        private const int MAX_HISTORY = 20;

        [Serializable]
        public class BehaviorSnapshot
        {
            public string action;
            public string context;
            public float fearLevel;
            public float obsessionLevel;
            public float aggressionLevel;
            public DateTime timestamp;
            public Dictionary<string, float> emotionalState;
        }

        [Serializable]
        public class EmotionalTrigger
        {
            public string trigger;
            public float intensity;
            public int occurrences;
            public DateTime lastTriggered;
            public List<string> associatedPatterns;
        }

        public PlayerAnalysisProfile()
        {
            InitializeProfile();
        }

        private void InitializeProfile()
        {
            // Initialize trait levels
            AggressionLevel = 0f;
            CuriosityLevel = 0f;
            FearLevel = 0f;
            ObsessionLevel = 0f;
            ParanoiaIndex = 0f;
            RealityDistortionLevel = 0f;
            EmotionalInstability = 0f;

            // Initialize tracking collections
            choiceFrequencies = new Dictionary<string, int>();
            behaviorPatterns = new Dictionary<string, int>();
            obsessivePatterns = new Dictionary<string, ObsessivePattern>();
            keywordUsage = new List<string>();
            behaviorHistory = new Queue<BehaviorSnapshot>();
            emotionalTriggers = new Dictionary<string, EmotionalTrigger>();
            PsychologicalTriggers = new Dictionary<string, float>();

            InitializePsychologicalTriggers();
        }

        private void InitializePsychologicalTriggers()
        {
            PsychologicalTriggers["isolation"] = 0.3f;
            PsychologicalTriggers["paranoia"] = 0.3f;
            PsychologicalTriggers["unreality"] = 0.2f;
            PsychologicalTriggers["observation"] = 0.4f;
            PsychologicalTriggers["reflection"] = 0.3f;
        }

        public void TrackChoice(string choiceType, string target)
        {
            // Update choice frequencies
            string key = choiceType.ToLower();
            if (!choiceFrequencies.ContainsKey(key))
                choiceFrequencies[key] = 0;
            choiceFrequencies[key]++;

            // Track keywords and patterns
            if (!string.IsNullOrEmpty(target))
            {
                keywordUsage.Add(target.ToLower());
                TrackKeywordPattern(target.ToLower());
            }

            // Create behavior snapshot
            RecordBehaviorSnapshot(choiceType, target);

            // Update psychological metrics
            UpdatePsychologicalMetrics(choiceType, target);
        }

        private void RecordBehaviorSnapshot(string action, string context)
        {
            var snapshot = new BehaviorSnapshot
            {
                action = action,
                context = context,
                fearLevel = FearLevel,
                obsessionLevel = ObsessionLevel,
                aggressionLevel = AggressionLevel,
                timestamp = DateTime.Now,
                emotionalState = new Dictionary<string, float>
                {
                    { "paranoia", ParanoiaIndex },
                    { "reality_distortion", RealityDistortionLevel },
                    { "emotional_instability", EmotionalInstability }
                }
            };

            behaviorHistory.Enqueue(snapshot);
            while (behaviorHistory.Count > MAX_HISTORY)
                behaviorHistory.Dequeue();
        }

        private void UpdatePsychologicalMetrics(string choiceType, string context)
        {
            // Update paranoia based on observation and caution choices
            if (choiceType.Contains("observe") || choiceType.Contains("caution"))
            {
                ParanoiaIndex = Mathf.Lerp(ParanoiaIndex, ParanoiaIndex + 0.1f, 0.3f);
                if (ParanoiaIndex > PARANOIA_THRESHOLD)
                {
                    PsychologicalTriggers["paranoia"] += 0.1f;
                }
            }

            // Update reality distortion based on unusual or impossible observations
            if (context.Contains("impossible") || context.Contains("unreal"))
            {
                RealityDistortionLevel = Mathf.Lerp(RealityDistortionLevel, RealityDistortionLevel + 0.15f, 0.4f);
                if (RealityDistortionLevel > REALITY_DISTORTION_THRESHOLD)
                {
                    PsychologicalTriggers["unreality"] += 0.15f;
                }
            }

            // Update emotional instability based on rapid trait changes
            if (behaviorHistory.Count >= 2)
            {
                var recentSnapshots = behaviorHistory.TakeLast(2).ToArray();
                float emotionalVariance = CalculateEmotionalVariance(recentSnapshots);
                EmotionalInstability = Mathf.Lerp(EmotionalInstability, emotionalVariance, 0.2f);
            }

            // Normalize psychological triggers
            NormalizePsychologicalTriggers();
        }

        private float CalculateEmotionalVariance(BehaviorSnapshot[] snapshots)
        {
            if (snapshots.Length < 2) return 0f;

            float fearVariance = Mathf.Abs(snapshots[1].fearLevel - snapshots[0].fearLevel);
            float obsessionVariance = Mathf.Abs(snapshots[1].obsessionLevel - snapshots[0].obsessionLevel);
            float aggressionVariance = Mathf.Abs(snapshots[1].aggressionLevel - snapshots[0].aggressionLevel);

            return (fearVariance + obsessionVariance + aggressionVariance) / 3f;
        }

        private void NormalizePsychologicalTriggers()
        {
            float total = PsychologicalTriggers.Values.Sum();
            if (total > 0)
            {
                foreach (var trigger in PsychologicalTriggers.Keys.ToList())
                {
                    PsychologicalTriggers[trigger] /= total;
                }
            }
        }

        public void TrackEmotionalTrigger(string trigger, float intensity)
        {
            if (!emotionalTriggers.ContainsKey(trigger))
            {
                emotionalTriggers[trigger] = new EmotionalTrigger
                {
                    trigger = trigger,
                    intensity = intensity,
                    occurrences = 1,
                    lastTriggered = DateTime.Now,
                    associatedPatterns = new List<string>()
                };
            }
            else
            {
                var existing = emotionalTriggers[trigger];
                existing.intensity = Mathf.Lerp(existing.intensity, intensity, 0.3f);
                existing.occurrences++;
                existing.lastTriggered = DateTime.Now;
            }

            // Update psychological triggers based on emotional response
            if (intensity > 0.7f)
            {
                foreach (var pTrigger in PsychologicalTriggers.Keys.ToList())
                {
                    if (trigger.Contains(pTrigger))
                    {
                        PsychologicalTriggers[pTrigger] = Mathf.Lerp(
                            PsychologicalTriggers[pTrigger],
                            PsychologicalTriggers[pTrigger] + 0.2f,
                            intensity
                        );
                    }
                }
                NormalizePsychologicalTriggers();
            }
        }

        public Dictionary<string, int> GetChoiceFrequencies()
        {
            return new Dictionary<string, int>(choiceFrequencies);
        }

        public List<string> GetKeywordUsage()
        {
            return new List<string>(keywordUsage);
        }

        public void TrackBehaviorPattern(string pattern)
        {
            if (!behaviorPatterns.ContainsKey(pattern))
            {
                behaviorPatterns[pattern] = 0;
            }
            behaviorPatterns[pattern]++;
        }

        private void TrackKeywordPattern(string keyword)
        {
            if (!obsessivePatterns.ContainsKey(keyword))
            {
                obsessivePatterns[keyword] = new ObsessivePattern { Keyword = keyword };
            }
            obsessivePatterns[keyword].Occurrences++;
        }

        public void AddObsessivePattern(string pattern, int count)
        {
            if (!behaviorPatterns.ContainsKey(pattern))
            {
                behaviorPatterns[pattern] = 0;
            }
            behaviorPatterns[pattern] += count;

            // Also track as a keyword pattern if it's frequent enough
            if (count > PATTERN_THRESHOLD)
            {
                TrackKeywordPattern(pattern);
            }
        }

        public ObsessivePattern[] GetActiveObsessions()
        {
            var active = new List<ObsessivePattern>();
            foreach (var pattern in obsessivePatterns.Values)
            {
                if (pattern.Occurrences >= PATTERN_THRESHOLD)
                {
                    active.Add(pattern);
                }
            }
            return active.ToArray();
        }

        public Dictionary<string, int> GetBehaviorPatterns()
        {
            return new Dictionary<string, int>(behaviorPatterns);
        }

        public BehaviorSnapshot[] GetRecentBehavior(int count = 5)
        {
            return behaviorHistory.TakeLast(count).ToArray();
        }

        public Dictionary<string, EmotionalTrigger> GetEmotionalTriggers()
        {
            return new Dictionary<string, EmotionalTrigger>(emotionalTriggers);
        }

        public float GetTriggerSensitivity(string trigger)
        {
            return PsychologicalTriggers.TryGetValue(trigger, out float sensitivity) ? sensitivity : 0f;
        }

        public void DecayPsychologicalMetrics(float deltaTime)
        {
            // Natural decay of psychological states
            ParanoiaIndex = Mathf.Max(0f, ParanoiaIndex - 0.05f * deltaTime);
            RealityDistortionLevel = Mathf.Max(0f, RealityDistortionLevel - 0.03f * deltaTime);
            EmotionalInstability = Mathf.Max(0f, EmotionalInstability - 0.04f * deltaTime);

            // Decay psychological triggers
            foreach (var trigger in PsychologicalTriggers.Keys.ToList())
            {
                PsychologicalTriggers[trigger] = Mathf.Max(0.1f, 
                    PsychologicalTriggers[trigger] - 0.02f * deltaTime);
            }
            NormalizePsychologicalTriggers();
        }
    }

    [System.Serializable]
    public class ObsessivePattern
    {
        public string Keyword { get; set; }
        public int Occurrences { get; set; }
    }
}