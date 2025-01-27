using System;
using System.Collections.Generic;
using System.IO;
using Dapper;
using System.Data.SQLite;
using System.Linq;

namespace SmartVault.DataGeneration
{
    public static class Utils
    {
        // Method to get documents associated with an account from the database
        public static List<string> GetDocumentsForAccount(SQLiteConnection connection, string accountId)
        {
            var sql = @"SELECT FilePath FROM Document WHERE AccountId = @AccountId";

            // Query database to retrieve document file paths related to the accountId
            var documentPaths = connection.Query<string>(sql, new { AccountId = accountId }).ToList();

            return documentPaths;
        }

        public static void ProcessAccountFilesFromDatabase(SQLiteConnection connection, string accountId, string outputFilePath, string searchText)
        {
            try
            {
                // Get the list of file paths for the account
                var documentPaths = GetDocumentsForAccount(connection, accountId);

                // Ensure the output directory exists
                var outputDir = Path.GetDirectoryName(outputFilePath);
                if (!Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }

                using (StreamWriter outputFile = new StreamWriter(outputFilePath, append: false))
                {
                    // Process every third document starting from the third file (index 2, 5, 8, etc.)
                    for (int i = 2; i < documentPaths.Count; i += 3)
                    {
                        var filePath = documentPaths[i];

                        if (File.Exists(filePath))
                        {
                            // Read file content
                            string fileContent = File.ReadAllText(filePath);

                            // Only write content to output file if it contains the search text
                            if (fileContent.Contains(searchText))
                            {
                                outputFile.WriteLine($"--- Content from {filePath} ---");
                                outputFile.WriteLine(fileContent);
                                outputFile.WriteLine(); // Add extra line break between files
                            }
                        }
                    }
                }

                Console.WriteLine($"Output written to {outputFilePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: " + ex.Message);
            }
        }



        // Method to get the total file size of all files for a given account
        public static long GetTotalFileSizeForAccount(SQLiteConnection connection, string accountId)
        {
            // Query to get file paths associated with the account
            var query = "SELECT f.FilePath FROM Document f WHERE f.AccountId = @AccountId";
            var filePaths = connection.Query<string>(query, new { AccountId = accountId }).ToList();

            // Calculate total file size
            long totalFileSize = 0;
            foreach (var filePath in filePaths)
            {
                if (File.Exists(filePath))
                {
                    var fileInfo = new FileInfo(filePath);
                    totalFileSize += fileInfo.Length;
                }
            }

            return totalFileSize;
        }
    }
}
