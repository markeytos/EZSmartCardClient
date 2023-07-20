using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace EZSmartCardClient.Models;

public class SmartcardDetailsModel
{
    [JsonPropertyName("RequestID")]
    public string RequestID { get; set; } = Guid.NewGuid().ToString();
    [JsonPropertyName("SCType")]
    public string SCType { get; set; } = string.Empty;
    [JsonPropertyName("Requester")]
    public string Requester { get; set; } = string.Empty;
    [JsonPropertyName("Approver")]
    public string Approver { get; set; } = string.Empty;
    [JsonPropertyName("Approved")]
    public bool? Approved { get; set; }
    [JsonPropertyName("SerialNumber")]
    public string? SerialNumber { get; set; }
    [JsonPropertyName("DateRequested")]
    public DateTime DateRequested { get; set; } = DateTime.UtcNow;
    [JsonPropertyName("DateApproved")]
    public DateTime? DateApproved { get; set; }
    [JsonPropertyName("TrackingNumber")]
    public string? TrackingNumber { get; set; }
    [JsonPropertyName("CurrentCertUPN")]
    public string? CurrentCertUPN { get; set; }
    [JsonPropertyName("CurrentFIDOUPN")]
    public string? CurrentFIDOUPN { get; set; }
    [JsonPropertyName("CurrentCertExpiry")]
    public DateTime? CurrentCertExpiry { get; set; }
    [JsonPropertyName("Deleted")]
    public bool? Deleted { get; set; }
}