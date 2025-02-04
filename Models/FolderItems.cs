namespace LanguageHelper.Models;

public class FolderItems
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime Created { get; set; } = DateTime.UtcNow;
}