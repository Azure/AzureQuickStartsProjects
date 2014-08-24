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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Management.Network;
using Microsoft.WindowsAzure.Management.Network.Models;
using System.Security.Cryptography.X509Certificates;

namespace DeployManageVirtualNetwork
{
    class Program
    {
        static void Main(string[] args)
        {
            //********************************************************************************************************
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
            //  2. Set the PublishSettingsFilePath property below. Do not enable Remote Desktop, as this 
            //      process won't upload your certificate. 
            //  3. Review the properties AffinityGroupName, VirtualNetworkSite and VirtualNetworkConfig below
            //      VirtualNetworkSite will be used to add a new virtual network to your existing configuraiton.
            //      VirtualNetworkConfig contains a sample network configuration that will replace your 
            //      existing network configuration.
            //  4. Run
            //********************************************************************************************************

            var serviceParameters = new ManagementControllerParameters
            {
                PublishSettingsFilePath = @"C:\somepath\somefile.publishsettings",
                AffinityGroupName = @"TestAffinityGroup",
                VirtualNetworkSite =
                      @"<VirtualNetworkSite name=""test"" AffinityGroup=""{0}""
                          xmlns=""http://schemas.microsoft.com/ServiceHosting/2011/07/NetworkConfiguration"">
                        <AddressSpace>
                          <AddressPrefix>10.0.0.0/8</AddressPrefix>
                        </AddressSpace>
                        <Subnets>
                          <Subnet name=""subnet1"">
                            <AddressPrefix>10.10.1.0/24</AddressPrefix>
                          </Subnet>
                          <Subnet name=""subnet2"">
                            <AddressPrefix>10.10.2.0/24</AddressPrefix>
                          </Subnet>
                        </Subnets>
                      </VirtualNetworkSite>",
                VirtualNetworkConfig =
                    @"<?xml version=""1.0"" encoding=""utf-8""?>
                    <NetworkConfiguration xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" 
                        xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" 
                        xmlns=""http://schemas.microsoft.com/ServiceHosting/2011/07/NetworkConfiguration"">
                      <VirtualNetworkConfiguration>
                        <Dns>
                          <DnsServers>
                            <DnsServer name=""DNS"" IPAddress=""10.1.0.4"" />
                          </DnsServers>
                        </Dns>
                        <LocalNetworkSites>
                          <LocalNetworkSite name=""MyLocalNetwork1"">
                            <AddressSpace>
                              <AddressPrefix>192.168.0.0/24</AddressPrefix>
                            </AddressSpace>
                            <VPNGatewayAddress>1.1.1.1</VPNGatewayAddress>
                          </LocalNetworkSite>
                        </LocalNetworkSites>
                        <VirtualNetworkSites>
	                       <VirtualNetworkSite name=""test1"" AffinityGroup=""{0}"">
                            <AddressSpace>
                              <AddressPrefix>10.0.0.0/8</AddressPrefix>
                            </AddressSpace>
                            <Subnets>
                              <Subnet name=""subnet1"">
                                <AddressPrefix>10.10.1.0/24</AddressPrefix>
                              </Subnet>
                              <Subnet name=""subnet2"">
                                <AddressPrefix>10.10.2.0/24</AddressPrefix>
                              </Subnet>
                            </Subnets>
		                    <DnsServersRef>
                              <DnsServerRef name=""DNS"" />
                            </DnsServersRef>
                          </VirtualNetworkSite>
	                       <VirtualNetworkSite name=""test2"" AffinityGroup=""{0}"">
                            <AddressSpace>
                              <AddressPrefix>10.0.0.0/8</AddressPrefix>
                            </AddressSpace>
                            <Subnets>
                                <Subnet name=""Subnet-1"">
                                  <AddressPrefix>10.1.0.0/16</AddressPrefix>
                                </Subnet>
                                <Subnet name=""Subnet-2"">
                                  <AddressPrefix>10.2.0.0/16</AddressPrefix>
                                </Subnet>
                                <Subnet name=""Subnet-3"">
                                  <AddressPrefix>10.3.0.0/16</AddressPrefix>
                                </Subnet>
                            </Subnets>
                            <DnsServersRef>
                              <DnsServerRef name=""DNS"" />
                            </DnsServersRef>
                          </VirtualNetworkSite>
                        </VirtualNetworkSites>
                      </VirtualNetworkConfiguration>
                    </NetworkConfiguration>"
            };

            Task.WaitAll(DemoVirtualNetworkOperations(serviceParameters));
        }

        private static async Task DemoVirtualNetworkOperations(ManagementControllerParameters managementControllerParameters)
        {
            using (var controller = new ManagementController(managementControllerParameters))
            {
                Console.WriteLine("1. List virtual networks");
                ConsoleContinuePrompt("list");
                controller.ListVirtualNetworks();
                Console.WriteLine("\n...Complete\n");
                
                Console.WriteLine("2. Get virtual network configuration");
                ConsoleContinuePrompt("get configuration");
                await controller.GetVirtualNetworkConfigurationAsync();
                Console.WriteLine("\n...Complete\n");
                
                Console.WriteLine("3. Add virtual network");
                ConsoleContinuePrompt("add");
                await controller.AddVirtualNetworkSiteAsync();
                Console.WriteLine("\n...Complete\n");
                
                Console.WriteLine("4. Set new virtual network configuration");
                Console.WriteLine("********************************************************************************");
                Console.WriteLine("         Your existing virtual network configuration will be replaced.");
                Console.WriteLine("         This operation will fail if you have virtual networks in use ");
                Console.WriteLine("         that cannot be deleted.");
                Console.WriteLine("********************************************************************************");
                ConsoleContinuePrompt("set");
                await controller.SetVirtualNetworkConfigurationAsync();
                Console.WriteLine("\n...Complete\n");
                
                Console.WriteLine("5. Restore original configuraiton");
                ConsoleContinuePrompt("restore");
                await controller.CleanUpAsync();
                Console.WriteLine("\n...Complete\n");

                Console.WriteLine("Done. Press a key to exit"); 
                Console.ReadKey();
            }
        }

        private static void ConsoleContinuePrompt(string prompt)
        {
            Console.WriteLine("\n\t > Press Enter to {0}", prompt);
            Console.ReadLine();
            Console.WriteLine("\t Starting, view network config in the Microsoft Azure managent portal...");
        }
    }
}
