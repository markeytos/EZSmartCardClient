using System.Text.Json.Serialization;

namespace EZSmartCardClient.Models;

public class SCAssignModel
{
    public SCAssignModel() { }

    public SCAssignModel(
        DBSCRequestModel request,
        string serialNumber,
        string smartCardCertificate,
        string trackingNumber
    )
    {
        RequestID = request.RequestID;
        SerialNumber = serialNumber;
        SmartCardEnabled = true;
        SmartCardCertificate = smartCardCertificate;
        TrackingNumber = trackingNumber;
    }

    [JsonPropertyName("RequestID")]
    public string RequestID { get; set; } = string.Empty;

    [JsonPropertyName("SerialNumber")]
    public string SerialNumber { get; set; } = string.Empty;

    [JsonPropertyName("SmartCardEnabled")]
    public bool SmartCardEnabled { get; set; } = true;

    [JsonPropertyName("SmartCardCertificate")]
    public string SmartCardCertificate { get; set; } = string.Empty;

    [JsonPropertyName("TrackingNumber")]
    public string? TrackingNumber { get; set; } = string.Empty;
}
