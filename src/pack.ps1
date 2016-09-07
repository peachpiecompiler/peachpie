$suffix = "preview6"

dotnet pack -c Release -o .nugs --version-suffix $suffix Peachpie.Runtime
dotnet pack -c Release -o .nugs --version-suffix $suffix Peachpie.NETStandard.Library
dotnet pack -c Release -o .nugs --version-suffix $suffix Peachpie.NETCore.Compiler.Tools

..\tools\nuget pack .\Peachpie.NETStandard.App.nuspec -o .nugs -version 0.1.1-$suffix
..\tools\nuget pack .\Peachpie.CodeAnalysis.nuspec -o .nugs -version 0.1.1-$suffix
