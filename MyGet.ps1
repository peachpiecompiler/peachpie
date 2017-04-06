# Diagnostic 
function Write-Diagnostic {
    param([string]$message)

    Write-Host
    Write-Host -f green $message
    Write-Host
}

function Die([string]$message, [object[]]$output) {
    if ($output) { Write-Output $output }
    Write-Error $message
    exit 1
}

.\build\build.ps1 -suffix "CI$env:BuildCounter"
if($LASTEXITCODE -ne 0) { Die("Build failed.") }

dotnet test .\src\Tests\Peachpie.ScriptTests\Peachpie.ScriptTests.csproj
if($LASTEXITCODE -ne 0) { Die("Peachpie.ScriptTests failed.") }

Write-Diagnostic "Succeeded."