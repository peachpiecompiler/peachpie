namespace Pchp.Library.DateTime
{
	#region User Code
	
	using System;
using System.Collections.Generic;
/*
 Copyright (c) 2005-2006 Tomas Matousek. Based on PHP5 implementation by Derick Rethans <derick@derickrethans.nl>. 
 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 You must not remove this notice from this software.
*/
#endregion
	
	
	internal class Scanner
	{
		public enum LexicalStates
		{
			YYINITIAL = 0,
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
		internal DateInfo Time { get { return time; } }
		private DateInfo time = new DateInfo();
		internal int Errors { get { return errors; } } 
		private int errors = 0;
		internal int Position { get { return pos; } }
		private int pos = 0;
		private string str;
		void INIT()
		{
			str = new string(buffer, token_start, token_end - token_start);
			pos = 0;
		}
		void DEINIT()
		{
		}
		
		private System.IO.TextReader reader;
		private char[] buffer = new char[512];
		
		// number of characters read into the buffer:
		private int chars_read;
		
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
		
		private bool yy_at_bol = false;
		
		public LexicalStates CurrentLexicalState { get { return current_lexical_state; } set { current_lexical_state = value; } } 
		private LexicalStates current_lexical_state;
		
		protected Scanner(System.IO.TextReader reader)
		{
			Initialize(reader, LexicalStates.YYINITIAL);
		}
		
		public void Initialize(System.IO.TextReader reader, LexicalStates lexicalState, bool atBol)
		{
			this.expanding_token = false;
			this.token_start = 0;
			this.chars_read = 0;
			this.lookahead_index = 0;
			this.token_chunk_start = 0;
			this.token_end = 0;
			this.reader = reader;
			this.yy_at_bol = atBol;
			this.current_lexical_state = lexicalState;
		}
		
		public void Initialize(System.IO.TextReader reader, LexicalStates lexicalState)
		{
			Initialize(reader, lexicalState, false);
		}
		
		#region Accept
		
		
		Tokens Accept0(int state,out bool accepted)
		{
			accepted = true;
			
			switch(state)
			{
				case 1:
					// #line 657
					{
						INIT();
						errors += time.SetTimeZone(str, ref pos) ? 0 : 1;
						DEINIT();
						return Tokens.TIMEZONE;
					}
					
				case 2:
					// #line 711
					{
					  break;
					}
					
				case 3:
					// #line 721
					{
					  return Tokens.ERROR;
					}
					
				case 4:
					// #line 437
					{
						INIT();
						if (time.have_date!=0) { return Tokens.ERROR; } 
						time.HAVE_DATE();
						time.d = DateInfo.ParseUnsignedInt(str, ref pos, 2);
						DateInfo.SkipDaySuffix(str, ref pos);
						time.m = DateInfo.ParseMonth(str, ref pos);
						DEINIT();
						return Tokens.DATE_TEXT;
					}
					
				case 5:
					// #line 200
					{
						INIT();
						time.HAVE_RELATIVE();
						time.UNHAVE_DATE();
						time.UNHAVE_TIME();
						var l = DateInfo.ParseSignedLong(str, ref pos, 24);
						time.y = 1970;
						time.m = 1;
						time.d = 1;
						time.h = time.i = time.s = 0;
						time.f = 0.0;
						time.relative.s += l;
						time.z = 0;
						time.HAVE_TZ();
						DEINIT();
						return Tokens.RELATIVE;
					}
					
				case 6:
					// #line 423
					{
						INIT();
						if (time.have_date!=0) { return Tokens.ERROR; } 
						time.HAVE_DATE();
						time.m = DateInfo.ParseMonth(str, ref pos);
						time.d = DateInfo.ParseUnsignedInt(str, ref pos, 2);
						time.y = DateInfo.ParseUnsignedInt(str, ref pos, 4);
						time.y = DateInfo.ProcessYear(time.y);
						DEINIT();
						return Tokens.DATE_TEXT;
					}
					
				case 7:
					// #line 643
					{
						INIT();
						time.HAVE_RELATIVE();
						time.HAVE_WEEKDAY_RELATIVE();
						time.UNHAVE_TIME();
						time.SetWeekDay(DateInfo.ReadToSpace(str,ref pos));
					  time.relative.weekday_behavior = 1;
						DEINIT();
						return Tokens.WEEKDAY;
					}
					
				case 8:
					// #line 245
					{
						INIT();
						if (time.have_time!=0) { return Tokens.ERROR; }
						time.HAVE_TIME();
						time.h = DateInfo.ParseUnsignedInt(str, ref pos, 2);
						time.i = DateInfo.ParseUnsignedInt(str, ref pos, 2);
						if (pos < str.Length && (str[pos] == ':' || str[pos] == '.')) 
						{
							time.s = DateInfo.ParseUnsignedInt(str, ref pos, 2);
							if (pos < str.Length && str[pos] == '.') 
								time.f = DateInfo.ParseFraction(str, ref pos, 8);
						}
						if (pos < str.Length) 
							errors += time.SetTimeZone(str, ref pos) ? 0 : 1;
						DEINIT();
						return Tokens.TIME24_WITH_ZONE;
					}
					
				case 9:
					// #line 308
					{
						INIT();
						if (time.have_date!=0) { return Tokens.ERROR; } 
						time.HAVE_DATE();
						time.m = DateInfo.ParseUnsignedInt(str, ref pos, 2);
						time.d = DateInfo.ParseUnsignedInt(str, ref pos, 2);
						if (pos < str.Length && str[pos] == '/') 
						{
							time.y = DateInfo.ParseUnsignedInt(str, ref pos, 4);
							time.y = DateInfo.ProcessYear(time.y);
					  }
						DEINIT();
						return Tokens.AMERICAN;
					}
					
				case 10:
					// #line 338
					{
						INIT();
						if (time.have_date!=0) { return Tokens.ERROR;} 
						time.HAVE_DATE();
						time.d = DateInfo.ParseUnsignedInt(str, ref pos, 2);
						DateInfo.SkipDaySuffix(str, ref pos);
						time.m = DateInfo.ParseMonth(str, ref pos);
						time.y = DateInfo.ParseUnsignedInt(str, ref pos, 4);
						time.y = DateInfo.ProcessYear(time.y);
						DEINIT();
						return Tokens.DATE_FULL;
					}
					
				case 11:
					// #line 221
					{
						INIT();
						if (time.have_time!=0) { return Tokens.ERROR; }
						time.HAVE_TIME();
						time.h = DateInfo.ParseUnsignedInt(str, ref pos, 2);
						if (pos < str.Length && (str[pos] == ':' || str[pos] == '.')) 
						{
						  time.i = DateInfo.ParseUnsignedInt(str, ref pos, 2);
						  if (pos < str.Length && (str[pos] == ':' || str[pos] == '.')) 
						  {
							  time.s = DateInfo.ParseUnsignedInt(str, ref pos, 2);
							}  
						}
						if (!time.SetMeridian(str, ref pos))
						{
							return Tokens.ERROR; 
						}	
						DEINIT();
						return Tokens.TIME12;
					}
					
				case 12:
					// #line 610
					{
						INIT();
						time.relative.y = -time.relative.y;
						time.relative.m = -time.relative.m;
						time.relative.d = -time.relative.d;
						time.relative.h = -time.relative.h;
						time.relative.i = -time.relative.i;
						time.relative.s = -time.relative.s;
						time.relative.weekday = -time.relative.weekday;
						DEINIT();
						return Tokens.AGO;
					}
					
				case 13:
					// #line 165
					{
						INIT();
						DEINIT();
						return Tokens.RELATIVE;
					}
					
				case 14:
					// #line 695
					{
						INIT();
						time.HAVE_RELATIVE();
						while(pos < str.Length) 
						{
							var amount = DateInfo.ParseSignedLong(str, ref pos, 24);
							while (pos < str.Length && str[pos] == ' ') pos++;
							time.SetRelative(DateInfo.ReadToSpace(str, ref pos), amount, 0);
						}
						DEINIT();
						return Tokens.RELATIVE;
					}
					
				case 15:
					// #line 268
					{
						INIT();
						switch (time.have_time) 
						{
							case 0:
								time.h = DateInfo.ParseUnsignedInt(str, ref pos, 2);
								time.i = DateInfo.ParseUnsignedInt(str, ref pos, 2);
								time.s = 0;
								break;
							case 1:
								time.y = DateInfo.ParseUnsignedInt(str, ref pos, 4);
								break;
							default:
								DEINIT();
								return Tokens.ERROR;
					  }
						time.have_time++;
						DEINIT();
						return Tokens.GNU_NOCOLON;
					}
					
				case 16:
					// #line 601
					{
						INIT();
						time.y = DateInfo.ParseUnsignedInt(str, ref pos, 4);
						DEINIT();
						return Tokens.CLF;
					}
					
				case 17:
					// #line 172
					{
						INIT();
						time.UNHAVE_TIME();
						time.HAVE_TIME();
						time.h = 12;
						DEINIT();
						return Tokens.RELATIVE;
					}
					
				case 18:
					// #line 353
					{
						INIT();
						if (time.have_date!=0) { return Tokens.ERROR; } 
						time.HAVE_DATE();
						time.y = DateInfo.ParseUnsignedInt(str, ref pos, 4);
						time.m = DateInfo.ParseUnsignedInt(str, ref pos, 2);
						time.d = DateInfo.ParseUnsignedInt(str, ref pos, 2);
						time.y = DateInfo.ProcessYear(time.y);
						DEINIT();
						return Tokens.ISO_DATE;
					}
					
				case 19:
					// #line 408
					{
						INIT();
						if (time.have_date!=0) { return Tokens.ERROR; } 
						time.HAVE_DATE();
						time.y = DateInfo.ParseUnsignedInt(str, ref pos, 4);
						time.m = DateInfo.ParseMonth(str, ref pos);
						time.d = 1;
						time.y = DateInfo.ProcessYear(time.y);
						DEINIT();
						return Tokens.DATE_NO_DAY;
					}
					
				case 20:
					// #line 394
					{
						INIT();
						if (time.have_date!=0) { return Tokens.ERROR; } 
						time.HAVE_DATE();
						time.m = DateInfo.ParseMonth(str, ref pos);
						time.y = DateInfo.ParseUnsignedInt(str, ref pos, 4);
						time.d = 1;
						time.y = DateInfo.ProcessYear(time.y);
						DEINIT();
						return Tokens.DATE_NO_DAY;
					}
					
				case 21:
					// #line 666
					{
						INIT();
						if (time.have_date!=0) { return Tokens.ERROR; } 
						time.HAVE_DATE();
						time.m = DateInfo.ParseMonth(str, ref pos);
						time.d = DateInfo.ParseUnsignedInt(str, ref pos, 2);
						if (time.have_time!=0) { return Tokens.ERROR; }
						time.HAVE_TIME();
						time.h = DateInfo.ParseUnsignedInt(str, ref pos, 2);
						time.i = DateInfo.ParseUnsignedInt(str, ref pos, 2);
						if (pos < str.Length && str[pos] == ':') 
						{
							time.s = DateInfo.ParseUnsignedInt(str, ref pos, 2);
							if (pos < str.Length && str[pos] == '.') 
								time.f = DateInfo.ParseFraction(str, ref pos, 8);
					  }
						if (pos < str.Length) 
							errors += time.SetTimeZone(str, ref pos) ? 0 : 1;
						DEINIT();
						return Tokens.SHORTDATE_WITH_TIME;
					}
					
				case 22:
					// #line 182
					{
						INIT();
						time.UNHAVE_TIME();
						DEINIT();
						return Tokens.RELATIVE;
					}
					
				case 23:
					// #line 380
					{
						INIT();
						if (time.have_date!=0) { return Tokens.ERROR;} 
						time.HAVE_DATE();
						time.d = DateInfo.ParseUnsignedInt(str, ref pos, 2);
						time.m = DateInfo.ParseUnsignedInt(str, ref pos, 2);
						time.y = DateInfo.ParseUnsignedInt(str, ref pos, 2);
						time.y = DateInfo.ProcessYear(time.y);
						DEINIT();
						return Tokens.DATE_FULL_POINTED;
					}
					
				case 24:
					// #line 292
					{
						INIT();
						if (time.have_time!=0) { return Tokens.ERROR; }
						time.HAVE_TIME();
						time.h = DateInfo.ParseUnsignedInt(str, ref pos, 2);
						time.i = DateInfo.ParseUnsignedInt(str, ref pos, 2);
						time.s = DateInfo.ParseUnsignedInt(str, ref pos, 2);
						if (pos < str.Length) 
							errors += time.SetTimeZone(str, ref pos) ? 0 : 1;
						DEINIT();
						return Tokens.ISO_NOCOLON;
					}
					
				case 25:
					// #line 590
					{
						INIT();
						if (time.have_date != 0) { return Tokens.ERROR; }
						time.HAVE_DATE();
						time.y = DateInfo.ParseUnsignedInt(str, ref pos, 4);
						time.m = DateInfo.ParseUnsignedInt(str, ref pos, 2);
						time.d = 1;
						DEINIT();
						return Tokens.YEAR_MONTH;
					}
					
				case 26:
					// #line 488
					{
						INIT();
						if (time.have_date!=0) { return Tokens.ERROR; } 
						time.HAVE_DATE();
						time.y = DateInfo.ParseUnsignedInt(str, ref pos, 4);
						time.d = DateInfo.ParseUnsignedInt(str, ref pos, 3);
						time.m = 1;
						time.y = DateInfo.ProcessYear(time.y);
						DEINIT();
						return Tokens.PG_YEARDAY;
					}
					
				case 27:
					// #line 521
					{
						{
							int w, d;
							INIT();
							if (time.have_date!=0) { return Tokens.ERROR; } 
							time.HAVE_DATE();
							time.HAVE_RELATIVE();
							time.y = DateInfo.ParseUnsignedInt(str, ref pos, 4);
							w = DateInfo.ParseUnsignedInt(str, ref pos, 2);
							d = 1;
							time.m = 1;
							time.d = 1;
							time.relative.d = DateInfo.WeekToDay(time.y, w, d);
							DEINIT();
							return Tokens.ISO_WEEK;
						}	
					}
					
				case 28:
					// #line 625
					{
						INIT();
						time.HAVE_RELATIVE();
						while (pos < str.Length) 
						{
						  int behavior;
							int amount = DateInfo.ParseRelativeText(str, ref pos, out behavior);
							while (pos < str.Length && str[pos] == ' ') pos++;
							time.SetRelative(DateInfo.ReadToSpace(str,ref pos), amount, behavior);
					  }
						DEINIT();
						return Tokens.RELATIVE;
					}
					
				case 29:
					// #line 542
					{
						INIT();
						if (time.have_date!=0) { return Tokens.ERROR; } 
						time.HAVE_DATE();
						time.m = DateInfo.ParseMonth(str, ref pos);
						time.d = DateInfo.ParseUnsignedInt(str, ref pos, 2);
						time.y = DateInfo.ParseUnsignedInt(str, ref pos, 4);
						time.y = DateInfo.ProcessYear(time.y);
						DEINIT();
						return Tokens.PG_TEXT;
					}
					
				case 30:
					// #line 367
					{
						INIT();
						if (time.have_date!=0) { return Tokens.ERROR;} 
						time.HAVE_DATE();
						time.d = DateInfo.ParseUnsignedInt(str, ref pos, 2);
						time.m = DateInfo.ParseUnsignedInt(str, ref pos, 2);
						time.y = DateInfo.ParseUnsignedInt(str, ref pos, 4);
						DEINIT();
						return Tokens.DATE_FULL_POINTED;
					}
					
				case 31:
					// #line 450
					{
						INIT();
						if (time.have_date!=0) { return Tokens.ERROR; } 
						time.HAVE_DATE();
						time.y = DateInfo.ParseUnsignedInt(str, ref pos, 4);
						time.m = DateInfo.ParseUnsignedInt(str, ref pos, 2);
						time.d = DateInfo.ParseUnsignedInt(str, ref pos, 2);
						DEINIT();
						return Tokens.DATE_NOCOLON;
					}
					
				case 32:
					// #line 502
					{
						int week, day;
						INIT();
						if (time.have_date!=0) { return Tokens.ERROR; } 
						time.HAVE_DATE();
						time.HAVE_RELATIVE();
						time.y = DateInfo.ParseUnsignedInt(str, ref pos, 4);
						week = DateInfo.ParseUnsignedInt(str, ref pos, 2);
						day = DateInfo.ParseUnsignedInt(str, ref pos, 1);
						time.m = 1;
						time.d = 1;
						time.relative.d = DateInfo.WeekToDay(time.y, week, day);
						DEINIT();
						return Tokens.ISO_WEEK;
					}
					
				case 33:
					// #line 190
					{
						INIT();
						time.HAVE_RELATIVE();
						time.UNHAVE_TIME();
						time.relative.d = 1;
						DEINIT();
						return Tokens.RELATIVE;
					}
					
				case 34:
					// #line 556
					{
						INIT();
						if (time.have_date!=0) { return Tokens.ERROR; } 
						time.HAVE_DATE();
						time.y = DateInfo.ParseUnsignedInt(str, ref pos, 4);
						time.m = DateInfo.ParseMonth(str, ref pos);
						time.d = DateInfo.ParseUnsignedInt(str, ref pos, 2);
						time.y = DateInfo.ProcessYear(time.y);
						DEINIT();
						return Tokens.PG_TEXT;
					}
					
				case 35:
					// #line 155
					{
						INIT();
						time.HAVE_RELATIVE();
						time.UNHAVE_TIME();
						time.relative.d = -1;
						DEINIT();
						return Tokens.RELATIVE;
					}
					
				case 36:
					// #line 325
					{
						INIT();
						if (time.have_date!=0) { return Tokens.ERROR; } 
						time.HAVE_DATE();
						time.y = DateInfo.ParseUnsignedInt(str, ref pos, 4);
						time.m = DateInfo.ParseUnsignedInt(str, ref pos, 2);
						time.d = DateInfo.ParseUnsignedInt(str, ref pos, 2);
						DEINIT();
						return Tokens.ISO_DATE;
					}
					
				case 37:
					// #line 463
					{
						INIT();
						if (time.have_time!=0) { return Tokens.ERROR; }
						time.HAVE_TIME();
						if (time.have_date!=0) { return Tokens.ERROR; } 
						time.HAVE_DATE();
						time.y = DateInfo.ParseUnsignedInt(str, ref pos, 4);
						time.m = DateInfo.ParseUnsignedInt(str, ref pos, 2);
						time.d = DateInfo.ParseUnsignedInt(str, ref pos, 2);
						time.h = DateInfo.ParseUnsignedInt(str, ref pos, 2);
						time.i = DateInfo.ParseUnsignedInt(str, ref pos, 2);
						time.s = DateInfo.ParseUnsignedInt(str, ref pos, 2);
						if (pos < str.Length && str[pos] == '.') 
						{
							time.f = DateInfo.ParseFraction(str, ref pos, 9);
							if (pos < str.Length)
							  errors += time.SetTimeZone(str, ref pos) ? 0 : 1;
						}
						DEINIT();
						return Tokens.XMLRPC_SOAP;
					}
					
				case 38:
					// #line 570
					{
						INIT();
						if (time.have_time!=0) { return Tokens.ERROR; }
						time.HAVE_TIME();
						if (time.have_date!=0) { return Tokens.ERROR; } 
						time.HAVE_DATE();
						time.d = DateInfo.ParseUnsignedInt(str, ref pos, 2);
						time.m = DateInfo.ParseMonth(str, ref pos);
						time.y = DateInfo.ParseUnsignedInt(str, ref pos, 4);
						time.h = DateInfo.ParseUnsignedInt(str, ref pos, 2);
						time.i = DateInfo.ParseUnsignedInt(str, ref pos, 2);
						time.s = DateInfo.ParseUnsignedInt(str, ref pos, 2);
						errors += time.SetTimeZone(str, ref pos) ? 0 : 1;
						DEINIT();
						return Tokens.CLF;
					}
					
				case 40: goto case 1;
				case 41: goto case 2;
				case 42: goto case 3;
				case 43: goto case 4;
				case 44: goto case 6;
				case 45: goto case 7;
				case 46: goto case 8;
				case 47: goto case 9;
				case 48: goto case 10;
				case 49: goto case 11;
				case 50: goto case 14;
				case 51: goto case 18;
				case 52: goto case 19;
				case 53: goto case 20;
				case 54: goto case 21;
				case 55: goto case 24;
				case 56: goto case 25;
				case 57: goto case 26;
				case 58: goto case 28;
				case 59: goto case 29;
				case 60: goto case 36;
				case 61: goto case 37;
				case 62: goto case 38;
				case 64: goto case 1;
				case 65: goto case 3;
				case 66: goto case 4;
				case 67: goto case 6;
				case 68: goto case 8;
				case 69: goto case 9;
				case 70: goto case 11;
				case 71: goto case 14;
				case 72: goto case 18;
				case 73: goto case 19;
				case 74: goto case 21;
				case 75: goto case 24;
				case 76: goto case 26;
				case 77: goto case 28;
				case 78: goto case 36;
				case 79: goto case 37;
				case 80: goto case 38;
				case 82: goto case 1;
				case 83: goto case 3;
				case 84: goto case 4;
				case 85: goto case 6;
				case 86: goto case 8;
				case 87: goto case 9;
				case 88: goto case 11;
				case 89: goto case 14;
				case 90: goto case 18;
				case 91: goto case 19;
				case 92: goto case 21;
				case 93: goto case 24;
				case 94: goto case 26;
				case 95: goto case 28;
				case 96: goto case 37;
				case 98: goto case 1;
				case 99: goto case 3;
				case 100: goto case 4;
				case 101: goto case 6;
				case 102: goto case 8;
				case 103: goto case 9;
				case 104: goto case 14;
				case 105: goto case 18;
				case 106: goto case 19;
				case 107: goto case 21;
				case 108: goto case 24;
				case 109: goto case 26;
				case 110: goto case 28;
				case 111: goto case 37;
				case 113: goto case 1;
				case 114: goto case 3;
				case 115: goto case 4;
				case 116: goto case 6;
				case 117: goto case 8;
				case 118: goto case 9;
				case 119: goto case 14;
				case 120: goto case 18;
				case 121: goto case 19;
				case 122: goto case 21;
				case 123: goto case 28;
				case 124: goto case 37;
				case 126: goto case 1;
				case 127: goto case 4;
				case 128: goto case 6;
				case 129: goto case 8;
				case 130: goto case 14;
				case 131: goto case 18;
				case 132: goto case 19;
				case 133: goto case 21;
				case 134: goto case 28;
				case 135: goto case 37;
				case 137: goto case 1;
				case 138: goto case 4;
				case 139: goto case 6;
				case 140: goto case 8;
				case 141: goto case 14;
				case 142: goto case 18;
				case 143: goto case 19;
				case 144: goto case 21;
				case 145: goto case 28;
				case 146: goto case 37;
				case 148: goto case 1;
				case 149: goto case 4;
				case 150: goto case 6;
				case 151: goto case 8;
				case 152: goto case 14;
				case 153: goto case 18;
				case 154: goto case 19;
				case 155: goto case 21;
				case 156: goto case 28;
				case 157: goto case 37;
				case 159: goto case 1;
				case 160: goto case 4;
				case 161: goto case 6;
				case 162: goto case 8;
				case 163: goto case 14;
				case 164: goto case 18;
				case 165: goto case 19;
				case 166: goto case 21;
				case 167: goto case 28;
				case 168: goto case 37;
				case 170: goto case 1;
				case 171: goto case 4;
				case 172: goto case 6;
				case 173: goto case 8;
				case 174: goto case 18;
				case 175: goto case 19;
				case 176: goto case 21;
				case 177: goto case 28;
				case 178: goto case 37;
				case 180: goto case 1;
				case 181: goto case 4;
				case 182: goto case 6;
				case 183: goto case 8;
				case 184: goto case 18;
				case 185: goto case 19;
				case 186: goto case 21;
				case 187: goto case 28;
				case 188: goto case 37;
				case 190: goto case 1;
				case 191: goto case 6;
				case 192: goto case 8;
				case 193: goto case 18;
				case 194: goto case 19;
				case 195: goto case 21;
				case 197: goto case 1;
				case 198: goto case 6;
				case 199: goto case 8;
				case 200: goto case 19;
				case 201: goto case 21;
				case 203: goto case 1;
				case 204: goto case 6;
				case 205: goto case 8;
				case 206: goto case 19;
				case 207: goto case 21;
				case 209: goto case 1;
				case 210: goto case 6;
				case 211: goto case 8;
				case 212: goto case 19;
				case 213: goto case 21;
				case 215: goto case 1;
				case 216: goto case 6;
				case 217: goto case 8;
				case 218: goto case 19;
				case 219: goto case 21;
				case 221: goto case 1;
				case 222: goto case 6;
				case 223: goto case 8;
				case 224: goto case 19;
				case 225: goto case 21;
				case 227: goto case 1;
				case 228: goto case 6;
				case 229: goto case 8;
				case 230: goto case 19;
				case 231: goto case 21;
				case 233: goto case 1;
				case 234: goto case 6;
				case 235: goto case 8;
				case 236: goto case 19;
				case 237: goto case 21;
				case 239: goto case 1;
				case 240: goto case 6;
				case 241: goto case 8;
				case 242: goto case 19;
				case 243: goto case 21;
				case 245: goto case 1;
				case 246: goto case 8;
				case 247: goto case 19;
				case 248: goto case 21;
				case 250: goto case 1;
				case 251: goto case 8;
				case 252: goto case 21;
				case 254: goto case 1;
				case 255: goto case 8;
				case 256: goto case 21;
				case 258: goto case 1;
				case 259: goto case 8;
				case 260: goto case 21;
				case 262: goto case 1;
				case 263: goto case 8;
				case 264: goto case 21;
				case 266: goto case 1;
				case 267: goto case 8;
				case 268: goto case 21;
				case 270: goto case 1;
				case 271: goto case 8;
				case 273: goto case 8;
				case 275: goto case 8;
				case 277: goto case 8;
				case 279: goto case 8;
				case 281: goto case 8;
				case 283: goto case 8;
				case 285: goto case 8;
				case 287: goto case 8;
				case 289: goto case 8;
				case 291: goto case 8;
				case 293: goto case 8;
				case 629: goto case 1;
				case 630: goto case 6;
				case 631: goto case 10;
				case 632: goto case 25;
				case 633: goto case 28;
				case 634: goto case 29;
				case 635: goto case 38;
				case 636: goto case 1;
				case 637: goto case 4;
				case 638: goto case 8;
				case 639: goto case 19;
				case 640: goto case 20;
				case 641: goto case 38;
				case 643: goto case 1;
				case 644: goto case 11;
				case 645: goto case 18;
				case 646: goto case 8;
				case 647: goto case 9;
				case 648: goto case 21;
				case 649: goto case 3;
				case 650: goto case 1;
				case 651: goto case 4;
				case 652: goto case 6;
				case 653: goto case 8;
				case 654: goto case 18;
				case 655: goto case 19;
				case 656: goto case 21;
				case 657: goto case 1;
				case 658: goto case 4;
				case 659: goto case 8;
				case 660: goto case 19;
				case 661: goto case 21;
				case 662: goto case 8;
				case 663: goto case 6;
				case 664: goto case 8;
				case 665: goto case 8;
				case 666: goto case 21;
				case 668: goto case 8;
				case 669: goto case 8;
				case 671: goto case 21;
				case 673: goto case 8;
				case 674: goto case 19;
				case 675: goto case 19;
				case 676: goto case 1;
				case 677: goto case 6;
				case 678: goto case 21;
				case 680: goto case 6;
				case 681: goto case 21;
				case 684: goto case 1;
				case 685: goto case 21;
				case 686: goto case 1;
				case 687: goto case 8;
				case 688: goto case 8;
				case 730: goto case 1;
				case 731: goto case 6;
				case 732: goto case 10;
				case 733: goto case 38;
				case 734: goto case 8;
				case 736: goto case 1;
				case 737: goto case 21;
				case 738: goto case 3;
				case 739: goto case 6;
				case 740: goto case 21;
				case 741: goto case 1;
				case 742: goto case 8;
				case 743: goto case 8;
				case 744: goto case 8;
				case 745: goto case 8;
				case 747: goto case 1;
				case 748: goto case 6;
				case 750: goto case 1;
				case 763: goto case 1;
				case 764: goto case 6;
				case 766: goto case 1;
				case 767: goto case 3;
				case 768: goto case 6;
				case 769: goto case 8;
				case 770: goto case 8;
				case 771: goto case 1;
				case 773: goto case 1;
				case 779: goto case 1;
				case 781: goto case 1;
				case 782: goto case 6;
				case 783: goto case 1;
				case 785: goto case 1;
				case 790: goto case 1;
				case 791: goto case 1;
				case 792: goto case 6;
				case 793: goto case 1;
				case 795: goto case 1;
				case 799: goto case 1;
				case 800: goto case 1;
				case 802: goto case 1;
				case 806: goto case 1;
				case 807: goto case 1;
				case 812: goto case 1;
				case 813: goto case 1;
				case 817: goto case 1;
				case 818: goto case 1;
				case 821: goto case 1;
				case 822: goto case 1;
				case 825: goto case 1;
				case 828: goto case 1;
				case 831: goto case 1;
				case 833: goto case 1;
				case 835: goto case 1;
				case 837: goto case 1;
				case 845: goto case 7;
				case 846: goto case 8;
				case 847: goto case 10;
				case 848: goto case 28;
				case 849: goto case 29;
				case 850: goto case 4;
				case 851: goto case 8;
				case 852: goto case 19;
				case 853: goto case 28;
				case 854: goto case 1;
				case 855: goto case 8;
				case 856: goto case 9;
				case 857: goto case 1;
				case 858: goto case 21;
				case 859: goto case 1;
				case 860: goto case 8;
				case 861: goto case 1;
				case 862: goto case 6;
				case 863: goto case 1;
				case 870: goto case 1;
				case 871: goto case 4;
				case 872: goto case 4;
				case 873: goto case 1;
				case 874: goto case 1;
				case 875: goto case 7;
				case 876: goto case 8;
				case 877: goto case 10;
				case 878: goto case 28;
				case 879: goto case 28;
				case 880: goto case 1;
				case 881: goto case 8;
				case 882: goto case 21;
				case 883: goto case 8;
				case 884: goto case 1;
				case 885: goto case 6;
				case 886: goto case 1;
				case 889: goto case 1;
				case 890: goto case 1;
				case 891: goto case 10;
				case 892: goto case 1;
				case 893: goto case 1;
				case 896: goto case 10;
				case 897: goto case 1;
				case 898: goto case 1;
				case 900: goto case 1;
				case 902: goto case 1;
				case 904: goto case 1;
				case 911: goto case 1;
				case 912: goto case 4;
				case 913: goto case 4;
				case 914: goto case 4;
				case 915: goto case 4;
				case 916: goto case 4;
				case 917: goto case 4;
				case 918: goto case 4;
				case 919: goto case 4;
				case 920: goto case 4;
				case 921: goto case 4;
				case 922: goto case 1;
				case 923: goto case 1;
				case 939: goto case 1;
				case 940: goto case 7;
				case 941: goto case 28;
				case 943: goto case 1;
				case 945: goto case 1;
				case 946: goto case 7;
				case 947: goto case 28;
				case 948: goto case 1;
				case 950: goto case 1;
				case 951: goto case 1;
				case 953: goto case 1;
				case 954: goto case 1;
				case 956: goto case 1;
				case 958: goto case 1;
				case 960: goto case 1;
				case 963: goto case 1;
				case 966: goto case 1;
				case 968: goto case 1;
				case 970: goto case 1;
				case 971: goto case 28;
				case 972: goto case 1;
				case 973: goto case 1;
				case 974: goto case 1;
				case 976: goto case 1;
				case 977: goto case 1;
				case 979: goto case 1;
				case 981: goto case 1;
				case 982: goto case 1;
				case 983: goto case 1;
				case 984: goto case 1;
				case 986: goto case 1;
				case 987: goto case 1;
				case 988: goto case 1;
				case 989: goto case 1;
				case 990: goto case 1;
				case 991: goto case 1;
			}
			accepted = false;
			return yyreturn;
		}
		
		
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
			
			return buffer[lookahead_index++];
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
		
		private Stack stateStack = new Stack(20);
		
		private void yy_push_state(LexicalStates state)
		{
			stateStack.Push(current_lexical_state);
			current_lexical_state = state;
		}
		
		private bool yy_pop_state()
		{
			if (stateStack.Count == 0) return false;
			current_lexical_state = (LexicalStates)stateStack.Pop();
			return true;
		}
		
		private LexicalStates yy_top_state()
		{
			return (LexicalStates)stateStack.Peek();
		}
		
		#region Tables
		
		private static readonly AcceptConditions[] acceptCondition = new AcceptConditions[]
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
			AcceptConditions.Accept, // 26
			AcceptConditions.Accept, // 27
			AcceptConditions.Accept, // 28
			AcceptConditions.Accept, // 29
			AcceptConditions.Accept, // 30
			AcceptConditions.Accept, // 31
			AcceptConditions.Accept, // 32
			AcceptConditions.Accept, // 33
			AcceptConditions.Accept, // 34
			AcceptConditions.Accept, // 35
			AcceptConditions.Accept, // 36
			AcceptConditions.Accept, // 37
			AcceptConditions.Accept, // 38
			AcceptConditions.NotAccept, // 39
			AcceptConditions.Accept, // 40
			AcceptConditions.Accept, // 41
			AcceptConditions.Accept, // 42
			AcceptConditions.Accept, // 43
			AcceptConditions.Accept, // 44
			AcceptConditions.Accept, // 45
			AcceptConditions.Accept, // 46
			AcceptConditions.Accept, // 47
			AcceptConditions.Accept, // 48
			AcceptConditions.Accept, // 49
			AcceptConditions.Accept, // 50
			AcceptConditions.Accept, // 51
			AcceptConditions.Accept, // 52
			AcceptConditions.Accept, // 53
			AcceptConditions.Accept, // 54
			AcceptConditions.Accept, // 55
			AcceptConditions.Accept, // 56
			AcceptConditions.Accept, // 57
			AcceptConditions.Accept, // 58
			AcceptConditions.Accept, // 59
			AcceptConditions.Accept, // 60
			AcceptConditions.Accept, // 61
			AcceptConditions.Accept, // 62
			AcceptConditions.NotAccept, // 63
			AcceptConditions.Accept, // 64
			AcceptConditions.Accept, // 65
			AcceptConditions.Accept, // 66
			AcceptConditions.Accept, // 67
			AcceptConditions.Accept, // 68
			AcceptConditions.Accept, // 69
			AcceptConditions.Accept, // 70
			AcceptConditions.Accept, // 71
			AcceptConditions.Accept, // 72
			AcceptConditions.Accept, // 73
			AcceptConditions.Accept, // 74
			AcceptConditions.Accept, // 75
			AcceptConditions.Accept, // 76
			AcceptConditions.Accept, // 77
			AcceptConditions.Accept, // 78
			AcceptConditions.Accept, // 79
			AcceptConditions.Accept, // 80
			AcceptConditions.NotAccept, // 81
			AcceptConditions.Accept, // 82
			AcceptConditions.Accept, // 83
			AcceptConditions.Accept, // 84
			AcceptConditions.Accept, // 85
			AcceptConditions.Accept, // 86
			AcceptConditions.Accept, // 87
			AcceptConditions.Accept, // 88
			AcceptConditions.Accept, // 89
			AcceptConditions.Accept, // 90
			AcceptConditions.Accept, // 91
			AcceptConditions.Accept, // 92
			AcceptConditions.Accept, // 93
			AcceptConditions.Accept, // 94
			AcceptConditions.Accept, // 95
			AcceptConditions.Accept, // 96
			AcceptConditions.NotAccept, // 97
			AcceptConditions.Accept, // 98
			AcceptConditions.Accept, // 99
			AcceptConditions.Accept, // 100
			AcceptConditions.Accept, // 101
			AcceptConditions.Accept, // 102
			AcceptConditions.Accept, // 103
			AcceptConditions.Accept, // 104
			AcceptConditions.Accept, // 105
			AcceptConditions.Accept, // 106
			AcceptConditions.Accept, // 107
			AcceptConditions.Accept, // 108
			AcceptConditions.Accept, // 109
			AcceptConditions.Accept, // 110
			AcceptConditions.Accept, // 111
			AcceptConditions.NotAccept, // 112
			AcceptConditions.Accept, // 113
			AcceptConditions.Accept, // 114
			AcceptConditions.Accept, // 115
			AcceptConditions.Accept, // 116
			AcceptConditions.Accept, // 117
			AcceptConditions.Accept, // 118
			AcceptConditions.Accept, // 119
			AcceptConditions.Accept, // 120
			AcceptConditions.Accept, // 121
			AcceptConditions.Accept, // 122
			AcceptConditions.Accept, // 123
			AcceptConditions.Accept, // 124
			AcceptConditions.NotAccept, // 125
			AcceptConditions.Accept, // 126
			AcceptConditions.Accept, // 127
			AcceptConditions.Accept, // 128
			AcceptConditions.Accept, // 129
			AcceptConditions.Accept, // 130
			AcceptConditions.Accept, // 131
			AcceptConditions.Accept, // 132
			AcceptConditions.Accept, // 133
			AcceptConditions.Accept, // 134
			AcceptConditions.Accept, // 135
			AcceptConditions.NotAccept, // 136
			AcceptConditions.Accept, // 137
			AcceptConditions.Accept, // 138
			AcceptConditions.Accept, // 139
			AcceptConditions.Accept, // 140
			AcceptConditions.Accept, // 141
			AcceptConditions.Accept, // 142
			AcceptConditions.Accept, // 143
			AcceptConditions.Accept, // 144
			AcceptConditions.Accept, // 145
			AcceptConditions.Accept, // 146
			AcceptConditions.NotAccept, // 147
			AcceptConditions.Accept, // 148
			AcceptConditions.Accept, // 149
			AcceptConditions.Accept, // 150
			AcceptConditions.Accept, // 151
			AcceptConditions.Accept, // 152
			AcceptConditions.Accept, // 153
			AcceptConditions.Accept, // 154
			AcceptConditions.Accept, // 155
			AcceptConditions.Accept, // 156
			AcceptConditions.Accept, // 157
			AcceptConditions.NotAccept, // 158
			AcceptConditions.Accept, // 159
			AcceptConditions.Accept, // 160
			AcceptConditions.Accept, // 161
			AcceptConditions.Accept, // 162
			AcceptConditions.Accept, // 163
			AcceptConditions.Accept, // 164
			AcceptConditions.Accept, // 165
			AcceptConditions.Accept, // 166
			AcceptConditions.Accept, // 167
			AcceptConditions.Accept, // 168
			AcceptConditions.NotAccept, // 169
			AcceptConditions.Accept, // 170
			AcceptConditions.Accept, // 171
			AcceptConditions.Accept, // 172
			AcceptConditions.Accept, // 173
			AcceptConditions.Accept, // 174
			AcceptConditions.Accept, // 175
			AcceptConditions.Accept, // 176
			AcceptConditions.Accept, // 177
			AcceptConditions.Accept, // 178
			AcceptConditions.NotAccept, // 179
			AcceptConditions.Accept, // 180
			AcceptConditions.Accept, // 181
			AcceptConditions.Accept, // 182
			AcceptConditions.Accept, // 183
			AcceptConditions.Accept, // 184
			AcceptConditions.Accept, // 185
			AcceptConditions.Accept, // 186
			AcceptConditions.Accept, // 187
			AcceptConditions.Accept, // 188
			AcceptConditions.NotAccept, // 189
			AcceptConditions.Accept, // 190
			AcceptConditions.Accept, // 191
			AcceptConditions.Accept, // 192
			AcceptConditions.Accept, // 193
			AcceptConditions.Accept, // 194
			AcceptConditions.Accept, // 195
			AcceptConditions.NotAccept, // 196
			AcceptConditions.Accept, // 197
			AcceptConditions.Accept, // 198
			AcceptConditions.Accept, // 199
			AcceptConditions.Accept, // 200
			AcceptConditions.Accept, // 201
			AcceptConditions.NotAccept, // 202
			AcceptConditions.Accept, // 203
			AcceptConditions.Accept, // 204
			AcceptConditions.Accept, // 205
			AcceptConditions.Accept, // 206
			AcceptConditions.Accept, // 207
			AcceptConditions.NotAccept, // 208
			AcceptConditions.Accept, // 209
			AcceptConditions.Accept, // 210
			AcceptConditions.Accept, // 211
			AcceptConditions.Accept, // 212
			AcceptConditions.Accept, // 213
			AcceptConditions.NotAccept, // 214
			AcceptConditions.Accept, // 215
			AcceptConditions.Accept, // 216
			AcceptConditions.Accept, // 217
			AcceptConditions.Accept, // 218
			AcceptConditions.Accept, // 219
			AcceptConditions.NotAccept, // 220
			AcceptConditions.Accept, // 221
			AcceptConditions.Accept, // 222
			AcceptConditions.Accept, // 223
			AcceptConditions.Accept, // 224
			AcceptConditions.Accept, // 225
			AcceptConditions.NotAccept, // 226
			AcceptConditions.Accept, // 227
			AcceptConditions.Accept, // 228
			AcceptConditions.Accept, // 229
			AcceptConditions.Accept, // 230
			AcceptConditions.Accept, // 231
			AcceptConditions.NotAccept, // 232
			AcceptConditions.Accept, // 233
			AcceptConditions.Accept, // 234
			AcceptConditions.Accept, // 235
			AcceptConditions.Accept, // 236
			AcceptConditions.Accept, // 237
			AcceptConditions.NotAccept, // 238
			AcceptConditions.Accept, // 239
			AcceptConditions.Accept, // 240
			AcceptConditions.Accept, // 241
			AcceptConditions.Accept, // 242
			AcceptConditions.Accept, // 243
			AcceptConditions.NotAccept, // 244
			AcceptConditions.Accept, // 245
			AcceptConditions.Accept, // 246
			AcceptConditions.Accept, // 247
			AcceptConditions.Accept, // 248
			AcceptConditions.NotAccept, // 249
			AcceptConditions.Accept, // 250
			AcceptConditions.Accept, // 251
			AcceptConditions.Accept, // 252
			AcceptConditions.NotAccept, // 253
			AcceptConditions.Accept, // 254
			AcceptConditions.Accept, // 255
			AcceptConditions.Accept, // 256
			AcceptConditions.NotAccept, // 257
			AcceptConditions.Accept, // 258
			AcceptConditions.Accept, // 259
			AcceptConditions.Accept, // 260
			AcceptConditions.NotAccept, // 261
			AcceptConditions.Accept, // 262
			AcceptConditions.Accept, // 263
			AcceptConditions.Accept, // 264
			AcceptConditions.NotAccept, // 265
			AcceptConditions.Accept, // 266
			AcceptConditions.Accept, // 267
			AcceptConditions.Accept, // 268
			AcceptConditions.NotAccept, // 269
			AcceptConditions.Accept, // 270
			AcceptConditions.Accept, // 271
			AcceptConditions.NotAccept, // 272
			AcceptConditions.Accept, // 273
			AcceptConditions.NotAccept, // 274
			AcceptConditions.Accept, // 275
			AcceptConditions.NotAccept, // 276
			AcceptConditions.Accept, // 277
			AcceptConditions.NotAccept, // 278
			AcceptConditions.Accept, // 279
			AcceptConditions.NotAccept, // 280
			AcceptConditions.Accept, // 281
			AcceptConditions.NotAccept, // 282
			AcceptConditions.Accept, // 283
			AcceptConditions.NotAccept, // 284
			AcceptConditions.Accept, // 285
			AcceptConditions.NotAccept, // 286
			AcceptConditions.Accept, // 287
			AcceptConditions.NotAccept, // 288
			AcceptConditions.Accept, // 289
			AcceptConditions.NotAccept, // 290
			AcceptConditions.Accept, // 291
			AcceptConditions.NotAccept, // 292
			AcceptConditions.Accept, // 293
			AcceptConditions.NotAccept, // 294
			AcceptConditions.NotAccept, // 295
			AcceptConditions.NotAccept, // 296
			AcceptConditions.NotAccept, // 297
			AcceptConditions.NotAccept, // 298
			AcceptConditions.NotAccept, // 299
			AcceptConditions.NotAccept, // 300
			AcceptConditions.NotAccept, // 301
			AcceptConditions.NotAccept, // 302
			AcceptConditions.NotAccept, // 303
			AcceptConditions.NotAccept, // 304
			AcceptConditions.NotAccept, // 305
			AcceptConditions.NotAccept, // 306
			AcceptConditions.NotAccept, // 307
			AcceptConditions.NotAccept, // 308
			AcceptConditions.NotAccept, // 309
			AcceptConditions.NotAccept, // 310
			AcceptConditions.NotAccept, // 311
			AcceptConditions.NotAccept, // 312
			AcceptConditions.NotAccept, // 313
			AcceptConditions.NotAccept, // 314
			AcceptConditions.NotAccept, // 315
			AcceptConditions.NotAccept, // 316
			AcceptConditions.NotAccept, // 317
			AcceptConditions.NotAccept, // 318
			AcceptConditions.NotAccept, // 319
			AcceptConditions.NotAccept, // 320
			AcceptConditions.NotAccept, // 321
			AcceptConditions.NotAccept, // 322
			AcceptConditions.NotAccept, // 323
			AcceptConditions.NotAccept, // 324
			AcceptConditions.NotAccept, // 325
			AcceptConditions.NotAccept, // 326
			AcceptConditions.NotAccept, // 327
			AcceptConditions.NotAccept, // 328
			AcceptConditions.NotAccept, // 329
			AcceptConditions.NotAccept, // 330
			AcceptConditions.NotAccept, // 331
			AcceptConditions.NotAccept, // 332
			AcceptConditions.NotAccept, // 333
			AcceptConditions.NotAccept, // 334
			AcceptConditions.NotAccept, // 335
			AcceptConditions.NotAccept, // 336
			AcceptConditions.NotAccept, // 337
			AcceptConditions.NotAccept, // 338
			AcceptConditions.NotAccept, // 339
			AcceptConditions.NotAccept, // 340
			AcceptConditions.NotAccept, // 341
			AcceptConditions.NotAccept, // 342
			AcceptConditions.NotAccept, // 343
			AcceptConditions.NotAccept, // 344
			AcceptConditions.NotAccept, // 345
			AcceptConditions.NotAccept, // 346
			AcceptConditions.NotAccept, // 347
			AcceptConditions.NotAccept, // 348
			AcceptConditions.NotAccept, // 349
			AcceptConditions.NotAccept, // 350
			AcceptConditions.NotAccept, // 351
			AcceptConditions.NotAccept, // 352
			AcceptConditions.NotAccept, // 353
			AcceptConditions.NotAccept, // 354
			AcceptConditions.NotAccept, // 355
			AcceptConditions.NotAccept, // 356
			AcceptConditions.NotAccept, // 357
			AcceptConditions.NotAccept, // 358
			AcceptConditions.NotAccept, // 359
			AcceptConditions.NotAccept, // 360
			AcceptConditions.NotAccept, // 361
			AcceptConditions.NotAccept, // 362
			AcceptConditions.NotAccept, // 363
			AcceptConditions.NotAccept, // 364
			AcceptConditions.NotAccept, // 365
			AcceptConditions.NotAccept, // 366
			AcceptConditions.NotAccept, // 367
			AcceptConditions.NotAccept, // 368
			AcceptConditions.NotAccept, // 369
			AcceptConditions.NotAccept, // 370
			AcceptConditions.NotAccept, // 371
			AcceptConditions.NotAccept, // 372
			AcceptConditions.NotAccept, // 373
			AcceptConditions.NotAccept, // 374
			AcceptConditions.NotAccept, // 375
			AcceptConditions.NotAccept, // 376
			AcceptConditions.NotAccept, // 377
			AcceptConditions.NotAccept, // 378
			AcceptConditions.NotAccept, // 379
			AcceptConditions.NotAccept, // 380
			AcceptConditions.NotAccept, // 381
			AcceptConditions.NotAccept, // 382
			AcceptConditions.NotAccept, // 383
			AcceptConditions.NotAccept, // 384
			AcceptConditions.NotAccept, // 385
			AcceptConditions.NotAccept, // 386
			AcceptConditions.NotAccept, // 387
			AcceptConditions.NotAccept, // 388
			AcceptConditions.NotAccept, // 389
			AcceptConditions.NotAccept, // 390
			AcceptConditions.NotAccept, // 391
			AcceptConditions.NotAccept, // 392
			AcceptConditions.NotAccept, // 393
			AcceptConditions.NotAccept, // 394
			AcceptConditions.NotAccept, // 395
			AcceptConditions.NotAccept, // 396
			AcceptConditions.NotAccept, // 397
			AcceptConditions.NotAccept, // 398
			AcceptConditions.NotAccept, // 399
			AcceptConditions.NotAccept, // 400
			AcceptConditions.NotAccept, // 401
			AcceptConditions.NotAccept, // 402
			AcceptConditions.NotAccept, // 403
			AcceptConditions.NotAccept, // 404
			AcceptConditions.NotAccept, // 405
			AcceptConditions.NotAccept, // 406
			AcceptConditions.NotAccept, // 407
			AcceptConditions.NotAccept, // 408
			AcceptConditions.NotAccept, // 409
			AcceptConditions.NotAccept, // 410
			AcceptConditions.NotAccept, // 411
			AcceptConditions.NotAccept, // 412
			AcceptConditions.NotAccept, // 413
			AcceptConditions.NotAccept, // 414
			AcceptConditions.NotAccept, // 415
			AcceptConditions.NotAccept, // 416
			AcceptConditions.NotAccept, // 417
			AcceptConditions.NotAccept, // 418
			AcceptConditions.NotAccept, // 419
			AcceptConditions.NotAccept, // 420
			AcceptConditions.NotAccept, // 421
			AcceptConditions.NotAccept, // 422
			AcceptConditions.NotAccept, // 423
			AcceptConditions.NotAccept, // 424
			AcceptConditions.NotAccept, // 425
			AcceptConditions.NotAccept, // 426
			AcceptConditions.NotAccept, // 427
			AcceptConditions.NotAccept, // 428
			AcceptConditions.NotAccept, // 429
			AcceptConditions.NotAccept, // 430
			AcceptConditions.NotAccept, // 431
			AcceptConditions.NotAccept, // 432
			AcceptConditions.NotAccept, // 433
			AcceptConditions.NotAccept, // 434
			AcceptConditions.NotAccept, // 435
			AcceptConditions.NotAccept, // 436
			AcceptConditions.NotAccept, // 437
			AcceptConditions.NotAccept, // 438
			AcceptConditions.NotAccept, // 439
			AcceptConditions.NotAccept, // 440
			AcceptConditions.NotAccept, // 441
			AcceptConditions.NotAccept, // 442
			AcceptConditions.NotAccept, // 443
			AcceptConditions.NotAccept, // 444
			AcceptConditions.NotAccept, // 445
			AcceptConditions.NotAccept, // 446
			AcceptConditions.NotAccept, // 447
			AcceptConditions.NotAccept, // 448
			AcceptConditions.NotAccept, // 449
			AcceptConditions.NotAccept, // 450
			AcceptConditions.NotAccept, // 451
			AcceptConditions.NotAccept, // 452
			AcceptConditions.NotAccept, // 453
			AcceptConditions.NotAccept, // 454
			AcceptConditions.NotAccept, // 455
			AcceptConditions.NotAccept, // 456
			AcceptConditions.NotAccept, // 457
			AcceptConditions.NotAccept, // 458
			AcceptConditions.NotAccept, // 459
			AcceptConditions.NotAccept, // 460
			AcceptConditions.NotAccept, // 461
			AcceptConditions.NotAccept, // 462
			AcceptConditions.NotAccept, // 463
			AcceptConditions.NotAccept, // 464
			AcceptConditions.NotAccept, // 465
			AcceptConditions.NotAccept, // 466
			AcceptConditions.NotAccept, // 467
			AcceptConditions.NotAccept, // 468
			AcceptConditions.NotAccept, // 469
			AcceptConditions.NotAccept, // 470
			AcceptConditions.NotAccept, // 471
			AcceptConditions.NotAccept, // 472
			AcceptConditions.NotAccept, // 473
			AcceptConditions.NotAccept, // 474
			AcceptConditions.NotAccept, // 475
			AcceptConditions.NotAccept, // 476
			AcceptConditions.NotAccept, // 477
			AcceptConditions.NotAccept, // 478
			AcceptConditions.NotAccept, // 479
			AcceptConditions.NotAccept, // 480
			AcceptConditions.NotAccept, // 481
			AcceptConditions.NotAccept, // 482
			AcceptConditions.NotAccept, // 483
			AcceptConditions.NotAccept, // 484
			AcceptConditions.NotAccept, // 485
			AcceptConditions.NotAccept, // 486
			AcceptConditions.NotAccept, // 487
			AcceptConditions.NotAccept, // 488
			AcceptConditions.NotAccept, // 489
			AcceptConditions.NotAccept, // 490
			AcceptConditions.NotAccept, // 491
			AcceptConditions.NotAccept, // 492
			AcceptConditions.NotAccept, // 493
			AcceptConditions.NotAccept, // 494
			AcceptConditions.NotAccept, // 495
			AcceptConditions.NotAccept, // 496
			AcceptConditions.NotAccept, // 497
			AcceptConditions.NotAccept, // 498
			AcceptConditions.NotAccept, // 499
			AcceptConditions.NotAccept, // 500
			AcceptConditions.NotAccept, // 501
			AcceptConditions.NotAccept, // 502
			AcceptConditions.NotAccept, // 503
			AcceptConditions.NotAccept, // 504
			AcceptConditions.NotAccept, // 505
			AcceptConditions.NotAccept, // 506
			AcceptConditions.NotAccept, // 507
			AcceptConditions.NotAccept, // 508
			AcceptConditions.NotAccept, // 509
			AcceptConditions.NotAccept, // 510
			AcceptConditions.NotAccept, // 511
			AcceptConditions.NotAccept, // 512
			AcceptConditions.NotAccept, // 513
			AcceptConditions.NotAccept, // 514
			AcceptConditions.NotAccept, // 515
			AcceptConditions.NotAccept, // 516
			AcceptConditions.NotAccept, // 517
			AcceptConditions.NotAccept, // 518
			AcceptConditions.NotAccept, // 519
			AcceptConditions.NotAccept, // 520
			AcceptConditions.NotAccept, // 521
			AcceptConditions.NotAccept, // 522
			AcceptConditions.NotAccept, // 523
			AcceptConditions.NotAccept, // 524
			AcceptConditions.NotAccept, // 525
			AcceptConditions.NotAccept, // 526
			AcceptConditions.NotAccept, // 527
			AcceptConditions.NotAccept, // 528
			AcceptConditions.NotAccept, // 529
			AcceptConditions.NotAccept, // 530
			AcceptConditions.NotAccept, // 531
			AcceptConditions.NotAccept, // 532
			AcceptConditions.NotAccept, // 533
			AcceptConditions.NotAccept, // 534
			AcceptConditions.NotAccept, // 535
			AcceptConditions.NotAccept, // 536
			AcceptConditions.NotAccept, // 537
			AcceptConditions.NotAccept, // 538
			AcceptConditions.NotAccept, // 539
			AcceptConditions.NotAccept, // 540
			AcceptConditions.NotAccept, // 541
			AcceptConditions.NotAccept, // 542
			AcceptConditions.NotAccept, // 543
			AcceptConditions.NotAccept, // 544
			AcceptConditions.NotAccept, // 545
			AcceptConditions.NotAccept, // 546
			AcceptConditions.NotAccept, // 547
			AcceptConditions.NotAccept, // 548
			AcceptConditions.NotAccept, // 549
			AcceptConditions.NotAccept, // 550
			AcceptConditions.NotAccept, // 551
			AcceptConditions.NotAccept, // 552
			AcceptConditions.NotAccept, // 553
			AcceptConditions.NotAccept, // 554
			AcceptConditions.NotAccept, // 555
			AcceptConditions.NotAccept, // 556
			AcceptConditions.NotAccept, // 557
			AcceptConditions.NotAccept, // 558
			AcceptConditions.NotAccept, // 559
			AcceptConditions.NotAccept, // 560
			AcceptConditions.NotAccept, // 561
			AcceptConditions.NotAccept, // 562
			AcceptConditions.NotAccept, // 563
			AcceptConditions.NotAccept, // 564
			AcceptConditions.NotAccept, // 565
			AcceptConditions.NotAccept, // 566
			AcceptConditions.NotAccept, // 567
			AcceptConditions.NotAccept, // 568
			AcceptConditions.NotAccept, // 569
			AcceptConditions.NotAccept, // 570
			AcceptConditions.NotAccept, // 571
			AcceptConditions.NotAccept, // 572
			AcceptConditions.NotAccept, // 573
			AcceptConditions.NotAccept, // 574
			AcceptConditions.NotAccept, // 575
			AcceptConditions.NotAccept, // 576
			AcceptConditions.NotAccept, // 577
			AcceptConditions.NotAccept, // 578
			AcceptConditions.NotAccept, // 579
			AcceptConditions.NotAccept, // 580
			AcceptConditions.NotAccept, // 581
			AcceptConditions.NotAccept, // 582
			AcceptConditions.NotAccept, // 583
			AcceptConditions.NotAccept, // 584
			AcceptConditions.NotAccept, // 585
			AcceptConditions.NotAccept, // 586
			AcceptConditions.NotAccept, // 587
			AcceptConditions.NotAccept, // 588
			AcceptConditions.NotAccept, // 589
			AcceptConditions.NotAccept, // 590
			AcceptConditions.NotAccept, // 591
			AcceptConditions.NotAccept, // 592
			AcceptConditions.NotAccept, // 593
			AcceptConditions.NotAccept, // 594
			AcceptConditions.NotAccept, // 595
			AcceptConditions.NotAccept, // 596
			AcceptConditions.NotAccept, // 597
			AcceptConditions.NotAccept, // 598
			AcceptConditions.NotAccept, // 599
			AcceptConditions.NotAccept, // 600
			AcceptConditions.NotAccept, // 601
			AcceptConditions.NotAccept, // 602
			AcceptConditions.NotAccept, // 603
			AcceptConditions.NotAccept, // 604
			AcceptConditions.NotAccept, // 605
			AcceptConditions.NotAccept, // 606
			AcceptConditions.NotAccept, // 607
			AcceptConditions.NotAccept, // 608
			AcceptConditions.NotAccept, // 609
			AcceptConditions.NotAccept, // 610
			AcceptConditions.NotAccept, // 611
			AcceptConditions.NotAccept, // 612
			AcceptConditions.NotAccept, // 613
			AcceptConditions.NotAccept, // 614
			AcceptConditions.NotAccept, // 615
			AcceptConditions.NotAccept, // 616
			AcceptConditions.NotAccept, // 617
			AcceptConditions.NotAccept, // 618
			AcceptConditions.NotAccept, // 619
			AcceptConditions.NotAccept, // 620
			AcceptConditions.NotAccept, // 621
			AcceptConditions.NotAccept, // 622
			AcceptConditions.NotAccept, // 623
			AcceptConditions.NotAccept, // 624
			AcceptConditions.NotAccept, // 625
			AcceptConditions.NotAccept, // 626
			AcceptConditions.NotAccept, // 627
			AcceptConditions.NotAccept, // 628
			AcceptConditions.Accept, // 629
			AcceptConditions.Accept, // 630
			AcceptConditions.Accept, // 631
			AcceptConditions.Accept, // 632
			AcceptConditions.Accept, // 633
			AcceptConditions.Accept, // 634
			AcceptConditions.Accept, // 635
			AcceptConditions.Accept, // 636
			AcceptConditions.Accept, // 637
			AcceptConditions.Accept, // 638
			AcceptConditions.Accept, // 639
			AcceptConditions.Accept, // 640
			AcceptConditions.Accept, // 641
			AcceptConditions.NotAccept, // 642
			AcceptConditions.Accept, // 643
			AcceptConditions.Accept, // 644
			AcceptConditions.Accept, // 645
			AcceptConditions.Accept, // 646
			AcceptConditions.Accept, // 647
			AcceptConditions.Accept, // 648
			AcceptConditions.Accept, // 649
			AcceptConditions.Accept, // 650
			AcceptConditions.Accept, // 651
			AcceptConditions.Accept, // 652
			AcceptConditions.Accept, // 653
			AcceptConditions.Accept, // 654
			AcceptConditions.Accept, // 655
			AcceptConditions.Accept, // 656
			AcceptConditions.Accept, // 657
			AcceptConditions.Accept, // 658
			AcceptConditions.Accept, // 659
			AcceptConditions.Accept, // 660
			AcceptConditions.Accept, // 661
			AcceptConditions.Accept, // 662
			AcceptConditions.Accept, // 663
			AcceptConditions.Accept, // 664
			AcceptConditions.Accept, // 665
			AcceptConditions.Accept, // 666
			AcceptConditions.NotAccept, // 667
			AcceptConditions.Accept, // 668
			AcceptConditions.Accept, // 669
			AcceptConditions.NotAccept, // 670
			AcceptConditions.Accept, // 671
			AcceptConditions.NotAccept, // 672
			AcceptConditions.Accept, // 673
			AcceptConditions.Accept, // 674
			AcceptConditions.Accept, // 675
			AcceptConditions.Accept, // 676
			AcceptConditions.Accept, // 677
			AcceptConditions.Accept, // 678
			AcceptConditions.NotAccept, // 679
			AcceptConditions.Accept, // 680
			AcceptConditions.Accept, // 681
			AcceptConditions.NotAccept, // 682
			AcceptConditions.NotAccept, // 683
			AcceptConditions.Accept, // 684
			AcceptConditions.Accept, // 685
			AcceptConditions.Accept, // 686
			AcceptConditions.Accept, // 687
			AcceptConditions.Accept, // 688
			AcceptConditions.NotAccept, // 689
			AcceptConditions.NotAccept, // 690
			AcceptConditions.NotAccept, // 691
			AcceptConditions.NotAccept, // 692
			AcceptConditions.NotAccept, // 693
			AcceptConditions.NotAccept, // 694
			AcceptConditions.NotAccept, // 695
			AcceptConditions.NotAccept, // 696
			AcceptConditions.NotAccept, // 697
			AcceptConditions.NotAccept, // 698
			AcceptConditions.NotAccept, // 699
			AcceptConditions.NotAccept, // 700
			AcceptConditions.NotAccept, // 701
			AcceptConditions.NotAccept, // 702
			AcceptConditions.NotAccept, // 703
			AcceptConditions.NotAccept, // 704
			AcceptConditions.NotAccept, // 705
			AcceptConditions.NotAccept, // 706
			AcceptConditions.NotAccept, // 707
			AcceptConditions.NotAccept, // 708
			AcceptConditions.NotAccept, // 709
			AcceptConditions.NotAccept, // 710
			AcceptConditions.NotAccept, // 711
			AcceptConditions.NotAccept, // 712
			AcceptConditions.NotAccept, // 713
			AcceptConditions.NotAccept, // 714
			AcceptConditions.NotAccept, // 715
			AcceptConditions.NotAccept, // 716
			AcceptConditions.NotAccept, // 717
			AcceptConditions.NotAccept, // 718
			AcceptConditions.NotAccept, // 719
			AcceptConditions.NotAccept, // 720
			AcceptConditions.NotAccept, // 721
			AcceptConditions.NotAccept, // 722
			AcceptConditions.NotAccept, // 723
			AcceptConditions.NotAccept, // 724
			AcceptConditions.NotAccept, // 725
			AcceptConditions.NotAccept, // 726
			AcceptConditions.NotAccept, // 727
			AcceptConditions.NotAccept, // 728
			AcceptConditions.NotAccept, // 729
			AcceptConditions.Accept, // 730
			AcceptConditions.Accept, // 731
			AcceptConditions.Accept, // 732
			AcceptConditions.Accept, // 733
			AcceptConditions.Accept, // 734
			AcceptConditions.NotAccept, // 735
			AcceptConditions.Accept, // 736
			AcceptConditions.Accept, // 737
			AcceptConditions.Accept, // 738
			AcceptConditions.Accept, // 739
			AcceptConditions.Accept, // 740
			AcceptConditions.Accept, // 741
			AcceptConditions.Accept, // 742
			AcceptConditions.Accept, // 743
			AcceptConditions.Accept, // 744
			AcceptConditions.Accept, // 745
			AcceptConditions.NotAccept, // 746
			AcceptConditions.Accept, // 747
			AcceptConditions.Accept, // 748
			AcceptConditions.NotAccept, // 749
			AcceptConditions.Accept, // 750
			AcceptConditions.NotAccept, // 751
			AcceptConditions.NotAccept, // 752
			AcceptConditions.NotAccept, // 753
			AcceptConditions.NotAccept, // 754
			AcceptConditions.NotAccept, // 755
			AcceptConditions.NotAccept, // 756
			AcceptConditions.NotAccept, // 757
			AcceptConditions.NotAccept, // 758
			AcceptConditions.NotAccept, // 759
			AcceptConditions.NotAccept, // 760
			AcceptConditions.NotAccept, // 761
			AcceptConditions.NotAccept, // 762
			AcceptConditions.Accept, // 763
			AcceptConditions.Accept, // 764
			AcceptConditions.NotAccept, // 765
			AcceptConditions.Accept, // 766
			AcceptConditions.Accept, // 767
			AcceptConditions.Accept, // 768
			AcceptConditions.Accept, // 769
			AcceptConditions.Accept, // 770
			AcceptConditions.Accept, // 771
			AcceptConditions.NotAccept, // 772
			AcceptConditions.Accept, // 773
			AcceptConditions.NotAccept, // 774
			AcceptConditions.NotAccept, // 775
			AcceptConditions.NotAccept, // 776
			AcceptConditions.NotAccept, // 777
			AcceptConditions.NotAccept, // 778
			AcceptConditions.Accept, // 779
			AcceptConditions.NotAccept, // 780
			AcceptConditions.Accept, // 781
			AcceptConditions.Accept, // 782
			AcceptConditions.Accept, // 783
			AcceptConditions.NotAccept, // 784
			AcceptConditions.Accept, // 785
			AcceptConditions.NotAccept, // 786
			AcceptConditions.NotAccept, // 787
			AcceptConditions.NotAccept, // 788
			AcceptConditions.NotAccept, // 789
			AcceptConditions.Accept, // 790
			AcceptConditions.Accept, // 791
			AcceptConditions.Accept, // 792
			AcceptConditions.Accept, // 793
			AcceptConditions.NotAccept, // 794
			AcceptConditions.Accept, // 795
			AcceptConditions.NotAccept, // 796
			AcceptConditions.NotAccept, // 797
			AcceptConditions.NotAccept, // 798
			AcceptConditions.Accept, // 799
			AcceptConditions.Accept, // 800
			AcceptConditions.NotAccept, // 801
			AcceptConditions.Accept, // 802
			AcceptConditions.NotAccept, // 803
			AcceptConditions.NotAccept, // 804
			AcceptConditions.NotAccept, // 805
			AcceptConditions.Accept, // 806
			AcceptConditions.Accept, // 807
			AcceptConditions.NotAccept, // 808
			AcceptConditions.NotAccept, // 809
			AcceptConditions.NotAccept, // 810
			AcceptConditions.NotAccept, // 811
			AcceptConditions.Accept, // 812
			AcceptConditions.Accept, // 813
			AcceptConditions.NotAccept, // 814
			AcceptConditions.NotAccept, // 815
			AcceptConditions.NotAccept, // 816
			AcceptConditions.Accept, // 817
			AcceptConditions.Accept, // 818
			AcceptConditions.NotAccept, // 819
			AcceptConditions.NotAccept, // 820
			AcceptConditions.Accept, // 821
			AcceptConditions.Accept, // 822
			AcceptConditions.NotAccept, // 823
			AcceptConditions.NotAccept, // 824
			AcceptConditions.Accept, // 825
			AcceptConditions.NotAccept, // 826
			AcceptConditions.NotAccept, // 827
			AcceptConditions.Accept, // 828
			AcceptConditions.NotAccept, // 829
			AcceptConditions.NotAccept, // 830
			AcceptConditions.Accept, // 831
			AcceptConditions.NotAccept, // 832
			AcceptConditions.Accept, // 833
			AcceptConditions.NotAccept, // 834
			AcceptConditions.Accept, // 835
			AcceptConditions.NotAccept, // 836
			AcceptConditions.Accept, // 837
			AcceptConditions.NotAccept, // 838
			AcceptConditions.NotAccept, // 839
			AcceptConditions.NotAccept, // 840
			AcceptConditions.NotAccept, // 841
			AcceptConditions.NotAccept, // 842
			AcceptConditions.NotAccept, // 843
			AcceptConditions.NotAccept, // 844
			AcceptConditions.Accept, // 845
			AcceptConditions.Accept, // 846
			AcceptConditions.Accept, // 847
			AcceptConditions.Accept, // 848
			AcceptConditions.Accept, // 849
			AcceptConditions.Accept, // 850
			AcceptConditions.Accept, // 851
			AcceptConditions.Accept, // 852
			AcceptConditions.Accept, // 853
			AcceptConditions.Accept, // 854
			AcceptConditions.Accept, // 855
			AcceptConditions.Accept, // 856
			AcceptConditions.Accept, // 857
			AcceptConditions.Accept, // 858
			AcceptConditions.Accept, // 859
			AcceptConditions.Accept, // 860
			AcceptConditions.Accept, // 861
			AcceptConditions.Accept, // 862
			AcceptConditions.Accept, // 863
			AcceptConditions.NotAccept, // 864
			AcceptConditions.NotAccept, // 865
			AcceptConditions.NotAccept, // 866
			AcceptConditions.NotAccept, // 867
			AcceptConditions.NotAccept, // 868
			AcceptConditions.NotAccept, // 869
			AcceptConditions.Accept, // 870
			AcceptConditions.Accept, // 871
			AcceptConditions.Accept, // 872
			AcceptConditions.Accept, // 873
			AcceptConditions.Accept, // 874
			AcceptConditions.Accept, // 875
			AcceptConditions.Accept, // 876
			AcceptConditions.Accept, // 877
			AcceptConditions.Accept, // 878
			AcceptConditions.Accept, // 879
			AcceptConditions.Accept, // 880
			AcceptConditions.Accept, // 881
			AcceptConditions.Accept, // 882
			AcceptConditions.Accept, // 883
			AcceptConditions.Accept, // 884
			AcceptConditions.Accept, // 885
			AcceptConditions.Accept, // 886
			AcceptConditions.NotAccept, // 887
			AcceptConditions.NotAccept, // 888
			AcceptConditions.Accept, // 889
			AcceptConditions.Accept, // 890
			AcceptConditions.Accept, // 891
			AcceptConditions.Accept, // 892
			AcceptConditions.Accept, // 893
			AcceptConditions.NotAccept, // 894
			AcceptConditions.NotAccept, // 895
			AcceptConditions.Accept, // 896
			AcceptConditions.Accept, // 897
			AcceptConditions.Accept, // 898
			AcceptConditions.NotAccept, // 899
			AcceptConditions.Accept, // 900
			AcceptConditions.NotAccept, // 901
			AcceptConditions.Accept, // 902
			AcceptConditions.NotAccept, // 903
			AcceptConditions.Accept, // 904
			AcceptConditions.NotAccept, // 905
			AcceptConditions.NotAccept, // 906
			AcceptConditions.NotAccept, // 907
			AcceptConditions.NotAccept, // 908
			AcceptConditions.NotAccept, // 909
			AcceptConditions.NotAccept, // 910
			AcceptConditions.Accept, // 911
			AcceptConditions.Accept, // 912
			AcceptConditions.Accept, // 913
			AcceptConditions.Accept, // 914
			AcceptConditions.Accept, // 915
			AcceptConditions.Accept, // 916
			AcceptConditions.Accept, // 917
			AcceptConditions.Accept, // 918
			AcceptConditions.Accept, // 919
			AcceptConditions.Accept, // 920
			AcceptConditions.Accept, // 921
			AcceptConditions.Accept, // 922
			AcceptConditions.Accept, // 923
			AcceptConditions.NotAccept, // 924
			AcceptConditions.NotAccept, // 925
			AcceptConditions.NotAccept, // 926
			AcceptConditions.NotAccept, // 927
			AcceptConditions.NotAccept, // 928
			AcceptConditions.NotAccept, // 929
			AcceptConditions.NotAccept, // 930
			AcceptConditions.NotAccept, // 931
			AcceptConditions.NotAccept, // 932
			AcceptConditions.NotAccept, // 933
			AcceptConditions.NotAccept, // 934
			AcceptConditions.NotAccept, // 935
			AcceptConditions.NotAccept, // 936
			AcceptConditions.NotAccept, // 937
			AcceptConditions.NotAccept, // 938
			AcceptConditions.Accept, // 939
			AcceptConditions.Accept, // 940
			AcceptConditions.Accept, // 941
			AcceptConditions.NotAccept, // 942
			AcceptConditions.Accept, // 943
			AcceptConditions.NotAccept, // 944
			AcceptConditions.Accept, // 945
			AcceptConditions.Accept, // 946
			AcceptConditions.Accept, // 947
			AcceptConditions.Accept, // 948
			AcceptConditions.NotAccept, // 949
			AcceptConditions.Accept, // 950
			AcceptConditions.Accept, // 951
			AcceptConditions.NotAccept, // 952
			AcceptConditions.Accept, // 953
			AcceptConditions.Accept, // 954
			AcceptConditions.NotAccept, // 955
			AcceptConditions.Accept, // 956
			AcceptConditions.NotAccept, // 957
			AcceptConditions.Accept, // 958
			AcceptConditions.NotAccept, // 959
			AcceptConditions.Accept, // 960
			AcceptConditions.NotAccept, // 961
			AcceptConditions.NotAccept, // 962
			AcceptConditions.Accept, // 963
			AcceptConditions.NotAccept, // 964
			AcceptConditions.NotAccept, // 965
			AcceptConditions.Accept, // 966
			AcceptConditions.NotAccept, // 967
			AcceptConditions.Accept, // 968
			AcceptConditions.NotAccept, // 969
			AcceptConditions.Accept, // 970
			AcceptConditions.Accept, // 971
			AcceptConditions.Accept, // 972
			AcceptConditions.Accept, // 973
			AcceptConditions.Accept, // 974
			AcceptConditions.NotAccept, // 975
			AcceptConditions.Accept, // 976
			AcceptConditions.Accept, // 977
			AcceptConditions.NotAccept, // 978
			AcceptConditions.Accept, // 979
			AcceptConditions.NotAccept, // 980
			AcceptConditions.Accept, // 981
			AcceptConditions.Accept, // 982
			AcceptConditions.Accept, // 983
			AcceptConditions.Accept, // 984
			AcceptConditions.NotAccept, // 985
			AcceptConditions.Accept, // 986
			AcceptConditions.Accept, // 987
			AcceptConditions.Accept, // 988
			AcceptConditions.Accept, // 989
			AcceptConditions.Accept, // 990
			AcceptConditions.Accept, // 991
			AcceptConditions.NotAccept, // 992
		};
		
		private static int[] colMap = new int[]
		{
			2, 3, 3, 3, 3, 3, 3, 3, 3, 2, 2, 3, 3, 2, 3, 3, 
			3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 
			4, 3, 3, 3, 3, 3, 3, 3, 5, 6, 3, 7, 8, 9, 10, 11, 
			12, 13, 14, 15, 16, 17, 18, 19, 20, 20, 21, 3, 3, 3, 3, 3, 
			22, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 
			3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 23, 
			3, 24, 25, 26, 27, 28, 1, 29, 30, 31, 32, 33, 34, 35, 36, 37, 
			38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 39, 3, 3, 3, 3, 3, 
			3, 3
		};
		
		private static int[] rowMap = new int[]
		{
			0, 1, 2, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 11, 12, 13, 
			14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 2, 28, 
			2, 20, 2, 20, 29, 30, 31, 32, 33, 32, 34, 35, 36, 20, 37, 38, 
			2, 2, 39, 40, 41, 42, 43, 44, 2, 45, 46, 2, 47, 48, 49, 24, 
			50, 51, 52, 53, 54, 55, 56, 57, 58, 2, 59, 60, 61, 62, 2, 2, 
			2, 63, 2, 64, 65, 66, 67, 68, 10, 69, 70, 71, 72, 73, 74, 75, 
			30, 76, 77, 78, 79, 80, 81, 82, 83, 84, 85, 2, 24, 2, 86, 87, 
			88, 89, 90, 91, 92, 93, 2, 94, 95, 96, 97, 98, 99, 100, 101, 102, 
			103, 104, 105, 2, 106, 107, 108, 109, 110, 111, 112, 113, 2, 114, 115, 116, 
			117, 118, 119, 120, 121, 122, 123, 124, 125, 126, 127, 128, 129, 130, 131, 132, 
			133, 134, 135, 2, 136, 137, 138, 139, 140, 141, 142, 143, 144, 145, 146, 147, 
			148, 149, 150, 151, 152, 153, 154, 155, 156, 157, 158, 2, 159, 160, 161, 162, 
			163, 164, 165, 166, 167, 168, 169, 170, 171, 172, 173, 174, 175, 176, 177, 178, 
			179, 180, 18, 181, 182, 183, 184, 185, 42, 186, 187, 188, 189, 190, 191, 192, 
			193, 194, 195, 196, 197, 198, 199, 200, 201, 24, 202, 203, 204, 205, 206, 207, 
			2, 208, 209, 210, 211, 15, 21, 212, 213, 214, 215, 216, 217, 218, 219, 220, 
			178, 221, 222, 223, 224, 225, 226, 227, 205, 228, 229, 230, 210, 231, 232, 233, 
			234, 235, 236, 237, 238, 239, 240, 241, 242, 243, 244, 245, 246, 247, 248, 192, 
			249, 250, 251, 237, 252, 239, 253, 254, 255, 256, 257, 258, 259, 260, 261, 262, 
			263, 264, 265, 266, 52, 267, 268, 269, 270, 271, 272, 273, 274, 275, 276, 277, 
			278, 279, 280, 281, 282, 283, 284, 285, 286, 287, 288, 289, 290, 291, 292, 293, 
			294, 295, 296, 297, 298, 299, 300, 301, 302, 303, 304, 305, 306, 307, 308, 309, 
			310, 311, 312, 313, 314, 315, 316, 317, 318, 319, 320, 321, 322, 323, 324, 325, 
			326, 327, 20, 328, 329, 330, 331, 332, 333, 334, 335, 336, 337, 338, 339, 340, 
			341, 342, 343, 344, 345, 346, 347, 348, 349, 350, 351, 352, 353, 354, 355, 356, 
			357, 358, 359, 360, 361, 362, 363, 364, 365, 366, 367, 368, 369, 370, 371, 372, 
			373, 374, 375, 376, 377, 378, 379, 380, 381, 382, 383, 384, 385, 386, 387, 388, 
			389, 390, 391, 392, 393, 394, 395, 396, 397, 398, 399, 400, 401, 402, 403, 22, 
			44, 60, 73, 404, 405, 406, 407, 408, 409, 410, 204, 411, 412, 413, 414, 415, 
			416, 417, 418, 419, 420, 421, 422, 423, 424, 425, 426, 427, 428, 429, 430, 431, 
			432, 230, 433, 434, 435, 436, 437, 438, 439, 440, 441, 442, 443, 444, 445, 446, 
			447, 448, 449, 450, 451, 452, 453, 454, 455, 456, 457, 458, 459, 460, 461, 462, 
			463, 464, 465, 466, 467, 468, 469, 470, 471, 472, 473, 474, 475, 476, 477, 478, 
			479, 480, 481, 482, 483, 450, 484, 485, 486, 487, 488, 489, 172, 490, 491, 492, 
			493, 494, 495, 496, 497, 498, 499, 500, 501, 502, 503, 504, 505, 506, 507, 508, 
			509, 510, 511, 512, 513, 514, 515, 516, 517, 139, 518, 519, 520, 521, 522, 523, 
			524, 525, 526, 527, 528, 116, 529, 530, 531, 532, 533, 534, 535, 536, 537, 538, 
			539, 540, 541, 542, 543, 507, 544, 518, 545, 546, 547, 548, 549, 550, 551, 552, 
			553, 554, 555, 556, 557, 558, 30, 48, 559, 560, 550, 561, 562, 563, 564, 565, 
			566, 567, 568, 569, 570, 571, 572, 573, 574, 575, 576, 577, 578, 579, 580, 581, 
			582, 583, 584, 585, 586, 587, 588, 589, 590, 591, 592, 593, 594, 595, 596, 597, 
			598, 599, 600, 601, 602, 603, 604, 605, 606, 607, 608, 609, 610, 611, 612, 512, 
			613, 614, 615, 616, 617, 582, 618, 619, 620, 224, 621, 622, 623, 624, 625, 626, 
			627, 628, 629, 630, 631, 632, 633, 634, 635, 636, 637, 204, 638, 639, 453, 640, 
			641, 642, 643, 644, 645, 646, 647, 455, 648, 649, 650, 651, 652, 653, 505, 654, 
			655, 496, 656, 537, 657, 658, 659, 660, 661, 662, 663, 664, 665, 666, 667, 668, 
			669, 670, 671, 672, 673, 674, 675, 676, 677, 678, 679, 680, 681, 682, 683, 684, 
			318, 21, 685, 686, 687, 688, 689, 431, 690, 691, 692, 693, 144, 694, 695, 696, 
			697, 698, 699, 700, 701, 702, 703, 704, 705, 453, 706, 707, 708, 709, 710, 711, 
			712, 713, 714, 715, 716, 717, 718, 719, 720, 721, 722, 723, 724, 725, 450, 726, 
			727, 728, 729, 730, 731, 732, 733, 734, 735, 736, 737, 738, 739, 740, 741, 742, 
			743, 744, 745, 746, 747, 748, 11, 749, 750, 751, 752, 753, 754, 755, 756, 757, 
			758, 759, 760, 761, 762, 763, 764, 765, 766, 767, 768, 769, 770, 771, 772, 665, 
			773, 774, 775, 776, 777, 778, 779, 780, 781, 782, 783, 784, 785, 786, 787, 788, 
			789, 790, 791, 792, 793, 794, 795, 796, 797, 798, 799, 800, 801, 802, 803, 804, 
			805, 806, 97, 124, 807, 808, 809, 810, 811, 812, 813, 814, 815, 816, 817, 818, 
			819, 820, 821, 822, 823, 824, 825, 826, 827, 828, 829, 830, 831, 26, 832, 833, 
			834, 835, 836, 837, 838, 839, 840, 841, 842, 843, 844, 845, 846, 847, 848, 849, 
			850, 851, 852, 853, 854, 855, 856, 857, 858, 859, 860, 861, 862, 863, 864, 865, 
			866, 867, 868, 869, 870, 871, 872, 873, 874, 875, 876, 877, 878, 879, 880, 881, 
			882, 803, 883, 884, 885, 886, 887, 888, 889, 890, 891, 20, 892, 893, 894, 895, 
			896, 897, 898, 899, 900, 901, 902, 903, 904, 905, 906, 907, 908, 909, 910, 911, 
			912
		};
		
		private static readonly int[,] nextState = new int[,]
		{
			{ -1, 1, 2, 3, 41, 42, 3, 65, 2, 65, 2, 3, 83, 99, 649, 738, 767, 767, 767, 767, 767, 3, 114, 3, 629, 730, 730, 763, 873, 730, 730, 40, 779, 730, 889, 790, 799, 806, 986, 730, 730, 812, 817, 730, 636, 821, 870, 990 },
			{ -1, 64, -1, -1, -1, -1, 82, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 64, 64, 64, 64, 643, 64, 64, 854, 64, 64, 64, 64, 64, 911, 64, 64, 98, 64, 64, 64, 64, 64, 64, 64 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 308, -1, -1, -1, -1, 308, 308, -1, 10, 10, 10, 10, 10, 10, 10, 10, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 637, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 66, -1, 66, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 5, 5, 5, 5, 5, 5, 5, 5, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 764, -1, -1, -1, 764, -1, 764, -1, 44, 44, 44, 44, 44, 44, 44, 44, 44, -1, -1, -1, -1, -1, -1, 764, -1, -1, 764, -1, -1, -1, -1, -1, 67, -1, -1, -1, 67, 85, 101, -1, -1, -1, -1, -1 },
			{ -1, 245, -1, -1, -1, -1, 82, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 245, 245, 245, 773, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245 },
			{ -1, 102, -1, -1, 336, 337, -1, 338, -1, 339, 340, -1, 46, 46, 46, 46, 46, 46, 46, 46, 46, 341, -1, -1, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 343, 69, 69, 69, 69, 69, 69, 69, 69, 69, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 344, -1, -1, -1, 344, 345, 346, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 847, 847, 847, 847, 847, 847, 847, 847, 847, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 49, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 245, -1, -1, -1, -1, 82, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 702, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 377, -1, -1, 378, -1, -1, -1, -1, 379, 380, 381, 382, 383, 384, 385, 386, 386, 387, 63, 63, -1, -1, -1, 700, -1, -1, 388, -1, -1, 196, 19, 701, -1, -1, 389, 703, 390, -1, -1, -1, 391, 682, -1, 52, 392, 852, 683 },
			{ -1, 377, -1, -1, 378, -1, -1, -1, -1, 379, 380, 381, 642, 735, 765, 780, 63, 63, 63, 63, 63, -1, -1, -1, 700, -1, -1, 388, -1, -1, 196, 19, 701, -1, -1, 389, 703, 390, -1, -1, -1, 391, 682, -1, 52, 392, 852, 683 },
			{ -1, 370, -1, -1, -1, -1, 82, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 645, 645, 645, 645, 645, 645, 645, 645, 645, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 416, -1, -1, -1, 416, 417, 418, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 639, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 73, -1, 73, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 369, -1, 677, 677, 677, 677, 677, 677, 677, 677, 677, 369, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 74, -1, -1, 460, 461, -1, 462, -1, 462, 464, -1, 54, 54, 54, 54, 54, 54, 54, 54, 54, 464, -1, -1, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74 },
			{ -1, 370, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 481, 481, 481, 481, 481, 481, 481, 481, 481, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 282, -1, -1, 284, -1, -1, -1, -1, -1, -1, -1, 63, 26, 26, 26, 26, 26, 26, 26, 26, -1, -1, -1, -1, -1, -1, 286, -1, -1, 196, -1, -1, -1, -1, 288, -1, -1, -1, -1, -1, 290, 682, -1, -1, 244, -1, 683 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 484, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 282, -1, -1, 284, -1, -1, -1, -1, -1, -1, -1, 63, 63, 63, 63, 63, 63, 63, 63, 63, -1, -1, -1, -1, -1, -1, 286, -1, -1, 196, -1, -1, -1, -1, 288, -1, -1, -1, -1, -1, 290, 682, -1, -1, 244, -1, 683 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 32, 32, 32, 32, 32, 32, 32, 32, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 370, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 370, 370, 370, 841, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 849, 849, 849, 849, 849, 849, 849, 849, 849, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 282, -1, -1, 284, -1, -1, -1, -1, -1, -1, -1, 63, 63, 63, 63, 63, 63, 63, 63, 63, -1, -1, -1, -1, -1, -1, 286, -1, -1, 196, -1, -1, -1, -1, 288, -1, -1, -1, -1, -1, 290, 554, -1, -1, 244, -1, 683 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 548, -1, -1, -1, 548, 549, 594, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 79, 79, 79, 79, 79, 79, 79, 79, 79, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 733, 733, 733, 733, 733, 733, 733, 733, 733, 62, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 39, -1, -1, -1, -1, -1, -1, -1, 63, 63, 63, 63, 63, 63, 63, 63, 63, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 64, -1, -1, 278, -1, 82, -1, -1, 278, 278, -1, 6, 6, 6, 630, 731, 731, 731, 731, 731, -1, -1, -1, 64, 64, 64, 64, 64, 64, 64, 148, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 159, 64, 159, 64 },
			{ -1, 113, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 113, 113, 113, 113, 113, 113, 113, 113, 113, 113, 113, 113, 113, 113, 113, 113, 113, 113, 113, 113, 113, 113, 113, 113 },
			{ -1, -1, -1, -1, 308, -1, -1, -1, -1, 308, 308, -1, 10, 10, 10, 10, 10, 10, 10, 10, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 850, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 764, -1, -1, -1, 764, -1, 128, -1, 139, 139, 150, 161, 161, 161, 161, 161, 161, 369, -1, -1, -1, -1, -1, 764, -1, -1, 764, -1, -1, -1, -1, -1, 67, -1, -1, -1, 67, 85, 101, -1, -1, -1, -1, -1 },
			{ -1, 102, -1, -1, 336, 337, -1, 338, -1, 339, 340, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 341, -1, -1, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 343, 69, 69, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 344, -1, -1, -1, 344, 345, 346, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 163, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 645, 645, 753, 753, 753, 753, 753, 753, 753, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 416, -1, -1, -1, 416, 417, 418, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 852, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 677, 677, 677, 677, 677, 677, 677, 677, 677, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 74, -1, -1, 460, 461, -1, 462, -1, 462, 464, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 464, -1, -1, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74 },
			{ -1, 282, -1, -1, 284, -1, -1, -1, -1, -1, -1, -1, 57, 76, 76, 94, 26, 26, 26, 26, 26, -1, -1, -1, -1, -1, -1, 286, -1, -1, 196, -1, -1, -1, -1, 288, -1, -1, -1, -1, -1, 290, 682, -1, -1, 244, -1, 683 },
			{ -1, 282, -1, -1, 284, -1, -1, -1, -1, -1, -1, -1, 63, 31, 31, 31, 31, 31, 31, 31, 31, -1, -1, -1, -1, -1, -1, 286, -1, -1, 196, -1, -1, -1, -1, 288, -1, -1, -1, -1, -1, 290, 682, -1, -1, 244, -1, 683 },
			{ -1, 370, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 971, 370, 370, 370, 370, 370, 370 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 78, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 641, 641, 641, 641, 641, 641, 80, 80, 80, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 822, -1, -1, -1, -1, 82, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822 },
			{ -1, -1, -1, -1, 39, -1, -1, -1, -1, -1, -1, -1, 126, 126, 657, 741, 741, 741, 741, 741, 741, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 308, -1, -1, -1, -1, 308, 308, -1, 10, 10, 10, 10, 10, 10, 10, 10, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 764, -1, -1, -1, 764, -1, 764, -1, 768, 768, 782, 792, 792, 792, 792, 792, 792, -1, -1, -1, -1, -1, -1, 172, -1, -1, 764, -1, -1, -1, -1, -1, 764, -1, -1, -1, 764, 764, 764, -1, -1, -1, -1, -1 },
			{ -1, 102, -1, -1, 336, 337, -1, 338, -1, 338, 341, -1, 638, 638, 638, 638, 638, 638, 638, 638, 638, 341, -1, -1, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 343, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 344, -1, -1, -1, 344, 345, 346, -1, -1, -1, -1, -1 },
			{ -1, 659, -1, -1, -1, -1, 140, -1, -1, -1, 49, 419, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 419, 659, 659, 659, 659, 659, 659, 659, 659, 659, 659, 659, 659, 659, 659, 659, 659, 659, 659, 659, 659, 659, 659, 659, 659 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 163, -1, 397, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 753, 753, 753, 753, 753, 753, 753, 753, 753, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 416, -1, -1, -1, 416, 417, 418, -1, -1, -1, -1, -1 },
			{ -1, 92, -1, -1, -1, -1, 107, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 92, 92, 92, 92, 92, 92, 92, 92, 92, 92, 92, 92, 92, 92, 92, 92, 92, 92, 92, 92, 92, 92, 92, 92 },
			{ -1, 282, -1, -1, 284, -1, -1, -1, -1, -1, -1, -1, 26, 26, 26, 26, 26, 26, 26, 26, 26, -1, -1, -1, -1, -1, -1, 286, -1, -1, 196, -1, -1, -1, -1, 288, -1, -1, -1, -1, -1, 290, 682, -1, -1, 244, -1, 683 },
			{ -1, 282, -1, -1, 284, -1, -1, -1, -1, -1, -1, -1, 31, 31, 31, 31, 31, 31, 31, 31, 31, -1, -1, -1, -1, -1, -1, 286, -1, -1, 196, -1, -1, -1, -1, 288, -1, -1, -1, -1, -1, 290, 682, -1, -1, 244, -1, 683 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 723, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 292, -1, -1, -1, -1, -1, -1, -1, -1, 294, -1, -1, 295, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 81, -1, -1, 97, -1, -1, -1, -1, 112, 125, 136, 147, 158, 158, 158, 158, 158, 158, 158, 158, 169, -1, -1, 179, -1, -1, 189, -1, -1, 196, 4, 202, -1, -1, 208, 214, 220, -1, -1, 226, 232, 238, -1, 43, 244, 850, 683 },
			{ -1, -1, -1, -1, 308, -1, -1, -1, -1, 308, 308, -1, 10, 10, 10, 10, 10, 10, 10, 10, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 373, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 764, -1, -1, -1, 764, -1, 764, -1, 768, 768, 782, 792, 792, 792, 792, 792, 792, -1, -1, -1, -1, -1, -1, 764, -1, -1, 764, -1, -1, -1, -1, -1, 764, -1, -1, -1, 764, 764, 172, -1, -1, -1, -1, -1 },
			{ -1, 102, -1, -1, 336, 337, -1, 338, -1, 339, 694, -1, 117, 117, 117, 117, 117, 117, 117, 117, 117, 695, -1, -1, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 856, 856, 856, 856, 856, 856, 856, 856, 856, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 702, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 398, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 120, 120, 120, 120, 120, 120, 120, 120, 120, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 416, -1, -1, -1, 416, 417, 418, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 720, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 648, -1, -1, -1, -1, 107, -1, -1, -1, -1, 537, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 537, 648, 648, 648, 648, 648, 648, 648, 648, 648, 648, 648, 648, 648, 648, 648, 648, 648, 648, 648, 648, 648, 648, 648, 648 },
			{ -1, 282, -1, -1, 284, -1, -1, -1, -1, -1, -1, -1, 26, 26, 26, 26, 26, 26, 26, 63, 63, -1, -1, -1, -1, -1, -1, 286, -1, -1, 196, -1, -1, -1, -1, 288, -1, -1, -1, -1, -1, 290, 682, -1, -1, 244, -1, 683 },
			{ -1, 282, -1, -1, 284, -1, -1, -1, -1, -1, -1, -1, 31, 31, 63, 63, 63, 63, 63, 63, 63, -1, -1, -1, -1, -1, -1, 286, -1, -1, 196, -1, -1, -1, -1, 288, -1, -1, -1, -1, -1, 290, 682, -1, -1, 244, -1, 683 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 187, -1, -1, -1, -1, -1, -1 },
			{ -1, 81, -1, -1, 97, -1, -1, -1, -1, 296, 296, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 179, -1, -1, 189, -1, -1, 196, 4, 202, -1, -1, 208, 670, 220, -1, -1, -1, 690, 682, -1, 43, 244, 850, 683 },
			{ -1, 822, -1, -1, -1, -1, 82, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 822, 822, 822, 822, 822, 822, 822, 7, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822 },
			{ -1, 81, -1, -1, 249, -1, -1, -1, -1, 112, 253, 136, 158, 158, 158, 257, 257, 257, 257, 257, 257, 261, -1, -1, 265, -1, -1, 189, -1, -1, 196, 4, 202, -1, -1, 208, 214, 220, 269, -1, 226, 232, 238, -1, 43, 244, 850, 683 },
			{ -1, -1, -1, -1, 308, -1, -1, -1, -1, 308, 308, -1, 10, 10, 10, 10, 10, 10, 10, 10, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 393, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 764, -1, -1, -1, 764, -1, 764, -1, 768, 768, 782, 792, 792, 792, 792, 792, 792, -1, -1, -1, -1, -1, -1, 764, -1, -1, 172, -1, -1, -1, -1, -1, 764, -1, -1, -1, 764, 764, 764, -1, -1, -1, -1, -1 },
			{ -1, 129, -1, -1, -1, -1, 140, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 129, 129, 129, 129, 129, 129, 129, 129, 129, 129, 129, 129, 129, 129, 129, 129, 129, 129, 129, 129, 129, 129, 129, 129 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 343, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 401, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 120, 120, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 416, -1, -1, -1, 416, 417, 418, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 529, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 187, -1, 566, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 135, 135, 135, 135, 135, 135, 135, 135, 135, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 749, -1, -1, 296, -1, -1, -1, -1, 296, 296, -1, 297, 298, 299, 299, 299, 299, 299, 299, 299, -1, -1, -1, 964, -1, -1, 772, -1, -1, -1, 4, 965, -1, -1, 689, 935, 967, -1, -1, -1, 784, -1, -1, 43, -1, 850, -1 },
			{ -1, 857, -1, -1, -1, -1, 82, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 857, 857, 857, 857, 857, 857, 857, 857, 857, 857, 857, 857, 857, 857, 857, 857, 857, 857, 857, 857, 857, 857, 857, 857 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 276, -1, -1, -1, 5, 5, 5, 5, 5, 5, 5, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 308, -1, -1, -1, -1, 308, 308, -1, 10, 10, 10, 10, 10, 10, 10, 10, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 394, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 369, -1, 663, 663, 663, 663, 663, 182, 182, 182, 182, 369, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 102, -1, -1, 404, 337, -1, 338, -1, 339, 694, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 695, -1, -1, 217, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 217, 102, 102, 102, 102, 102, 102, 102, 102, 102 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 402, -1, -1, -1, 163, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 416, -1, -1, -1, 416, 417, 418, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 530, -1, -1, -1, -1 },
			{ -1, 858, -1, -1, -1, -1, 107, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 858, 858, 858, 858, 858, 858, 858, 858, 858, 858, 858, 858, 858, 858, 858, 858, 858, 858, 858, 858, 858, 858, 858, 858 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 723, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 567, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 135, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 300, -1, -1, 296, -1, -1, -1, -1, 296, 296, -1, 8, 846, 876, 876, 876, 876, 46, 46, 46, -1, -1, -1, 179, -1, -1, 937, -1, -1, -1, 4, 202, -1, -1, 936, 670, 220, -1, -1, -1, 938, -1, -1, 43, -1, 850, -1 },
			{ -1, 282, -1, -1, 284, -1, -1, -1, -1, -1, -1, -1, 741, 741, 741, 741, 741, 741, 741, 741, 741, 221, -1, -1, -1, -1, -1, 286, -1, -1, 196, -1, -1, -1, -1, 288, -1, -1, -1, -1, -1, 290, 682, -1, -1, 244, -1, 683 },
			{ -1, -1, -1, -1, 308, -1, -1, -1, -1, 308, 308, -1, 10, 10, 10, 10, 10, 10, 10, 10, 10, -1, -1, -1, -1, -1, -1, -1, 395, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 764, -1, -1, -1, 764, -1, 764, -1, 222, 222, 862, 885, 885, 885, 228, 228, 228, -1, -1, -1, -1, -1, -1, 764, -1, -1, 764, -1, -1, -1, -1, -1, 764, -1, -1, -1, 764, 764, 764, -1, -1, -1, -1, -1 },
			{ -1, 659, -1, -1, -1, -1, 140, -1, -1, -1, -1, 419, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 419, 659, 659, 659, 659, 659, 659, 659, 659, 659, 659, 659, 659, 659, 659, 659, 659, 659, 659, 659, 659, 659, 659, 659, 659 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 706, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 531, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 661, 661, 661, 661, 661, 661, 661, 661, 661, 207, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 724, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 626, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 794, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 9, 9, 9, 47, 69, 69, 69, 69, 69, -1, -1, -1, 667, -1, -1, 801, -1, -1, -1, -1, 672, -1, -1, 751, 746, 679, -1, -1, -1, 808, -1, -1, -1, -1, -1, -1 },
			{ -1, 822, -1, -1, -1, -1, 82, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 12, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822 },
			{ -1, -1, -1, -1, 308, -1, -1, -1, -1, 308, 308, -1, 10, 10, 10, 10, 10, 10, 10, 10, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 66 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 369, -1, 20, 20, 20, 20, 20, 20, 20, 20, 20, 369, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 403, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 654, 654, 654, 654, 654, 654, 654, 654, 654, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 548, -1, -1, -1, 548, 549, 550, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 73 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 661, 661, 661, 661, 661, 213, 107, 107, 107, 207, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 568, -1, -1, -1, 187, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 628, -1, 628, -1, -1, 146, 146, 146, 146, 146, 146, 146, 146, 146, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 81, -1, -1, 97, -1, -1, -1, -1, 112, 125, 136, 301, 301, 301, 301, 301, 301, 302, 302, 302, 169, -1, -1, 179, -1, -1, 189, -1, -1, 196, 4, 202, -1, -1, 208, 214, 220, -1, -1, 226, 232, 238, -1, 43, 244, 850, 683 },
			{ -1, 822, -1, -1, 278, -1, 82, -1, -1, 278, 278, 280, 6, 6, 6, 630, 731, 731, 731, 731, 731, -1, -1, 280, 822, 822, 822, 822, 822, 822, 822, 676, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822 },
			{ -1, -1, -1, -1, 308, -1, -1, -1, -1, 308, 308, -1, 10, 10, 10, 10, 10, 10, 10, 10, 10, -1, -1, -1, -1, -1, -1, -1, 66, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 369, -1, 20, 20, 20, 20, 20, 53, 53, 53, 53, 369, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 860, -1, -1, -1, -1, 140, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 860, 860, 860, 860, 860, 860, 860, 860, 860, 860, 860, 860, 860, 860, 860, 860, 860, 860, 860, 860, 860, 860, 860, 860 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 775, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 654, 654, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 548, -1, -1, -1, 548, 549, 550, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 73, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 656, -1, -1, 538, 868, -1, 539, -1, 539, 464, -1, 666, 666, 666, 666, 666, 666, 666, 666, 666, 464, -1, -1, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 569, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 178, 178, 178, 178, 178, 178, 178, 178, 178, 188, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 81, -1, -1, 249, -1, -1, -1, -1, 112, 253, 136, 301, 301, 301, 301, 301, 301, 302, 302, 302, 261, -1, -1, 265, -1, -1, 189, -1, -1, 196, 4, 202, -1, -1, 208, 214, 220, 269, -1, 226, 232, 238, -1, 43, 244, 850, 683 },
			{ -1, 822, -1, -1, 278, -1, 82, -1, -1, 278, 278, 280, 6, 6, 6, 630, 731, 731, 731, 731, 731, -1, -1, 280, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822 },
			{ -1, -1, -1, -1, 308, -1, -1, -1, -1, 308, 308, -1, 10, 10, 10, 10, 10, 10, 10, 10, 10, -1, -1, -1, -1, -1, 396, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 369, -1, 53, 53, 53, 53, 53, 53, 53, 53, 53, 369, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 665, 665, 665, 665, 665, 665, 665, 665, 665, 223, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 654, 36, 36, 36, 36, 36, 36, 36, 36, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 548, -1, -1, -1, 548, 549, 550, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 532, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 656, -1, -1, 538, 868, -1, 539, -1, 539, 464, -1, 671, -1, -1, -1, -1, -1, -1, -1, -1, 464, -1, -1, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 570, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 178, 178, 178, 178, 178, 96, 79, 79, 79, 188, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 68, 68, 68, 68, 68, 68, 638, 638, 638, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 822, -1, -1, -1, -1, 82, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 7, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822 },
			{ -1, -1, -1, -1, 308, -1, -1, -1, -1, 308, 308, -1, 10, 10, 10, 10, 10, 10, 10, 10, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 400, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 764, -1, -1, -1, 764, -1, 764, -1, 768, 768, 782, 792, 792, 792, 792, 792, 792, -1, -1, -1, -1, -1, -1, 764, -1, -1, 764, -1, -1, -1, -1, -1, 764, -1, -1, -1, 764, 764, 764, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 665, 665, 665, 665, 665, 229, 140, 140, 140, 223, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 36, 36, 36, 36, 36, 36, 36, 36, 36, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 548, -1, -1, -1, 548, 549, 550, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 718, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 656, -1, -1, 538, 868, -1, 539, -1, 539, 464, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 464, -1, -1, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 725, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 96, 96, 96, 96, 96, 96, 79, 79, 79, 188, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 303, -1, -1, -1, -1, 304, -1, -1, -1, -1 },
			{ -1, 822, -1, -1, -1, -1, 82, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 239, 822, 822, 822, 822, 822, 822, 984, 13, 822, 822 },
			{ -1, -1, -1, -1, 308, -1, -1, -1, -1, 308, 308, -1, 10, 10, 10, 10, 10, 10, 10, 10, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 658, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 640, 640, 640, 640, 640, 640, 640, 640, 640, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 662, -1, -1, 420, 866, -1, 421, -1, 421, 422, -1, 251, 251, 251, 251, 251, 251, 251, 251, 251, -1, -1, -1, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 36, 36, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 548, -1, -1, -1, 548, 549, 550, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 660, -1, -1, -1, -1, -1 },
			{ -1, 656, -1, -1, 540, 868, -1, 722, -1, 722, -1, -1, 671, 671, 671, 671, 671, 671, 671, 671, 671, -1, -1, -1, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 96, 96, 96, 96, 96, 96, 79, 79, 79, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 305, -1, -1, -1, 306, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 822, -1, -1, -1, -1, 82, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 845, 822, 822, 822, 822, 822 },
			{ -1, -1, -1, -1, 764, -1, -1, -1, 764, -1, 764, -1, 44, 234, 234, 234, 234, 234, 234, 234, 234, -1, -1, -1, -1, -1, -1, 764, -1, -1, 764, -1, -1, -1, -1, -1, 67, -1, -1, -1, 67, 85, 101, -1, -1, -1, -1, -1 },
			{ -1, 662, -1, -1, 420, 866, -1, 421, -1, 421, 422, -1, 251, 23, 23, 23, 23, 23, 23, 23, 23, -1, -1, -1, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 593, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 506, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 720, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 656, -1, -1, 540, 868, -1, 722, -1, 722, -1, -1, 671, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 307, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 822, -1, -1, -1, -1, 82, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 822, 822, 822, 822, 822, 822, 822, 835, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 940, 822, 822, 822, 822 },
			{ -1, -1, -1, -1, 764, -1, -1, -1, 764, -1, 764, -1, 234, 234, 234, 234, 234, 234, 234, 234, 234, -1, -1, -1, -1, -1, -1, 764, -1, -1, 764, -1, -1, -1, -1, -1, 67, -1, -1, -1, 67, 85, 101, -1, -1, -1, -1, -1 },
			{ -1, 662, -1, -1, 420, 866, -1, 421, -1, 421, 422, -1, 23, 23, 23, 23, 23, 23, 23, 23, 23, -1, -1, -1, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 506, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 529, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 656, -1, -1, 540, 868, -1, 722, -1, 722, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 309, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 310, -1, -1, -1, -1 },
			{ -1, 822, -1, -1, -1, -1, 82, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 822, 822, 822, 822, 875, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822 },
			{ -1, -1, -1, -1, 764, -1, -1, -1, 764, -1, 764, -1, 234, 234, 116, 652, 652, 652, 652, 652, 652, -1, -1, -1, -1, -1, -1, 764, -1, -1, 764, -1, -1, -1, -1, -1, 67, -1, -1, -1, 67, 85, 101, -1, -1, -1, -1, -1 },
			{ -1, 662, -1, -1, 420, 866, -1, 421, -1, 421, 422, -1, 211, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 506, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 530, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 213, 213, 213, 213, 213, 213, 107, 107, 107, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 311, -1, -1, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, 313, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 822, -1, -1, -1, -1, 82, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 822, 822, 822, 946, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822 },
			{ -1, 662, -1, -1, 420, 866, -1, 421, -1, 421, 422, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 506, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 531, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 107, 107, 107, 107, 107, 107, 107, 107, 107, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 314, -1, -1, -1, -1, -1, -1, -1, -1, -1, 315, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 245, -1, -1, 278, -1, 82, -1, -1, 329, 278, 280, 6, 6, 6, 630, 731, 731, 731, 731, 731, -1, -1, 280, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 968, 245, 245, 245, 245, 245, 245, 245 },
			{ -1, 129, -1, -1, -1, -1, 140, -1, -1, -1, 456, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 129, 129, 129, 129, 129, 129, 129, 129, 129, 129, 129, 70, 129, 129, 129, 129, 129, 129, 129, 129, 129, 129, 129, 129 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 506, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 73 },
			{ -1, 584, -1, -1, -1, -1, 107, -1, -1, -1, -1, 537, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 537, 584, 584, 584, 584, 584, 584, 584, 584, 584, 584, 584, 584, 584, 584, 584, 584, 584, 584, 584, 584, 584, 584, 584, 584 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 250, 250, 250, 250, 250, 250, 82, 82, 82, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 74, -1, -1, 460, 461, -1, 462, -1, 462, 463, -1, 680, 680, 680, 680, 680, 680, 680, 680, 680, 463, -1, -1, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 229, 229, 229, 229, 229, 229, 140, 140, 140, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 506, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 73, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 737, -1, -1, -1, -1, 107, -1, -1, -1, -1, 585, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 585, 737, 737, 737, 737, 737, 737, 737, 737, 737, 737, 737, 737, 737, 737, 737, 737, 737, 737, 737, 737, 737, 737, 737, 737 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 314, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 282, -1, -1, 284, -1, -1, -1, -1, -1, -1, -1, 233, 233, 233, 233, 233, 233, 233, 233, 233, -1, -1, -1, -1, -1, -1, 286, -1, -1, 196, -1, -1, -1, -1, 288, -1, -1, -1, -1, -1, 290, 682, -1, -1, 244, -1, 683 },
			{ -1, 74, -1, -1, 460, 461, -1, 462, -1, 462, 463, -1, 216, 216, 216, 216, 216, 216, 216, 216, 216, 463, -1, -1, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 140, 140, 140, 140, 140, 140, 140, 140, 140, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 506, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 532, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 243, 243, 243, 243, 243, 243, 243, 243, 243, 256, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 317, -1, -1, -1, 318, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 314, 319, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 764, -1, -1, -1, 764, 478, 128, -1, 139, 139, 150, 161, 161, 161, 161, 161, 161, 369, -1, -1, -1, -1, -1, 764, -1, -1, 764, -1, -1, -1, -1, -1, 67, -1, -1, -1, 67, 85, 101, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 229, 229, 229, 229, 229, 229, 267, 267, 267, 223, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 506, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 243, 243, 243, 243, 243, 213, 107, 107, 107, 256, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 321, -1, -1, -1, -1 },
			{ -1, 245, -1, -1, -1, -1, 82, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 17, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 267, 267, 267, 267, 267, 267, 267, 267, 267, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 506, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 718, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 213, 213, 213, 213, 213, 213, 107, 107, 107, 256, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 322, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 506, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 675, -1, -1, -1, -1, -1 },
			{ -1, 248, -1, -1, -1, -1, -1, -1, -1, -1, -1, 537, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 537, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248 },
			{ -1, 81, -1, -1, 249, -1, -1, -1, -1, 296, 296, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 265, -1, -1, 189, -1, -1, 196, 4, 202, -1, -1, 208, 670, 220, 269, -1, -1, 690, 682, -1, 43, 244, 850, 683 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 82, 82, 82, 82, 82, 82, 82, 82, 82, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 662, -1, -1, 420, 866, -1, 421, -1, 421, 422, -1, 481, 481, 481, 481, 481, 481, 481, 481, 481, -1, -1, -1, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662 },
			{ -1, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 300, -1, -1, 296, -1, -1, -1, -1, 296, 296, -1, 86, 855, 881, 881, 881, 881, 851, 851, 851, -1, -1, -1, 179, -1, -1, 937, -1, -1, -1, 4, 202, -1, -1, 936, 670, 220, -1, -1, -1, 938, -1, -1, 43, -1, 850, -1 },
			{ -1, 370, -1, -1, 278, -1, 82, -1, -1, 278, 278, 280, 6, 6, 6, 630, 731, 731, 731, 731, 731, -1, -1, 280, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370 },
			{ -1, 662, -1, -1, 505, 866, -1, 421, -1, 421, 422, -1, 481, 481, 481, 481, 481, 481, 481, 481, 481, -1, -1, -1, 281, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 281, 662, 662, 662, 662, 662, 662, 662, 662, 662 },
			{ -1, 81, -1, -1, 97, -1, -1, -1, -1, 112, 125, 324, 301, 301, 301, 301, 301, 301, 302, 302, 302, 169, -1, -1, 179, -1, -1, 189, -1, -1, 196, 4, 202, -1, -1, 208, 214, 220, -1, -1, 226, 232, 238, -1, 43, 244, 850, 683 },
			{ -1, 776, -1, -1, 412, -1, 82, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 370, 370, 370, 787, 370, 370, 888, 370, 370, 370, 370, 797, 370, 370, 370, 370, 370, 804, 810, 370, 370, 816, 370, 895 },
			{ -1, 662, -1, -1, 505, 866, -1, 421, -1, 421, 422, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 281, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 281, 662, 662, 662, 662, 662, 662, 662, 662, 662 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 268, 268, 268, 268, 268, 268, 268, 268, 268, 256, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 646, 646, 646, 646, 646, 646, 734, 734, 734, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 370, -1, -1, -1, -1, 82, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 22 },
			{ -1, 524, -1, -1, -1, -1, 140, -1, -1, -1, -1, 419, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 419, 524, 524, 524, 524, 524, 524, 524, 524, 524, 524, 524, 524, 524, 524, 524, 524, 524, 524, 524, 524, 524, 524, 524, 524 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 325, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 11, -1, -1, 303, -1, -1, -1, -1, 304, -1, -1, -1, -1 },
			{ -1, 266, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 266, 266, 266, 266, 266, 266, 266, 266, 266, 266, 266, 266, 266, 266, 266, 266, 266, 266, 266, 266, 266, 266, 266, 266 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 30, 30, 30, 30, 30, 30, 30, 30, 30, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 325, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 82, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 742, -1, -1, -1, -1, 140, -1, -1, -1, -1, 525, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 525, 742, 742, 742, 742, 742, 742, 742, 742, 742, 742, 742, 742, 742, 742, 742, 742, 742, 742, 742, 742, 742, 742, 742, 742 },
			{ -1, 81, -1, -1, 97, -1, -1, -1, -1, 112, 326, 324, 302, 302, 302, 302, 302, 302, 302, 302, 302, -1, -1, -1, 179, -1, -1, 189, -1, -1, 196, 4, 202, -1, -1, 208, 214, 220, -1, -1, 226, 232, 238, -1, 43, 244, 850, 683 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 277, 277, 277, 277, 277, 277, 277, 277, 277, 287, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 282, -1, -1, 284, -1, -1, -1, -1, 327, -1, -1, 302, 302, 302, 302, 302, 302, 302, 302, 302, -1, -1, -1, -1, -1, -1, 286, -1, -1, 196, -1, -1, -1, -1, 288, -1, -1, -1, -1, -1, 290, 682, -1, -1, 244, -1, 683 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 277, 277, 277, 277, 277, 229, 140, 140, 140, 287, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 5, 5, 5, 5, 5, 5, 5, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 229, 229, 229, 229, 229, 229, 140, 140, 140, 287, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 278, -1, -1, -1, -1, 278, 278, -1, 6, 6, 6, 630, 731, 731, 731, 731, 731, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 662, -1, -1, 526, 866, -1, 527, -1, 527, -1, -1, 279, 279, 279, 279, 279, 279, 279, 279, 279, -1, -1, -1, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662 },
			{ -1, 328, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 328, 328, 328, 328, 328, 328, 328, 328, 328, 328, 328, 328, 328, 328, 328, 328, 328, 328, 328, 328, 328, 328, 328, 328 },
			{ -1, 271, -1, -1, -1, -1, 140, -1, -1, -1, 456, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 271, 271, 271, 271, 271, 271, 271, 271, 271, 271, 271, 644, 271, 271, 271, 271, 271, 271, 271, 271, 271, 271, 271, 271 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 294, -1, -1, 295, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 283, -1, -1, -1, -1, -1, -1, -1, -1, -1, 419, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 419, 283, 283, 283, 283, 283, 283, 283, 283, 283, 283, 283, 283, 283, 283, 283, 283, 283, 283, 283, 283, 283, 283, 283, 283 },
			{ -1, 282, -1, -1, 284, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 286, -1, -1, 196, -1, -1, -1, -1, 288, -1, -1, -1, -1, -1, 290, 682, -1, -1, 244, -1, 683 },
			{ -1, -1, -1, -1, -1, -1, 140, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 305, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, 313, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 293, 293, 293, 293, 293, 293, 293, 293, 293, 287, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 317, -1, -1, -1, 330, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 319, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 84, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 332, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 14, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 300, -1, -1, 296, -1, -1, -1, -1, 296, 296, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 179, -1, -1, 937, -1, -1, -1, 4, 202, -1, -1, 936, 670, 220, -1, -1, -1, 938, -1, -1, 43, -1, 850, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 333, 334, -1, 299, 299, 299, 299, 299, 299, 299, 299, 299, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 333, 334, -1, 299, 299, 299, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 333, 334, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 292, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 282, -1, -1, 284, -1, -1, -1, -1, 327, -1, -1, 15, 15, 15, 15, 15, 15, 15, 15, 15, -1, -1, -1, -1, -1, -1, 286, -1, -1, 196, -1, -1, -1, -1, 288, -1, -1, -1, -1, -1, 290, 682, -1, -1, 244, -1, 683 },
			{ -1, 282, -1, -1, 284, -1, -1, -1, -1, 327, -1, -1, 16, 16, 16, 16, 16, 16, 16, 16, 16, -1, -1, -1, -1, -1, -1, 286, -1, -1, 196, -1, -1, -1, -1, 288, -1, -1, -1, -1, -1, 290, 682, -1, -1, 244, -1, 683 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 100, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 115, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 50 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 127, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 355, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 651, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 138, -1, 149, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 160, -1, -1, -1, -1, -1, -1, 66 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 71, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 89, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 300, -1, -1, 296, -1, -1, -1, -1, 356, 326, 324, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 179, -1, -1, 937, -1, -1, -1, 4, 202, -1, -1, 936, 670, 220, -1, -1, -1, 938, -1, -1, 43, -1, 850, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 127, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 171, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 104, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 119, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 181, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 14, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 300, -1, -1, 296, -1, -1, -1, -1, 356, 326, 324, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 179, -1, -1, 937, -1, -1, -1, 4, 202, -1, -1, 936, 670, 220, -1, -1, -1, 938, -1, 130, 43, -1, 850, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 141, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 152, 357, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 355, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 794, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 667, -1, -1, 801, -1, -1, -1, -1, 672, -1, -1, 751, 746, 679, -1, -1, -1, 808, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 300, -1, -1, 296, -1, -1, -1, -1, 296, 296, -1, 358, 359, 360, 360, 360, 360, 360, 360, 360, -1, -1, -1, 179, -1, -1, 937, -1, -1, -1, 4, 202, -1, -1, 936, 670, 220, -1, -1, -1, 938, -1, -1, 43, -1, 850, -1 },
			{ -1, 361, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 362, 363, 364, 364, 364, 364, 364, 364, 364, -1, -1, -1, 365, -1, -1, 698, -1, -1, -1, -1, 366, -1, -1, 367, 368, 696, -1, -1, -1, 754, -1, -1, -1, -1, -1, -1 },
			{ -1, 266, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 266, 266, 266, 266, 266, 266, 266, 266, 266, 266, 266, 266, 266, 266, 266, 266, 266, 266, 266, 266, 266, 266, 266, 266 },
			{ -1, -1, -1, -1, 278, -1, -1, -1, -1, 278, 278, -1, 191, 198, 198, 204, 731, 731, 731, 731, 731, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 119, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 130, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 374, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 18, 18, 18, 51, 72, 72, 72, 72, 72, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 865, 865, 865, 865, 865, 865, 865, 865, 865, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 921, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 102, -1, -1, 336, 337, -1, 338, -1, 338, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102 },
			{ -1, 151, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 151, 151, 151, 151, 151, 151, 151, 151, 151, 151, 151, 151, 151, 151, 151, 151, 151, 151, 151, 151, 151, 151, 151, 151 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 162, 162, 173, 665, 665, 665, 665, 665, 665, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 664, 664, 744, 769, 769, 769, 769, 769, 769, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 183, 183, 183, 183, 183, 183, 192, 199, 199, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 668, 668, 668, 668, 668, 668, 205, 211, 211, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 375, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 87, 87, 87, 87, 87, 87, 87, 87, 87, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 103, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 103, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 103, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 375, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 375, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 375, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 375, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 375, -1, 375, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 375, -1, -1, -1, -1, -1, -1, 375 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 375, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 376, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 50, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 300, -1, -1, 296, -1, -1, -1, -1, 296, 296, -1, 399, 697, 691, 691, 691, 691, 691, 691, 691, -1, -1, -1, 179, -1, -1, 937, -1, -1, -1, 4, 202, -1, -1, 936, 670, 220, -1, -1, -1, 938, -1, -1, 43, -1, 850, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 50, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 334, 693, -1, 360, 360, 360, 360, 360, 360, 360, 360, 360, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 334, 693, -1, 752, 752, 752, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 334, 693, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 708, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 405, -1, -1, 364, 364, 364, 364, 364, 364, 364, 364, 364, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 405, -1, -1, 364, 364, 364, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 405, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 709, -1, -1, -1, -1, 406, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 710, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 407, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 408, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 409, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 21, 21, 21, 21, 21, 21, 54, 54, 54, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 776, -1, -1, 412, -1, -1, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 370, 370, 370, 787, 370, 370, 888, 370, 370, 370, 370, 797, 370, 370, 370, 370, 370, 804, 810, 370, 370, 816, 370, 895 },
			{ -1, 370, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 45 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 413, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 760, -1, -1, -1, -1, -1, 414, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 887, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 887, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 375, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 423, -1, -1, -1, -1, -1, -1, -1, -1, 294, -1, -1, 295, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 377, -1, -1, 378, -1, -1, -1, -1, 424, 424, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 700, -1, -1, 388, -1, -1, 196, 19, 701, -1, -1, 389, 703, 390, -1, -1, -1, 391, 682, -1, 52, 244, 852, 683 },
			{ -1, 425, -1, -1, 424, -1, -1, -1, -1, 424, 424, -1, 426, 427, 428, 428, 428, 428, 428, 428, 428, -1, -1, -1, 429, -1, -1, 713, -1, -1, -1, 19, 430, -1, -1, 712, 431, 757, -1, -1, -1, 762, -1, -1, 52, -1, 852, -1 },
			{ -1, 814, -1, -1, 424, -1, -1, -1, -1, 424, 424, -1, 774, 786, 786, 432, -1, -1, -1, -1, -1, -1, -1, -1, 700, -1, -1, 778, -1, -1, -1, 19, 701, -1, -1, 761, 703, 390, -1, -1, -1, 789, -1, -1, 52, -1, 852, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 433, 434, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 282, -1, -1, 284, -1, -1, -1, -1, -1, -1, -1, 24, 55, 55, 55, 55, 55, 55, 55, 55, -1, -1, -1, -1, -1, -1, 286, -1, -1, 196, -1, -1, -1, -1, 288, -1, -1, -1, -1, -1, 290, 682, -1, -1, 244, -1, 683 },
			{ -1, 282, -1, -1, 284, -1, -1, -1, -1, -1, -1, -1, 55, 55, 55, 75, 75, 75, 75, 75, 75, -1, -1, -1, -1, -1, -1, 286, -1, -1, 196, -1, -1, -1, -1, 288, -1, -1, -1, -1, -1, 290, 682, -1, -1, 244, -1, 683 },
			{ -1, 282, -1, -1, 284, -1, -1, -1, -1, -1, -1, -1, 75, 75, 75, 75, 75, 75, 75, 75, 75, -1, -1, -1, -1, -1, -1, 286, -1, -1, 196, -1, -1, -1, -1, 288, -1, -1, -1, -1, -1, 290, 682, -1, -1, 244, -1, 683 },
			{ -1, 282, -1, -1, 284, -1, -1, -1, -1, -1, -1, -1, 75, 75, 75, 75, 75, 75, 93, 108, 108, -1, -1, -1, -1, -1, -1, 286, -1, -1, 196, -1, -1, -1, -1, 288, -1, -1, -1, -1, -1, 290, 682, -1, -1, 244, -1, 683 },
			{ -1, 282, -1, -1, 284, -1, -1, -1, -1, -1, -1, -1, 108, 108, 108, 108, 108, 108, 108, 108, 108, -1, -1, -1, -1, -1, -1, 286, -1, -1, 196, -1, -1, -1, -1, 288, -1, -1, -1, -1, -1, 290, 682, -1, -1, 244, -1, 683 },
			{ -1, 282, -1, -1, 284, -1, -1, -1, -1, -1, -1, -1, 108, 63, 63, 63, 63, 63, 63, 63, 63, -1, -1, -1, -1, -1, -1, 286, -1, -1, 196, -1, -1, -1, -1, 288, -1, -1, -1, -1, -1, 290, 682, -1, -1, 244, -1, 683 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 305, -1, -1, -1, 437, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 440, -1, -1, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, 313, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 442, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 317, -1, -1, -1, 443, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 319, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 444, 445, 445, 445, 445, 446, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 322, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 66, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 451, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 758, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 66, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 452, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 50, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 334, 334, -1, 691, 691, 691, 691, 691, 691, 691, 691, 691, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 798, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 711, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 453, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 777, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 102, -1, -1, 404, 337, -1, 338, -1, 338, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 217, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 217, 102, 102, 102, 102, 102, 102, 102, 102, 102 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 90, 90, 90, 105, 120, 120, 120, 120, 120, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 458, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 458, -1, 458, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 458, -1, -1, -1, -1, -1, -1, 458 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 458, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 459, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 370, -1, -1, 278, -1, -1, -1, -1, 278, 278, 280, 6, 6, 6, 630, 731, 731, 731, 731, 731, -1, -1, 280, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370 },
			{ -1, 466, -1, -1, 412, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 788, -1, -1, 714, -1, -1, -1, -1, 467, -1, -1, -1, -1, -1, 468, 469, -1, -1, 805, -1, 811 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 479, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 480, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 163 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 131, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 131, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 131, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 482, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 482, 482, 482, 482, 482, 482, 482, 482, 482, 482, 482, 482, 482, 482, 482, 482, 482, 482, 482, 482, 482, 482, 482, 482 },
			{ -1, 662, -1, -1, 420, 866, -1, 421, -1, 421, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 273, 273, 275, 277, 277, 277, 277, 277, 277, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 279, 279, 279, 279, 279, 279, 279, 279, 279, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 91, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 814, -1, -1, 424, -1, -1, -1, -1, 424, 424, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 700, -1, -1, 778, -1, -1, -1, 19, 701, -1, -1, 761, 703, 390, -1, -1, -1, 789, -1, -1, 52, -1, 852, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 483, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 484, -1, -1, 25, 632, 632, 632, 632, 632, 632, 632, 632, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 484, -1, -1, 632, 632, 632, 56, 56, 56, 56, 56, 56, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 484, -1, -1, 56, 56, 56, 56, 56, 56, 56, 56, 56, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 485, -1, -1, -1, -1, 486, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 488, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 489, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 491, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 495, 495, 495, 495, 495, 495, 496, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 498, 498, 498, 498, 498, 498, 498, 498, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 498, 498, 498, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 106, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 121, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 132, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 655, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 143, -1, 154, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 165, -1, -1, -1, -1, -1, -1, 73 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 132, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 175, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 119, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 185, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 27, 27, 27, 27, 27, 27, 27, 27, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 27, 27, 27, 27, 27, 27, 27, 27, 27, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 27, 27, 27, 27, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 66, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 50, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 50, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 503, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 504, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 88, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 23, 23, 23, 23, 23, 23, 23, 23, 23, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 506, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 699, -1, -1, -1, -1, -1 },
			{ -1, 74, -1, -1, 460, 461, -1, 462, -1, 462, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74 },
			{ -1, 122, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 122, 122, 122, 122, 122, 122, 122, 122, 122, 122, 122, 122, 122, 122, 122, 122, 122, 122, 122, 122, 122, 122, 122, 122 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 133, 133, 144, 661, 661, 661, 661, 661, 661, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 155, 155, 155, 155, 155, 155, 166, 176, 176, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 186, 186, 186, 186, 186, 186, 195, 201, 201, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 370, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 370, 370, 370, 370, 370, 370, 370, 28, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 716, -1, -1, 507, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 510, -1, -1, -1, -1, -1, 511, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 512, -1, -1, -1, 513, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 514, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 515, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 516, -1, -1, -1, -1 },
			{ -1, 370, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 58 },
			{ -1, 370, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 853, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370 },
			{ -1, 370, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 633, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370 },
			{ -1, 370, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 848, 370, 370, 370, 370, 370 },
			{ -1, 370, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 370, 370, 879, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370 },
			{ -1, 370, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 941, 370, 370, 370, 370 },
			{ -1, 370, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 370, 370, 370, 370, 878, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370 },
			{ -1, 370, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 370, 370, 370, 947, 520, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 29, 29, 29, 29, 29, 29, 29, 29, 29, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 500, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 523, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 283, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 283, 283, 283, 283, 283, 283, 283, 283, 283, 283, 283, 283, 283, 283, 283, 283, 283, 283, 283, 283, 283, 283, 283, 283 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 194, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 142, 142, 142, 153, 654, 654, 654, 654, 654, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 200, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 206, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 212, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 674, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 218, -1, 224, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 230, -1, -1, -1, -1, -1, -1, 236 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 212, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 242, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 247, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 109, 109, 109, 109, 109, 109, 109, 109, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 109, 109, 109, 109, 109, 109, 109, 109, 109, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 109, 109, 109, 109, 109, 109, 109, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 185, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 719, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 501, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 66 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 66, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 415, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 533, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 502, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 662, -1, -1, 505, 866, -1, 421, -1, 421, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 281, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 281, 662, 662, 662, 662, 662, 662, 662, 662, 662 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 534, 535, 535, 536, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 77, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 95 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 542, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 110, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 123, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 134, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 145, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 77, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 156, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 167, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 177, 543, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 542, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 370, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 58, 370, 370, 370, 370, 370, 370, 370 },
			{ -1, 370, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 370, 370, 370, 370, 370, 370, 370, 370, 370, 58, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370 },
			{ -1, 370, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 22, 370, 370, 370, 370, 370 },
			{ -1, 370, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 33, 370, 370 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 546, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 524, -1, -1, -1, -1, -1, -1, -1, -1, -1, 419, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 419, 524, 524, 524, 524, 524, 524, 524, 524, 524, 524, 524, 524, 524, 524, 524, 524, 524, 524, 524, 524, 524, 524, 524, 524 },
			{ -1, 717, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 717, 717, 717, 717, 717, 717, 717, 717, 717, 717, 717, 717, 717, 717, 717, 717, 717, 717, 717, 717, 717, 717, 717, 717 },
			{ -1, 662, -1, -1, 526, 866, -1, 527, -1, 527, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 289, 289, 291, 293, 293, 293, 293, 293, 293, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 164, 174, 174, 184, 654, 654, 654, 654, 654, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 73, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 555, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 556, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 73, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 34, 34, 34, 34, 34, 34, 34, 34, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 34, 34, 34, 34, 34, 34, 34, 34, 34, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 34, 34, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 560, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 560, 560, 560, 560, 560, 560, 560, 560, 560, 560, 560, 560, 560, 560, 560, 560, 560, 560, 560, 560, 560, 560, 560, 560 },
			{ -1, 656, -1, -1, 538, 868, -1, 539, -1, 539, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 231, 231, 237, 243, 243, 243, 243, 243, 243, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 565, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 95, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 95, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 370, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 370, 370, 370, 370, 370, 370, 58, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370 },
			{ -1, 370, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 35 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 50, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 557, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 193, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 193, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 575, 575, 576, 577, 577, 577, 577, 577, 577, -1, -1, -1, -1, -1, -1, -1, -1, -1, 193, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 60, 60, 60, 60, 60, 60, 60, 60, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 60, 60, 60, 60, 60, 60, 60, 60, 60, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 60, 60, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 578, 578, 579, 580, 580, 580, 580, 580, 580, -1, -1, -1, -1, -1, -1, -1, -1, -1, 331, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 321, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 73, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 558, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 581, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 582, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 583, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 248, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248 },
			{ -1, 656, -1, -1, 561, 868, -1, 562, -1, 562, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 260, 260, 264, 268, 268, 268, 268, 268, 268, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 656, -1, -1, 563, 868, -1, 562, -1, 562, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656 },
			{ -1, 370, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 971 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 727, -1, -1, -1, -1, -1, 586, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 588, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 95, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 589, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 590, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 370, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 370, 370, 370, 370, 58, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370 },
			{ -1, 370, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 370, 370, 370, 58, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370 },
			{ -1, 573, -1, -1, -1, -1, -1, -1, -1, -1, -1, 525, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 525, 573, 573, 573, 573, 573, 573, 573, 573, 573, 573, 573, 573, 573, 573, 573, 573, 573, 573, 573, 573, 573, 573, 573, 573 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 592, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 577, 577, 577, 577, 577, 577, 577, 577, 577, 803, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 577, 577, 577, 577, 577, -1, -1, -1, -1, 803, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 803, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 595, 595, 595, 595, 595, 595, 580, 580, 580, 596, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 595, 595, 595, 595, 595, 809, -1, -1, -1, 596, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 809, 809, 809, 809, 809, 809, -1, -1, -1, 596, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 73, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 597, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 584, -1, -1, -1, -1, -1, -1, -1, -1, -1, 537, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 537, 584, 584, 584, 584, 584, 584, 584, 584, 584, 584, 584, 584, 584, 584, 584, 584, 584, 584, 584, 584, 584, 584, 584, 584 },
			{ -1, 726, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 726, 726, 726, 726, 726, 726, 726, 726, 726, 726, 726, 726, 726, 726, 726, 726, 726, 726, 726, 726, 726, 726, 726, 726 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 598, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 187 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 95, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 95, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 587, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 599, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 815, 815, 600, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 575, 575, 576, 577, 577, 577, 577, 577, 577, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 603, 603, 604, 577, 577, 577, 577, 577, 577, -1, -1, -1, -1, -1, -1, -1, -1, -1, 193, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 819, 819, 819, 819, 819, 819, 605, 605, 605, 596, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 823, 823, 823, 823, 823, 823, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 608, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 609, 609, 609, 609, 609, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 602, 602, 602, 602, 602, 602, 602, 602, 602, 610, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 610, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 611, 611, 611, 611, 611, 611, 611, 611, 611, 803, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 611, 611, 611, 611, 611, -1, -1, -1, -1, 803, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 614, 614, 614, 614, 614, 614, 615, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 606, -1, -1, -1, -1, -1, -1, -1, -1, -1, 585, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 585, 606, 606, 606, 606, 606, 606, 606, 606, 606, 606, 606, 606, 606, 606, 606, 606, 606, 606, 606, 606, 606, 606, 606, 606 },
			{ -1, 370, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 58, 370, 370, 370, 370, 370 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 616, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 869, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 37, 37, 37, 37, 37, 37, 61, 79, 79, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 617, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 96, 96, 96, 96, 96, 96, 61, 79, 79, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 37, 614, 614, 614, 614, 614, 615, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 95, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 619, 619, 619, 619, 619, 619, 602, 602, 602, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 729, 729, 729, 729, 729, 729, 729, 729, 729, 610, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 621, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 829, 829, 829, 829, 829, 829, 623, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 111, 111, 111, 111, 111, 111, 124, 79, 79, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 624, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 625, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 625, -1, -1, 627, -1, 627, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 146, 146, 146, 146, 146, 146, 146, 146, 146, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 38, 38, 635, 733, 733, 733, 733, 733, 733, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 157, 157, 168, 178, 178, 178, 178, 178, 178, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 64, -1, -1, -1, -1, 82, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 64, 64, 64, 64, 64, 137, 64, 64, 64, 64, 64, 64, 64, 64, 736, 64, 64, 64, 64, 766, 64, 64, 64, 64 },
			{ -1, -1, -1, -1, 764, -1, -1, -1, 764, -1, 764, -1, 44, 44, 116, 652, 652, 652, 652, 652, 652, -1, -1, -1, -1, -1, -1, 764, -1, -1, 764, -1, -1, -1, -1, -1, 67, -1, -1, -1, 67, 85, 101, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 48, 48, 48, 48, 48, 48, 48, 48, 48, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 528, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 370, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 370, 370, 370, 841, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 544, 370, 370, 370, 370, 370 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 59, 59, 59, 59, 59, 59, 59, 59, 59, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 733, 733, 733, 733, 733, 641, 80, 80, 80, 62, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 64, -1, -1, 278, -1, 82, -1, -1, 278, 278, -1, 6, 6, 6, 630, 731, 731, 731, 731, 731, -1, -1, -1, 64, 64, 64, 64, 64, 64, 64, 859, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64 },
			{ -1, -1, -1, -1, 308, -1, -1, -1, -1, 308, 308, -1, 10, 10, 10, 10, 10, 10, 10, 10, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 66, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 102, -1, -1, 336, 337, -1, 338, -1, 338, 341, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 341, -1, -1, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 73, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 240, 240, 240, 240, 240, 240, 240, 240, 240, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 80, 80, 80, 80, 80, 80, 80, 80, 80, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 282, -1, -1, 284, -1, -1, -1, -1, -1, -1, -1, 447, 448, 448, 448, 448, 448, 448, 448, 448, -1, -1, -1, -1, -1, -1, 286, -1, -1, 196, -1, -1, -1, -1, 288, -1, -1, -1, -1, -1, 290, 682, -1, -1, 244, -1, 683 },
			{ -1, 822, -1, -1, -1, -1, 82, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 822, 215, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822 },
			{ -1, 742, -1, -1, -1, -1, 140, -1, -1, -1, 49, 525, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 525, 742, 742, 742, 742, 742, 742, 742, 742, 742, 742, 742, 742, 742, 742, 742, 742, 742, 742, 742, 742, 742, 742, 742, 742 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 481, 481, 481, 481, 481, 481, 481, 481, 481, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 416, -1, -1, -1, 416, 417, 418, -1, -1, -1, -1, -1 },
			{ -1, 102, -1, -1, 336, 337, -1, 338, -1, 338, 695, -1, 653, 653, 653, 653, 653, 653, 653, 653, 653, 695, -1, -1, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 118, 118, 118, 118, 118, 118, 118, 118, 118, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 219, -1, -1, -1, -1, 107, -1, -1, -1, -1, 537, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 537, 219, 219, 219, 219, 219, 219, 219, 219, 219, 219, 219, 219, 219, 219, 219, 219, 219, 219, 219, 219, 219, 219, 219, 219 },
			{ -1, 81, -1, -1, 249, -1, -1, -1, -1, 112, 253, 136, 257, 257, 257, 257, 257, 272, 272, 272, 272, 261, -1, -1, 265, -1, -1, 189, -1, -1, 196, 4, 202, -1, -1, 208, 214, 220, 269, -1, 226, 232, 238, -1, 43, 244, 850, 683 },
			{ -1, 270, -1, -1, -1, -1, 82, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 270, 270, 270, 270, 270, 270, 270, 270, 270, 270, 270, 270, 270, 270, 270, 270, 270, 270, 270, 270, 270, 270, 270, 270 },
			{ -1, -1, -1, -1, 308, -1, -1, -1, -1, 308, 308, -1, 10, 10, 10, 10, 10, 10, 10, 10, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 755, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 369, -1, 182, 182, 182, 182, 182, 182, 182, 182, 182, 369, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 102, -1, -1, 404, 337, -1, 338, -1, 338, 695, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 695, -1, -1, 217, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 217, 102, 102, 102, 102, 102, 102, 102, 102, 102 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 548, -1, -1, -1, 548, 549, 550, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 721, -1, -1, -1, -1 },
			{ -1, 225, -1, -1, -1, -1, 107, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 225, 225, 225, 225, 225, 225, 225, 225, 225, 225, 225, 225, 225, 225, 225, 225, 225, 225, 225, 225, 225, 225, 225, 225 },
			{ -1, 282, -1, -1, 284, -1, -1, -1, -1, -1, -1, -1, 741, 741, 741, 741, 741, 227, 233, 233, 233, 221, -1, -1, -1, -1, -1, 286, -1, -1, 196, -1, -1, -1, -1, 288, -1, -1, -1, -1, -1, 290, 682, -1, -1, 244, -1, 683 },
			{ -1, -1, -1, -1, 308, -1, -1, -1, -1, 308, 308, -1, 10, 10, 10, 10, 10, 10, 10, 10, 10, -1, -1, -1, -1, -1, -1, -1, 454, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 263, -1, -1, -1, -1, 140, -1, -1, -1, -1, 419, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 419, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263, 263 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 559, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 213, 213, 213, 213, 213, 213, 107, 107, 107, 207, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 271, -1, -1, -1, -1, 140, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 271, 271, 271, 271, 271, 271, 271, 271, 271, 271, 271, 271, 271, 271, 271, 271, 271, 271, 271, 271, 271, 271, 271, 271 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 369, -1, 640, 640, 640, 640, 640, 640, 640, 640, 640, 369, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 235, 235, 235, 235, 235, 235, 235, 235, 235, 223, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 229, 229, 229, 229, 229, 229, 140, 140, 140, 223, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 656, -1, -1, 561, 868, -1, 562, -1, 562, 464, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 464, -1, -1, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 347, -1, -1, -1, -1, 348, -1, -1, -1, -1 },
			{ -1, 662, -1, -1, 420, 866, -1, 421, -1, 421, 422, -1, 211, 211, 211, 211, 211, 211, 211, 211, 211, -1, -1, -1, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662 },
			{ -1, 662, -1, -1, 420, 866, -1, 421, -1, 421, 422, -1, 255, 23, 23, 23, 23, 23, 23, 23, 23, -1, -1, -1, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 315, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 350, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 351, -1, -1, -1, -1 },
			{ -1, 662, -1, -1, 420, 866, -1, 421, -1, 421, 422, -1, 259, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 506, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 721, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 506, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 559, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 245, -1, -1, 278, -1, 82, -1, -1, 278, 278, 280, 6, 6, 6, 630, 731, 731, 731, 731, 731, -1, -1, 280, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245 },
			{ -1, 606, -1, -1, -1, -1, 107, -1, -1, -1, -1, 585, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 585, 606, 606, 606, 606, 606, 606, 606, 606, 606, 606, 606, 606, 606, 606, 606, 606, 606, 606, 606, 606, 606, 606, 606, 606 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 692, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 74, -1, -1, 460, 461, -1, 462, -1, 462, 463, -1, 677, 677, 677, 677, 677, 677, 677, 677, 677, 463, -1, -1, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 331, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 321, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 323, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 370, -1, -1, -1, -1, 82, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 370, 370, 370, 370, 370, 370, 371, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370 },
			{ -1, 685, -1, -1, -1, -1, -1, -1, -1, -1, -1, 585, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 585, 685, 685, 685, 685, 685, 685, 685, 685, 685, 685, 685, 685, 685, 685, 685, 685, 685, 685, 685, 685, 685, 685, 685, 685 },
			{ -1, 370, -1, -1, 278, -1, 82, -1, -1, 329, 278, 280, 6, 6, 6, 630, 731, 731, 731, 731, 731, -1, -1, 280, 370, 370, 370, 370, 969, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370 },
			{ -1, 573, -1, -1, -1, -1, 140, -1, -1, -1, -1, 525, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 525, 573, 573, 573, 573, 573, 573, 573, 573, 573, 573, 573, 573, 573, 573, 573, 573, 573, 573, 573, 573, 573, 573, 573, 573 },
			{ -1, 688, -1, -1, -1, -1, -1, -1, -1, -1, -1, 525, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 525, 688, 688, 688, 688, 688, 688, 688, 688, 688, 688, 688, 688, 688, 688, 688, 688, 688, 688, 688, 688, 688, 688, 688, 688 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 930, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 317, -1, -1, -1, 318, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 319, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 334, 334, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 375, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 457, 457, 457, 457, 457, 457, 457, 457, 457, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 745, 745, 745, 745, 745, 745, 669, 199, 199, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 770, 770, 770, 770, 770, 770, 673, 211, 211, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 707, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 334, 334, -1, 691, 691, 691, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 705, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 435, -1, -1, -1, -1, 436, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 438, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 439, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 441, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 370, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 370, 370, 370, 370, 370, 370, 371, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 458, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 711, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 458, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 458, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 458, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 458, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 490, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 487, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 509, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 370, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 28, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 541, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 688, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 688, 688, 688, 688, 688, 688, 688, 688, 688, 688, 688, 688, 688, 688, 688, 688, 688, 688, 688, 688, 688, 688, 688, 688 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 551, 552, 552, 553, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 547, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 681, 681, 237, 243, 243, 243, 243, 243, 243, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 570, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 591, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 685, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 685, 685, 685, 685, 685, 685, 685, 685, 685, 685, 685, 685, 685, 685, 685, 685, 685, 685, 685, 685, 685, 685, 685, 685 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 586, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 618, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 622, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 64, -1, -1, -1, -1, 82, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64 },
			{ -1, -1, -1, -1, 764, -1, -1, -1, 764, -1, 764, -1, 739, 739, 116, 652, 652, 652, 652, 652, 652, -1, -1, -1, -1, -1, -1, 764, -1, -1, 764, -1, -1, -1, -1, -1, 67, -1, -1, -1, 67, 85, 101, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 631, 631, 631, 631, 631, 631, 631, 631, 631, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 641, 641, 641, 641, 641, 641, 80, 80, 80, 62, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 102, -1, -1, 336, 337, -1, 338, -1, 338, 695, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 695, -1, -1, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102 },
			{ -1, 282, -1, -1, 284, -1, -1, -1, -1, -1, -1, -1, 448, 448, 448, 449, 449, 449, 449, 449, 449, -1, -1, -1, -1, -1, -1, 286, -1, -1, 196, -1, -1, -1, -1, 288, -1, -1, -1, -1, -1, 290, 682, -1, -1, 244, -1, 683 },
			{ -1, 822, -1, -1, -1, -1, 82, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 861, 822, 822, 822, 822, 822, 822, 822 },
			{ -1, 678, -1, -1, -1, -1, 107, -1, -1, -1, -1, 585, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 585, 678, 678, 678, 678, 678, 678, 678, 678, 678, 678, 678, 678, 678, 678, 678, 678, 678, 678, 678, 678, 678, 678, 678, 678 },
			{ -1, 81, -1, -1, 249, -1, -1, -1, -1, 112, 253, 136, 272, 272, 274, 274, 274, 274, 274, 274, 274, 261, -1, -1, 265, -1, -1, 189, -1, -1, 196, 4, 202, -1, -1, 208, 214, 220, 269, -1, 226, 232, 238, -1, 43, 244, 850, 683 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 369, -1, 663, 663, 663, 663, 663, 663, 663, 663, 663, 369, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 252, -1, -1, -1, -1, 107, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252 },
			{ -1, 282, -1, -1, 284, -1, -1, -1, -1, -1, -1, -1, 227, 227, 227, 227, 227, 227, 233, 233, 233, 221, -1, -1, -1, -1, -1, 286, -1, -1, 196, -1, -1, -1, -1, 288, -1, -1, -1, -1, -1, 290, 682, -1, -1, 244, -1, 683 },
			{ -1, 687, -1, -1, -1, -1, 140, -1, -1, -1, -1, 525, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 525, 687, 687, 687, 687, 687, 687, 687, 687, 687, 687, 687, 687, 687, 687, 687, 687, 687, 687, 687, 687, 687, 687, 687, 687 },
			{ -1, 285, -1, -1, -1, -1, 140, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 285, 285, 285, 285, 285, 285, 285, 285, 285, 285, 285, 285, 285, 285, 285, 285, 285, 285, 285, 285, 285, 285, 285, 285 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 235, 235, 235, 235, 235, 241, 246, 246, 246, 223, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 662, -1, -1, 420, 866, -1, 421, -1, 421, 422, -1, 255, 255, 255, 255, 255, 255, 255, 255, 255, -1, -1, -1, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 353, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 245, -1, -1, 278, -1, 82, -1, -1, 329, 278, 280, 6, 6, 6, 630, 731, 731, 731, 731, 731, -1, -1, 280, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 254 },
			{ -1, 74, -1, -1, 460, 461, -1, 462, -1, 462, 464, -1, 677, 677, 677, 677, 677, 677, 677, 677, 677, 464, -1, -1, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 924, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 370, -1, -1, -1, -1, 82, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 371, 370, 370, 370, 370, 370 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 352, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 410, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 759, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 370, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 411, 370, 370, 370, 370, 370 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 492, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 499, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 414, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 440, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 493, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 64, -1, -1, -1, -1, 82, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 64, 64, 64, 64, 781, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64 },
			{ -1, 282, -1, -1, 284, -1, -1, -1, -1, -1, -1, -1, 449, 449, 449, 449, 449, 449, 449, 449, 449, -1, -1, -1, -1, -1, -1, 286, -1, -1, 196, -1, -1, -1, -1, 288, -1, -1, -1, -1, -1, 290, 682, -1, -1, 244, -1, 683 },
			{ -1, 822, -1, -1, -1, -1, 82, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 822, 822, 822, 822, 822, 922, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822 },
			{ -1, 81, -1, -1, 249, -1, -1, -1, -1, 112, 253, 136, 274, 274, 274, 274, 274, 274, 274, 274, 274, 261, -1, -1, 265, -1, -1, 189, -1, -1, 196, 4, 202, -1, -1, 208, 214, 220, 269, -1, 226, 232, 238, -1, 43, 244, 850, 683 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 369, -1, 210, 210, 210, 210, 210, 210, 210, 210, 210, 369, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 241, 241, 241, 241, 241, 241, 246, 246, 246, 223, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 662, -1, -1, 420, 866, -1, 421, -1, 421, 422, -1, 259, 259, 259, 259, 259, 259, 259, 259, 259, -1, -1, -1, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662 },
			{ -1, 245, -1, -1, 278, -1, 82, -1, -1, 329, 278, 280, 6, 6, 6, 630, 731, 731, 731, 731, 731, -1, -1, 280, 245, 245, 245, 245, 254, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 927, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 370, -1, -1, -1, -1, 82, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 372, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 494, 495, 495, 495, 495, 495, 495, 495, 495, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 455, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 370, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 992, 370, 370, 465, 370, 370, 370, 370, 370, 370, 370 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 437, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 64, -1, -1, -1, -1, 82, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 791, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 800, 64, 64, 64, 64 },
			{ -1, 282, -1, -1, 284, -1, -1, -1, -1, -1, -1, -1, 449, 449, 449, 449, 449, 449, 450, 63, 63, -1, -1, -1, -1, -1, -1, 286, -1, -1, 196, -1, -1, -1, -1, 288, -1, -1, -1, -1, -1, 290, 682, -1, -1, 244, -1, 683 },
			{ -1, 822, -1, -1, -1, -1, 82, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 822, 822, 984, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 369, -1, 210, 210, 210, 210, 210, 216, 216, 216, 216, 369, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 245, -1, -1, 278, -1, 82, -1, -1, 329, 278, 280, 6, 6, 6, 630, 731, 731, 731, 731, 731, -1, -1, 280, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 686, 245, 245, 245, 245, 245 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 335, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 370, -1, -1, -1, -1, 82, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 411, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 495, 495, 495, 495, 495, 495, 495, 495, 495, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 370, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 470, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 508, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 497, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 64, -1, -1, -1, -1, 82, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 807, 64, 64, 64, 64, 64, 64, 963, 64, 64, 64, 64, 64, 170, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64 },
			{ -1, 822, -1, -1, -1, -1, 82, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 966, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 369, -1, 216, 216, 216, 216, 216, 216, 216, 216, 216, 369, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 245, -1, -1, 278, -1, 82, -1, -1, 278, 278, 280, 6, 6, 6, 630, 731, 731, 731, 731, 731, -1, -1, 280, 245, 245, 245, 245, 245, 245, 245, 254, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 342, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 370, -1, -1, -1, -1, 82, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 370, 370, 370, 370, 370, 370, 411, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 574, 574, 574, 574, 574, 574, 574, 574, 574, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 370, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 370, 370, 370, 370, 370, 370, 370, 471, 370, 370, 370, 370, 370, 472, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370 },
			{ -1, 64, -1, -1, -1, -1, 82, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 64, 64, 64, 64, 897, 64, 64, 900, 64, 64, 64, 64, 64, 180, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64 },
			{ -1, 822, -1, -1, -1, -1, 82, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 747, 822, 771, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 349, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 370, -1, -1, -1, -1, 82, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 370, 370, 370, 371, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 601, 601, 601, 601, 601, 601, 602, 602, 602, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 370, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 473, 370, 370, 370, 474, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 715, 370, 370, 370, 370 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 517, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 64, -1, -1, -1, -1, 82, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 64, 64, 813, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64 },
			{ -1, 822, -1, -1, -1, -1, 82, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 884, 822, 822, 822, 822, 822, 822, 989 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 354, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 605, 605, 605, 605, 605, 605, 605, 605, 605, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 370, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 370, 370, 370, 370, 370, 370, 475, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 476, 370, 370, 370, 370 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 518, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 64, -1, -1, -1, -1, 82, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 190, 64, 64, 64, 818, 64, 64, 902, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 170, 64, 64, 64, 64 },
			{ -1, 822, -1, -1, -1, -1, 82, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 973, 822, 822, 822, 822, 822 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 423, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 609, 609, 609, 609, 609, 609, 609, 609, 609, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 370, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 370, 370, 370, 370, 477, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370 },
			{ -1, 64, -1, -1, -1, -1, 82, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 64, 64, 64, 64, 900, 64, 197, 64, 64, 64, 64, 64, 64, 904, 64, 64, 64, 64, 64, 203, 64, 972, 64, 64 },
			{ -1, 822, -1, -1, -1, -1, 82, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 822, 822, 890, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 783, 822, 822, 822, 822, 822, 945, 822, 822, 822 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 612, 612, 612, 612, 612, 612, 613, 605, 605, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 370, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 370, 370, 370, 371, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370 },
			{ -1, 64, -1, -1, -1, -1, 82, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 64, 64, 64, 64, 209, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 728, 728, 728, 728, 728, 728, 728, 728, 728, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 370, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 372, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370 },
			{ -1, 245, -1, -1, -1, -1, 82, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 684, 245, 245, 245, 245, 245 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 620, 620, 620, 620, 620, 620, 620, 620, 620, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 370, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 411 },
			{ -1, 245, -1, -1, -1, -1, 82, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 750, 245, 245, 245, 245, 245, 245 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 624, 624, 624, 624, 624, 624, 624, 624, 624, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 370, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 519, 370, 370, 370, 370 },
			{ -1, 245, -1, -1, -1, -1, 82, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 245, 245, 245, 245, 245, 245, 750, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245 },
			{ -1, 370, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 519, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370 },
			{ -1, 245, -1, -1, -1, -1, 82, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 258, 245, 245, 245, 245, 245 },
			{ -1, 370, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 370, 370, 370, 370, 370, 370, 521, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370 },
			{ -1, 245, -1, -1, -1, -1, 82, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 802, 258, 245, 245, 245, 245, 245, 245 },
			{ -1, 370, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 411, 370, 370, 370, 370, 370, 370, 370 },
			{ -1, 245, -1, -1, -1, -1, 82, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 262, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245 },
			{ -1, 370, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 522, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370 },
			{ -1, 370, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 371, 370, 370, 370, 370, 370, 370 },
			{ -1, 370, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 545, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370 },
			{ -1, 370, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 564, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370 },
			{ -1, 370, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 571, 370, 370, 370, 370, 370 },
			{ -1, 370, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 572, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370 },
			{ -1, 370, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 370, 370, 370, 370, 370, 370, 607, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370 },
			{ -1, 245, -1, -1, -1, -1, 82, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 951, 245, 245, 245, 245 },
			{ -1, 102, -1, -1, 336, 337, -1, 338, -1, 339, 340, -1, 46, 46, 46, 638, 638, 638, 638, 638, 638, 341, -1, -1, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102 },
			{ -1, 370, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 959, 370, 370, 370, 370 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 634, 634, 634, 634, 634, 634, 634, 634, 634, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 308, -1, -1, -1, -1, 308, 308, -1, 10, 10, 10, 10, 10, 10, 10, 10, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 637, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 102, -1, -1, 336, 337, -1, 338, -1, 339, 694, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 695, -1, -1, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 639, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 370, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 971, 370, 842, 370, 370, 370, 370 },
			{ -1, 825, -1, -1, -1, -1, 82, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 828, 822, 822, 822, 822, 822, 822, 822 },
			{ -1, 102, -1, -1, 336, 337, -1, 338, -1, 339, 694, -1, 117, 117, 117, 653, 653, 653, 653, 653, 653, 695, -1, -1, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 647, 647, 647, 647, 647, 647, 647, 647, 647, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 650, -1, -1, -1, -1, 82, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 650, 650, 650, 650, 650, 650, 650, 650, 650, 650, 650, 650, 650, 650, 650, 650, 650, 650, 650, 650, 650, 650, 650, 650 },
			{ -1, 740, -1, -1, -1, -1, 107, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 740, 740, 740, 740, 740, 740, 740, 740, 740, 740, 740, 740, 740, 740, 740, 740, 740, 740, 740, 740, 740, 740, 740, 740 },
			{ -1, 822, -1, -1, 278, -1, 82, -1, -1, 278, 278, 280, 6, 6, 6, 630, 731, 731, 731, 731, 731, -1, -1, 280, 822, 822, 822, 822, 822, 822, 822, 793, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822 },
			{ -1, 743, -1, -1, -1, -1, 140, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 743, 743, 743, 743, 743, 743, 743, 743, 743, 743, 743, 743, 743, 743, 743, 743, 743, 743, 743, 743, 743, 743, 743, 743 },
			{ -1, 245, -1, -1, 278, -1, 82, -1, -1, 329, 278, 280, 6, 6, 6, 630, 731, 731, 731, 731, 731, -1, -1, 280, 245, 245, 245, 245, 245, 245, 245, 785, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245 },
			{ -1, 74, -1, -1, 460, 461, -1, 462, -1, 462, 463, -1, 680, 680, 680, 680, 680, 748, 748, 748, 748, 463, -1, -1, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74 },
			{ -1, 370, -1, -1, -1, -1, 82, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 704, 370, 370, 370, 370, 370 },
			{ -1, -1, -1, -1, 308, -1, -1, -1, -1, 308, 308, -1, 877, 891, 891, 896, 10, 10, 10, 10, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 753, 753, 753, 753, 753, 753, 753, 753, 753, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 883, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 883, 883, 883, 883, 883, 883, 883, 883, 883, 883, 883, 883, 883, 883, 883, 883, 883, 883, 883, 883, 883, 883, 883, 883 },
			{ -1, 370, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 827, 370, 370, 370, 370, 370, 370, 370 },
			{ -1, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 826, 826, 826, 826, 826, 826, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 64, -1, -1, 278, -1, 82, -1, -1, 278, 278, -1, 6, 6, 6, 630, 731, 731, 731, 731, 731, -1, -1, -1, 64, 64, 64, 64, 64, 64, 64, 148, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64 },
			{ -1, -1, -1, -1, 308, -1, -1, -1, -1, 864, 308, -1, 10, 10, 10, 10, 10, 10, 10, 10, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 755, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 308, -1, -1, -1, -1, 864, 308, -1, 10, 10, 10, 10, 10, 10, 10, 10, 10, -1, -1, -1, -1, -1, -1, -1, 454, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 64, -1, -1, -1, -1, 82, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 64, 64, 64, 64, 64, 64, 64, 880, 64, 64, 983, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64 },
			{ -1, 245, -1, -1, -1, -1, 82, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 863, 245, 245, 245, 245, 245, 245, 245 },
			{ -1, 245, -1, -1, -1, -1, 82, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 898, 245, 245, 245, 245, 245, 245 },
			{ -1, 102, -1, -1, 336, 337, -1, 338, -1, 339, 340, -1, 638, 638, 638, 638, 638, 638, 638, 638, 638, 341, -1, -1, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 847, 732, 732, 732, 732, 732, 732, 732, 732, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 370, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 909, 370, 370, 370, 370, 370, 370 },
			{ -1, 370, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 843, 370, 370, 370, 971, 370, 370, 370, 370, 370, 370 },
			{ -1, 822, -1, -1, -1, -1, 82, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 822, 822, 822, 822, 822, 831, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822 },
			{ -1, 102, -1, -1, 336, 337, -1, 338, -1, 339, 694, -1, 653, 653, 653, 653, 653, 653, 653, 653, 653, 695, -1, -1, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102 },
			{ -1, 245, -1, -1, 278, -1, 82, -1, -1, 329, 278, 280, 6, 6, 6, 630, 731, 731, 731, 731, 731, -1, -1, 280, 245, 245, 795, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245 },
			{ -1, 74, -1, -1, 460, 461, -1, 462, -1, 462, 463, -1, 748, 748, 748, 748, 748, 748, 748, 748, 748, 463, -1, -1, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74, 74 },
			{ -1, 370, -1, -1, -1, -1, 82, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 756, 370, 370, 370, 370, 370, 370 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 942, 942, 942, 942, 942, 942, 942, 942, 942, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 370, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 830, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370 },
			{ -1, 64, -1, -1, -1, -1, 82, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 892, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64 },
			{ -1, 245, -1, -1, -1, -1, 82, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 893, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 732, 732, 732, 732, 732, 732, 732, 732, 732, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 822, -1, -1, -1, -1, 82, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 833, 822, 822, 822, 822, 822, 822 },
			{ -1, 370, -1, -1, -1, -1, 82, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 820, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 796, 796, 796, 796, 796, 796, 796, 796, 796, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 370, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 370, 370, 370, 370, 832, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 732, 732, 847, 847, 847, 847, 847, 847, 847, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 822, -1, -1, -1, -1, 82, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 833, 822 },
			{ -1, 370, -1, -1, -1, -1, 82, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 370, 370, 370, 824, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370 },
			{ -1, 370, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 370, 370, 370, 370, 370, 834, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370 },
			{ -1, 822, -1, -1, -1, -1, 82, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 825, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822 },
			{ -1, 370, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 370, 370, 370, 370, 836, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370 },
			{ -1, 822, -1, -1, -1, -1, 82, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 825, 822 },
			{ -1, 370, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 370, 370, 370, 824, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370 },
			{ -1, 822, -1, -1, -1, -1, 82, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 822, 822, 822, 837, 822, 822, 822, 822, 822, 822, 822, 950, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822 },
			{ -1, 370, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 704, 370, 370, 370, 370, 370 },
			{ -1, 370, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 838, 370, 370, 370, 370, 370, 370, 370 },
			{ -1, 370, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 839, 370, 370, 370, 370 },
			{ -1, 370, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 370, 370, 370, 840, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370 },
			{ -1, 370, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 370, 370, 370, 370, 370, 844, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370 },
			{ -1, 822, -1, -1, -1, -1, 82, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 874, 822, 822, 822, 822 },
			{ -1, -1, -1, -1, 308, -1, -1, -1, -1, 864, 308, -1, 10, 10, 10, 10, 10, 10, 10, 10, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 308, -1, -1, -1, -1, 864, 308, -1, 10, 10, 10, 10, 10, 10, 10, 10, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 373, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 308, -1, -1, -1, -1, 864, 308, -1, 10, 10, 10, 10, 10, 10, 10, 10, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 393, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 308, -1, -1, -1, -1, 864, 308, -1, 10, 10, 10, 10, 10, 10, 10, 10, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 394, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 308, -1, -1, -1, -1, 864, 308, -1, 10, 10, 10, 10, 10, 10, 10, 10, 10, -1, -1, -1, -1, -1, -1, -1, 395, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 308, -1, -1, -1, -1, 864, 308, -1, 10, 10, 10, 10, 10, 10, 10, 10, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 66 },
			{ -1, -1, -1, -1, 308, -1, -1, -1, -1, 864, 308, -1, 10, 10, 10, 10, 10, 10, 10, 10, 10, -1, -1, -1, -1, -1, -1, -1, 66, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 308, -1, -1, -1, -1, 864, 308, -1, 10, 10, 10, 10, 10, 10, 10, 10, 10, -1, -1, -1, -1, -1, 396, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 308, -1, -1, -1, -1, 864, 308, -1, 10, 10, 10, 10, 10, 10, 10, 10, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 400, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 308, -1, -1, -1, -1, 864, 308, -1, 10, 10, 10, 10, 10, 10, 10, 10, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 872, -1, -1, -1, -1, -1 },
			{ -1, 245, -1, -1, 278, -1, 82, -1, -1, 329, 278, 280, 6, 6, 6, 630, 731, 731, 731, 731, 731, -1, -1, 280, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 886, 245, 245, 245, 245 },
			{ -1, 370, -1, -1, -1, -1, 82, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 867, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 913, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 914, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 915, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 916, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 871, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 917, -1, 918, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 919, -1, -1, -1, -1, -1, -1, 912 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 916, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 920, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 181, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 370, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 867, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 931, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 311, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 306, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 933, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 245, -1, -1, -1, -1, 82, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 943, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245 },
			{ -1, 245, -1, -1, -1, -1, 82, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 956, 245, 245, 245, 245, 245, 245, 245 },
			{ -1, 370, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 961, 370, 370, 370, 370, 370, 370, 370 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 894, 894, 894, 894, 894, 894, 894, 894, 894, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 370, -1, -1, -1, -1, 82, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 370, 370, 370, 370, 370, 370, 370, 899, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370 },
			{ -1, 370, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 370, 901, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370 },
			{ -1, 245, -1, -1, -1, -1, 82, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 245, 245, 245, 245, 954, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245 },
			{ -1, 245, -1, -1, -1, -1, 82, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 981, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245 },
			{ -1, 370, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 978, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370 },
			{ -1, 370, -1, -1, -1, -1, 82, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 370, 901, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370 },
			{ -1, 370, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 905, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370 },
			{ -1, 245, -1, -1, -1, -1, 82, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 958, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245 },
			{ -1, 370, -1, -1, -1, -1, 82, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 903, 370, 370, 370, 370, 370, 370, 370 },
			{ -1, 370, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 907, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370 },
			{ -1, 245, -1, -1, -1, -1, 82, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 960, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245 },
			{ -1, 370, -1, -1, -1, -1, 82, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 905, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370 },
			{ -1, 370, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 903, 370, 370, 370, 370, 370, 370 },
			{ -1, 370, -1, -1, -1, -1, 82, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 903, 370, 370, 370, 370, 370, 370 },
			{ -1, 370, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 908, 370, 370, 370, 370, 370, 370, 370 },
			{ -1, 370, -1, -1, -1, -1, 82, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 906, 370, 370, 370, 370, 370, 370, 370 },
			{ -1, 370, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 909, 370, 370, 370, 370, 370, 370, 370 },
			{ -1, 905, -1, -1, -1, -1, 82, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370 },
			{ -1, 370, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 370, 370, 370, 370, 370, 370, 370, 910, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370 },
			{ -1, 822, -1, -1, -1, -1, 82, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 822, 822, 822, 939, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 925, -1, -1, -1, -1, 926, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 928, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 929, -1, -1, -1, -1 },
			{ -1, 245, -1, -1, 278, -1, 82, -1, -1, 329, 278, 280, 6, 6, 6, 630, 731, 731, 731, 731, 731, -1, -1, 280, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 923, 245, 245, 245, 245 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 932, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 370, -1, -1, -1, -1, 82, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 934, 370, 370, 370, 370 },
			{ -1, 370, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 944, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370 },
			{ -1, 245, -1, -1, -1, -1, 82, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 977, 245, 245, 245 },
			{ -1, 822, -1, -1, -1, -1, 82, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 822, 822, 822, 822, 953, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822 },
			{ -1, 245, -1, -1, 278, -1, 82, -1, -1, 329, 278, 280, 6, 6, 6, 630, 731, 731, 731, 731, 731, -1, -1, 280, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 948, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245 },
			{ -1, 370, -1, -1, -1, -1, 82, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 944, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370 },
			{ -1, 370, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 370, 370, 370, 370, 370, 370, 980, 370, 370, 370, 370, 370, 962, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370 },
			{ -1, 245, -1, -1, -1, -1, 82, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 982, 245, 245, 245, 245, 245 },
			{ -1, 370, -1, -1, -1, -1, 82, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 370, 370, 370, 370, 949, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370 },
			{ -1, 370, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 370, 370, 370, 370, 961, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370 },
			{ -1, 370, -1, -1, -1, -1, 82, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 370, 370, 370, 370, 370, 370, 370, 952, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370 },
			{ -1, 370, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 962, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370 },
			{ -1, 370, -1, -1, -1, -1, 82, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 370, 370, 370, 370, 955, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370 },
			{ -1, 370, -1, -1, -1, -1, 82, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 370, 370, 370, 370, 957, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370 },
			{ -1, 822, -1, -1, -1, -1, 82, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 822, 822, 822, 822, 970, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822 },
			{ -1, 245, -1, -1, 278, -1, 82, -1, -1, 329, 278, 280, 6, 6, 6, 630, 731, 731, 731, 731, 731, -1, -1, 280, 245, 245, 245, 245, 974, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245 },
			{ -1, 370, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 975, 370, 370, 370, 370, 370 },
			{ -1, 64, -1, -1, -1, -1, 82, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 988, 64, 64, 64, 64, 64, 64, 64 },
			{ -1, 245, -1, -1, -1, -1, 82, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 979, 245, 245, 245 },
			{ -1, 822, -1, -1, -1, -1, 82, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 822, 822, 822, 822, 987, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822 },
			{ -1, 245, -1, -1, 278, -1, 82, -1, -1, 329, 278, 280, 6, 6, 6, 630, 731, 731, 731, 731, 731, -1, -1, 280, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245, 245 },
			{ -1, 64, -1, -1, -1, -1, 82, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 64, 64, 64, 64, 991, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64 },
			{ -1, 822, -1, -1, -1, -1, 82, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 822, 976, 822, 822, 822, 822, 822, 822 },
			{ -1, 370, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 370, 985, 370, 370, 370, 370, 370, 370, 370 }
		};
		
		
		private static readonly int[] yy_state_dtrans = new int[]
		{
			  0
		};
		
		#endregion
		
		private Tokens GetNextToken()
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
							System.Diagnostics.Debug.Assert(last_accept_state >= 993);
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

