.\build\build.ps1 -suffix "CI$env:BuildCounter"
dotnet test .\src\Tests\ScriptsTest\ScriptsTest.csproj