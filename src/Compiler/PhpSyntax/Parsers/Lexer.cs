using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Globalization;

namespace Pchp.Syntax.Parsers
{
    #region PhpStringBuilder

    /// <summary>
    /// The PHP-semantic string builder. Binary or Unicode string builder.
    /// </summary>
    internal class PhpStringBuilder
    {
        #region Fields & Properties
        /// <summary>
        /// Currently used encoding.
        /// </summary>
        private readonly Encoding/*!*/encoding;

        private readonly byte[] encodeBytes = new byte[4];
        private readonly char[] encodeChars = new char[5];

        private StringBuilder _unicodeBuilder;
        private List<byte> _binaryBuilder;

        private bool IsUnicode { get { return !IsBinary; } }
        private bool IsBinary { get { return _binaryBuilder != null; } }

        private Text.Span span;

        /// <summary>
        /// Length of contained data (string or byte[]).
        /// </summary>
        public int Length
        {
            get
            {
                if (_unicodeBuilder != null)
                    return _unicodeBuilder.Length;
                else if (_binaryBuilder != null)
                    return _binaryBuilder.Count;
                else
                    return 0;
            }
        }

        private StringBuilder UnicodeBuilder
        {
            get
            {
                if (_unicodeBuilder == null)
                {
                    if (_binaryBuilder != null && _binaryBuilder.Count > 0)
                    {
                        byte[] bytes = _binaryBuilder.ToArray();
                        _unicodeBuilder = new StringBuilder(encoding.GetString(bytes,0,bytes.Length ));
                    }
                    else
                    {
                        _unicodeBuilder = new StringBuilder();
                    }
                    _binaryBuilder = null;
                }

                return _unicodeBuilder;
            }
        }

        private List<byte> BinaryBuilder
        {
            get
            {
                if (_binaryBuilder == null)
                {
                    if (_unicodeBuilder != null && _unicodeBuilder.Length > 0)
                    {
                        _binaryBuilder = new List<byte>(encoding.GetBytes(_unicodeBuilder.ToString()));
                    }
                    else
                    {
                        _binaryBuilder = new List<byte>();
                    }
                    _unicodeBuilder = null;
                }

                return _binaryBuilder;
            }
        }

        #endregion

        #region Results

        /// <summary>
        /// The result of builder: String or byte[].
        /// </summary>
        public object Result
        {
            get
            {
                if (IsBinary)
                    return BinaryBuilder.ToArray();
                else
                    return UnicodeBuilder.ToString();
            }
        }

        public Pchp.Syntax.AST.Literal CreateLiteral()
        {
            if (IsBinary)
                return new Pchp.Syntax.AST.BinaryStringLiteral(span, BinaryBuilder.ToArray());
            else
                return new Pchp.Syntax.AST.StringLiteral(span, UnicodeBuilder.ToString());
        }

        #endregion

        #region Construct

        /// <summary>
        /// Initialize the PhpStringBuilder.
        /// </summary>
        /// <param name="encoding"></param>
        /// <param name="binary"></param>
        /// <param name="initialLength"></param>
        public PhpStringBuilder(Encoding/*!*/encoding, bool binary, int initialLength)
        {
            Debug.Assert(encoding != null);

            this.encoding = encoding;
            this.span = Text.Span.Invalid;

            //if (binary)
            //    _binaryBuilder = new List<byte>(initialLength);
            //else
                _unicodeBuilder = new StringBuilder(initialLength);
        }

        public PhpStringBuilder(Encoding/*!*/encoding, string/*!*/value, Text.Span span)
            :this(encoding, false, value.Length)
        {
            Append(value, span);
        }

        #endregion

        #region Append

        private void Append(Text.Span span)
        {
            if (this.span.IsValid)
            {
                if (span.IsValid)
                    this.span = Text.Span.Combine(this.span, span);
            }
            else
            {
                this.span = span;
            }
        }

        public void Append(string str, Text.Span span)
        {
            Append(span);
            Append(str);
        }
        public void Append(string str)
        {
            if (IsUnicode)
                UnicodeBuilder.Append(str);
            else
            {
                BinaryBuilder.AddRange(encoding.GetBytes(str));
            }
        }

