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
using Microsoft.WindowsAzure.Storage.Queue;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataStorageQueue
{
    class Program
    {
        static void Main(string[] args)
        {
            //******************************************************************************************************
            // This will show you how to perform common scenarios using the Microsoft Azure Queue storage service using 
            // the Microsoft Azure Storage Client for .NET. The scenarios covered include inserting, peeking, getting, 
            // and deleting queue messages, as well as creating and deleting queues.             
            // 
            // TODO: Create a Storage Account through the Portaland provide your [AccountName] and 
            //       [AccountKey] in the App.Config http://go.microsoft.com/fwlink/?LinkId=325277            
            //*****************************************************************************************************

            // Retrieve storage account from connection string.
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("StorageConnectionString"));

            //1. Create the queue client
            Console.WriteLine("1. Create a CloudQueueClient");

            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();

            //2. Create a Queue
            Console.WriteLine("2. Create a Queue");

            //Use the queueClient object to get a reference to the queue you want to use. You can create the queue if it doesn't exist.
            CloudQueue queue = queueClient.GetQueueReference("myqueue");

            if (queue.CreateIfNotExists())
                Console.WriteLine("\t Created Queue named: \t {0}", queue.Name);
            else
                Console.WriteLine("\t Table {0} already exists", queue.Name);

            //3. Insert a message into a queue
            Console.WriteLine("3. Insert a single message into a queue");

            // To insert a message into an existing queue, first create a new CloudQueueMessage. 
            // Next, call the AddMessage method. A CloudQueueMessage can be created from either a string (in UTF-8 format) or a byte array.             

            // Note there are multiple overloads to the AddMessage method that allows you to add TTL, visibility delay and others
            queue.AddMessage(new CloudQueueMessage("Hello World"));

            //4. Peek at the next message
            Console.WriteLine("4. Peek at the next Message");

            //You can peek at the message in the front of a queue without removing it from the queue by calling the PeekMessage method
            // If you want to peek N messages use PeekMessages
            CloudQueueMessage peekedMessage = queue.PeekMessage();

            Console.WriteLine("\t The peeked message is: {0}", peekedMessage.AsString);

            //5. How to change the contents of a queued message       
            Console.WriteLine("5. Change the contents of a queued message");
            // You can change the contents of a message in-place in the queue. If the message represents a work task, 
            // you could use this feature to update the status of the work task. 
            // The following code updates the queue message with new contents, and sets the visibility to be visibile immeidately.
            // In a real scenario you could however set the timeout to extend another 60 seconds. This would savee the state of work associated with 
            // the message, and gives the client another minute to continue working on the message. 
            // You could use this technique to track multi-step workflows on queue messages, without having to start over from 
            // the beginning if a processing step fails due to hardware or software failure. Typically, you would keep a retry count as well, and if
            // the message is retried more than n times, you would delete it. 
            // This protects against a message that triggers an application error each time it is processed.

            // Get the message from the queue and update the message contents.
            CloudQueueMessage message = queue.GetMessage();
            message.SetMessageContent("\t Updated contents.");

            queue.UpdateMessage(message, TimeSpan.FromSeconds(0.0), // Make it visible immediately.
                MessageUpdateFields.Content | MessageUpdateFields.Visibility);

            //6. How to De-queue the next message
            Console.WriteLine("6. De-queue the next message");

            // Your code de-queues a message from a queue in two steps. When you call GetMessage, you get the next message in a 
            // queue. Unlike PeekMessage a message returned from GetMessage becomes invisible to any other code reading messages from this queue. 
            // By default, this message stays invisible for 30 seconds. To finish removing the message from the queue, you must also call 
            // DeleteMessage. This two-step process of removing a message assures that if your code fails to process a message due to hardware or 
            // software failure, another instance of your code can get the same message and try again. Your code calls DeleteMessage right after 
            // the message has been processed.

            // Get the next message
            message = queue.GetMessage();

            //Process the message in less than 30 seconds, and then delete the message. Note you can set the visiblity timeout on GetMessage(...)
            if (message != null)
            {
                Console.WriteLine("\t Processing & deleting message with content: {0}", message.AsString);
                queue.DeleteMessage(message);
            }

            //7. Leverage additional options for de-queuing messages
            Console.WriteLine("7. Deuqueue N x messages and set visibility timeout to 5 minutes");
            QueueSomeMessages(queue, 21, "sample message content");//Add some messages to be used for the remainder of the steps below

            // There are two ways you can customize message retrieval from a queue. First, you can get a batch of messages (up to 32). 
            // Second, you can set a longer or shorter invisibility timeout, allowing your code more or less time to fully process each message. 
            // The following code example uses the GetMessages method to get 20 messages in one call. Then it processes each message using a 
            // foreach loop. It also sets the invisibility timeout to five minutes for each message. Note that the 5 minutes starts for all 
            // messages at the same time, so after 5 minutes have passed since the call to GetMessages, any messages which have not been deleted 
            // will become visible again.

            // Dequeue 20 messages and set visibility timeout to 5 minutes.
            foreach (CloudQueueMessage msg in queue.GetMessages(20, TimeSpan.FromMinutes(5)))
            {
                Console.WriteLine("\t Processing & deleting message with content: {0}", msg.AsString);
                // Process all messages in less than 5 minutes, deleting each message after processing.
                queue.DeleteMessage(msg);
            }

            //8. Get the queue length
            Console.WriteLine("8. Get the Queue Length");

            // You can get an estimate of the number of messages in a queue. The FetchAttributes method asks the Queue service to retrieve 
            // the queue attributes, including the message count. The ApproximateMethodCount property returns the last value retrieved by 
            // the FetchAttributes method, without calling the Queue service

            // Fetch the queue attributes.
            queue.FetchAttributes();

            // Retrieve the cached approximate message count.
            int? cachedMessageCount = queue.ApproximateMessageCount;

            Console.WriteLine("\t Number of messages in queue: {0}", cachedMessageCount);

            //9. Delete the queue
            Console.WriteLine("9. Deleting the Queue");
            queue.Delete();

            Console.WriteLine("Press any key to exit");
            Console.Read();
        }

        private static void QueueSomeMessages(CloudQueue queue, int count, string message)
        {
            for (int i = 0; i < count; i++)
                queue.AddMessage(new CloudQueueMessage(string.Format("{0} - {1}", i, message)));
        }
    }
}
