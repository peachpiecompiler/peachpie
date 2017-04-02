Param(
  [string]$config = "Release",
  [string]$suffix = "preview-" + [datetime]::now.tostring("yyyyMMdd-HHmmss")
)

msbuild.exe "..\Peachpie.sln" /t:restore /v:m /p:VersionSuffix=$suffix,PackageVersionSuffix=$suffix,Configuration=$config
msbuild.exe "..\Peachpie.sln" /t:build /v:m /p:VersionSuffix=$suffix,PackageVersionSuffix=$suffix,Configuration=$config