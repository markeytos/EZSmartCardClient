using System.Text;
using Azure.Identity;
using EZSmartCardClient.Managers;
using EZSmartCardClient.Models;
using ManualBulkOperations.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SampleSharedServices.Services;

Console.WriteLine("Welcome to the EZCMS HR Sample");
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
ILogger logger = CreateLogger(appInsightsConnectionString);
var cliAuthOptions = new AzureCliCredentialOptions { AuthorityHost = new Uri(adInstance) };
IEZSmartCardManager ezCMSManager = new EZSmartCardManager(
    new(),
    instanceURL,
    logger,
    new AzureCliCredential(cliAuthOptions),
    tokenScopes
);
Console.WriteLine("Select Run Action (enter number only):");
Console.WriteLine("1. Update Users Manager");
Console.WriteLine("2. Bulk Assign SmartCards");
string? action;
do
{
    action = Console.ReadLine();
} while (!string.IsNullOrWhiteSpace(action) && action != "1" && action != "2");

if (action == "1")
{
    //Update Users Manager
    Console.WriteLine("Enter Manager Email");
    string? managerEmail = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(managerEmail))
    {
        Console.WriteLine("Invalid Manager Email");
        return;
    }
    List<HRUser> users = await ezCMSManager.GetExistingUsersAsync(true);
    Console.WriteLine($"Found {users.Count} active users");
    foreach (HRUser user in users)
    {
        user.ManagerEmail = managerEmail;
    }
    HRAddResponseModel UpdateResponse = await ezCMSManager.AddUsersAsync(users);
    if (UpdateResponse.Updated.Count == users.Count)
    {
        Console.WriteLine("Users Updated Successfully");
    }
    else
    {
        Console.WriteLine("Error Updating Users");
        foreach (var user in UpdateResponse.Invalid)
        {
            Console.WriteLine($"Invalid user {user.Email} Manager {user.ManagerEmail}");
        }
    }
}
else if (action == "2")
{
    Console.WriteLine("Please Enter the CSV File Path");
    string? filePath = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(filePath))
    {
        Console.WriteLine("Invalid File Path");
        return;
    }

    List<BulkSmartCardCSVModel> smartCards = FileService.GetCSVFile<BulkSmartCardCSVModel>(
        filePath
    );
    Console.WriteLine($"Found {smartCards.Count} SmartCards");
    await RegisterSmartCardsInInventoryAsync(smartCards);
    await AssignSmartCardsToUsersAsync(
        smartCards
            .Where(i =>
                i.FailedRequest != true
                && i.FailedAssignment != false
                && !string.IsNullOrWhiteSpace(i.SerialNumber)
            )
            .ToList()
    );
    string outputPath = filePath.Replace(".csv", "_output.csv");
    FileService.SaveFile(
        outputPath,
        new UTF8Encoding(true).GetBytes(FileService.GetCSVString(smartCards))
    );
    Console.WriteLine($"Output File Saved to {outputPath}");
}
else
{
    Console.WriteLine("Invalid Action");
}

async Task AssignSmartCardsToUsersAsync(List<BulkSmartCardCSVModel> smartCards)
{
    List<DBSCRequestModel> pendingRequests = await ezCMSManager.GetPendingSmartCardRequestsAsync();
    Console.WriteLine($"Found {pendingRequests.Count} Pending Requests");
    foreach (DBSCRequestModel request in pendingRequests)
    {
        BulkSmartCardCSVModel? smartCard = smartCards.FirstOrDefault(i =>
            i.UserEmail == request.SCAssignedTo
        );
        if (smartCard != null)
        {
            SCAssignModel assignModel =
                new(
                    request,
                    smartCard.SerialNumber,
                    smartCard.AttestationCertificate,
                    smartCard.TrackingNumber
                );
            APIResultModel result = await ezCMSManager.AssignUserSmartCardAsync(assignModel);
            if (result.Success)
            {
                Console.WriteLine($"SmartCard Assigned to {smartCard.UserEmail}");
                smartCard.FailedAssignment = false;
            }
            else
            {
                Console.WriteLine(
                    $"Error Assigning SmartCard to {smartCard.UserEmail} " + result.Message
                );
                smartCard.FailedAssignment = true;
            }
        }
    }
}

async Task RegisterSmartCardsInInventoryAsync(List<BulkSmartCardCSVModel> smartCards)
{
    foreach (BulkSmartCardCSVModel smartCard in smartCards.Where(i => i.FailedRequest != false))
    {
        SCRequestModel smartCardRequest =
            new()
            {
                BehalfSomeone = true,
                SomeOnesEmail = smartCard.UserEmail,
                Address = smartCard.Address,
                City = smartCard.City,
                State = smartCard.State,
                Country = smartCard.Country,
                ZipCode = smartCard.ZipCode,
                ReasonForRequest = smartCard.ReasonForRequest,
                SCType = smartCard.SCType
            };
        APIResultModel result = await ezCMSManager.CreateUserSmartCardRequestAsync(
            smartCardRequest
        );
        if (result.Success)
        {
            Console.WriteLine($"SmartCard Request Created for {smartCard.UserEmail}");
            smartCard.FailedRequest = false;
        }
        else
        {
            Console.WriteLine(
                $"Error Creating SmartCard Request for {smartCard.UserEmail} " + result.Message
            );
            smartCard.FailedRequest = true;
        }
    }
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
            Console.WriteLine($"Deleted {userToDelete.Email} from EZCMS");
        }
        else
        {
            Console.WriteLine($"Error deleting {userToDelete.Email} from EZCMS " + result.Message);
        }
    }
    HRAddResponseModel UpdateResponse = await ezCMSManager.AddUsersAsync(toUpdate);
    foreach (var user in UpdateResponse.Updated)
    {
        Console.WriteLine($"Updated {user.Email} ");
    }
    HRAddResponseModel addResponse = await ezCMSManager.AddUsersAsync(toAdd);
    foreach (var user in addResponse.Updated)
    {
        Console.WriteLine($"Added {user.Email} ");
    }
}

async Task<APIResultModel> DeleteUserAsync(string email)
{
    try
    {
        //Get Active SmartCards
        List<SmartcardDetailsModel> userSmartCards = await ezCMSManager.AdminGetUserSmartCardsAsync(
            email
        );
        foreach (var smartCard in userSmartCards)
        {
            //Delete SmartCard, Revoke Certificates and remove from inventory
            //change the boolean to false if you want to reuse that smart card
            if (!string.IsNullOrWhiteSpace(smartCard.SerialNumber))
            {
                await ezCMSManager.AdminDeleteSmartCardAsync(smartCard.RequestID, true);
            }
        }
        APIResultModel result = await ezCMSManager.DeleteUsersAsync([email]);
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
