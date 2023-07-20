using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace EZSmartCardClient.Models;

public class HRUser
{

    public HRUser()
    {
    }

    public HRUser(string firstName, string lastName, string email,
        string managerEmail)
    {
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        Alias = email.Split("@")[0];
        Active = true;
        ManagerEmail = managerEmail;
    }

    public override bool Equals(object? obj)
    {
        if (obj is HRUser user)
        {
            return Email == user.Email
                   && FirstName == user.FirstName
                   && MiddleName == user.MiddleName
                   && LastName == user.LastName
                   && Email == user.Email
                   && Alias == user.Alias
                   && ManagerEmail == user.ManagerEmail
                   && Clearances == user.Clearances
                   && CostCenter == user.CostCenter
                   && Active == user.Active
                   && DocumentNumber == user.DocumentNumber
                   && DOB == user.DOB
                   && Country == user.Country;
        }
        return false;
    }

    public override int GetHashCode()
    {
        // ReSharper disable once NonReadonlyMemberInGetHashCode
        return Email.GetHashCode();
    }

    [JsonPropertyName("FirstName")]
    public string FirstName { get; set; } = string.Empty;
    [JsonPropertyName("MiddleName")]
    public string MiddleName { get; set; } = string.Empty;
    [JsonPropertyName("LastName")]
    public string LastName { get; set; } = string.Empty;
    [JsonPropertyName("Email")]
    public string Email { get; set; } = string.Empty;
    [JsonPropertyName("Alias")]
    public string Alias { get; set; } = string.Empty;
    [JsonPropertyName("ManagerEmail")]
    public string ManagerEmail { get; set; } = string.Empty;
    [JsonPropertyName("Clearances")]
    public string Clearances { get; set; } = string.Empty;
    [JsonPropertyName("CostCenter")]
    public string CostCenter { get; set; } = string.Empty;
    [JsonPropertyName("Active")]
    public bool Active { get; set; }
    [JsonPropertyName("Deleted")]
    public bool Deleted { get; set; } = false;
    [JsonPropertyName("LastUpdated")]
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    [JsonPropertyName("DeleteTime")]
    public DateTime? DeleteTime { get; set; }
    [JsonPropertyName("RemovedFromDomains")]
    public bool RemovedFromDomains { get; set; } = false;
    [JsonPropertyName("DocumentNumber")]
    public string DocumentNumber { get; set; } = string.Empty; //optional for ID verification
    [JsonPropertyName("DOB")]
    public string DOB { get; set; } = string.Empty; //optional for ID verification
    [JsonPropertyName("Country")]
    public string Country { get; set; } = string.Empty; //optional for ID verification
}