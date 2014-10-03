//----------------------------------------------------------------------------------
// Microsoft Developer & Platform Evangelism
//
// Copyright (c) Microsoft Corporation. All rights reserved.
//
// THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, 
// EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES 
// OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE.
//----------------------------------------------------------------------------------
// The example companies, organizations, products, domain names,
// e-mail addresses, logos, people, places, and events depicted
// herein are fictitious.  No association with any real company,
// organization, product, domain name, email address, logo, person,
// places, or events is intended or should be inferred.
//----------------------------------------------------------------------------------

namespace DataDocumentDB.Models
{
    using Microsoft.Azure.Documents;
    
    // These classes represent an object that extend the DocumentDB base class for Document
    class Family : Document
    {
        public string FamilyName { get; set; }
        public Parent[] Parents { get; set; }
        public Child[] Children { get; set; }
        public Pet[] Pets { get; set; }
    }
    class Parent
    {
        public string FirstName { get; set; }
    }
    class Child
    {
        public string FirstName { get; set; }
        public string Gender { get; set; }
        public int Grade { get; set; }
    }
    class Pet
    {
        public string Name { get; set; }
        public string Type { get; set; }
    }
}

namespace DataDocumentDB
{
    using DataDocumentDB.Models;
    using Microsoft.Azure.Documents;
    using Microsoft.Azure.Documents.Client;
    using Microsoft.Azure.Documents.Linq;
    using System;
    using System.Configuration;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    class Program
    {
        //**************************************************************************************************************************************
        // In order to run this sample for Azure DocumentDB you need to have the following pre-requistes;
        //           1) An Azure subscription
        //              If you don't have an Azure subscription you can get a free trial. To create a free trial see:
        //              http://azure.microsoft.com/en-us/pricing/free-trial/
        //
        //           2) An Azure DocumentDB Account
        //              For instructions on creating an Azure DocumentDB account see: 
        //              http://azure.microsoft.com/en-us/documentation/articles/documentdb-create-account/
        //
        // NOTE: Never store credentials in source code. In this example placeholders in App.config are used. 
        //       For information on how to store credentials, see:
        //       Azure Websites: How Application Strings and Connection Strings Work 
        //       http://azure.microsoft.com/blog/2013/07/17/windows-azure-web-sites-how-application-strings-and-connection-strings-work/
        //
        // Additional Azure DocumentDB Code Samples can be found on http://go.microsoft.com/fwlink/?LinkID=512740
        //**************************************************************************************************************************************

        static readonly string endpoint = ConfigurationManager.AppSettings["endpoint"];
        static readonly string authKey = ConfigurationManager.AppSettings["authKey"];
        static DocumentClient client;

        static void Main(string[] args)
        {
            try
            {

                // 1. Get a reference to your Azure DocumentDB.
                //    It is recommended to create an instance of DocumentClient and reuse the same instance
                //    as opposed to creating, using and destroying the instance time and time again
                //    For this sample we are using the Defaults. There are optional parameters for things like ConnectionPolicy
                //    that allows you to change from Gateway to Direct or from HTTPS to TCP. 
                //    For more information on this, please consult the Azure DocumentDB Documentation
                Console.WriteLine("1. Create an instance of DocumentClient");
                using (client = new DocumentClient(new Uri(endpoint), authKey))
                {
                    // 2. The first thing you need is a Database. 
                    Console.WriteLine("2. Getting reference to Database");
                    Database database = ReadOrCreateDatabase("QuickStarts");

                    // 3. The next thing you need is a DocumentCollection.
                    Console.WriteLine("3. Getting reference to a DocumentCollection");
                    DocumentCollection collection = ReadOrCreateCollection(database.SelfLink, "Documents");

                    // 4. Now let's add a few Documents
                    Console.WriteLine("4. Inserting Documents");
                    CreateDocuments(collection.SelfLink);

                    // 5. Now let's query for some Documents
                    Console.WriteLine("5. Querying for Documents");
                    QueryDocuments(collection.SelfLink);
                    
                    // 6. Finally cleanup by deleting the Database
                    Console.WriteLine("6. Cleaning Up");
                    Cleanup(database.SelfLink);
                }
            }
            catch (DocumentClientException docEx)
            {
                Exception baseException = docEx.GetBaseException();
                Console.WriteLine("{0} StatusCode error occurred with activity id {3}: {1}, Message: {2}", 
                    docEx.StatusCode, docEx.Message, baseException.Message, docEx.ActivityId);
            }
            catch (AggregateException aggEx)
            {
                Console.WriteLine("One or more errors occured during execution");
                foreach (var exception in aggEx.InnerExceptions)
                {                    
                    Console.WriteLine("An exception of type {0} occured: {1}", exception.GetType(), exception.Message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An unexpected exception of type {0} occured: {0}", ex.GetType(), ex.Message);
            }

            Console.WriteLine("\nSample complete. Press any key to exit.");
            Console.ReadKey();
        }
 
        private static Database ReadOrCreateDatabase(string databaseId)
        {
            // Most times you won't need to create the Database in code, someone has likely created
            // the Database already in the Azure Management Portal, but you still need a reference to the
            // Database object so that you can work with it. Therefore this first query should return a record
            // the majority of the time
            var db = client.CreateDatabaseQuery()
                            .Where(d => d.Id == databaseId)
                            .AsEnumerable()
                            .FirstOrDefault();


            // In case there was no database matching, go ahead and create it. 
            if (db == null)
            {
                Console.WriteLine("2. Database not found, creating");
                db = client.CreateDatabaseAsync(new Database { Id = databaseId }).Result;
            }

            return db;
        }
        
        private static DocumentCollection ReadOrCreateCollection(string databaseLink, string collectionId)
        {
            var col = client.CreateDocumentCollectionQuery(databaseLink)
                              .Where(c => c.Id == collectionId)
                              .AsEnumerable()
                              .FirstOrDefault();

            // For this sample, if we found a DocumentCollection matching our criteria we are simply deleting the collection
            // and then recreating it. This is the easiest way to clear out existing doucments that might be left over in a collection
            //
            // NOTE: This is not the expected behavior for a production application. 
            // You would likely do the same as with a Database previously. If found, then return, else create
            if (col != null)
            {
                Console.WriteLine("3. Found DocumentCollection.\n3. Deleting DocumentCollection.");
                client.DeleteDocumentCollectionAsync(col.SelfLink).Wait();
            }

            Console.WriteLine("3. Creating DocumentCollection");
            return client.CreateDocumentCollectionAsync(databaseLink, new DocumentCollection { Id = collectionId }).Result;
        }
        
        private static void CreateDocuments(string collectionLink)
        {
            // DocumentDB provides many different ways of working with documents. 
            // 1. You can create an object that extends the Document base class
            // 2. You can use any POCO whether as it is without extending the Document base class
            // 3. You can use dynamic types
            // 4. You can even work with Streams directly.
            //
            // This sample method demonstrates only the first example
            // For more examples of other ways to work with documents please consult the samples on MSDN. 

            // Work with a well defined type that extends Document
            // In DocumetnDB every Document must have an "id" property. If you supply one, it must be unique. 
            // If you do not supply one, DocumentDB will generate a unique value for you and add it to the Document. 
            var task1 = client.CreateDocumentAsync(collectionLink, new Family
            {
                Id = "AndersonFamily",
                FamilyName = "Anderson",
                Parents = new Parent[]
                {
                    new Parent{FirstName="Thomas"}, 
                    new Parent{FirstName="Mary Kay"}
                },
                Children = new Child[] 
                {
                    new Child{FirstName="John", Gender="male", Grade=7}
                },
                Pets = new Pet[] 
                { 
                    new Pet{Name="Fluffy", Type="Dog"}
                }
            });

            var task2 = client.CreateDocumentAsync(collectionLink, new Family
            {
                Id = "WakefieldFamily",
                FamilyName = "Wakefield",
                Parents = new Parent[]
                {
                    new Parent{FirstName="Robin"}, 
                    new Parent{FirstName="Ben"}
                },
                Children = new Child[] 
                {
                    new Child{FirstName="Jesse", Gender="female", Grade=1},
                    new Child{FirstName="Lisa", Gender="female", Grade=8}
                },
                Pets = new Pet[] 
                { 
                    new Pet{Name="Goofy", Type="Dog"},
                    new Pet{Name="Shadow", Type="Horse"}
                }
            });

            var task3 = client.CreateDocumentAsync(collectionLink, new Family
            {
                Id = "AdamsFamily",
                FamilyName = "Adams",
                Parents = new Parent[]
                {
                    new Parent{FirstName="Susan"}, 
                },
                Children = new Child[] 
                {
                    new Child{FirstName="Megan", Gender="female"},
                },
            });

            // Wait for the above Async operations to finish executing
            Task.WaitAll(task1, task2, task3);

            Console.WriteLine("4. Documents successfully created");
        }

        private static void QueryDocuments(string collectionLink)
        {
            // The .NET SDK for DocumentDB supports 3 different methods of Querying for Documents
            // LINQ queries, lamba and SQL
                        
            // 1. LINQ Query
            var query = from f in client.CreateDocumentQuery<Family>(collectionLink)
                        where f.Id == "AndersonFamily"
                           select f;

            Console.WriteLine(query.AsEnumerable().FirstOrDefault<Family>().FamilyName);

            //2. LINQ Lambda
            query = client.CreateDocumentQuery<Family>(collectionLink).Where(f => f.Id == "WakefieldFamily");

            Console.WriteLine(query.AsEnumerable().FirstOrDefault<Family>().FamilyName);

            //3. SQL
            query = client.CreateDocumentQuery<Family>(collectionLink, "SELECT * " +
                                                                       "FROM Families f " +
                                                                       "WHERE f.id='AdamsFamily'");

            Console.WriteLine(query.AsEnumerable().FirstOrDefault<Family>().FamilyName);
        }

        private static void Cleanup(string databaseId)
        {
            client.DeleteDatabaseAsync(databaseId).Wait();
        }
    }
}