        public void Append(char c, Text.Span span)
        {
            Append(span);
            Append(c);
        }
        public void Append(char c)
        {
            if (IsUnicode)
                UnicodeBuilder.Append(c);
            else
            {
                encodeChars[0] = c;
                int count = encoding.GetBytes(encodeChars, 0, 1, encodeBytes, 0);
                for (int i = 0; i < count; ++i)
                    BinaryBuilder.Add(encodeBytes[i]);
            }
        }

        public void Append(byte b, Text.Span span)
        {
            Append(span);
            Append(b);
        }
        public void Append(byte b)
        {
            // force binary string

            if (IsUnicode)
            {
                encodeBytes[0] = b;
                UnicodeBuilder.Append(encodeChars, 0, encoding.GetChars(encodeBytes, 0, 1, encodeChars, 0));
            }
            else
                BinaryBuilder.Add(b);
        }

        public void Append(int c, Text.Span span)
        {
            Append(span);
            Append(c);
        }
        public void Append(int c)
        {
            Debug.Assert(c >= 0);

            //if (c <= 0xff)
            if (IsBinary)
                BinaryBuilder.Add((byte)c);
            else
                UnicodeBuilder.Append((char)c);
        }

        #endregion

        #region Misc

        /// <summary>
        /// Trim ending /r/n or /n characters. Assuming the string ends with /n.
        /// </summary>
        internal void TrimEoln()
        {
            if (IsUnicode)
            {
                if (UnicodeBuilder.Length > 0)
                {
                    if (UnicodeBuilder.Length >= 2 && UnicodeBuilder[UnicodeBuilder.Length - 2] == '\r')
                    {
                        // trim ending \r\n:
                        UnicodeBuilder.Length -= 2;
                    }
                    else
                    {
                        // trim ending \n:
                        UnicodeBuilder.Length -= 1;
                    }
                }
            }
            else
            {
                if (BinaryBuilder.Count > 0)
                {
                    if (BinaryBuilder.Count >= 2 && BinaryBuilder[BinaryBuilder.Count - 2] == (byte)'\r')
                    {
                        BinaryBuilder.RemoveRange(BinaryBuilder.Count - 2, 2);
                    }
                    else
                    {
                        BinaryBuilder.RemoveAt(BinaryBuilder.Count - 1);
                    }
                }
            }
            
        }

        #endregion
    }

    #endregion

    public partial class Lexer
	{
		protected bool AllowAspTags = true;
        protected bool AllowShortTags = true;

		/// <summary>
		/// Whether tokens T_STRING, T_VARIABLE, '[', ']', '{', '}', '$', "->" are encapsulated in a string.
		/// </summary>
		protected bool inString;

		/// <summary>
		/// Whether T_STRING token should be treated as a string code token and not a plain string token.
		/// </summary>
		protected bool isCode;

		public bool InUnicodeString { get { return inUnicodeString; } set { inUnicodeString = true; } }
		private bool inUnicodeString = false;

		protected string hereDocLabel = null;
		protected Stack<LexicalStates> StateStack { get { return stateStack; } set { stateStack = value; } }

        protected void _yymore() { yymore(); }

		#region Token Buffer Interpretation

		public int GetTokenByteLength(Encoding/*!*/ encoding)
		{
			return encoding.GetByteCount(buffer, token_start, token_end - token_start);
		}

        protected char[] Buffer { get { return buffer; } }
        protected int BufferTokenStart { get { return token_start; } }

        protected char GetTokenChar(int index)
		{
			return buffer[token_start + index];
		}

		protected string GetTokenString()
		{
			return new String(buffer, token_start, token_end - token_start);
		}

        protected string GetTokenChunkString()
		{
			return new String(buffer, token_chunk_start, token_end - token_chunk_start);
		}

		protected string GetTokenSubstring(int startIndex)
		{
			return new String(buffer, token_start + startIndex, token_end - token_start - startIndex);
		}

