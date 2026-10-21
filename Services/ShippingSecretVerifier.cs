using Azure.Security.KeyVault.Secrets;

namespace AzureOmsDemo.Services
{
    public sealed class ShippingSecretVerifier
    {
        private readonly SecretClient _secretClient;

        public ShippingSecretVerifier(SecretClient secretClient)
        {
            _secretClient = secretClient;
        }

        public async Task<bool> IsConfiguredAsync(
            CancellationToken cancellationToken = default)
        {
            var response = await _secretClient.GetSecretAsync(
                "ShippingApiKey",
                cancellationToken: cancellationToken);

            return !string.IsNullOrWhiteSpace(
                response.Value.Value);
        }
    }
}
