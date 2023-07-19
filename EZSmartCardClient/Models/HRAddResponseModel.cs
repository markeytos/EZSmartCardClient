using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace EZSmartCardClient.Models;

public class HRAddResponseModel
{
    [JsonPropertyName("Added")]
    public List<HRUser> Added { get; set; } = new();
    [JsonPropertyName("Updated")]
    public List<HRUser> Updated { get; set; } = new();
    [JsonPropertyName("Invalid")]
    public List<HRUser> Invalid { get; set; } = new();
}