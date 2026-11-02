# "..\..\..\Tools\cslex" "StrToTime.lex" "..\Parsers\StrToTimeScanner`Generated.cs" /v:2

# REM "..\..\..\Tools\cslex" "json.lex" "..\Parsers\jsonLexer.cs" /v:2
# REM "..\..\..\Tools\gppg" /l /r "json.y" "..\Parsers\jsonParser.cs" "..\Parsers\json.log"

# @pause

# Generators -> Library -> src -> repo root. The directory is "tools" (lowercase).
$Root = (Resolve-Path (Join-Path $PSScriptRoot "../../..")).Path
$CsLex = Join-Path $Root "tools/CsLex.dll"
$Lex = Join-Path $PSScriptRoot "StrToTime.lex"
$Out = Join-Path $PSScriptRoot "../Parsers/StrToTimeScannerGenerated.cs"
dotnet $CsLex $Lex $Out /v:2
