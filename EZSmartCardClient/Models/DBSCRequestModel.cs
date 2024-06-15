using System.Text.Json.Serialization;

namespace EZSmartCardClient.Models;

public class DBSCRequestModel
{
    [JsonPropertyName("RequestID")]
    public string RequestID { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("BehalfSomeone")]
    public bool BehalfSomeone { get; set; } = false;

    [JsonPropertyName("SCAssignedTo")]
    public string SCAssignedTo { get; set; } = "";

    [JsonPropertyName("FullName")]
    public string FullName { get; set; } = "";

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

    [JsonPropertyName("Requester")]
    public string Requester { get; set; } = "";

    [JsonPropertyName("Approver")]
    public string Approver { get; set; } = "";

    [JsonPropertyName("Approved")]
    public bool? Approved { get; set; }

    [JsonPropertyName("Deleted")]
    public bool? Deleted { get; set; }

    [JsonPropertyName("DeletedBy")]
    public string? DeletedBy { get; set; }

    [JsonPropertyName("DateDeleted")]
    public DateTime? DateDeleted { get; set; }

    [JsonPropertyName("AssignedBy")]
    public string? AssignedBy { get; set; }

    [JsonPropertyName("SerialNumber")]
    public string? SerialNumber { get; set; }

    [JsonPropertyName("AttestationCert")]
    public string? AttestationCert { get; set; }

    [JsonPropertyName("DateRequested")]
    public DateTime DateRequested { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("DateApproved")]
    public DateTime? DateApproved { get; set; }

    [JsonPropertyName("TrackingNumber")]
    public string? TrackingNumber { get; set; }

    [JsonPropertyName("CostCenter")]
    public string? CostCenter { get; set; }
}
