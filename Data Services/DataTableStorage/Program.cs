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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using DataTableStorageSample.Model;

namespace DataTableStorageSample
{
    /// <summary>
    /// Azure Table Service Sample - Demonstrate how to perform common tasks using the Microsoft Azure Table storage 
    /// including blah blah blah             
    /// 
    /// Documentation References: 
    /// - What is a Storage Account - http://azure.microsoft.com/en-us/documentation/articles/storage-whatis-account/
    /// - Getting Started with Tables - http://azure.microsoft.com/en-us/documentation/articles/storage-dotnet-how-to-use-tables/
    /// - Table Service Concepts - http://msdn.microsoft.com/en-us/library/dd179463.aspx
    /// - Table Service REST API - http://msdn.microsoft.com/en-us/library/dd179423.aspx
    /// - Table Service C# API - http://go.microsoft.com/fwlink/?LinkID=398944
    /// </summary>

    class Program
    {
        //*************************************************************************************************************************
        // Instructions: 
        // TODO: 1. Create a Storage Account through the Portal and provide your [AccountName] and [AccountKey] in the App.Config 
        //          See http://go.microsoft.com/fwlink/?LinkId=325277 for more information
        //       2. Set breakpoints and run the project
        //*************************************************************************************************************************
        const string TABLENAME = "people";

        static void Main(string[] args)
        {
            Console.WriteLine("Azure Storage Table Sample\n");

            // Retrieve storage account information from connection string.
            CloudStorageAccount storageAccount = CreateStorageAccountFromConnectionString(CloudConfigurationManager.GetSetting("StorageConnectionString"));

            // Create a table client for interacting with the table service 
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            Console.WriteLine("1. Create a Table in Table Storage");
            CloudTable table = CreateTable(tableClient, TABLENAME);

            // Demonstrate how to Add an entity to a table
            Console.WriteLine("2. Inserting a single Entity to a Table");
            InsertEntityToTable(table);

            // Demonstrate how to insert a batch of entities
            Console.WriteLine("3. Inserting a batch of entities to a Table");
            InsertEntitiesToTable(table);
            
            // Demonstrate how to query customers in a given parition
            Console.WriteLine("\n4. Query entities in a given partition");
            QueryAllEntitiesInPartition(table, "Smith");

            // Demonstrate how to query customers in a partition within a given range
            Console.WriteLine("\n5. Query entities in a partition within a given range");
            QueryAllEntitiesInPartition(table, "Smith", "E");
            
            // Demonstrate how to retrive a single entity
            Console.WriteLine("\n6. Retrieve a single entity using a PartitionKey and RowKey");
            CustomerEntity customer = GetSingleEntity(table, "Smith", "Ben");
            
            // Demonstrate how to Replace an entity 
            Console.WriteLine("7. Replace a given entity");
            customer.PhoneNumber = "425-555-0105";// Change the phone number.
            ReplaceEntity(table, customer);

            // Demonstrate how to Insert or Replace an entity                
            Console.WriteLine("\n8. Insert OR Replace a given entity");
            customer.PhoneNumber = "425-555-1234"; // Change the phone number.
            InsertOrReplaceEntity(table, customer);

            // Demonstrate how to Query a subset of entity properties, in this case we will do just email
            Console.WriteLine("\n9. Query a subset of entity properties");
            QueryEntityPropertySubSet(table, "Email");

            // Demonstrate how to Delete an entity
            Console.WriteLine("10. Delete an entity");
            DeleteEntity(table, customer);

            // Demonstrate how to Delete a table
            Console.WriteLine("\n11. Delete a table");
            DeleteTable(table);

            Console.WriteLine("Press any key to exit");
            Console.Read();
        }

        /// <summary>
        /// Validate the connection string information in app.config and throws an exception if it looks like 
        /// the user hasn't updated this to valid values. 
        /// </summary>
        private static CloudStorageAccount CreateStorageAccountFromConnectionString(string storageConnectionString)
        {
            CloudStorageAccount storageAccount;
            try
            {
                storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            }
            catch (System.FormatException)
            {
                Console.WriteLine("Invalid storage account information provided. Please confirm the AccountName and AccountKey are valid in the app.config file.");
                throw;
            }
            return storageAccount;
        }

