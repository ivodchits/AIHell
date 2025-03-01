using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

public class TimelineCoherenceSystem : MonoBehaviour
{
    private LLMManager llmManager;
    private GameStateManager gameStateManager;
    private Dictionary<string, TimelineNode> timelineNodes;
    private List<TimelineArc> timelineArcs;
    private Dictionary<string, float> temporalWeights;
    private float minCoherenceThreshold = 0.5f;

    [System.Serializable]
    public class TimelineNode
    {
        public string id;
        public string content;
        public float timestamp;
        public float psychologicalImpact;
        public List<string> thematicElements;
        public Dictionary<string, float> contextualFactors;
        public List<string> dependencies;
        public bool isResolved;
    }

    [System.Serializable]
    public class TimelineArc
    {
        public string id;
        public string description;
        public List<string> nodeIds;
        public float coherenceScore;
        public AnimationCurve progressionCurve;
        public Dictionary<string, float> psychologicalWeights;
        public bool isActive;
    }

    [System.Serializable]
    public class CoherenceResult
    {
        public bool isCoherent;
        public float coherenceScore;
        public Dictionary<string, float> temporalFactors;
        public string[] relevantArcs;
        public string[] suggestedAdditions;
    }

    private void Awake()
    {
        llmManager = GameManager.Instance.LLMManager;
        gameStateManager = GameManager.Instance.StateManager;
        timelineNodes = new Dictionary<string, TimelineNode>();
        timelineArcs = new List<TimelineArc>();
        temporalWeights = new Dictionary<string, float>();
        
        InitializeTemporalWeights();
    }

    private void InitializeTemporalWeights()
    {
        // Core temporal weights for horror progression
        temporalWeights["buildup"] = 0.7f;
        temporalWeights["revelation"] = 0.8f;
        temporalWeights["escalation"] = 0.9f;
        temporalWeights["climax"] = 1.0f;
        temporalWeights["aftermath"] = 0.6f;
        temporalWeights["psychological_echo"] = 0.8f;
        temporalWeights["thematic_recurrence"] = 0.7f;
        temporalWeights["emotional_resonance"] = 0.85f;
    }

    public async Task<CoherenceResult> AnalyzeCoherence(string content, float currentTime)
    {
        // Create temporary node
        var tempNode = await CreateTimelineNode(content, currentTime);
        
        // Find relevant timeline connections
        var connections = FindTimelineConnections(tempNode);
        
        // Calculate temporal coherence
        var coherence = await CalculateTemporalCoherence(tempNode, connections);
        
        // Generate coherence result
        return await CreateCoherenceResult(tempNode, connections, coherence);
    }

    private async Task<TimelineNode> CreateTimelineNode(string content, float currentTime)
    {
        string prompt = "Analyze this horror content for timeline placement:\n" +
                       $"{content}\n\n" +
                       "Consider:\n" +
                       "- Psychological progression\n" +
                       "- Thematic elements\n" +
                       "- Temporal dependencies\n" +
                       "- Horror buildup";

        string response = await llmManager.GenerateResponse(prompt, "timeline_analysis");
        return await ProcessTimelineAnalysis(response, content, currentTime);
    }

    private async Task<TimelineNode> ProcessTimelineAnalysis(string analysis, string content, float currentTime)
    {
        string structurePrompt = $"Structure this horror timeline analysis:\n{analysis}";
        string structured = await llmManager.GenerateResponse(structurePrompt, "analysis_structuring");

        return new TimelineNode
        {
            id = System.Guid.NewGuid().ToString(),
            content = content,
            timestamp = currentTime,
            psychologicalImpact = ExtractPsychologicalImpact(structured),
            thematicElements = ExtractThematicElements(structured),
            contextualFactors = ExtractContextualFactors(structured),
            dependencies = ExtractDependencies(structured),
            isResolved = false
        };
    }

    private float ExtractPsychologicalImpact(string structured)
    {
        // Implementation would extract psychological impact from LLM response
        return 0.7f;
    }

    private List<string> ExtractThematicElements(string structured)
    {
        // Implementation would extract thematic elements from LLM response
        return new List<string>();
    }

    private Dictionary<string, float> ExtractContextualFactors(string structured)
    {
        // Implementation would extract contextual factors from LLM response
        return new Dictionary<string, float>();
    }

    private List<string> ExtractDependencies(string structured)
    {
        // Implementation would extract dependencies from LLM response
        return new List<string>();
    }

