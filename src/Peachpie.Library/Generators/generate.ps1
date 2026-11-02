# "..\..\..\Tools\cslex" "StrToTime.lex" "..\Parsers\StrToTimeScanner`Generated.cs" /v:2

# REM "..\..\..\Tools\cslex" "json.lex" "..\Parsers\jsonLexer.cs" /v:2
# REM "..\..\..\Tools\gppg" /l /r "json.y" "..\Parsers\jsonParser.cs" "..\Parsers\json.log"

# @pause

$Tools = "$PSScriptRoot/../../../Tools"
dotnet "$Tools/cslex.dll" "$PSScriptRoot/StrToTime.lex" "$PSScriptRoot/../Parsers/StrToTimeScanner`Generated.cs" /v:2