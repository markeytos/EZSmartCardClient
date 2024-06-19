using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure.Core;
using EZSmartCardClient.Models;
using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace SampleSharedServices.Services;

public interface IGraphService
{
    Task<List<HRUser>> GetGroupMembersAsync(string groupID);
    Task<List<HRUser>> AllAADUsersAsync();
}

public class GraphService : IGraphService
{
    private readonly GraphServiceClient _graphService;

    public GraphService(TokenCredential tokenCredential, string authorityURL)
    {
        string graphEndpoint = "https://graph.microsoft.com/";
        if(authorityURL.Contains("microsoftonline.us"))
        {
            graphEndpoint = "https://graph.microsoft.us/";
        }
        _graphService = new GraphServiceClient(
            tokenCredential,
            new string[] { $"{graphEndpoint}.default"},
            graphEndpoint + "v1.0"
        );
    }

    public async Task<List<HRUser>> GetGroupMembersAsync(string groupID)
    {
        if (string.IsNullOrWhiteSpace(groupID))
        {
            throw new ArgumentNullException(nameof(groupID));
        }
        DirectoryObjectCollectionResponse? groupMembers = await _graphService
            .Groups[groupID]
            .TransitiveMembers.GetAsync();
        if (groupMembers == null)
        {
            throw new ApplicationException(
                "The group was not found, please verify the group ID and ensure this account has directory read access"
            );
        }
        List<DirectoryObject> members = groupMembers.Value ?? new();
        var pageIterator = PageIterator<
            DirectoryObject,
            DirectoryObjectCollectionResponse
        >.CreatePageIterator(
            _graphService,
            groupMembers,
            (user) =>
            {
                members.Add(user);
                return true;
            }
        );
        await pageIterator.IterateAsync();
        List<HRUser> users = new();
        foreach (
            var user in members.Where(m => m.OdataType == "#microsoft.graph.user").Cast<User>()
        )
        {
            if (
                !string.IsNullOrWhiteSpace(user.GivenName)
                && !string.IsNullOrWhiteSpace(user.Surname)
                && !string.IsNullOrWhiteSpace(user.UserPrincipalName)
            )
            {
                string manager = await GetManagersEmailAsync(user);
                users.Add(
                    new HRUser(user.GivenName, user.Surname, user.UserPrincipalName, manager)
                );
            }
            else
            {
                Console.WriteLine(
                    $"Skipping user {user.UserPrincipalName} {user.GivenName} {user.Surname} due to incomplete information"
                );
            }
        }
        return users;
    }

    public async Task<List<HRUser>> AllAADUsersAsync()
    {
        UserCollectionResponse? userCollection = await _graphService.Users.GetAsync(
            requestConfiguration =>
            {
                //requestConfiguration.QueryParameters.Top = 1;
            }
        );
        if (userCollection == null)
        {
            throw new ApplicationException(
                "No users found in Azure, please ensure this account has directory read access"
            );
        }
        List<User> users = userCollection.Value ?? new();
        var pageIterator = PageIterator<User, UserCollectionResponse>.CreatePageIterator(
            _graphService,
            userCollection,
            (user) =>
            {
                users.Add(user);
                return true;
            }
        );
        await pageIterator.IterateAsync();
        List<HRUser> hrUsers = new();
        foreach (var user in users)
        {
            if (
                !string.IsNullOrWhiteSpace(user.GivenName)
                && !string.IsNullOrWhiteSpace(user.Surname)
                && !string.IsNullOrWhiteSpace(user.UserPrincipalName)
            )
            {
                string manager = await GetManagersEmailAsync(user);
                hrUsers.Add(
                    new HRUser(user.GivenName, user.Surname, user.UserPrincipalName, manager)
                );
            }
            else
            {
                Console.WriteLine(
                    $"Skipping user {user.UserPrincipalName} {user.GivenName} {user.Surname} due to incomplete information"
                );
            }
        }
        return hrUsers;
    }

    private async Task<string> GetManagersEmailAsync(User user)
    {
        try
        {
            User? manager = (User?)(await _graphService.Users[user.Id].Manager.GetAsync());
            if (manager != null && !string.IsNullOrWhiteSpace(manager.UserPrincipalName))
            {
                return manager.UserPrincipalName;
            }
        }
        catch (Microsoft.Graph.Models.ODataErrors.ODataError)
        {
            Console.WriteLine("No manager found for user " + user.UserPrincipalName);
        }
        catch (Exception ex)
        {
            if (
                ex.Message
                != "Exception of type 'Microsoft.Graph.Models.ODataErrors.ODataError' was thrown."
                || ex.Message != "Resource 'manager' does not exist or one of its queried reference-property objects are not present."
            )
            {
                throw;
            }
        }
        return user.UserPrincipalName ?? "";
    }
}
