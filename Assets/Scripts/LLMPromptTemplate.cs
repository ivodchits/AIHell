using System;
using UnityEngine;

/// <summary>
/// Defines a prompt template for LLM interactions
/// </summary>
[Serializable]
public class LLMPromptTemplate
{
    public string templateName;
    public string templateText;
    public ModelType modelType;
}