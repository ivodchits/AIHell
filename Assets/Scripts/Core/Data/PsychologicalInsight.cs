using UnityEngine;

namespace AIHell.Core.Data
{
    [System.Serializable]
    public class PsychologicalInsight
    {
        public string description;
        public float confidence;
        public string[] evidence;
        public float timestamp;
        public string[] implications;
        public bool isActive;

        public PsychologicalInsight()
        {
            evidence = new string[0];
            implications = new string[0];
            isActive = true;
            timestamp = Time.time;
        }
    }
}