		protected string GetTokenSubstring(int startIndex, int length)
		{
			return new String(buffer, token_start + startIndex, length);
		}

		protected void AppendTokenTextTo(StringBuilder/*!*/ builder)
		{
			builder.Append(buffer, token_start, token_end - token_start);
		}

		/// <summary>
		/// Checks whether a specified heredoc lebel exactly matches {LABEL} in ^{LABEL}(";")?{NEWLINE}.
		/// </summary>
		private bool IsCurrentHeredocEnd(int startIndex)
		{
			int i = StringUtils.FirstDifferent(buffer, token_start + startIndex, hereDocLabel, 0, false);
			return i == hereDocLabel.Length && (buffer[token_start + i] == ';' ||
				IsNewLineCharacter(buffer[token_start + i]));
		}

		protected char GetTokenAsEscapedCharacter(int startIndex)
		{
			Debug.Assert(GetTokenChar(startIndex) == '\\');
			char c;
			switch (c = GetTokenChar(startIndex + 1))
			{
				case 'n': return '\n';
				case 't': return '\t';
				case 'r': return '\r';
				default: return c;
			}
		}

		/// <summary>
		/// Checks whether {LNUM} fits to integer, long or double 
		/// and returns appropriate value from Tokens enum.
		/// </summary>
		protected Tokens GetIntegerTokenType(int startIndex)
		{
			int i = token_start + startIndex;
			while (i < token_end && buffer[i] == '0') i++;

			int number_length = token_end - i;
			if (i != token_start + startIndex)
			{
				// starts with zero - octal
				// similar to GetHexIntegerTokenType code
				if ((number_length < 11) || (number_length == 11 && buffer[i] == '1'))
					return Tokens.T_LNUMBER;
				if (number_length < 22) 
					return Tokens.T_L64NUMBER;
				return Tokens.T_DNUMBER;
			}
			else
			{
				// decimal
				if (number_length < 10)
					return Tokens.T_LNUMBER;
				if (number_length > 19)
					return Tokens.T_DNUMBER;
				if (number_length >= 11 && number_length <= 18)
					return Tokens.T_L64NUMBER;

				// can't easily check for numbers of different length
				SemanticValueType val = default(SemanticValueType);
				return GetTokenAsDecimalNumber(startIndex, 10, ref val);				
			}
		}

		/// <summary>
		/// Checks whether {HNUM} fits to integer, long or double 
		/// and returns appropriate value from Tokens enum.
		/// </summary>
		protected Tokens GetHexIntegerTokenType(int startIndex)
		{
			// 0xffffffff no
			// 0x7fffffff yes
			int i = token_start + startIndex;
			while (i < token_end && buffer[i] == '0') i++;

			// returns T_LNUMBER when: length without zeros is less than 8
			// or equals 8 and first non-zero character is less than '8'
			if ((token_end - i < 8) || ((token_end - i == 8) && buffer[i] >= '0' && buffer[i] < '8'))
				return Tokens.T_LNUMBER;

			// similar for long
			if ((token_end - i < 16) || ((token_end - i == 16) && buffer[i] >= '0' && buffer[i] < '8'))
				return Tokens.T_L64NUMBER;

			return Tokens.T_DNUMBER;
		}

		// base == 10: [0-9]*
		// base == 16: [0-9A-Fa-f]*
		// assuming result < max int
		protected int GetTokenAsInteger(int startIndex, int @base)
		{
			int result = 0;
			int buffer_pos = token_start + startIndex;

			for (; ; )
			{
				int digit = Convert.AlphaNumericToDigit(buffer[buffer_pos]);
				if (digit >= @base) break;

				result = result * @base + digit;
				buffer_pos++;
			}

			return result;
		}


