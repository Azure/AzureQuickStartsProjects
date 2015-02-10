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
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Management.Models;
using Microsoft.WindowsAzure.Management.Storage.Models;

namespace DeployManageAzureStorage
{
    class Program
    {
        //***********************************************************************************************
        // The Microsoft Azure Management Libraries are intended for developers who want to automate 
        // the management, provisioning, deprovisioning and test of cloud infrastructure with ease.            
        // These services support Microsoft Azure Virtual Machines, Hosted Services, Storage, Virtual Networks, 
        // Web Sites and core data center infrastructure management. For more information on the Management
        // Libraries for .NET, see https://msdn.microsoft.com/en-us/library/azure/dn722415.aspx. 
        //
        // If you dont have a Microsoft Azure subscription you can get a FREE trial account here:
        // http://go.microsoft.com/fwlink/?LinkId=330212
        //
        // This Quickstart demonstrates using WAML how to create, delete, and configure storage service 
        // accounts and credentials.
        //
        // TODO: Perform the following steps before running the sample 
        //  1. Download your *.publishsettings file from the Microsoft Azure management portal and save to
        //     to your local drive http://go.microsoft.com/fwlink/?LinkID=276844
        //  2. Set the PublishSettingsFilePath property below.  
        //  3. Run
        //***********************************************************************************************

        static void Main(string[] args)
        {
            var serviceParameters = new ManagementControllerParameters
            {
                PublishSettingsFilePath = @"C:\Your.publishsettings",
                Region = LocationNames.WestUS,
                StorageAccountName = Path.GetFileNameWithoutExtension(Path.GetRandomFileName()),
                StorageAccountType = StorageAccountTypes.StandardGRS
            };

            if (!VerifyConfiguration(serviceParameters))
            {
                Console.ReadLine();
                return;
            }

            int step = 1;

            CheckNameAvailability(serviceParameters, step++);

            // use one or the other of these two variations on SetupStorageAccount
            //SetupStorageAccount(serviceParameters, step++); // Sync version

            Task.WaitAll(SetupStorageAccountAsync(serviceParameters, step++));  // Async version
            // *******************************************************************

            UpdateStorageAccount(serviceParameters, step++);

            GetStorageAccountConnectionString(serviceParameters, step++);

            RegenerateKeys(serviceParameters, step++);

            GetStorageAccountConnectionString(serviceParameters, step++);

            GetStorageAccountProperties(serviceParameters, step++);

            ListStorageAccounts(serviceParameters, step++);

            TearDownStorageAccount(serviceParameters, step++);

            Console.WriteLine("Done");
            Console.ReadLine();
        }

        private static bool VerifyConfiguration(ManagementControllerParameters serviceParameters)
        {
            bool configOK = true;
            if (!File.Exists(serviceParameters.PublishSettingsFilePath))
            {
                configOK = false;
                Console.WriteLine("Please download your .publishsettings file and specify the location in the Main method.");
            }
            return configOK;
        }
        private static void CheckNameAvailability(ManagementControllerParameters managementControllerParameters, int step)
        {
            using (var controller = new ManagementController(managementControllerParameters))
            {
                Console.WriteLine("{1}. Check availability of the requested name: {0}", managementControllerParameters.StorageAccountName, step);
                ConsoleContinuePrompt("Check Availability");

                bool available = controller.CheckNameAvailbility();

                Console.WriteLine("   The requested name {0} {1} available.", managementControllerParameters.StorageAccountName, (available ? "is" : "is not"));

                Console.WriteLine("...Complete");
            }
        }

        private static void SetupStorageAccount(ManagementControllerParameters managementControllerParameters, int step)
        {
            using (var controller = new ManagementController(managementControllerParameters))
            {
                Console.WriteLine("\n{2}. Create Storage Account named {0} in Region {1}", managementControllerParameters.StorageAccountName, managementControllerParameters.Region, step);
                ConsoleContinuePrompt("Create");

                controller.CreateStorageAccount();

                Console.WriteLine("...Complete");
            }
        }

        private static async Task SetupStorageAccountAsync(ManagementControllerParameters managementControllerParameters, int step)
        {
            using (var controller = new ManagementController(managementControllerParameters))
            {
                Console.WriteLine("\n{2}. Create Storage Account (async) named {0} in Region {1}", managementControllerParameters.StorageAccountName, managementControllerParameters.Region, step);
                ConsoleContinuePrompt("Create");

                Task t = Task.Run(() => controller.CreateStorageAccountAsync());
                while (t.Status != TaskStatus.RanToCompletion &&
                       t.Status != TaskStatus.Canceled &&
                       t.Status != TaskStatus.Faulted)
                {
                    Console.WriteLine("   Working - status is {0}", t.Status.ToString());
                    Thread.Sleep(5000);
                }

                Console.WriteLine("...Complete");
            }
        }