    private List<TimelineArc> FindTimelineConnections(TimelineNode node)
    {
        return timelineArcs
            .Where(arc => IsArcRelevant(arc, node))
            .OrderByDescending(arc => CalculateArcRelevance(arc, node))
            .ToList();
    }

    private bool IsArcRelevant(TimelineArc arc, TimelineNode node)
    {
        // Check thematic overlap
        bool hasThematicOverlap = node.thematicElements
            .Intersect(GetArcThematicElements(arc))
            .Any();
        
        // Check temporal proximity
        bool isTemporallyRelevant = IsTemporallyRelevant(arc, node);
        
        // Check psychological continuity
        bool hasPsychologicalContinuity = HasPsychologicalContinuity(arc, node);
        
        return hasThematicOverlap && isTemporallyRelevant && hasPsychologicalContinuity;
    }

    private List<string> GetArcThematicElements(TimelineArc arc)
    {
        var elements = new List<string>();
        foreach (var nodeId in arc.nodeIds)
        {
            if (timelineNodes.TryGetValue(nodeId, out TimelineNode node))
            {
                elements.AddRange(node.thematicElements);
            }
        }
        return elements.Distinct().ToList();
    }

    private bool IsTemporallyRelevant(TimelineArc arc, TimelineNode node)
    {
        if (!timelineNodes.TryGetValue(arc.nodeIds.Last(), out TimelineNode lastNode))
            return false;

        float timeDifference = Mathf.Abs(node.timestamp - lastNode.timestamp);
        return timeDifference <= 300f; // 5 minutes threshold
    }

    private bool HasPsychologicalContinuity(TimelineArc arc, TimelineNode node)
    {
        if (arc.nodeIds.Count == 0)
            return false;

        var lastNodeId = arc.nodeIds.Last();
        if (!timelineNodes.TryGetValue(lastNodeId, out TimelineNode lastNode))
            return false;

        return Mathf.Abs(node.psychologicalImpact - lastNode.psychologicalImpact) <= 0.3f;
    }

    private float CalculateArcRelevance(TimelineArc arc, TimelineNode node)
    {
        float thematicRelevance = CalculateThematicRelevance(arc, node);
        float temporalRelevance = CalculateTemporalRelevance(arc, node);
        float psychologicalRelevance = CalculatePsychologicalRelevance(arc, node);
        
        return (thematicRelevance * 0.4f + 
                temporalRelevance * 0.3f + 
                psychologicalRelevance * 0.3f);
    }

    private float CalculateThematicRelevance(TimelineArc arc, TimelineNode node)
    {
        var arcThemes = GetArcThematicElements(arc);
        var commonThemes = node.thematicElements.Intersect(arcThemes).Count();
        return (float)commonThemes / Mathf.Max(node.thematicElements.Count, arcThemes.Count);
    }

    private float CalculateTemporalRelevance(TimelineArc arc, TimelineNode node)
    {
        if (!timelineNodes.TryGetValue(arc.nodeIds.Last(), out TimelineNode lastNode))
            return 0f;

        float timeDifference = Mathf.Abs(node.timestamp - lastNode.timestamp);
        return Mathf.Exp(-timeDifference / 300f); // 5 minutes decay
    }

    private float CalculatePsychologicalRelevance(TimelineArc arc, TimelineNode node)
    {
        float arcPsychologicalImpact = arc.nodeIds
            .Where(id => timelineNodes.ContainsKey(id))
            .Average(id => timelineNodes[id].psychologicalImpact);

        return 1f - Mathf.Abs(node.psychologicalImpact - arcPsychologicalImpact);
    }

    private async Task<float> CalculateTemporalCoherence(TimelineNode node, List<TimelineArc> connections)
    {
        if (connections.Count == 0)
            return node.psychologicalImpact;

        string prompt = "Calculate temporal coherence for this horror content:\n" +
                       $"Content: {node.content}\n\n" +
                       "Connected Arcs:\n";
        
        foreach (var arc in connections)
        {
            prompt += $"- {arc.description} (Coherence: {arc.coherenceScore})\n";
        }

        string response = await llmManager.GenerateResponse(prompt, "coherence_calculation");
        return ParseCoherenceValue(response);
    }

    private float ParseCoherenceValue(string response)
    {
        // Implementation would parse coherence value from LLM response
        return 0.7f;
    }

