Param(
  [string]$version = "0.9.9-dev",
  [string]$config = "Debug"
)

$props = "/p:Version=$version,Configuration=$config"

Write-Host -f Green "msbuild $props ..."

msbuild /t:restore "$PSScriptRoot/../Peachpie.sln" /v:m $props
dotnet restore "$PSScriptRoot/../src/Peachpie.Compiler.Tools" /v:m $props
msbuild /t:build "$PSScriptRoot/../Peachpie.sln" /v:m $props
