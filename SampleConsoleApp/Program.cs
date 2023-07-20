using Azure.Identity;
using EZSmartCardClient.Managers;
using EZSmartCardClient.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SampleSharedServices.Services;

Console.WriteLine("Welcome to the EZSmartCard HR Sample");
string groupObjectID = "";//enter a group ID if you want EZSmartCard to only add users from a specific group leave empty to add all the AAD Users
string? instanceURL = "";//replace with your EZSmartCard instance URL
string appInsightsConnectionString = "";

if (string.IsNullOrWhiteSpace(instanceURL))
{
    Console.WriteLine("Please enter the EZSmartCard Instance URL");
    instanceURL = Console.ReadLine();
}

if (string.IsNullOrWhiteSpace(instanceURL))
{
    Console.WriteLine("Invalid EZSmartCard Instance URL");
    return;
}
ILogger logger = CreateLogger(appInsightsConnectionString);
IEZSmartCardManager ezSmartCardManager = new EZSmartCardManager(new(),
    instanceURL, logger, new AzureCliCredential());
IGraphService graphService = new GraphService(new DefaultAzureCredential());


try
{
    Console.WriteLine("Getting EZSmartCard Active Users");
    List<HRUser> users = 
        await ezSmartCardManager.GetExistingUsersAsync(true);
    Console.WriteLine($"Found {users.Count} active users");
    List<HRUser> aadUsers = new();
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
    Console.WriteLine($"Found {aadUsers.Count} users in Azure AD");
    if (users.Count > 10 && aadUsers.Count < (users.Count * .95))
    {
        //A workforce reduction of 5% or more is a red flag that something is wrong stopping removal of users
        throw new ("The number of users in EZSmartCard is " +
                          "significantly higher than the number of users in Azure AD stopping update");
    }
    Console.WriteLine("Comparing EZSmartCard Users to Azure AD Users");
    await UpdateUserChangesAsync(aadUsers, users);
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
            Console.WriteLine($"Error deleting {userToDelete.Email} from EZSmartCard " + result.Message);
        }
    }
    HRAddResponseModel UpdateResponse = await ezSmartCardManager.AddUsersAsync(toUpdate);
    HRAddResponseModel addResponse = await ezSmartCardManager.AddUsersAsync(toAdd);
}

async Task<APIResultModel> DeleteUserAsync(string email)
{
    try
    {
        //Get Active SmartCards
        List<SmartcardDetailsModel> userSmartCards = await 
            ezSmartCardManager.AdminGetUserSmartCardsAsync(email);
        foreach (var smartCard in userSmartCards)
        {
            //Delete SmartCard, Revoke Certificates and remove from inventory
            //change the boolean to false if you want to reuse that smart card
            if (!string.IsNullOrWhiteSpace(smartCard.SerialNumber))
            {
                await ezSmartCardManager.AdminDeleteSmartCardAsync(smartCard.RequestID, true);
            }
        }
        APIResultModel result = await ezSmartCardManager.DeleteUsersAsync(
            new() { email });
        if (result.Success)
        {
            Console.WriteLine($"Deleted {email} from EZSmartCard");
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
                configureTelemetryConfiguration: (config) => config.ConnectionString =
                    appInsightsKey,
                configureApplicationInsightsLoggerOptions: (options) => { });
        }
#pragma warning disable CA1416
        builder.AddEventLog();
#pragma warning restore CA1416
    });
    IServiceProvider serviceProvider = services.BuildServiceProvider();
    return serviceProvider.GetRequiredService<ILogger<Program>>();
}