    private async Task<CoherenceResult> CreateCoherenceResult(
        TimelineNode node,
        List<TimelineArc> connections,
        float coherence)
    {
        var result = new CoherenceResult
        {
            isCoherent = coherence >= minCoherenceThreshold,
            coherenceScore = coherence,
            temporalFactors = CalculateTemporalFactors(node, connections),
            relevantArcs = connections
                .Select(a => a.id)
                .ToArray()
        };

        if (coherence < 0.7f)
        {
            result.suggestedAdditions = await GenerateCoherenceSuggestions(node, connections);
        }

        return result;
    }

    private Dictionary<string, float> CalculateTemporalFactors(TimelineNode node, List<TimelineArc> connections)
    {
        var factors = new Dictionary<string, float>();
        
        // Calculate progression factor
        factors["progression"] = CalculateProgressionFactor(node, connections);
        
        // Calculate continuity factor
        factors["continuity"] = CalculateContinuityFactor(node, connections);
        
        // Calculate development factor
        factors["development"] = CalculateDevelopmentFactor(node, connections);
        
        return factors;
    }

    private float CalculateProgressionFactor(TimelineNode node, List<TimelineArc> connections)
    {
        if (connections.Count == 0)
            return 0.5f;

        return connections.Average(arc => 
            arc.progressionCurve.Evaluate(GetArcProgress(arc, node.timestamp))
        );
    }

    private float GetArcProgress(TimelineArc arc, float currentTime)
    {
        if (arc.nodeIds.Count == 0)
            return 0f;

        float startTime = timelineNodes[arc.nodeIds.First()].timestamp;
        float endTime = timelineNodes[arc.nodeIds.Last()].timestamp;
        float duration = endTime - startTime;
        
        if (duration <= 0f)
            return 1f;

        return Mathf.Clamp01((currentTime - startTime) / duration);
    }

    private float CalculateContinuityFactor(TimelineNode node, List<TimelineArc> connections)
    {
        if (connections.Count == 0)
            return 0.5f;

        float continuity = 0f;
        foreach (var arc in connections)
        {
            var arcNodes = arc.nodeIds
                .Where(id => timelineNodes.ContainsKey(id))
                .Select(id => timelineNodes[id])
                .OrderBy(n => n.timestamp)
                .ToList();

            if (arcNodes.Count < 2)
                continue;

            float arcContinuity = CalculateArcContinuity(arcNodes);
            continuity += arcContinuity;
        }

        return continuity / connections.Count;
    }

    private float CalculateArcContinuity(List<TimelineNode> nodes)
    {
        float continuity = 0f;
        for (int i = 1; i < nodes.Count; i++)
        {
            float timeDifference = nodes[i].timestamp - nodes[i-1].timestamp;
            float psychologicalDifference = Mathf.Abs(
                nodes[i].psychologicalImpact - nodes[i-1].psychologicalImpact
            );
            
            continuity += Mathf.Exp(-timeDifference / 300f) * (1f - psychologicalDifference);
        }
        return continuity / (nodes.Count - 1);
    }

    private float CalculateDevelopmentFactor(TimelineNode node, List<TimelineArc> connections)
    {
        if (connections.Count == 0)
            return node.psychologicalImpact;

        return connections.Average(arc => {
            var lastNode = timelineNodes[arc.nodeIds.Last()];
            var development = node.psychologicalImpact - lastNode.psychologicalImpact;
            return Mathf.Clamp01(0.5f + development);
        });
    }

    private async Task<string[]> GenerateCoherenceSuggestions(TimelineNode node, List<TimelineArc> connections)
    {
        string prompt = "Generate coherence improvements for this horror content:\n" +
                       $"Content: {node.content}\n" +
                       "Current Connections:\n";
        
        foreach (var arc in connections)
        {
            prompt += $"- {arc.description}\n";
        }

        prompt += "\nSuggest additions that would strengthen:\n" +
                 "- Psychological progression\n" +
                 "- Thematic consistency\n" +
                 "- Horror development";

        string response = await llmManager.GenerateResponse(prompt, "suggestion_generation");
        return ParseSuggestions(response);
    }

    private string[] ParseSuggestions(string response)
    {
        // Implementation would parse suggestions from LLM response
        return new string[0];
    }

    public async Task RegisterTimelineNode(string content, float timestamp)
    {
        var node = await CreateTimelineNode(content, timestamp);
        timelineNodes[node.id] = node;
        
        // Update existing arcs
        await UpdateTimelineArcs(node);
        
        // Create new arc if needed
        await CreateNewArcIfNeeded(node);
    }

