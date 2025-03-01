using UnityEngine;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.CodeDom.Compiler;
using System.Reflection;
using System.Text;
using Microsoft.CSharp;
using AIHell.Core.Data;

public class RuntimeCodeGenerator : MonoBehaviour
{
    private LLMManager llmManager;
    private Dictionary<string, Type> generatedTypes;
    private Dictionary<string, Assembly> loadedAssemblies;
    private HashSet<string> bannedKeywords;

    [System.Serializable]
    public class GenerationTemplate
    {
        public string name;
        public string baseClass;
        public string[] requiredMethods;
        public string[] optionalMethods;
        public string[] requiredProperties;
        public string[] namespaces;
        public string description;
    }

    [System.Serializable]
    public class GeneratedCode
    {
        public string code;
        public string className;
        public Type generatedType;
        public Dictionary<string, object> metadata;
    }

    private readonly List<GenerationTemplate> templates = new List<GenerationTemplate>
    {
        new GenerationTemplate
        {
            name = "HorrorEffect",
            baseClass = "MonoBehaviour",
            requiredMethods = new[] { "ApplyEffect", "UpdateEffect" },
            optionalMethods = new[] { "OnActivate", "OnDeactivate" },
            requiredProperties = new[] { "intensity", "duration" },
            namespaces = new[] { "UnityEngine", "System.Collections" },
            description = "Base template for psychological horror effects"
        },
        new GenerationTemplate
        {
            name = "PsychologicalTrigger",
            baseClass = "MonoBehaviour",
            requiredMethods = new[] { "CheckTrigger", "OnTrigger" },
            optionalMethods = new[] { "ModifyPsychologicalState" },
            requiredProperties = new[] { "triggerProbability", "psychologicalImpact" },
            namespaces = new[] { "UnityEngine", "System.Collections" },
            description = "Template for psychological event triggers"
        }
    };

    private void Awake()
    {
        llmManager = GameManager.Instance.LLMManager;
        generatedTypes = new Dictionary<string, Type>();
        loadedAssemblies = new Dictionary<string, Assembly>();
        InitializeBannedKeywords();
    }

    private void InitializeBannedKeywords()
    {
        bannedKeywords = new HashSet<string>
        {
            "unsafe", "fixed", "stackalloc", "sizeof",
            "System.IO", "System.Net", "System.Diagnostics",
            "DllImport", "extern", "internal"
        };
    }

    public async Task<GeneratedCode> GenerateHorrorEffect(string effectDescription, PlayerAnalysisProfile profile)
    {
        string prompt = BuildEffectGenerationPrompt(effectDescription, profile);
        string response = await llmManager.GenerateResponse(prompt, "code_generation");
        
        return await ProcessGeneratedCode(response, "HorrorEffect");
    }

    private string BuildEffectGenerationPrompt(string effectDescription, PlayerAnalysisProfile profile)
    {
        var template = templates.Find(t => t.name == "HorrorEffect");
        
        return $"Generate a Unity C# horror effect class based on:\n" +
               $"Description: {effectDescription}\n" +
               $"Psychological State:\n" +
               $"- Fear: {profile.FearLevel}\n" +
               $"- Obsession: {profile.ObsessionLevel}\n" +
               $"- Aggression: {profile.AggressionLevel}\n\n" +
               "Requirements:\n" +
               $"- Base Class: {template.baseClass}\n" +
               $"- Required Methods: {string.Join(", ", template.requiredMethods)}\n" +
               "- Must be safe and performant\n" +
               "- Focus on psychological impact\n" +
               "- No file system or network access";
    }

    private async Task<GeneratedCode> ProcessGeneratedCode(string code, string templateName)
    {
        // First, validate the code
        if (!ValidateGeneratedCode(code))
        {
            throw new System.Exception("Generated code failed security validation");
        }

        // Clean and format the code
        code = await CleanGeneratedCode(code);

        // Extract class name
        string className = ExtractClassName(code);
        
        // Create metadata
        var metadata = new Dictionary<string, object>
        {
            { "generationTime", DateTime.Now },
            { "template", templateName },
            { "className", className }
        };

        // Compile the code
        Type generatedType = CompileCode(code, className);
        
        return new GeneratedCode
        {
            code = code,
            className = className,
            generatedType = generatedType,
            metadata = metadata
        };
    }

    private bool ValidateGeneratedCode(string code)
    {
        // Check for banned keywords
        foreach (var keyword in bannedKeywords)
        {
            if (code.Contains(keyword))
                return false;
        }

        // Check for suspicious patterns
        if (code.Contains("System.IO.File") ||
            code.Contains("System.Net.") ||
            code.Contains("System.Diagnostics.Process"))
            return false;

        return true;
    }

