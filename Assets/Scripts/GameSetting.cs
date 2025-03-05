[System.Serializable]
public class GameSetting
{
    public string fullSetting;
    public string briefSetting;
    public LevelSetting[] levels;

    public string Print()
    {
        return $"Setting:\n{fullSetting}\nLevels:\n{string.Join<LevelSetting>("\n", levels)}";
    }
}

[System.Serializable]
public class LevelSetting
{
    public string theme;
    public string tone;
    
    public override string ToString()
    {
        return $"Theme: {theme},\nTone: {tone}";
    }
}