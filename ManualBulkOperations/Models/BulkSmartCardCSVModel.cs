using System.Text.Json.Serialization;

namespace ManualBulkOperations.Models;

public class BulkSmartCardCSVModel
{
    [JsonPropertyName("UserEmail")]
    public string? UserEmail { get; set; }

    [JsonPropertyName("Address")]
    public string Address { get; set; } = "";

    [JsonPropertyName("City")]
    public string City { get; set; } = "";

    [JsonPropertyName("State")]
    public string State { get; set; } = "";

    [JsonPropertyName("Country")]
    public string Country { get; set; } = "";

    [JsonPropertyName("ZipCode")]
    public string ZipCode { get; set; } = "";

    [JsonPropertyName("ReasonForRequest")]
    public string ReasonForRequest { get; set; } = "";

    [JsonPropertyName("SCType")]
    public string SCType { get; set; } = "";

    [JsonPropertyName("SerialNumber")]
    public string SerialNumber { get; set; } = "";

    [JsonPropertyName("AttestationCertificate")]
    public string AttestationCertificate { get; set; } = "";

    [JsonPropertyName("TrackingNumber")]
    public string TrackingNumber { get; set; } = "";

    [JsonPropertyName("FailedRequest")]
    public bool? FailedRequest { get; set; }

    [JsonPropertyName("FailedAssignment")]
    public bool? FailedAssignment { get; set; }
}
