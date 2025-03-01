using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using AIHell.Core.Data;

public class SymbologySystem : MonoBehaviour
{
    [System.Serializable]
    public class Symbol
    {
        public string name;
        public string baseDescription;
        public string[] variations;
        public float psychologicalWeight;
        public string[] associatedThemes;
        public string[] triggerWords;
        public bool isRevealed;
        public int occurrences;

        public Symbol(string name, string baseDesc, float weight, string[] themes)
        {
            this.name = name;
            baseDescription = baseDesc;
            psychologicalWeight = weight;
            associatedThemes = themes;
            isRevealed = false;
            occurrences = 0;
        }
    }

    private Dictionary<string, Symbol> activeSymbols;
    private Dictionary<string, float> themeWeights;
    private List<string> symbolHistory;
    private const int MAX_HISTORY = 20;

    private void Awake()
    {
        InitializeSymbology();
    }

    private void InitializeSymbology()
    {
        activeSymbols = new Dictionary<string, Symbol>();
        themeWeights = new Dictionary<string, float>();
        symbolHistory = new List<string>();

        // Initialize base psychological symbols
        InitializeBaseSymbols();
    }

    private void InitializeBaseSymbols()
    {
        // Level 1 - Surface Anxiety Symbols
        AddSymbol(new Symbol("Mirror", 
            "A mirror that shows more than just reflections",
            0.5f,
            new[] { "identity", "self-reflection", "distortion" })
        {
            variations = new[] {
                "The mirror surface ripples like water when you look away",
                "Your reflection seems delayed by a fraction of a second",
                "The mirror shows a version of you that feels wrong"
            },
            triggerWords = new[] { "mirror", "reflection", "glass", "surface" }
        });

        AddSymbol(new Symbol("Clock",
            "A clock that measures more than time",
            0.6f,
            new[] { "time", "pressure", "inevitability" })
        {
            variations = new[] {
                "The clock ticks irregularly, like a failing heart",
                "The hands move in impossible directions",
                "Time seems to flow differently around the clock"
            },
            triggerWords = new[] { "clock", "time", "ticking", "hands" }
        });

        // Level 2 - Deep Isolation Symbols
        AddSymbol(new Symbol("Door",
            "A door that may lead nowhere",
            0.7f,
            new[] { "choice", "transition", "escape" })
        {
            variations = new[] {
                "The door seems to breathe slightly",
                "Whispers emerge from behind the door",
                "The door frame contains impossible angles"
            },
            triggerWords = new[] { "door", "entrance", "exit", "threshold" }
        });

        // Level 3 - Existential Dread Symbols
        AddSymbol(new Symbol("Void",
            "An absence that feels alive",
            0.8f,
            new[] { "emptiness", "nihilism", "cosmic horror" })
        {
            variations = new[] {
                "The darkness seems to watch you",
                "The void pulses with unknowable intent",
                "Reality thins around the emptiness"
            },
            triggerWords = new[] { "void", "empty", "darkness", "nothing" }
        });
    }

    public void AddSymbol(Symbol symbol)
    {
        if (!activeSymbols.ContainsKey(symbol.name))
        {
            activeSymbols.Add(symbol.name, symbol);
            UpdateThemeWeights(symbol);
        }
    }

    public string InterpretSymbolInContext(string symbolName, PlayerAnalysisProfile profile)
    {
        if (activeSymbols.TryGetValue(symbolName, out Symbol symbol))
        {
            string description = symbol.variations != null && symbol.variations.Length > 0 ?
                symbol.variations[Random.Range(0, symbol.variations.Length)] :
                symbol.baseDescription;

            // Modify description based on psychological state
            description = EnhanceSymbolDescription(description, symbol, profile);

            // Track symbol occurrence
            TrackSymbolOccurrence(symbol);

            return description;
        }
        return string.Empty;
    }

    private string EnhanceSymbolDescription(string baseDescription, Symbol symbol, PlayerAnalysisProfile profile)
    {
        string enhanced = baseDescription;

        // Add psychological layers based on profile
        if (profile.FearLevel > 0.7f && symbol.psychologicalWeight > 0.6f)
        {
            enhanced += "\n" + GenerateFearVariation(symbol);
        }

        if (profile.ObsessionLevel > 0.6f)
        {
            enhanced += "\n" + GenerateObsessionVariation(symbol);
        }

        // Add recurring motif if symbol appears frequently
        if (symbol.occurrences > 3)
        {
            enhanced += "\n" + GenerateRecurringMotif(symbol);
        }

        return enhanced;
    }

    private string GenerateFearVariation(Symbol symbol)
    {
        string[] fearVariations = {
            $"The {symbol.name.ToLower()} seems to react to your fear...",
            $"Your dread intensifies as you focus on the {symbol.name.ToLower()}...",
            $"Something about the {symbol.name.ToLower()} feels deeply wrong..."
        };

        return fearVariations[Random.Range(0, fearVariations.Length)];
    }

    private string GenerateObsessionVariation(Symbol symbol)
    {
        string[] obsessionVariations = {
            $"You can't take your attention away from the {symbol.name.ToLower()}...",
            $"The {symbol.name.ToLower()} seems to call to you...",
            $"Your thoughts keep circling back to the {symbol.name.ToLower()}..."
        };

        return obsessionVariations[Random.Range(0, obsessionVariations.Length)];
    }

    private string GenerateRecurringMotif(Symbol symbol)
    {
        string[] motifVariations = {
            $"This {symbol.name.ToLower()} feels familiar, like you've seen it before...",
            $"There's something significant about these recurring {symbol.name.ToLower()}s...",
            $"The {symbol.name.ToLower()} seems to follow you through this place..."
        };

        return motifVariations[Random.Range(0, motifVariations.Length)];
    }

    private void TrackSymbolOccurrence(Symbol symbol)
    {
        symbol.occurrences++;
        symbol.isRevealed = true;
        
        symbolHistory.Add(symbol.name);
        if (symbolHistory.Count > MAX_HISTORY)
        {
            symbolHistory.RemoveAt(0);
        }
    }

    private void UpdateThemeWeights(Symbol symbol)
    {
        foreach (var theme in symbol.associatedThemes)
        {
            if (!themeWeights.ContainsKey(theme))
            {
                themeWeights[theme] = 0f;
            }
            themeWeights[theme] += symbol.psychologicalWeight;
        }
    }

    public Symbol[] GetActiveSymbolsByTheme(string theme)
    {
        return activeSymbols.Values
            .Where(s => s.associatedThemes.Contains(theme))
            .ToArray();
    }

    public Symbol[] GetRevealedSymbols()
    {
        return activeSymbols.Values
            .Where(s => s.isRevealed)
            .ToArray();
    }

    public string[] GetDominantThemes()
    {
        return themeWeights
            .OrderByDescending(kv => kv.Value)
            .Take(3)
            .Select(kv => kv.Key)
            .ToArray();
    }

    public bool IsSymbolRecurring(string symbolName)
    {
        return symbolHistory.Count(s => s == symbolName) >= 3;
    }

    public void ResetSymbology()
    {
        foreach (var symbol in activeSymbols.Values)
        {
            symbol.isRevealed = false;
            symbol.occurrences = 0;
        }
        symbolHistory.Clear();
    }
}