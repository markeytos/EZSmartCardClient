using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace EZSmartCardClient.Models
{
    public class UserMappingModel
    {
        [JsonPropertyName("DomainID")]
        public string DomainID { get; set; } = string.Empty;

        [JsonPropertyName("Alias")]
        public string Alias { get; set; } = string.Empty;

        [JsonPropertyName("NewAlias")]
        public string NewAlias { get; set; } = string.Empty;
    }
}
