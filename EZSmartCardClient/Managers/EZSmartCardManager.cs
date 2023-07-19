using Azure.Core;
using EZSmartCardClient.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using EZSmartCardClient.Models;
using Azure.Identity;
using Azure;

namespace EZSmartCardClient.Managers;

public interface IEZSmartCardManager
{
    /// <summary>
    /// Get the existing users from the EZSmartCard HR Database
    /// </summary>
    /// <param name="activeOnly"> bool to indicate to return only the active employees </param>
    /// <returns>List of <see cref="HRUser"/> </returns>
    /// <exception cref="HttpRequestException">Error contacting server</exception>
    Task<List<HRUser>> GetExistingUsersAsync(bool activeOnly);
    /// <summary>
    /// Add or update the users in the EZSmartCard HR Database
    /// </summary>
    /// <param name="users"> List of <see cref="HRUser"/> Users you want to add or edit </param>
    /// <returns> <see cref="HRAddResponseModel"/> containing the lists of added, updated, and invalid users </returns>
    /// <exception cref="HttpRequestException">Error contacting server</exception>
    Task<HRAddResponseModel> AddUsersAsync(List<HRUser> users);
    /// <summary>
    /// Delete users in the EZSmartCard HR Database
    /// </summary>
    /// <param name="emails"> List of emails of the employees you would like to delete from the database</param>
    /// <returns> <see cref="APIResultModel"/> The result of the delete operation</returns>
    /// <exception cref="HttpRequestException">Error contacting server</exception>
    Task<APIResultModel> DeleteUsersAsync(List<string> emails);
}

public class EZSmartCardManager : IEZSmartCardManager
{
    private readonly HttpClientService _httpClient;
    private readonly string _url;
    private AccessToken _token;
    private readonly TokenCredential? _azureTokenCredential;

    public EZSmartCardManager(HttpClient httpClient, string baseUrl,
        ILogger? logger = null,
        TokenCredential? azureTokenCredential = null)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new ArgumentNullException(nameof(baseUrl));
        }
        if (httpClient == null)
        {
            throw new ArgumentNullException(nameof(httpClient));
        }
        _azureTokenCredential = azureTokenCredential;
        _httpClient = new(httpClient, logger);
        _url = baseUrl.TrimEnd('/').Replace("http://", "https://");
    }

    public async Task<List<HRUser>> GetExistingUsersAsync(bool activeOnly)
    {
        int currentPage = 0;
        List<HRUser> users = new();
        await GetTokenAsync();
        do
        {
            APIResultModel response = await _httpClient.CallGenericAsync(
                _url + "/api/HR/GetHRUsers?pageNumber=" + currentPage,
                null, _token.Token, HttpMethod.Get);
            if (response.Success)
            {
                return users;
                HRResponseModel? hrResponse = JsonSerializer.Deserialize<HRResponseModel>(response.Message);
                if (hrResponse == null)
                {
                    currentPage = -1;
                }
                else
                {
                    users.AddRange(activeOnly ? hrResponse.HREntries :
                        hrResponse.HREntries.Where(x => x.Active));
                    currentPage = hrResponse.NextPage;
                }
            }
            else
            {
                throw new HttpRequestException("Error getting users " + response.Message);
            }
        } while (currentPage > 0);
    }

    public async Task<HRAddResponseModel> AddUsersAsync(List<HRUser> users)
    {
        if (users == null || users.Any() == false)
        {
            throw new ArgumentNullException(nameof(users));
        }
        await GetTokenAsync();
        APIResultModel result = await _httpClient.CallGenericAsync(_url + "/api/HR/AddHRUsers",
                       JsonSerializer.Serialize(users), _token.Token, HttpMethod.Post);
        HRAddResponseModel response = new();
        if (result.Success)
        {
            result = JsonSerializer.Deserialize<APIResultModel>(result.Message) ??
                     new(false, "Error contacting server, please try again");
            if (result.Success)
            {
                response = JsonSerializer.Deserialize<HRAddResponseModel>(result.Message) ??
                                              new();
            }
        }
        if(result.Success == false)
        {
            throw new HttpRequestException("Error getting users " + result.Message);
        }
        return response;
    }

    public async Task<APIResultModel> DeleteUsersAsync(List<string> emails)
    {
        if (emails == null || emails.Any() == false)
        {
            throw new ArgumentNullException(nameof(emails));
        }
        await GetTokenAsync();
        APIResultModel result = await _httpClient.CallGenericAsync(_url + "/api/HR/DeleteHRUsers",
            JsonSerializer.Serialize(emails), _token.Token, HttpMethod.Post);
        if (result.Success)
        {
            result = JsonSerializer.Deserialize<APIResultModel>(result.Message) ??
                     new(false, "Error contacting server, please try again");
        }
        return result;
    }

    private async Task GetTokenAsync()
    {
        TokenRequestContext authContext = new(
            new[] { "https://management.core.windows.net/.default" });
        if (_azureTokenCredential == null)
        {
            throw new ArgumentNullException(nameof(_azureTokenCredential));
        }
        _token = await _azureTokenCredential.GetTokenAsync(authContext, default);
        if (string.IsNullOrWhiteSpace(_token.Token))
        {
            throw new AuthenticationFailedException("Error getting token");
        }
    }


}