		/// <summary>
		/// Reads token as a number (accepts tokens with any reasonable base [0-9a-zA-Z]*).
		/// Parsed value is stored in <paramref name="val"/> as integer (when value is less than MaxInt),
		/// as Long (when value is less then MaxLong) or as double.
		/// </summary>
        /// <param name="startIndex">Starting read position of the token.</param>
        /// <param name="base">The base of the number.</param>
		/// <param name="val">Parsed value is stored in this union</param>
		/// <returns>Returns one of T_LNUMBER (int), T_L64NUMBER (long) or T_DNUMBER (double)</returns>
		protected Tokens GetTokenAsDecimalNumber(int startIndex, int @base, ref SemanticValueType val)
		{
			long lresult = 0;
			double dresult = 0;

			int digit;
			int buffer_pos = token_start + startIndex;

			// try to parse INT value
			// most of the literals are parsed using the following loop
			while (buffer_pos < buffer.Length && (digit = Convert.AlphaNumericToDigit(buffer[buffer_pos])) < @base && lresult <= Int32.MaxValue)
			{
				lresult = lresult * @base + digit;
				buffer_pos++;
			}

			if (lresult > Int32.MaxValue)
			{
				// try to parse LONG value (check for the overflow and if it occurs converts data to double)
				bool longOverflow = false;
				while (buffer_pos < buffer.Length && (digit = Convert.AlphaNumericToDigit(buffer[buffer_pos])) < @base)
				{
					try
					{
						lresult = checked(lresult * @base + digit);
					}
					catch (OverflowException)
					{ 
						longOverflow = true; break; 
					}
					buffer_pos++;
				}

				if (longOverflow)
				{
					// too big for LONG - use double
					dresult = (double)lresult;
					while (buffer_pos < buffer.Length && (digit = Convert.AlphaNumericToDigit(buffer[buffer_pos])) < @base)
					{
						dresult = dresult * @base + digit;
						buffer_pos++;
					}
					val.Double = dresult;
					return Tokens.T_DNUMBER;
				}
				else
				{
					val.Long = lresult;
					return Tokens.T_L64NUMBER;
				}
			}
			else
			{
				val.Integer = (int)lresult;
				return Tokens.T_LNUMBER;
			}
		}


		// [0-9]*[.][0-9]+
		// [0-9]+[.][0-9]*
		// [0-9]*[.][0-9]+[eE][+-]?[0-9]+
		// [0-9]+[.][0-9]*[eE][+-]?[0-9]+
		// [0-9]+[eE][+-]?[0-9]+
		protected double GetTokenAsDouble(int startIndex)
		{
            string str = new string(buffer, token_start, token_end - token_start);

            try
            {
                return double.Parse(
                    str,
                    NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent,
                    CultureInfo.InvariantCulture);
            }
            catch (OverflowException)
            {
                return (str.Length > 0 && str[0] == '-') ? double.NegativeInfinity : double.PositiveInfinity;
            }
		}

        //protected void GetTokenAsQualifiedName(int startIndex, List<string>/*!*/ result)
        //{
        //    Debug.Assert(result != null);

        //    int current_name = token_start + startIndex;
        //    int next_separator = token_start + startIndex;

        //    for (; ; )
        //    {
        //        while (next_separator < token_end && buffer[next_separator] != '\\')
        //            next_separator++;

        //        if (next_separator == token_end) break;

        //        result.Add(new String(buffer, current_name, next_separator - current_name));
        //        next_separator += QualifiedName.Separator.ToString().Length;
        //        current_name = next_separator;
        //    }

        //    // last item:
        //    result.Add(new String(buffer, current_name, token_end - current_name));
        //}

        #region GetTokenAs*QuotedString

        protected object GetTokenAsDoublyQuotedString(int startIndex, Encoding/*!*/ encoding, bool forceBinaryString)
		{
            PhpStringBuilder result = new PhpStringBuilder(encoding, forceBinaryString, TokenLength);

			int buffer_pos = token_start + startIndex + 1;
            
			// the following loops expect the token ending by "
			Debug.Assert(buffer[buffer_pos - 1] == '"' && buffer[token_end - 1] == '"');

			//StringBuilder result = new StringBuilder(TokenLength);

