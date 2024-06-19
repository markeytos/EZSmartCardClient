using Azure.Identity;
using EZSmartCardClient.Managers;
using EZSmartCardClient.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SampleSharedServices.Services;

Console.WriteLine("Welcome to the EZCMS HR Sample");
string? groupObjectID = ""; //enter a group ID if you want EZSmartCard to only add users from a specific group leave empty to add all the AAD Users
string? instanceURL = ""; //replace with your EZSmartCard instance URL
string appInsightsConnectionString = "";

if (string.IsNullOrWhiteSpace(instanceURL))
{
    Console.WriteLine("Please enter the EZCMS Instance URL");
    instanceURL = Console.ReadLine();
}

if (string.IsNullOrWhiteSpace(instanceURL))
{
    Console.WriteLine("Invalid EZCMS Instance URL");
    return;
}
Console.WriteLine("Please Enter your AD Instance URL (leave blank for default)");
string? adInstance = Console.ReadLine();
if (string.IsNullOrWhiteSpace(adInstance))
{
    adInstance = "https://login.microsoftonline.com/";
}
Console.WriteLine("Please Enter your Token Scopes (leave blank for default)");
string? tokenScopes = Console.ReadLine();
if (string.IsNullOrWhiteSpace(tokenScopes))
{
    tokenScopes = "d3c9fa4a-db26-4197-b7d2-be2129ab70ba/.default";
}
if(string.IsNullOrWhiteSpace(groupObjectID))
{
    Console.WriteLine("Please Enter the Group Object ID (leave blank for all users)");
    groupObjectID = Console.ReadLine();
}
ILogger logger = CreateLogger(appInsightsConnectionString);
var cliAuthOptions = new AzureCliCredentialOptions { AuthorityHost = new Uri(adInstance) };
var graphAuthOptions = new DefaultAzureCredentialOptions  { AuthorityHost = new Uri(adInstance) };
IEZSmartCardManager ezSmartCardManager = new EZSmartCardManager(
    new(),
    instanceURL,
    logger,
    new AzureCliCredential(cliAuthOptions),
    tokenScopes
);
IGraphService graphService = new GraphService(new DefaultAzureCredential(graphAuthOptions), adInstance);
try
{
    Console.WriteLine("Getting EZCMS Active Users");
    List<HRUser> users = await ezSmartCardManager.GetExistingUsersAsync(true);
    Console.WriteLine($"Found {users.Count} active users");
    List<HRUser> aadUsers;
    if (!string.IsNullOrWhiteSpace(groupObjectID))
    {
        Console.WriteLine("Getting Users from Azure AD Group");
        aadUsers = await graphService.GetGroupMembersAsync(groupObjectID);
    }
    else
    {
        Console.WriteLine("Getting All Users from Azure AD");
        aadUsers = await graphService.AllAADUsersAsync();
    }
    aadUsers = aadUsers.DistinctBy(i => i.Email).ToList();
    Console.WriteLine($"Found {aadUsers.Count} users in Azure AD");
    if (users.Count > 10 && aadUsers.Count < (users.Count * .95))
    {
        //A workforce reduction of 5% or more is a red flag that something is wrong stopping removal of users
        Console.WriteLine(
            $"The number of users in EZCMS {users.Count} is significantly higher than the number of users in Entra ID {aadUsers.Count} Do you want to continue? (Y/N)"
        );
        string? decision = Console.ReadLine();
        if (decision?.ToLower().Contains("y") != true)
        {
            throw new(
                "The number of users in EZCMS is "
                + "significantly higher than the number of users in Azure AD stopping update"
            );
        }
    }
    Console.WriteLine("Comparing EZCMS Users to Azure AD Users");
    await UpdateUserChangesAsync(aadUsers, users);
    Console.WriteLine("User Update Complete");
}
catch (Exception e)
{
    logger.LogError(e, "Error updating users");
    Console.WriteLine(e);
}

async Task UpdateUserChangesAsync(List<HRUser> aadUsers, List<HRUser> users)
{
    Dictionary<string, HRUser> userDict = users.ToDictionary(u => u.Email);
    List<HRUser> toAdd = new();
    List<HRUser> toUpdate = new();
    // No need for a list for deletion, we can just modify the original list
    foreach (var aadUser in aadUsers)
    {
        if (userDict.TryGetValue(aadUser.Email, out HRUser? existingUser))
        {
            // If they are not equal, it's an update
            if (!existingUser.Equals(aadUser))
            {
                toUpdate.Add(aadUser);
            }

            // Remove from dictionary to leave only users to be deleted at the end
            userDict.Remove(aadUser.Email);
        }
        else
        {
            toAdd.Add(aadUser);
        }
    }
    foreach (var userToDelete in userDict.Values)
    {
        APIResultModel result = await DeleteUserAsync(userToDelete.Email);
        if (result.Success)
        {
            Console.WriteLine($"Deleted {userToDelete.Email} from EZSmartCard");
        }
        else
        {
            Console.WriteLine(
                $"Error deleting {userToDelete.Email} from EZSmartCard " + result.Message
            );
        }
    }

    if (toUpdate.Count > 0)
    {
        HRAddResponseModel UpdateResponse = await ezSmartCardManager.AddUsersAsync(toUpdate);
        
        foreach (var user in UpdateResponse.Updated)
        {
            Console.WriteLine($"Updated {user.Email} ");
        }
    }    
    if(toAdd.Count > 0)
    {
        HRAddResponseModel addResponse = await ezSmartCardManager.AddUsersAsync(toAdd);
        foreach (var user in addResponse.Updated)
        {
            Console.WriteLine($"Added {user.Email} ");
        }
    }
}

async Task<APIResultModel> DeleteUserAsync(string email)
{
    try
    {
        //Get Active SmartCards
        List<SmartcardDetailsModel> userSmartCards =
            await ezSmartCardManager.AdminGetUserSmartCardsAsync(email);
        foreach (var smartCard in userSmartCards)
        {
            //Delete SmartCard, Revoke Certificates and remove from inventory
            //change the boolean to false if you want to reuse that smart card
            if (!string.IsNullOrWhiteSpace(smartCard.SerialNumber))
            {
                await ezSmartCardManager.AdminDeleteSmartCardAsync(smartCard.RequestID, true);
            }
        }
        APIResultModel result = await ezSmartCardManager.DeleteUsersAsync([email]);
        if (result.Success)
        {
            Console.WriteLine($"Deleted {email} from EZCMS");
        }
        else
        {
            throw new Exception(result.Message);
        }
        return new(true);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error deleting user");
        return new(false, ex.Message);
    }
}

ILogger CreateLogger(string? appInsightsKey)
{
    IServiceCollection services = new ServiceCollection();
    services.AddLogging(builder =>
    {
        if (!string.IsNullOrWhiteSpace(appInsightsKey))
        {
            builder.AddApplicationInsights(
                configureTelemetryConfiguration: (config) =>
                    config.ConnectionString = appInsightsKey,
                configureApplicationInsightsLoggerOptions: (_) => { }
            );
        }
#if WINDOWS
        builder.AddEventLog();
#endif
    });
    IServiceProvider serviceProvider = services.BuildServiceProvider();
    return serviceProvider.GetRequiredService<ILogger<Program>>();
}
