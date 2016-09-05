dotnet pack -c Release -o .nugs --version-suffix preview3 Peachpie.Runtime
dotnet pack -c Release -o .nugs --version-suffix preview3 Peachpie.NETStandard.Library
dotnet pack -c Release -o .nugs --version-suffix preview3 Peachpie.NETCore.Compiler.Tools

..\tools\nuget pack .\Peachpie.NETStandard.App.nuspec -o .nugs -version 0.1.1-preview3
..\tools\nuget pack .\Peachpie.CodeAnalysis.nuspec -o .nugs -version 0.1.1-preview3
