namespace Pchp.Library.Json
{
	#region User Code
	
	using System;
using System.Text;
using System.Collections.Generic;
#endregion
	
	
	internal partial class Lexer
	{
		public enum LexicalStates
		{
			INITIAL = 0,
			IN_STRING = 1,
		}
		
		[Flags]
		private enum AcceptConditions : byte
		{
			NotAccept = 0,
			AcceptOnStart = 1,
			AcceptOnEnd = 2,
			Accept = 4
		}
		
		private const int NoState = -1;
		private const char BOL = (char)128;
		private const char EOF = (char)129;
		
		private Tokens yyreturn;
		// content of the STRING literal text
		protected string yytext()
		{
			return new String(buffer, token_start, token_end - token_start);
		}
		private StringBuilder str = null;
		protected string QuotedStringContent{get{return str.ToString();}}
		
		private System.IO.TextReader reader;
		private char[] buffer = new char[512];
		
		// whether the currently parsed token is being expanded (yymore has been called):
		private bool expanding_token;
		
		// offset in buffer where the currently parsed token starts:
		private int token_start;
		
		// offset in buffer where the currently parsed token chunk starts:
		private int token_chunk_start;
		
		// offset in buffer one char behind the currently parsed token (chunk) ending character:
		private int token_end;
		
		// offset of the lookahead character (number of characters parsed):
		private int lookahead_index;
		
		// number of characters read into the buffer:
		private int chars_read;
		
		private bool yy_at_bol = false;
		
		public LexicalStates CurrentLexicalState { get { return current_lexical_state; } set { current_lexical_state = value; } } 
		private LexicalStates current_lexical_state;
		
		public Lexer(System.IO.TextReader reader)
		{
			Initialize(reader, LexicalStates.INITIAL);
		}
		
		public void Initialize(System.IO.TextReader reader, LexicalStates lexicalState, bool atBol)
		{
			this.expanding_token = false;
			this.token_start = 0;
			this.chars_read = 0;
			this.lookahead_index = 0;
			this.token_chunk_start = 0;
			this.token_end = 0;
			this.token_end_pos = new Position(0);
			this.reader = reader;
			this.yy_at_bol = atBol;
			this.current_lexical_state = lexicalState;
		}
		
		public void Initialize(System.IO.TextReader reader, LexicalStates lexicalState)
		{
			Initialize(reader, lexicalState, false);
		}
		
		#region Accept
		
