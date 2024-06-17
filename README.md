# EZSmartCard Client
This Repo Contains the Nuget Package for EZSmartCard the first [passwordless authentication CMS for Azure](https://www.keytos.io/passwordless-onboarding.html)

## Getting Started
1) Add the Nuget Package to your project `dotnet add package EZSmartCardClient --version 1.0.0`
2) Use the SampleConsoleApp as a starting point and replace the values `groupObjectID`, `appInsightsConnectionString` and `instanceURL` with your own values.

## Using Manual Bulk Operations
This is a sample to show how to use the bulk operations in the EZSmartCardClient
Run the code in the `ManualBulkOperations` ([SignedVersion](https://download.keytos.io/Downloads/EZCMSTools/ManualBulkOperations.exe)).
1) It will then ask for your EZCMS instance enter your instance URL.
1) It will then ask for your Entra ID Instance URL (Leave empty if using public cloud) enter "https://login.microsoftonline.us/". For Gov Cloud.
1) It will then ask for your Token Scopes, Leave empty if using regular EZCMS instance, Enter your "API SCOPE" If using private infrastructure.
1) Then Select "2" to Add Bulk SmartCard Requests from a CSV file.
1) It will then ask for the path to the CSV file. Use the Sample.csv file in the project "ManualBulkOperations" folder as a template. If you want to skip assignment you can Set FailedAssignment to "False" 
