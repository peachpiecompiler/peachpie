Param(
  [string]$suffix = "xxx"
)

..\tools\nuget.exe push ..\.nugs\*.*$suffix.nupkg -Source https://www.nuget.org/api/v2/package