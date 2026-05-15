#!/usr/bin/env bash
set -euo pipefail

# A script to update the NuGet cache with built packages

version="${1:-1.2.0}"
suffix="${2:-dev}"

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
root_dir="$(realpath "$script_dir/..")"
peachpie_version="$version-$suffix"

echo "Root at $root_dir"

packages_source="$HOME/.nuget/packages"
restore_args=(
  "/p:PeachpieVersion=$peachpie_version"
  "/p:VersionPrefix=$version"
  "/p:VersionSuffix=$suffix"
)

echo -e "\033[32mDeleting '$version-$suffix' packages from '$packages_source' ...\033[0m"

packages=(
  "Peachpie.Runtime"
  "Peachpie.Library"
  "Peachpie.Library.Scripting"
  "Peachpie.Library.MySql"
  "Peachpie.Library.MsSql"
  "Peachpie.Library.Graphics"
  "Peachpie.Library.Network"
  "Peachpie.Library.PDO"
  "Peachpie.Library.XmlDom"
  "Peachpie.App"
  "Peachpie.CodeAnalysis"
  "Peachpie.AspNetCore.Web"
  "Peachpie.RequestHandler"
  "Peachpie.AspNetCore.Mvc"
  "Peachpie.NET.Sdk"
  "Peachpie.Library.PDO.MySQL"
  "Peachpie.Library.PDO.Sqlite"
  "Peachpie.Library.PDO.SqlSrv"
)

for package in "${packages[@]}"; do
  installed_folder="$packages_source/$package/$version-$suffix"

  if [ -d "$installed_folder" ]; then
    rm -rf "$installed_folder"
  fi
done

# Clean up the installed tool settings so that no old dependencies hang in there
# tool_folder="$packages_source/.tools/Peachpie.Compiler.Tools/$version-$suffix"
# if [ -d "$tool_folder" ]; then
#   rm -rf "$tool_folder"
# fi

# Restore top packages, dependencies restored recursively
# echo -e "\033[32mRestoring packages ...\033[0m"
# for package in "Peachpie.NET.Sdk" "Peachpie.Library.Scripting" "Peachpie.AspNetCore.Web"; do
#   dotnet restore "$default_args" "$root_dir/src/$package"
# done

echo -e "\033[32mInstalling packages to nuget cache ...\033[0m"
dotnet restore "${restore_args[@]}" "$root_dir/build/dummy" --ignore-failed-sources