    private async Task<string> CleanGeneratedCode(string code)
    {
        // Remove any unsafe code markers
        code = code.Replace("unsafe", "");
        
        // Format the code
        string prompt = $"Clean and format this C# code while preserving functionality:\n{code}";
        return await llmManager.GenerateResponse(prompt, "code_cleaning");
    }

    private string ExtractClassName(string code)
    {
        // Simple class name extraction - could be more robust
        int classIndex = code.IndexOf("class ");
        if (classIndex == -1) return "GeneratedEffect";
        
        int nameStart = classIndex + 6;
        int nameEnd = code.IndexOf(" ", nameStart);
        if (nameEnd == -1) nameEnd = code.IndexOf("{", nameStart);
        
        return code.Substring(nameStart, nameEnd - nameStart).Trim();
    }

    private Type CompileCode(string code, string className)
    {
        var provider = new CSharpCodeProvider();
        var parameters = new CompilerParameters
        {
            GenerateInMemory = true,
            GenerateExecutable = false
        };

        // Add necessary references
        parameters.ReferencedAssemblies.Add("UnityEngine.dll");
        parameters.ReferencedAssemblies.Add("System.dll");
        
        // Compile the code
        CompilerResults results = provider.CompileAssemblyFromSource(parameters, code);
        
        if (results.Errors.HasErrors)
        {
            var errors = new StringBuilder();
            foreach (CompilerError error in results.Errors)
            {
                errors.AppendLine($"Error ({error.Line},{error.Column}): {error.ErrorText}");
            }
            throw new Exception($"Compilation failed:\n{errors}");
        }

        // Store the assembly
        string assemblyKey = $"{className}_{DateTime.Now.Ticks}";
        loadedAssemblies[assemblyKey] = results.CompiledAssembly;
        
        // Get and store the generated type
        Type generatedType = results.CompiledAssembly.GetType(className);
        generatedTypes[className] = generatedType;
        
        return generatedType;
    }

    public async Task<Component> InstantiateEffect(GeneratedCode generatedCode, GameObject target)
    {
        // Validate the generated type
        if (!typeof(MonoBehaviour).IsAssignableFrom(generatedCode.generatedType))
        {
            throw new Exception("Generated type must inherit from MonoBehaviour");
        }

        // Create the component
        Component component = target.AddComponent(generatedCode.generatedType);
        
        // Initialize effect parameters
        await InitializeEffectParameters(component, generatedCode);
        
        return component;
    }

    private async Task InitializeEffectParameters(Component component, GeneratedCode generatedCode)
    {
        // Generate parameters based on current psychological state
        var profile = GameManager.Instance.ProfileManager.CurrentProfile;
        
        string prompt = $"Generate initialization parameters for horror effect:\n" +
                       $"Class: {generatedCode.className}\n" +
                       $"Fear Level: {profile.FearLevel}\n" +
                       $"Obsession Level: {profile.ObsessionLevel}\n" +
                       $"Aggression Level: {profile.AggressionLevel}";

        string response = await llmManager.GenerateResponse(prompt, "parameter_generation");
        
        // Parse and apply parameters
        Dictionary<string, object> parameters = ParseParameters(response);
        ApplyParameters(component, parameters);
    }

    private Dictionary<string, object> ParseParameters(string response)
    {
        // Parse LLM response into parameter dictionary
        // This would be implemented based on the LLM's output format
        return new Dictionary<string, object>();
    }

    private void ApplyParameters(Component component, Dictionary<string, object> parameters)
    {
        foreach (var param in parameters)
        {
            var property = component.GetType().GetProperty(param.Key);
            if (property != null && property.CanWrite)
            {
                try
                {
                    object convertedValue = Convert.ChangeType(param.Value, property.PropertyType);
                    property.SetValue(component, convertedValue);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to set parameter {param.Key}: {e.Message}");
                }
            }
        }
    }

    public Type GetGeneratedType(string className)
    {
        return generatedTypes.TryGetValue(className, out Type type) ? type : null;
    }

    public void UnloadGeneratedAssembly(string className)
    {
        if (generatedTypes.ContainsKey(className))
        {
            generatedTypes.Remove(className);
            // Note: Assembly unloading is not directly supported in .NET
            // The assembly will be unloaded when its AppDomain is unloaded
        }
    }

    private void OnDestroy()
    {
        generatedTypes.Clear();
        loadedAssemblies.Clear();
    }
}