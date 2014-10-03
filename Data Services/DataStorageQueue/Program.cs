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
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;

namespace DataStorageQueueSample
{
    /// <summary>
    /// Azure Queues Service Sample - Demonstrate how to perform common tasks using the Microsoft Azure Queue storage 
    /// including inserting, peeking, getting and deleting queue messages, as well as creating and deleting queues.             
    /// 
    /// Documentation References: 
    /// - What is a Storage Account - http://azure.microsoft.com/en-us/documentation/articles/storage-whatis-account/
    /// - Getting Started with Queues - http://azure.microsoft.com/en-us/documentation/articles/storage-dotnet-how-to-use-queues/
    /// - Queue Service Concepts - http://msdn.microsoft.com/en-us/library/dd179353.aspx
    /// - Queue Service REST API - http://msdn.microsoft.com/en-us/library/dd179363.aspx
    /// - Queue Service C# API - http://go.microsoft.com/fwlink/?LinkID=398944
    /// </summary>

    class Program
    {
        //*************************************************************************************************************************
        // Instructions: 
        // TODO: 1. Create a Storage Account through the Portal and provide your [AccountName] and [AccountKey] in the App.Config 
        //          See http://go.microsoft.com/fwlink/?LinkId=325277 for more information
        //       2. Set breakpoints and run the project
        //*************************************************************************************************************************
        static void Main(string[] args)
        {
            Console.WriteLine("Azure Storage Queue Sample\n");

            // Retrieve storage account information from connection string.
            CloudStorageAccount storageAccount = CreateStorageAccountFromConnectionString(CloudConfigurationManager.GetSetting("StorageConnectionString"));

            // Create a queue client for interacting with the queue service
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();

            Console.WriteLine("1. Create a queue");
            // Use the queueClient object to get a reference to the queue you want to use. Create the queue if it doesn't exist.
            CloudQueue queue = queueClient.GetQueueReference("myqueue");
            if (queue.CreateIfNotExists())
                Console.WriteLine("Created queue named: {0}\n", queue.Name);
            else
                Console.WriteLine("Table {0} already exists\n", queue.Name);

            Console.WriteLine("2. Insert a single message into a queue\n");
            // To insert a message into an existing queue, create a new CloudQueueMessage and call the AddMessage method. 
            // Note there are multiple overloads to the AddMessage method that allows you to add TTL, visibility delay and others
            queue.AddMessage(new CloudQueueMessage("Hello World"));

            Console.WriteLine("3. Peek at the next message");
            // Peek at the message in the front of a queue without removing it from the queue using PeekMessage (PeekMessages lets you peek >1 message)
            CloudQueueMessage peekedMessage = queue.PeekMessage();
            Console.WriteLine("The peeked message is: {0}\n", peekedMessage.AsString);

            Console.WriteLine("4. Change the contents of a queued message\n");

            // Updates an enqueued message with new contents and update the visibility timeout. For workflow scenarios this could 
            // enable you to update the status of a task as well as extend the visibility timeout in order to provide more time for 
            // a client continue working on the message before another client can see the message. 
            CloudQueueMessage message = queue.GetMessage();
            message.SetMessageContent("Updated contents.");

            queue.UpdateMessage(message, TimeSpan.FromSeconds(0.0), // For the purpose of the sample make the update visible immediately
                MessageUpdateFields.Content | MessageUpdateFields.Visibility);

            Console.WriteLine("5. De-queue the next message");
            // You de-queue a message in two steps. Call GetMessage at which point the message becomes invisible to any other code reading messages 
            // from this queue for the a default period of 30 seconds. To finish removing the message from the queue, you call DeleteMessage. 
            // This two-step process ensures that if your code fails to process a message due to hardware or software failure, another instance 
            // of your code can get the same message and try again. 
            message = queue.GetMessage();
            if (message != null)
            {
                Console.WriteLine("Processing & deleting message with content: {0}\n", message.AsString);
                queue.DeleteMessage(message);
            }

            Console.WriteLine("6. Dequeue N x messages and set visibility timeout to 5 minutes");
            QueueSomeMessages(queue, 21, "sample message content");//Add some messages to be used for the remainder of the steps below

            // Dequeue a batch of 20 messages (up to 32) and set visibility timeout to 5 minutes.
            foreach (CloudQueueMessage msg in queue.GetMessages(20, TimeSpan.FromMinutes(5)))
            {
                Console.WriteLine("Processing & deleting message with content: {0}", msg.AsString);
                // Process all messages in less than 5 minutes, deleting each message after processing.
                queue.DeleteMessage(msg);
            }

            Console.WriteLine("\n7. Get the queue length");

            // The FetchAttributes method asks the Queue service to retrieve the queue attributes, including an approximation of message count 
            queue.FetchAttributes();
            int? cachedMessageCount = queue.ApproximateMessageCount;
            Console.WriteLine("Number of messages in queue: {0}\n", cachedMessageCount);

            Console.WriteLine("8. Delete the queue\n");
            queue.Delete();

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

        /// <summary>
        /// Helper method to add a number of test messages to a queue
        /// </summary>
        private static void QueueSomeMessages(CloudQueue queue, int count, string message)
        {
            for (int i = 0; i < count; i++)
                queue.AddMessage(new CloudQueueMessage(string.Format("{0} - {1}", i, message)));
        }
    }
}
