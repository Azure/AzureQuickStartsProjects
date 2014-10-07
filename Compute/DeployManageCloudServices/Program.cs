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
using System.Diagnostics;
using System.Threading.Tasks;

namespace DeployManageCloudServices
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
            //      to your local dive http://go.microsoft.com/fwlink/?LinkID=276844
            //  2. Set the PublishSettingsFilePath, ServiceConfigurationFilePath and ServicePackageFilePath
            //      properties below.  Do not enable Remote Desktop, as this process won't upload your certificate. 
            //  3. Run
            //***********************************************************************************************

            var serviceParameters = new ManagementControllerParameters
                {
                    PublishSettingsFilePath = @"C:\Your.publishsettings",
                    ServicePackageFilePath = @"C:\YourPackageFile.cspkg",
                    ServiceConfigurationFilePath = @"YourServiceConfiguration.Cloud.cscfg",
                    CloudServiceName = string.Format("MgmtLibDemo{0}", DateTime.Now.Ticks),
                    Region = "West US",
                    StorageAccountName = Path.GetFileNameWithoutExtension(Path.GetRandomFileName())
                };

            Task.WaitAll(SetupAndTearDownCloudService(serviceParameters));

            Console.WriteLine("Done");
            Console.ReadLine();
        }

        private static async Task SetupAndTearDownCloudService(ManagementControllerParameters managementControllerParameters)
        {
            ManagementController controller = new ManagementController(managementControllerParameters);

            Console.WriteLine("1. Create Storage Account named {0} in Region {1}", managementControllerParameters.StorageAccountName, managementControllerParameters.Region);
            ConsoleContinuePrompt("Create");

            await controller.CreateStorageAccount();

            Console.WriteLine("...Complete");
            Console.WriteLine("2. Upload Service Config {0} and Service Package {1} to Storage Account {2}", managementControllerParameters.ServiceConfigurationFilePath, managementControllerParameters.ServicePackageFilePath, managementControllerParameters.StorageAccountName);
            ConsoleContinuePrompt("Upload");

            var blob = await controller.UploadDeploymentPackage();

            Console.WriteLine("...Complete");
            Console.WriteLine("3. Creating a Cloud Service hosted service slot named {0} in Region {1}", managementControllerParameters.CloudServiceName, managementControllerParameters.Region);
            ConsoleContinuePrompt("Create");

            await controller.CreateCloudService();

            Console.WriteLine("...Complete");
            Console.WriteLine("4. Deploy the Cloud Service to production slot");
            ConsoleContinuePrompt("Deploy");

            await controller.DeployCloudService(blob.Uri);

            Console.WriteLine("...Complete");
            Console.WriteLine("5. Delete Cloud Service Deployment, Hosted Service and Storage Account");
            ConsoleContinuePrompt("Delete");

            controller.TearDown();
        }

        private static void ConsoleContinuePrompt(string prompt)
        {
            Console.WriteLine("\t > Press Enter to {0}", prompt);
            Console.ReadKey();
            Console.WriteLine("\t\t Starting, view progress in the managent portal....");
        }
    }
}
