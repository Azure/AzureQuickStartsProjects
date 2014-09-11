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
using Microsoft.WindowsAzure.Management.WebSites;
using Microsoft.WindowsAzure.Management.WebSites.Models;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace DeployManageWebSites
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
            //  2. Set the PublishSettingsFilePath
            //  3. Install git (e.g. http://msysgit.github.io/) and add the directory to the PATH variable 
            //  4. Run
            //***********************************************************************************************

            var webSiteParameters = new ManagementControllerParameters
            {
                PublishSettingsFilePath = @"C:\Your.publishsettings",
                WebSiteName = string.Format("MgmtLibWebSiteDemo{0}", DateTime.Now.Ticks),
                GeoRegion = GeoRegionNames.NorthEurope,
                UpgradePlan = WebSitePlans.Shared,
                // WorkerSize and NumberOfWorkers are only used in Standard mode 
                // Depending on your subscription type certain capacity restrictions may apply
                WorkerSize = WorkerSizeOptions.Small,
                NumberOfWorkers = 2
            };

            try
            {
                Task.WaitAll(SetupAndTearDownWebsite(webSiteParameters));
            }
            catch (WebSiteCloudException cloudException)
            {
                Console.WriteLine(cloudException.ErrorMessage);
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
            }
            Console.WriteLine("Done");
            Console.ReadLine();
        }


        private static async Task SetupAndTearDownWebsite(ManagementControllerParameters managementControllerParameters)
        {
            using (ManagementController controller = new ManagementController(managementControllerParameters))
            {

                Console.WriteLine("1. Create WebSite named {0} in GeoRegion {1}", managementControllerParameters.WebSiteName, managementControllerParameters.GeoRegion);
                ConsoleContinuePrompt("CREATE WebSite");

                await controller.CreateWebSite();

                Console.WriteLine("...Complete");
                Console.WriteLine("2. List WebSites");
                ConsoleContinuePrompt("LIST WebSites", false);

                LogWebsites(controller.GetWebSites());

                Console.WriteLine("...Complete"); 
                Console.WriteLine("3. Configure WebSite");
                ConsoleContinuePrompt("CONFIGURE WebSite");

                await controller.ConfigureWebSite();

                Console.WriteLine("...Complete");
                Console.WriteLine("4. Publish WebSite");
                ConsoleContinuePrompt("PUBLISH WebSite", false);

                await controller.PublishWebSite();
                
                var webSiteUrl= @"http://" + managementControllerParameters.WebSiteName + ".azurewebsites.net";
                Console.WriteLine("...git publishing to {0} in progress. Site will be ready when git commands executed in opened command window", webSiteUrl);
                Console.WriteLine("5. Open WebSite in browser");
                ConsoleContinuePrompt("OPEN in browser", false);
                
                Process.Start(webSiteUrl);

                Console.WriteLine("...Complete");
                Console.WriteLine("6. Upgrade WebSite");
                ConsoleContinuePrompt("UPGRADE WebSite");

                await controller.UpgradeWebSite();

                Console.WriteLine("...Complete");
                Console.WriteLine("7. Delete WebSite");
                ConsoleContinuePrompt("DELETE WebSite");

                await controller.TearDownWebSite();
            }
        }

        private static void ConsoleContinuePrompt(string prompt, bool viewProgressInPortal = true)
        {
            Console.WriteLine("\t > Press Enter to {0}", prompt);
            Console.ReadKey();
            if (viewProgressInPortal)
            {
                Console.WriteLine("\t\t Starting, view progress in the managent portal....");
            }
        }

        private static void LogWebsites(Dictionary<WebSpacesListResponse.WebSpace, IList<WebSite>> webSitesPerRegion)
        {
            Console.WriteLine("\nWebSites per region:");
            foreach (var webspace in webSitesPerRegion)
            {
                Console.WriteLine("\t {0}", webspace.Key.Name);
                foreach (var website in webspace.Value)
                {
                    Console.WriteLine("\t\t {0}", website.Name);
                }
            }
        }
    }
}

