using NUnit.Framework;
using SmartVault.DataGeneration;
using System.IO;
using System.Data.SQLite;
using System;
using Dapper;

namespace SmartVault.Tests
{
    [TestFixture]
    public class UtilsTests
    {
        private string _databaseFileName;
        private string _tempDirectory;

        [SetUp]
        public void SetUp()
        {
            // Create a temporary directory to store files and a simulated database
            _tempDirectory = Path.Combine(Directory.GetCurrentDirectory(), "TempFiles");
            Directory.CreateDirectory(_tempDirectory);

            _databaseFileName = Path.Combine(_tempDirectory, "testdb.sqlite");

            // Simulate the SQLite database
            var connectionString = $"Data Source={_databaseFileName};Version=3;";
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                connection.Execute("CREATE TABLE Document (Id INTEGER PRIMARY KEY, AccountId TEXT, FilePath TEXT)");
                connection.Execute("INSERT INTO Document (AccountId, FilePath) VALUES (@AccountId, @FilePath)",
                    new { AccountId = "account1", FilePath = Path.Combine(_tempDirectory, "file1.txt") });
                connection.Execute("INSERT INTO Document (AccountId, FilePath) VALUES (@AccountId, @FilePath)",
                    new { AccountId = "account1", FilePath = Path.Combine(_tempDirectory, "file2.txt") });
            }

            // Create test files
            File.WriteAllText(Path.Combine(_tempDirectory, "file1.txt"), "Some content for file 1");
            File.WriteAllText(Path.Combine(_tempDirectory, "file2.txt"), "Some content for file 2");
        }

