.\build\build.ps1 -suffix "CI$env:BuildCounter"
dotnet test .\src\Tests\Peachpie.ScriptTests\Peachpie.ScriptTests.csproj