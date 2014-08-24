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
using System.Linq;
using System.Xml.Linq;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Management;
using Microsoft.WindowsAzure.Management.Network;
using Microsoft.WindowsAzure.Management.Network.Models;
using Microsoft.WindowsAzure.Management.Models;

namespace DeployManageVirtualNetwork
{
    internal partial class ManagementController : IDisposable
    {
        private NetworkManagementClient _virtualNetworkManagementClient = null;
        private ManagementClient _managementClient = null;

        private NetworkGetConfigurationResponse _networkConfig = null;
        private PublishSettingsSubscriptionItem _publishSettingCreds;
        private ManagementControllerParameters _parameters;

        public ManagementController(ManagementControllerParameters parameters)
        {
            _parameters = parameters;

            // To authenticate against the Microsoft Azure service management API we require management certificate
            // load this from a publish settings file and later use it with the Service Management Libraries
            var credentials = GetSubscriptionCloudCredentials(parameters.PublishSettingsFilePath);

            // Create a management client and a virtual network management client
            _managementClient = CloudContext.Clients.CreateManagementClient(credentials);
            _virtualNetworkManagementClient = CloudContext.Clients.CreateVirtualNetworkManagementClient(credentials);
        }

        internal void ListVirtualNetworks()
        {
            if (_virtualNetworkManagementClient != null)
            {
                Console.WriteLine("\n Virtual networks for Subscription '{0}':", _publishSettingCreds.SubscriptionId);

                foreach (NetworkListResponse.VirtualNetworkSite network in _virtualNetworkManagementClient.Networks.List())
                {
                    Console.WriteLine("\t Name...................: {0}", network.Name);
                    Console.WriteLine("\t Status.................: {0}", network.State);
                    Console.WriteLine("\t Affinity Group.........: {0}", network.AffinityGroup);
                    Console.WriteLine("\t Number of Subnets......: {0}", network.Subnets.Count);
                    Console.WriteLine("\t Number of DNS Servers..: {0}\n", network.DnsServers.Count);
                }

                Console.WriteLine("{0} virtual networks", _virtualNetworkManagementClient.Networks.List().Count());
            }
        }

        internal void GetVirtualNetworkConfiguration()
        {
            if (_virtualNetworkManagementClient != null)
            {
                _networkConfig = _virtualNetworkManagementClient.Networks.GetConfiguration();

                Console.Write(_networkConfig.Configuration);
            }
        }

        internal async Task GetVirtualNetworkConfigurationAsync()
        {
            if (_virtualNetworkManagementClient != null)
            {
                _networkConfig = await _virtualNetworkManagementClient.Networks.GetConfigurationAsync();

                Console.Write(_networkConfig.Configuration);
            }
        }

        internal async Task AddVirtualNetworkSiteAsync()
        {
            if (_virtualNetworkManagementClient != null && _managementClient != null)
            {
                try
                {
                    // Get the existing netwirking configuration
                    if (_networkConfig == null)
                    {
                        _networkConfig = await _virtualNetworkManagementClient.Networks.GetConfigurationAsync();
                    }

                    // create affinity group
                    await CreateAffinityGroupAsync();

                    // use the new affinity group for the virtual network 
                    string newVirtualNetworkElement = string.Format(_parameters.VirtualNetworkSite, _parameters.AffinityGroupName);

                    // add the new virtual network element to the existing network configuration under VirtualNetworkSites
                    string newNetworkConfig = AddXmlElement(_networkConfig.Configuration, "VirtualNetworkSites", newVirtualNetworkElement);

                    await _virtualNetworkManagementClient.Networks.SetConfigurationAsync(
                        new NetworkSetConfigurationParameters()
                        {
                            Configuration = newNetworkConfig
                        });
                }
                catch (Exception e)
                {
                    Console.WriteLine("\n Error: \n {0}", e.ToString());
                }
            }
        }


        internal async Task SetVirtualNetworkConfigurationAsync()
        {
            if (_virtualNetworkManagementClient != null && _managementClient != null)
            {
                try
                {
                    // create affinity group 
                    await CreateAffinityGroupAsync();

                    // use the affinity group for the virtual network configuration 
                    string newVirtualNetworkConfig = string.Format(_parameters.VirtualNetworkConfig, _parameters.AffinityGroupName);

                    await _virtualNetworkManagementClient.Networks.SetConfigurationAsync(
                        new NetworkSetConfigurationParameters()
                        {
                            Configuration = newVirtualNetworkConfig
                        });
                }
                catch (Exception e)
                {
                    Console.WriteLine("\n Error: \n {0}", e.ToString());
                }
            }
        }

        internal async Task CreateAffinityGroupAsync()
        {
            bool exists = false;

            try
            {
                // check if the affinity group already exists
                var affinityGroup = await _managementClient.AffinityGroups.GetAsync(_parameters.AffinityGroupName);

                exists = (affinityGroup.Name == _parameters.AffinityGroupName);
            }
            catch (Microsoft.WindowsAzure.CloudException e)
            {
                if (e.ErrorCode != "ResourceNotFound") throw;
            }
            catch (Exception e)
            {
                Console.WriteLine("\n Error: \n {0}", e.ToString());
            }

            if (!exists)
            {
                // get default location
                LocationsListResponse.Location defaultLocation = _managementClient.Locations.List().Locations.FirstOrDefault<LocationsListResponse.Location>();

                // create new affinity group 
                await _managementClient.AffinityGroups.CreateAsync(
                    new AffinityGroupCreateParameters()
                    {
                        Name = _parameters.AffinityGroupName,
                        Description = _parameters.AffinityGroupName,
                        Label = _parameters.AffinityGroupName,
                        Location = defaultLocation.Name
                    });
            }
        }

        internal async Task CleanUpAsync()
        {
            if (_virtualNetworkManagementClient != null && _managementClient != null)
            {
                try
                {
                    if (_networkConfig != null)
                    {
                        // restore original configuration
                        await _virtualNetworkManagementClient.Networks.SetConfigurationAsync(
                            new NetworkSetConfigurationParameters()
                            {
                                Configuration = _networkConfig.Configuration
                            });
                    }

                    // check if the affinity group already exists
                    await _managementClient.AffinityGroups.DeleteAsync(_parameters.AffinityGroupName);

                }
                catch (Exception e)
                {
                    Console.WriteLine("\n Error: \n {0}", e.ToString());
                }
            }

        }

        private string AddXmlElement(string xmlDocument, string elementName, string newElement)
        {
            XNamespace nc = "http://schemas.microsoft.com/ServiceHosting/2011/07/NetworkConfiguration";

            var document = XElement.Parse(xmlDocument);
            var content = XElement.Parse(newElement);

            // find elements with the element name
            var elements =
                from e in document.Descendants(nc + elementName)
                select e;


            if (elements.Count() > 0)
            {
                // add the new element as a child
                elements.First().Add(content);
            }
            else
            {
                Console.WriteLine("Cannot find element {0} in {1}", elementName, xmlDocument);
            }

            // return the modified xml document
            return document.ToString();
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

        public void Dispose()
        {
            if (_virtualNetworkManagementClient != null)
                _virtualNetworkManagementClient.Dispose();
        }
    }
}