        [Test]
        public void GetTotalFileSizeForAccount_ShouldReturnCorrectFileSize()
        {
            // Create the connection to the database
            var connectionString = $"Data Source={_databaseFileName};Version=3;";
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                // Call the method to get the total file size
                long totalFileSize = Utils.GetTotalFileSizeForAccount(connection, "account1");

                // Validate that the returned size matches the expected size
                long expectedFileSize = new FileInfo(Path.Combine(_tempDirectory, "file1.txt")).Length +
                                        new FileInfo(Path.Combine(_tempDirectory, "file2.txt")).Length;

                Assert.That(totalFileSize, Is.EqualTo(expectedFileSize));
            }
        }

        [Test]
        public void GetTotalFileSizeForAccount_ShouldReturnZeroForEmptyFile()
        {
            // Create an empty file
            string emptyFilePath = Path.Combine(_tempDirectory, "emptyFile99.txt");
            File.WriteAllText(emptyFilePath, string.Empty);  // Ensure the file is empty

            // Insert the empty file into the database
            var connectionString = $"Data Source={_databaseFileName};Version=3;";
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                connection.Execute("INSERT INTO Document (AccountId, FilePath) VALUES (@AccountId, @FilePath)",
                    new { AccountId = "account99", FilePath = emptyFilePath });
            }

            // Check if the method returns zero for an empty file
            long totalFileSize;
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                totalFileSize = Utils.GetTotalFileSizeForAccount(connection, "account99");
            }

            // Assert that the file size is zero
            Assert.That(totalFileSize, Is.EqualTo(0), "The total file size for an empty file should be zero.");
        }

        [Test]
        public void GetTotalFileSizeForAccount_ShouldReturnZeroWhenNoFiles()
        {
            // Ensure the database is set up correctly and the table exists.
            var connectionString = $"Data Source={_databaseFileName};Version=3;";
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                // Ensure the table is created (can be moved to the SetUp method if needed)
                connection.Execute("CREATE TABLE IF NOT EXISTS Document (Id INTEGER PRIMARY KEY, AccountId TEXT, FilePath TEXT)");

                // In case there are existing records, we clear them out for a fresh start
                connection.Execute("DELETE FROM Document WHERE AccountId = @AccountId", new { AccountId = "account1" });

                // Now, call the method to get the total file size for an account with no files
                long totalFileSize = Utils.GetTotalFileSizeForAccount(connection, "account1");

                // Assert that the file size is zero, as there are no files for the account
                Assert.That(totalFileSize, Is.EqualTo(0), "The total file size should be zero when there are no files associated with the account.");
            }
        }

        [Test]
        public void ProcessAccountFilesFromDatabase_ShouldCreateOutputFile()
        {
            string outputFilePath = Path.Combine(_tempDirectory, "output.txt");

            // Create the connection to the database
            var connectionString = $"Data Source={_databaseFileName};Version=3;";
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                // Process the files and verify that the output file was created
                Utils.ProcessAccountFilesFromDatabase(connection, "account1", outputFilePath, "content");
            }

            Assert.IsTrue(File.Exists(outputFilePath));
        }

        [Test]
        public void ProcessAccountFilesFromDatabase_ShouldWriteToFileIfSearchTextFound()
        {
            // Setup necessary test files and data
            var connectionString = $"Data Source={_databaseFileName};Version=3;";
            string outputFilePath = Path.Combine(_tempDirectory, "output.txt");
            string searchText = "Smith Property";

            // Ensure the table exists
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                // Prepare test data by inserting files that contain the searchText
                connection.Execute("CREATE TABLE IF NOT EXISTS Document (Id INTEGER PRIMARY KEY, AccountId TEXT, FilePath TEXT)");
                connection.Execute("DELETE FROM Document WHERE AccountId = @AccountId", new { AccountId = "account1" });
                connection.Execute("INSERT INTO Document (AccountId, FilePath) VALUES (@AccountId, @FilePath)",
                    new { AccountId = "account1", FilePath = Path.Combine(_tempDirectory, "file1.txt") });
                connection.Execute("INSERT INTO Document (AccountId, FilePath) VALUES (@AccountId, @FilePath)",
                    new { AccountId = "account1", FilePath = Path.Combine(_tempDirectory, "file2.txt") });
                connection.Execute("INSERT INTO Document (AccountId, FilePath) VALUES (@AccountId, @FilePath)",
                    new { AccountId = "account1", FilePath = Path.Combine(_tempDirectory, "file3.txt") });

                // Create files with content that should be found
                File.WriteAllText(Path.Combine(_tempDirectory, "file1.txt"), "Some content for file 1 with Smith Property");
                File.WriteAllText(Path.Combine(_tempDirectory, "file2.txt"), "Some content for file 2");
                File.WriteAllText(Path.Combine(_tempDirectory, "file3.txt"), "Some content for file 3 with Smith Property");

                // Now run the method that processes the files
                Utils.ProcessAccountFilesFromDatabase(connection, "account1", outputFilePath, searchText);

                // Read the output file to check its content
                string outputContent = File.ReadAllText(outputFilePath);

                // Assert that the output content is not empty (i.e., content should be written)
                Assert.That(outputContent, Is.Not.Empty, "The output file should contain content if the search text is found.");

                // Assert that the output contains the correct content from the file that contains "Smith Property"
                Assert.That(outputContent, Does.Contain("Some content for file 3 with Smith Property"), "The output should contain content from file3.");

                // Ensure the output does not contain content from other files that don't match the search text
                Assert.That(outputContent, Does.Not.Contain("Some content for file 1"), "The output should not contain content from file1.");
                Assert.That(outputContent, Does.Not.Contain("Some content for file 2"), "The output should not contain content from file2.");
            }
        }



        [Test]
        public void ProcessAccountFilesFromDatabase_ShouldNotWriteToFileIfNoContentFound()
        {
            string outputFilePath = Path.Combine(_tempDirectory, "output.txt");

            // Create the connection to the database
            var connectionString = $"Data Source={_databaseFileName};Version=3;";
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                // Process the files and verify that the output file remains empty
                Utils.ProcessAccountFilesFromDatabase(connection, "account1", outputFilePath, "nonexistent");

                string outputContent = File.ReadAllText(outputFilePath);
                Assert.That(outputContent, Is.Empty);
            }
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up temporary files after tests
            Directory.Delete(_tempDirectory, true);
        }
    }
}
