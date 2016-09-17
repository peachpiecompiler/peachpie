Param(
  [string]$version = "0.2.0",
  [string]$config = "Release",
  [string]$suffix = "preview-1"
)

// TODO: compile CodeAnalysis

$out = "../.nugs"
$projects = @("Peachpie.Runtime", "Peachpie.Library", "Peachpie.App", "Peachpie.Compiler.Tools")
$nuspecs = @("Peachpie.Compiler.nuspec")

foreach ($p in $projects) {
   dotnet pack -c $config -o $out --version-suffix $suffix $p
}

foreach ($n in $nuspecs) {
   ..\tools\nuget pack $n -o $out -version "${version}-${suffix}"
}

pause