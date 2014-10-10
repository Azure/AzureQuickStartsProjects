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
namespace DataStorageQueueSample
{
    using Microsoft.WindowsAzure;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Queue;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Azure Queue Service Sample - The Queue Service provides reliable messaging for workflow processing and for communication 
    /// between loosely coupled components of cloud services. This sample demonstrates how to perform common tasks including  
    /// inserting, peeking, getting and deleting queue messages, as well as creating and deleting queues.     
    /// 
    /// Note: This sample uses the .NET 4.5 asynchronous programming model to demonstrate how to call the Storage Service using the 
    /// storage client libraries asynchronous API's. When used in real applications this approach enables you to improve the 
    /// responsiveness of your application. Calls to the storage service are prefixed by the await keyword. 
    /// 
    /// Documentation References: 
    /// - What is a Storage Account - http://azure.microsoft.com/en-us/documentation/articles/storage-whatis-account/
    /// - Getting Started with Queues - http://azure.microsoft.com/en-us/documentation/articles/storage-dotnet-how-to-use-queues/
    /// - Queue Service Concepts - http://msdn.microsoft.com/en-us/library/dd179353.aspx
    /// - Queue Service REST API - http://msdn.microsoft.com/en-us/library/dd179363.aspx
    /// - Queue Service C# API - http://go.microsoft.com/fwlink/?LinkID=398944
    /// - Storage Emulator - http://msdn.microsoft.com/en-us/library/azure/hh403989.aspx
    /// - Asynchronous Programming with Async and Await  - http://msdn.microsoft.com/en-us/library/hh191443.aspx
    /// </summary>
    public class Program
    {
        // *************************************************************************************************************************
        // Instructions: This sample can be run using either the Azure Storage Emulator that installs as part of this SDK - or by
        // updating the App.Config file with your AccountName and Key. 
        // 
        // To run the sample using the Storage Emulator (default)
        //      1. Start the Azure Storage Emulator (once only), press the Start button or the Windows key. Begin Azure Storage Emulator, 
        //         and Azure Storage Emulator from the list of applications.
        //      2. Set breakpoints and run the project using F10. 
        // 
        // To run the sample using the Storage Service
        //      1. Create a Storage Account through the Azure Portal and provide your [AccountName] and [AccountKey] in 
        //         the App.Config file. See http://go.microsoft.com/fwlink/?LinkId=325277 for more information
        //      2. Set breakpoints and run the project using F10. 
        // 
        // *************************************************************************************************************************
        public static void Main(string[] args)
        {
            Console.WriteLine("Azure Storage Queue Sample\n");

            // Create or reference an existing queue 
            CloudQueue queue = CreateQueue().Result;

            // Demonstrate basic queue functionality 
            BasicQueueOperations(queue).Wait();

            // Demonstrate how to update an enqueued message
            UpdateEnqueuedMessage(queue).Wait();

            // Demonstrate advanced functionality such as processing of batches of messages
            ProcessBatchOfMessages(queue).Wait();

            // When you delete a queue it could take several seconds before you can recreate a queue with the same
            // name - hence to enable you to run the demo in quick succession the queue is not deleted. If you want 
            // to delete the queue uncomment the line of code below. 
            // DeleteQueue(queue).Wait();

            Console.WriteLine("Press any key to exit");
            Console.Read();
        }


        /// <summary>
        /// Create a queue for the sample application to process messages in. 
        /// </summary>
        /// <returns>A CloudQueue object</returns>
        private static async Task<CloudQueue> CreateQueue()
        {
            // Retrieve storage account information from connection string.
            CloudStorageAccount storageAccount = CreateStorageAccountFromConnectionString(CloudConfigurationManager.GetSetting("StorageConnectionString"));

            // Create a queue client for interacting with the queue service
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            
            Console.WriteLine("1. Create a queue for the demo\n");
            CloudQueue queue = queueClient.GetQueueReference("samplequeue");
            try
            {
                await queue.CreateIfNotExistsAsync();
            }
            catch (StorageException ex)
            {
                Console.WriteLine("If you are running with the default configuration please make sure you have started the storage emulator. Press the Windows key and type Azure Storage to select and run it from the list of applications - then restart the sample.");
                Console.ReadLine(); 
                throw; 
            }

            return queue; 
        }

