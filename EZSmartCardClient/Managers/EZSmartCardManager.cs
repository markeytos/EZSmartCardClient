using System.Text.Json;
using Azure.Core;
using Azure.Identity;
using EZSmartCardClient.Models;
using EZSmartCardClient.Services;
using Microsoft.Extensions.Logging;

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

    /// <summary>
    /// Delete or un assign the smart card. To use this function it has to be a smart card administrator account
    /// </summary>
    /// <param name="requestID"> The requestID associated with that smart card </param>
    /// <param name="delete"> true if you want to delete the smart card from inventory, false if you only want to un assign</param>
    /// <returns> <see cref="Task"/> </returns>
    /// <exception cref="HttpRequestException">Error contacting server</exception>
    Task AdminDeleteSmartCardAsync(string requestID, bool delete);

    /// <summary>
    /// Get user's assigned smart cards. To use this function it has to be a smart card administrator account
    /// </summary>
    /// <param name="email"> User email you want to get the smart cards for </param>
    /// <returns> <see cref="SmartcardDetailsModel"/> List of smart cards</returns>
    /// <exception cref="HttpRequestException">Error contacting server</exception>
    Task<List<SmartcardDetailsModel>> AdminGetUserSmartCardsAsync(string email);

    /// <summary>
    /// Create user mappings for domains where aliases are not the same as the email address
    /// </summary>
    /// <param name="userMappings"> List of User Mappings to add </param>
    /// <returns> <see cref="UserMappingResponse"/> </returns>
    /// <exception cref="HttpRequestException">Error contacting server</exception>
    Task<UserMappingResponse> CreateUserMappingsAsync(List<UserMappingModel> userMappings);

    /// <summary>
    /// Deletes user mappings for domains where aliases are not the same as the email address
    /// </summary>
    /// <param name="userMappings"> List of User Mappings to add </param>
    /// <returns> <see cref="UserMappingResponse"/> </returns>
    /// <exception cref="HttpRequestException">Error contacting server</exception>
    Task<UserMappingResponse> DeleteUserMappingsAsync(List<UserMappingModel> userMappings);

    /// <summary>
    /// Creates a User Smart Card Request
    /// </summary>
    /// <param name="newSC"> SmartCardRequest enter 'office' for all address fields if you want to avoid address verification </param>
    /// <returns> <see cref="APIResultModel"/> </returns>
    Task<APIResultModel> CreateUserSmartCardRequestAsync(SCRequestModel newSC);

    /// <summary>
    /// Get all pending smart card requests
    /// </summary>
    /// <returns> <see cref="DBSCRequestModel"/> List of pending requests </returns>
    Task<List<DBSCRequestModel>> GetPendingSmartCardRequestsAsync();

    /// <summary>
    /// Assign a smart card to a user
    /// </summary>
    /// <param name="assignModel"> <see cref="SCAssignModel"/> </param>
    /// <returns><see cref="APIResultModel"/></returns>
    Task<APIResultModel> AssignUserSmartCardAsync(SCAssignModel assignModel);
}

public class EZSmartCardManager : IEZSmartCardManager
{
    private readonly HttpClientService _httpClient;
    private readonly string _url;
    private readonly string _scopes;
    private AccessToken _token;
    private readonly TokenCredential? _azureTokenCredential;

