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
using System.Linq;
using System.Configuration;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.WindowsAzure.MediaServices.Client;
using System.Diagnostics;

namespace AppServicesMediaTranscode
{
    class Program
    {
        //****************************************************************************************
        // Microsoft Azure Media Services allows you to build scalable, cost effective, end-to-end
        // media distribution solutions that can stream media to Adobe Flash, Android, iOS, Windows,
        // and other devices and platforms. If you dont have a Microsoft Azure subscription you can 
        // get a FREE trial account here: http://go.microsoft.com/fwlink/?LinkId=330212
        //
        // This QuickStart demonstrates the following scenarios:
        //  1. Connecting to an Azure Media Services account
        //  2. Uploading an MP4 video asset
        //  3. Creating and executing a Smooth Streaming encoding job
        //  4. Creating a locator URL to a streaming media asset on an origin server.
        //
        // For more information about Azure Media Services, see 
        // http://azure.microsoft.com/en-us/develop/media-services/
        //
        //TODO: 1. Provision a Media Service using the Microsoft Azure Management Portal 
        //      http://go.microsoft.com/fwlink/?LinkId=324582 
        //      2. Open App.Config and update the value of  appSetting MediaServicesAccountName
        //         and MediaServicesAccountKey
        //      3. Update _singleInputMp4Path variable below to point at your *.mp4 input file
        //****************************************************************************************    

        private static readonly string _singleInputMp4Path = @"C:\temp\myvideo.mp4";
        private static readonly string _mediaServicesAccountKey = ConfigurationManager.AppSettings["MediaServicesAccountKey"];
        private static readonly string _mediaServicesAccountName = ConfigurationManager.AppSettings["MediaServicesAccountName"];


        // Field for service context.
        private static CloudMediaContext _context = null;

        static void Main(string[] args)
        {
            if (!VerifyConfiguration())
            {
                Console.ReadLine();
                return;
            }

            _context = new CloudMediaContext(_mediaServicesAccountName, _mediaServicesAccountKey);

            //Create an encrypted asset and upload the mp4. 
            IAsset asset = CreateAssetAndUploadSingleFile(AssetCreationOptions.StorageEncrypted, _singleInputMp4Path);


            IJob job = EncodeToSmoothStreaming(asset, _singleInputMp4Path);

            if (job != null)
            {
                if (job.State != JobState.Error)
                {
                    // Get a reference to the output asset from the job.
                    IAsset outputAsset = job.OutputMediaAssets[0];
                    GetStreamingOriginLocator(outputAsset);
                }
            }

            Console.WriteLine("Please press enter to exit...");
            Console.ReadLine();
        }

        private static bool VerifyConfiguration()
        {
            bool configOK = true;
            if (String.IsNullOrWhiteSpace(_mediaServicesAccountName))
            {
                configOK = false;
                Console.WriteLine("Please update the 'MediaServicesAccountName' appSetting in app.config to specify your Azure Media Services account name.");
            }
            if (String.IsNullOrWhiteSpace(_mediaServicesAccountKey))
            {
                configOK = false;
                Console.WriteLine("Please update the 'MediaServicesAccountKey' appSetting in app.config to specify your Azure Media Services account key.");
            }
            if (!File.Exists(_singleInputMp4Path))
            {
                configOK = false;
                Console.WriteLine("Please update the '_singleInputMp4Path' variable to point to a local video file that will be uploaded.");
            }
            return configOK;

        }


        static public IAsset CreateAssetAndUploadSingleFile(AssetCreationOptions assetCreationOptions, string singleFilePath)
        {
            var assetName = "UploadSingleFile_" + DateTime.UtcNow.ToString();
            var asset = CreateEmptyAsset(assetName, assetCreationOptions);

            var fileName = Path.GetFileName(singleFilePath);

            var assetFile = asset.AssetFiles.Create(fileName);

            Console.WriteLine("Created assetFile {0}", assetFile.Name);

            var accessPolicy = _context.AccessPolicies.Create(assetName, TimeSpan.FromDays(30),
                                                                AccessPermissions.Write | AccessPermissions.List);

            var locator = _context.Locators.CreateLocator(LocatorType.Sas, asset, accessPolicy);

            Console.WriteLine("Upload {0}", assetFile.Name);

            assetFile.Upload(singleFilePath);
            Console.WriteLine("Done uploading {0}", assetFile.Name);

            locator.Delete();
            accessPolicy.Delete();

            return asset;
        }

