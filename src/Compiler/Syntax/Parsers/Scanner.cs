using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;

using PHP.Core;
using PHP.Core.Parsers.GPPG;
using PHP.Core.Text;

namespace PHP.Core.Parsers
{
    #region ICommentsSink

    /// <summary>
    /// Sink for comment tokens and tokens not handled in parser.
    /// These tokens are ignored by tokenizer, so they are not available in resulting AST.
    /// By providing this interface as a part of <see cref="IReductionsSink"/> implementation, implementers may handle additional language elements at token level.
    /// </summary>
    public interface ICommentsSink
    {
        void OnLineComment(Scanner/*!*/scanner, Text.TextSpan span);
        void OnComment(Scanner/*!*/scanner, Text.TextSpan span);
        void OnPhpDocComment(Scanner/*!*/scanner, PHPDocBlock phpDocBlock);

        void OnOpenTag(Scanner/*!*/scanner, Text.TextSpan span);
        void OnCloseTag(Scanner/*!*/scanner, Text.TextSpan span);
    }

    #endregion

    #region IScannerHandler

    public interface IScannerHandler
    {
        /// <summary>
        /// Called by <see cref="Scanner"/> when new token is obtained from lexer.
        /// </summary>
        /// <param name="token">Token.</param>
        /// <param name="buffer">Internal text buffer.</param>
        /// <param name="tokenStart">Position within <paramref name="buffer"/> where the token text starts.</param>
        /// <param name="tokenLength">Length of the token text.</param>
        void OnNextToken(Tokens token, char[] buffer, int tokenStart, int tokenLength);
    }

    #endregion

    public sealed class Scanner : Lexer, ITokenProvider<SemanticValueType, Text.Span>
    {
        #region Nested class: NullCommentsSink

        internal sealed class NullCommentsSink : ICommentsSink
        {
            #region ICommentsSink Members

            public void OnLineComment(Scanner scanner, Text.TextSpan span) { }
            public void OnComment(Scanner scanner, Text.TextSpan span) { }
            public void OnPhpDocComment(Scanner scanner, PHPDocBlock phpDocBlock) { }
            public void OnOpenTag(Scanner scanner, Text.TextSpan span) { }
            public void OnCloseTag(Scanner scanner, Text.TextSpan span) { }

            #endregion
        }

        #endregion

        #region Nested class: NullScannerHandler

        internal sealed class NullScannerHandler : IScannerHandler
        {
            #region IScannerHandler Members

            public void OnNextToken(Tokens token, char[] buffer, int tokenStart, int tokenLength) { }

            #endregion
        }

        #endregion

        public ErrorSink/*!*/ ErrorSink { get { return errors; } }
        private readonly ErrorSink/*!*/ errors;

        /// <summary>
        /// Sink for comments.
        /// </summary>
        private readonly ICommentsSink/*!*/commentsSink;

        /// <summary>
        /// Sink for various scanner events.
        /// </summary>
        private readonly IScannerHandler/*!*/scannerHandler;

        public LanguageFeatures LanguageFeatures { get { return features; } }
        private readonly LanguageFeatures features;

        public SourceUnit/*!*/ SourceUnit { get { return sourceUnit; } }
        private readonly SourceUnit/*!*/ sourceUnit;

        // encapsed string buffering:
        public StringBuilder/*!*/ EncapsedStringBuffer { get { return encapsedStringBuffer; } }
        private readonly StringBuilder/*!*/ encapsedStringBuffer = new StringBuilder(1000);

        private SemanticValueType tokenSemantics;
        private Text.Span tokenPosition;
        private Text.TextSpan TokenTextSpan { get { return new TextSpan(sourceUnit, tokenPosition); } }

        private int charOffset;

        private Encoding Encoding { get { return sourceUnit.Encoding; } }
        private bool IsPure { get { return sourceUnit.IsPure; } }

