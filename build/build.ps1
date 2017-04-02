Param(
  [string]$config = "Release",
  [string]$suffix = "preview-" + [datetime]::now.tostring("yyyyMMdd-HHmmss")
)

dotnet restore "..\Peachpie.sln" /v:m /p:VersionSuffix=$suffix,PackageVersionSuffix=$suffix,Configuration=$config
dotnet build "..\Peachpie.sln" /v:m /p:VersionSuffix=$suffix,PackageVersionSuffix=$suffix,Configuration=$config