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

using AzureQuickStarts.Common;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Management.Compute;
using Microsoft.WindowsAzure.Management.Compute.Models;
using Microsoft.WindowsAzure.Management.Storage;
using Microsoft.WindowsAzure.Management.Storage.Models;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace DeployManageVirtualMachines
{
    internal class VMManagementController : IDisposable
    {
        private StorageManagementClient _storageManagementClient;
        private ComputeManagementClient _computeManagementClient;
        private VMManagementControllerParameters _parameters;
        private string _primaryKey;

        public VMManagementController(VMManagementControllerParameters parameters)
        {
            _parameters = parameters;

            // To authenticate against the Microsoft Azure service management API we require management certificate
            // load this from a publish settings file and later use it with the Service Management Libraries
            var credentials = GetSubscriptionCloudCredentials(parameters.PublishSettingsFilePath);

            _storageManagementClient = CloudContext.Clients.CreateStorageManagementClient(credentials);
            _computeManagementClient = CloudContext.Clients.CreateComputeManagementClient(credentials);
        }

        /// <summary>
        /// Build the SubscriptionCloudCredentials using the information from the Publish Settings file
        /// </summary>
        /// <param name="publishSettingsFilePath"></param>
        /// <returns></returns>
        private SubscriptionCloudCredentials GetSubscriptionCloudCredentials(string publishSettingsFilePath)
        {
            PublishSettingsSubscriptionItem publishSettingCreds = null;

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

                    publishSettingCreds = new PublishSettingsSubscriptionItem
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

            return new CertificateCloudCredentials(publishSettingCreds.SubscriptionId, new X509Certificate2(Convert.FromBase64String(publishSettingCreds.ManagementCertificate)));             
        }

        /// <summary>
        /// Create a new Storage Account in the given region
        /// </summary>
        /// <returns></returns>
        internal async Task CreateStorageAccount()
        {
            // Create the Storage Account
            await _storageManagementClient.StorageAccounts.CreateAsync(
                new StorageAccountCreateParameters
                {
                    Location = _parameters.Region,
                    Name = _parameters.StorageAccountName,
                    AccountType = _parameters.StorageAccountType
                });
            // Retrieve the primary key that will allow us to access Storage later
            var keys = await _storageManagementClient.StorageAccounts.GetKeysAsync(_parameters.StorageAccountName);
            _primaryKey = keys.PrimaryKey;
        }

        /// <summary>
        /// Create a Cloud Service where the VM will be hosted
        /// </summary>
        /// <returns></returns>
        internal async Task CreateCloudService()
        {
            //Create the hosted service
            await _computeManagementClient.HostedServices.CreateAsync(new HostedServiceCreateParameters
            {
                Location = _parameters.Region,
                ServiceName = _parameters.CloudServiceName
            });
        }

        /// <summary>
        /// Retrieve a list of available OS images
        /// </summary>
        /// <returns></returns>
        internal async Task<IList<VirtualMachineOSImageListResponse.VirtualMachineOSImage>> GetImagesList()
        {
            var response = await _computeManagementClient.VirtualMachineOSImages.ListAsync();
            return response.Images;
        }

        /// <summary>
        /// Create the Virtual Machine
        /// </summary>
        /// <param name="imageName">The OS image name to use</param>
        /// <returns></returns>
        internal async Task CreateVirtualMachine(string imageName)
        {
            // Get the details of the selected OS image
            var image = await _computeManagementClient.VirtualMachineOSImages.GetAsync(imageName);

            // The Deployment where the Virtual Machine will be hosted
            var parameters = new VirtualMachineCreateDeploymentParameters
            {
                DeploymentSlot = DeploymentSlot.Production,
                Name = _parameters.CloudServiceName,
                Label = String.Format("CloudService{0}", _parameters.CloudServiceName),
            };

            // The Role defines the details of the VM parameters
            parameters.Roles.Add(new Role
            {
                OSVirtualHardDisk = new OSVirtualHardDisk
                {
                    HostCaching = VirtualHardDiskHostCaching.ReadWrite,
                    MediaLink = new Uri(string.Format("https://{0}.blob.core.windows.net/vhds/{1}",
                        _parameters.StorageAccountName, _parameters.VMName)),
                    SourceImageName = image.Name
                },
                // You could use DataVirtualHardDisks here to add additional persistent data disks
                RoleName = _parameters.VMName,
                RoleType = VirtualMachineRoleType.PersistentVMRole.ToString(),
                RoleSize = VirtualMachineRoleSize.Small,
                ProvisionGuestAgent = true
            });

            // Add a ConfigurationSet for the Windows specific configuration
            parameters.Roles[0].ConfigurationSets.Add(new ConfigurationSet
            {
                ConfigurationSetType = ConfigurationSetTypes.WindowsProvisioningConfiguration,
                ComputerName = _parameters.VMName, // TODO: HostName for Linux
                AdminUserName = _parameters.AdminUsername,
                AdminPassword = _parameters.AdminPassword,
                EnableAutomaticUpdates = false,
                TimeZone = "Pacific Standard Time"
            });

            // Add a ConfigurationSet describing the ports to open to the outside
            parameters.Roles[0].ConfigurationSets.Add(new ConfigurationSet
            {
                ConfigurationSetType = ConfigurationSetTypes.NetworkConfiguration,
                InputEndpoints = new List<InputEndpoint>()
                {
                    // Open the RDP port using a non-standard external port 
                    new InputEndpoint()
                    {
                        Name = "RDP",
                        Protocol = InputEndpointTransportProtocol.Tcp,
                        LocalPort = 3389,
                        Port = _parameters.RDPPort
                    }
                }
            });

            // Create the Virtual Machine
            var response = await _computeManagementClient.VirtualMachines.CreateDeploymentAsync(_parameters.CloudServiceName, parameters);
        }

        /// <summary>
        /// Shutdown the Virtual Machine.
        /// </summary>
        /// <param name="deallocate">Specify deallocate = true to completely deallocate the VM resources and stop billing</param>
        /// <returns></returns>
        internal async Task StopVirtualMachine(bool deallocate)
        {
            var shutdownParams = new VirtualMachineShutdownParameters();

            if (deallocate)
                shutdownParams.PostShutdownAction = PostShutdownAction.StoppedDeallocated; // Fully deallocate resources and stop billing
            else
                shutdownParams.PostShutdownAction = PostShutdownAction.Stopped; // Just put the machine in stopped state, keeping resources allocated

            await _computeManagementClient.VirtualMachines.ShutdownAsync(_parameters.CloudServiceName, _parameters.CloudServiceName, _parameters.VMName, shutdownParams);
        }

        /// <summary>
        /// Start the Virtual Machine
        /// </summary>
        /// <returns></returns>
        internal async Task StartVirtualMachine()
        {
            await _computeManagementClient.VirtualMachines.StartAsync(_parameters.CloudServiceName, _parameters.CloudServiceName, _parameters.VMName);
        }

        /// <summary>
        /// Delete the Virtual Machine and associated resources
        /// </summary>
        /// <returns></returns>
        internal async Task DeleteVirtualMachine()
        {
            // Get a reference to the Hosted Service and Deployment
            var hostedService = await _computeManagementClient.HostedServices.GetDetailedAsync(_parameters.CloudServiceName);
            var deployment = hostedService.Deployments.First();

            // Delete the Deployment (asking to delete the OS Disk as well)
            var deleteDeploymentOp = await _computeManagementClient.Deployments.DeleteByNameAsync(_parameters.CloudServiceName, deployment.Name, true);

            // Delete the Hosted Service
            var response = await _computeManagementClient.HostedServices.DeleteAsync(_parameters.CloudServiceName);
        }

        /// <summary>
        /// Delete the Storage Account
        /// </summary>
        /// <returns></returns>
        internal async Task DeleteStorageAccount()
        {
            var response = await _storageManagementClient.StorageAccounts.DeleteAsync(_parameters.StorageAccountName);
        }

        /// <summary>
        /// Wait for the VM to reach a given state by polling its Status.
        /// More details on Role States: http://msdn.microsoft.com/en-us/library/windowsazure/ee460804.aspx
        /// </summary>
        /// <param name="targetStatus">The string representing the state we are waiting for: </param>
        /// <param name="pollIntervalSeconds"></param>
        /// <param name="action">The Action to call to update status. Will receive current status as parameter.</param>
        /// <returns></returns>
        internal void PollVMStatus(string targetStatus, int pollIntervalSeconds, Action<string> action)
        {
            DeploymentGetResponse deployment;
            string status;

            while (true)
            {
                // Get the deployment
                deployment = _computeManagementClient.Deployments.GetByName(_parameters.CloudServiceName, _parameters.CloudServiceName);
                // Retrieve the status of the first Role Instance (the VM)
                status = deployment.RoleInstances[0].InstanceStatus;
                // Break if we reached the target status
                if (status == targetStatus) break;
                // Execute the action
                action(status);
                // Wait a while
                System.Threading.Thread.Sleep(pollIntervalSeconds * 1000);
            }
        }

        /// <summary>
        /// Wait for the VHD Blob to disappear in order to confirm the VM and its OS disk were deleted.
        /// </summary>
        /// <param name="pollIntervalSeconds"></param>
        /// <param name="action"></param>
        internal void PollVHDBlob(int pollIntervalSeconds, Action action)
        {
            //var account = new CloudStorageAccount(new Microsoft.WindowsAzure.Storage.Auth.StorageCredentials(_parameters.StorageAccountName, _primaryKey), true);
            //var client = account.CreateCloudBlobClient();
            var blobName = String.Format("https://{0}.blob.core.windows.net/vhds/{1}", _parameters.StorageAccountName, _parameters.VMName);
            var blob = new CloudPageBlob(new Uri(blobName), new Microsoft.WindowsAzure.Storage.Auth.StorageCredentials(_parameters.StorageAccountName, _primaryKey));

            while (true)
            {
                // Break if the Blob has disappeared
                if (!blob.Exists()) break;
                // Execute the action
                action();
                // Wait a while
                System.Threading.Thread.Sleep(pollIntervalSeconds * 1000);
            }
        }

        /// <summary>
        /// Clean up the Service Management clients
        /// </summary>
        public void Dispose()
        {
            if (_computeManagementClient != null)
            {
                _computeManagementClient.Dispose();
            }

            if (_storageManagementClient != null)
            {
                _storageManagementClient.Dispose();
            }
        }
    }
}
