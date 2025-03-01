using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using System.Linq;
using AIHell.Core.Data;

public class SemanticHorrorAnalyzer : MonoBehaviour
{
    private LLMManager llmManager;
    private Dictionary<string, HorrorConcept> conceptDatabase;
    private List<SemanticRelation> semanticNetwork;
    private Dictionary<string, float> psychologicalWeights;

    [System.Serializable]
    public class HorrorConcept
    {
        public string id;
        public string name;
        public string description;
        public string[] categories;
        public float psychologicalImpact;
        public string[] associations;
        public Dictionary<string, float> emotionalWeights;
        public Dictionary<string, object> metadata;
    }

    [System.Serializable]
    public class SemanticRelation
    {
        public string sourceConceptId;
        public string targetConceptId;
        public string relationType;
        public float strength;
        public string context;
    }

    [System.Serializable]
    public class AnalysisResult
    {
        public List<string> primaryConcepts;
        public Dictionary<string, float> conceptStrengths;
        public Dictionary<string, List<string>> associations;
        public float overallPsychologicalImpact;
        public Dictionary<string, float> emotionalProfile;
    }

    private void Awake()
    {
        llmManager = GameManager.Instance.LLMManager;
        InitializeAnalyzer();
    }

    private void InitializeAnalyzer()
    {
        conceptDatabase = new Dictionary<string, HorrorConcept>();
        semanticNetwork = new List<SemanticRelation>();
        psychologicalWeights = new Dictionary<string, float>();
        
        InitializeWeights();
    }

    private void InitializeWeights()
    {
        psychologicalWeights["existential"] = 0.8f;
        psychologicalWeights["personal"] = 0.9f;
        psychologicalWeights["cosmic"] = 0.7f;
        psychologicalWeights["psychological"] = 0.85f;
        psychologicalWeights["environmental"] = 0.6f;
    }

    public async Task<AnalysisResult> AnalyzeHorrorContent(string content, PlayerAnalysisProfile profile)
    {
        // Extract core concepts
        var concepts = await ExtractCoreConcepts(content);
        
        // Build semantic relationships
        await BuildSemanticNetwork(concepts, content);
        
        // Analyze psychological impact
        var result = await AnalyzePsychologicalImpact(concepts, profile);
        
        // Enhance with personal context
        await EnhanceWithPersonalContext(result, profile);
        
        return result;
    }

    private async Task<List<HorrorConcept>> ExtractCoreConcepts(string content)
    {
        string prompt = "Extract core psychological horror concepts from this content:\n" +
                       $"{content}\n\n" +
                       "Focus on elements that have deep psychological impact. " +
                       "Include existential, personal, and cosmic horror elements.";

        string response = await llmManager.GenerateResponse(prompt, "concept_extraction");
        return await ParseConcepts(response);
    }

    private async Task<List<HorrorConcept>> ParseConcepts(string llmResponse)
    {
        var concepts = new List<HorrorConcept>();
        
        // Get structured format from LLM
        string structurePrompt = $"Structure these horror concepts into a clear format:\n{llmResponse}";
        string structured = await llmManager.GenerateResponse(structurePrompt, "concept_structuring");
        
        // Parse structured response
        // Implementation would depend on LLM output format
        
        return concepts;
    }

    private async Task BuildSemanticNetwork(List<HorrorConcept> concepts, string context)
    {
        foreach (var concept in concepts)
        {
            foreach (var otherConcept in concepts)
            {
                if (concept.id == otherConcept.id) continue;

                var relation = await AnalyzeConceptRelation(concept, otherConcept, context);
                if (relation.strength > 0.3f) // Only keep significant relations
                {
                    semanticNetwork.Add(relation);
                }
            }
        }
    }

    private async Task<SemanticRelation> AnalyzeConceptRelation(HorrorConcept source, HorrorConcept target, string context)
    {
        string prompt = "Analyze the relationship between these horror concepts:\n" +
                       $"Concept 1: {source.name} - {source.description}\n" +
                       $"Concept 2: {target.name} - {target.description}\n" +
                       $"Context: {context}\n\n" +
                       "Determine relationship type and strength (0-1).";

        string response = await llmManager.GenerateResponse(prompt, "relation_analysis");
        
        return new SemanticRelation
        {
            sourceConceptId = source.id,
            targetConceptId = target.id,
            relationType = ExtractRelationType(response),
            strength = ExtractRelationStrength(response),
            context = context
        };
    }

    private string ExtractRelationType(string response)
    {
        // Parse relation type from LLM response
        // This would be implemented based on the LLM's output format
        return "psychological"; // Placeholder
    }

    private float ExtractRelationStrength(string response)
    {
        // Parse strength value from LLM response
        // This would be implemented based on the LLM's output format
        return 0.5f; // Placeholder
    }

