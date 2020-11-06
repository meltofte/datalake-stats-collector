using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using datalake_stats.Model;
using ExcelDataReader;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;

namespace datalake_stats.Helpers
{
    /// <summary>
    /// Helper class for blob storage operations.
    /// </summary>
    internal static class BlobStorageHelper
    {
        //https://docs.microsoft.com/en-us/azure/storage/blobs/storage-blobs-list?tabs=dotnet

        //https://github.com/Azure-Samples/storage-blob-dotnet-getting-started/blob/master/BlobStorage/Advanced.cs

        //https://docs.microsoft.com/en-us/azure/storage/blobs/storage-quickstart-blobs-dotnet


        /// <summary>
        /// Returns a BlobServiceClient using DefaultAzureCredential.
        /// It will authenticate using the credentials specified in the environment variables AZURE_TENANT_ID, AZURE_CLIENT_ID and AZURE_CLIENT_SECRET
        /// </summary>
        /// <param name="accountName">The storage account name.</param>
        /// <returns>An instance of BlobServiceClient.</returns>
        private static BlobServiceClient GetBlobServiceClient(string accountName)
        {
            return new BlobServiceClient(new Uri($"https://{accountName}.blob.core.windows.net"), new DefaultAzureCredential());
        }

        /// <summary>
        /// Reads the blob as an Excel file and returns the number of rows in the Samples sheet
        /// </summary>
        /// <param name="container">An instance of ContainerClient</param>
        /// <param name="blobPath">The full path to the blob containing the Excel file</param>
        /// <returns>The number of rows in the "Samples" sheet</returns>
        internal static int GetNumberOfSamples(BlobContainerClient container, string blobPath)
        {
            BlobClient blobClient = container.GetBlobClient(blobPath);
            DataSet dataSet;
            using (var stream = new MemoryStream())
            {
                blobClient.DownloadTo(stream);
                using var reader = ExcelReaderFactory.CreateReader(stream);
                dataSet = reader.AsDataSet(new ExcelDataSetConfiguration()
                {
                    ConfigureDataTable = (_) => new ExcelDataTableConfiguration()
                    {
                        UseHeaderRow = true,
                    }
                });
            }
            DataTable table = dataSet.Tables["Samples"];
            return table.Rows.Count;
        }

        /// <summary>
        /// Collects proteomics statistics.
        /// </summary>
        /// <param name="accountName">The name of the proteomics storage account.</param>
        /// <param name="containerName">The name of the container to collect stats in.</param>
        /// <returns>An instance of ProteomicsStats</returns>
        internal static ProteomicsStats GetProteomicsStats(string accountName, string containerName)
        {
            var client = GetBlobServiceClient(accountName);
            var containerClient = client.GetBlobContainerClient(containerName);

            int numberOfFolders = 0;
            int numberOfSamples = 0;
            var requestNames = new List<string>();

            try
            {
                string segmentContinuationToken = null;
                do
                {
                    var resultSegment = containerClient.GetBlobsByHierarchy(prefix: "", delimiter: "/").AsPages(segmentContinuationToken);

                    //loop result segments
                    foreach (Azure.Page<BlobHierarchyItem> blobPage in resultSegment)
                    {
                        //if IsPrefix=true, we have a virtual folder
                        var folders = blobPage.Values.Where(x => x.IsPrefix);
                        numberOfFolders += folders.Count();

                        //loop (virtual) folders in first level = request runs
                        foreach (var folder in folders)
                        {
                            string folderContinuationToken = null;
                            var pref = folder.Prefix;
                            //get and loop second level folders (first level folders within a request run folder)
                            var innerResultSegment = containerClient.GetBlobsByHierarchy(prefix: pref, delimiter: "/").AsPages(folderContinuationToken);
                            foreach (Azure.Page<BlobHierarchyItem> innerBlobPage in innerResultSegment)
                            {
                                string metadataSheetPath = pref + "metadata.xlsx";

                                if (innerBlobPage.Values.Any(x => x.IsBlob && x.Blob.Name == metadataSheetPath))
                                {
                                    numberOfSamples += GetNumberOfSamples(containerClient, metadataSheetPath);
                                }
                                folderContinuationToken = innerBlobPage.ContinuationToken;
                            }
                        }

                        //add folder names to list of request names
                        requestNames.AddRange(folders.Select(x => x.Prefix.Replace("/", "")));

                        //Get the continuation token and loop until it is empty.
                        segmentContinuationToken = blobPage.ContinuationToken;
                    }

                } while (segmentContinuationToken != "");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                throw;
            }

            var stats = new ProteomicsStats
            {
                NumberOfRuns = numberOfFolders,
                NumberOfSamples = numberOfSamples,
                SizeInBytes = GetSize(containerClient, ""),
                RequestNames = JsonConvert.SerializeObject(requestNames)
            };

            return stats;
        }

