using System.Collections.Generic;

public class LLMChat
{
    public readonly string ChatName;
    public readonly ModelType ModelType;
    public readonly float Temperature;
    public LLMProvider LLMProvider { get; private set; }
    
    public string SystemPrompt { get; set; } = string.Empty;

    public IReadOnlyList<ChatEntry> ChatHistory => chatHistory;
    
    readonly List<ChatEntry> chatHistory = new();
    
    public LLMChat(string chatName, LLMProvider llmProvider, ModelType modelType, float temperature = -1)
    {
        ChatName = chatName;
        LLMProvider = llmProvider;
        ModelType = modelType;
        Temperature = temperature;
    }

    public void AddEntry(ChatEntry entry)
    {
        chatHistory.Add(entry);
    }
    
    public string GetChatHistoryAsString()
    {
        string history = "";
        for (var i = 0; i < chatHistory.Count; i++)
        {
            var entry = chatHistory[i];
            history += $"\n{ParseEntry(entry)}";
            if (i < chatHistory.Count - 1)
            {
                history += ",";
            }
        }

        return history;
    }
    
    public string ParseEntry(ChatEntry entry)
    {
        var role = entry.IsUser ? "user" : LLMProvider == LLMProvider.LocalLLM ? "assistant" : "model";
        var content = LLMProvider == LLMProvider.LocalLLM
            ? $"\"content\": \"{entry.Content}\""
            : $"\"parts\": [{{\"text\": \"{entry.Content}\"}}]";
        return $"{{\"role\": \"{role}\", {content}}}";
    }

    public void ConvertToLocal()
    {
        if (LLMProvider == LLMProvider.LocalLLM)
            return;
        
        LLMProvider = LLMProvider.LocalLLM;
    }
}

public readonly struct ChatEntry
{
    public bool IsUser { get; }
    public string Content { get; }
    
    public ChatEntry(bool isUser, string content)
    {
        IsUser = isUser;
        Content = content;
    }
}