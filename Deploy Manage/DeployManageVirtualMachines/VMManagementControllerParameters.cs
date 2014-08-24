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

namespace DeployManageVirtualMachines
{
    internal class VMManagementControllerParameters
    {
        internal string Region { get; set; }
        internal string StorageAccountName { get; set; }
        internal string CloudServiceName { get; set; }
        internal string VMName { get; set; }
        internal string PublishSettingsFilePath { get; set; }
        internal string AdminUsername { get; set; }
        internal string AdminPassword { get; set; }
        public int RDPPort { get; set; }
    }
}
