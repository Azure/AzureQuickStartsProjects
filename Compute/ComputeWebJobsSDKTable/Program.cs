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
using Microsoft.Azure.WebJobs;
using System.Configuration;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage;

namespace ComputeWebJobsSDKTableStorage
{
    // To learn more about Microsoft Azure WebJobs SDK, please see http://go.microsoft.com/fwlink/?LinkID=320976

    //******************************************************************************************************
    // This will show you how to perform common scenarios using the Microsoft Azure Table storage service using 
    // the Microsoft Azure WebJobs SDK. The scenarios covered include reading and writing data to Tables.
       // 
    // TODO: Create a Storage Account through the Portal or Visual Studio and provide your [AccountName] and 
    //       [AccountKey] in the App.Config http://go.microsoft.com/fwlink/?LinkId=325277            
    //*****************************************************************************************************

    class Program
    {

        // The following code will write a sentence as a message on a queue called textinput.
        // The SDK will trigger a function called CountAndSplitInWords which is listening on textinput queue.
        // CountAndSplitInWords will split the sentence into words and store results in Table storage.
        // 
        static void Main()
        {
            CreateDemoData();

            JobHost host = new JobHost();
            Task callTask = host.CallAsync(typeof(Functions).GetMethod("ManualTrigger"));

            Console.WriteLine("Waiting for async operation...");
            callTask.Wait();
            Console.WriteLine("Task completed: " + callTask.Status);

            host.RunAndBlock();
        }

        private static void CreateDemoData()
        {
            string WebJobsDashboard = ConfigurationManager.AppSettings["AzureWebJobsDashboard"];
            string WebJobsStorage = ConfigurationManager.AppSettings["AzureWebJobsStorage"];

            if (string.IsNullOrWhiteSpace(WebJobsDashboard) || string.IsNullOrWhiteSpace(WebJobsStorage) ||
                String.Equals(WebJobsDashboard, "AzureWebJobsDashboard", StringComparison.OrdinalIgnoreCase) ||
                String.Equals(WebJobsStorage, "AzureWebJobsDashboard", StringComparison.OrdinalIgnoreCase))
            {

                Console.WriteLine("Please add the Azure Storage account credentials in App.config");
                Console.ReadKey();
            }

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.ConnectionStrings["AzureWebJobsStorage"].ConnectionString);

            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            CloudQueue queue = queueClient.GetQueueReference("textinput");
            queue.CreateIfNotExists();

            queue.AddMessage(new CloudQueueMessage("Hello hello world"));
        }
    }
}
