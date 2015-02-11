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
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ComputeWebJobsSDKBlobStorage
{
    //******************************************************************************************************
    // This will show you how to perform common scenarios using the Microsoft Azure Blob storage service using 
    // the Microsoft Azure WebJobs SDK. The scenarios covered include triggering a function when a new blob is detected
    // or updated in a blob container and bind the contents of the blob to BCL types so you can read/ write the contents.   
    // 
    // To learn more about Microsoft Azure WebJobs SDK, please see http://go.microsoft.com/fwlink/?LinkID=320976
    //
    // TODO: Create a Storage Account through the Portal or Visual Studio and provide your [AccountName] and 
    //       [AccountKey] in the App.Config http://go.microsoft.com/fwlink/?LinkId=325277            
    //*****************************************************************************************************
    class Program
    {
        static void Main()
        {
            if (!VerifyConfiguration())
            {
                Console.ReadLine();
                return;
            }

            CreateDemoData();

            JobHost host = new JobHost();
            host.RunAndBlock();
        }

        private static bool VerifyConfiguration()
        {
            string webJobsDashboard = ConfigurationManager.ConnectionStrings["AzureWebJobsDashboard"].ConnectionString;
            string webJobsStorage = ConfigurationManager.ConnectionStrings["AzureWebJobsStorage"].ConnectionString;

            bool configOK = true;
            if (string.IsNullOrWhiteSpace(webJobsDashboard) || string.IsNullOrWhiteSpace(webJobsStorage))
            {
                configOK = false;
                Console.WriteLine("Please add the Azure Storage account credentials in App.config");
            }
            return configOK;
        }

        private static void CreateDemoData()
        {
            Console.WriteLine("Creating Demo data");
            Console.WriteLine("Functions will store logs in the specified Azure storage account. The functions take in a parameter called TextWriter for logging");
           
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.ConnectionStrings["AzureWebJobsStorage"].ConnectionString);
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference("input");
            container.CreateIfNotExists();

            CloudBlockBlob blob = container.GetBlockBlobReference("BlobOperations.txt");
            blob.UploadText("Hello world!");

            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            CloudQueue queue = queueClient.GetQueueReference("persons");
            queue.CreateIfNotExists();

            Person person = new Person()
            {
                Name = "John",
                Age = 42
            };

            queue.AddMessage(new CloudQueueMessage(JsonConvert.SerializeObject(person)));
        }
    }
}
