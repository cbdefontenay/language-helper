using LanguageHelper.Modals;

namespace LanguageHelper.Services;

public class FolderDbService
{
    private readonly string
        _dbFilePath =
            Path.Combine(FileSystem.AppDataDirectory, "folder_vocabulary.db"); // Ensure this is a correct path

    public FolderDbService()
    {
        EnsureDatabaseExists();
    }

    private void EnsureDatabaseExists()
    {
        bool databaseExists = File.Exists(_dbFilePath);

        using var connection = new SqliteConnection($"Data Source={_dbFilePath}");
        connection.Open();

        if (!databaseExists)
        {
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
                    Learned INTEGER DEFAULT 0,
                    FOREIGN KEY (FolderId) REFERENCES Folders(Id)
                );";

            connection.Execute(createFoldersTableSql);
            connection.Execute(createVocabularyListsTableSql);
        }
        else
        {
            // Ensure the 'Learned' column exists
            const string checkColumnSql = "PRAGMA table_info(VocabularyLists);";
            var columns = connection.Query<string>(checkColumnSql);

            if (!columns.Any(c => c.Contains("Learned")))
            {
                const string addLearnedColumnSql = "ALTER TABLE VocabularyLists ADD COLUMN Learned INTEGER DEFAULT 0;";
                connection.Execute(addLearnedColumnSql);
            }
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
            "SELECT Id, FolderId, Word, Translation, Learned FROM VocabularyLists WHERE FolderId = @FolderId",
            new { FolderId = folderId });

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

    public async Task<int> DeleteFolderAsync(int folderId)
    {
        await using var connection = new SqliteConnection($"Data Source={_dbFilePath}");
        connection.Open();

        await using var transaction = connection.BeginTransaction();

        try
        {
            await connection.ExecuteAsync(
                "DELETE FROM VocabularyLists WHERE FolderId = @FolderId",
                new { FolderId = folderId }, transaction);

            var result = await connection.ExecuteAsync(
                "DELETE FROM Folders WHERE Id = @FolderId",
                new { FolderId = folderId }, transaction);

            transaction.Commit();
            return result;
        }
        catch (Exception)
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<int> UpdateVocabularyAsync(int wordId, string? word, string? translation)
    {
        await using var connection = new SqliteConnection($"Data Source={_dbFilePath}");
        connection.Open();

        var result = await connection.ExecuteAsync(
            "UPDATE VocabularyLists SET Word = @Word, Translation = @Translation WHERE Id = @WordId",
            new { Word = word, Translation = translation, WordId = wordId });

        return result;
    }

    public async Task<int> DeleteWordAsync(int wordId)
    {
        await using var connection = new SqliteConnection($"Data Source={_dbFilePath}");
        connection.Open();

        var result = await connection.ExecuteAsync(
            "DELETE FROM VocabularyLists WHERE Id = @WordId",
            new { WordId = wordId });

        return result;
    }

    public async Task ToggleLearnedStatusAsync(int wordId, bool learned)
    {
        await using var connection = new SqliteConnection($"Data Source={_dbFilePath}");
        connection.Open();

        await connection.ExecuteAsync(
            "UPDATE VocabularyLists SET Learned = @Learned WHERE Id = @WordId",
            new { Learned = learned ? 1 : 0, WordId = wordId });
    }
}