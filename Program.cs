using System;
using datalake_stats.Helpers;

namespace datalake_stats
{
    class Program
    {
        private static void CollectProteomicsStats()
        {
            int? logId = null;
            try
            {
                var accountName = Environment.GetEnvironmentVariable("PROTEOMICS_ACCOUNT");
                var containerName = Environment.GetEnvironmentVariable("PROTEOMICS_CONTAINER");
                logId = DbHelper.LogStart("PROTEOMICS_RUNS");
                var stats = BlobStorageHelper.GetProteomicsStats(accountName, containerName);
                DbHelper.WriteProteomicsStatsToTable(stats, logId.Value);
                DbHelper.LogFinish(logId.Value, 2);
            }
            catch (Exception e)
            {
                if (logId.HasValue)
                {
                    DbHelper.LogFinish(logId.Value, 3);
                }
                Console.WriteLine(e.Message);
                throw;
            }
        }

        private static void CollectNgsSamplesStats()
        {
            int? logId = null;
            try
            {
                var accountName = Environment.GetEnvironmentVariable("NGS_ACCOUNT");
                var containerName = Environment.GetEnvironmentVariable("NGS_SAMPLES_CONTAINER");
                logId = DbHelper.LogStart("NGS_SAMPLES");
                var stats = BlobStorageHelper.GetNgsSampleStats(accountName, containerName);
                DbHelper.WriteNgsSampleStatsToTable(stats, logId.Value);
                DbHelper.LogFinish(logId.Value, 2);
            }
            catch (Exception e)
            {
                if (logId.HasValue)
                {
                    DbHelper.LogFinish(logId.Value, 3);
                }
                Console.WriteLine(e.Message);
                throw;
            }
        }

        private static void CollectNgsRunStats(string containerName, string prefix)
        {
            int? logId = null;
            try
            {
                var accountName = Environment.GetEnvironmentVariable("NGS_ACCOUNT");
                logId = DbHelper.LogStart("NGS_RUNS_" + containerName.ToUpper());
                var stats = BlobStorageHelper.GetNgsRunStats(accountName, containerName, prefix);
                stats.SeqMachine = containerName;
                DbHelper.WriteNgsRunStatsToTable(stats, logId.Value);
                DbHelper.LogFinish(logId.Value, 2);
            }
            catch (Exception e)
            {
                if (logId.HasValue)
                {
                    DbHelper.LogFinish(logId.Value, 3);
                }
                Console.WriteLine(e.Message);
                throw;
            }
        }

        /// <summary>
        /// Writes the specified message to console prepending a timestamp if in debug mode.
        /// </summary>
        /// <param name="message">The message to write.</param>
        private static void WriteOut(string message)
        {
#if DEBUG
            Console.WriteLine("{0}: {1}", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff"), message);
#else
            Console.WriteLine(message);
#endif
        }

        static void Main(string[] args)
        {

            // NB: This is needed to use the AsDataSet extension for ExcelDataReader
            // See https://github.com/ExcelDataReader/ExcelDataReader
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            WriteOut("Start collecting stats...");

            WriteOut("Collecting proteomics stats...");
            CollectProteomicsStats();
            WriteOut("Finished collecting proteomics stats");

            WriteOut("Collecting NGS samples stats...");
            CollectNgsSamplesStats();
            WriteOut("Finished collecting NGS samples stats");

            var nextSeqContainerName = Environment.GetEnvironmentVariable("NGS_NEXTSEQ_CONTAINER");
            var miSeqContainerName = Environment.GetEnvironmentVariable("NGS_MISEQ_CONTAINER");

            WriteOut("Collecting NGS run stats from NextSeqOutput...");
            CollectNgsRunStats(nextSeqContainerName, "NextSeqOutput/");
            WriteOut("Finished collecting NGS run stats from NextSeqOutput");

            WriteOut("Collecting NGS run stats from MiSeqOutput...");
            CollectNgsRunStats(miSeqContainerName, "MiSeqOutput/");
            WriteOut("Finished collecting NGS run stats from MiSeqOutput");

            WriteOut("Finisted collecting stats");
        }
    }
}