        static private IAsset CreateEmptyAsset(string assetName, AssetCreationOptions assetCreationOptions)
        {
            var asset = _context.Assets.Create(assetName, assetCreationOptions);

            Console.WriteLine("Asset name: " + asset.Name);
            Console.WriteLine("Time created: " + asset.Created.Date.ToString());

            return asset;
        }

        public static IJob EncodeToSmoothStreaming(IAsset asset, string inputMediaFilePath)
        {
            // Declare a new job.
            IJob job = _context.Jobs.Create("My MP4 to Smooth Streaming encoding job");

            // Get a media processor reference, and pass to it the name of the 
            // processor to use for the specific task.
            IMediaProcessor processor = GetLatestMediaProcessorByName("Windows Azure Media Encoder");


            // Create a task with the conversion details, using a configuration file. 
            ITask task = job.Tasks.AddNew("My Mp4 to Smooth Task",
                processor,
                "H264 Smooth Streaming 720p",
                TaskOptions.None);

            // Specify the input asset to be encoded.
            task.InputAssets.Add(asset);

            // Add an output asset to contain the results of the job. We do not need 
            // to persist the output asset to storage, so set the shouldPersistOutputOnCompletion
            // param to false. 
            task.OutputAssets.AddNew("Output asset", AssetCreationOptions.None);

            // Use the following event handler to check job progress. 
            job.StateChanged += new EventHandler<JobStateChangedEventArgs>(StateChanged);

            // Launch the async job.

            job.Submit();

            // Optionally log job details. This displays basic job details
            // to the console and saves them to a JobDetails-{JobId}.txt file 
            // in your output folder.
            LogJobDetails(job.Id);

            // Check job execution and wait for job to finish. 
            Task progressJobTask = job.GetExecutionProgressTask(CancellationToken.None);
            progressJobTask.Wait();

            // Get a refreshed job reference after waiting on a thread.
            job = GetJob(job.Id);

            // Check for errors
            if (job.State == JobState.Error)
            {
                Console.WriteLine("\nExiting method due to job error.");
                return job;
            }

            // Get a reference to the output asset from the job.
            IAsset outputAsset = job.OutputMediaAssets[0];


            // Check for the .ism file and set it as the primary file
            var outputAssetFiles = outputAsset.AssetFiles.ToList().
                      Where(f => f.Name.EndsWith(".ism", StringComparison.OrdinalIgnoreCase)).ToArray();

            if (outputAssetFiles.Count() != 1)
                throw new ArgumentException("The asset should have only one, .ism file");

            outputAssetFiles.First().IsPrimary = true;
            outputAssetFiles.First().Update();
            return job;
        }

