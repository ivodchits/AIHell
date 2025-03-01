using UnityEngine;
using System.Collections.Generic;
using System;
using AIHell.Core.Data;

public class AchievementSystem : MonoBehaviour
{
    [System.Serializable]
    public class Achievement
    {
        public string id;
        public string title;
        public string description;
        public bool isSecret;
        public bool isUnlocked;
        public DateTime? unlockTime;
        public string unlockedMessage;

        public Achievement(string id, string title, string description, bool isSecret = false)
        {
            this.id = id;
            this.title = title;
            this.description = description;
            this.isSecret = isSecret;
            this.isUnlocked = false;
            this.unlockTime = null;
        }
    }

    private Dictionary<string, Achievement> achievements;
    private const string ACHIEVEMENTS_KEY = "AIHell_Achievements";

    private void Awake()
    {
        InitializeAchievements();
    }

    private void InitializeAchievements()
    {
        achievements = new Dictionary<string, Achievement>
        {
            // Psychological milestone achievements
            { "paranoia_peak", new Achievement(
                "paranoia_peak",
                "Paranoid Thoughts",
                "Experience extreme paranoia for the first time",
                true
            )},
            { "obsession_manifest", new Achievement(
                "obsession_manifest",
                "Manifest Obsession",
                "Develop a strong obsession with a specific element",
                true
            )},
            { "fear_master", new Achievement(
                "fear_master",
                "Master of Fear",
                "Maintain high fear levels throughout an entire level",
                true
            )},
            { "reality_break", new Achievement(
                "reality_break",
                "Reality Breaker",
                "Experience severe room distortions in a single session",
                true
            )},
            { "pattern_seeker", new Achievement(
                "pattern_seeker",
                "Pattern Seeker",
                "Identify recurring patterns in your descent",
                true
            )},
            { "deep_insight", new Achievement(
                "deep_insight",
                "Deep Insight",
                "Uncover hidden truths about your psychological state",
                true
            )}
        };

        LoadAchievements();
    }

    public void CheckPsychologicalAchievements(PlayerAnalysisProfile profile)
    {
        // Check paranoia achievement
        if (profile.FearLevel >= 0.9f && !IsAchievementUnlocked("paranoia_peak"))
        {
            UnlockAchievement("paranoia_peak", "Your paranoia reaches new heights...");
        }

        // Check obsession achievement
        if (profile.ObsessionLevel >= 0.8f && !IsAchievementUnlocked("obsession_manifest"))
        {
            UnlockAchievement("obsession_manifest", "Your obsessions begin to manifest in reality...");
        }

        // Check pattern recognition
        var patterns = profile.GetBehaviorPatterns();
        if (patterns.Count >= 5 && !IsAchievementUnlocked("pattern_seeker"))
        {
            UnlockAchievement("pattern_seeker", "You begin to see patterns everywhere...");
        }

        // Check sustained fear
        if (profile.FearLevel >= 0.7f)
        {
            UnlockAchievement("fear_master", "Fear becomes your constant companion...");
        }
    }

    public void CheckRoomDistortionAchievement(int distortionCount)
    {
        if (distortionCount >= 5 && !IsAchievementUnlocked("reality_break"))
        {
            UnlockAchievement("reality_break", "Reality begins to crack around you...");
        }
    }

    public void UnlockAchievement(string id, string unlockMessage = null)
    {
        if (achievements.TryGetValue(id, out Achievement achievement) && !achievement.isUnlocked)
        {
            achievement.isUnlocked = true;
            achievement.unlockTime = DateTime.Now;
            achievement.unlockedMessage = unlockMessage ?? achievement.description;

            // Display achievement notification
            DisplayAchievementUnlock(achievement);

            // Save achievements
            SaveAchievements();

            // Trigger any achievement-specific effects
            TriggerAchievementEffects(achievement);
        }
    }

    private void DisplayAchievementUnlock(Achievement achievement)
    {
        string message = achievement.isSecret ? 
            $"Hidden Achievement Unlocked: {achievement.title}" :
            $"Achievement Unlocked: {achievement.title}";

        GameManager.Instance.UIManager.DisplayMessage($"\n<color=#FFD700>{message}</color>");
        if (!string.IsNullOrEmpty(achievement.unlockedMessage))
        {
            GameManager.Instance.UIManager.DisplayMessage($"<color=#B22222>{achievement.unlockedMessage}</color>");
        }
    }

    private void TriggerAchievementEffects(Achievement achievement)
    {
        switch (achievement.id)
        {
            case "paranoia_peak":
                // Trigger paranoia visual effects
                GameManager.Instance.UIManager.GetComponent<UIEffects>()
                    .ApplyPsychologicalEffect(new PsychologicalEffect(
                        PsychologicalEffect.EffectType.ParanoiaInduction,
                        1f,
                        "Your paranoia manifests visually...",
                        "achievement_paranoia"
                    ));
                break;

            case "reality_break":
                // Trigger reality distortion effects
                GameManager.Instance.UIManager.GetComponent<UIEffects>()
                    .ApplyPsychologicalEffect(new PsychologicalEffect(
                        PsychologicalEffect.EffectType.RoomDistortion,
                        1f,
                        "Reality fractures around you...",
                        "achievement_reality_break"
                    ));
                break;
        }
    }

    private void SaveAchievements()
    {
        string jsonData = JsonUtility.ToJson(new SerializableAchievements(achievements));
        PlayerPrefs.SetString(ACHIEVEMENTS_KEY, jsonData);
        PlayerPrefs.Save();
    }

    private void LoadAchievements()
    {
        if (PlayerPrefs.HasKey(ACHIEVEMENTS_KEY))
        {
            string jsonData = PlayerPrefs.GetString(ACHIEVEMENTS_KEY);
            var loadedAchievements = JsonUtility.FromJson<SerializableAchievements>(jsonData);
            
            foreach (var achievement in loadedAchievements.achievements)
            {
                if (achievements.ContainsKey(achievement.id))
                {
                    achievements[achievement.id].isUnlocked = achievement.isUnlocked;
                    achievements[achievement.id].unlockTime = achievement.unlockTime;
                }
            }
        }
    }

    public bool IsAchievementUnlocked(string id)
    {
        return achievements.TryGetValue(id, out Achievement achievement) && achievement.isUnlocked;
    }

    public Achievement[] GetUnlockedAchievements()
    {
        return Array.FindAll(
            new List<Achievement>(achievements.Values).ToArray(),
            a => a.isUnlocked
        );
    }

    [System.Serializable]
    private class SerializableAchievements
    {
        public List<Achievement> achievements;

        public SerializableAchievements(Dictionary<string, Achievement> achievementDict)
        {
            achievements = new List<Achievement>(achievementDict.Values);
        }
    }
}