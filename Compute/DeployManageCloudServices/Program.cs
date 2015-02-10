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
using Microsoft.WindowsAzure.Management.Storage.Models;

namespace DeployManageCloudServices
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

        // This sample demonstates the following scenarios:
        //  1. Creating an Azure Storage Account
        //  2. Uploading an Azure Cloud Service package to Storage
        //  3. Creating an Azure Cloud Service
        //  4. Deploying the package to the Azure Cloud Service production slot
        //  5. Deleting a Cloud Service Deployment, Cloud Service and Storage Account.
        //

        // TODO: Perform the following steps before running the sample 
        //  1. Download your *.publishsettings file from the Microsoft Azure management portal and save to
        //      to your local dive http://go.microsoft.com/fwlink/?LinkID=276844
        //  2. Open a new instance of Visual Studio, create an Azure Cloud Service project with at least
        //      one role (don't worry about writing any custom code), right-click the project and choose
        //      Package to create the .cspkg and .cscfg files which will be deployed to Azure by this sample.
        //  3. Set the PublishSettingsFilePath, ServiceConfigurationFilePath and ServicePackageFilePath
        //      properties below.  
        //  4. Run
        //***********************************************************************************************

        static void Main(string[] args)
        {

            var serviceParameters = new ManagementControllerParameters
                {
                    PublishSettingsFilePath = @"C:\Your.publishsettings",
                    ServicePackageFilePath = @"C:\YourPackageFile.cspkg",
                    ServiceConfigurationFilePath = @"C:\YourServiceConfiguration.Cloud.cscfg",
                    CloudServiceName = string.Format("MgmtLibDemo{0}", DateTime.Now.Ticks),
                    Region = "West US",
                    StorageAccountName = Path.GetFileNameWithoutExtension(Path.GetRandomFileName()),
                    StorageAccountType = StorageAccountTypes.StandardGRS
                };

            if (!VerifyConfiguration(serviceParameters))
            {
                Console.ReadLine();
                return;
            }

            Task.WaitAll(SetupAndTearDownCloudService(serviceParameters));

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
            if (!File.Exists(serviceParameters.ServicePackageFilePath) || !File.Exists(serviceParameters.ServiceConfigurationFilePath))
            {
                configOK = false;
                Console.WriteLine("Please create an Azure Cloud Service project in another instance of Visual Studio, Package it, and specify the locations of the .cspkg and .cscfg files in the Main method.");
            }
            return configOK;
            
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
            Console.WriteLine("\t\t Starting, view progress in the management portal....");
        }
    }
}
