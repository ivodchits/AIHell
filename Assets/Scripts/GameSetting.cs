[System.Serializable]
public class GameSetting
{
    public string full_setting;
    public LevelSetting[] levels;

    public string Print()
    {
        return $"Setting:\n{full_setting}\nLevels:\n{string.Join<LevelSetting>("\n", levels)}";
    }
}

[System.Serializable]
public class LevelSetting
{
    public string level_theme;
    public string level_tone;
    
    public override string ToString()
    {
        return $"Theme: {level_theme},\nTone: {level_tone}";
    }
}