            char c;
            while ((c = buffer[buffer_pos++]) != '"')
			{
				if (c == '\\')
				{
					switch (c = buffer[buffer_pos++])
					{
						case 'n':
							result.Append('\n');
							break;

						case 'r':
							result.Append('\r');
							break;

						case 't':
							result.Append('\t');
							break;

						case '\\':
						case '$':
						case '"':
							result.Append(c);
							break;

						case 'C':
							if (!inUnicodeString) goto default;
							result.Append(ParseCodePointName(ref buffer_pos));
							break;

						case 'u':
						case 'U':
							if (!inUnicodeString) goto default;
							result.Append(ParseCodePoint(c == 'u' ? 4 : 6, ref buffer_pos));
							break;

						case 'x':
							{
								int digit;
								if ((digit = Convert.AlphaNumericToDigit(buffer[buffer_pos])) < 16)
								{
									int hex_code = digit;
									buffer_pos++;
									if ((digit = Convert.AlphaNumericToDigit(buffer[buffer_pos])) < 16)
									{
										buffer_pos++;
										hex_code = (hex_code << 4) + digit;
									}

                                    //encodeBytes[0] = (byte)hex_code;
                                    //result.Append(encodeChars, 0, encoding.GetChars(encodeBytes, 0, 1, encodeChars, 0));
                                    result.Append((byte)hex_code);
								}
								else
								{
									result.Append('\\');
									result.Append('x');
								}
								break;
							}

						default:
							{
								int digit;
								if ((digit = Convert.NumericToDigit(c)) < 8)
								{
									int octal_code = digit;

									if ((digit = Convert.NumericToDigit(buffer[buffer_pos])) < 8)
									{
										octal_code = (octal_code << 3) + digit;
										buffer_pos++;

										if ((digit = Convert.NumericToDigit(buffer[buffer_pos])) < 8)
										{
											buffer_pos++;
											octal_code = (octal_code << 3) + digit;
										}
									}
                                    //encodeBytes[0] = (byte)octal_code;
                                    //result.Append(encodeChars, 0, encoding.GetChars(encodeBytes, 0, 1, encodeChars, 0));
                                    result.Append((byte)octal_code);
								}
								else
								{
									result.Append('\\');
									result.Append(c);
								}
								break;
							}
					}
				}
				else
				{
					result.Append(c);
				}
			}

			return result.Result;
		}

        protected object GetTokenAsSinglyQuotedString(int startIndex, Encoding/*!*/ encoding, bool forceBinaryString)
		{
            PhpStringBuilder result = new PhpStringBuilder(encoding, forceBinaryString, TokenLength);

			int buffer_pos = token_start + startIndex + 1;

			// the following loops expect the token ending by '
			Debug.Assert(buffer[buffer_pos - 1] == '\'' && buffer[token_end - 1] == '\'');

			//StringBuilder result = new StringBuilder(TokenLength);
			char c;

			while ((c = buffer[buffer_pos++]) != '\'')
			{
				if (c == '\\')
				{
					switch (c = buffer[buffer_pos++])
					{
						case '\\':
						case '\'':
							result.Append(c);
							break;

						// ??? will cause many problems ... but PHP allows this
						//case 'C':
						//  if (!inUnicodeString) goto default;
						//  result.Append(ParseCodePointName(ref buffer_pos));
						//  break;

						//case 'u':
						//case 'U':
						//  if (!inUnicodeString) goto default;
						//  result.Append(ParseCodePoint( c == 'u' ? 4 : 6, ref buffer_pos));
						//  break;

						default:
							result.Append('\\');
							result.Append(c);
							break;
					}
				}
				else
				{
					result.Append(c);
				}
			}

            return result.Result;
        }

        #endregion

        private string ParseCodePointName(ref int pos)
		{
			if (buffer[pos] == '{')
			{
				int start = ++pos;
				while (pos < token_end && buffer[pos] != '}') pos++;

				if (pos < token_end)
				{
					string name = new String(buffer, start, pos - start);

					// TODO: name look-up
					// return ...[name];

					// skip '}'
					pos++;
				}
			}

			//errors.Add(Errors.InvalidCodePointName, sourceFile, );

			return "?";
		}

