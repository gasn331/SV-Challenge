using SmartVault.DataGeneration;
using SmartVault.Library;
using System.Data.SQLite;
using System;
using Microsoft.Extensions.Configuration;
using System.IO;
using Dapper;
using System.Linq;

namespace SmartVault.Program
{
    partial class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                return;
            }

            // Load the configuration from appsettings.json
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory()) // Set the base path to the current directory
                .AddJsonFile("appsettings.json") // Add the appsettings.json file
                .Build();

            // Get the database file name and connection string template
            string databaseFileName = configuration["DatabaseFileName"];
            string connectionStringTemplate = configuration.GetSection("ConnectionStrings")["DefaultConnection"];

            // Construct the full connection string by replacing the placeholder with the actual database file name
            string connectionString = string.Format(connectionStringTemplate, databaseFileName);


            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();  // Ensure the connection is open

                WriteEveryThirdFileToFile(connection, args[0], args[1], args[2]);
                GetAllFileSizes(connection);
            }
        }

        private static void GetAllFileSizes(SQLiteConnection connection)
        {
            // Get all account IDs from the database
            var accountIds = connection.Query<string>("SELECT Id FROM Account").ToList();

            foreach (var accountId in accountIds)
            {
                // Get total file size for each account
                long totalFileSize = Utils.GetTotalFileSizeForAccount(connection, accountId);

                // Print the total file size for the account
                Console.WriteLine($"Total file size for account {accountId}: {totalFileSize} bytes");
            }
        }

        private static void WriteEveryThirdFileToFile(SQLiteConnection connection, string accountId, string outputFilePath, string searchText = "Smith Property")
        {
            // Call the method from the Utils class
            Utils.ProcessAccountFilesFromDatabase(connection, accountId, outputFilePath, searchText);

            // Optional: Any other logic you want to perform after calling the method
            Console.WriteLine("Processing complete!");
        }
    }
}