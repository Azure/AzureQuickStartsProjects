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

using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using DataTableStorage.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataTableStorage
{
    class Program
    {
        static void Main(string[] args)
        {
            //******************************************************************************************************
            // TODO: Create a Storage Account through the Portal and provide your [AccountName] and 
            //       [AccountKey] in the App.Config http://go.microsoft.com/fwlink/?LinkId=325279          
            //*****************************************************************************************************

            // Retrieve storage account from connection string.
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("StorageConnectionString"));

            //1. Create the table client
            Console.WriteLine("1. Create a CloudTableClient");

            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            //2. Demonstrate how to Create a Table
            Console.WriteLine("2. Create a Table in Table Storage");

            bool success = false;
            string tableName = "people";
            CloudTable table = CreateTable(tableClient, tableName, out success);

            if (success)
                Console.WriteLine("\t Created Table named: \t {0}", tableName);
            else
                Console.WriteLine("\t Table {0} already exists", tableName);


            //3. Demonstrate how to Add an entity to a table
            Console.WriteLine("3. Inserting a single Entity to a Table");

            TableResult result = InsertEntityToTable(table, new CustomerEntity("Harp", "Walter")
            {
                Email = "Walter@contoso.com",
                PhoneNumber = "425-555-0101"
            });

            if (result.HttpStatusCode == 201)
            {
                var customerInserted = result.Result as CustomerEntity;
                Console.WriteLine("\t Inserted entity with \r\n\t Etag = {0} and PartitionKey = {1}, RowKey = {2}", customerInserted.ETag, customerInserted.PartitionKey, customerInserted.RowKey);
            }

            //4. Demonstrate how to insert a batch of entities
            Console.WriteLine("4. Inserting a batch of entities to a Table");

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

            IList<TableResult> results = InsertEntitiesToTable(table, customers);

            foreach (var res in results)
            {
                if (res.HttpStatusCode == 201)
                {
                    var customerInserted = res.Result as CustomerEntity;
                    Console.WriteLine("\t Inserted entity with \r\n\t Etag = {0} and PartitionKey = {1}, RowKey = {2}", customerInserted.ETag, customerInserted.PartitionKey, customerInserted.RowKey);
                }
            }

            //5. Demonstrate how to query customers in a given parition
            Console.WriteLine("5. Query entities in a given partition");

            foreach (CustomerEntity entity in QueryAllEntitiesInPartition<CustomerEntity>(table, "Smith"))
                Console.WriteLine("\t {0}\t{1}\t{2}\t{3}", entity.PartitionKey, entity.RowKey, entity.Email, entity.PhoneNumber);

            //6. Demonstrate how to query customers in a partition within a given range
            Console.WriteLine("6. Query entities in a partition within a given range");

            foreach (CustomerEntity entity in QueryAllEntitiesInPartition<CustomerEntity>(table, "Smith", "E"))
                Console.WriteLine("\t{0}\t{1}\t{2}\t{3}", entity.PartitionKey, entity.RowKey, entity.Email, entity.PhoneNumber);

            //7. Demonstrate how to retrive a single entity
            Console.WriteLine("7. Retrieve a single entity using a PartitionKey and RowKey");

            CustomerEntity customer = GetSingleEntity<CustomerEntity>(table, "Smith", "Ben");
            if (customer != null)
                Console.WriteLine("\t{0}\t{1}\t{2}\t{3}", customer.PartitionKey, customer.RowKey, customer.Email, customer.PhoneNumber);

            //8. Demonstrate how to Replace an entity 
            Console.WriteLine("8. Replace a given entity");

            customer.PhoneNumber = "425-555-0105";// Change the phone number.
            ReplaceEntity(table, customer);

            //9. Demonstrate how to Insert or Replace an entity                
            Console.WriteLine("9. Insert OR Replace a given entity");

            customer.PhoneNumber = "425-555-1234"; // Change the phone number.
            InsertOrReplaceEntity(table, customer);

            //10. Demonstrate how to Query a subset of entity properties, in this case we will do just email
            Console.WriteLine("10. Query a subset of entity properties");

            foreach (string projectedEmail in QueryEntityPropertySubSet(table, "Email"))
                Console.WriteLine("\t {0}", projectedEmail);

            //11. Demonstrate how to Delete an entity
            Console.WriteLine("11. Delete an entity");

            DeleteEntity(table, customer);

            //12. Demonstrate how to Delete a table
            Console.WriteLine("10. Delete a table");

            DeleteTable(table);

            Console.Read();
        }

        private static IEnumerable<string> QueryEntityPropertySubSet(CloudTable table, string property)
        {
            // Define the query, and only select the provided property
            TableQuery<DynamicTableEntity> projectionQuery = new TableQuery<DynamicTableEntity>().Select(new List<string> { property });

            // Define an entity resolver to work with the entity after retrieval.
            EntityResolver<string> resolver = (pk, rk, ts, props, etag) => props.ContainsKey(property) ? props[property].StringValue : null;

            return table.ExecuteQuery(projectionQuery, resolver, null, null);
        }

        private static CloudTable CreateTable(CloudTableClient tableClient, string tableName, out bool success)
        {
            // Create the table if it doesn't exist
            CloudTable table = tableClient.GetTableReference(tableName);
            success = table.CreateIfNotExists();

            return table;
        }

        private static TableResult InsertEntityToTable<T>(CloudTable table, T entity) where T : ITableEntity
        {
            // The operation to be performed is represented by a TableOperation object. 
            // The following code example shows the use of the CloudTable object and then a CustomerEntity object. 
            // To prepare the operation, a TableOperation is created to insert the customer entity into the table. 
            // Finally, the operation is executed by calling CloudTable.Execute.        

            // Create the TableOperation that inserts the customer entity.
            TableOperation insertOperation = TableOperation.Insert(entity);
            TableResult result = table.Execute(insertOperation);

            // Execute the insert operation
            return result;
        }

        private static IList<TableResult> InsertEntitiesToTable<T>(CloudTable table, IEnumerable<T> entities) where T : ITableEntity
        {
            //You can insert a batch of entities into a table in one write operation. Some other notes on batch operations:
            //  1.You can perform updates, deletes, and inserts in the same single batch operation.
            //  2.A single batch operation can include up to 100 entities.
            //  3.All entities in a single batch operation must have the same partition key.
            //  4.While it is possible to perform a query as a batch operation, it must be the only operation in the batch.
            //The following code example adds each entity in the entities collection to a TableBatchOperation using the Insert method. 
            //Then CloudTable.Execute is called to execute the operation.

            // Create the batch operation.
            TableBatchOperation batchOperation = new TableBatchOperation();

            // Add entities to the batch insert operation.
            foreach (var entity in entities)
                batchOperation.Insert(entity);

            // Execute the batch operation.
            return table.ExecuteBatch(batchOperation);
        }

        private static IEnumerable<T> QueryAllEntitiesInPartition<T>(CloudTable table, string paritionKey) where T : ITableEntity, new()
        {
            // To query a table for all entities in a partition, use a TableQuery object. 
            // The following code example specifies a filter for entities where the parameter named partitionKey is the partition key. 
            // This example prints the fields of each entity in the query results to the console.

            TableQuery<T> query = new TableQuery<T>().Where
                (TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, paritionKey));

            return table.ExecuteQuery(query);
        }

        private static IEnumerable<T> QueryAllEntitiesInPartition<T>(CloudTable table, string partitionKey, string rowKey) where T : ITableEntity, new()
        {
            // If you don't want to query all the entities in a partition, you can specify a range by combining the partition key 
            // filter with a row key filter. The following code example uses two filters to get all entities in partition provided 
            // in the partitionKey parameter where the row key (first name) starts with a letter less than that provided in rowKey in the alphabet             

            // Create the table query.
            TableQuery<T> rangeQuery = new TableQuery<T>().Where(
                TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionKey),
                    TableOperators.And,
                    TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.LessThan, rowKey)));

            return table.ExecuteQuery(rangeQuery);
        }

        private static T GetSingleEntity<T>(CloudTable table, string partitionKey, string rowKey) where T : TableEntity
        {
            // You can write a query to retrieve a single, specific entity. The following code uses a TableOperation to specify the customer 
            // 'Ben Smith'. This method returns just one entity, rather than a collection, and the returned value in TableResult.Result is a 
            // CustomerEntity. Specifying both partition and row keys in a query is the fastest way to retrieve a single entity from the Table 
            // service.
            TableOperation retrieveOperation = TableOperation.Retrieve<CustomerEntity>(partitionKey, rowKey);

            return table.Execute(retrieveOperation).Result as T;
        }

        private static TableResult ReplaceEntity<T>(CloudTable table, T entity) where T : ITableEntity
        {
            //To update an entity, retrieve it from the table service, modify the entity object, and then save the changes back to the table service. The following code changes an existing customer's phone number. Instead of calling Insert, this code uses Replace. This causes the entity to be fully replaced on the server, unless the entity on the server has changed since it was retrieved, in which case the operation will fail. This failure is to prevent your application from inadvertently overwriting a change made between the retrieval and update by another component of your application. The proper handling of this failure is to retrieve the entity again, make your changes (if still valid), and then perform another Replace operation. The next section will show you how to override this behavior.
            if (entity == null)
                throw new ArgumentNullException("entity");

            // Create the Replace TableOperation
            TableOperation updateOperation = TableOperation.Replace(entity);

            // Execute the operation.
            return table.Execute(updateOperation);
        }

        private static TableResult InsertOrReplaceEntity<T>(CloudTable table, T entity) where T : ITableEntity
        {
            //Replace operations will fail if the entity has been changed since it was retrieved from the server. Furthermore, you must retrieve the entity from the server first in order for the Replace to be successful. Sometimes, however, you don't know if the entity exists on the server and the current values stored in it are irrelevant - your update should overwrite them all. To accomplish this, you would use an InsertOrReplace operation. This operation inserts the entity if it doesn't exist, or replaces it if it does, regardless of when the last update was made. In the following code example, the customer entity for Ben Smith is still retrieved, but it is then saved back to the server using InsertOrReplace. Any updates made to the entity between the retrieval and update operation will be overwritten.
            if (entity == null)
                throw new ArgumentNullException("entity");

            // Create the InsertOrReplace  TableOperation
            TableOperation insertOrReplaceOperation = TableOperation.InsertOrReplace(entity);

            // Execute the operation.
            return table.Execute(insertOrReplaceOperation);
        }

        private static void DeleteEntity<T>(CloudTable table, T deleteEntity) where T : ITableEntity
        {
            if (deleteEntity == null)
                throw new ArgumentNullException("deleteEntity");

            // Create the Delete TableOperation.
            TableOperation deleteOperation = TableOperation.Delete(deleteEntity);

            // Execute the operation.
            table.Execute(deleteOperation);
        }

        private static bool DeleteTable(CloudTable table)
        {
            return table.DeleteIfExists();
        }
    }
}
