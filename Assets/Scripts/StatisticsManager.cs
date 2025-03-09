using UnityEngine;
using System.Collections.Generic;

public class StatisticsManager : MonoBehaviour
{
    // Dictionaries to track tokens by category
    Dictionary<string, int> _promptTokensByCategory = new Dictionary<string, int>();
    Dictionary<string, int> _responseTokensByCategory = new Dictionary<string, int>();
    
    // Total token counts
    int _totalPromptTokens = 0;
    int _totalResponseTokens = 0;

    // Properties to access totals
    public int TotalPromptTokens => _totalPromptTokens;
    public int TotalResponseTokens => _totalResponseTokens;
    public int TotalTokens => _totalPromptTokens + _totalResponseTokens;

    /// <summary>
    /// Add token counts from an IResponse to the statistics
    /// </summary>
    /// <param name="chatName">Category/chat name to track tokens under</param>
    /// <param name="response">The IResponse containing token counts</param>
    public void Add(string chatName, IResponse response)
    {
        // Add to category dictionaries
        if (!_promptTokensByCategory.ContainsKey(chatName))
        {
            _promptTokensByCategory[chatName] = 0;
            _responseTokensByCategory[chatName] = 0;
        }

        _promptTokensByCategory[chatName] += response.PromptTokenCount;
        _responseTokensByCategory[chatName] += response.ResponseTokenCount;

        // Add to totals
        _totalPromptTokens += response.PromptTokenCount;
        _totalResponseTokens += response.ResponseTokenCount;
    }

    /// <summary>
    /// Get prompt tokens for a specific category
    /// </summary>
    /// <param name="chatName">Category name</param>
    /// <returns>Number of prompt tokens, or 0 if category doesn't exist</returns>
    public int GetPromptTokens(string chatName)
    {
        return _promptTokensByCategory.GetValueOrDefault(chatName, 0);
    }

    /// <summary>
    /// Get response tokens for a specific category
    /// </summary>
    /// <param name="chatName">Category name</param>
    /// <returns>Number of response tokens, or 0 if category doesn't exist</returns>
    public int GetResponseTokens(string chatName)
    {
        return _responseTokensByCategory.GetValueOrDefault(chatName, 0);
    }

    /// <summary>
    /// Get total tokens (prompt + response) for a specific category
    /// </summary>
    /// <param name="chatName">Category name</param>
    /// <returns>Total number of tokens, or 0 if category doesn't exist</returns>
    public int GetTotalTokens(string chatName)
    {
        int promptTokens = GetPromptTokens(chatName);
        int responseTokens = GetResponseTokens(chatName);
        return promptTokens + responseTokens;
    }

    /// <summary>
    /// Get all categories that have been tracked
    /// </summary>
    /// <returns>Array of category names</returns>
    public string[] GetCategories()
    {
        string[] categories = new string[_promptTokensByCategory.Count];
        _promptTokensByCategory.Keys.CopyTo(categories, 0);
        return categories;
    }

    /// <summary>
    /// Reset all statistics
    /// </summary>
    public void Reset()
    {
        _promptTokensByCategory.Clear();
        _responseTokensByCategory.Clear();
        _totalPromptTokens = 0;
        _totalResponseTokens = 0;
    }
}