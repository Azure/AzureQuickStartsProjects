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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Management.Storage;
using Microsoft.WindowsAzure.Management.Storage.Models;
using AzureQuickStarts.Common;

namespace DeployManageAzureStorage
{
    internal partial class ManagementController : IDisposable
    {
        private StorageManagementClient _storageManagementClient;
        private PublishSettingsSubscriptionItem _publishSettingCreds;
        private ManagementControllerParameters _parameters;

        public ManagementController(ManagementControllerParameters parameters)
        {
            _parameters = parameters;

            // To authenticate against the Microsoft Azure service management API we require management certificate
            // load this from a publish settings file and later use it with the Service Management Libraries
            var credential = GetSubscriptionCloudCredentials(parameters.PublishSettingsFilePath);

            _storageManagementClient = CloudContext.Clients.CreateStorageManagementClient(credential);
        }

        private SubscriptionCloudCredentials GetSubscriptionCloudCredentials(string publishSettingsFilePath)
        {
            using (var fs = File.OpenRead(publishSettingsFilePath))
            {
                var document = XDocument.Load(fs);
                var subscriptions =
                    from e in document.Descendants("Subscription")
                    select e;

                if (subscriptions.Count() >= 1)
                {
                    // use first subscription in the publish settings file
                    var subscription = subscriptions.First();

                    _publishSettingCreds = new PublishSettingsSubscriptionItem
                    {
                        SubscriptionName = subscription.Attribute("Name").Value,
                        SubscriptionId = subscription.Attribute("Id").Value,
                        ManagementCertificate = subscription.Attribute("ManagementCertificate").Value
                    };
                }
                else
                {
                    Console.WriteLine("Invalid publishsettings file: Subscription not found.");
                }
            }

            return CertificateAuthenticationHelper.GetCredentials(_publishSettingCreds.SubscriptionId, _publishSettingCreds.ManagementCertificate);
        }

        internal bool CheckNameAvailbility()
        {
            CheckNameAvailabilityResponse r = _storageManagementClient.StorageAccounts.CheckNameAvailability(
                _parameters.StorageAccountName);

            return r.IsAvailable;
        }

        internal void CreateStorageAccount()
        {
            //Create a storage account in the given region
            _storageManagementClient.StorageAccounts.Create(
                new StorageAccountCreateParameters
                {
                    Location = _parameters.Region,
                    Name = _parameters.StorageAccountName,
                    AccountType = _parameters.StorageAccountType
                });
        }

        internal async Task CreateStorageAccountAsync()
        {
            //Create a storage account in the given region
            await _storageManagementClient.StorageAccounts.CreateAsync(
                new StorageAccountCreateParameters
                {
                    Location = _parameters.Region,
                    Name = _parameters.StorageAccountName,
                    AccountType = _parameters.StorageAccountType
                });
        }

        internal string GetStorageAccountConnectionString()
        {
            //Retrieve the storage account keys
            var keys = _storageManagementClient.StorageAccounts.GetKeys(_parameters.StorageAccountName);

            string connectionString = string.Format(
                CultureInfo.InvariantCulture,
                "DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1}",
                _parameters.StorageAccountName, keys.PrimaryKey);

            return connectionString;
        }

        internal void RegenerateKeys()
        {
            StorageAccountRegenerateKeysParameters kp = new StorageAccountRegenerateKeysParameters();
            kp.KeyType = StorageKeyType.Primary;
            kp.Name = _parameters.StorageAccountName;

            _storageManagementClient.StorageAccounts.RegenerateKeys(kp);
        }

        internal void UpdateStorageAccount(string Description, string Label, string AccountType)
        {
            var parms = new StorageAccountUpdateParameters();
            parms.Description = Description;
            parms.Label = Label;
            parms.AccountType = AccountType;

            _storageManagementClient.StorageAccounts.Update(
                _parameters.StorageAccountName,
                parms);
        }

        internal StorageAccountGetResponse GetStorageAccountProperties()
        {
            StorageAccountGetResponse gr = new StorageAccountGetResponse();

            gr = _storageManagementClient.StorageAccounts.Get(
                _parameters.StorageAccountName);

            return gr;
        }

        internal StorageAccountListResponse ListStorageAccounts()
        {
            var list = _storageManagementClient.StorageAccounts.List();
            return list;
        }

        internal void TearDown()
        {
            //tear down everything that was created
            _storageManagementClient.StorageAccounts.Delete(
                _parameters.StorageAccountName);
        }

        public void Dispose()
        {
            if (_storageManagementClient != null)
                _storageManagementClient.Dispose();
        }


    }
}
