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

using System.IO;
using System.Diagnostics;
using System.Xml.Linq;
using System.Linq;
using System;
using System.Threading.Tasks;
using System.Globalization;
using System.Collections.Generic;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Management.WebSites;
using Microsoft.WindowsAzure.Management.WebSites.Models;
using System.Security.Cryptography.X509Certificates;


namespace DeployManageWebSites
{
    internal partial class ManagementController : IDisposable
    {
        private WebSiteManagementClient _webSiteManagementClient;
        private ManagementControllerParameters _parameters;
        private WebSiteCreateParameters.WebSpaceDetails _webSpaceDetails;
        
        private const string ServerFarmName = "DefaultServerFarm";



        public ManagementController(ManagementControllerParameters parameters)
        {
            _parameters = parameters;

            // To authenticate against the Microsoft Azure service management API we require management certificate
            // load this from a publish settings file and later use it with the Service Management Libraries
            var credential = GetSubscriptionCloudCredentials(parameters.PublishSettingsFilePath);

            _webSiteManagementClient = CloudContext.Clients.CreateWebSiteManagementClient(credential);

            _webSpaceDetails = CreateWebSpaceDetails(_parameters.GeoRegion);
        }

        private WebSiteCreateParameters.WebSpaceDetails CreateWebSpaceDetails(string geoRegion)
        {
            return new WebSiteCreateParameters.WebSpaceDetails
            {
                Name = GetWebSpaceName(geoRegion),
                GeoRegion = geoRegion,
                Plan = WebSpacePlanNames.VirtualDedicatedPlan
            };
        }

        private string GetWebSpaceName(string geoRegion)
        {
            switch (geoRegion)
            {
                case GeoRegionNames.EastAsia:
                    return WebSpaceNames.EastAsiaWebSpace;
                case GeoRegionNames.EastUS:
                    return WebSpaceNames.EastUSWebSpace;
                case GeoRegionNames.NorthCentralUS:
                    return WebSpaceNames.NorthCentralUSWebSpace;
                case GeoRegionNames.NorthEurope:
                    return WebSpaceNames.NorthEuropeWebSpace;
                case GeoRegionNames.WestEurope:
                    return WebSpaceNames.WestEuropeWebSpace;
                case GeoRegionNames.WestUS:
                    return WebSpaceNames.WestUSWebSpace;
                default:
                    throw new Exception("WebSpaceName for region doensn't exist/not mapped");
            }
        }

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

            return CertificateAuthenticationHelper.GetCredentials(publishSettingCreds.SubscriptionId, publishSettingCreds.ManagementCertificate);
        }

        private async Task UpdateServerFarm()
        {
            var serverFarmList = await _webSiteManagementClient.ServerFarms.ListAsync(_webSpaceDetails.Name);
            if (serverFarmList.ServerFarms.Count > 0)
            {
                // ServerFarm already exists (there can only be one ServerFarm per WebSpace)
                if (serverFarmList.ServerFarms[0].Name == ServerFarmName)
                {
                    await _webSiteManagementClient.ServerFarms.UpdateAsync(_webSpaceDetails.Name,
                        new ServerFarmUpdateParameters
                        {
                            NumberOfWorkers = _parameters.NumberOfWorkers,
                            WorkerSize = _parameters.WorkerSize
                        });
                }
            }
            else
            {
                await _webSiteManagementClient.ServerFarms.CreateAsync(_webSpaceDetails.Name,
                    new ServerFarmCreateParameters
                    {
                        NumberOfWorkers = _parameters.NumberOfWorkers,
                        WorkerSize = _parameters.WorkerSize                        
                    });
            }
        }
        internal async Task CreateWebSite()
        {
            await _webSiteManagementClient.WebSites.CreateAsync(_webSpaceDetails.Name,
                new WebSiteCreateParameters
                {
                    Name = _parameters.WebSiteName,
                    HostNames = new string[]{_parameters.WebSiteName +".azurewebsites.net"},
                    ComputeMode = WebSiteComputeMode.Shared,
                    SiteMode = WebSiteMode.Limited,
                    ServerFarm = ServerFarmName,
                    WebSpace = _webSpaceDetails,
                    WebSpaceName = _webSpaceDetails.Name
                });
        }

