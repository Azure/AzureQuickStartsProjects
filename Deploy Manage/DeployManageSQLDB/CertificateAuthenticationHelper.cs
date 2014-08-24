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
using System;
using System.Security.Cryptography.X509Certificates;

namespace DeployManageSQLDB
{
    internal class CertificateAuthenticationHelper
    {
        internal static SubscriptionCloudCredentials GetCredentials(
            string subscrtionId,
            string base64EncodedCert)
        {
            return new CertificateCloudCredentials(subscrtionId,
                new X509Certificate2(Convert.FromBase64String(base64EncodedCert)));
        }
    }
}
