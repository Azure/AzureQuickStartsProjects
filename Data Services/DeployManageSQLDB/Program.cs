﻿//----------------------------------------------------------------------------------
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


using Microsoft.WindowsAzure.Management.Sql.Models;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace DeployManageSQLDB
{
    class Program
    {
        //*************************************************************************************************************
        // The Microsoft Azure Management Libraries are intended for developers who want to automate 
        // the management, provisioning, deprovisioning and test of cloud infrastructure with ease.            
        // These services support Microsoft Azure Virtual Machines, Hosted Services, Storage, Virtual Networks, 
        // Web Sites and core data center infrastructure management. For more information on the Management
        // Libraries for .NET, see https://msdn.microsoft.com/en-us/library/azure/dn722415.aspx. 
        //
        // If you dont have a Microsoft Azure subscription you can get a FREE trial account here:
        // http://go.microsoft.com/fwlink/?LinkId=330212
        //
        // This Quickstart demonstrates using WAML how to provision a new Microsoft Azure SQL DB Server,
        // Configure the Firewall Rules, Create a new Database, Then drop the Database and Delete the Server.
        //
        // TODO: Perform the following steps before running the sample:
        //
        // 1. Download your Microsoft Azure PublishSettings file; to do so click here:
        //    http://go.microsoft.com/fwlink/?LinkID=276844 
        //
        // 2. Fill in the full path of the PublishSettings file below in PublishSettingsFilePath.
        //
        // 3. Choose an [ADMIN USER] and [ADMIN PASSWORD] that you wish to use for the server.
        //
        // 4. Update the FirewallRuleStartIP and FirewallRuleEndIP values.
        //    If you wish to add a firewall rule to allow your local development computer access
        //    to the Server / Database then configure FirewallRuleStartIP and FirewallRuleEndIP to the public IP of 
        //    your local development machine.
        //
        // 6. Adjust values of any other parameter as you wish
        //*************************************************************************************************************

        private static string _serverName;

        static void Main(string[] args)
        {
            var parameters = new SqlManagementControllerParameters
            {
                PublishSettingsFilePath = @"C:\Your.publishsettings", // UPDATE
                ServerRegion = "West US",
                ServerAdminUsername = "[ADMIN USER]", // UPDATE
                ServerAdminPassword = "[ADMIN PASSWORD]", // UPDATE
                FirewallRuleAllowAzureServices = true,
                FirewallRuleName = "Sample Firewall Rule",
                FirewallRuleStartIP = "0.0.0.0",
                FirewallRuleEndIP = "255.255.255.254", // Example Firewall Rule only. Do Not Use in Production.
                DatabaseName = "Demo",
                DatabaseEdition = "Basic",
                DatabaseMaxSizeInGB = 1,
                DatabaseCollation = "SQL_Latin1_General_CP1_CI_AS"
            };

            if (!VerifyConfiguration(parameters))
            {
                Console.ReadLine();
                return;
            }

            Task.WaitAll(SetupAndTearDownLogicalSQLServer(parameters));

            Console.WriteLine("Done");
            Console.ReadLine();
        }

        private static bool VerifyConfiguration(SqlManagementControllerParameters serviceParameters)
        {
            bool configOK = true;
            if (!File.Exists(serviceParameters.PublishSettingsFilePath))
            {
                configOK = false;
                Console.WriteLine("Please download your .publishsettings file and specify the location in the Main method.");
            }
            if (serviceParameters.ServerAdminUsername.StartsWith("[") || serviceParameters.ServerAdminPassword.StartsWith("["))
            {
                configOK = false;
                Console.WriteLine("Please specify a server admin username and password in the Main method.");
            }
            return configOK;
        }

        private static async Task SetupAndTearDownLogicalSQLServer(SqlManagementControllerParameters parameters)
        {
            int step = 1;

            // Create a new logical server     
            await SetupServerAsync(parameters, step++);

            // List servers in subscription
            await ListServersAsync(parameters, step++);

            // Add new Firewall rules on the logical server created
            await ConfigureFirewallAsync(parameters, step++);

            // List Firewall Rules on server
            await ListFirewallRulesAsync(parameters, step++);

            // Create a new database on the server
            await CreateDatabaseAsync(parameters, step++);

            // List Firewall Rules on server
            await ListDatabasesAsync(parameters, step++);

            // Cleanup
            await TearDownDatabaseAsync(parameters, step++);
            await TearDownServerAsync(parameters, step++);
        }

        private static async Task ListDatabasesAsync(SqlManagementControllerParameters parameters, int step)
        {
            using (var controller = new SqlManagementController(parameters.PublishSettingsFilePath))
            {
                Console.WriteLine("\n{1}. Listing Databases on Server {0}", _serverName, step);
                ConsoleContinuePrompt("List");

                var t = Task<DatabaseListResponse>.Run(() => { return controller.ListDatabasesAsync(_serverName); });
                WaitForStatus(t);

                var databases = from s
                              in t.Result.Databases
                                select s.Name;

                Console.Write("\n");

                foreach (string database in databases)
                {
                    Console.WriteLine("   Database - {0}", database);
                }

                Console.WriteLine("...Complete");
            }
        }

        private static async Task ListFirewallRulesAsync(SqlManagementControllerParameters parameters, int step)
        {
            using (var controller = new SqlManagementController(parameters.PublishSettingsFilePath))
            {
                Console.WriteLine("\n{1}. Listing Firewall Rules for Server {0}", _serverName, step);
                ConsoleContinuePrompt("List");

                var t = Task<FirewallRuleListResponse>.Run(() => { return controller.ListFirewallRulesAsync(_serverName); });
                WaitForStatus(t);

                var rules = from r
                            in t.Result.FirewallRules
                            select new
                            {
                                Name = r.Name,
                                StartIP = r.StartIPAddress,
                                EndIP = r.EndIPAddress
                            };

                Console.Write("\n");

                foreach (var rule in rules)
                {
                    Console.WriteLine("   Rule - {0}\tStart IP - {1}\tEnd IP - {2}", rule.Name, rule.StartIP, rule.EndIP);
                }

                Console.WriteLine("...Complete");
            }
        }

        private static async Task ListServersAsync(SqlManagementControllerParameters parameters, int step)
        {
            using (var controller = new SqlManagementController(parameters.PublishSettingsFilePath))
            {
                Console.WriteLine("\n{1}. Listing Servers", _serverName, step);
                ConsoleContinuePrompt("List");

                var t = Task<ServerListResponse>.Run(() => { return controller.ListServersAsync(); });
                WaitForStatus(t);

                var servers = from s
                              in t.Result.Servers
                              select s.Name;

                Console.Write("\n");

                foreach (string server in servers)
                {
                    Console.WriteLine("   Server - {0}", server);
                }

                Console.WriteLine("...Complete");
            }
        }

        private static async Task TearDownServerAsync(SqlManagementControllerParameters parameters, int step)
        {
            using (var controller = new SqlManagementController(parameters.PublishSettingsFilePath))
            {
                Console.WriteLine("\n{1}. Dropping Server {0}", _serverName, step);
                ConsoleContinuePrompt("Drop");

                Task t = Task.Run(() => controller.DeleteServerAsync(_serverName));
                WaitForStatus(t);

                Console.WriteLine("\n...Complete");
            }
        }
        private static async Task TearDownDatabaseAsync(SqlManagementControllerParameters parameters, int step)
        {
            using (var controller = new SqlManagementController(parameters.PublishSettingsFilePath))
            {
                Console.WriteLine("\n{2}. Dropping Database {1} on Server {0}", _serverName, parameters.DatabaseName, step);
                ConsoleContinuePrompt("Drop");

                Task t = Task.Run(() => controller.DropDatabaseAsync(_serverName, parameters.DatabaseName));
                WaitForStatus(t);

                Console.WriteLine("\n...Complete");
            }
        }
        private static async Task CreateDatabaseAsync(SqlManagementControllerParameters parameters, int step)
        {
            using (var controller = new SqlManagementController(parameters.PublishSettingsFilePath))
            {
                Console.WriteLine("\n{2}. Creating Database {1} on Server {0}", _serverName, parameters.DatabaseName, step);
                ConsoleContinuePrompt("Create");

                Task t = Task.Run(() => controller.CreateDatabaseAsync(_serverName, parameters.DatabaseName, parameters.DatabaseCollation, parameters.DatabaseEdition, parameters.DatabaseMaxSizeInGB));
                WaitForStatus(t);

                Console.WriteLine("\n...Complete");
            }
        }
        private static async Task ConfigureFirewallAsync(SqlManagementControllerParameters parameters, int step)
        {
            using (var controller = new SqlManagementController(parameters.PublishSettingsFilePath))
            {
                Console.WriteLine("\n{1}. Adding Firewall rules for server {0}", _serverName, step);
                ConsoleContinuePrompt("Create");

                Task t = Task.Run(() => controller.ConfigureFirewallAsync(_serverName, parameters.FirewallRuleName, parameters.FirewallRuleStartIP, parameters.FirewallRuleEndIP));
                WaitForStatus(t);

                Console.WriteLine("\n...Complete");
            }
        }
        private static async Task SetupServerAsync(SqlManagementControllerParameters parameters, int step)
        {
            using (var controller = new SqlManagementController(parameters.PublishSettingsFilePath))
            {
                Console.WriteLine("\n{1}. Create logical Server in Region {0}", parameters.ServerRegion, step);
                ConsoleContinuePrompt("Create");

                var t = Task.Run<ServerCreateResponse>(() =>
                {
                    return controller.CreateServerAsync(parameters.ServerRegion, parameters.ServerAdminUsername,
                        parameters.ServerAdminPassword);
                });

                WaitForStatus(t);

                // now that the task is done, save the returned Server Name in to a global variable
                // so that we can use it again later when creating the Firewall Rules and Database etc. 
                _serverName = t.Result.ServerName;

                if (parameters.FirewallRuleAllowAzureServices)
                {
                    Console.WriteLine("\n{1}. Adding Firewall rules for Azure Services on server {0}", _serverName, step);

                    Task p = Task.Run(() => controller.ConfigureFirewallAsync(_serverName, "Azure Services", parameters.FirewallRuleStartIP, parameters.FirewallRuleEndIP));
                    WaitForStatus(p);
                }

                Console.WriteLine("\n...Complete");
            }
        }

        private static void WaitForStatus(Task t)
        {
            while (t.Status != TaskStatus.RanToCompletion &&
                   t.Status != TaskStatus.Canceled &&
                   t.Status != TaskStatus.Faulted)
            {
                Console.WriteLine(string.Format("\t\t{0}", t.Status));
                Thread.Sleep(5000);
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
