Param(
  [string]$config = "Release",
  [string]$suffix = "preview-" + [datetime]::now.tostring("yyyyMMdd-HHmmss")
)

$props = "/p:VersionSuffix=$suffix,PackageVersionSuffix=$suffix,Configuration=$config"

Write-Host -f Green "msbuild $props ..."

msbuild /t:restore "$PSScriptRoot/../Peachpie.sln" /v:m $props
dotnet restore "$PSScriptRoot/../src/Peachpie.Compiler.Tools" /v:m $props
msbuild /t:build "$PSScriptRoot/../Peachpie.sln" /v:m $props