    public EZSmartCardManager(
        HttpClient httpClient,
        string baseUrl,
        ILogger? logger = null,
        TokenCredential? azureTokenCredential = null,
        string scopes = "d3c9fa4a-db26-4197-b7d2-be2129ab70ba/.default"
    )
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
        _scopes = scopes;
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
                null,
                _token.Token,
                HttpMethod.Get
            );
            if (response.Success)
            {
                HRResponseModel? hrResponse = JsonSerializer.Deserialize<HRResponseModel>(
                    response.Message
                );
                if (hrResponse == null)
                {
                    currentPage = -1;
                }
                else
                {
                    users.AddRange(
                        activeOnly
                            ? hrResponse.HREntries.Where(x => x.Active && x.Deleted == false)
                            : hrResponse.HREntries
                    );
                    currentPage = hrResponse.NextPage;
                }
            }
            else
            {
                throw new HttpRequestException("Error getting users " + response.Message);
            }
        } while (currentPage > 0);
        return users;
    }

    public async Task<HRAddResponseModel> AddUsersAsync(List<HRUser> users)
    {
        if (users == null || users.Any() == false)
        {
            throw new ArgumentNullException(nameof(users));
        }
        await GetTokenAsync();
        APIResultModel result = await _httpClient.CallGenericAsync(
            _url + "/api/HR/AddHRUsers",
            JsonSerializer.Serialize(users),
            _token.Token,
            HttpMethod.Post
        );
        HRAddResponseModel response = new();
        if (result.Success)
        {
            result =
                JsonSerializer.Deserialize<APIResultModel>(result.Message)
                ?? new(false, "Error contacting server, please try again");
            if (result.Success)
            {
                response = JsonSerializer.Deserialize<HRAddResponseModel>(result.Message) ?? new();
            }
        }
        if (result.Success == false)
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
        APIResultModel result = await _httpClient.CallGenericAsync(
            _url + "/api/HR/DeleteHRUsers",
            JsonSerializer.Serialize(emails),
            _token.Token,
            HttpMethod.Post
        );
        if (result.Success)
        {
            result =
                JsonSerializer.Deserialize<APIResultModel>(result.Message)
                ?? new(false, "Error contacting server, please try again");
        }
        return result;
    }

    public async Task AdminDeleteSmartCardAsync(string requestID, bool delete)
    {
        if (string.IsNullOrWhiteSpace(requestID))
        {
            throw new ArgumentNullException(nameof(requestID));
        }
        await GetTokenAsync();
        APIResultModel request = new(delete, requestID);
        APIResultModel result = await _httpClient.CallGenericAsync(
            _url + "/api/SmartCard/AdminRemoveSC",
            JsonSerializer.Serialize(request),
            _token.Token,
            HttpMethod.Post
        );
        if (result.Success)
        {
            result =
                JsonSerializer.Deserialize<APIResultModel>(result.Message)
                ?? new(false, "Error contacting server, please try again");
        }
        if (!result.Success)
        {
            throw new HttpRequestException("Error deleting smart card " + result.Message);
        }
    }

    public async Task<List<SmartcardDetailsModel>> AdminGetUserSmartCardsAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentNullException(nameof(email));
        }
        await GetTokenAsync();
        APIResultModel response = await _httpClient.CallGenericAsync(
            _url + "/api/SmartCard/AdminGetSmartCards?email=" + email,
            null,
            _token.Token,
            HttpMethod.Get
        );
        if (response.Success)
        {
            response =
                JsonSerializer.Deserialize<APIResultModel>(response.Message)
                ?? new(false, "Server internal error");
        }
        if (!response.Success)
        {
            throw new HttpRequestException("Error getting smart cards " + response.Message);
        }
        return JsonSerializer.Deserialize<List<SmartcardDetailsModel>>(response.Message) ?? new();
    }

    public async Task<UserMappingResponse> CreateUserMappingsAsync(
        List<UserMappingModel> userMappings
    )
    {
        if (userMappings == null || userMappings.Any() == false)
        {
            throw new ArgumentNullException(nameof(userMappings));
        }
        return await CallUserMappingAPIs(userMappings, "/api/HR/AddUserMapping");
    }

    public async Task<UserMappingResponse> DeleteUserMappingsAsync(
        List<UserMappingModel> userMappings
    )
    {
        if (userMappings == null || userMappings.Any() == false)
        {
            throw new ArgumentNullException(nameof(userMappings));
        }
        return await CallUserMappingAPIs(userMappings, "/api/HR/DeleteUserMapping");
    }

    public async Task<APIResultModel> CreateUserSmartCardRequestAsync(SCRequestModel newSC)
    {
        await GetTokenAsync();
        APIResultModel response = await _httpClient.CallGenericAsync(
            _url + "/api/SmartCard/RequestSmartCard",
            JsonSerializer.Serialize(newSC),
            _token.Token,
            HttpMethod.Post
        );
        if (response.Success)
        {
            response =
                JsonSerializer.Deserialize<APIResultModel>(response.Message)
                ?? new(false, "Server internal error");
        }
        return response;
    }

    public async Task<List<DBSCRequestModel>> GetPendingSmartCardRequestsAsync()
    {
        await GetTokenAsync();
        APIResultModel result = await _httpClient.CallGenericAsync(
            _url + "/api/SmartCard/GetPendingSCAssignments",
            null,
            _token.Token,
            HttpMethod.Get
        );
        if (result.Success)
        {
            result =
                JsonSerializer.Deserialize<APIResultModel>(result.Message)
                ?? new(false, "Error contacting server, please try again");
            if (result.Success)
            {
                return JsonSerializer.Deserialize<List<DBSCRequestModel>>(result.Message) ?? new();
            }
        }
        throw new HttpRequestException("Error getting pending requests " + result.Message);
    }

    public async Task<APIResultModel> AssignUserSmartCardAsync(SCAssignModel assignModel)
    {
        await GetTokenAsync();
        APIResultModel response = await _httpClient.CallGenericAsync(
            _url + "/api/SmartCard/AssignSmartCard",
            JsonSerializer.Serialize(assignModel),
            _token.Token,
            HttpMethod.Post
        );
        if (response.Success)
        {
            response =
                JsonSerializer.Deserialize<APIResultModel>(response.Message)
                ?? new(false, "Server internal error");
        }
        return response;
    }

    private async Task<UserMappingResponse> CallUserMappingAPIs(
        List<UserMappingModel> userMappings,
        string endpoint
    )
    {
        await GetTokenAsync();
        APIResultModel response = await _httpClient.CallGenericAsync(
            _url + endpoint,
            JsonSerializer.Serialize(userMappings),
            _token.Token,
            HttpMethod.Post
        );
        if (response.Success)
        {
            response =
                JsonSerializer.Deserialize<APIResultModel>(response.Message)
                ?? new(false, "Server internal error");
        }
        if (!response.Success)
        {
            throw new HttpRequestException("Error managing user mappings" + response.Message);
        }
        return JsonSerializer.Deserialize<UserMappingResponse>(response.Message) ?? new();
    }

    private async Task GetTokenAsync()
    {
        TokenRequestContext authContext = new([_scopes]);
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
