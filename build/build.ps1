Param(
  [string]$config = "Release",
  [string]$suffix = "preview-" + [datetime]::now.tostring("yyyyMMdd-HHmmss")
)

$projects = @("Peachpie.Runtime", "Peachpie.Library", "Peachpie.Library.MySql", "Peachpie.Library.MsSql", "Peachpie.App", "Peachpie.CodeAnalysis", "Peachpie.NETCore.Web", "Peachpie.Compiler.Tools")

foreach ($p in $projects) {
   msbuild.exe "..\src\$p\$p.csproj" /t:build /v:m /p:VersionSuffix=$suffix /p:Configuration=$config /p:GeneratePackageOnBuild=true
}
