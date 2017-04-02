Param(
  [string]$config = "Release",
  [string]$suffix = "preview-" + [datetime]::now.tostring("yyyyMMdd-HHmmss")
)

dotnet restore "$PSScriptRoot/../Peachpie.sln" /v:m /p:VersionSuffix=$suffix,PackageVersionSuffix=$suffix,Configuration=$config
dotnet build "$PSScriptRoot/../Peachpie.sln" /v:m /p:VersionSuffix=$suffix,PackageVersionSuffix=$suffix,Configuration=$config