    private async Task<AnalysisResult> AnalyzePsychologicalImpact(List<HorrorConcept> concepts, PlayerAnalysisProfile profile)
    {
        var result = new AnalysisResult
        {
            primaryConcepts = new List<string>(),
            conceptStrengths = new Dictionary<string, float>(),
            associations = new Dictionary<string, List<string>>(),
            emotionalProfile = new Dictionary<string, float>()
        };

        // Build complex prompt for psychological analysis
        string analysisPrompt = BuildPsychologicalAnalysisPrompt(concepts, profile);
        string response = await llmManager.GenerateResponse(analysisPrompt, "psychological_analysis");
        
        // Process the analysis
        await ProcessPsychologicalAnalysis(response, result);
        
        return result;
    }

    private string BuildPsychologicalAnalysisPrompt(List<HorrorConcept> concepts, PlayerAnalysisProfile profile)
    {
        var prompt = new StringBuilder();
        prompt.AppendLine("Analyze the psychological impact of these horror concepts:");
        
        foreach (var concept in concepts)
        {
            prompt.AppendLine($"\nConcept: {concept.name}");
            prompt.AppendLine($"Description: {concept.description}");
            prompt.AppendLine($"Categories: {string.Join(", ", concept.categories)}");
        }

        prompt.AppendLine("\nPsychological State:");
        prompt.AppendLine($"Fear Level: {profile.FearLevel}");
        prompt.AppendLine($"Obsession Level: {profile.ObsessionLevel}");
        prompt.AppendLine($"Aggression Level: {profile.AggressionLevel}");

        return prompt.ToString();
    }

    private async Task ProcessPsychologicalAnalysis(string analysis, AnalysisResult result)
    {
        // Get structured analysis from LLM
        string structurePrompt = $"Structure this psychological horror analysis into clear sections:\n{analysis}";
        string structured = await llmManager.GenerateResponse(structurePrompt, "analysis_structuring");
        
        // Parse structured response into result object
        ParseAnalysisResponse(structured, result);
    }

    private void ParseAnalysisResponse(string structured, AnalysisResult result)
    {
        // Implementation would parse LLM response into result object
        // This would depend on the LLM's output format
    }

    private async Task EnhanceWithPersonalContext(AnalysisResult result, PlayerAnalysisProfile profile)
    {
        // Build personal context prompt
        string contextPrompt = $"Enhance this horror analysis with personal psychological context:\n" +
                             $"Primary Concepts: {string.Join(", ", result.primaryConcepts)}\n" +
                             $"Current Fear Level: {profile.FearLevel}\n" +
                             $"Current Obsession Level: {profile.ObsessionLevel}\n" +
                             $"Current Aggression Level: {profile.AggressionLevel}\n\n" +
                             "Generate personalized psychological implications.";

        string response = await llmManager.GenerateResponse(contextPrompt, "personal_enhancement");
        
        // Update result with personal context
        await IntegratePersonalContext(response, result);
    }

    private async Task IntegratePersonalContext(string personalContext, AnalysisResult result)
    {
        // Get structured personal context from LLM
        string structurePrompt = $"Structure this personal horror context into clear impacts:\n{personalContext}";
        string structured = await llmManager.GenerateResponse(structurePrompt, "context_structuring");
        
        // Update result with personal context
        UpdateResultWithPersonalContext(structured, result);
    }

    private void UpdateResultWithPersonalContext(string structured, AnalysisResult result)
    {
        // Implementation would update result with personal context
        // This would depend on the LLM's output format
    }

    public List<HorrorConcept> GetRelatedConcepts(string conceptId, float minStrength = 0.5f)
    {
        return semanticNetwork
            .Where(r => r.sourceConceptId == conceptId && r.strength >= minStrength)
            .Select(r => conceptDatabase[r.targetConceptId])
            .ToList();
    }

    public float GetConceptImpact(string conceptId, PlayerAnalysisProfile profile)
    {
        if (!conceptDatabase.TryGetValue(conceptId, out HorrorConcept concept))
            return 0f;

        float impact = concept.psychologicalImpact;
        
        // Modify based on psychological state
        if (concept.categories.Contains("fear"))
            impact *= (1f + profile.FearLevel);
        
        if (concept.categories.Contains("obsession"))
            impact *= (1f + profile.ObsessionLevel);
        
        if (concept.categories.Contains("aggression"))
            impact *= (1f + profile.AggressionLevel);

        return Mathf.Clamp01(impact);
    }

    public void UpdateConceptDatabase(HorrorConcept concept)
    {
        conceptDatabase[concept.id] = concept;
    }

    public void ClearSemanticNetwork()
    {
        semanticNetwork.Clear();
    }
}