        private static CloudTable CreateTable(CloudTableClient tableClient, string tableName)
        {
            // Create the table if it doesn't exist
            CloudTable table = tableClient.GetTableReference(tableName);
            
            try
            {
                bool success = table.CreateIfNotExists();
                // TODO: Vinay - are there other reasons why CreateIfNotExists may return false other than the table already exists?
                if (success)
                    Console.WriteLine("Created Table named: {0}\n", tableName);
                else
                    Console.WriteLine("Table {0} already exists\n", tableName);
            }
            catch (StorageException ex)
            {
                // TODO: Vinay - Alternative is not to delete the table at the end of the run - this is a little clumsy
                if (ex.RequestInformation.HttpStatusCode == (int)HttpStatusCode.Conflict)
                {
                    Console.WriteLine("Table {0} is still being deleted from last run. Try again in a minute. ", tableName);
                }
                throw; 
            }

            return table;
        }

        private static void InsertEntityToTable(CloudTable table) 
        {
            // The operation to be performed is represented by a TableOperation object. 
            // The following code example shows the use of the CloudTable object and then a CustomerEntity object. 
            // To prepare the operation, a TableOperation is created to insert the customer entity into the table. 
            // Finally, the operation is executed by calling CloudTable.Execute.        

            // Create the TableOperation that inserts the customer entity.
            CustomerEntity customer = new CustomerEntity("Harp", "Walter")
            {
                Email = "Walter@contoso.com",
                PhoneNumber = "425-555-0101"
            };

            TableOperation insertOperation = TableOperation.Insert(customer);
            TableResult result = table.Execute(insertOperation);

            if (result.HttpStatusCode == (int)HttpStatusCode.Created)
            {
                var customerInserted = result.Result as CustomerEntity;
                Console.WriteLine("Inserted entity with \r\n\t Etag = {0} and PartitionKey = {1}, RowKey = {2}\n", customerInserted.ETag, customerInserted.PartitionKey, customerInserted.RowKey);
            }
        }

        private static void InsertEntitiesToTable(CloudTable table) 
        {
            //You can insert a batch of entities into a table in one write operation. Some other notes on batch operations:
            //  1.You can perform updates, deletes, and inserts in the same single batch operation.
            //  2.A single batch operation can include up to 100 entities.
            //  3.All entities in a single batch operation must have the same partition key.
            //  4.While it is possible to perform a query as a batch operation, it must be the only operation in the batch.
            //The following code example adds each entity in the entities collection to a TableBatchOperation using the Insert method. 
            //Then CloudTable.Execute is called to execute the operation.

            List<CustomerEntity> customers = new List<CustomerEntity>();
            customers.Add(new CustomerEntity("Smith", "Jeff")
            {
                Email = "Walter@contoso.com",
                PhoneNumber = "425-555-0104"
            });

            customers.Add(new CustomerEntity("Smith", "Ben")
            {
                Email = "Ben@contoso.com",
                PhoneNumber = "425-555-0103"
            });

            // Create the batch operation.
            TableBatchOperation batchOperation = new TableBatchOperation();

            // Add entities to the batch insert operation.
            foreach (var entity in customers)
                batchOperation.Insert(entity);

            // Execute the batch operation.
            IList<TableResult> results = table.ExecuteBatch(batchOperation);

            foreach (var res in results)
            {
                if (res.HttpStatusCode == (int)HttpStatusCode.Created)
                {
                    var customerInserted = res.Result as CustomerEntity;
                    Console.WriteLine("Inserted entity with \r\n\t Etag = {0} and PartitionKey = {1}, RowKey = {2}", customerInserted.ETag, customerInserted.PartitionKey, customerInserted.RowKey);
                }
            }
        }


        private static void QueryEntityPropertySubSet(CloudTable table, string property)
        {
            // Define the query, and only select the provided property
            TableQuery<DynamicTableEntity> projectionQuery = new TableQuery<DynamicTableEntity>().Select(new List<string> { property });

            // Define an entity resolver to work with the entity after retrieval.
            EntityResolver<string> resolver = (pk, rk, ts, props, etag) => props.ContainsKey(property) ? props[property].StringValue : null;

            IEnumerable<string> customerList = table.ExecuteQuery(projectionQuery, resolver, null, null);

            foreach (string projectedEmail in customerList)
            {
                Console.WriteLine("\t {0}", projectedEmail);
            }

        }


        private static CustomerEntity GetSingleEntity(CloudTable table, string partitionKey, string rowKey)
        {
            // You can write a query to retrieve a single, specific entity. The following code uses a TableOperation to specify the customer 
            // 'Ben Smith'. This method returns just one entity, rather than a collection, and the returned value in TableResult.Result is a 
            // CustomerEntity. Specifying both partition and row keys in a query is the fastest way to retrieve a single entity from the Table 
            // service.
            TableOperation retrieveOperation = TableOperation.Retrieve<CustomerEntity>(partitionKey, rowKey);

            CustomerEntity customer = (CustomerEntity)table.Execute(retrieveOperation).Result;

            // TODO: under what conditions should this be null?
            if (customer != null)
            {
                Console.WriteLine("\t{0}\t{1}\t{2}\t{3}", customer.PartitionKey, customer.RowKey, customer.Email, customer.PhoneNumber);
            }
            return customer;
        }

