using LanguageHelper.Modals;

namespace LanguageHelper.Services;

public class FolderDbService
{
    private readonly string _dbFilePath = Path.Combine(FileSystem.AppDataDirectory, "folder_vocabulary.db"); // Ensure this is a correct path

    public FolderDbService()
    {
        EnsureDatabaseExists();
    }

    private void EnsureDatabaseExists()
    {
        if (!File.Exists(_dbFilePath))
        {
            using var connection = new SqliteConnection($"Data Source={_dbFilePath}");
            connection.Open();

            // SQL commands to create tables if they don't exist
            const string createFoldersTableSql = @"
                    CREATE TABLE IF NOT EXISTS Folders (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name TEXT NOT NULL,
                        Created DATETIME NOT NULL
                    );";

            const string createVocabularyListsTableSql = @"
                    CREATE TABLE IF NOT EXISTS VocabularyLists (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        FolderId INTEGER NOT NULL,
                        Word TEXT NOT NULL,
                        Translation TEXT NOT NULL,
                        FOREIGN KEY (FolderId) REFERENCES Folders(Id)
                    );";

            // Execute SQL commands
            connection.Execute(createFoldersTableSql);
            connection.Execute(createVocabularyListsTableSql);
        }
    }

    public async Task<List<FolderItems>> GetFoldersAsync()
    {
        await using var connection = new SqliteConnection($"Data Source={_dbFilePath}");
        connection.Open();

        var folders = await connection.QueryAsync<FolderItems>("SELECT * FROM Folders");
        return folders.ToList();
    }

    public async Task<int> CreateFolderAsync(string folderName)
    {
        await using var connection = new SqliteConnection($"Data Source={_dbFilePath}");
        connection.Open();

        var result = await connection.ExecuteAsync(
            "INSERT INTO Folders (Name, Created) VALUES (@Name, @Created)",
            new { Name = folderName, Created = DateTime.UtcNow });

        return result;
    }

    public async Task<int> CreateVocabularyAsync(int folderId, string? word, string? translation)
    {
        await using var connection = new SqliteConnection($"Data Source={_dbFilePath}");
        connection.Open();

        var result = await connection.ExecuteAsync(
            "INSERT INTO VocabularyLists (FolderId, Word, Translation) VALUES (@FolderId, @Word, @Translation)",
            new { FolderId = folderId, Word = word, Translation = translation });

        return result;
    }

    public async Task<List<VocabularyItems>> GetVocabularyForFolderAsync(int folderId)
    {
        await using var connection = new SqliteConnection($"Data Source={_dbFilePath}");
        connection.Open();

        var vocabulary = await connection.QueryAsync<VocabularyItems>(
            "SELECT * FROM VocabularyLists WHERE FolderId = @FolderId", new { FolderId = folderId });

        return vocabulary.ToList();
    }
    
    public async Task<string> GetFolderNameAsync(int folderId)
    {
        await using var connection = new SqliteConnection($"Data Source={_dbFilePath}");
        connection.Open();

        var folderName = await connection.QuerySingleOrDefaultAsync<string>(
            "SELECT Name FROM Folders WHERE Id = @FolderId",
            new { FolderId = folderId });

        return folderName ?? "Unknown Folder";
    }

}

 