        // Create a locator URL to a streaming media asset 
        // on an origin server.
        public static ILocator GetStreamingOriginLocator(IAsset assetToStream)
        {
            // Get a reference to the streaming manifest file from the  
            // collection of files in the asset. 
            var theManifest =
                                from f in assetToStream.AssetFiles
                                where f.Name.EndsWith(".ism")
                                select f;

            // Cast the reference to a true IAssetFile type. 
            IAssetFile manifestFile = theManifest.First();

            // Create a 30-day readonly access policy. 
            IAccessPolicy policy = _context.AccessPolicies.Create("Streaming policy",
                TimeSpan.FromDays(30),
                AccessPermissions.Read);

            // Create a locator to the streaming content on an origin. 
            ILocator originLocator = _context.Locators.CreateLocator(LocatorType.OnDemandOrigin, assetToStream,
                policy,
                DateTime.UtcNow.AddMinutes(-5));

            // Display some useful values based on the locator.
            // Display the base path to the streaming asset on the origin server.
            Console.WriteLine();
            Console.WriteLine("Streaming asset base path on origin: ");
            Console.WriteLine(originLocator.Path);
            Console.WriteLine();

            // Create a full URL to the manifest file. Use this for playback
            // in streaming media clients. 
            string urlForClientStreaming = originLocator.Path + manifestFile.Name + "/manifest";

            Console.WriteLine("URL to manifest for smooth streaming: ");
            Console.WriteLine(urlForClientStreaming);
            Console.WriteLine();

            Console.WriteLine("Launching web player for smooth streaming.");
            Console.WriteLine();
            Process.Start("http://smf.cloudapp.net/healthmonitor?url=" + urlForClientStreaming);

            // Return the locator. 
            return originLocator;
        }
        private static void StateChanged(object sender, JobStateChangedEventArgs e)
        {
            Console.WriteLine("Job state changed event:");
            Console.WriteLine("  Previous state: " + e.PreviousState);
            Console.WriteLine("  Current state: " + e.CurrentState);

            switch (e.CurrentState)
            {
                case JobState.Finished:
                    Console.WriteLine();
                    Console.WriteLine("********************");
                    Console.WriteLine("Job is finished.");
                    Console.WriteLine("********************");
                    Console.WriteLine();
                    Console.WriteLine();
                    break;
                case JobState.Canceling:
                case JobState.Queued:
                case JobState.Scheduled:
                case JobState.Processing:
                    Console.WriteLine("Please wait...\n");
                    break;
                case JobState.Canceled:
                case JobState.Error:
                    // Cast sender as a job.
                    IJob job = (IJob)sender;
                    // Display or log error details as needed.
                    LogJobStop(job.Id);
                    break;
                default:
                    break;
            }
        }
        private static void LogJobDetails(string jobId)
        {
            IJob job = GetJob(jobId);

            Console.WriteLine("Job ID: " + job.Id);
            Console.WriteLine("Job Name: " + job.Name);
            Console.WriteLine("Job started (server UTC time): " + job.StartTime.ToString());
        }
        private static void LogJobStop(string jobId)
        {
            IJob job = GetJob(jobId);

            LogJobDetails(job.Id);

            // Log job errors if they exist.  
            if (job.State == JobState.Error)
            {
                Console.WriteLine("Error Details: \n");
                foreach (ITask task in job.Tasks)
                {
                    foreach (ErrorDetail detail in task.ErrorDetails)
                    {
                        Console.WriteLine("  Task Id: " + task.Id);
                        Console.WriteLine("    Error Code: " + detail.Code);
                        Console.WriteLine("    Error Message: " + detail.Message + "\n");
                    }
                }
            }
        }
        private static IMediaProcessor GetLatestMediaProcessorByName(string mediaProcessorName)
        {
            // The possible strings that can be passed into the 
            // method for the mediaProcessor parameter:
            //   Windows Azure Media Encoder
            //   Windows Azure Media Packager
            //   Windows Azure Media Encryptor
            //   Storage Decryption

            var processor = _context.MediaProcessors.Where(p => p.Name == mediaProcessorName).
                ToList().OrderBy(p => new Version(p.Version)).LastOrDefault();

            if (processor == null)
                throw new ArgumentException(string.Format("Unknown media processor", mediaProcessorName));

            return processor;
        }

        static IJob GetJob(string jobId)
        {
            // Use a Linq select query to get an updated 
            // reference by Id. 
            var jobInstance =
                from j in _context.Jobs
                where j.Id == jobId
                select j;
            // Return the job reference as an Ijob. 
            IJob job = jobInstance.FirstOrDefault();

            return job;
        }
        static IAsset GetAsset(string assetId)
        {
            // Use a LINQ Select query to get an asset.
            var assetInstance =
                from a in _context.Assets
                where a.Id == assetId
                select a;
            // Reference the asset as an IAsset.
            IAsset asset = assetInstance.FirstOrDefault();

            return asset;
        }
    }
}