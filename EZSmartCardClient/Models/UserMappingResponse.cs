using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace EZSmartCardClient.Models
{
    public class UserMappingResponse
    {
        [JsonPropertyName("SuccessfulEntries")]
        public List<UserMappingModel> SuccessfulEntries { get; set; } = new();
        [JsonPropertyName("FailedMapping")]
        public List<FailedUserMappingModel> FailedMapping { get; set; } = new();
        [JsonPropertyName("UpdatedEntries")]
        public List<UserMappingModel> UpdatedEntries { get; set; } = new();
    }

    public class FailedUserMappingModel
    {
        public FailedUserMappingModel()
        {

        }

        public FailedUserMappingModel(UserMappingModel dBUser, string reason)
        {
            UserMappingObj = dBUser;
            Reason = reason;
        }

        [JsonPropertyName("UserMappingObj")]
        UserMappingModel UserMappingObj { get; set; } = new();
        [JsonPropertyName("Reason")]
        string Reason { get; set; } = string.Empty;
    }
}
