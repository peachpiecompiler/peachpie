using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;

namespace PHP.Core.Parsers
{
	public sealed class Tokenizer : Lexer
	{
		[Flags]
		public enum Features
		{
			V5Keywords = 1,
			ContextKeywords = 2,
			SkipWhitespace = 8,

			AllowAspTags = 16,
			AllowShortTags = 32,
			TypeKeywords = 64,

			Default = V5Keywords | AllowAspTags | AllowShortTags |  ContextKeywords | TypeKeywords
		}

		private Features features;

		public Tokenizer(TextReader/*!*/ reader)
			: this(reader, Features.Default)
		{
		}

		public Tokenizer(TextReader/*!*/ reader, Features features)
			: base(reader)
		{
			if (reader == null)
				throw new ArgumentNullException("reader");

			AllowAspTags = (features & Features.AllowAspTags) != 0;
			AllowShortTags = (features & Features.AllowShortTags) != 0;

			this.features = features;
		}

		public TokenCategory TokenCategory { get { return tokenCategory; } }
		private TokenCategory tokenCategory;

        public Text.Span TokenSpan { get { return Text.Span.FromBounds(token_start_pos.Char, token_end_pos.Char + 1); } }

		public Tokens RealToken { get { return realToken; } }
		private Tokens realToken;