        private static void QueryAllEntitiesInPartition(CloudTable table, string partitionKey) 
        {
            // To query a table for all entities in a partition, use a TableQuery object. 
            // The following code example specifies a filter for entities where the parameter named partitionKey is the partition key. 
            // This example prints the fields of each entity in the query results to the console.

            TableQuery<CustomerEntity> query = new TableQuery<CustomerEntity>().Where
                (TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionKey));

            foreach (CustomerEntity entity in table.ExecuteQuery(query))
            {
                Console.WriteLine("{0}\t{1}\t{2}\t{3}", entity.PartitionKey, entity.RowKey, entity.Email, entity.PhoneNumber);
            }
        }

        private static void QueryAllEntitiesInPartition(CloudTable table, string partitionKey, string rowKey) 
        {
            // If you don't want to query all the entities in a partition, you can specify a range by combining the partition key 
            // filter with a row key filter. The following code example uses two filters to get all entities in partition provided 
            // in the partitionKey parameter where the row key (first name) starts with a letter less than that provided in rowKey in the alphabet             

            // Create the table query.
            TableQuery<CustomerEntity> rangeQuery = new TableQuery<CustomerEntity>().Where(
                TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionKey),
                    TableOperators.And,
                    TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.LessThan, rowKey)));

            foreach (CustomerEntity entity in table.ExecuteQuery(rangeQuery))
            {
                Console.WriteLine("\t{0}\t{1}\t{2}\t{3}", entity.PartitionKey, entity.RowKey, entity.Email, entity.PhoneNumber);
            }

        }

        private static TableResult ReplaceEntity(CloudTable table, CustomerEntity entity) 
        {
            //To update an entity, retrieve it from the table service, modify the entity object, and then save the changes back to the table service. The following code changes an existing customer's phone number. Instead of calling Insert, this code uses Replace. This causes the entity to be fully replaced on the server, unless the entity on the server has changed since it was retrieved, in which case the operation will fail. This failure is to prevent your application from inadvertently overwriting a change made between the retrieval and update by another component of your application. The proper handling of this failure is to retrieve the entity again, make your changes (if still valid), and then perform another Replace operation. The next section will show you how to override this behavior.
            if (entity == null)
                throw new ArgumentNullException("entity");

            TableResult result = new TableResult();

            try
            {
                // Create the Replace TableOperation
                TableOperation updateOperation = TableOperation.Replace(entity);
                result = table.Execute(updateOperation);
            }
            catch (StorageException ex)
            {
                if (ex.RequestInformation.HttpStatusCode == 412)
                    Console.WriteLine("Optimistic concurrency violation – entity has changed since it was retrieved.");
                else
                    throw; 
            }

            // Execute the operation.
            return result;
        }

        private static TableResult InsertOrReplaceEntity(CloudTable table, CustomerEntity entity) 
        {
            //Replace operations will fail if the entity has been changed since it was retrieved from the server. Furthermore, you must retrieve the entity from the server first in order for the Replace to be successful. Sometimes, however, you don't know if the entity exists on the server and the current values stored in it are irrelevant - your update should overwrite them all. To accomplish this, you would use an InsertOrReplace operation. This operation inserts the entity if it doesn't exist, or replaces it if it does, regardless of when the last update was made. In the following code example, the customer entity for Ben Smith is still retrieved, but it is then saved back to the server using InsertOrReplace. Any updates made to the entity between the retrieval and update operation will be overwritten.
            if (entity == null)
                throw new ArgumentNullException("entity");

            // Create the InsertOrReplace  TableOperation
            TableOperation insertOrReplaceOperation = TableOperation.InsertOrReplace(entity);

            // Execute the operation.
            return table.Execute(insertOrReplaceOperation);
        }

        private static void DeleteEntity(CloudTable table, CustomerEntity deleteEntity) 
        {
            if (deleteEntity == null)
                throw new ArgumentNullException("deleteEntity");

            // Create the Delete TableOperation.
            TableOperation deleteOperation = TableOperation.Delete(deleteEntity);
            // TODO: Add exception handling 

            // Execute the operation.
            table.Execute(deleteOperation);
        }

        private static bool DeleteTable(CloudTable table)
        {
            return table.DeleteIfExists();
        }
    }
}
