dotnet build -c release
$cert = Get-ChildItem Cert:\CurrentUser\My |
  Where-Object { $_.Thumbprint -eq "FF359F7EB11C96F7D182BA642800E0625E3AFBEB" } |
  Select-Object Subject, Thumbprint, @{n="SHA256";e={$_.GetCertHashString("SHA256")}}
dotnet nuget sign .\bin\release\EZSmartCardClient.1.0.3.nupkg --certificate-fingerprint $cert.SHA256 --timestamper http://timestamp.digicert.com
dotnet nuget verify --all .\bin\release\EZSmartCardClient.1.0.3.nupkg