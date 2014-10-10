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
using Microsoft.WindowsAzure.Management.Compute;
using Microsoft.WindowsAzure.Management.Compute.Models;
using Microsoft.WindowsAzure.Management.Storage;
using Microsoft.WindowsAzure.Management.Storage.Models;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using AzureQuickStarts.Common;

namespace DeployManageCloudServices
{
    internal partial class ManagementController : IDisposable
    {
        private StorageManagementClient _storageManagementClient;
        private ComputeManagementClient _computeManagementClient;
        private PublishSettingsSubscriptionItem _publishSettingCreds;
        private ManagementControllerParameters _parameters;

        public ManagementController(ManagementControllerParameters parameters)
        {
            _parameters = parameters;

            // To authenticate against the Microsoft Azure service management API we require management certificate
            // load this from a publish settings file and later use it with the Service Management Libraries
            var credential = GetSubscriptionCloudCredentials(parameters.PublishSettingsFilePath);

            _storageManagementClient = CloudContext.Clients.CreateStorageManagementClient(credential);
            _computeManagementClient = CloudContext.Clients.CreateComputeManagementClient(credential);

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

        internal async Task CreateStorageAccount()
        {
            //Create a storage account in the given region
            await _storageManagementClient.StorageAccounts.CreateAsync(
                new StorageAccountCreateParameters
                {
                    Location = _parameters.Region,
                    Name = _parameters.StorageAccountName
                });
        }

        internal async Task<string> GetStorageAccountConnectionString()
        {
            //Retrieve the storage account keys
            var keys = await _storageManagementClient.StorageAccounts.GetKeysAsync(_parameters.StorageAccountName);

            string connectionString = string.Format(
                CultureInfo.InvariantCulture,
                "DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1}",
                _parameters.StorageAccountName, keys.SecondaryKey);

            return connectionString;
        }

        internal async Task CreateCloudService()
        {
            //Create the hosted service
            await _computeManagementClient.HostedServices.CreateAsync(new HostedServiceCreateParameters
            {
                Location = _parameters.Region,
                ServiceName = _parameters.CloudServiceName
            });
        }

        internal async Task<CloudBlockBlob> UploadDeploymentPackage()
        {
            //upload cloud service package and config to storage account
            var storageConnectionString = await GetStorageAccountConnectionString();

            var account = CloudStorageAccount.Parse(storageConnectionString);

            var blobs = account.CreateCloudBlobClient();

            var container = blobs.GetContainerReference("deployments");

            await container.CreateIfNotExistsAsync();

            await container.SetPermissionsAsync(
                new BlobContainerPermissions()
                {
                    PublicAccess = BlobContainerPublicAccessType.Container
                });

            var blob = container.GetBlockBlobReference(
                Path.GetFileName(_parameters.ServicePackageFilePath));

            await blob.UploadFromFileAsync(_parameters.ServicePackageFilePath, FileMode.Open);

            return blob;
        }

        internal async Task DeployCloudService(Uri blobUri)
        {
            //deploy the cloud service into the provisioned slot using the uploaded *.cspkg and *.cscfg
            await _computeManagementClient.Deployments.CreateAsync(_parameters.CloudServiceName,
                    DeploymentSlot.Production,
                    new DeploymentCreateParameters
                    {
                        Label = _parameters.CloudServiceName,
                        Name = _parameters.CloudServiceName + "Prod",
                        PackageUri = blobUri,
                        Configuration = File.ReadAllText(_parameters.ServiceConfigurationFilePath),
                        StartDeployment = true
                    });
        }

        internal void TearDown()
        {
            //tear down everything that was created
            _computeManagementClient.Deployments.DeleteBySlot(_parameters.CloudServiceName, DeploymentSlot.Production);
            _computeManagementClient.HostedServices.Delete(_parameters.CloudServiceName);
            _storageManagementClient.StorageAccounts.Delete(_parameters.StorageAccountName);
        }

        public void Dispose()
        {
            if (_storageManagementClient != null)
                _storageManagementClient.Dispose();
            if (_computeManagementClient != null)
                _computeManagementClient.Dispose();
        }

    }
}