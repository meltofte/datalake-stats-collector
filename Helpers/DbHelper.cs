using datalake_stats.Model;
using System;
using System.Data;
using System.Data.SqlClient;

namespace datalake_stats.Helpers
{
    /// <summary>
    /// Helper class for database operations.
    /// </summary>
    internal static class DbHelper
    {
        /// <summary>
        /// Builds an returns a connection string.
        /// </summary>
        private static string GetConnectionString()
        {
            string serverName = Environment.GetEnvironmentVariable("DWH_SERVER_NAME");
            string databaseName = Environment.GetEnvironmentVariable("DWH_DB_NAME");
            string userName = Environment.GetEnvironmentVariable("DWH_USER_NAME");
            string pwd = Environment.GetEnvironmentVariable("DWH_PASSWORD");
            return new SqlConnectionStringBuilder()
            {
                DataSource = $"{serverName}.database.windows.net",
                UserID = userName,
                Password = pwd,
                InitialCatalog = databaseName,
            }.ConnectionString;
        }

        /// <summary>
        /// Returns an open SQL Server connection.
        /// </summary>
        private static SqlConnection GetSqlConnection()
        {
            string connectionString = GetConnectionString();
            var connection = new SqlConnection(connectionString);
            connection.Open();
            return connection;
        }

        /// <summary>
        /// Writes NGS run stats to database table.
        /// </summary>
        /// <param name="stats">An instance of NgsRunStats.</param>
        /// <param name="logId">Log id from log.data_lake_stats.</param>
        internal static void WriteNgsRunStatsToTable(NgsRunStats stats, int logId)
        {
            try
            {
                using var con = GetSqlConnection();
                using var cmd = new SqlCommand("data_lake.insert_ngs_run_stats", con)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@no_of_runs", stats.NumberOfRuns);
                cmd.Parameters.AddWithValue("@data_volume_bytes", stats.SizeInBytes);
                cmd.Parameters.AddWithValue("@seq_machine", stats.SeqMachine);
                cmd.Parameters.AddWithValue("@dw_log_id", logId);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Writes NGS sample stats to database table.
        /// </summary>
        /// <param name="stats">An instance of NgsSampleStats.</param>
        /// <param name="logId">Log id from log.data_lake_stats.</param>
        internal static void WriteNgsSampleStatsToTable(NgsSampleStats stats, int logId)
        {
            try
            {
                using var con = GetSqlConnection();
                using var cmd = new SqlCommand("data_lake.insert_ngs_sample_stats", con)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@no_of_samples", stats.NumberOfSamples);
                cmd.Parameters.AddWithValue("@data_volume_bytes", stats.SizeInBytes);
                cmd.Parameters.AddWithValue("@sample_names", stats.SampleNames);
                cmd.Parameters.AddWithValue("@dw_log_id", logId);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Writes proteomics stats to database table.
        /// </summary>
        /// <param name="stats">An instance of ProteomicsStats.</param>
        /// <param name="logId">Log id from log.data_lake_stats.</param>
        internal static void WriteProteomicsStatsToTable(ProteomicsStats stats, int logId)
        {
            try
            {
                using var con = GetSqlConnection();
                using var cmd = new SqlCommand("data_lake.insert_proteomics_stats", con)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@no_of_runs", stats.NumberOfRuns);
                cmd.Parameters.AddWithValue("@no_of_samples", stats.NumberOfSamples);
                cmd.Parameters.AddWithValue("@data_volume_bytes", stats.SizeInBytes);
                cmd.Parameters.AddWithValue("@request_names", stats.RequestNames);
                cmd.Parameters.AddWithValue("@dw_log_id", logId);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Logs the start of a statistics collection operation.
        /// </summary>
        /// <param name="statsType">A string representing the type of statistics collection.</param>
        /// <returns>The log id.</returns>
        internal static int LogStart(string statsType)
        {
            int logId = -1;
            try
            {
                using var con = GetSqlConnection();
                using var cmd = new SqlCommand("log.log_data_lake_collect_stats_start", con)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@stats_type", statsType);
                logId = (int)(cmd.ExecuteScalar());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
            return logId;
        }

        /// <summary>
        /// Marks the operation as finished by setting status of the specified log entry.
        /// </summary>
        /// <param name="logId">The log id.</param>
        /// <param name="status">Status: 2=finished, 3=failed.</param>
        internal static void LogFinish(int logId, int status)
        {
            try
            {
                using var con = GetSqlConnection();
                using var cmd = new SqlCommand("log.log_data_lake_collect_stats_finish", con)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@log_id", logId);
                cmd.Parameters.AddWithValue("@status", status);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }
    }
}
