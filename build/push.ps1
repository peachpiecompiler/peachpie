Param(
  [string]$suffix = "preview7"
)

..\tools\nuget.exe push ..\.nugs\Release\*.*$suffix.nupkg -Source https://www.nuget.org/api/v2/package