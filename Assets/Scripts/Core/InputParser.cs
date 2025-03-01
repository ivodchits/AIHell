using UnityEngine;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

public class InputParser
{
    private Dictionary<string, float> emotionalKeywords;
    private Dictionary<string, string> synonymMap;
    private HashSet<string> aggressiveWords;
    private HashSet<string> anxiousWords;
    private HashSet<string> obsessiveWords;

    public InputParser()
    {
        InitializeKeywordDictionaries();
    }

    private void InitializeKeywordDictionaries()
    {
        // Emotional weight of different keywords
        emotionalKeywords = new Dictionary<string, float>
        {
            // Aggressive words
            {"attack", 0.8f}, {"break", 0.6f}, {"smash", 0.8f}, {"destroy", 0.9f},
            {"kill", 1.0f}, {"fight", 0.7f}, {"punch", 0.6f},
            
            // Anxious words
            {"run", 0.6f}, {"hide", 0.7f}, {"escape", 0.8f}, {"flee", 0.9f},
            {"avoid", 0.5f}, {"leave", 0.4f},
            
            // Obsessive words
            {"check", 0.4f}, {"examine", 0.3f}, {"inspect", 0.3f}, {"study", 0.4f},
            {"watch", 0.5f}, {"observe", 0.4f}, {"repeat", 0.6f}
        };

        // Synonym mapping for command normalization
        synonymMap = new Dictionary<string, string>
        {
            {"look", "examine"}, {"view", "examine"}, {"see", "examine"},
            {"attack", "fight"}, {"hit", "fight"}, {"strike", "fight"},
            {"go", "move"}, {"walk", "move"}, {"run", "move"},
            {"grab", "take"}, {"get", "take"}, {"pickup", "take"}
        };

        // Categorized word sets
        aggressiveWords = new HashSet<string> { "attack", "break", "smash", "destroy", "kill", "fight", "punch" };
        anxiousWords = new HashSet<string> { "run", "hide", "escape", "flee", "avoid", "leave" };
        obsessiveWords = new HashSet<string> { "check", "examine", "inspect", "study", "watch", "observe", "repeat" };
    }

    public (string command, string target, float emotionalIntensity) ParseInput(string input)
    {
        input = input.ToLower().Trim();
        string[] parts = input.Split(new[] { ' ' }, 2);
        
        string command = parts[0];
        string target = parts.Length > 1 ? parts[1] : string.Empty;
        
        // Normalize command through synonym mapping
        if (synonymMap.ContainsKey(command))
        {
            command = synonymMap[command];
        }

        // Calculate emotional intensity
        float intensity = CalculateEmotionalIntensity(input);
        
        return (command, target, intensity);
    }

    private float CalculateEmotionalIntensity(string input)
    {
        float totalIntensity = 0f;
        int matchCount = 0;
        
        foreach (var keyword in emotionalKeywords)
        {
            if (input.Contains(keyword.Key))
            {
                totalIntensity += keyword.Value;
                matchCount++;
            }
        }

        return matchCount > 0 ? totalIntensity / matchCount : 0f;
    }

    public CommandAnalysis AnalyzeCommand(string input)
    {
        var analysis = new CommandAnalysis();
        input = input.ToLower();

        // Analyze word patterns
        analysis.AggressionLevel = CalculateCategoryLevel(input, aggressiveWords);
        analysis.AnxietyLevel = CalculateCategoryLevel(input, anxiousWords);
        analysis.ObsessionLevel = CalculateCategoryLevel(input, obsessiveWords);

        // Detect repetitive patterns
        analysis.IsRepetitive = DetectRepetition(input);

        // Analyze sentence structure
        analysis.IsForceful = input.Contains('!') || ContainsForcefulWords(input);
        analysis.IsHesitant = input.Contains('?') || ContainsHesitantWords(input);

        return analysis;
    }

    private float CalculateCategoryLevel(string input, HashSet<string> categoryWords)
    {
        int matches = categoryWords.Count(word => input.Contains(word));
        return matches > 0 ? (float)matches / categoryWords.Count : 0f;
    }

    private bool DetectRepetition(string input)
    {
        // Check for repeated words
        var words = input.Split(' ');
        var wordCounts = new Dictionary<string, int>();
        
        foreach (var word in words)
        {
            if (!wordCounts.ContainsKey(word))
                wordCounts[word] = 0;
            wordCounts[word]++;
            
            if (wordCounts[word] > 2)
                return true;
        }
        
        return false;
    }

    private bool ContainsForcefulWords(string input)
    {
        string[] forcefulWords = { "must", "need", "now", "immediately", "demand" };
        return forcefulWords.Any(word => input.Contains(word));
    }

    private bool ContainsHesitantWords(string input)
    {
        string[] hesitantWords = { "maybe", "perhaps", "might", "try", "attempt" };
        return hesitantWords.Any(word => input.Contains(word));
    }

    public List<string> ExtractKeywords(string input)
    {
        var keywords = new List<string>();
        var words = input.ToLower().Split(' ');
        
        foreach (var word in words)
        {
            if (emotionalKeywords.ContainsKey(word) || 
                IsSignificantWord(word))
            {
                keywords.Add(word);
            }
        }
        
        return keywords;
    }

    private bool IsSignificantWord(string word)
    {
        // Filter out common articles, prepositions, etc.
        string[] insignificantWords = { "the", "a", "an", "in", "on", "at", "to", "for", "of", "with" };
        return !insignificantWords.Contains(word) && word.Length > 2;
    }
}

public class CommandAnalysis
{
    public float AggressionLevel { get; set; }
    public float AnxietyLevel { get; set; }
    public float ObsessionLevel { get; set; }
    public bool IsRepetitive { get; set; }
    public bool IsForceful { get; set; }
    public bool IsHesitant { get; set; }
}