        public Scanner(TextReader/*!*/ reader, SourceUnit/*!*/ sourceUnit,
            ErrorSink/*!*/ errors, ICommentsSink commentsSink, IScannerHandler scannerHandler,
            LanguageFeatures features, int positionShift)
            : base(reader)
        {
            if (reader == null)
                throw new ArgumentNullException("reader");
            if (sourceUnit == null)
                throw new ArgumentNullException("sourceUnit");
            if (errors == null)
                throw new ArgumentNullException("errors");

            this.errors = errors;
            this.commentsSink = commentsSink ?? new NullCommentsSink();
            this.scannerHandler = scannerHandler ?? new NullScannerHandler();
            this.features = features;
            this.sourceUnit = sourceUnit;
            this.charOffset = positionShift;

            this.AllowAspTags = (features & LanguageFeatures.AspTags) != 0;
            this.AllowShortTags = (features & LanguageFeatures.ShortOpenTags) != 0;
        }

        private void StoreEncapsedString()
        {
            tokenSemantics.Integer = TokenLength;
            tokenSemantics.Offset = encapsedStringBuffer.Length;
            AppendTokenTextTo(encapsedStringBuffer);
        }

        private void StoreEncapsedString(string str)
        {
            tokenSemantics.Integer = str.Length;
            tokenSemantics.Offset = encapsedStringBuffer.Length;
            encapsedStringBuffer.Append(str);
        }

        public string GetEncapsedString(int offset, int length)
        {
            return encapsedStringBuffer.ToString(offset, length);
        }

        /// <summary>
        /// Updates <see cref="charOffset"/> and <see cref="tokenPosition"/>.
        /// </summary>
        private void UpdateTokenPosition()
        {
            int tokenLength = this.TokenLength;

            // update token position info:
            tokenPosition = new Span(charOffset, tokenLength);
            charOffset += tokenLength;
        }