        public string TokenText { get { return _tokenText ?? (_tokenText = base.GetTokenString()); } }
		[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        private string _tokenText;

		#region Compressed State

		public struct CompressedState : IEquatable<CompressedState>
		{
			internal string HereDocLabel { get { return hereDocLabel; } }
			private readonly string hereDocLabel;

			internal LexicalStates CurrentState { get { return currentState; } }
			private readonly LexicalStates currentState;

			private readonly LexicalStates[]/*!*/ stateStack;

			public CompressedState(Tokenizer tokenizer)
			{
				this.hereDocLabel = tokenizer.hereDocLabel;
				this.currentState = tokenizer.CurrentLexicalState;
				this.stateStack = tokenizer.StateStack.ToArray();
			}

			public override int GetHashCode()
			{
				unchecked
				{
					int result = (hereDocLabel != null) ? hereDocLabel.GetHashCode() : 0x2312347;
					for (int i = 0; i < stateStack.Length; i++)
						result ^= (int)stateStack[i] << i;
					return result ^ ((int)currentState << 7);
				}
			}

			public bool Equals(CompressedState other)
			{
				if (hereDocLabel != other.hereDocLabel) return false;
				if (stateStack.Length != other.stateStack.Length) return false;

				for (int i = 0; i < stateStack.Length; i++)
				{
					if (stateStack[i] != other.stateStack[i])
						return false;
				}

				return true;
			}

			public Stack<LexicalStates> GetStateStack()
			{
				return new Stack<LexicalStates>(stateStack);
			}
		}

		public CompressedState GetCompressedState()
		{
			return new CompressedState(this);
		}

		public void RestoreCompressedState(CompressedState state)
		{
			hereDocLabel = state.HereDocLabel;
			StateStack = state.GetStateStack();
			CurrentLexicalState = state.CurrentState;
		}

		#endregion

		public static bool IsCharToken(Tokens token)
		{
			return (int)token > 0 && (int)token < (int)Toks.ERROR;
		}

		public new Tokens GetNextToken()
		{
			for (; ; )
			{
				inString = false;
				isCode = false;

				Tokens token = realToken = base.GetNextToken();
				_tokenText = null;

				switch (token)
				{
					case Tokens.EOF:
					case Tokens.ERROR:
						tokenCategory = TokenCategory.Unknown;
						return token;

					case Tokens.ErrorInvalidIdentifier:
					case Tokens.ErrorNotSupported:
						tokenCategory = TokenCategory.Unknown;
						return Tokens.ERROR;

					#region Token Postprocessing

					// following two cases have to determine the token type
					// (T_LNUMBER/T_L64NUMBER or T_DNUMBER) depending on the actual value
					case Tokens.ParseDecimalNumber:
						tokenCategory = TokenCategory.Number;
						return GetHexIntegerTokenType(0);
					case Tokens.ParseHexadecimalNumber:
						tokenCategory = TokenCategory.Number; 
						return GetHexIntegerTokenType(2);
                    case Tokens.ParseBinaryNumber:
                        tokenCategory = TokenCategory.Number;
                        return GetHexIntegerTokenType(2);
					
					case Tokens.ParseDouble:
						goto case Tokens.T_DNUMBER;

					case Tokens.DoubleQuotedString:
					case Tokens.SingleQuotedString:
						goto case Tokens.T_CONSTANT_ENCAPSED_STRING;

					case Tokens.OctalCharCode:
					case Tokens.HexCharCode:
					case Tokens.UnicodeCharCode:
					case Tokens.UnicodeCharName:
					case Tokens.EscapedCharacter:
						goto case Tokens.T_CHARACTER;

					// i'xxx'	
					case Tokens.SingleQuotedIdentifier:
						tokenCategory = TokenCategory.Identifier;
						token = Tokens.T_STRING;
						return token;

					#endregion

					#region Special Keywords

					case Tokens.T_TRUE:
					case Tokens.T_FALSE:
					case Tokens.T_NULL:
					case Tokens.T_GET:
					case Tokens.T_SET:
					case Tokens.T_CALL:
                    case Tokens.T_CALLSTATIC:
					case Tokens.T_SLEEP:
					case Tokens.T_WAKEUP:
					case Tokens.T_TOSTRING:
					case Tokens.T_CONSTRUCT:
					case Tokens.T_DESTRUCT:
					case Tokens.T_PARENT:
					case Tokens.T_SELF:
					case Tokens.T_AUTOLOAD:
					case Tokens.T_PARTIAL:
						{
							if ((features & Features.ContextKeywords) == 0)
							{
								token = Tokens.T_STRING;
								goto case Tokens.T_STRING;
							}

							tokenCategory = TokenCategory.Keyword;
							return token;
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
							if ((features & Features.TypeKeywords) == 0)
							{
								token = Tokens.T_STRING;
								goto case Tokens.T_STRING;
							}

							tokenCategory = TokenCategory.Keyword;
							return token;
						}

                    case Tokens.T_GOTO:
                    case Tokens.T_TRY:
					case Tokens.T_CATCH:
                    case Tokens.T_FINALLY:
                    case Tokens.T_THROW:
					case Tokens.T_INTERFACE:
					case Tokens.T_IMPLEMENTS:
					case Tokens.T_CLONE:
					case Tokens.T_ABSTRACT:
					case Tokens.T_FINAL:
					case Tokens.T_PRIVATE:
					case Tokens.T_PROTECTED:
					case Tokens.T_PUBLIC:
					case Tokens.T_INSTANCEOF:
                    case Tokens.T_NAMESPACE:
                    case Tokens.T_NAMESPACE_C:
                    case Tokens.T_USE:
                        {
							if ((features & Features.V5Keywords) == 0)
							{
								token = Tokens.T_STRING;
								goto case Tokens.T_STRING;
							}

							tokenCategory = TokenCategory.Keyword;
							return token;
						}

					case Tokens.T_IMPORT:
                        {
                            //if ((features & Features.V6Keywords) == 0)
                            //{
                            //    token = Tokens.T_STRING;
                            //    goto case Tokens.T_STRING;
                            //}

                            tokenCategory = TokenCategory.Keyword;
                            return token;
                        }

					#endregion

					#region Basic Keywords

					case Tokens.T_REQUIRE_ONCE:
					case Tokens.T_REQUIRE:
					case Tokens.T_EVAL:
					case Tokens.T_INCLUDE_ONCE:
					case Tokens.T_INCLUDE:
					case Tokens.T_LOGICAL_OR:           // or
					case Tokens.T_LOGICAL_XOR:          // xor
					case Tokens.T_LOGICAL_AND:          // and
					case Tokens.T_PRINT:
					case Tokens.T_NEW:
					case Tokens.T_EXIT:
					case Tokens.T_IF:
					case Tokens.T_ELSEIF:
					case Tokens.T_ELSE:
					case Tokens.T_ENDIF:
					case Tokens.T_ECHO:
					case Tokens.T_DO:
					case Tokens.T_WHILE:
					case Tokens.T_ENDWHILE:
					case Tokens.T_FOR:
					case Tokens.T_ENDFOR:
					case Tokens.T_FOREACH:
					case Tokens.T_ENDFOREACH:
					case Tokens.T_AS:
					case Tokens.T_SWITCH:
					case Tokens.T_ENDSWITCH:
					case Tokens.T_CASE:
					case Tokens.T_DEFAULT:
					case Tokens.T_BREAK:
					case Tokens.T_CONTINUE:
					case Tokens.T_FUNCTION:
					case Tokens.T_CONST:
					case Tokens.T_RETURN:
                    case Tokens.T_YIELD:
					case Tokens.T_GLOBAL:
					case Tokens.T_STATIC:
					case Tokens.T_VAR:
					case Tokens.T_UNSET:
					case Tokens.T_ISSET:
					case Tokens.T_EMPTY:
					case Tokens.T_CLASS:
                    case Tokens.T_TRAIT:
                    case Tokens.T_INSTEADOF:
					case Tokens.T_EXTENDS:
					case Tokens.T_LIST:
					case Tokens.T_ARRAY:
					case Tokens.T_CLASS_C:              // __CLASS__
                    case Tokens.T_TRAIT_C:              // __TRAIT__
					case Tokens.T_METHOD_C:             // __METHOD__
					case Tokens.T_FUNC_C:               // __FUNCTION__
					case Tokens.T_FILE:                 // __FILE__
					case Tokens.T_LINE:                 // __LINE__
                    case Tokens.T_DIR:                  // __DIR__
                    case Tokens.T_CALLABLE:             // callable
						tokenCategory = TokenCategory.Keyword;
						return token;

					#endregion

					#region Operators

					case Tokens.T_UNSET_CAST:           // (unset)
					case Tokens.T_BOOL_CAST:            // (bool)
					case Tokens.T_OBJECT_CAST:          // (object)
					case Tokens.T_ARRAY_CAST:           // (array)
					case Tokens.T_STRING_CAST:          // (string)
					case Tokens.T_UNICODE_CAST:			// (unicode)
                    case Tokens.T_BINARY_CAST:          // (binary)
					case Tokens.T_DOUBLE_CAST:          // (double)
					case Tokens.T_FLOAT_CAST:           // (float)
					case Tokens.T_INT_CAST:             // (int)
					case Tokens.T_AT:                   // @
					case Tokens.T_QUESTION:             // ?
					case Tokens.T_LT:                   // <
					case Tokens.T_GT:                   // >
					case Tokens.T_PERCENT:              // %
					case Tokens.T_EXCLAM:               // !
					case Tokens.T_TILDE:                // ~
					case Tokens.T_EQ:                   // =
					case Tokens.T_SLASH:                // /
					case Tokens.T_CARET:                // ^
					case Tokens.T_AMP:                  // &
					case Tokens.T_PLUS:                 // +
					case Tokens.T_MINUS:                // -
					case Tokens.T_PIPE:                 // |
					case Tokens.T_MUL:                  // *
                    case Tokens.T_POW:                  // **
					case Tokens.T_DOT:                  // .
					case Tokens.T_SR_EQUAL:             // >>=
					case Tokens.T_SL_EQUAL:             // <<=
					case Tokens.T_XOR_EQUAL:            // ^=
					case Tokens.T_OR_EQUAL:             // |=
					case Tokens.T_AND_EQUAL:            // &=
					case Tokens.T_MOD_EQUAL:            // %=
					case Tokens.T_CONCAT_EQUAL:         // .=
					case Tokens.T_DIV_EQUAL:            // /=
					case Tokens.T_MUL_EQUAL:            // *=
                    case Tokens.T_POW_EQUAL:            // **=
					case Tokens.T_MINUS_EQUAL:          // -=
					case Tokens.T_PLUS_EQUAL:           // +=
					case Tokens.T_BOOLEAN_OR:           // ||      
					case Tokens.T_BOOLEAN_AND:          // &&
					case Tokens.T_IS_NOT_IDENTICAL:     // !==
					case Tokens.T_IS_IDENTICAL:         // ===
					case Tokens.T_IS_NOT_EQUAL:         // !=
					case Tokens.T_IS_EQUAL:             // ==
					case Tokens.T_IS_GREATER_OR_EQUAL:  // >=
					case Tokens.T_IS_SMALLER_OR_EQUAL:  // <=
					case Tokens.T_SR:                   // >>
					case Tokens.T_SL:                   // <<
					case Tokens.T_DEC:                  // --
					case Tokens.T_INC:                  // ++
					case Tokens.T_DOUBLE_COLON:         // ::
					case Tokens.T_COLON:                // :
					case Tokens.T_DOUBLE_ARROW:         // =>
                    case Tokens.T_ELLIPSIS:             // ...
						tokenCategory = TokenCategory.Operator;
						return token;

					#endregion

					#region Others

					case Tokens.T_LPAREN:                       // (
					case Tokens.T_RPAREN:                       // )
					case Tokens.T_LGENERIC:                     // <:
					case Tokens.T_RGENERIC:                     // :>
					case Tokens.T_SEMI:                         // ;
					case Tokens.T_COMMA:                        // ,
                    case Tokens.T_NS_SEPARATOR:                 // \
                        tokenCategory = TokenCategory.Delimiter;
						return token;

					//case Tokens.T_NAMESPACE_NAME:               // namespace name
					case Tokens.T_STRING_VARNAME:               // identifier following encapsulated "${"
						tokenCategory = TokenCategory.Identifier;
						return token;

					case Tokens.T_DNUMBER:                      // double (or overflown integer) out of string 
					case Tokens.T_LNUMBER:                      // integer (or hex integer) out of string
					case Tokens.T_L64NUMBER:                    // long integer - overflown integer (or hex long) out of string
						tokenCategory = TokenCategory.Number;
						return token;

					case Tokens.T_DOUBLE_QUOTES:                // "
					case Tokens.T_BINARY_DOUBLE:                // b"
					case Tokens.T_BACKQUOTE:                    // `
					case Tokens.T_START_HEREDOC:                // <<<XXX
					case Tokens.T_BINARY_HEREDOC:               // b<<<XXX
					case Tokens.T_END_HEREDOC:                  // XXX
					case Tokens.T_CHARACTER:                    // character(s) in string
					case Tokens.T_ENCAPSED_AND_WHITESPACE:      // character(s) in string
					case Tokens.T_CONSTANT_ENCAPSED_STRING:     // quoted string not containing '$' 
					case Tokens.T_BAD_CHARACTER:                // incorrectly slashed character in string
					case Tokens.T_NUM_STRING:                   // number in string
                        tokenCategory = TokenCategory.String;
						return token;

                    case Tokens.T_DOLLAR_OPEN_CURLY_BRACES:     // "${" in string - starts non-string code
                    case Tokens.T_CURLY_OPEN:                   // "{$" in string
						tokenCategory = TokenCategory.StringCode;
						return token;

					case Tokens.T_WHITESPACE:
						if ((features & Features.SkipWhitespace) != 0) break;
						tokenCategory = TokenCategory.WhiteSpace;
						return token;

					case Tokens.T_COMMENT:
					case Tokens.T_DOC_COMMENT:
						tokenCategory = TokenCategory.Comment;
						return token;

					case Tokens.T_LINE_COMMENT:
						tokenCategory = TokenCategory.LineComment;
						return Tokens.T_COMMENT;

					case Tokens.T_PRAGMA_FILE:
					case Tokens.T_PRAGMA_LINE:
					case Tokens.T_PRAGMA_DEFAULT_FILE:
					case Tokens.T_PRAGMA_DEFAULT_LINE:
						tokenCategory = TokenCategory.LineComment;
						return token;

					case Tokens.T_OPEN_TAG:
					case Tokens.T_OPEN_TAG_WITH_ECHO:
					case Tokens.T_CLOSE_TAG:
						tokenCategory = TokenCategory.ScriptTags;
						return token;

					case Tokens.T_INLINE_HTML:
						tokenCategory = TokenCategory.Html;
						return token;

					#endregion

					#region Tokens with Ambiguous Category

					case Tokens.T_LBRACKET:                     // [
					case Tokens.T_RBRACKET:                     // ]
					case Tokens.T_LBRACE:                       // {
                        tokenCategory = (inString) ? TokenCategory.String : TokenCategory.Delimiter;
                        return token;

                    case Tokens.T_RBRACE:                       // }
                        if (inString)
                            // we are in string:
                            tokenCategory = TokenCategory.String;
                        else if (CurrentLexicalState == LexicalStates.ST_DOUBLE_QUOTES || CurrentLexicalState == LexicalStates.ST_BACKQUOTE || CurrentLexicalState == LexicalStates.ST_HEREDOC)
                            // right brace can complete ${ or {$,
                            // so we are returning from other state to string state:
                            tokenCategory = TokenCategory.StringCode;
                        else
                            // part of script:
                            tokenCategory = TokenCategory.Delimiter;

                        return token;

					case Tokens.T_STRING:                       // identifier
						tokenCategory = (inString) ? (isCode ? TokenCategory.StringCode : TokenCategory.String) : TokenCategory.Identifier;
						return token;

					case Tokens.T_DOLLAR:                       // isolated '$'
					case Tokens.T_OBJECT_OPERATOR:              // ->
						tokenCategory = (inString) ? TokenCategory.StringCode : TokenCategory.Operator;
						return token;

					case Tokens.T_VARIABLE:                     // identifier
						tokenCategory = (inString) ? TokenCategory.StringCode : TokenCategory.Variable;
						return token;

					#endregion

					default:
						Debug.Assert(false, "Unknown token '" + token + "'");
						return token;
				}
			}
		}
	}
}