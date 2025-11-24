# How to create a release

## Create NuGet package

- Open Visual Studio Developer Command Prompt
- Change directory to the root directory of this solution
- Run "dotnet pack -c Release AutoIndexCache\AutoIndexCache.csproj"

## Sign NuGet package

- Open Visual Studio Developer Command Prompt
- Change directory to the root directory of this solution
- Run "dotnet nuget sign AutoIndexCache\bin\Release\AutoIndexCache.1.x.x.nupkg --certificate-path Path\To\CodeSigningCertificate.pfx --timestamper http://timestamp.comodoca.com/rfc3161 --certificate-password CertificatePassword"
- Enter the encryption password for the code signing certificate.

## Sign NuGet symbols package

- Open Visual Studio Developer Command Prompt
- Change directory to the root directory of this solution
- Run "dotnet nuget sign AutoIndexCache\bin\Release\AutoIndexCache.1.x.x.snupkg --certificate-path Path\To\CodeSigningCertificate.pfx --timestamper http://timestamp.comodoca.com/rfc3161 --certificate-password CertificatePassword"
- Enter the encryption password for the code signing certificate.