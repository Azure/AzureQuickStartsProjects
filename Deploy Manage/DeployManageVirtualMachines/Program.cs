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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeployManageVirtualMachines
{
    class Program
    {
        static void Main(string[] args)
        {
            //***********************************************************************************************
            // The Microsoft Azure Management Libraries are inteded for developers who want to automate 
            // the management, provisioning, deprovisioning and test of cloud infrastructure with ease.            
            // These services support Microsoft Azure Virtual Machines, Hosted Services, Storage, Virtual Networks, 
            // Web Sites and core data center infrastructure management. If you dont have a Microsoft Azure 
            // subscription you can get a FREE trial account here:
            // http://go.microsoft.com/fwlink/?LinkId=330212
            //
            // TODO: Perform the following steps before running the sample 
            //  1. Download your *.publishsettings file from the Microsoft Azure management portal and save to
            //     to your local drive http://go.microsoft.com/fwlink/?LinkID=276844
            //  2. Set the PublishSettingsFilePath property below.  
            //  3. Run
            //***********************************************************************************************

            var serviceParameters = new VMManagementControllerParameters
            {
                PublishSettingsFilePath = @"C:\your.publishsettings",
                CloudServiceName = string.Format("quickstart{0}", DateTime.Now.Ticks),
                VMName = "vmdemo",
                RDPPort = 53389,
                Region = "West US",
                StorageAccountName = Path.GetFileNameWithoutExtension(Path.GetRandomFileName())
            };

            // Request login/password

            Console.Write("Please enter the username to create on the VM: ");
            serviceParameters.AdminUsername = Console.ReadLine();

            Console.Write("Please enter the account password (at least 8 characters): ");
            serviceParameters.AdminPassword = Console.ReadLine();

            // Run the sequence

            Task.WaitAll(SetupAndTearDownVirtualMachine(serviceParameters));

            Console.WriteLine("Done");
            Console.ReadLine();
        }

        private static async Task SetupAndTearDownVirtualMachine(VMManagementControllerParameters managementControllerParameters)
        {
            using (VMManagementController controller = new VMManagementController(managementControllerParameters))
            {
                // Retrieve a list of available images and display a choice of Windows images

                Console.WriteLine("Please choose an OS image to use:");

                var images = await controller.GetImagesList();
                var windowsImages = images.Where(i => i.OperatingSystemType == "Windows" && i.PublisherName == "Microsoft Windows Server Group");
                for (int i = 0; i < windowsImages.Count(); i++)
                {
                    Console.WriteLine(i + 1 + ". " + windowsImages.ElementAt(i).Label);
                }
                var imageId = Console.ReadLine();

                // Keep a reference to the chosen image

                var imageToGet = windowsImages.ElementAt(int.Parse(imageId) - 1);

                // Start the steps to create the VM

                Console.WriteLine("1. Creating Storage Account named {0} in Region {1}...", managementControllerParameters.StorageAccountName, managementControllerParameters.Region);

                // Create the Storage Account
                await controller.CreateStorageAccount();

                Console.WriteLine("...Complete");

                Console.WriteLine("2. Creating a Cloud Service named {0} in Region {1}", managementControllerParameters.CloudServiceName, managementControllerParameters.Region);
                ConsoleContinuePrompt("Create the Cloud Service");

                // Create the Cloud Service
                await controller.CreateCloudService();

                Console.WriteLine("...Complete");

                Console.WriteLine("3. Create the Virtual Machine");
                ConsoleContinuePrompt("Create the VM");

                // Create the Virtual Machine
                await controller.CreateVirtualMachine(imageToGet.Name);

                // Wait for the Virtual Machine to be ready
                controller.PollVMStatus("ReadyRole", 5, (s) =>
                {
                    Console.WriteLine("Waiting... Current status: " + s);
                });

                Console.WriteLine("...Complete. You can now log on the Virtual Machine.");

                Console.WriteLine("4. Shut down the Virtual Machine and deallocate resources");
                ConsoleContinuePrompt("Shutdown the VM");

                // Shutdown the Virtual Machine
                await controller.StopVirtualMachine(true);

                // Wait for the Virtual Machine to be stopped
                controller.PollVMStatus("StoppedDeallocated", 5, (s) =>
                {
                    Console.WriteLine("Waiting... Current status: " + s);
                });

                Console.WriteLine("...Complete.");

                Console.WriteLine("5. Start the Virtual Machine again");
                ConsoleContinuePrompt("Start the VM");

                // Shutdown the Virtual Machine
                await controller.StartVirtualMachine();

                // Wait for the Virtual Machine to be ready
                controller.PollVMStatus("ReadyRole", 5, (s) =>
                {
                    Console.WriteLine("Waiting... Current status: " + s);
                });

                Console.WriteLine("...Complete. You can now log back on the Virtual Machine.");

                Console.WriteLine("6. Delete Virtual Machine");
                ConsoleContinuePrompt("Delete the VM");

                // Delete the Virtual Machine
                await controller.DeleteVirtualMachine();

                // Wait for the disk to disappear
                controller.PollVHDBlob(5, () =>
                {
                    Console.WriteLine("Waiting...");
                });

                Console.WriteLine("...Complete");

                Console.WriteLine("7. Delete Storage Account {0}", managementControllerParameters.StorageAccountName);
                ConsoleContinuePrompt("Delete the Storage Account");

                // Delete the Storage Account
                await controller.DeleteStorageAccount();

                Console.WriteLine("...Complete");
            }
        }

        private static void ConsoleContinuePrompt(string prompt)
        {
            Console.WriteLine("\tPress Enter to: {0}", prompt);
            Console.ReadKey();
            Console.WriteLine("\tStarting, view progress in the management portal...");
        }

    }
}
