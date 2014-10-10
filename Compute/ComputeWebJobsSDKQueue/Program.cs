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

using Microsoft.Azure.WebJobs;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using System;
using System.Configuration;

namespace ComputeWebJobsSDKStorageQueue
{
    // To learn more about Microsoft Azure WebJobs SDK, please see http://go.microsoft.com/fwlink/?LinkID=320976

    //******************************************************************************************************
    // This will show you how to perform common scenarios using the Microsoft Azure Queue storage service using 
    // the Microsoft Azure WebJobs SDK. The scenarios covered include triggering a function when a new message comes
    // on a queue, sending a message on a queue.   
    // 
    // TODO: Create a Storage Account through the Portal or Visual Studio and provide your [AccountName] and 
    //       [AccountKey] in the App.Config http://go.microsoft.com/fwlink/?LinkId=325277            
    //*****************************************************************************************************

    class Program
    {
        static void Main()
        {
            CreateDemoData();

            JobHost host = new JobHost();
            host.Start();
        }

        private static void CreateDemoData()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.ConnectionStrings["AzureWebJobsStorage"].ConnectionString);

            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            CloudQueue queue = queueClient.GetQueueReference("initialorder");
            queue.CreateIfNotExists();

            Order person = new Order()
            {
                Name = "Alex",
                OrderId = Guid.NewGuid().ToString("N").ToLower()
            };

            queue.AddMessage(new CloudQueueMessage(JsonConvert.SerializeObject(person)));
        }
    }
}