        /// <summary>
        /// Collects NGS run statistics for the specified folder.
        /// </summary>
        /// <param name="accountName">The name of the proteomics storage account.</param>
        /// <param name="containerName">The name of the container to collect stats in.</param>
        /// <param name="prefix">The prefix of the folder path.</param>
        /// <returns>An instance of NgsRunStats</returns>
        internal static NgsRunStats GetNgsRunStats(string accountName, string containerName, string prefix)
        {
            var client = GetBlobServiceClient(accountName);
            var containerClient = client.GetBlobContainerClient(containerName);

            int folderCount = 0;

            try
            {
                string continuationToken = null;
                do
                {
                    var resultSegment = containerClient.GetBlobsByHierarchy(prefix: prefix, delimiter: "/").AsPages(continuationToken);
                    //loop result segments
                    foreach (Azure.Page<BlobHierarchyItem> blobPage in resultSegment)
                    {
                        //if IsPrefix=true, we have a virtual folder
                        var folders = blobPage.Values.Where(x => x.IsPrefix);
                        folderCount += folders.Count();

                        //get the continuation token and loop until it is empty.
                        continuationToken = blobPage.ContinuationToken;
                    }
                } while (continuationToken != "");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                throw;
            }

            var stats = new NgsRunStats
            {
                NumberOfRuns = folderCount,
                SizeInBytes = GetSize(containerClient, prefix)
            };

            return stats;
        }

        /// <summary>
        /// Collects NGS sample statistics.
        /// </summary>
        /// <param name="accountName">The name of the proteomics storage account.</param>
        /// <param name="containerName">The name of the container to collect stats in.</param>
        /// <returns></returns>
        internal static NgsSampleStats GetNgsSampleStats(string accountName, string containerName)
        {
            var client = GetBlobServiceClient(accountName);
            var containerClient = client.GetBlobContainerClient(containerName);
            int folderCount = 0;

            var sampleNames = new List<string>();

            try
            {
                string continuationToken = null;
                do
                {
                    var resultSegment = containerClient.GetBlobsByHierarchy(prefix: "", delimiter: "/").AsPages(continuationToken);
                    //loop result segments
                    foreach (Azure.Page<BlobHierarchyItem> blobPage in resultSegment)
                    {
                        //if IsPrefix=true, we have a virtual folder
                        var folders = blobPage.Values.Where(x => x.IsPrefix);
                        folderCount += folders.Count();

                        sampleNames.AddRange(folders.Select(x => x.Prefix.Replace("/", "")));

                        //get the continuation token and loop until it is empty.
                        continuationToken = blobPage.ContinuationToken;
                    }


                } while (continuationToken != "");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                throw;
            }

            var stats = new NgsSampleStats
            {
                NumberOfSamples = folderCount,
                SizeInBytes = GetSize(containerClient, ""),
                SampleNames = JsonConvert.SerializeObject(sampleNames)
            };

            return stats;
        }

        /// <summary>
        /// Returns the total size of the blobs in the specified container.
        /// </summary>
        /// <param name="container">An instance of BlobContainerClient</param>
        /// <param name="prefix">The prefix of the folder path</param>
        /// <returns>The size in bytes.</returns>
        private static long GetSize(BlobContainerClient container, string prefix)
        {
            long fileSize = 0;
            foreach (var blobItem in container.GetBlobs(prefix: prefix))
            {
                fileSize += blobItem.Properties.ContentLength.Value;
            }
            return fileSize;
        }

        //private static int CountFolders(BlobContainerClient container, string prefix)
        //{
        //    int result = 0;

        //    try
        //    {
        //        string continuationToken = null;
        //        do
        //        {
        //            var resultSegment = container.GetBlobsByHierarchy(prefix: prefix, delimiter: "/").AsPages(continuationToken);

        //            foreach (Azure.Page<BlobHierarchyItem> blobPage in resultSegment)
        //            {
        //                var folders = blobPage.Values.Where(x => x.IsPrefix);
        //                result += folders.Count();
        //                continuationToken = blobPage.ContinuationToken;
        //            }
        //        } while (continuationToken != "");
        //    }
        //    catch (Exception e)
        //    {
        //        Console.WriteLine(e.Message);
        //        throw;
        //    }

        //    return result;
        //}


        //static void SaveBlob(BlobServiceClient blobServiceClient, string message)
        //{
        //    string containerName = "stats-test";
        //    BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);

        //    // Create a local file in the ./data/ directory for uploading and downloading
        //    string localPath = "./";
        //    string fileName = "stats_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".txt";
        //    string localFilePath = Path.Combine(localPath, fileName);

        //    // Write text to the file
        //    File.WriteAllText(localFilePath, message);

        //    // Get a reference to a blob
        //    BlobClient blobClient = containerClient.GetBlobClient(fileName);

        //    Console.WriteLine("Uploading to Blob storage as blob:\n\t {0}\n", blobClient.Uri);

        //    // Open the file and upload its data
        //    using FileStream uploadFileStream = File.OpenRead(localFilePath);
        //    blobClient.Upload(uploadFileStream, true);
        //    uploadFileStream.Close();
        //}
    }
}