		private string ParseCodePoint(int maxLength, ref int pos)
		{
			int digit;
			int code_point = 0;
			while (maxLength > 0 && (digit = Convert.NumericToDigit(buffer[pos])) < 16)
			{
				code_point = code_point << 4 + digit;
				pos++;
				maxLength--;
			}

			if (maxLength != 0)
			{
				// TODO: warning
			}

			try
			{
				if ((code_point < 0 || code_point > 0x10ffff) || (code_point >= 0xd800 && code_point <= 0xdfff))
				{
					// TODO: errors.Add(Errors.InvalidCodePoint, sourceFile, tokenPosition.Short, GetTokenString());
					return "?";
				}
				else
				{
					return StringUtils.Utf32ToString(code_point);
				}
			}
			catch (ArgumentOutOfRangeException)
			{
				// TODO: errors.Add(Errors.InvalidCodePoint, sourceFile, tokenPosition.Short, GetTokenString());
				return "?";
			}
		}

		private int GetPragmaValueStart(int directiveLength)
		{
			int buffer_pos = token_start + "#pragma".Length;
			Debug.Assert(new String(buffer, token_start, buffer_pos - token_start) == "#pragma");

			while (buffer[buffer_pos] == ' ' || buffer[buffer_pos] == '\t') buffer_pos++;

			buffer_pos += directiveLength;

			while (buffer[buffer_pos] == ' ' || buffer[buffer_pos] == '\t') buffer_pos++;

			return buffer_pos - token_start;
		}

		protected string GetTokenAsFilePragma()
		{
			int start_offset = GetPragmaValueStart("file".Length);
			int end = token_end - 1;

			while (end > 0 && Char.IsWhiteSpace(buffer[end])) end--;

			return GetTokenSubstring(start_offset, end - token_start - start_offset + 1);
		}

		protected int? GetTokenAsLinePragma()
		{
			int start_offset = GetPragmaValueStart("line".Length);

			int sign = +1;
			
			if (GetTokenChar(start_offset) == '-')
			{
				sign = -1;
				start_offset++;
			}

			// TP_COMMENT: modified call to GetTokenAsDecimalNumber
			SemanticValueType val = default(SemanticValueType);
			Tokens ret = GetTokenAsDecimalNumber(start_offset, 10, ref val);

			// multiplication cannot overflow as ivalue >= 0
			return (ret!=Tokens.T_LNUMBER) ? null : (int?)(val.Integer * sign);
		}

		#endregion

		private char Map(char c)
		{
			return (c > SByte.MaxValue) ? 'a' : c;
		}

		/*
		 * #region Unit Test
				#if DEBUG

				[Test]
				static void Test2()
				{
					Debug.Assert(IsInteger("000000000000000001"));
					Debug.Assert(IsInteger("0000"));
					Debug.Assert(IsInteger("0"));
					Debug.Assert(IsInteger("2147483647"));
					Debug.Assert(!IsInteger("2147483648"));
					Debug.Assert(!IsInteger("2147483648999999999999999999999999999999999999"));

					Debug.Assert(IsHexInteger("0x00000000000001"));
					Debug.Assert(IsHexInteger("0x00000"));
					Debug.Assert(IsHexInteger("0x"));
					Debug.Assert(!IsHexInteger("0x0012ABC67891"));
					Debug.Assert(IsHexInteger("0xFFFFFFFF"));
					Debug.Assert(!IsHexInteger("0x100000000"));

					Debug.Assert(HereDocLabelsEqual("EOT", "EOT;\n"));
					Debug.Assert(!HereDocLabelsEqual("EOT", "EOt\n"));
					Debug.Assert(!HereDocLabelsEqual("EOT", "EOTT;\n"));
					Debug.Assert(!HereDocLabelsEqual("EOT", "EOTT\n"));
					Debug.Assert(!HereDocLabelsEqual("EOTX", "EOT\r"));
					Debug.Assert(!HereDocLabelsEqual("EOTXYZ", "EOT\r"));
				}

				#endif
				#endregion
		*/
	}
}
