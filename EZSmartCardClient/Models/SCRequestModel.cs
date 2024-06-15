using System.Text.Json.Serialization;

namespace EZSmartCardClient.Models;

public class SCRequestModel
{
    public SCRequestModel() { }

    [JsonPropertyName("BehalfSomeone")]
    public bool BehalfSomeone { get; set; } = false;

    [JsonPropertyName("SomeOnesEmail")]
    public string? SomeOnesEmail { get; set; }

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
}
