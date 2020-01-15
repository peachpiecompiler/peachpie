using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Pchp.Library.Parsers;

namespace Pchp.Library.Json
{
    internal class JsonScanner : Json.Lexer, ITokenProvider<Json.SemanticValueType, Json.Position>
    {
        Json.SemanticValueType tokenSemantics;
        Json.Position tokenPosition;

        private readonly PhpSerialization.JsonSerializer.DecodeOptions/*!*/decodeOptions;

        public JsonScanner(TextReader/*!*/ reader, PhpSerialization.JsonSerializer.DecodeOptions/*!*/decodeOptions)
            : base(reader)
        {
            Debug.Assert(decodeOptions != null);

            this.decodeOptions = decodeOptions;
        }

        #region ITokenProvider<SemanticValueType,Position> Members

        public Json.SemanticValueType TokenValue
        {
            get { return tokenSemantics; }
        }

        public Json.Position TokenPosition
        {
            get { return tokenPosition; }
        }

        public new int GetNextToken()
        {
            tokenPosition = new Json.Position();
            tokenSemantics = new Json.SemanticValueType();

            Json.Tokens token = base.GetNextToken();

            switch (token)
            {
                case Json.Tokens.STRING_BEGIN:
                    while ((token = base.GetNextToken()) != Json.Tokens.STRING_END)
                    {
                        if (token == Json.Tokens.ERROR || token == Json.Tokens.EOF)
                            throw new Exception("Syntax error, unexpected " + TokenChunkLength.ToString());
                    }
                    token = Json.Tokens.STRING;
                    tokenSemantics.obj = base.QuotedStringContent;
                    break;
                case Json.Tokens.INTEGER:
                case Json.Tokens.DOUBLE:
                    {
                        string numtext = yytext();
                        switch (Core.Convert.StringToNumber(numtext, out var l, out var d) & Core.Convert.NumberInfo.TypeMask)
                        {
                            case Core.Convert.NumberInfo.Double:
                                if (decodeOptions.BigIntAsString && token == Json.Tokens.INTEGER)
                                    tokenSemantics.value = numtext;   // it was integer, but converted to double because it was too long
                                else
                                    tokenSemantics.value = d;
                                break;
                            case Core.Convert.NumberInfo.LongInteger:
                                tokenSemantics.value = l;
                                break;
                            default:
                                tokenSemantics.value = numtext;
                                break;

                        }
                    }
                    break;
            }

            return (int)token;
        }

        public void ReportError(string[] expectedTokens)
        {
        }

        #endregion
    }
}
