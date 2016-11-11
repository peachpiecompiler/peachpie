Param(
  [string]$version = "0.3.0",
  [string]$config = "Release",
  [string]$suffix = "preview"
)

$out = "../.nugs/${config}"
$projects = @("Peachpie.Runtime", "Peachpie.Library", "Peachpie.App", "Peachpie.CodeAnalysis", "Peachpie.NETCore.Web", "Peachpie.Compiler.Tools")

foreach ($p in $projects) {
   dotnet pack -c $config -o $out --version-suffix $suffix $p
}

pause