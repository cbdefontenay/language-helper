namespace LanguageHelper.Modals;

public class VocabularyItems
{
    public int Id { get; set; }
    public int FolderId { get; set; }
    public string Word { get; set; } = string.Empty;
    public string Translation { get; set; } = string.Empty;
    public bool Learned { get; set; }
}
