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
using Microsoft.ServiceBus.Messaging;
using System.IO;
using System.Configuration;
using Microsoft.ServiceBus;

namespace ComputeWebJobsSDKServiceBus
{
    // To learn more about Microsoft Azure WebJobs SDK, please see http://go.microsoft.com/fwlink/?LinkID=320976

    //******************************************************************************************************
    // This will show you how to perform common scenarios using the Microsoft Azure Service Bus queue  service using 
    // the Microsoft Azure WebJobs SDK. The scenarios covered include triggering a function when a new message comes
    // on a queue or topic, sending a message on a queue.     
    // 
    // TODO: Create a Storage Account through the Portal or Visual Studio and provide your [AccountName] and 
    //       [AccountKey] in the App.Config http://go.microsoft.com/fwlink/?LinkId=325277            
    //*****************************************************************************************************
    class Program
    {
        private static string _servicesBusConnectionString;

        private static NamespaceManager _namespaceManager;

        public static void Main()
        {
            _servicesBusConnectionString = ConfigurationManager.ConnectionStrings["AzureWebJobsServiceBus"].ConnectionString;
            _namespaceManager = NamespaceManager.CreateFromConnectionString(_servicesBusConnectionString);
            CreateStartMessage();

            JobHostConfiguration config = new JobHostConfiguration()
            {
                ServiceBusConnectionString = _servicesBusConnectionString,
            };

            JobHost host = new JobHost(config);
            host.RunAndBlock();
        }

        private static void CreateStartMessage()
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
            if (!_namespaceManager.QueueExists(Functions.StartQueueName))
            {
                _namespaceManager.CreateQueue(Functions.StartQueueName);
            }

            QueueClient queueClient = QueueClient.CreateFromConnectionString(_servicesBusConnectionString, Functions.StartQueueName);

            using (Stream stream = new MemoryStream())
            using (TextWriter writer = new StreamWriter(stream))
            {
                writer.Write("Start");
                writer.Flush();
                stream.Position = 0;

                queueClient.Send(new BrokeredMessage(stream));
            }

            queueClient.Close();
        }
    }
}