		#pragma warning disable 162
		
		
		Tokens Accept0(int state,out bool accepted)
		{
			accepted = true;
			
			switch(state)
			{
				case 2:
					// #line 54
					{return Tokens.ARRAY_OPEN;}
					break;
					
				case 3:
					// #line 55
					{return Tokens.ARRAY_CLOSE;}
					break;
					
				case 4:
					// #line 56
					{return Tokens.ITEMS_SEPARATOR;}
					break;
					
				case 5:
					// #line 57
					{return Tokens.NAMEVALUE_SEPARATOR;}
					break;
					
				case 6:
					// #line 58
					{return Tokens.OBJECT_OPEN;}
					break;
					
				case 7:
					// #line 59
					{return Tokens.OBJECT_CLOSE;}
					break;
					
				case 8:
					// #line 64
					{return Tokens.INTEGER;}
					break;
					
				case 9:
					// #line 65
					{}
					break;
					
				case 10:
					// #line 67
					{BEGIN(LexicalStates.IN_STRING); str = new StringBuilder(); return Tokens.STRING_BEGIN;}
					break;
					
				case 11:
					// #line 63
					{return Tokens.DOUBLE;}
					break;
					
				case 12:
					// #line 60
					{return Tokens.TRUE;}
					break;
					
				case 13:
					// #line 62
					{return Tokens.NULL;}
					break;
					
				case 14:
					// #line 61
					{return Tokens.FALSE;}
					break;
					
				case 15:
					// #line 68
					{str.Append(yytext()); return Tokens.CHARS;}
					break;
					
				case 16:
					// #line 78
					{BEGIN(LexicalStates.INITIAL); return Tokens.STRING_END;}
					break;
					
				case 17:
					// #line 74
					{str.Append('\t'); return Tokens.ESCAPEDCHAR;}
					break;
					
				case 18:
					// #line 72
					{str.Append('\r'); return Tokens.ESCAPEDCHAR;}
					break;
					
				case 19:
					// #line 70
					{str.Append('\f'); return Tokens.ESCAPEDCHAR;}
					break;
					
				case 20:
					// #line 73
					{str.Append('\n'); return Tokens.ESCAPEDCHAR;}
					break;
					
				case 21:
					// #line 77
					{str.Append('"'); return Tokens.ESCAPEDCHAR;}
					break;
					
				case 22:
					// #line 75
					{str.Append('\\'); return Tokens.ESCAPEDCHAR;}
					break;
					
				case 23:
					// #line 71
					{str.Append('\b'); return Tokens.ESCAPEDCHAR;}
					break;
					
				case 24:
					// #line 76
					{str.Append('/'); return Tokens.ESCAPEDCHAR;}
					break;
					
				case 25:
					// #line 69
					{str.Append((char)int.Parse(yytext().Substring(2), System.Globalization.NumberStyles.HexNumber)); return Tokens.UNICODECHAR;}
					break;
					
				case 27: goto case 11;
			}
			accepted = false;
			return yyreturn;
		}
		
		#pragma warning restore 162
		
		
		#endregion
		private void BEGIN(LexicalStates state)
		{
			current_lexical_state = state;
		}
		
		private char Advance()
		{
			if (lookahead_index >= chars_read)
			{
				if (token_start > 0)
				{
					// shift buffer left:
					int length = chars_read - token_start;
					System.Buffer.BlockCopy(buffer, token_start << 1, buffer, 0, length << 1);
					token_end -= token_start;
					token_chunk_start -= token_start;
					token_start = 0;
					chars_read = lookahead_index = length;
					
					// populate the remaining bytes:
					int count = reader.Read(buffer, chars_read, buffer.Length - chars_read);
					if (count <= 0) return EOF;
					
					chars_read += count;
				}
				
				while (lookahead_index >= chars_read)
				{
					if (lookahead_index >= buffer.Length)
						buffer = ResizeBuffer(buffer);
					
					int count = reader.Read(buffer, chars_read, buffer.Length - chars_read);
					if (count <= 0) return EOF;
					chars_read += count;
				}
			}
			
			return Map(buffer[lookahead_index++]);
		}
		
		private char[] ResizeBuffer(char[] buf)
		{
			char[] result = new char[buf.Length << 1];
			System.Buffer.BlockCopy(buf, 0, result, 0, buf.Length << 1);
			return result;
		}
		
		
		protected static bool IsNewLineCharacter(char ch)
		{
		    return ch == '\r' || ch == '\n' || ch == (char)0x2028 || ch == (char)0x2029;
		}
		private void TrimTokenEnd()
		{
			if (token_end > token_chunk_start && buffer[token_end - 1] == '\n')
				token_end--;
			if (token_end > token_chunk_start && buffer[token_end - 1] == '\r')
				token_end--;
			}
		
		private void MarkTokenChunkStart()
		{
			token_chunk_start = lookahead_index;
		}
		
		private void MarkTokenEnd()
		{
			token_end = lookahead_index;
		}
		
		private void MoveToTokenEnd()
		{
			lookahead_index = token_end;
			yy_at_bol = (token_end > token_chunk_start) && (buffer[token_end - 1] == '\r' || buffer[token_end - 1] == '\n');
		}
		
		public int TokenLength
		{
			get { return token_end - token_start; }
		}
		
		public int TokenChunkLength
		{
			get { return token_end - token_chunk_start; }
		}
		
