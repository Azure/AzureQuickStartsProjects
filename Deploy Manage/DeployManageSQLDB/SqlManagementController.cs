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


using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Management.Sql;
using Microsoft.WindowsAzure.Management.Sql.Models;
using System;
using System.Threading.Tasks;

namespace DeployManageSQLDB
{
    internal partial class SqlManagementController : IDisposable
    {
        private SqlManagementClient _sqlManagementClient;

        public SqlManagementController(string publishSettingsFilePath)
        {
            // To authenticate against the Microsoft Azure service management API we require management certificate
            // load this from a publish settings file and use it to new up an instance of SqlManagementClient CloudClient
            var credentials = CredentialsHelper.GetSubscriptionCloudCredentials(publishSettingsFilePath);

            _sqlManagementClient = CloudContext.Clients.CreateSqlManagementClient(credentials);
        }


        internal async Task<ServerCreateResponse> CreateServerAsync(string region, string adminUsername, string adminPassword)
        {
            return await _sqlManagementClient.Servers.CreateAsync(
                        new ServerCreateParameters
                        {
                            AdministratorPassword = adminPassword,
                            AdministratorUserName = adminUsername,
                            Location = region,
                        });
        }

        internal async Task<FirewallRuleCreateResponse> ConfigureFirewallAsync(string serverName, string name, string startIP, string endIP)
        {
            return await _sqlManagementClient.FirewallRules.CreateAsync(serverName,
                        new FirewallRuleCreateParameters
                        {
                            Name = name,
                            StartIPAddress = startIP,
                            EndIPAddress = endIP
                        });
        }

        internal async Task<DatabaseCreateResponse> CreateDatabaseAsync(string serverName, string databaseName, string collation, string edition, int? maxSizeInGB)
        {
            return await _sqlManagementClient.Databases.CreateAsync(serverName,
                            new DatabaseCreateParameters
                            {
                                CollationName = collation,
                                Edition = edition,
                                MaximumDatabaseSizeInGB = maxSizeInGB,
                                Name = databaseName
                            });
        }

        internal async Task<OperationResponse> DropDatabaseAsync(string serverName, string databaseName)
        {
            return await _sqlManagementClient.Databases.DeleteAsync(serverName, databaseName);
        }

        internal async Task<OperationResponse> DeleteServerAsync(string serverName)
        {
            return await _sqlManagementClient.Servers.DeleteAsync(serverName);
        }

        internal async Task<ServerListResponse> ListServersAsync()
        {
            return await _sqlManagementClient.Servers.ListAsync();
        }

        internal async Task<DatabaseListResponse> ListDatabasesAsync(string serverName)
        {
            return await _sqlManagementClient.Databases.ListAsync(serverName);
        }

        internal async Task<FirewallRuleListResponse> ListFirewallRulesAsync(string serverName)
        {
            return await _sqlManagementClient.FirewallRules.ListAsync(serverName);
        }

        internal async Task<OperationResponse> UpdateAdministratorPasswordAsync(string serverName, string newPassword)
        {
            return await _sqlManagementClient.Servers.ChangeAdministratorPasswordAsync(serverName,
                                new ServerChangeAdministratorPasswordParameters
                                {
                                    NewPassword = newPassword
                                });
        }

        internal async Task<DatabaseUpdateResponse> UpdateDatabaseAsync(string serverName, string databaseName, string newName, string collation, string edition, int? maxSizeInGB)
        {
            var p = new DatabaseUpdateParameters();
            if (!string.IsNullOrEmpty(newName)) { p.Name = newName; }
            if (!string.IsNullOrEmpty(collation)) { p.CollationName = collation; }
            if (!string.IsNullOrEmpty(edition)) { p.Edition = newName; }
            if (maxSizeInGB != null) { p.MaximumDatabaseSizeInGB = maxSizeInGB; }

            return await _sqlManagementClient.Databases.UpdateAsync(serverName, databaseName, p);
        }

        public void Dispose()
        {
            if (_sqlManagementClient != null)
                _sqlManagementClient.Dispose();
        }
    }
}