namespace LanguageHelper.Modals;

public class VocabularyItems
{
    public int Id { get; set; }
    public int FolderId { get; set; }
    public string? Word { get; set; }
    public string? Translation { get; set; }
    public bool IsFavorite { get; set; }
}