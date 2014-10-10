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


namespace DeployManageSQLDB
{
    struct SqlManagementControllerParameters
    {
        internal string PublishSettingsFilePath { get; set; }
        internal string ServerRegion { get; set; }
        internal string ServerAdminPassword { get; set; }
        internal string ServerAdminUsername { get; set; }
        internal string DatabaseName { get; set; }
        internal string DatabaseEdition { get; set; }
        internal int? DatabaseMaxSizeInGB { get; set; }
        internal string DatabaseCollation { get; set; }
        internal string FirewallRuleName { get; set; }
        internal string FirewallRuleStartIP { get; set; }
        internal string FirewallRuleEndIP { get; set; }
        internal bool FirewallRuleAllowAzureServices { get; set; }
    }
}
