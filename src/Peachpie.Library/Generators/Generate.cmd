"..\..\..\Tools\cslex" "StrToTime.lex" "..\Parsers\StrToTimeScanner.cs" /v:2

"..\..\..\Tools\cslex" "json.lex" "..\Parsers\jsonLexer.cs" /v:2
"..\..\..\Tools\gppg" /l /r "json.y" "..\Parsers\jsonParser.cs" "..\Parsers\json.log"

@pause