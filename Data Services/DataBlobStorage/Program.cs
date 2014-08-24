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
    class Program
    {

        static void Main(string[] args)
        {

            //******************************************************************************************************
            //TODO: 1. Create a Storage Account through the Portal and provide your [AccountName] and 
            //         [AccountKey] in the App.Config http://go.microsoft.com/fwlink/?LinkId=325276
            //      2. Set the fullPathToFileForUpload variable below
            //      3. Set breakpoints and run the project
            //*****************************************************************************************************

            var fullPathToFileForUpload = @"C:\<your_file_path>";

            // Retrieve storage account from connection string.
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("StorageConnectionString"));

            //Create the blob client
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            //Demonstrate how to create a container with public access
            CloudBlobContainer container = CreateContainer(blobClient, "democontainer", new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Blob });

            //Demonstrate how to upload a BlockBlob
            CloudBlockBlob blockBlob = UploadBlockBlob(container, fullPathToFileForUpload);

            ListBlobsInContainer(container);

            DownloadBlob(blobClient, blockBlob.Uri);

            DeleteBlob(blobClient, blockBlob.Uri);
        }

        private static CloudBlobContainer CreateContainer(CloudBlobClient blobClient, string containerName, BlobContainerPermissions permissions)
        {
            Console.WriteLine("> Create Container '{0}' and Set Permissions to {1}", containerName, permissions.ToString());
            //Retrieve a reference to a container
            CloudBlobContainer container = blobClient.GetContainerReference(containerName.ToLower());

            //Create the container if it doesn't already exist
            container.CreateIfNotExists();

            //Set the container permissions
            container.SetPermissions(permissions);

            return container;
        }

        private static CloudBlockBlob UploadBlockBlob(CloudBlobContainer container, string fullPathToFileForUpload)
        {
            Console.WriteLine("> Uploading BlockBlob");
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(Path.GetFileName(fullPathToFileForUpload));

            using (var filestream = File.OpenRead(fullPathToFileForUpload))
            {
                blockBlob.UploadFromStream(filestream);
            }

            Console.WriteLine("\t Blob is now available at {0}", blockBlob.Uri.ToString());

            return blockBlob;
        }

        private static void ListBlobsInContainer(CloudBlobContainer container)
        {
            Console.WriteLine("> List Blobs");
            foreach (IListBlobItem blob in container.ListBlobs(null, false))
            {
                //Blob type will be CloudBlockBlob, CloudPageBlob or CloudBlobDirectory
                // use blob.GetType() and cast to appropriate type to gain access to properties specific to each type
                Console.WriteLine("\t {0} {1} \t {2}", blob.GetType(), Environment.NewLine, blob.Uri);
            }

            foreach (IListBlobItem blob in container.ListBlobs(null, true))
            {
                //Blob type will be CloudBlockBlob, CloudPageBlob or CloudBlobDirectory
                // use blob.GetType() and cast to appropriate type to gain access to properties specific to each type
                Console.WriteLine("\t {0} {1} \t {2}", blob.GetType(), Environment.NewLine, blob.Uri);
            }
        }

        private static void DownloadBlob(CloudBlobClient blobClient, Uri uri)
        {
            Console.WriteLine("> Download Blob from {0}", uri.ToString());
            //Demonstrate how to download a blob from a Uri to the file system 

            ICloudBlob blob = blobClient.GetBlobReferenceFromServer(uri);
            var downloadToPath = string.Format("./{0}", blob.Name);
            using (var fs = File.OpenWrite(downloadToPath))
            {
                blob.DownloadToStream(fs);
                Console.WriteLine("\t Blob downloaded to file: {0}", downloadToPath);
            }

            //Demonstrate how to download a blob from uri to a MemoryStream
            using (var ms = new MemoryStream())
            {
                blob.DownloadToStream(ms);
                //Now process the memory stream however you like
                Console.WriteLine("\t Now process the memory stream however you like. Memory Stream Length: {0}", ms.Length);
            }
        }

        private static void DeleteBlob(CloudBlobClient blobClient, Uri uri)
        {
            Console.WriteLine("> Delete Blob");

            ICloudBlob blob = blobClient.GetBlobReferenceFromServer(uri);
            var success = blob.DeleteIfExists();

            Console.WriteLine("\t {0} Deleting Blob {1}", success ? "Successful" : "Unsuccessful", uri.ToString());
        }
    }
}
