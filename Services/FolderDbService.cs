namespace LanguageHelper.Services;

public class FolderDbService
{
    private readonly string _dbFilePath =
        Path.Combine(FileSystem.AppDataDirectory, "folder_vocabulary.db");

    public FolderDbService()
    {
        try
        {
            EnsureDatabaseExists();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Database initialization failed: {ex.Message}");
        }
    }

    private void EnsureDatabaseExists()
    {
        try
        {
            bool databaseExists = File.Exists(_dbFilePath);

            using var connection = new SqliteConnection($"Data Source={_dbFilePath}");
            connection.Open();

            connection.Execute("PRAGMA foreign_keys = ON;");

            connection.Execute("PRAGMA journal_mode = WAL;");

            if (!databaseExists)
            {
                using var transaction = connection.BeginTransaction();

                try
                {
                    // Create tables within a transaction
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
                            FOREIGN KEY (FolderId) REFERENCES Folders(Id) ON DELETE CASCADE
                        );";

                    connection.Execute(createFoldersTableSql, transaction: transaction);
                    connection.Execute(createVocabularyListsTableSql, transaction: transaction);
                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
            else
            {
                EnsureColumnExists(connection, "VocabularyLists", "Learned", "INTEGER DEFAULT 0");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error initializing database: {ex.Message}");
            throw;
        }
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

    private void EnsureColumnExists(SqliteConnection connection, string tableName, string columnName,
        string columnDefinition)
    {
        try
        {
            var columns = connection.Query<string>("SELECT name FROM pragma_table_info(@TableName);",
                new { TableName = tableName }).ToList();

            if (!columns.Any(c => c.Contains(columnName)))
            {
                string addColumnSql = $"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnDefinition};";
                connection.Execute(addColumnSql);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error checking/adding column '{columnName}' in table '{tableName}': {ex.Message}");
        }
    }

    public async Task<List<FolderItems>> GetFoldersAsync()
    {
        try
        {
            await using var connection = new SqliteConnection($"Data Source={_dbFilePath}");
            await connection.OpenAsync();

            var folders = await connection.QueryAsync<FolderItems>("SELECT * FROM Folders");
            return folders.ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching folders: {ex.Message}");
            return new List<FolderItems>();
        }
    }

    public async Task<int> CreateFolderAsync(string folderName)
    {
        try
        {
            await using var connection = new SqliteConnection($"Data Source={_dbFilePath}");
            await connection.OpenAsync();

            return await connection.ExecuteAsync(
                "INSERT INTO Folders (Name, Created) VALUES (@Name, @Created)",
                new { Name = folderName, Created = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating folder: {ex.Message}");
            return -1;
        }
    }

    public async Task<int> CreateVocabularyAsync(int folderId, string? word, string? translation)
    {
        try
        {
            await using var connection = new SqliteConnection($"Data Source={_dbFilePath}");
            await connection.OpenAsync();

            return await connection.ExecuteAsync(
                "INSERT INTO VocabularyLists (FolderId, Word, Translation) VALUES (@FolderId, @Word, @Translation)",
                new { FolderId = folderId, Word = word, Translation = translation });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating vocabulary: {ex.Message}");
            return -1;
        }
    }

    public async Task<List<VocabularyItems>> GetVocabularyForFolderAsync(int folderId)
    {
        try
        {
            await using var connection = new SqliteConnection($"Data Source={_dbFilePath}");
            await connection.OpenAsync();

            var vocabulary = await connection.QueryAsync<VocabularyItems>(
                "SELECT Id, FolderId, Word, Translation, Learned FROM VocabularyLists WHERE FolderId = @FolderId",
                new { FolderId = folderId });

            return vocabulary.ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching vocabulary: {ex.Message}");
            return new List<VocabularyItems>();
        }
    }

    public async Task<int> DeleteFolderAsync(int folderId)
    {
        try
        {
            await using var connection = new SqliteConnection($"Data Source={_dbFilePath}");
            await connection.OpenAsync();
            await using var transaction = await connection.BeginTransactionAsync();

            try
            {
                // Only delete the folder, SQLite will automatically delete related vocabulary lists
                int result = await connection.ExecuteAsync(
                    "DELETE FROM Folders WHERE Id = @FolderId",
                    new { FolderId = folderId }, transaction);

                await transaction.CommitAsync();
                return result;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting folder: {ex.Message}");
            return -1;
        }
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

    public async Task<int> UpdateVocabularyAsync(int wordId, string? word, string? translation)
    {
        try
        {
            await using var connection = new SqliteConnection($"Data Source={_dbFilePath}");
            await connection.OpenAsync();

            return await connection.ExecuteAsync(
                "UPDATE VocabularyLists SET Word = @Word, Translation = @Translation WHERE Id = @WordId",
                new { Word = word, Translation = translation, WordId = wordId });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating vocabulary: {ex.Message}");
            return -1;
        }
    }

    public async Task ToggleLearnedStatusAsync(int wordId, bool learned)
    {
        try
        {
            await using var connection = new SqliteConnection($"Data Source={_dbFilePath}");
            await connection.OpenAsync();

            await connection.ExecuteAsync(
                "UPDATE VocabularyLists SET Learned = @Learned WHERE Id = @WordId",
                new { Learned = learned ? 1 : 0, WordId = wordId });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error toggling learned status: {ex.Message}");
        }
    }
}