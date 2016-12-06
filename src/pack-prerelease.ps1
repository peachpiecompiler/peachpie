Param(
  [string]$config = "Release",
  [string]$suffix = "preview"
)

$out = "../.nugs/${config}"
$projects = @("Peachpie.Runtime", "Peachpie.Library", "Peachpie.Library.MySql", "Peachpie.App", "Peachpie.CodeAnalysis", "Peachpie.NETCore.Web", "Peachpie.Compiler.Tools")

foreach ($p in $projects) {
   dotnet pack -c $config -o $out --version-suffix $suffix $p
}

pause