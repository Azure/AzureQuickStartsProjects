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
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataBlobStorage
{
    /// <summary>
    /// Azure Storage Blob Sample - Demonstrate how to use Blob Storage 
    /// 
    /// References: 
    /// - What is a Storage Account - http://azure.microsoft.com/en-us/documentation/articles/storage-whatis-account/
    /// - Getting Started with Blobs - http://azure.microsoft.com/en-us/documentation/articles/storage-dotnet-how-to-use-blobs/
    /// </summary>
    class Program
    {

        static void Main(string[] args)
        {
            //*************************************************************************************************************************
            //TODO: 1. Create a Storage Account through the Portal and provide your [AccountName] and [AccountKey] in the App.Config 
            //      2. Set the fullPathToFileForUpload variable below
            //      3. Set breakpoints and run the project
            //      4. Note that if you exit before the application terminates completely you might leave an image publically viewable
            //*************************************************************************************************************************

            var imageToUpload = "HelloWorld.png";

            CloudStorageAccount storageAccount = CreateStorageAccountFromConnectionString(CloudConfigurationManager.GetSetting("StorageConnectionString"));

            // Create a blob client for interacting with the blob service
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            // Create a container for organizing blobs within the storage account granting it public access
            CloudBlobContainer container = CreateContainer(blobClient, "democontainer", new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Blob });

            // Upload a BlockBlob to the newly creating container
            CloudBlockBlob blockBlob = UploadBlockBlob(container, imageToUpload);

            // List all the blobs in the container 
            ListBlobsInContainer(container);

            // Download a blob to your file system
            DownloadBlob(blobClient, blockBlob.Uri);

            // Clean up after the demo 
            DeleteBlob(blobClient, blockBlob.Uri);

            Console.ReadLine();
        }

        /// <summary>
        /// This method validates the connection string information in app.config and throws an exception if it looks like 
        /// the user hasn't updated this to valid values. 
        /// </summary>
        private static CloudStorageAccount CreateStorageAccountFromConnectionString(string storageConnectionString)
        {
            CloudStorageAccount storageAccount;
            try
            {
                storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            }
            catch (System.FormatException)
            {
                Console.WriteLine("Invalid storage account information provided. Please confirm the AccountName and AccountKey are valid in the app.config file.");
                throw;
            }
            return storageAccount;
        }

        /// <summary>
        /// Create a container for organizing blobs within this storage account. By default containers are private so 
        /// we also set the permsissions on this to be public thus enabling anyone to view the blobs without requiring authentication. 
        /// </summary>
        private static CloudBlobContainer CreateContainer(CloudBlobClient blobClient, string containerName, BlobContainerPermissions permissions)
        {
            Console.WriteLine("> Create Container '{0}' and Set Permissions to {1}", containerName, permissions.ToString());
            CloudBlobContainer container = blobClient.GetContainerReference(containerName.ToLower());
            container.CreateIfNotExists();
            container.SetPermissions(permissions);
            return container;
        }

        /// <summary>
        /// Upload an image to blob storage. If the image already exists it will be overwritten. 
        /// </summary>
        private static CloudBlockBlob UploadBlockBlob(CloudBlobContainer container, string fileForUpload)
        {
            Console.WriteLine("> Uploading BlockBlob");

            // Verify the file to upload exists
            if (!File.Exists(fileForUpload))
                throw new FileNotFoundException("File to upload was not found on your local file system.", fileForUpload);

            // Create a reference to blob that we want to upload and upload it
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(Path.GetFileName(fileForUpload));
            using (var filestream = File.OpenRead(fileForUpload))
            {
                blockBlob.UploadFromStream(filestream);
            }

            Console.WriteLine("\t Blob is now available at {0}\n", blockBlob.Uri.ToString());

            return blockBlob;
        }

        /// <summary>
        /// List all the blobs in a container - see getting started for more details on listing hierarchies and blob properties
        /// </summary>
        private static void ListBlobsInContainer(CloudBlobContainer container)
        {
            Console.WriteLine("> List Blobs");
            foreach (IListBlobItem blob in container.ListBlobs(null, false))
            {
                // Blob type will be CloudBlockBlob, CloudPageBlob or CloudBlobDirectory
                // Use blob.GetType() and cast to appropriate type to gain access to properties specific to each type
                Console.WriteLine("\t {0} {1} \t {2}\n", blob.GetType(), Environment.NewLine, blob.Uri);
            }

        }

        /// <summary>
        /// Download a blob to the local file system
        /// </summary>
        private static void DownloadBlob(CloudBlobClient blobClient, Uri uri)
        {
            Console.WriteLine("> Download Blob from {0}", uri.ToString());
            //Demonstrate how to download a blob from a Uri to the file system 

            ICloudBlob blob = blobClient.GetBlobReferenceFromServer(uri);
            var downloadToPath = string.Format("./CopyOf{0}", blob.Name);
            using (var fs = File.OpenWrite(downloadToPath))
            {
                blob.DownloadToStream(fs);
                Console.WriteLine("\t Blob downloaded to file: {0}\n", downloadToPath);
            }

        }

        /// <summary>
        /// Clean up by deleting the blob from the container 
        /// </summary>
        private static void DeleteBlob(CloudBlobClient blobClient, Uri uri)
        {
            Console.WriteLine("> Delete Blob");

            ICloudBlob blob = blobClient.GetBlobReferenceFromServer(uri);
            var success = blob.DeleteIfExists();

            Console.WriteLine("\t {0} Deleting Blob {1}\n", success ? "Successful" : "Unsuccessful", uri.ToString());
        }
    }
}