        public new Tokens GetNextToken()
        {
            for (; ; )
            {
                inString = false;
                isCode = false;

                Tokens token = base.GetNextToken();
                UpdateTokenPosition();

                this.scannerHandler.OnNextToken(token, this.Buffer, this.BufferTokenStart, this.TokenLength);

                switch (token)
                {
                    #region Comments

                    // ignored tokens:
                    case Tokens.T_WHITESPACE: break;
                    case Tokens.T_COMMENT: this.commentsSink.OnComment(this, TokenTextSpan); break;
                    case Tokens.T_LINE_COMMENT: this.commentsSink.OnLineComment(this, TokenTextSpan); break;
                    case Tokens.T_OPEN_TAG: this.commentsSink.OnOpenTag(this, TokenTextSpan); break;
                    case Tokens.T_DOC_COMMENT: this.commentsSink.OnPhpDocComment(this, new PHPDocBlock(base.GetTokenString(), this.tokenPosition)); break;

                    case Tokens.T_PRAGMA_FILE:
                        throw new NotSupportedException();//sourceUnit.AddSourceFileMapping(TokenTextSpan.FirstLine, base.GetTokenAsFilePragma());
                        
                    case Tokens.T_PRAGMA_LINE:
                        throw new NotSupportedException();
                        
                    case Tokens.T_PRAGMA_DEFAULT_FILE:
                        throw new NotSupportedException();//sourceUnit.AddSourceFileMapping(TokenTextSpan.FirstLine, SourceUnit.DefaultFile);
                        
                    case Tokens.T_PRAGMA_DEFAULT_LINE:
                        throw new NotSupportedException();//sourceUnit.AddSourceLineMapping(TokenTextSpan.FirstLine, SourceUnit.DefaultLine);

                    #endregion

                    #region String Semantics

                    case Tokens.T_VARIABLE:
                        // exclude initial $ from the name:
                        Debug.Assert(GetTokenChar(0) == '$');
                        tokenSemantics.Object = base.GetTokenSubstring(1);
                        goto default;

                    case Tokens.T_STRING:
                        if (inString)
                            StoreEncapsedString();
                        else
                            tokenSemantics.Object = base.GetTokenString();

                        goto default;

                    case Tokens.T_ARRAY:
                    case Tokens.T_LIST:
                        tokenSemantics.Object = base.GetTokenString();  // remember the token string, so we can use these tokens as literals later, case sensitively
                        goto default;

                    case Tokens.T_STRING_VARNAME:
                    case Tokens.T_NUM_STRING:
                    case Tokens.T_ENCAPSED_AND_WHITESPACE:
                    case Tokens.T_BAD_CHARACTER:
                        StoreEncapsedString();
                        goto default;

                    case Tokens.T_INLINE_HTML:
                        tokenSemantics.Object = base.GetTokenString();
                        goto default;


                    // \[uU]#{0-6}
                    case Tokens.UnicodeCharCode:
                        {
                            Debug.Assert(inString);

                            //if (GetTokenChar(1) == 'u')
                            //{
                            //  if (TokenLength != 2 + 4)
                            //    errors.Add(Warnings.InvalidEscapeSequenceLength, sourceFile, tokenPosition.Short, GetTokenString(), 4);
                            //}
                            //else
                            //{
                            //  if (TokenLength != 2 + 6)
                            //    errors.Add(Warnings.InvalidEscapeSequenceLength, sourceFile, tokenPosition.Short, GetTokenString(), 6);
                            //}

                            int code_point = GetTokenAsInteger(2, 16);

                            try
                            {
                                if ((code_point < 0 || code_point > 0x10ffff) || (code_point >= 0xd800 && code_point <= 0xdfff))
                                {
                                    errors.Add(Errors.InvalidCodePoint, SourceUnit, tokenPosition, GetTokenString());
                                    StoreEncapsedString("?");
                                }
                                else
                                {
                                    StoreEncapsedString(StringUtils.Utf32ToString(code_point));
                                }
                            }
                            catch (ArgumentOutOfRangeException)
                            {
                                errors.Add(Errors.InvalidCodePoint, SourceUnit, tokenPosition, GetTokenString());
                                StoreEncapsedString("?");
                            }
                            token = Tokens.T_STRING;
                            goto default;
                        }

                    // \C{name}
                    case Tokens.UnicodeCharName:
                        Debug.Assert(inString);
                        StoreEncapsedString(); // N/S
                        token = Tokens.T_STRING;
                        goto default;

                    // b?"xxx"
                    case Tokens.DoubleQuotedString:
                        {
                            bool forceBinaryString = GetTokenChar(0) == 'b';

                            tokenSemantics.Object = GetTokenAsDoublyQuotedString(forceBinaryString ? 1 : 0, this.Encoding, forceBinaryString);
                            token = Tokens.T_CONSTANT_ENCAPSED_STRING;
                            goto default;
                        }

                    // b?'xxx'
                    case Tokens.SingleQuotedString:
                        {
                            bool forceBinaryString = GetTokenChar(0) == 'b';

                            tokenSemantics.Object = GetTokenAsSinglyQuotedString(forceBinaryString ? 1 : 0, this.Encoding, forceBinaryString);
                            token = Tokens.T_CONSTANT_ENCAPSED_STRING;
                            goto default;
                        }

                    #endregion

                    #region Numeric Semantics

                    case Tokens.T_CURLY_OPEN:
                        tokenSemantics.Integer = (int)Tokens.T_CURLY_OPEN;
                        goto default;

                    case Tokens.T_CHARACTER:
                        tokenSemantics.Integer = (int)GetTokenChar(0);
                        goto default;

                    case Tokens.EscapedCharacter:
                        tokenSemantics.Integer = (int)GetTokenAsEscapedCharacter(0);
                        token = Tokens.T_CHARACTER;
                        goto default;

                    case Tokens.T_LINE:
                        // TODO: 
                        tokenSemantics.Integer = 1;
                        goto default;

                    // "\###"
                    case Tokens.OctalCharCode:
                        tokenSemantics.Integer = GetTokenAsInteger(1, 10);
                        token = Tokens.T_CHARACTER;
                        goto default;

                    // "\x##"
                    case Tokens.HexCharCode:
                        tokenSemantics.Integer = GetTokenAsInteger(2, 16);
                        token = Tokens.T_CHARACTER;
                        goto default;

                    // {LNUM}
                    case Tokens.ParseDecimalNumber:
                        {
                            // [0-9]* - value is either in octal or in decimal
                            if (GetTokenChar(0) == '0')
                                token = GetTokenAsDecimalNumber(1, 8, ref tokenSemantics);
                            else
                                token = GetTokenAsDecimalNumber(0, 10, ref tokenSemantics);

                            if (token == Tokens.T_DNUMBER)
                            {
                                // conversion to double causes data loss
                                errors.Add(Warnings.TooBigIntegerConversion, SourceUnit, tokenPosition, GetTokenString());
                            }
                            goto default;
                        }

                    // {HNUM}
                    case Tokens.ParseHexadecimalNumber:
                        {
                            // parse hexadecimal value
                            token = GetTokenAsDecimalNumber(2, 16, ref tokenSemantics);

                            if (token == Tokens.T_DNUMBER)
                            {
                                // conversion to double causes data loss
                                errors.Add(Warnings.TooBigIntegerConversion, SourceUnit, tokenPosition, GetTokenString());
                            }
                            goto default;
                        }

                    // {BNUM}
                    case Tokens.ParseBinaryNumber:
                        // parse binary number value
                        token = GetTokenAsDecimalNumber(2, 2, ref tokenSemantics);

                        if (token == Tokens.T_DNUMBER)
                        {
                            // conversion to double causes data loss
                            errors.Add(Warnings.TooBigIntegerConversion, SourceUnit, tokenPosition, GetTokenString());
                        }
                        goto default;

                    // {DNUM}|{EXPONENT_DNUM}
                    case Tokens.ParseDouble:
                        tokenSemantics.Double = GetTokenAsDouble(0);
                        token = Tokens.T_DNUMBER;
                        goto default;

                    #endregion

                    #region Another Semantics

                    // i'xxx'	
                    case Tokens.SingleQuotedIdentifier:
                        tokenSemantics.Object = (string)GetTokenAsSinglyQuotedString(1, this.Encoding, false);
                        token = Tokens.T_STRING;
                        goto default;

                    #endregion

                    #region Token Reinterpreting

                    case Tokens.T_OPEN_TAG_WITH_ECHO:
                        this.commentsSink.OnOpenTag(this, TokenTextSpan);
                        token = Tokens.T_ECHO;
                        goto default;

                    case Tokens.T_CLOSE_TAG:
                        this.commentsSink.OnCloseTag(this, TokenTextSpan);
                        token = Tokens.T_SEMI;
                        goto default;

                    case Tokens.EOF:
                        if (this.CurrentLexicalState == LexicalStates.ST_ONE_LINE_COMMENT)
                        {
                            this.CurrentLexicalState = LexicalStates.ST_IN_SCRIPTING;
                            token = Tokens.T_LINE_COMMENT;
                            _yymore();
                            goto case Tokens.T_LINE_COMMENT;
                        }
                        goto default;


                    case Tokens.T_TRUE:
                    case Tokens.T_FALSE:
                    case Tokens.T_NULL:
                    case Tokens.T_GET:
                    case Tokens.T_SET:
                    case Tokens.T_CALL:
                    case Tokens.T_CALLSTATIC:
                    case Tokens.T_WAKEUP:
                    case Tokens.T_SLEEP:
                    case Tokens.T_TOSTRING:
                    case Tokens.T_CONSTRUCT:
                    case Tokens.T_DESTRUCT:
                    case Tokens.T_PARENT:
                    case Tokens.T_SELF:
                    case Tokens.T_AUTOLOAD:
                        token = Tokens.T_STRING;
                        goto case Tokens.T_STRING;

                    case Tokens.T_TRY:
                    case Tokens.T_CATCH:
                    case Tokens.T_FINALLY:
                    case Tokens.T_THROW:
                    case Tokens.T_IMPLEMENTS:
                    case Tokens.T_CLONE:
                    case Tokens.T_ABSTRACT:
                    case Tokens.T_FINAL:
                    case Tokens.T_PRIVATE:
                    case Tokens.T_PROTECTED:
                    case Tokens.T_PUBLIC:
                    case Tokens.T_INSTANCEOF:
                    case Tokens.T_INTERFACE:
                    case Tokens.T_GOTO:
                    case Tokens.T_NAMESPACE:
                    case Tokens.T_NAMESPACE_C:
                    case Tokens.T_NS_SEPARATOR:
                    case Tokens.T_USE:
                        {
                            if ((features & LanguageFeatures.V5Keywords) == 0)
                            {
                                token = Tokens.T_STRING;
                                goto case Tokens.T_STRING;
                            }

                            if (token == Tokens.T_ABSTRACT)
                            {
                                // remember this for possible CLR qualified name:
                                tokenSemantics.Object = base.GetTokenString();
                            }

                            goto default;
                        }

                    case Tokens.T_IMPORT:
                        {
                            if (!sourceUnit.IsPure)
                            {
                                token = Tokens.T_STRING;
                                goto case Tokens.T_STRING;
                            }

                            goto default;
                        }

                    case Tokens.T_BOOL_TYPE:
                    case Tokens.T_INT_TYPE:
                    case Tokens.T_INT64_TYPE:
                    case Tokens.T_DOUBLE_TYPE:
                    case Tokens.T_STRING_TYPE:
                    case Tokens.T_RESOURCE_TYPE:
                    case Tokens.T_OBJECT_TYPE:
                    case Tokens.T_TYPEOF:
                        {
                            if ((features & LanguageFeatures.TypeKeywords) == 0)
                            {
                                token = Tokens.T_STRING;
                                goto case Tokens.T_STRING;
                            }

                            tokenSemantics.Object = base.GetTokenString();

                            goto default;
                        }

                    case Tokens.T_PARTIAL:
                        {
                            if (!IsPure)
                            {
                                token = Tokens.T_STRING;
                                goto case Tokens.T_STRING;
                            }

                            goto default;
                        }

                    #endregion

                    #region Error Tokens

                    case Tokens.ERROR:
                        goto default;

                    case Tokens.ErrorInvalidIdentifier:
                        {
                            // invalid identifier i'XXX':
                            errors.Add(Errors.InvalidIdentifier, SourceUnit, tokenPosition, (string)GetTokenAsSinglyQuotedString(1, this.Encoding, false));

                            tokenSemantics.Object = GetErrorIdentifier();
                            token = Tokens.T_STRING;
                            goto default;
                        }

                    case Tokens.ErrorNotSupported:
                        errors.Add(Errors.ConstructNotSupported, SourceUnit, tokenPosition, GetTokenString());
                        tokenSemantics.Object = GetErrorIdentifier();
                        token = Tokens.T_STRING;
                        goto default;

                    #endregion

                    case Tokens.T_SEMI:
                    default:
                        return token;
                }
            }
        }

        #region ITokenProvider<SemanticValueType, Parsers.Position> Members

        int ITokenProvider<SemanticValueType, Text.Span>.GetNextToken()
        {
            return (int)GetNextToken();
        }

        void ITokenProvider<SemanticValueType, Text.Span>.ReportError(string[] expectedTerminals)
        {
            // TODO (expected tokens....)
            errors.Add(FatalErrors.SyntaxError, SourceUnit, tokenPosition,
                CoreResources.GetString("unexpected_token", GetTokenString()));

            //throw new CompilerException();	
        }

        SemanticValueType ITokenProvider<SemanticValueType, Text.Span>.TokenValue
        {
            get { return tokenSemantics; }
        }

        Text.Span ITokenProvider<SemanticValueType, Text.Span>.TokenPosition
        {
            get { return tokenPosition; }
        }

        #endregion

        #region Erroneous Identifiers

        private int errorNameCounter = 0;
        private const string ErrorNamePrefix = "__error#";

        internal string/*!*/ GetErrorIdentifier()
        {
            return ErrorNamePrefix + errorNameCounter++;
        }

        #endregion
    }
}