		private void yymore()
		{
			if (!expanding_token)
			{
				token_start = token_chunk_start;
				expanding_token = true;
			}
		}
		
		private void yyless(int count)
		{
			lookahead_index = token_end = token_chunk_start + count;
		}
		
		private Stack<LexicalStates> stateStack = new Stack<LexicalStates>(20);
		
		private void yy_push_state(LexicalStates state)
		{
			stateStack.Push(current_lexical_state);
			current_lexical_state = state;
		}
		
		private bool yy_pop_state()
		{
			if (stateStack.Count == 0) return false;
			current_lexical_state = stateStack.Pop();
			return true;
		}
		
		private LexicalStates yy_top_state()
		{
			return stateStack.Peek();
		}
		
		#region Tables
		
		private static AcceptConditions[] acceptCondition = new AcceptConditions[]
		{
			AcceptConditions.NotAccept, // 0
			AcceptConditions.Accept, // 1
			AcceptConditions.Accept, // 2
			AcceptConditions.Accept, // 3
			AcceptConditions.Accept, // 4
			AcceptConditions.Accept, // 5
			AcceptConditions.Accept, // 6
			AcceptConditions.Accept, // 7
			AcceptConditions.Accept, // 8
			AcceptConditions.Accept, // 9
			AcceptConditions.Accept, // 10
			AcceptConditions.Accept, // 11
			AcceptConditions.Accept, // 12
			AcceptConditions.Accept, // 13
			AcceptConditions.Accept, // 14
			AcceptConditions.Accept, // 15
			AcceptConditions.Accept, // 16
			AcceptConditions.Accept, // 17
			AcceptConditions.Accept, // 18
			AcceptConditions.Accept, // 19
			AcceptConditions.Accept, // 20
			AcceptConditions.Accept, // 21
			AcceptConditions.Accept, // 22
			AcceptConditions.Accept, // 23
			AcceptConditions.Accept, // 24
			AcceptConditions.Accept, // 25
			AcceptConditions.NotAccept, // 26
			AcceptConditions.Accept, // 27
			AcceptConditions.NotAccept, // 28
			AcceptConditions.NotAccept, // 29
			AcceptConditions.NotAccept, // 30
			AcceptConditions.NotAccept, // 31
			AcceptConditions.NotAccept, // 32
			AcceptConditions.NotAccept, // 33
			AcceptConditions.NotAccept, // 34
			AcceptConditions.NotAccept, // 35
			AcceptConditions.NotAccept, // 36
			AcceptConditions.NotAccept, // 37
			AcceptConditions.NotAccept, // 38
			AcceptConditions.NotAccept, // 39
			AcceptConditions.NotAccept, // 40
			AcceptConditions.NotAccept, // 41
			AcceptConditions.NotAccept, // 42
			AcceptConditions.NotAccept, // 43
			AcceptConditions.NotAccept, // 44
			AcceptConditions.NotAccept, // 45
			AcceptConditions.NotAccept, // 46
			AcceptConditions.NotAccept, // 47
		};
		
		private static int[] colMap = new int[]
		{
			23, 23, 23, 23, 23, 23, 23, 23, 23, 20, 20, 23, 23, 20, 23, 23, 
			23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 
			20, 23, 22, 23, 23, 23, 23, 23, 23, 23, 23, 19, 3, 16, 18, 27, 
			17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 4, 23, 23, 23, 23, 23, 
			23, 12, 26, 25, 25, 10, 11, 23, 23, 23, 23, 23, 13, 23, 15, 23, 
			23, 23, 8, 14, 7, 9, 23, 23, 23, 23, 23, 1, 24, 2, 23, 23, 
			23, 12, 26, 25, 25, 10, 11, 23, 23, 23, 23, 23, 13, 23, 15, 23, 
			23, 23, 8, 14, 7, 9, 23, 23, 23, 23, 23, 5, 21, 6, 23, 23, 
			0, 0
		};
		
