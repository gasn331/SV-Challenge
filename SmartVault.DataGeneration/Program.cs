using Dapper;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using SmartVault.Library;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Xml.Serialization;

namespace SmartVault.DataGeneration
{
    partial class Program
    {
        static void Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json").Build();

            SQLiteConnection.CreateFile(configuration["DatabaseFileName"]);
            File.WriteAllText("TestDoc.txt", $"This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}...");

            using (var connection = new SQLiteConnection(string.Format(configuration?["ConnectionStrings:DefaultConnection"] ?? "", configuration?["DatabaseFileName"])))
            {
                connection.Open();  // Ensure the connection is open before starting the transaction

                // Begin a transaction
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Use the implicit table creation from BusinessObjectScript or any other schema initialization steps
                        var files = Directory.GetFiles(@"..\..\..\..\BusinessObjectSchema");
                        for (int i = 0; i <= 3; i++)
                        {
                            var serializer = new XmlSerializer(typeof(BusinessObject));
                            var businessObject = serializer.Deserialize(new StreamReader(files[i])) as BusinessObject;

                            // Execute the script to create the tables if they don't already exist
                            if (businessObject?.Script != null)
                            {
                                connection.Execute(businessObject.Script, transaction: transaction);  // Pass the transaction to each command
                            }
                        }

                        // Insert data for users, accounts, and documents
                        var documentNumber = 0;
                        for (int i = 0; i < 100; i++)
                        {
                            var randomDayIterator = RandomDay().GetEnumerator();
                            randomDayIterator.MoveNext();
                            connection.Execute($"INSERT INTO User (Id, FirstName, LastName, DateOfBirth, AccountId, Username, Password) VALUES('{i}','FName{i}','LName{i}','{randomDayIterator.Current.ToString("yyyy-MM-dd")}','{i}','UserName-{i}','e10adc3949ba59abbe56e057f20f883e')", transaction: transaction);
                            connection.Execute($"INSERT INTO Account (Id, Name) VALUES('{i}','Account{i}')", transaction: transaction);

                            for (int d = 0; d < 10000; d++, documentNumber++)
                            {
                                var documentPath = new FileInfo("TestDoc.txt").FullName;
                                connection.Execute($"INSERT INTO Document (Id, Name, FilePath, Length, AccountId) VALUES('{documentNumber}','Document{i}-{d}.txt','{documentPath}','{new FileInfo(documentPath).Length}','{i}')", transaction: transaction);
                            }
                        }

                        // Commit the transaction if everything succeeds
                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        // Rollback the transaction if there is an error
                        Console.WriteLine("Error: " + ex.Message);
                        transaction.Rollback();
                    }
                }

                // Query counts after committing the transaction
                var accountData = connection.Query("SELECT COUNT(*) FROM Account;");
                Console.WriteLine($"AccountCount: {JsonConvert.SerializeObject(accountData)}");

                var documentData = connection.Query("SELECT COUNT(*) FROM Document;");
                Console.WriteLine($"DocumentCount: {JsonConvert.SerializeObject(documentData)}");

                var userData = connection.Query("SELECT COUNT(*) FROM User;");
                Console.WriteLine($"UserCount: {JsonConvert.SerializeObject(userData)}");

                var oAuthData = connection.Query("SELECT COUNT(*) FROM OAuthIntegration;");
                Console.WriteLine($"OAuthCount: {JsonConvert.SerializeObject(oAuthData)}");
            }
        }

        static IEnumerable<DateTime> RandomDay()
        {
            DateTime start = new DateTime(1985, 1, 1);
            Random gen = new Random();
            int range = (DateTime.Today - start).Days;
            while (true)
                yield return start.AddDays(gen.Next(range));
        }
    }
}
