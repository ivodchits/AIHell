public class Character
{
    public string CharacterID { get; private set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string InitialDialogue { get; set; }
    public bool HasInteracted { get; private set; }

    public Character(string characterID, string name, string description, string initialDialogue)
    {
        CharacterID = characterID;
        Name = name;
        Description = description;
        InitialDialogue = initialDialogue;
        HasInteracted = false;
    }

    public void Interact()
    {
        if (!HasInteracted)
        {
            GameManager.Instance.UIManager.DisplayMessage(InitialDialogue);
            HasInteracted = true;
        }
    }
}