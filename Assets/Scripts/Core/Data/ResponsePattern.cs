using UnityEngine;
using System.Collections.Generic;

namespace AIHell.Core.Data
{
    [System.Serializable]
    public class ResponsePattern
    {
        public string id;
        public string description;
        public float frequency;
        public float psychologicalSignificance;
        public string[] triggers;
        public Dictionary<string, float> correlations;
        public AnimationCurve intensityCurve;

        public ResponsePattern()
        {
            correlations = new Dictionary<string, float>();
            intensityCurve = new AnimationCurve();
            // Default curve with normal response pattern
            intensityCurve.AddKey(0f, 0f);
            intensityCurve.AddKey(0.5f, 0.5f);
            intensityCurve.AddKey(1f, 1f);
        }

        public ResponsePattern(string id, string description, float significance = 0.5f)
        {
            this.id = id;
            this.description = description;
            this.psychologicalSignificance = significance;
            this.frequency = 0f;
            this.triggers = new string[0];
            this.correlations = new Dictionary<string, float>();
            this.intensityCurve = new AnimationCurve();
            // Default curve with normal response pattern
            intensityCurve.AddKey(0f, 0f);
            intensityCurve.AddKey(0.5f, 0.5f);
            intensityCurve.AddKey(1f, 1f);
        }
    }
}