        /// <summary>
        /// Demonstrate basic queue operations such as adding a message to a queue, peeking at the front of the queue and dequeing a message.
        /// </summary>
        /// <param name="queue">The sample queue</param>
        private static async Task BasicQueueOperations(CloudQueue queue)
        {
            // Insert a message into the queue using the AddMessage method. 
            Console.WriteLine("2. Insert a single message into a queue\n");
            await queue.AddMessageAsync(new CloudQueueMessage("Hello World!"));

            // Peek at the message in the front of a queue without removing it from the queue using PeekMessage (PeekMessages lets you peek >1 message)
            Console.WriteLine("3. Peek at the next message");
            CloudQueueMessage peekedMessage = await queue.PeekMessageAsync();
            Console.WriteLine("The peeked message is: {0}\n", peekedMessage.AsString);

            // You de-queue a message in two steps. Call GetMessage at which point the message becomes invisible to any other code reading messages 
            // from this queue for the a default period of 30 seconds. To finish removing the message from the queue, you call DeleteMessage. 
            // This two-step process ensures that if your code fails to process a message due to hardware or software failure, another instance 
            // of your code can get the same message and try again. 
            Console.WriteLine("4. De-queue the next message");
            CloudQueueMessage message = await queue.GetMessageAsync();
            if (message != null)
            {
                Console.WriteLine("Processing & deleting message with content: {0}\n", message.AsString);
                await queue.DeleteMessageAsync(message);
            }
        }

        /// <summary>
        /// Update an enqueued message and its visibility time. For workflow scenarios this could enable you to update 
        /// the status of a task as well as extend the visibility timeout in order to provide more time for a client 
        /// continue working on the message before another client can see the message. 
        /// </summary>
        /// <param name="queue">The sample queue</param>
        private static async Task UpdateEnqueuedMessage(CloudQueue queue)
        {
            // Insert another test message into the queue 
            Console.WriteLine("5. Insert another test message \n");
            await queue.AddMessageAsync(new CloudQueueMessage("Hello World Again!"));

            Console.WriteLine("6. Change the contents of a queued message\n");
            CloudQueueMessage message = await queue.GetMessageAsync();
            message.SetMessageContent("Updated contents.");
            await queue.UpdateMessageAsync(
                message, 
                TimeSpan.FromSeconds(0.0),  // For the purpose of the sample make the update visible immediately
                MessageUpdateFields.Content | 
                MessageUpdateFields.Visibility);
        }

        /// <summary>
        /// Demonstrate adding a number of messages, checking the message count and batch retrieval of messages. During retrieval we 
        /// also set the visibility timeout to 5 minutes. Visibility timeout is the amount of time message is not visible to other 
        /// clients after a GetMessageOperation assuming DeleteMessage is not called. 
        /// </summary>
        /// <param name="queue">The sample queue</param>
        private static async Task ProcessBatchOfMessages(CloudQueue queue)
        {
            // Enqueue 20 messages by which to demonstrate batch retrieval
            Console.WriteLine("7. Enqueue 20 messages."); 
            for (int i = 0; i < 20; i++)
            {
                await queue.AddMessageAsync(new CloudQueueMessage(string.Format("{0} - {1}", i, "Hello World")));
            }

            // The FetchAttributes method asks the Queue service to retrieve the queue attributes, including an approximation of message count 
            Console.WriteLine("\n8. Get the queue length");
            queue.FetchAttributes();
            int? cachedMessageCount = queue.ApproximateMessageCount;
            Console.WriteLine("Number of messages in queue: {0}\n", cachedMessageCount);

            // Dequeue a batch of 20 messages (up to 32) and set visibility timeout to 5 minutes. 
            Console.WriteLine("\n9. Dequeue 20 messages, allowing 5 minutes for the clients to process.");
            foreach (CloudQueueMessage msg in await queue.GetMessagesAsync(20, TimeSpan.FromMinutes(5), null, null))
            {
                Console.WriteLine("Processing & deleting message with content: {0}", msg.AsString);

                // Process all messages in less than 5 minutes, deleting each message after processing.
                await queue.DeleteMessageAsync(msg);
            }
        }

        /// <summary>
        /// Validate the connection string information in app.config and throws an exception if it looks like 
        /// the user hasn't updated this to valid values. 
        /// </summary>
        /// <param name="storageConnectionString">The storage connection string</param>
        /// <returns>CloudStorageAccount object</returns>
        private static CloudStorageAccount CreateStorageAccountFromConnectionString(string storageConnectionString)
        {
            CloudStorageAccount storageAccount;
            try
            {
                storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            }
            catch (System.FormatException)
            {
                Console.WriteLine("Invalid storage account information provided. Please confirm the AccountName and AccountKey are valid in the app.config file - then restart the sample.");
                Console.ReadLine(); 
                throw;
            }

            return storageAccount;
        }

        /// <summary>
        /// Delete the queue that was created for this sample
        /// </summary>
        /// <param name="queue">The sample queue to delete</param>
        private static async Task DeleteQueue(CloudQueue queue)
        {
            Console.WriteLine("10. Delete the queue\n");
            await queue.DeleteAsync();
        }
    }
}
