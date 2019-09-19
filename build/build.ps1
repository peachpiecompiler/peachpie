Param(
  [string]$version = "1.0.0-dev",
  [string]$config = "Debug"
)

$props = "/p:Version=$version,Configuration=$config"

Write-Host -f Green "msbuild $props ..."

msbuild /t:restore "$PSScriptRoot/../Peachpie.sln" /v:m $props
dotnet restore "$PSScriptRoot/../src/Peachpie.NET.Sdk" /v:m $props
msbuild /t:build "$PSScriptRoot/../Peachpie.sln" /v:m $props
