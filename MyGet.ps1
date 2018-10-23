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

function Test([string]$projectpath) {
    Write-Diagnostic "Testing $projectpath ..."
    dotnet test .\src\Tests\$projectpath\$projectpath.csproj
    if($LASTEXITCODE -ne 0) { Die("$projectpath failed.") }
}

.\build\build.ps1 -version "$env:PackageVersion" -config "$env:Configuration"
if($LASTEXITCODE -ne 0) { Die("Build failed.") }

Test "Peachpie.ScriptTests"
Test "Peachpie.DiagnosticTests"

Write-Diagnostic "Succeeded."