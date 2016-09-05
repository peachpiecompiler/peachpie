dotnet pack -c Release -o . --version-suffix preview3 Peachpie.Runtime
dotnet pack -c Release -o . --version-suffix preview3 Peachpie.NETStandard.Library
dotnet pack -c Release -o . --version-suffix preview3 Peachpie.NETCore.Compiler.Tools

..\tools\nuget pack .\Peachpie.NETStandard.App.nuspec
..\tools\nuget pack .\Peachpie.CodeAnalysis.nuspec

pause