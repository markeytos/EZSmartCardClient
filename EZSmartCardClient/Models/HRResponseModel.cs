using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace EZSmartCardClient.Models;

public class HRResponseModel
{
    public HRResponseModel()
    {

    }

    public HRResponseModel(int currentPage)
    {
        CurrentPage = currentPage;
    }

    [JsonPropertyName("HREntries")]
    public List<HRUser> HREntries { get; set; } = new();
    [JsonPropertyName("NextPage")]
    public int NextPage { get; set; } = -1;
    [JsonPropertyName("CurrentPage")]
    public int CurrentPage { get; set; } = -1;
}