using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Azure.Services.AppAuthentication;
using System;

namespace datalake_stats.Helpers
{
    /// <summary>
    /// Azure Key Vault helper.
    /// </summary>
    internal class KayVaultHelper
    {
        /// <summary>
        /// Returns a secret value from the specified key vault.
        /// It will authenticate using the credentials specified in the environment variables AZURE_TENANT_ID, AZURE_CLIENT_ID and AZURE_CLIENT_SECRET
        /// </summary>
        /// <param name="keyVaultName">Name of the key vault.</param>
        /// <param name="secretName">Secret name.</param>
        /// <returns></returns>
        internal static string GetSecret(string keyVaultName, string secretName)
        {
            var kvUri = $"https://{keyVaultName}.vault.azure.net";
            SecretClientOptions options = new SecretClientOptions()
            {
                Retry =
                {
                    Delay= TimeSpan.FromSeconds(2),
                    MaxDelay = TimeSpan.FromSeconds(16),
                    MaxRetries = 5,
                    Mode = RetryMode.Exponential
                 }
            };

            var credOptions = new DefaultAzureCredentialOptions()
            {
                ManagedIdentityClientId = Environment.GetEnvironmentVariable("AZURE_MANAGED_IDENTITY_CLIENT_ID"),
            };
            var client = new SecretClient(new Uri(kvUri), new DefaultAzureCredential(credOptions), options);
            var secret = client.GetSecret(secretName);
            return secret.Value.Value;
        }
    }
}
