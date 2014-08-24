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

namespace DeployManageWebSites
{
    class PublishDemoHelper
    {
        internal static void GenerateDeploymentScript(string fileName, string userName, string webSiteName, string repoHost)
        {
            string gitRemote = string.Format("https://{0}@{1}:443/{2}.git",userName, repoHost, webSiteName);
            string[] lines = {
            "echo off",                        
            "echo git publishing",
            "echo ==============",
            "echo when prompted, please enter the password for pusblishing user: "+userName,
            "echo use the Microsoft Azure Management Portal to change the publishing credentials for this subscription",
            "pause",
            "git init",
            "git add default.html",
            "git commit -m \"test\"",
            "git remote remove azure",
            "git remote add azure "+ gitRemote,
            "git push azure master",
            "echo finished git publishing",
            "pause"
            };
            System.IO.File.WriteAllLines(fileName, lines);
        }

        internal static void GenerateDefaultHtml(string fileName, string userName)
        {
            string[] htmlLines = {
            "<html>",
            "<h1>Hello "+userName+"</h1>",
            "</html>"
            };
            System.IO.File.WriteAllLines("default.html", htmlLines);
        }
    }
}