    private async Task UpdateTimelineArcs(TimelineNode node)
    {
        foreach (var arc in timelineArcs.Where(a => a.isActive))
        {
            if (IsNodeRelevantToArc(node, arc))
            {
                await AddNodeToArc(node, arc);
            }
        }
    }

    private bool IsNodeRelevantToArc(TimelineNode node, TimelineArc arc)
    {
        return IsArcRelevant(arc, node) && 
               CalculateArcRelevance(arc, node) >= minCoherenceThreshold;
    }

    private async Task AddNodeToArc(TimelineNode node, TimelineArc arc)
    {
        arc.nodeIds.Add(node.id);
        arc.coherenceScore = await RecalculateArcCoherence(arc);
        arc.progressionCurve = await GenerateProgressionCurve(arc);
    }

    private async Task<float> RecalculateArcCoherence(TimelineArc arc)
    {
        var arcNodes = arc.nodeIds
            .Where(id => timelineNodes.ContainsKey(id))
            .Select(id => timelineNodes[id])
            .OrderBy(n => n.timestamp)
            .ToList();

        string prompt = "Calculate coherence for this horror timeline arc:\n";
        foreach (var node in arcNodes)
        {
            prompt += $"- {node.content}\n";
        }

        string response = await llmManager.GenerateResponse(prompt, "arc_coherence_calculation");
        return ParseCoherenceValue(response);
    }

    private async Task<AnimationCurve> GenerateProgressionCurve(TimelineArc arc)
    {
        var arcNodes = arc.nodeIds
            .Where(id => timelineNodes.ContainsKey(id))
            .Select(id => timelineNodes[id])
            .OrderBy(n => n.timestamp)
            .ToList();

        string prompt = "Generate horror progression curve for:\n";
        foreach (var node in arcNodes)
        {
            prompt += $"- {node.content} (Impact: {node.psychologicalImpact})\n";
        }

        string response = await llmManager.GenerateResponse(prompt, "curve_generation");
        return ParseProgressionCurve(response);
    }

    private AnimationCurve ParseProgressionCurve(string response)
    {
        // Implementation would parse progression curve from LLM response
        return new AnimationCurve(
            new Keyframe(0f, 0.3f),
            new Keyframe(0.5f, 0.6f),
            new Keyframe(1f, 0.9f)
        );
    }

    private async Task CreateNewArcIfNeeded(TimelineNode node)
    {
        if (timelineArcs.Count == 0 || ShouldCreateNewArc(node))
        {
            await CreateNewArc(node);
        }
    }

    private bool ShouldCreateNewArc(TimelineNode node)
    {
        return !timelineArcs.Any(arc => 
            arc.isActive && IsNodeRelevantToArc(node, arc)
        );
    }

    private async Task CreateNewArc(TimelineNode node)
    {
        string prompt = "Generate new horror timeline arc for:\n" +
                       $"Initial Content: {node.content}\n" +
                       $"Psychological Impact: {node.psychologicalImpact}\n" +
                       $"Thematic Elements: {string.Join(", ", node.thematicElements)}";

        string response = await llmManager.GenerateResponse(prompt, "arc_generation");
        var arc = await ProcessArcGeneration(response, node);
        
        timelineArcs.Add(arc);
    }

    private async Task<TimelineArc> ProcessArcGeneration(string response, TimelineNode node)
    {
        string structurePrompt = $"Structure this horror timeline arc:\n{response}";
        string structured = await llmManager.GenerateResponse(structurePrompt, "arc_structuring");

        return new TimelineArc
        {
            id = System.Guid.NewGuid().ToString(),
            description = ExtractArcDescription(structured),
            nodeIds = new List<string> { node.id },
            coherenceScore = 1f,
            progressionCurve = new AnimationCurve(
                new Keyframe(0f, 0.3f),
                new Keyframe(1f, 0.9f)
            ),
            psychologicalWeights = new Dictionary<string, float>(node.contextualFactors),
            isActive = true
        };
    }

    private string ExtractArcDescription(string structured)
    {
        // Implementation would extract arc description from LLM response
        return "Default arc description";
    }

    public List<TimelineArc> GetActiveArcs()
    {
        return timelineArcs.Where(a => a.isActive).ToList();
    }

    public void UpdateTemporalWeight(string factor, float weight)
    {
        temporalWeights[factor] = Mathf.Clamp01(weight);
    }

    public void ClearInactiveArcs()
    {
        timelineArcs.RemoveAll(a => !a.isActive);
    }
}