        internal async Task ConfigureWebSite()
        {
            var newConnectionStrings = new List<WebSiteUpdateConfigurationParameters.ConnectionStringInfo>();
            newConnectionStrings.Add(new WebSiteUpdateConfigurationParameters.ConnectionStringInfo()
            {
                Type = "SQLAzure", // SQLAzure, MySql, SqlServer, Custom
                Name = "sqlazure_connection_string",
                ConnectionString = "value_for_sqlazure_connection_string"
            });
            newConnectionStrings.Add(new WebSiteUpdateConfigurationParameters.ConnectionStringInfo()
            {
                Type = "MySql", // SQLAzure, MySql, SqlServer, Custom
                Name = "mysql_connection_string",
                ConnectionString = "value_for_mysql_connection_string"
            });

            var newApplicationSettings = new Dictionary<string, string>
            {
                {"setting_1", "value_1"}, 
                {"setting_2", "value_2"}
            };

            await _webSiteManagementClient.WebSites.UpdateConfigurationAsync(_webSpaceDetails.Name, _parameters.WebSiteName,
                new WebSiteUpdateConfigurationParameters
                {
                    DefaultDocuments = { "default.html" },
                    AppSettings = newApplicationSettings,
                    ConnectionStrings = newConnectionStrings,
                    PhpVersion = "OFF",
                    ScmType = "LocalGit"
                });
            await _webSiteManagementClient.WebSites.CreateRepositoryAsync(_webSpaceDetails.Name, _parameters.WebSiteName);
        }

        internal async Task PublishWebSite()
        {
            string publishingUserName = "undefined";
            var userResponse = await _webSiteManagementClient.WebSpaces.ListPublishingUsersAsync();
            if (userResponse.Users.Count > 0)
            {
                publishingUserName = userResponse.Users[0].Name;
            }
            // get the git repo URL
            var repoResponse = await _webSiteManagementClient.WebSites.GetRepositoryAsync(_webSpaceDetails.Name, _parameters.WebSiteName);

            var workingDir = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(Directory.CreateDirectory("deployment").FullName);
            PublishDemoHelper.GenerateDefaultHtml("default.html", publishingUserName);
            PublishDemoHelper.GenerateDeploymentScript(@"deploy.cmd", publishingUserName, _parameters.WebSiteName, repoResponse.Uri.Host);
            Process.Start(@"deploy.cmd");
            Directory.SetCurrentDirectory(workingDir);
        }

        internal async Task UpgradeWebSite()
        {
            if (_parameters.UpgradePlan == WebSitePlans.Standard)
            {
                // Standard mode requires a ServerFarm update
                await UpdateServerFarm();
            }
            var resp = await _webSiteManagementClient.WebSites.GetAsync(_webSpaceDetails.Name, _parameters.WebSiteName, new WebSiteGetParameters { });
            WebSiteUpdateParameters updateParameters = new WebSiteUpdateParameters
            {
                HostNames = resp.WebSite.HostNames,
                ServerFarm = _parameters.UpgradePlan == WebSitePlans.Standard ? ServerFarmName : resp.WebSite.ServerFarm,
                ComputeMode = _parameters.UpgradePlan == WebSitePlans.Standard ? WebSiteComputeMode.Dedicated : WebSiteComputeMode.Shared,
                SiteMode = _parameters.UpgradePlan == WebSitePlans.Free ? WebSiteMode.Limited : WebSiteMode.Basic
            };

            await _webSiteManagementClient.WebSites.UpdateAsync(_webSpaceDetails.Name, _parameters.WebSiteName, updateParameters);
        }

        internal Dictionary<WebSpacesListResponse.WebSpace, IList<WebSite>> GetWebSites()
        {
            var websitesPerRegion = new Dictionary<WebSpacesListResponse.WebSpace, IList<WebSite>>();
            foreach (var webspace in _webSiteManagementClient.WebSpaces.List().WebSpaces)
            {
                foreach (var website in _webSiteManagementClient.WebSpaces.ListWebSites(webspace.Name, new WebSiteListParameters()).WebSites)
                {
                    if (websitesPerRegion.ContainsKey(webspace))
                    {
                        websitesPerRegion[webspace].Add(website);
                    }
                    else
                    {
                        var websites = new List<WebSite>();
                        websites.Add(website);

                        websitesPerRegion.Add(webspace, websites);
                    }
                }
            }
            return websitesPerRegion;
        }

        internal async Task TearDownWebSite()
        {
            //tear down everything that was created
            await _webSiteManagementClient.WebSites.DeleteAsync(
                GetWebSpaceName(_parameters.GeoRegion),
                _parameters.WebSiteName,
                new WebSiteDeleteParameters
                {
                    DeleteAllSlots = true,
                    DeleteEmptyServerFarm = true,
                    DeleteMetrics = false
                });
        }

        public void Dispose()
        {
            if (_webSiteManagementClient != null)
                _webSiteManagementClient.Dispose();
        }

    }
}