		private static int[] rowMap = new int[]
		{
			0, 1, 1, 1, 1, 1, 1, 1, 2, 1, 1, 3, 1, 1, 1, 4, 
			1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 5, 6, 7, 8, 9, 10, 
			11, 12, 13, 14, 15, 3, 16, 11, 17, 18, 19, 20, 21, 22, 23, 24
		};
		
		private static int[,] nextState = new int[,]
		{
			{ 1, 2, 3, 4, 5, 6, 7, 26, -1, -1, -1, 28, -1, -1, -1, 29, 30, 8, -1, -1, 9, 9, 10, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 32, -1, -1, -1, -1, -1, -1, 8, 33, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, -1, 15, -1, 15, 15, 15 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, 44, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 39, -1, -1, -1, -1, -1, -1, 27, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 31, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 45, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 35, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 37, 11, -1, 37, -1, 37, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 27, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 12, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 38, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 13, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 14, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ 1, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 16, 15, 41, 15, 15, 15 },
			{ -1, -1, -1, -1, -1, -1, -1, 17, 18, 42, -1, 19, -1, -1, -1, 20, -1, -1, -1, -1, -1, -1, 21, -1, 22, -1, 23, 24 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 47, 47, 47, -1, -1, -1, -1, 47, -1, -1, -1, -1, -1, -1, -1, 47, 47, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 25, 25, 25, -1, -1, -1, -1, 25, -1, -1, -1, -1, -1, -1, -1, 25, 25, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 34, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 36, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 43, 43, 43, -1, -1, -1, -1, 43, -1, -1, -1, -1, -1, -1, -1, 43, 43, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 46, 46, 46, -1, -1, -1, -1, 46, -1, -1, -1, -1, -1, -1, -1, 46, 46, -1 }
		};
		
		
		private static int[] yy_state_dtrans = new int[]
		{
			  0,
			  40
		};
		
		#endregion
		
		public Tokens GetNextToken()
		{
			int current_state = yy_state_dtrans[(int)current_lexical_state];
			int last_accept_state = NoState;
			bool is_initial_state = true;
			
			MarkTokenChunkStart();
			token_start = token_chunk_start;
			expanding_token = false;
			
			if (acceptCondition[current_state] != AcceptConditions.NotAccept)
			{
				last_accept_state = current_state;
				MarkTokenEnd();
			}
			
			while (true)
			{
				char lookahead = (is_initial_state && yy_at_bol) ? BOL : Advance();
				int next_state = nextState[rowMap[current_state], colMap[lookahead]];
				
				if (lookahead == EOF && is_initial_state)
				{
					return Tokens.EOF;
				}
				if (next_state != -1)
				{
					current_state = next_state;
					is_initial_state = false;
					
					if (acceptCondition[current_state] != AcceptConditions.NotAccept)
					{
						last_accept_state = current_state;
						MarkTokenEnd();
					}
				}
				else
				{
					if (last_accept_state == NoState)
					{
						return Tokens.ERROR;
					}
					else
					{
						if ((acceptCondition[last_accept_state] & AcceptConditions.AcceptOnEnd) != 0)
							TrimTokenEnd();
						MoveToTokenEnd();
						
						if (last_accept_state < 0)
						{
							System.Diagnostics.Debug.Assert(last_accept_state >= 48);
						}
						else
						{
							bool accepted = false;
							yyreturn = Accept0(last_accept_state, out accepted);
							if (accepted)
							{
								return yyreturn;
							}
						}
						
						// token ignored:
						is_initial_state = true;
						current_state = yy_state_dtrans[(int)current_lexical_state];
						last_accept_state = NoState;
						MarkTokenChunkStart();
						if (acceptCondition[current_state] != AcceptConditions.NotAccept)
						{
							last_accept_state = current_state;
							MarkTokenEnd();
						}
					}
				}
			}
		} // end of GetNextToken
	}
}

