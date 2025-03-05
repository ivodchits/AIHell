using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public class Logger : MonoBehaviour
{
    // Log collections
    private List<string> allLogs = new List<string>();
    private List<string> standardLogs = new List<string>();

    // File paths
    private string allLogsFilePath;
    private string standardLogsFilePath;
    
    // HTML styling
    private const string HTML_HEAD = @"<!DOCTYPE html>
<html>
<head>
    <title>AIHell Logs</title>
    <style>
        body { font-family: Arial, sans-serif; margin: 20px; background-color: #f5f5f5; }
        .log-container { max-width: 1000px; margin: 0 auto; background-color: white; padding: 20px; border-radius: 5px; box-shadow: 0 2px 5px rgba(0,0,0,0.1); }
        .log-entry { margin-bottom: 10px; padding: 8px; border-left: 3px solid #ccc; }
        .standard { border-left-color: #2196F3; background-color: #E3F2FD; }
        .extra { border-left-color: #FF9800; background-color: #FFF3E0; }
        .image { border-left-color: #4CAF50; background-color: #E8F5E9; }
        .timestamp { color: #666; font-size: 0.9em; }
        img { max-width: 100%; margin-top: 10px; border: 1px solid #ddd; }
        h1 { color: #333; text-align: center; }
    </style>
</head>
<body>
    <div class='log-container'>
    <h1>AIHell Log Report</h1>
";

    private const string HTML_FOOTER = @"
    </div>
</body>
</html>";
    
    private void Awake()
    {
        // Initialize file paths in persistent data path
        string logDirectory = Path.Combine(Application.persistentDataPath, "Logs");
        
        // Create directory if it doesn't exist
        if (!Directory.Exists(logDirectory))
        {
            Directory.CreateDirectory(logDirectory);
        }
        
        // Set file paths with timestamp to avoid overwriting
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        allLogsFilePath = Path.Combine(logDirectory, $"AllLogs_{timestamp}.html");
        standardLogsFilePath = Path.Combine(logDirectory, $"StandardLogs_{timestamp}.html");
        
        Debug.Log($"Logger initialized. Log files will be saved to: {logDirectory}");
    }
    
    /// <summary>
    /// Logs a standard message
    /// </summary>
    public void Log(string log)
    {
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        string htmlLog = $"<div class='log-entry standard'><span class='timestamp'>[{timestamp}]</span> {log}</div>";
        
        // Add to both logs
        allLogs.Add(htmlLog);
        standardLogs.Add(htmlLog);
        
        // Print to console for debugging
        Debug.Log($"[Standard] {log}");
    }
    
    /// <summary>
    /// Logs an extra message (only goes to all logs)
    /// </summary>
    public void LogExtra(string log)
    {
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        string htmlLog = $"<div class='log-entry extra'><span class='timestamp'>[{timestamp}]</span> <strong>[EXTRA]</strong> {log}</div>";
        
        // Add only to all logs
        allLogs.Add(htmlLog);
        
        // Print to console for debugging
        Debug.Log($"[Extra] {log}");
    }
    
    /// <summary>
    /// Logs an image reference with embedded image
    /// </summary>
    public void LogImage(string imagePath)
    {
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        
        // Create HTML for embedded image
        string imageHtml;
        if (File.Exists(imagePath))
        {
            try
            {
                // Convert image to base64 for embedding
                byte[] imageBytes = File.ReadAllBytes(imagePath);
                string base64Image = Convert.ToBase64String(imageBytes);
                string imageExtension = Path.GetExtension(imagePath).ToLower().TrimStart('.');
                
                // Create HTML with embedded image
                imageHtml = $"<div class='log-entry image'><span class='timestamp'>[{timestamp}]</span> <strong>[IMAGE]</strong> {Path.GetFileName(imagePath)}<br/>" +
                           $"<img src='data:image/{imageExtension};base64,{base64Image}' alt='{Path.GetFileName(imagePath)}' /></div>";
            }
            catch (Exception e)
            {
                // If there's an error, just link to the image
                imageHtml = $"<div class='log-entry image'><span class='timestamp'>[{timestamp}]</span> <strong>[IMAGE]</strong> {imagePath} (Error embedding: {e.Message})</div>";
            }
        }
        else
        {
            imageHtml = $"<div class='log-entry image'><span class='timestamp'>[{timestamp}]</span> <strong>[IMAGE]</strong> {imagePath} (File not found)</div>";
        }
        
        // Add to both logs
        allLogs.Add(imageHtml);
        standardLogs.Add(imageHtml);
        
        // Print to console for debugging
        Debug.Log($"[Image] {imagePath}");
    }
    
    /// <summary>
    /// Saves all logs to their respective HTML files
    /// </summary>
    public void SaveLogs()
    {
        try
        {
            // Write all logs with HTML formatting
            SaveHtmlLog(allLogsFilePath, allLogs);
            
            // Write standard logs with HTML formatting
            SaveHtmlLog(standardLogsFilePath, standardLogs);
            
            Debug.Log($"HTML logs saved successfully to {Path.GetDirectoryName(allLogsFilePath)}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save HTML logs: {e.Message}");
        }
    }
    
    /// <summary>
    /// Appends current logs to HTML files without clearing memory
    /// </summary>
    public void AppendLogsToFile()
    {
        try
        {
            // For HTML files, we need to read the existing file, replace the footer,
            // add new content, and then add the footer back
            AppendToHtmlLog(allLogsFilePath, allLogs);
            AppendToHtmlLog(standardLogsFilePath, standardLogs);
            
            Debug.Log("Logs appended to HTML files successfully");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to append logs: {e.Message}");
        }
    }
    
    /// <summary>
    /// Clears all logs from memory (does not affect saved files)
    /// </summary>
    public void ClearLogs()
    {
        allLogs.Clear();
        standardLogs.Clear();
        Debug.Log("Logs cleared from memory");
    }
    
    /// <summary>
    /// Saves logs as a complete HTML file
    /// </summary>
    private void SaveHtmlLog(string filePath, List<string> logs)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append(HTML_HEAD);
        
        foreach (string log in logs)
        {
            sb.AppendLine(log);
        }
        
        sb.Append(HTML_FOOTER);
        
        File.WriteAllText(filePath, sb.ToString());
    }
    
    /// <summary>
    /// Appends logs to an existing HTML file or creates a new one
    /// </summary>
    private void AppendToHtmlLog(string filePath, List<string> logs)
    {
        StringBuilder sb = new StringBuilder();
        
        if (!File.Exists(filePath))
        {
            // If file doesn't exist, create a new complete HTML file
            SaveHtmlLog(filePath, logs);
            return;
        }
        
        // Read the existing file
        string existingContent = File.ReadAllText(filePath);
        
        // Find where the footer starts and remove it
        int footerIndex = existingContent.LastIndexOf(HTML_FOOTER);
        if (footerIndex >= 0)
        {
            sb.Append(existingContent.Substring(0, footerIndex));
        }
        else
        {
            // If footer not found, start with fresh HTML
            sb.Append(HTML_HEAD);
        }
        
        // Add new logs
        foreach (string log in logs)
        {
            sb.AppendLine(log);
        }
        
        // Add footer back
        sb.Append(HTML_FOOTER);
        
        // Write back to file
        File.WriteAllText(filePath, sb.ToString());
    }
    
    private void OnApplicationQuit()
    {
        // Log statistics data before quitting
        if (StatisticsManager.Instance != null)
        {
            LogExtra("=== STATISTICS SUMMARY ===");
            var categories = StatisticsManager.Instance.GetCategories();
            var prompt = 0;
            var response = 0;
            var total = 0;
            foreach (var category in categories)
            {
                var p = StatisticsManager.Instance.GetPromptTokens(category);
                var r = StatisticsManager.Instance.GetResponseTokens(category);
                var t = p + r;
                prompt += p;
                response += r;
                total += t;
                LogExtra($"Chat: {category}, Prompt Tokens: {p}, Response Tokens: {r}, Total: {t}");
            }
            LogExtra($"TOTAL TOKENS USED:\nPrompt: {prompt}, Response: {response}, Total: {total}");
            LogExtra("========================");
        }
        // Auto-save logs when application quits
        SaveLogs();
    }
}