        private static void UpdateStorageAccount(ManagementControllerParameters managementControllerParameters, int step)
        {
            using (var controller = new ManagementController(managementControllerParameters))
            {
                Console.WriteLine("\n{1}. Update Storage Account named {0}.\n   Updates Description, Label, and GeoReplication status.", managementControllerParameters.StorageAccountName, step);
                ConsoleContinuePrompt("Update");

                controller.UpdateStorageAccount("My New Storage Account", "Account Label", StorageAccountTypes.StandardGRS);

                Console.WriteLine("...Complete");
            }
        }

        private static void GetStorageAccountConnectionString(ManagementControllerParameters managementControllerParameters, int step)
        {
            using (var controller = new ManagementController(managementControllerParameters))
            {
                Console.WriteLine("\n{1}. Get connection string for Storage Account named {0}.", managementControllerParameters.StorageAccountName, step);
                ConsoleContinuePrompt("Get Connection String");

                string cn = controller.GetStorageAccountConnectionString();
                Console.WriteLine("   Connection String is:\n{0}\n", cn);

                Console.WriteLine("...Complete");
            }
        }

        private static void RegenerateKeys(ManagementControllerParameters managementControllerParameters, int step)
        {
            using (var controller = new ManagementController(managementControllerParameters))
            {
                Console.WriteLine("\n{1}. Regenerate primary key for Storage Account named {0}.", managementControllerParameters.StorageAccountName, step);
                ConsoleContinuePrompt("Regenerate Primary Key");

                controller.RegenerateKeys();

                Console.WriteLine("...Complete");
            }
        }

        private static void GetStorageAccountProperties(ManagementControllerParameters managementControllerParameters, int step)
        {
            using (var controller = new ManagementController(managementControllerParameters))
            {
                Console.WriteLine("\n{1}. Get properties for Storage Account named {0}.", managementControllerParameters.StorageAccountName, step);
                ConsoleContinuePrompt("Get Properties");

                StorageAccountGetResponse gr = controller.GetStorageAccountProperties();

                Console.WriteLine("   Status.................: {0}", gr.StorageAccount.Properties.Status);
                Console.WriteLine("   Label..................: {0}", gr.StorageAccount.Properties.Label);
                Console.WriteLine("   Description............: {0}", gr.StorageAccount.Properties.Description);
                Console.WriteLine("   Affinity Group.........: {0}", gr.StorageAccount.Properties.AffinityGroup);
                Console.WriteLine("   Location...............: {0}", gr.StorageAccount.Properties.Location);
                Console.WriteLine("   Geo-Primary Region.....: {0}", gr.StorageAccount.Properties.GeoPrimaryRegion);
                Console.WriteLine("   Geo-Secondary Region...: {0}", gr.StorageAccount.Properties.GeoSecondaryRegion);
                Console.WriteLine("   Last geo-failover time.: {0}\n", gr.StorageAccount.Properties.LastGeoFailoverTime.ToString());

                Console.WriteLine("...Complete");
            }
        }

        private static void ListStorageAccounts(ManagementControllerParameters managementControllerParameters, int step)
        {
            using (var controller = new ManagementController(managementControllerParameters))
            {
                Console.WriteLine("\n{0}. List Storage Accounts in subscription.", step);
                ConsoleContinuePrompt("List Storage Accounts");

                StorageAccountListResponse lr = controller.ListStorageAccounts();

                foreach (var r in lr)
                {
                    Console.WriteLine("   Account Name: {0}", r.Name, r.Uri);
                }
                Console.WriteLine("\n   Additional properties are available via .Properties for each account.\n");

                Console.WriteLine("...Complete");
            }
        }

        private static void TearDownStorageAccount(ManagementControllerParameters managementControllerParameters, int step)
        {
            using (var controller = new ManagementController(managementControllerParameters))
            {
                Console.WriteLine("\n{0}. Delete Storage Account", step);
                ConsoleContinuePrompt("Delete");

                controller.TearDown();
            }
        }

        private static void ConsoleContinuePrompt(string prompt)
        {
            Console.WriteLine("\t > Press Enter to {0}", prompt);
            Console.ReadKey();
            Console.WriteLine("\t\t Starting, view progress in the management portal....");
        }
    }
}
