using System;

public interface IResponse
{
    int PromptTokenCount { get; }
    int ResponseTokenCount { get; }
}

// Classes to deserialize LLM responses
[Serializable]
public class GeminiResponse : IResponse
{
    public Candidate[] candidates;
    public UsageMetadata usage;

    public int PromptTokenCount => usage.promptTokenCount;
    public int ResponseTokenCount => usage.candidatesTokenCount;
}

[Serializable]
public class GeminiResponseAlternative : IResponse
{
    public CandidateAlternative[] candidates;
    public UsageMetadata usage;

    public int PromptTokenCount => usage.promptTokenCount;
    public int ResponseTokenCount => usage.candidatesTokenCount;
}

[Serializable]
public class UsageMetadata
{
    public int promptTokenCount;
    public int candidatesTokenCount;
    public int totalTokenCount;
}

[Serializable]
public class Candidate
{
    public Content content;
}

[Serializable]
public class CandidateAlternative
{
    public Part[] parts;
}

[Serializable]
public class Content
{
    public Part[] parts;
}

[Serializable]
public class Part
{
    public string text;
}

[Serializable]
public class LocalLLMResponse : IResponse
{
    public Choices[] choices;
    public Usage usage;

    public int PromptTokenCount => usage.prompt_tokens;
    public int ResponseTokenCount => usage.completion_tokens;
}

[Serializable]
public class Usage
{
    public int prompt_tokens;
    public int completion_tokens;
    public int total_tokens;
}

[Serializable]
public class Choices
{
    public Message message;
}

[Serializable]
public class Message
{
    public string role;
    public string content;
}