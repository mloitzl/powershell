using System;
using System.Text.Json.Serialization;

namespace PnP.PowerShell.Commands.Model.AzureAD
{
    /// <summary>
    /// Definition of a Microsoft 365 Group Endpoint such as Yammer or Teams
    /// </summary>
    public class AzureADGroupEndPoint
    {
        [JsonPropertyName("id")]
        public Guid? Id { get; set; }

        [JsonPropertyName("deletedDateTime")]
        public DateTime? DeletedDateTime { get; set; }

        [JsonPropertyName("capability")]
        public string Capability { get; set; }

        [JsonPropertyName("providerId")]
        public string ProviderId { get; set; }

        [JsonPropertyName("providerName")]
        public string ProviderName { get; set; }

        [JsonPropertyName("uri")]
        public string Uri { get; set; }

        [JsonPropertyName("providerResourceId")]
        public string ProviderResourceId { get; set; }
    }
}