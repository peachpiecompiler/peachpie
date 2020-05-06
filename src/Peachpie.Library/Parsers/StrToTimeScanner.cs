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
		
		public Scanner(System.IO.TextReader reader)
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
		
		#pragma warning disable 162
		
		
		Tokens Accept0(int state,out bool accepted)
		{
			accepted = true;
			
			switch(state)
			{
				case 1:
					// #line 697
					{
					  break;
					}
					break;
					
				case 2:
					// #line 701
					{
					  return Tokens.ERROR;
					}
					break;
					
				case 3:
					// #line 643
					{
						INIT();
						errors += time.SetTimeZone(str, ref pos) ? 0 : 1;
						DEINIT();
						return Tokens.TIMEZONE;
					}
					break;
					
				case 4:
					// #line 434
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
					break;
					
				case 5:
					// #line 197
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
					break;
					
				case 6:
					// #line 420
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
					break;
					
				case 7:
					// #line 242
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
					break;
					
				case 8:
					// #line 305
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
					break;
					
				case 9:
					// #line 335
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
					break;
					
				case 10:
					// #line 218
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
					break;
					
				case 11:
					// #line 596
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
					break;
					
				case 12:
					// #line 629
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
					break;
					
				case 13:
					// #line 162
					{
						INIT();
						DEINIT();
						return Tokens.RELATIVE;
					}
					break;
					
				case 14:
					// #line 265
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
					break;
					
				case 15:
					// #line 587
					{
						INIT();
						time.y = DateInfo.ParseUnsignedInt(str, ref pos, 4);
						DEINIT();
						return Tokens.CLF;
					}
					break;
					
				case 16:
					// #line 681
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
					break;
					
				case 17:
					// #line 169
					{
						INIT();
						time.UNHAVE_TIME();
						time.HAVE_TIME();
						time.h = 12;
						DEINIT();
						return Tokens.RELATIVE;
					}
					break;
					
				case 18:
					// #line 350
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
					break;
					
				case 19:
					// #line 405
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
					break;
					
				case 20:
					// #line 391
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
					break;
					
				case 21:
					// #line 652
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
					break;
					
				case 22:
					// #line 179
					{
						INIT();
						time.UNHAVE_TIME();
						DEINIT();
						return Tokens.RELATIVE;
					}
					break;
					
				case 23:
					// #line 377
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
					break;
					
				case 24:
					// #line 289
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
					break;
					
				case 25:
					// #line 485
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
					break;
					
				case 26:
					// #line 518
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
					break;
					
				case 27:
					// #line 611
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
					break;
					
				case 28:
					// #line 364
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
					break;
					
				case 29:
					// #line 447
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
					break;
					
				case 30:
					// #line 499
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
					break;
					
				case 31:
					// #line 539
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
					break;
					
				case 32:
					// #line 187
					{
						INIT();
						time.HAVE_RELATIVE();
						time.UNHAVE_TIME();
						time.relative.d = 1;
						DEINIT();
						return Tokens.RELATIVE;
					}
					break;
					
				case 33:
					// #line 553
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
					break;
					
				case 34:
					// #line 152
					{
						INIT();
						time.HAVE_RELATIVE();
						time.UNHAVE_TIME();
						time.relative.d = -1;
						DEINIT();
						return Tokens.RELATIVE;
					}
					break;
					
				case 35:
					// #line 322
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
					break;
					
				case 36:
					// #line 460
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
					break;
					
				case 37:
					// #line 567
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
					break;
					
				case 39: goto case 1;
				case 40: goto case 2;
				case 41: goto case 3;
				case 42: goto case 4;
				case 43: goto case 6;
				case 44: goto case 7;
				case 45: goto case 8;
				case 46: goto case 9;
				case 47: goto case 10;
				case 48: goto case 12;
				case 49: goto case 16;
				case 50: goto case 18;
				case 51: goto case 19;
				case 52: goto case 20;
				case 53: goto case 21;
				case 54: goto case 24;
				case 55: goto case 25;
				case 56: goto case 27;
				case 57: goto case 31;
				case 58: goto case 35;
				case 59: goto case 36;
				case 60: goto case 37;
				case 62: goto case 2;
				case 63: goto case 3;
				case 64: goto case 4;
				case 65: goto case 6;
				case 66: goto case 7;
				case 67: goto case 8;
				case 68: goto case 10;
				case 69: goto case 16;
				case 70: goto case 18;
				case 71: goto case 19;
				case 72: goto case 21;
				case 73: goto case 24;
				case 74: goto case 25;
				case 75: goto case 27;
				case 76: goto case 35;
				case 77: goto case 36;
				case 78: goto case 37;
				case 80: goto case 2;
				case 81: goto case 3;
				case 82: goto case 4;
				case 83: goto case 6;
				case 84: goto case 7;
				case 85: goto case 8;
				case 86: goto case 10;
				case 87: goto case 16;
				case 88: goto case 18;
				case 89: goto case 19;
				case 90: goto case 21;
				case 91: goto case 24;
				case 92: goto case 25;
				case 93: goto case 27;
				case 94: goto case 36;
				case 96: goto case 2;
				case 97: goto case 3;
				case 98: goto case 4;
				case 99: goto case 6;
				case 100: goto case 7;
				case 101: goto case 8;
				case 102: goto case 16;
				case 103: goto case 18;
				case 104: goto case 19;
				case 105: goto case 21;
				case 106: goto case 24;
				case 107: goto case 25;
				case 108: goto case 27;
				case 109: goto case 36;
				case 111: goto case 2;
				case 112: goto case 3;
				case 113: goto case 4;
				case 114: goto case 6;
				case 115: goto case 7;
				case 116: goto case 8;
				case 117: goto case 16;
				case 118: goto case 18;
				case 119: goto case 19;
				case 120: goto case 21;
				case 121: goto case 27;
				case 122: goto case 36;
				case 124: goto case 3;
				case 125: goto case 4;
				case 126: goto case 6;
				case 127: goto case 7;
				case 128: goto case 16;
				case 129: goto case 18;
				case 130: goto case 19;
				case 131: goto case 21;
				case 132: goto case 27;
				case 133: goto case 36;
				case 135: goto case 3;
				case 136: goto case 4;
				case 137: goto case 6;
				case 138: goto case 7;
				case 139: goto case 16;
				case 140: goto case 18;
				case 141: goto case 19;
				case 142: goto case 21;
				case 143: goto case 27;
				case 144: goto case 36;
				case 146: goto case 3;
				case 147: goto case 4;
				case 148: goto case 6;
				case 149: goto case 7;
				case 150: goto case 16;
				case 151: goto case 18;
				case 152: goto case 19;
				case 153: goto case 21;
				case 154: goto case 27;
				case 155: goto case 36;
				case 157: goto case 3;
				case 158: goto case 4;
				case 159: goto case 6;
				case 160: goto case 7;
				case 161: goto case 16;
				case 162: goto case 18;
				case 163: goto case 19;
				case 164: goto case 21;
				case 165: goto case 27;
				case 166: goto case 36;
				case 168: goto case 3;
				case 169: goto case 4;
				case 170: goto case 6;
				case 171: goto case 7;
				case 172: goto case 18;
				case 173: goto case 19;
				case 174: goto case 21;
				case 175: goto case 27;
				case 176: goto case 36;
				case 178: goto case 3;
				case 179: goto case 4;
				case 180: goto case 6;
				case 181: goto case 7;
				case 182: goto case 18;
				case 183: goto case 19;
				case 184: goto case 21;
				case 185: goto case 27;
				case 186: goto case 36;
				case 188: goto case 3;
				case 189: goto case 6;
				case 190: goto case 7;
				case 191: goto case 18;
				case 192: goto case 19;
				case 193: goto case 21;
				case 195: goto case 3;
				case 196: goto case 6;
				case 197: goto case 7;
				case 198: goto case 19;
				case 199: goto case 21;
				case 201: goto case 3;
				case 202: goto case 6;
				case 203: goto case 7;
				case 204: goto case 19;
				case 205: goto case 21;
				case 207: goto case 3;
				case 208: goto case 6;
				case 209: goto case 7;
				case 210: goto case 19;
				case 211: goto case 21;
				case 213: goto case 3;
				case 214: goto case 6;
				case 215: goto case 7;
				case 216: goto case 19;
				case 217: goto case 21;
				case 219: goto case 3;
				case 220: goto case 6;
				case 221: goto case 7;
				case 222: goto case 19;
				case 223: goto case 21;
				case 225: goto case 3;
				case 226: goto case 6;
				case 227: goto case 7;
				case 228: goto case 19;
				case 229: goto case 21;
				case 231: goto case 3;
				case 232: goto case 6;
				case 233: goto case 7;
				case 234: goto case 19;
				case 235: goto case 21;
				case 237: goto case 3;
				case 238: goto case 6;
				case 239: goto case 7;
				case 240: goto case 19;
				case 241: goto case 21;
				case 243: goto case 3;
				case 244: goto case 7;
				case 245: goto case 19;
				case 246: goto case 21;
				case 248: goto case 3;
				case 249: goto case 7;
				case 250: goto case 21;
				case 252: goto case 3;
				case 253: goto case 7;
				case 254: goto case 21;
				case 256: goto case 3;
				case 257: goto case 7;
				case 258: goto case 21;
				case 260: goto case 3;
				case 261: goto case 7;
				case 262: goto case 21;
				case 264: goto case 3;
				case 265: goto case 7;
				case 267: goto case 3;
				case 268: goto case 7;
				case 270: goto case 7;
				case 272: goto case 7;
				case 274: goto case 7;
				case 276: goto case 7;
				case 278: goto case 7;
				case 280: goto case 7;
				case 282: goto case 7;
				case 284: goto case 7;
				case 623: goto case 3;
				case 624: goto case 6;
				case 625: goto case 9;
				case 626: goto case 31;
				case 627: goto case 37;
				case 628: goto case 3;
				case 629: goto case 4;
				case 630: goto case 7;
				case 631: goto case 19;
				case 632: goto case 20;
				case 633: goto case 27;
				case 634: goto case 37;
				case 636: goto case 3;
				case 637: goto case 10;
				case 638: goto case 18;
				case 639: goto case 21;
				case 640: goto case 7;
				case 641: goto case 8;
				case 642: goto case 2;
				case 643: goto case 3;
				case 644: goto case 4;
				case 645: goto case 7;
				case 646: goto case 19;
				case 647: goto case 4;
				case 648: goto case 6;
				case 649: goto case 7;
				case 650: goto case 18;
				case 651: goto case 19;
				case 652: goto case 21;
				case 653: goto case 7;
				case 654: goto case 21;
				case 655: goto case 7;
				case 656: goto case 21;
				case 657: goto case 6;
				case 658: goto case 7;
				case 660: goto case 7;
				case 661: goto case 21;
				case 663: goto case 7;
				case 664: goto case 19;
				case 665: goto case 21;
				case 667: goto case 7;
				case 668: goto case 19;
				case 669: goto case 3;
				case 670: goto case 6;
				case 671: goto case 21;
				case 673: goto case 7;
				case 674: goto case 6;
				case 675: goto case 3;
				case 676: goto case 21;
				case 679: goto case 3;
				case 680: goto case 3;
				case 681: goto case 21;
				case 682: goto case 7;
				case 683: goto case 7;
				case 731: goto case 3;
				case 732: goto case 6;
				case 733: goto case 9;
				case 734: goto case 37;
				case 735: goto case 7;
				case 737: goto case 3;
				case 738: goto case 2;
				case 739: goto case 3;
				case 740: goto case 6;
				case 741: goto case 7;
				case 742: goto case 21;
				case 743: goto case 21;
				case 744: goto case 7;
				case 746: goto case 7;
				case 747: goto case 21;
				case 748: goto case 7;
				case 749: goto case 6;
				case 750: goto case 3;
				case 752: goto case 3;
				case 765: goto case 3;
				case 766: goto case 6;
				case 768: goto case 2;
				case 769: goto case 3;
				case 770: goto case 6;
				case 771: goto case 7;
				case 772: goto case 21;
				case 773: goto case 21;
				case 774: goto case 7;
				case 775: goto case 7;
				case 776: goto case 21;
				case 777: goto case 7;
				case 778: goto case 3;
				case 780: goto case 3;
				case 785: goto case 3;
				case 787: goto case 3;
				case 788: goto case 6;
				case 789: goto case 21;
				case 790: goto case 7;
				case 791: goto case 7;
				case 792: goto case 3;
				case 794: goto case 3;
				case 798: goto case 3;
				case 799: goto case 3;
				case 800: goto case 6;
				case 801: goto case 7;
				case 802: goto case 3;
				case 804: goto case 3;
				case 808: goto case 3;
				case 809: goto case 3;
				case 810: goto case 7;
				case 812: goto case 3;
				case 815: goto case 3;
				case 816: goto case 3;
				case 820: goto case 3;
				case 821: goto case 3;
				case 824: goto case 3;
				case 825: goto case 3;
				case 828: goto case 3;
				case 829: goto case 3;
				case 832: goto case 3;
				case 833: goto case 3;
				case 836: goto case 3;
				case 839: goto case 3;
				case 841: goto case 3;
				case 843: goto case 3;
				case 845: goto case 3;
				case 853: goto case 7;
				case 854: goto case 9;
				case 855: goto case 12;
				case 856: goto case 27;
				case 857: goto case 31;
				case 858: goto case 4;
				case 859: goto case 7;
				case 860: goto case 19;
				case 861: goto case 27;
				case 862: goto case 7;
				case 863: goto case 8;
				case 864: goto case 3;
				case 865: goto case 3;
				case 866: goto case 7;
				case 867: goto case 21;
				case 868: goto case 21;
				case 869: goto case 7;
				case 870: goto case 6;
				case 871: goto case 7;
				case 872: goto case 3;
				case 873: goto case 3;
				case 879: goto case 3;
				case 880: goto case 3;
				case 881: goto case 4;
				case 882: goto case 4;
				case 883: goto case 3;
				case 884: goto case 7;
				case 885: goto case 9;
				case 886: goto case 12;
				case 887: goto case 27;
				case 888: goto case 27;
				case 889: goto case 7;
				case 890: goto case 3;
				case 891: goto case 6;
				case 892: goto case 3;
				case 893: goto case 3;
				case 896: goto case 3;
				case 897: goto case 3;
				case 898: goto case 9;
				case 899: goto case 3;
				case 900: goto case 3;
				case 901: goto case 3;
				case 904: goto case 9;
				case 905: goto case 3;
				case 906: goto case 3;
				case 908: goto case 3;
				case 910: goto case 3;
				case 912: goto case 3;
				case 919: goto case 4;
				case 920: goto case 4;
				case 921: goto case 3;
				case 922: goto case 4;
				case 923: goto case 4;
				case 924: goto case 4;
				case 925: goto case 4;
				case 926: goto case 4;
				case 927: goto case 4;
				case 928: goto case 4;
				case 929: goto case 4;
				case 930: goto case 3;
				case 947: goto case 3;
				case 948: goto case 12;
				case 949: goto case 27;
				case 950: goto case 3;
				case 952: goto case 3;
				case 954: goto case 3;
				case 955: goto case 12;
				case 956: goto case 27;
				case 957: goto case 3;
				case 958: goto case 3;
				case 960: goto case 3;
				case 961: goto case 3;
				case 963: goto case 3;
				case 964: goto case 3;
				case 966: goto case 3;
				case 968: goto case 3;
				case 970: goto case 3;
				case 973: goto case 3;
				case 977: goto case 3;
				case 979: goto case 3;
				case 980: goto case 3;
				case 981: goto case 27;
				case 982: goto case 3;
				case 983: goto case 3;
				case 985: goto case 3;
				case 986: goto case 3;
				case 987: goto case 3;
				case 989: goto case 3;
				case 991: goto case 3;
				case 992: goto case 3;
				case 993: goto case 3;
				case 995: goto case 3;
				case 996: goto case 3;
				case 997: goto case 3;
				case 998: goto case 3;
				case 999: goto case 3;
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
			AcceptConditions.NotAccept, // 38
			AcceptConditions.Accept, // 39
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
			AcceptConditions.NotAccept, // 61
			AcceptConditions.Accept, // 62
			AcceptConditions.Accept, // 63
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
			AcceptConditions.NotAccept, // 79
			AcceptConditions.Accept, // 80
			AcceptConditions.Accept, // 81
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
			AcceptConditions.NotAccept, // 95
			AcceptConditions.Accept, // 96
			AcceptConditions.Accept, // 97
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
			AcceptConditions.NotAccept, // 110
			AcceptConditions.Accept, // 111
			AcceptConditions.Accept, // 112
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
			AcceptConditions.NotAccept, // 123
			AcceptConditions.Accept, // 124
			AcceptConditions.Accept, // 125
			AcceptConditions.Accept, // 126
			AcceptConditions.Accept, // 127
			AcceptConditions.Accept, // 128
			AcceptConditions.Accept, // 129
			AcceptConditions.Accept, // 130
			AcceptConditions.Accept, // 131
			AcceptConditions.Accept, // 132
			AcceptConditions.Accept, // 133
			AcceptConditions.NotAccept, // 134
			AcceptConditions.Accept, // 135
			AcceptConditions.Accept, // 136
			AcceptConditions.Accept, // 137
			AcceptConditions.Accept, // 138
			AcceptConditions.Accept, // 139
			AcceptConditions.Accept, // 140
			AcceptConditions.Accept, // 141
			AcceptConditions.Accept, // 142
			AcceptConditions.Accept, // 143
			AcceptConditions.Accept, // 144
			AcceptConditions.NotAccept, // 145
			AcceptConditions.Accept, // 146
			AcceptConditions.Accept, // 147
			AcceptConditions.Accept, // 148
			AcceptConditions.Accept, // 149
			AcceptConditions.Accept, // 150
			AcceptConditions.Accept, // 151
			AcceptConditions.Accept, // 152
			AcceptConditions.Accept, // 153
			AcceptConditions.Accept, // 154
			AcceptConditions.Accept, // 155
			AcceptConditions.NotAccept, // 156
			AcceptConditions.Accept, // 157
			AcceptConditions.Accept, // 158
			AcceptConditions.Accept, // 159
			AcceptConditions.Accept, // 160
			AcceptConditions.Accept, // 161
			AcceptConditions.Accept, // 162
			AcceptConditions.Accept, // 163
			AcceptConditions.Accept, // 164
			AcceptConditions.Accept, // 165
			AcceptConditions.Accept, // 166
			AcceptConditions.NotAccept, // 167
			AcceptConditions.Accept, // 168
			AcceptConditions.Accept, // 169
			AcceptConditions.Accept, // 170
			AcceptConditions.Accept, // 171
			AcceptConditions.Accept, // 172
			AcceptConditions.Accept, // 173
			AcceptConditions.Accept, // 174
			AcceptConditions.Accept, // 175
			AcceptConditions.Accept, // 176
			AcceptConditions.NotAccept, // 177
			AcceptConditions.Accept, // 178
			AcceptConditions.Accept, // 179
			AcceptConditions.Accept, // 180
			AcceptConditions.Accept, // 181
			AcceptConditions.Accept, // 182
			AcceptConditions.Accept, // 183
			AcceptConditions.Accept, // 184
			AcceptConditions.Accept, // 185
			AcceptConditions.Accept, // 186
			AcceptConditions.NotAccept, // 187
			AcceptConditions.Accept, // 188
			AcceptConditions.Accept, // 189
			AcceptConditions.Accept, // 190
			AcceptConditions.Accept, // 191
			AcceptConditions.Accept, // 192
			AcceptConditions.Accept, // 193
			AcceptConditions.NotAccept, // 194
			AcceptConditions.Accept, // 195
			AcceptConditions.Accept, // 196
			AcceptConditions.Accept, // 197
			AcceptConditions.Accept, // 198
			AcceptConditions.Accept, // 199
			AcceptConditions.NotAccept, // 200
			AcceptConditions.Accept, // 201
			AcceptConditions.Accept, // 202
			AcceptConditions.Accept, // 203
			AcceptConditions.Accept, // 204
			AcceptConditions.Accept, // 205
			AcceptConditions.NotAccept, // 206
			AcceptConditions.Accept, // 207
			AcceptConditions.Accept, // 208
			AcceptConditions.Accept, // 209
			AcceptConditions.Accept, // 210
			AcceptConditions.Accept, // 211
			AcceptConditions.NotAccept, // 212
			AcceptConditions.Accept, // 213
			AcceptConditions.Accept, // 214
			AcceptConditions.Accept, // 215
			AcceptConditions.Accept, // 216
			AcceptConditions.Accept, // 217
			AcceptConditions.NotAccept, // 218
			AcceptConditions.Accept, // 219
			AcceptConditions.Accept, // 220
			AcceptConditions.Accept, // 221
			AcceptConditions.Accept, // 222
			AcceptConditions.Accept, // 223
			AcceptConditions.NotAccept, // 224
			AcceptConditions.Accept, // 225
			AcceptConditions.Accept, // 226
			AcceptConditions.Accept, // 227
			AcceptConditions.Accept, // 228
			AcceptConditions.Accept, // 229
			AcceptConditions.NotAccept, // 230
			AcceptConditions.Accept, // 231
			AcceptConditions.Accept, // 232
			AcceptConditions.Accept, // 233
			AcceptConditions.Accept, // 234
			AcceptConditions.Accept, // 235
			AcceptConditions.NotAccept, // 236
			AcceptConditions.Accept, // 237
			AcceptConditions.Accept, // 238
			AcceptConditions.Accept, // 239
			AcceptConditions.Accept, // 240
			AcceptConditions.Accept, // 241
			AcceptConditions.NotAccept, // 242
			AcceptConditions.Accept, // 243
			AcceptConditions.Accept, // 244
			AcceptConditions.Accept, // 245
			AcceptConditions.Accept, // 246
			AcceptConditions.NotAccept, // 247
			AcceptConditions.Accept, // 248
			AcceptConditions.Accept, // 249
			AcceptConditions.Accept, // 250
			AcceptConditions.NotAccept, // 251
			AcceptConditions.Accept, // 252
			AcceptConditions.Accept, // 253
			AcceptConditions.Accept, // 254
			AcceptConditions.NotAccept, // 255
			AcceptConditions.Accept, // 256
			AcceptConditions.Accept, // 257
			AcceptConditions.Accept, // 258
			AcceptConditions.NotAccept, // 259
			AcceptConditions.Accept, // 260
			AcceptConditions.Accept, // 261
			AcceptConditions.Accept, // 262
			AcceptConditions.NotAccept, // 263
			AcceptConditions.Accept, // 264
			AcceptConditions.Accept, // 265
			AcceptConditions.NotAccept, // 266
			AcceptConditions.Accept, // 267
			AcceptConditions.Accept, // 268
			AcceptConditions.NotAccept, // 269
			AcceptConditions.Accept, // 270
			AcceptConditions.NotAccept, // 271
			AcceptConditions.Accept, // 272
			AcceptConditions.NotAccept, // 273
			AcceptConditions.Accept, // 274
			AcceptConditions.NotAccept, // 275
			AcceptConditions.Accept, // 276
			AcceptConditions.NotAccept, // 277
			AcceptConditions.Accept, // 278
			AcceptConditions.NotAccept, // 279
			AcceptConditions.Accept, // 280
			AcceptConditions.NotAccept, // 281
			AcceptConditions.Accept, // 282
			AcceptConditions.NotAccept, // 283
			AcceptConditions.Accept, // 284
			AcceptConditions.NotAccept, // 285
			AcceptConditions.NotAccept, // 286
			AcceptConditions.NotAccept, // 287
			AcceptConditions.NotAccept, // 288
			AcceptConditions.NotAccept, // 289
			AcceptConditions.NotAccept, // 290
			AcceptConditions.NotAccept, // 291
			AcceptConditions.NotAccept, // 292
			AcceptConditions.NotAccept, // 293
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
			AcceptConditions.Accept, // 623
			AcceptConditions.Accept, // 624
			AcceptConditions.Accept, // 625
			AcceptConditions.Accept, // 626
			AcceptConditions.Accept, // 627
			AcceptConditions.Accept, // 628
			AcceptConditions.Accept, // 629
			AcceptConditions.Accept, // 630
			AcceptConditions.Accept, // 631
			AcceptConditions.Accept, // 632
			AcceptConditions.Accept, // 633
			AcceptConditions.Accept, // 634
			AcceptConditions.NotAccept, // 635
			AcceptConditions.Accept, // 636
			AcceptConditions.Accept, // 637
			AcceptConditions.Accept, // 638
			AcceptConditions.Accept, // 639
			AcceptConditions.Accept, // 640
			AcceptConditions.Accept, // 641
			AcceptConditions.Accept, // 642
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
			AcceptConditions.NotAccept, // 659
			AcceptConditions.Accept, // 660
			AcceptConditions.Accept, // 661
			AcceptConditions.NotAccept, // 662
			AcceptConditions.Accept, // 663
			AcceptConditions.Accept, // 664
			AcceptConditions.Accept, // 665
			AcceptConditions.NotAccept, // 666
			AcceptConditions.Accept, // 667
			AcceptConditions.Accept, // 668
			AcceptConditions.Accept, // 669
			AcceptConditions.Accept, // 670
			AcceptConditions.Accept, // 671
			AcceptConditions.NotAccept, // 672
			AcceptConditions.Accept, // 673
			AcceptConditions.Accept, // 674
			AcceptConditions.Accept, // 675
			AcceptConditions.Accept, // 676
			AcceptConditions.NotAccept, // 677
			AcceptConditions.NotAccept, // 678
			AcceptConditions.Accept, // 679
			AcceptConditions.Accept, // 680
			AcceptConditions.Accept, // 681
			AcceptConditions.Accept, // 682
			AcceptConditions.Accept, // 683
			AcceptConditions.NotAccept, // 684
			AcceptConditions.NotAccept, // 685
			AcceptConditions.NotAccept, // 686
			AcceptConditions.NotAccept, // 687
			AcceptConditions.NotAccept, // 688
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
			AcceptConditions.NotAccept, // 730
			AcceptConditions.Accept, // 731
			AcceptConditions.Accept, // 732
			AcceptConditions.Accept, // 733
			AcceptConditions.Accept, // 734
			AcceptConditions.Accept, // 735
			AcceptConditions.NotAccept, // 736
			AcceptConditions.Accept, // 737
			AcceptConditions.Accept, // 738
			AcceptConditions.Accept, // 739
			AcceptConditions.Accept, // 740
			AcceptConditions.Accept, // 741
			AcceptConditions.Accept, // 742
			AcceptConditions.Accept, // 743
			AcceptConditions.Accept, // 744
			AcceptConditions.NotAccept, // 745
			AcceptConditions.Accept, // 746
			AcceptConditions.Accept, // 747
			AcceptConditions.Accept, // 748
			AcceptConditions.Accept, // 749
			AcceptConditions.Accept, // 750
			AcceptConditions.NotAccept, // 751
			AcceptConditions.Accept, // 752
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
			AcceptConditions.NotAccept, // 763
			AcceptConditions.NotAccept, // 764
			AcceptConditions.Accept, // 765
			AcceptConditions.Accept, // 766
			AcceptConditions.NotAccept, // 767
			AcceptConditions.Accept, // 768
			AcceptConditions.Accept, // 769
			AcceptConditions.Accept, // 770
			AcceptConditions.Accept, // 771
			AcceptConditions.Accept, // 772
			AcceptConditions.Accept, // 773
			AcceptConditions.Accept, // 774
			AcceptConditions.Accept, // 775
			AcceptConditions.Accept, // 776
			AcceptConditions.Accept, // 777
			AcceptConditions.Accept, // 778
			AcceptConditions.NotAccept, // 779
			AcceptConditions.Accept, // 780
			AcceptConditions.NotAccept, // 781
			AcceptConditions.NotAccept, // 782
			AcceptConditions.NotAccept, // 783
			AcceptConditions.NotAccept, // 784
			AcceptConditions.Accept, // 785
			AcceptConditions.NotAccept, // 786
			AcceptConditions.Accept, // 787
			AcceptConditions.Accept, // 788
			AcceptConditions.Accept, // 789
			AcceptConditions.Accept, // 790
			AcceptConditions.Accept, // 791
			AcceptConditions.Accept, // 792
			AcceptConditions.NotAccept, // 793
			AcceptConditions.Accept, // 794
			AcceptConditions.NotAccept, // 795
			AcceptConditions.NotAccept, // 796
			AcceptConditions.NotAccept, // 797
			AcceptConditions.Accept, // 798
			AcceptConditions.Accept, // 799
			AcceptConditions.Accept, // 800
			AcceptConditions.Accept, // 801
			AcceptConditions.Accept, // 802
			AcceptConditions.NotAccept, // 803
			AcceptConditions.Accept, // 804
			AcceptConditions.NotAccept, // 805
			AcceptConditions.NotAccept, // 806
			AcceptConditions.NotAccept, // 807
			AcceptConditions.Accept, // 808
			AcceptConditions.Accept, // 809
			AcceptConditions.Accept, // 810
			AcceptConditions.NotAccept, // 811
			AcceptConditions.Accept, // 812
			AcceptConditions.NotAccept, // 813
			AcceptConditions.NotAccept, // 814
			AcceptConditions.Accept, // 815
			AcceptConditions.Accept, // 816
			AcceptConditions.NotAccept, // 817
			AcceptConditions.NotAccept, // 818
			AcceptConditions.NotAccept, // 819
			AcceptConditions.Accept, // 820
			AcceptConditions.Accept, // 821
			AcceptConditions.NotAccept, // 822
			AcceptConditions.NotAccept, // 823
			AcceptConditions.Accept, // 824
			AcceptConditions.Accept, // 825
			AcceptConditions.NotAccept, // 826
			AcceptConditions.NotAccept, // 827
			AcceptConditions.Accept, // 828
			AcceptConditions.Accept, // 829
			AcceptConditions.NotAccept, // 830
			AcceptConditions.NotAccept, // 831
			AcceptConditions.Accept, // 832
			AcceptConditions.Accept, // 833
			AcceptConditions.NotAccept, // 834
			AcceptConditions.NotAccept, // 835
			AcceptConditions.Accept, // 836
			AcceptConditions.NotAccept, // 837
			AcceptConditions.NotAccept, // 838
			AcceptConditions.Accept, // 839
			AcceptConditions.NotAccept, // 840
			AcceptConditions.Accept, // 841
			AcceptConditions.NotAccept, // 842
			AcceptConditions.Accept, // 843
			AcceptConditions.NotAccept, // 844
			AcceptConditions.Accept, // 845
			AcceptConditions.NotAccept, // 846
			AcceptConditions.NotAccept, // 847
			AcceptConditions.NotAccept, // 848
			AcceptConditions.NotAccept, // 849
			AcceptConditions.NotAccept, // 850
			AcceptConditions.NotAccept, // 851
			AcceptConditions.NotAccept, // 852
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
			AcceptConditions.Accept, // 864
			AcceptConditions.Accept, // 865
			AcceptConditions.Accept, // 866
			AcceptConditions.Accept, // 867
			AcceptConditions.Accept, // 868
			AcceptConditions.Accept, // 869
			AcceptConditions.Accept, // 870
			AcceptConditions.Accept, // 871
			AcceptConditions.Accept, // 872
			AcceptConditions.Accept, // 873
			AcceptConditions.NotAccept, // 874
			AcceptConditions.NotAccept, // 875
			AcceptConditions.NotAccept, // 876
			AcceptConditions.NotAccept, // 877
			AcceptConditions.NotAccept, // 878
			AcceptConditions.Accept, // 879
			AcceptConditions.Accept, // 880
			AcceptConditions.Accept, // 881
			AcceptConditions.Accept, // 882
			AcceptConditions.Accept, // 883
			AcceptConditions.Accept, // 884
			AcceptConditions.Accept, // 885
			AcceptConditions.Accept, // 886
			AcceptConditions.Accept, // 887
			AcceptConditions.Accept, // 888
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
			AcceptConditions.Accept, // 899
			AcceptConditions.Accept, // 900
			AcceptConditions.Accept, // 901
			AcceptConditions.NotAccept, // 902
			AcceptConditions.NotAccept, // 903
			AcceptConditions.Accept, // 904
			AcceptConditions.Accept, // 905
			AcceptConditions.Accept, // 906
			AcceptConditions.NotAccept, // 907
			AcceptConditions.Accept, // 908
			AcceptConditions.NotAccept, // 909
			AcceptConditions.Accept, // 910
			AcceptConditions.NotAccept, // 911
			AcceptConditions.Accept, // 912
			AcceptConditions.NotAccept, // 913
			AcceptConditions.NotAccept, // 914
			AcceptConditions.NotAccept, // 915
			AcceptConditions.NotAccept, // 916
			AcceptConditions.NotAccept, // 917
			AcceptConditions.NotAccept, // 918
			AcceptConditions.Accept, // 919
			AcceptConditions.Accept, // 920
			AcceptConditions.Accept, // 921
			AcceptConditions.Accept, // 922
			AcceptConditions.Accept, // 923
			AcceptConditions.Accept, // 924
			AcceptConditions.Accept, // 925
			AcceptConditions.Accept, // 926
			AcceptConditions.Accept, // 927
			AcceptConditions.Accept, // 928
			AcceptConditions.Accept, // 929
			AcceptConditions.Accept, // 930
			AcceptConditions.NotAccept, // 931
			AcceptConditions.NotAccept, // 932
			AcceptConditions.NotAccept, // 933
			AcceptConditions.NotAccept, // 934
			AcceptConditions.NotAccept, // 935
			AcceptConditions.NotAccept, // 936
			AcceptConditions.NotAccept, // 937
			AcceptConditions.NotAccept, // 938
			AcceptConditions.NotAccept, // 939
			AcceptConditions.NotAccept, // 940
			AcceptConditions.NotAccept, // 941
			AcceptConditions.NotAccept, // 942
			AcceptConditions.NotAccept, // 943
			AcceptConditions.NotAccept, // 944
			AcceptConditions.NotAccept, // 945
			AcceptConditions.NotAccept, // 946
			AcceptConditions.Accept, // 947
			AcceptConditions.Accept, // 948
			AcceptConditions.Accept, // 949
			AcceptConditions.Accept, // 950
			AcceptConditions.NotAccept, // 951
			AcceptConditions.Accept, // 952
			AcceptConditions.NotAccept, // 953
			AcceptConditions.Accept, // 954
			AcceptConditions.Accept, // 955
			AcceptConditions.Accept, // 956
			AcceptConditions.Accept, // 957
			AcceptConditions.Accept, // 958
			AcceptConditions.NotAccept, // 959
			AcceptConditions.Accept, // 960
			AcceptConditions.Accept, // 961
			AcceptConditions.NotAccept, // 962
			AcceptConditions.Accept, // 963
			AcceptConditions.Accept, // 964
			AcceptConditions.NotAccept, // 965
			AcceptConditions.Accept, // 966
			AcceptConditions.NotAccept, // 967
			AcceptConditions.Accept, // 968
			AcceptConditions.NotAccept, // 969
			AcceptConditions.Accept, // 970
			AcceptConditions.NotAccept, // 971
			AcceptConditions.NotAccept, // 972
			AcceptConditions.Accept, // 973
			AcceptConditions.NotAccept, // 974
			AcceptConditions.NotAccept, // 975
			AcceptConditions.NotAccept, // 976
			AcceptConditions.Accept, // 977
			AcceptConditions.NotAccept, // 978
			AcceptConditions.Accept, // 979
			AcceptConditions.Accept, // 980
			AcceptConditions.Accept, // 981
			AcceptConditions.Accept, // 982
			AcceptConditions.Accept, // 983
			AcceptConditions.NotAccept, // 984
			AcceptConditions.Accept, // 985
			AcceptConditions.Accept, // 986
			AcceptConditions.Accept, // 987
			AcceptConditions.NotAccept, // 988
			AcceptConditions.Accept, // 989
			AcceptConditions.NotAccept, // 990
			AcceptConditions.Accept, // 991
			AcceptConditions.Accept, // 992
			AcceptConditions.Accept, // 993
			AcceptConditions.NotAccept, // 994
			AcceptConditions.Accept, // 995
			AcceptConditions.Accept, // 996
			AcceptConditions.Accept, // 997
			AcceptConditions.Accept, // 998
			AcceptConditions.Accept, // 999
			AcceptConditions.NotAccept, // 1000
		};
		
		private static int[] colMap = new int[]
		{
			4, 5, 5, 5, 5, 5, 5, 5, 5, 4, 4, 5, 5, 4, 5, 5, 
			5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 
			1, 5, 5, 5, 5, 5, 5, 5, 6, 7, 5, 8, 3, 9, 2, 10, 
			11, 12, 13, 14, 15, 16, 17, 18, 19, 19, 20, 5, 5, 5, 5, 5, 
			21, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 
			5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 22, 
			5, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 
			38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 39, 5, 5, 5, 5, 5, 
			5, 5
		};
		
		private static int[] rowMap = new int[]
		{
			0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 11, 13, 14, 
			15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 2, 27, 2, 28, 
			21, 2, 21, 29, 30, 31, 1, 2, 32, 33, 34, 35, 36, 37, 2, 2, 
			21, 38, 39, 40, 41, 42, 43, 44, 45, 2, 46, 47, 48, 24, 49, 50, 
			51, 52, 53, 54, 55, 56, 57, 2, 58, 59, 60, 61, 2, 2, 2, 62, 
			63, 2, 64, 65, 66, 67, 10, 68, 69, 70, 71, 72, 73, 74, 30, 75, 
			76, 77, 78, 79, 80, 81, 82, 83, 84, 85, 24, 2, 86, 87, 88, 89, 
			90, 91, 92, 93, 2, 94, 95, 96, 97, 98, 99, 100, 101, 102, 103, 104, 
			105, 2, 106, 107, 108, 109, 110, 111, 112, 113, 114, 115, 116, 117, 2, 118, 
			119, 120, 121, 122, 123, 124, 125, 126, 127, 128, 129, 130, 131, 132, 133, 134, 
			135, 2, 136, 137, 138, 139, 140, 141, 142, 143, 144, 145, 146, 147, 148, 149, 
			150, 151, 152, 153, 154, 155, 156, 157, 158, 2, 159, 160, 161, 162, 2, 163, 
			164, 165, 166, 167, 168, 169, 170, 171, 172, 173, 174, 175, 176, 177, 178, 179, 
			19, 180, 181, 182, 183, 184, 41, 185, 186, 187, 188, 189, 190, 191, 192, 193, 
			194, 24, 195, 196, 197, 198, 199, 200, 201, 202, 203, 204, 205, 206, 2, 207, 
			208, 177, 209, 210, 22, 211, 212, 213, 16, 214, 193, 215, 216, 217, 198, 218, 
			219, 220, 221, 222, 223, 224, 225, 226, 227, 228, 229, 230, 231, 232, 233, 234, 
			235, 236, 196, 237, 238, 239, 240, 241, 242, 243, 228, 244, 231, 245, 246, 247, 
			248, 249, 250, 251, 252, 253, 254, 255, 256, 257, 258, 259, 51, 260, 261, 262, 
			263, 264, 265, 266, 267, 268, 269, 270, 271, 272, 273, 274, 275, 276, 277, 278, 
			279, 280, 281, 282, 283, 284, 285, 286, 287, 288, 289, 290, 291, 292, 293, 294, 
			295, 296, 297, 298, 299, 300, 301, 302, 303, 304, 305, 306, 307, 308, 309, 310, 
			311, 312, 313, 314, 315, 316, 317, 318, 319, 320, 321, 322, 323, 324, 325, 326, 
			327, 328, 329, 330, 331, 332, 333, 334, 335, 336, 337, 338, 339, 340, 341, 342, 
			343, 344, 345, 346, 347, 348, 349, 350, 351, 352, 353, 354, 355, 356, 357, 21, 
			358, 359, 360, 361, 362, 363, 364, 365, 366, 367, 368, 369, 370, 371, 372, 373, 
			374, 375, 376, 377, 378, 379, 380, 381, 382, 383, 384, 385, 386, 387, 388, 389, 
			390, 391, 392, 393, 394, 23, 43, 59, 72, 395, 396, 397, 398, 399, 400, 401, 
			402, 403, 404, 405, 406, 407, 408, 409, 410, 411, 412, 413, 414, 415, 416, 417, 
			418, 419, 420, 421, 422, 423, 424, 233, 425, 426, 427, 428, 429, 430, 431, 432, 
			433, 434, 435, 436, 437, 438, 439, 440, 441, 442, 443, 444, 445, 446, 447, 448, 
			449, 450, 451, 452, 453, 454, 455, 456, 457, 458, 459, 460, 461, 462, 463, 464, 
			465, 466, 467, 468, 469, 470, 471, 472, 473, 474, 475, 476, 477, 478, 400, 479, 
			480, 481, 482, 483, 484, 485, 158, 486, 487, 488, 489, 490, 491, 492, 493, 494, 
			495, 496, 497, 498, 499, 500, 501, 502, 503, 504, 505, 506, 507, 508, 509, 510, 
			511, 512, 139, 513, 514, 515, 516, 517, 518, 519, 520, 521, 522, 523, 524, 117, 
			525, 526, 527, 528, 529, 530, 531, 532, 533, 534, 535, 536, 537, 538, 539, 502, 
			540, 513, 541, 542, 543, 544, 545, 546, 547, 548, 549, 550, 551, 552, 553, 554, 
			30, 47, 555, 556, 546, 557, 558, 559, 560, 561, 562, 563, 564, 565, 566, 567, 
			568, 569, 570, 571, 572, 573, 574, 575, 576, 577, 578, 579, 580, 581, 582, 583, 
			584, 585, 586, 587, 588, 589, 590, 591, 592, 593, 594, 595, 596, 597, 598, 599, 
			600, 601, 602, 603, 604, 507, 605, 606, 607, 608, 609, 610, 611, 612, 576, 212, 
			613, 614, 615, 616, 617, 618, 619, 620, 621, 622, 623, 624, 625, 626, 627, 628, 
			629, 630, 631, 632, 633, 634, 203, 635, 636, 637, 447, 638, 639, 640, 641, 642, 
			643, 644, 449, 645, 646, 647, 648, 203, 649, 447, 650, 651, 652, 653, 499, 654, 
			655, 500, 656, 657, 533, 658, 659, 660, 661, 662, 663, 664, 665, 666, 667, 668, 
			669, 670, 671, 672, 673, 674, 675, 676, 677, 678, 679, 680, 191, 681, 682, 683, 
			684, 685, 308, 22, 686, 687, 688, 443, 689, 690, 691, 692, 693, 694, 144, 695, 
			696, 697, 698, 699, 198, 700, 701, 702, 171, 703, 704, 705, 706, 707, 708, 709, 
			710, 711, 712, 713, 714, 676, 715, 679, 716, 717, 718, 719, 720, 721, 722, 723, 
			724, 725, 726, 727, 728, 729, 730, 731, 732, 733, 231, 734, 735, 736, 737, 738, 
			739, 740, 741, 742, 743, 744, 745, 746, 747, 748, 749, 750, 751, 11, 752, 753, 
			754, 755, 756, 757, 758, 759, 760, 761, 762, 763, 764, 765, 766, 767, 768, 769, 
			770, 771, 772, 773, 774, 775, 666, 776, 777, 778, 779, 780, 781, 782, 783, 784, 
			785, 786, 787, 788, 680, 789, 790, 703, 791, 792, 793, 794, 795, 796, 797, 798, 
			799, 800, 801, 802, 803, 804, 805, 806, 807, 808, 809, 810, 811, 812, 813, 814, 
			815, 816, 817, 818, 819, 820, 821, 822, 823, 824, 825, 826, 827, 828, 829, 830, 
			831, 832, 833, 834, 835, 45, 836, 837, 838, 839, 840, 841, 842, 843, 844, 845, 
			846, 847, 848, 849, 850, 851, 852, 853, 854, 855, 856, 857, 858, 859, 860, 861, 
			862, 863, 864, 865, 866, 867, 868, 869, 870, 871, 872, 873, 874, 875, 876, 877, 
			878, 879, 880, 881, 882, 883, 884, 885, 886, 887, 888, 807, 889, 890, 891, 892, 
			893, 894, 895, 896, 897, 21, 898, 899, 900, 901, 902, 903, 904, 905, 906, 907, 
			908, 909, 910, 911, 912, 913, 914, 915, 916
		};
		
		private static int[,] nextState = new int[,]
		{
			{ -1, 1, 39, 39, 39, 2, 40, 2, 62, 62, 2, 80, 96, 642, 738, 768, 768, 768, 768, 768, 2, 111, 2, 3, 623, 623, 731, 879, 765, 623, 623, 41, 785, 623, 896, 798, 808, 815, 995, 623, 623, 820, 824, 623, 628, 828, 880, 998 },
			{ -1, 38, -1, -1, -1, -1, -1, -1, -1, -1, -1, 61, 61, 61, 61, 61, 61, 61, 61, 61, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 81, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 97, 97, 97, 97, 97, 97, 112, 97, 97, 97, 97, 97, 97, 97, 97, 643, 97, 97, 97, 97, 739, 97, 97, 97, 97 },
			{ -1, 300, 300, -1, -1, -1, -1, -1, -1, 300, -1, 9, 9, 9, 9, 9, 9, 9, 9, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 629, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 64, -1, 64, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 5, 5, 5, 5, 5, 5, 5, 5, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 766, 766, 766, -1, -1, -1, -1, -1, -1, -1, 43, 43, 43, 43, 43, 43, 43, 43, 43, -1, -1, -1, -1, -1, -1, 766, -1, -1, -1, 766, -1, -1, -1, -1, -1, 65, -1, -1, -1, 65, 83, 99, -1, -1, -1, -1, -1 },
			{ -1, 323, 324, -1, -1, -1, 325, -1, 326, 875, -1, 44, 44, 44, 44, 44, 44, 44, 44, 44, 327, -1, -1, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 331, 67, 67, 67, 67, 67, 67, 67, 67, 67, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 332, -1, -1, -1, 332, 333, 334, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 854, 854, 854, 854, 854, 854, 854, 854, 854, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, 47, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 81, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248 },
			{ -1, -1, -1, -1, -1, -1, -1, 81, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 248, 248, 248, 794, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248 },
			{ -1, 363, 364, -1, -1, -1, -1, -1, -1, 365, 366, 367, 368, 369, 370, 371, 371, 372, 61, 61, -1, -1, -1, 695, -1, -1, 373, -1, 374, -1, 194, 19, 697, -1, -1, 375, 699, 376, -1, -1, -1, 377, 677, -1, 51, 378, 860, 678 },
			{ -1, 363, 364, -1, -1, -1, -1, -1, -1, 365, 366, 635, 736, 767, 786, 61, 61, 61, 61, 61, -1, -1, -1, 695, -1, -1, 373, -1, 374, -1, 194, 19, 697, -1, -1, 375, 699, 376, -1, -1, -1, 377, 677, -1, 51, 378, 860, 678 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 161, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 81, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 638, 638, 638, 638, 638, 638, 638, 638, 638, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 409, -1, -1, -1, 409, 410, 411, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 631, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 71, -1, 71, -1 },
			{ -1, -1, 360, -1, -1, -1, -1, -1, -1, -1, -1, 670, 670, 670, 670, 670, 670, 670, 670, 670, 360, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 453, 457, -1, -1, -1, 455, -1, 456, 456, -1, 53, 53, 53, 53, 53, 53, 53, 53, 53, 457, -1, -1, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 471, 471, 471, 471, 471, 471, 471, 471, 471, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 277, -1, -1, -1, -1, -1, -1, -1, -1, -1, 61, 25, 25, 25, 25, 25, 25, 25, 25, -1, -1, -1, -1, -1, -1, 279, -1, 281, -1, 194, -1, -1, -1, -1, 283, -1, -1, -1, -1, -1, 285, 677, -1, -1, 242, -1, 678 },
			{ -1, 277, -1, -1, -1, -1, -1, -1, -1, -1, -1, 61, 61, 61, 61, 61, 61, 61, 61, 61, -1, -1, -1, -1, -1, -1, 279, -1, 281, -1, 194, -1, -1, -1, -1, 283, -1, -1, -1, -1, -1, 285, 677, -1, -1, 242, -1, 678 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 30, 30, 30, 30, 30, 30, 30, 30, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 981, 399, 399, 399, 399, 399, 399 },
			{ -1, 277, -1, -1, -1, -1, -1, -1, -1, -1, -1, 61, 61, 61, 61, 61, 61, 61, 61, 61, -1, -1, -1, -1, -1, -1, 279, -1, 281, -1, 194, -1, -1, -1, -1, 283, -1, -1, -1, -1, -1, 285, 546, -1, -1, 242, -1, 678 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 857, 857, 857, 857, 857, 857, 857, 857, 857, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 540, -1, -1, -1, 540, 541, 588, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 77, 77, 77, 77, 77, 77, 77, 77, 77, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 734, 734, 734, 734, 734, 734, 734, 734, 734, 60, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 832, 832, 832, 832, 832, 832, 832, 832, 832, 832, 832, 832, 832, 832, 832, 832, 832, 832, 832, 832, 832, 832, 832, 832, 832 },
			{ -1, 275, 275, -1, -1, -1, -1, 81, -1, 275, -1, 6, 6, 6, 624, 732, 732, 732, 732, 732, -1, -1, -1, 97, 97, 97, 97, 97, 97, 97, 97, 135, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 146, 97, 146, 97 },
			{ -1, 300, 300, -1, -1, -1, -1, -1, -1, 300, -1, 9, 9, 9, 9, 9, 9, 9, 9, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 858, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 766, 126, 766, -1, -1, -1, -1, -1, -1, -1, 137, 137, 148, 159, 159, 159, 159, 159, 159, 360, -1, -1, -1, -1, -1, 766, -1, -1, -1, 766, -1, -1, -1, -1, -1, 65, -1, -1, -1, 65, 83, 99, -1, -1, -1, -1, -1 },
			{ -1, 323, 324, -1, -1, -1, 325, -1, 326, 875, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 327, -1, -1, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 331, 67, 67, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 332, -1, -1, -1, 332, 333, 334, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 698, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 638, 638, 755, 755, 755, 755, 755, 755, 755, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 409, -1, -1, -1, 409, 410, 411, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 860, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 670, 670, 670, 670, 670, 670, 670, 670, 670, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 453, 457, -1, -1, -1, 455, -1, 456, 456, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 457, -1, -1, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72 },
			{ -1, 277, -1, -1, -1, -1, -1, -1, -1, -1, -1, 55, 74, 74, 92, 25, 25, 25, 25, 25, -1, -1, -1, -1, -1, -1, 279, -1, 281, -1, 194, -1, -1, -1, -1, 283, -1, -1, -1, -1, -1, 285, 677, -1, -1, 242, -1, 678 },
			{ -1, 277, -1, -1, -1, -1, -1, -1, -1, -1, -1, 61, 29, 29, 29, 29, 29, 29, 29, 29, -1, -1, -1, -1, -1, -1, 279, -1, 281, -1, 194, -1, -1, -1, -1, 283, -1, -1, -1, -1, -1, 285, 677, -1, -1, 242, -1, 678 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 399, 399, 399, 849, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 76, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 77, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 634, 634, 634, 634, 634, 634, 78, 78, 78, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 38, -1, -1, -1, -1, -1, -1, -1, -1, -1, 63, 63, 636, 737, 737, 737, 737, 737, 737, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 277, -1, -1, -1, -1, -1, -1, -1, -1, -1, 737, 737, 737, 737, 737, 737, 737, 737, 737, 213, -1, -1, -1, -1, -1, 279, -1, 281, -1, 194, -1, -1, -1, -1, 283, -1, -1, -1, -1, -1, 285, 677, -1, -1, 242, -1, 678 },
			{ -1, 300, 300, -1, -1, -1, -1, -1, -1, 300, -1, 9, 9, 9, 9, 9, 9, 9, 9, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 766, 766, 766, -1, -1, -1, -1, -1, -1, -1, 770, 770, 788, 800, 800, 800, 800, 800, 800, -1, -1, -1, -1, -1, -1, 170, -1, -1, -1, 766, -1, -1, -1, -1, -1, 766, -1, -1, -1, 766, 766, 766, -1, -1, -1, -1, -1 },
			{ -1, 323, 327, -1, -1, -1, 325, -1, 326, 326, -1, 630, 630, 630, 630, 630, 630, 630, 630, 630, 327, -1, -1, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 331, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 332, -1, -1, -1, 332, 333, 334, -1, -1, -1, -1, -1 },
			{ -1, -1, 47, -1, -1, -1, -1, 190, -1, -1, 408, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 408, 663, 663, 663, 663, 663, 663, 663, 663, 663, 663, 663, 663, 663, 663, 663, 663, 663, 663, 663, 663, 663, 663, 663, 663, 663 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 161, -1, 385, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 755, 755, 755, 755, 755, 755, 755, 755, 755, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 409, -1, -1, -1, 409, 410, 411, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 142, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 153, 153, 153, 153, 153, 153, 153, 153, 153, 153, 153, 153, 153, 153, 153, 153, 153, 153, 153, 153, 153, 153, 153, 153, 153 },
			{ -1, 277, -1, -1, -1, -1, -1, -1, -1, -1, -1, 25, 25, 25, 25, 25, 25, 25, 25, 25, -1, -1, -1, -1, -1, -1, 279, -1, 281, -1, 194, -1, -1, -1, -1, 283, -1, -1, -1, -1, -1, 285, 677, -1, -1, 242, -1, 678 },
			{ -1, 277, -1, -1, -1, -1, -1, -1, -1, -1, -1, 29, 29, 29, 29, 29, 29, 29, 29, 29, -1, -1, -1, -1, -1, -1, 279, -1, 281, -1, 194, -1, -1, -1, -1, 283, -1, -1, -1, -1, -1, 285, 677, -1, -1, 242, -1, 678 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 185, -1, -1, -1, -1, -1, -1 },
			{ -1, 79, 286, -1, -1, -1, -1, -1, -1, 286, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 167, -1, -1, 177, -1, 187, -1, 194, 4, 200, -1, -1, 206, 662, 218, -1, -1, -1, 685, 677, -1, 42, 242, 858, 678 },
			{ -1, 79, 95, -1, -1, -1, -1, -1, -1, 110, 123, 134, 145, 145, 145, 145, 145, 145, 145, 145, 156, -1, -1, 167, -1, -1, 177, -1, 187, -1, 194, 4, 200, -1, -1, 206, 212, 218, -1, -1, 224, 230, 236, -1, 42, 242, 858, 678 },
			{ -1, 300, 300, -1, -1, -1, -1, -1, -1, 300, -1, 9, 9, 9, 9, 9, 9, 9, 9, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 379, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 766, 766, 766, -1, -1, -1, -1, -1, -1, -1, 770, 770, 788, 800, 800, 800, 800, 800, 800, -1, -1, -1, -1, -1, -1, 766, -1, -1, -1, 766, -1, -1, -1, -1, -1, 766, -1, -1, -1, 766, 766, 170, -1, -1, -1, -1, -1 },
			{ -1, 323, 688, -1, -1, -1, 325, -1, 326, 875, -1, 115, 115, 115, 115, 115, 115, 115, 115, 115, 689, -1, -1, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 863, 863, 863, 863, 863, 863, 863, 863, 863, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 698, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 386, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 118, 118, 118, 118, 118, 118, 118, 118, 118, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 409, -1, -1, -1, 409, 410, 411, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 521, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 530, 457, -1, -1, -1, 531, -1, 532, 532, -1, 652, 652, 652, 652, 652, 652, 652, 652, 652, 457, -1, -1, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193 },
			{ -1, 277, -1, -1, -1, -1, -1, -1, -1, -1, -1, 25, 25, 25, 25, 25, 25, 25, 61, 61, -1, -1, -1, -1, -1, -1, 279, -1, 281, -1, 194, -1, -1, -1, -1, 283, -1, -1, -1, -1, -1, 285, 677, -1, -1, 242, -1, 678 },
			{ -1, 277, -1, -1, -1, -1, -1, -1, -1, -1, -1, 29, 29, 61, 61, 61, 61, 61, 61, 61, -1, -1, -1, -1, -1, -1, 279, -1, 281, -1, 194, -1, -1, -1, -1, 283, -1, -1, -1, -1, -1, 285, 677, -1, -1, 242, -1, 678 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 724, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 286, 286, -1, -1, -1, -1, -1, -1, 286, -1, 7, 853, 884, 884, 884, 884, 44, 44, 44, -1, -1, -1, 167, -1, -1, 751, -1, 779, -1, -1, 4, 200, -1, -1, 684, 662, 218, -1, -1, -1, 793, -1, -1, 42, -1, 858, -1 },
			{ -1, 247, 251, -1, -1, -1, -1, -1, -1, 110, 123, 145, 145, 145, 255, 255, 255, 255, 255, 255, 259, -1, -1, 263, -1, -1, 177, -1, 187, -1, 194, 4, 200, -1, -1, 206, 212, 218, 266, -1, 224, 230, 236, -1, 42, 242, 858, 678 },
			{ -1, -1, -1, -1, -1, -1, -1, 81, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829 },
			{ -1, 300, 300, -1, -1, -1, -1, -1, -1, 300, -1, 9, 9, 9, 9, 9, 9, 9, 9, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 380, -1, -1, -1, -1 },
			{ -1, 766, 766, 766, -1, -1, -1, -1, -1, -1, -1, 770, 770, 788, 800, 800, 800, 800, 800, 800, -1, -1, -1, -1, -1, -1, 766, -1, -1, -1, 170, -1, -1, -1, -1, -1, 766, -1, -1, -1, 766, 766, 766, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 190, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 197, 197, 197, 197, 197, 197, 197, 197, 197, 197, 197, 197, 197, 197, 197, 197, 197, 197, 197, 197, 197, 197, 197, 197, 197 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 331, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 389, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 118, 118, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 409, -1, -1, -1, 409, 410, 411, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 522, -1, -1, -1, -1 },
			{ -1, 530, 457, -1, -1, -1, 531, -1, 532, 532, -1, 661, -1, -1, -1, -1, -1, -1, -1, -1, 457, -1, -1, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 185, -1, 559, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 133, 133, 133, 133, 133, 133, 133, 133, 133, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 286, 286, -1, -1, -1, -1, -1, -1, 286, -1, 287, 288, 289, 289, 289, 289, 289, 289, 289, -1, -1, -1, 974, -1, -1, 944, -1, 945, -1, -1, 4, 975, -1, -1, 943, 942, 976, -1, -1, -1, 946, -1, -1, 42, -1, 858, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 273, -1, -1, 5, 5, 5, 5, 5, 5, 5, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 81, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 11, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829 },
			{ -1, 300, 300, -1, -1, -1, -1, -1, -1, 300, -1, 9, 9, 9, 9, 9, 9, 9, 9, 9, -1, -1, -1, -1, -1, -1, -1, 381, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, 360, -1, -1, -1, -1, -1, -1, -1, -1, 657, 657, 657, 657, 657, 180, 180, 180, 180, 360, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 392, 688, -1, -1, -1, 325, -1, 326, 875, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 689, -1, -1, 203, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 203, 100, 100, 100, 100, 100, 100, 100, 100, 100 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 390, -1, -1, -1, 161, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 409, -1, -1, -1, 409, 410, 411, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 523, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 530, 457, -1, -1, -1, 531, -1, 532, 532, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 457, -1, -1, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 724, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 560, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 133, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 8, 8, 8, 45, 67, 67, 67, 67, 67, -1, -1, -1, 659, -1, -1, 803, -1, 811, -1, -1, -1, 666, -1, -1, 753, 745, 672, -1, -1, -1, 817, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 81, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 829, 829, 829, 829, 829, 829, 829, 829, 12, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829 },
			{ -1, 300, 300, -1, -1, -1, -1, -1, -1, 300, -1, 9, 9, 9, 9, 9, 9, 9, 9, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 382, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 766, 766, 766, -1, -1, -1, -1, -1, -1, -1, 220, 220, 870, 891, 891, 891, 226, 226, 226, -1, -1, -1, -1, -1, -1, 766, -1, -1, -1, 766, -1, -1, -1, -1, -1, 766, -1, -1, -1, 766, 766, 766, -1, -1, -1, -1, -1 },
			{ -1, 404, 405, -1, -1, -1, 406, -1, 407, 407, -1, 209, 209, 209, 209, 209, 209, 209, 209, 209, -1, -1, -1, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 701, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 720, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 742, 742, 742, 742, 742, 742, 742, 742, 742, 205, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 725, -1, -1, -1, -1 },
			{ -1, -1, 620, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 79, 95, -1, -1, -1, -1, -1, -1, 110, 123, 290, 290, 290, 290, 290, 290, 291, 291, 291, 156, -1, -1, 167, -1, -1, 177, -1, 187, -1, 194, 4, 200, -1, -1, 206, 212, 218, -1, -1, 224, 230, 236, -1, 42, 242, 858, 678 },
			{ -1, 275, 275, -1, -1, -1, -1, 81, -1, 275, 320, 6, 6, 6, 624, 732, 732, 732, 732, 732, -1, -1, 320, 829, 829, 829, 829, 829, 829, 829, 829, 675, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829 },
			{ -1, 300, 300, -1, -1, -1, -1, -1, -1, 300, -1, 9, 9, 9, 9, 9, 9, 9, 9, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 64 },
			{ -1, -1, 360, -1, -1, -1, -1, -1, -1, -1, -1, 20, 20, 20, 20, 20, 20, 20, 20, 20, 360, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 404, 405, -1, -1, -1, 406, -1, 407, 407, -1, 209, 23, 23, 23, 23, 23, 23, 23, 23, -1, -1, -1, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 391, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 650, 650, 650, 650, 650, 650, 650, 650, 650, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 540, -1, -1, -1, 540, 541, 542, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 71 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 561, -1, -1, -1, 185, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, 622, 622, -1, 144, 144, 144, 144, 144, 144, 144, 144, 144, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 247, 251, -1, -1, -1, -1, -1, -1, 110, 123, 290, 290, 290, 290, 290, 290, 291, 291, 291, 259, -1, -1, 263, -1, -1, 177, -1, 187, -1, 194, 4, 200, -1, -1, 206, 212, 218, 266, -1, 224, 230, 236, -1, 42, 242, 858, 678 },
			{ -1, 275, 275, -1, -1, -1, -1, 81, -1, 275, 320, 6, 6, 6, 624, 732, 732, 732, 732, 732, -1, -1, 320, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829 },
			{ -1, 300, 300, -1, -1, -1, -1, -1, -1, 300, -1, 9, 9, 9, 9, 9, 9, 9, 9, 9, -1, -1, -1, -1, -1, -1, -1, 64, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, 360, -1, -1, -1, -1, -1, -1, -1, -1, 20, 20, 20, 20, 20, 52, 52, 52, 52, 360, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 404, 405, -1, -1, -1, 406, -1, 407, 407, -1, 23, 23, 23, 23, 23, 23, 23, 23, 23, -1, -1, -1, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 782, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 650, 650, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 540, -1, -1, -1, 540, 541, 542, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 71, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 142, -1, -1, 533, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 533, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656, 656 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 562, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 176, 176, 176, 176, 176, 176, 176, 176, 176, 186, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 66, 66, 66, 66, 66, 66, 630, 630, 630, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 81, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 12, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829 },
			{ -1, 300, 300, -1, -1, -1, -1, -1, -1, 300, -1, 9, 9, 9, 9, 9, 9, 9, 9, 9, -1, -1, -1, -1, -1, 384, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, 360, -1, -1, -1, -1, -1, -1, -1, -1, 52, 52, 52, 52, 52, 52, 52, 52, 52, 360, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 744, 744, 744, 744, 744, 744, 744, 744, 744, 227, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 650, 35, 35, 35, 35, 35, 35, 35, 35, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 540, -1, -1, -1, 540, 541, 542, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 524, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 534, -1, -1, -1, -1, 722, -1, 723, 723, -1, 661, 661, 661, 661, 661, 661, 661, 661, 661, -1, -1, -1, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 563, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 176, 176, 176, 176, 176, 94, 77, 77, 77, 186, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 292, -1, -1, -1, -1, 293, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 81, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 237, 829, 829, 829, 829, 829, 829, 892, 13, 829, 829 },
			{ -1, 300, 300, -1, -1, -1, -1, -1, -1, 300, -1, 9, 9, 9, 9, 9, 9, 9, 9, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 388, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 766, 766, 766, -1, -1, -1, -1, -1, -1, -1, 770, 770, 788, 800, 800, 800, 800, 800, 800, -1, -1, -1, -1, -1, -1, 766, -1, -1, -1, 766, -1, -1, -1, -1, -1, 766, -1, -1, -1, 766, 766, 766, -1, -1, -1, -1, -1 },
			{ -1, 404, 405, -1, -1, -1, 406, -1, 407, 407, -1, 181, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 35, 35, 35, 35, 35, 35, 35, 35, 35, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 540, -1, -1, -1, 540, 541, 542, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 718, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 534, -1, -1, -1, -1, 722, -1, 723, 723, -1, 661, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 726, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 94, 94, 94, 94, 94, 94, 77, 77, 77, 186, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 294, -1, -1, -1, 295, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 81, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 855, 829, 829, 829, 829, 829 },
			{ -1, 300, 300, -1, -1, -1, -1, -1, -1, 300, -1, 9, 9, 9, 9, 9, 9, 9, 9, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 647, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 632, 632, 632, 632, 632, 632, 632, 632, 632, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 404, 405, -1, -1, -1, 406, -1, 407, 407, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 35, 35, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 540, -1, -1, -1, 540, 541, 542, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 651, -1, -1, -1, -1, -1 },
			{ -1, 534, -1, -1, -1, -1, 722, -1, 723, 723, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 94, 94, 94, 94, 94, 94, 77, 77, 77, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 296, -1, -1, -1, -1, -1, -1, -1, -1, -1, 297, -1, -1, 298, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 81, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 829, 829, 829, 829, 829, 829, 829, 829, 843, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 948, 829, 829, 829, 829 },
			{ -1, 766, 766, 766, -1, -1, -1, -1, -1, -1, -1, 43, 232, 232, 232, 232, 232, 232, 232, 232, -1, -1, -1, -1, -1, -1, 766, -1, -1, -1, 766, -1, -1, -1, -1, -1, 65, -1, -1, -1, 65, 83, 99, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 587, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 498, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 521, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 142, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 743, 743, 743, 743, 743, 743, 743, 743, 743, 743, 743, 743, 743, 743, 743, 743, 743, 743, 743, 743, 743, 743, 743, 743, 743 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 299, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 81, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 829, 829, 829, 829, 886, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829 },
			{ -1, 766, 766, 766, -1, -1, -1, -1, -1, -1, -1, 232, 232, 232, 232, 232, 232, 232, 232, 232, -1, -1, -1, -1, -1, -1, 766, -1, -1, -1, 766, -1, -1, -1, -1, -1, 65, -1, -1, -1, 65, 83, 99, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 190, -1, -1, 408, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 408, 663, 663, 663, 663, 663, 663, 663, 663, 663, 663, 663, 663, 663, 663, 663, 663, 663, 663, 663, 663, 663, 663, 663, 663, 663 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 498, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 522, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 142, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 665, 665, 665, 665, 665, 665, 665, 665, 665, 665, 665, 665, 665, 665, 665, 665, 665, 665, 665, 665, 665, 665, 665, 665, 665 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 301, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 302, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 81, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 829, 829, 829, 955, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829 },
			{ -1, 766, 766, 766, -1, -1, -1, -1, -1, -1, -1, 232, 232, 114, 648, 648, 648, 648, 648, 648, -1, -1, -1, -1, -1, -1, 766, -1, -1, -1, 766, -1, -1, -1, -1, -1, 65, -1, -1, -1, 65, 83, 99, -1, -1, -1, -1, -1 },
			{ -1, -1, 450, -1, -1, -1, -1, 190, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 197, 197, 197, 197, 197, 197, 197, 197, 197, 197, 197, 197, 68, 197, 197, 197, 197, 197, 197, 197, 197, 197, 197, 197, 197 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 498, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 523, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 211, 211, 211, 211, 211, 211, 142, 142, 142, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 303, -1, -1, -1, -1, -1, -1, -1, 304, -1, -1, -1, -1, -1, 305, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 81, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 669, 669, 669, 669, 669, 669, 669, 669, 669, 669, 669, 669, 669, 669, 669, 669, 669, 669, 669, 669, 669, 669, 669, 669, 669 },
			{ -1, 404, 405, -1, -1, -1, 406, -1, 407, 407, -1, 471, 471, 471, 471, 471, 471, 471, 471, 471, -1, -1, -1, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 498, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 720, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 142, 142, 142, 142, 142, 142, 142, 142, 142, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 306, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 307, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 243, 243, 243, 243, 243, 243, 81, 81, 81, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 190, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 746, 746, 746, 746, 746, 746, 746, 746, 746, 746, 746, 746, 746, 746, 746, 746, 746, 746, 746, 746, 746, 746, 746, 746, 746 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 498, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 71 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 229, 229, 229, 229, 229, 229, 229, 229, 229, 241, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 308, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 277, -1, -1, -1, -1, -1, -1, -1, -1, -1, 225, 225, 225, 225, 225, 225, 225, 225, 225, -1, -1, -1, -1, -1, -1, 279, -1, 281, -1, 194, -1, -1, -1, -1, 283, -1, -1, -1, -1, -1, 285, 677, -1, -1, 242, -1, 678 },
			{ -1, 453, 454, -1, -1, -1, 455, -1, 456, 456, -1, 674, 674, 674, 674, 674, 674, 674, 674, 674, 454, -1, -1, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72 },
			{ -1, -1, -1, -1, -1, -1, -1, 190, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 673, 673, 673, 673, 673, 673, 673, 673, 673, 673, 673, 673, 673, 673, 673, 673, 673, 673, 673, 673, 673, 673, 673, 673, 673 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 498, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 71, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 772, 772, 772, 772, 772, 211, 142, 142, 142, 241, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 306, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 453, 454, -1, -1, -1, 455, -1, 456, 456, -1, 214, 214, 214, 214, 214, 214, 214, 214, 214, 454, -1, -1, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 233, 233, 233, 233, 233, 233, 190, 190, 190, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 498, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 524, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 211, 211, 211, 211, 211, 211, 142, 142, 142, 241, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 309, -1, -1, -1, 310, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 306, 311, -1, -1, -1, -1 },
			{ -1, 275, 275, -1, -1, -1, -1, 81, -1, 359, 320, 6, 6, 6, 624, 732, 732, 732, 732, 732, -1, -1, 320, 248, 248, 248, 248, 248, 248, 248, 248, 679, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248 },
			{ -1, 766, 126, 766, -1, -1, -1, -1, -1, 499, -1, 137, 137, 148, 159, 159, 159, 159, 159, 159, 360, -1, -1, -1, -1, -1, 766, -1, -1, -1, 766, -1, -1, -1, -1, -1, 65, -1, -1, -1, 65, 83, 99, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 190, 190, 190, 190, 190, 190, 190, 190, 190, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 498, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 142, -1, -1, 533, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 533, 579, 579, 579, 579, 579, 579, 579, 579, 579, 579, 579, 579, 579, 579, 579, 579, 579, 579, 579, 579, 579, 579, 579, 579, 579 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 313, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 81, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 17, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 270, 270, 270, 270, 270, 270, 270, 270, 270, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 498, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 718, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 314, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 81, 81, 81, 81, 81, 81, 81, 81, 81, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 498, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 668, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 254, 254, 254, 254, 254, 254, 254, 254, 254, 241, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 247, 286, -1, -1, -1, -1, -1, -1, 286, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 263, -1, -1, 177, -1, 187, -1, 194, 4, 200, -1, -1, 206, 662, 218, 266, -1, -1, 685, 677, -1, 42, 242, 858, 678 },
			{ -1, 497, 405, -1, -1, -1, 406, -1, 407, 407, -1, 471, 471, 471, 471, 471, 471, 471, 471, 471, -1, -1, -1, 667, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 667, 215, 215, 215, 215, 215, 215, 215, 215, 215 },
			{ -1, 286, 286, -1, -1, -1, -1, -1, -1, 286, -1, 84, 862, 889, 889, 889, 889, 859, 859, 859, -1, -1, -1, 167, -1, -1, 751, -1, 779, -1, -1, 4, 200, -1, -1, 684, 662, 218, -1, -1, -1, 793, -1, -1, 42, -1, 858, -1 },
			{ -1, 275, 275, -1, -1, -1, -1, 81, -1, 275, 320, 6, 6, 6, 624, 732, 732, 732, 732, 732, -1, -1, 320, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399 },
			{ -1, 497, 405, -1, -1, -1, 406, -1, 407, 407, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 667, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 667, 215, 215, 215, 215, 215, 215, 215, 215, 215 },
			{ -1, 79, 95, -1, -1, -1, -1, -1, -1, 110, 316, 290, 290, 290, 290, 290, 290, 291, 291, 291, 156, -1, -1, 167, -1, -1, 177, -1, 187, -1, 194, 4, 200, -1, -1, 206, 212, 218, -1, -1, 224, 230, 236, -1, 42, 242, 858, 678 },
			{ -1, 403, -1, -1, -1, -1, -1, 81, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 399, 399, 399, 783, 399, 796, 399, 895, 399, 399, 399, 399, 806, 399, 399, 399, 399, 399, 814, 819, 399, 399, 823, 399, 903 },
			{ -1, 516, -1, -1, -1, -1, 708, -1, 517, 517, -1, 257, 257, 257, 257, 257, 257, 257, 257, 257, -1, -1, -1, 869, 869, 869, 869, 869, 869, 869, 869, 869, 869, 869, 869, 869, 869, 869, 869, 869, 869, 869, 869, 869, 869, 869, 869, 869 },
			{ -1, -1, -1, -1, -1, -1, -1, 142, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 640, 640, 640, 640, 640, 640, 735, 735, 735, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 81, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 22 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 268, 268, 268, 268, 268, 268, 268, 268, 268, 274, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 533, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 533, 262, 262, 262, 262, 262, 262, 262, 262, 262, 262, 262, 262, 262, 262, 262, 262, 262, 262, 262, 262, 262, 262, 262, 262, 262 },
			{ -1, -1, 317, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 10, -1, -1, 292, -1, -1, -1, -1, 293, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 81, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 810, 810, 810, 810, 810, 233, 190, 190, 190, 274, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, 317, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 267, 267, 267, 267, 267, 267, 267, 267, 267, 267, 267, 267, 267, 267, 267, 267, 267, 267, 267, 267, 267, 267, 267, 267, 267 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 233, 233, 233, 233, 233, 233, 190, 190, 190, 274, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 79, 318, -1, -1, -1, -1, -1, -1, 110, 316, 291, 291, 291, 291, 291, 291, 291, 291, 291, -1, -1, -1, 167, -1, -1, 177, -1, 187, -1, 194, 4, 200, -1, -1, 206, 212, 218, -1, -1, 224, 230, 236, -1, 42, 242, 858, 678 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 28, 28, 28, 28, 28, 28, 28, 28, 28, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 277, -1, -1, -1, -1, -1, -1, -1, 319, -1, 291, 291, 291, 291, 291, 291, 291, 291, 291, -1, -1, -1, -1, -1, -1, 279, -1, 281, -1, 194, -1, -1, -1, -1, 283, -1, -1, -1, -1, -1, 285, 677, -1, -1, 242, -1, 678 },
			{ -1, -1, -1, -1, -1, -1, -1, 190, -1, -1, 408, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 408, 519, 519, 519, 519, 519, 519, 519, 519, 519, 519, 519, 519, 519, 519, 519, 519, 519, 519, 519, 519, 519, 519, 519, 519, 519 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 5, 5, 5, 5, 5, 5, 5, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 275, 275, -1, -1, -1, -1, -1, -1, 275, -1, 6, 6, 6, 624, 732, 732, 732, 732, 732, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 190, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 277, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 279, -1, 281, -1, 194, -1, -1, -1, -1, 283, -1, -1, -1, -1, -1, 285, 677, -1, -1, 242, -1, 678 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 408, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 408, 278, 278, 278, 278, 278, 278, 278, 278, 278, 278, 278, 278, 278, 278, 278, 278, 278, 278, 278, 278, 278, 278, 278, 278, 278 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 294, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 284, 284, 284, 284, 284, 284, 284, 284, 284, 274, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 297, -1, -1, 298, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 304, -1, -1, -1, -1, -1, 305, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 309, -1, -1, -1, 321, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 311, -1, -1, -1, -1 },
			{ -1, 286, 286, -1, -1, -1, -1, -1, -1, 286, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 167, -1, -1, 751, -1, 779, -1, -1, 4, 200, -1, -1, 684, 662, 218, -1, -1, -1, 793, -1, -1, 42, -1, 858, -1 },
			{ -1, -1, 329, -1, -1, -1, -1, -1, -1, 330, -1, 289, 289, 289, 289, 289, 289, 289, 289, 289, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, 329, -1, -1, -1, -1, -1, -1, 330, -1, 289, 289, 289, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, 329, -1, -1, -1, -1, -1, -1, 330, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 277, -1, -1, -1, -1, -1, -1, -1, 319, -1, 14, 14, 14, 14, 14, 14, 14, 14, 14, -1, -1, -1, -1, -1, -1, 279, -1, 281, -1, 194, -1, -1, -1, -1, 283, -1, -1, -1, -1, -1, 285, 677, -1, -1, 242, -1, 678 },
			{ -1, 277, -1, -1, -1, -1, -1, -1, -1, 319, -1, 15, 15, 15, 15, 15, 15, 15, 15, 15, -1, -1, -1, -1, -1, -1, 279, -1, 281, -1, 194, -1, -1, -1, -1, 283, -1, -1, -1, -1, -1, 285, 677, -1, -1, 242, -1, 678 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 82, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 98, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 16 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 113, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 125, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 692, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 49, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 344, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 644, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 136, -1, 147, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 158, -1, -1, -1, -1, -1, -1, 64 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 69, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 87, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 286, 318, -1, -1, -1, -1, -1, -1, 345, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 167, -1, -1, 751, -1, 779, -1, -1, 4, 200, -1, -1, 684, 662, 218, -1, -1, -1, 793, -1, -1, 42, -1, 858, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 113, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 169, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 102, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 117, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 179, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 49, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 286, 318, -1, -1, -1, -1, -1, -1, 345, 316, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 167, -1, -1, 751, -1, 779, -1, -1, 4, 200, -1, -1, 684, 662, 218, -1, -1, -1, 793, -1, 128, 42, -1, 858, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 139, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 150, 346, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 344, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 659, -1, -1, 803, -1, 811, -1, -1, -1, 666, -1, -1, 753, 745, 672, -1, -1, -1, 817, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 286, 286, -1, -1, -1, -1, -1, -1, 286, -1, 347, 348, 349, 349, 349, 349, 349, 349, 349, -1, -1, -1, 167, -1, -1, 751, -1, 779, -1, -1, 4, 200, -1, -1, 684, 662, 218, -1, -1, -1, 793, -1, -1, 42, -1, 858, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 350, 351, 352, 352, 352, 352, 352, 352, 352, -1, -1, -1, 353, -1, -1, 354, -1, 696, -1, -1, -1, 355, -1, -1, 356, 357, 691, -1, -1, -1, 756, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 358, 358, 358, 358, 358, 358, 358, 358, 358, 358, 358, 358, 358, 358, 358, 358, 358, 358, 358, 358, 358, 358, 358, 358, 358 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 117, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 128, -1, -1, -1, -1 },
			{ -1, 323, -1, -1, -1, -1, 325, -1, 326, 326, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 127, 127, 127, 127, 127, 127, 138, 149, 149, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 645, 645, 645, 645, 645, 645, 645, 645, 645, 645, 645, 645, 645, 645, 645, 645, 645, 645, 645, 645, 645, 645, 645, 645, 645 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 160, 160, 658, 744, 744, 744, 744, 744, 744, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 653, 653, 653, 653, 653, 653, 171, 181, 181, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 179, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 876, 876, 876, 876, 876, 876, 876, 876, 876, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 18, 18, 18, 50, 70, 70, 70, 70, 70, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 85, 85, 85, 85, 85, 85, 85, 85, 85, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 101, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 101, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 101, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 361, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 361, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 361, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 361, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 361, -1, 361, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 361, -1, -1, -1, -1, -1, -1, 361 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 361, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 361, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 362, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 16, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 286, 286, -1, -1, -1, -1, -1, -1, 286, -1, 387, 693, 686, 686, 686, 686, 686, 686, 686, -1, -1, -1, 167, -1, -1, 751, -1, 779, -1, -1, 4, 200, -1, -1, 684, 662, 218, -1, -1, -1, 793, -1, -1, 42, -1, 858, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 16, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, 690, -1, -1, -1, -1, -1, -1, 329, -1, 349, 349, 349, 349, 349, 349, 349, 349, 349, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, 690, -1, -1, -1, -1, -1, -1, 329, -1, 754, 754, 754, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, 690, -1, -1, -1, -1, -1, -1, 329, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 393, -1, 352, 352, 352, 352, 352, 352, 352, 352, 352, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 393, -1, 352, 352, 352, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 393, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 704, -1, -1, -1, -1, 394, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 700, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 705, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 395, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 396, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 397, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 267, 267, 267, 267, 267, 267, 267, 267, 267, 267, 267, 267, 267, 267, 267, 267, 267, 267, 267, 267, 267, 267, 267, 267, 267 },
			{ -1, 275, 275, -1, -1, -1, -1, -1, -1, 275, -1, 189, 196, 196, 202, 732, 732, 732, 732, 732, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 21, 21, 21, 21, 21, 21, 53, 53, 53, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 894, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 894, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 361, -1, -1, -1, -1, -1 },
			{ -1, 363, 412, -1, -1, -1, -1, -1, -1, 412, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 695, -1, -1, 373, -1, 374, -1, 194, 19, 697, -1, -1, 375, 699, 376, -1, -1, -1, 377, 677, -1, 51, 242, 860, 678 },
			{ -1, 412, 412, -1, -1, -1, -1, -1, -1, 412, -1, 781, 795, 795, 413, -1, -1, -1, -1, -1, -1, -1, -1, 695, -1, -1, 414, -1, 709, -1, -1, 19, 697, -1, -1, 415, 699, 376, -1, -1, -1, 762, -1, -1, 51, -1, 860, -1 },
			{ -1, 412, 412, -1, -1, -1, -1, -1, -1, 412, -1, 416, 417, 418, 418, 418, 418, 418, 418, 418, -1, -1, -1, 419, -1, -1, 784, -1, 797, -1, -1, 19, 420, -1, -1, 710, 421, 758, -1, -1, -1, 807, -1, -1, 51, -1, 860, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 422, 423, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 277, -1, -1, -1, -1, -1, -1, -1, -1, -1, 24, 54, 54, 54, 54, 54, 54, 54, 54, -1, -1, -1, -1, -1, -1, 279, -1, 281, -1, 194, -1, -1, -1, -1, 283, -1, -1, -1, -1, -1, 285, 677, -1, -1, 242, -1, 678 },
			{ -1, 277, -1, -1, -1, -1, -1, -1, -1, -1, -1, 54, 54, 54, 73, 73, 73, 73, 73, 73, -1, -1, -1, -1, -1, -1, 279, -1, 281, -1, 194, -1, -1, -1, -1, 283, -1, -1, -1, -1, -1, 285, 677, -1, -1, 242, -1, 678 },
			{ -1, 277, -1, -1, -1, -1, -1, -1, -1, -1, -1, 73, 73, 73, 73, 73, 73, 73, 73, 73, -1, -1, -1, -1, -1, -1, 279, -1, 281, -1, 194, -1, -1, -1, -1, 283, -1, -1, -1, -1, -1, 285, 677, -1, -1, 242, -1, 678 },
			{ -1, 277, -1, -1, -1, -1, -1, -1, -1, -1, -1, 73, 73, 73, 73, 73, 73, 91, 106, 106, -1, -1, -1, -1, -1, -1, 279, -1, 281, -1, 194, -1, -1, -1, -1, 283, -1, -1, -1, -1, -1, 285, 677, -1, -1, 242, -1, 678 },
			{ -1, 277, -1, -1, -1, -1, -1, -1, -1, -1, -1, 106, 106, 106, 106, 106, 106, 106, 106, 106, -1, -1, -1, -1, -1, -1, 279, -1, 281, -1, 194, -1, -1, -1, -1, 283, -1, -1, -1, -1, -1, 285, 677, -1, -1, 242, -1, 678 },
			{ -1, 277, -1, -1, -1, -1, -1, -1, -1, -1, -1, 106, 61, 61, 61, 61, 61, 61, 61, 61, -1, -1, -1, -1, -1, -1, 279, -1, 281, -1, 194, -1, -1, -1, -1, 283, -1, -1, -1, -1, -1, 285, 677, -1, -1, 242, -1, 678 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 294, -1, -1, -1, 426, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 427, -1, -1, -1, -1, -1, -1, -1, -1, -1, 297, -1, -1, 298, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 430, -1, -1, -1, -1, -1, -1, -1, 304, -1, -1, -1, -1, -1, 305, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 432, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 309, -1, -1, -1, 433, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 311, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 434, 435, 435, 435, 435, 436, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 314, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 64, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 441, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 388, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 442, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 760, -1, -1, -1, -1, -1, 443, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 64, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 445, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 16, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, 329, -1, -1, -1, -1, -1, -1, 329, -1, 686, 686, 686, 686, 686, 686, 686, 686, 686, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 446, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 706, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 447, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 713, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 392, -1, -1, -1, -1, 325, -1, 326, 326, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 203, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 203, 100, 100, 100, 100, 100, 100, 100, 100, 100 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 88, 88, 88, 103, 118, 118, 118, 118, 118, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 711, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 711, -1, 711, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 711, -1, -1, -1, -1, -1, -1, 711 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 711, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 452, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 275, 275, -1, -1, -1, -1, -1, -1, 275, 320, 6, 6, 6, 624, 732, 732, 732, 732, 732, -1, -1, 320, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399 },
			{ -1, 403, -1, -1, -1, -1, -1, -1, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 399, 399, 399, 783, 399, 796, 399, 895, 399, 399, 399, 399, 806, 399, 399, 399, 399, 399, 814, 819, 399, 399, 823, 399, 903 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 48 },
			{ -1, 403, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 763, -1, 458, -1, 712, -1, -1, -1, -1, 459, -1, -1, -1, -1, -1, 460, 461, -1, -1, 714, -1, 764 },
			{ -1, 404, -1, -1, -1, -1, 406, -1, 407, 407, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 257, 257, 257, 257, 257, 257, 257, 257, 257, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 871, 871, 871, 871, 871, 871, 871, 871, 871, 871, 871, 871, 871, 871, 871, 871, 871, 871, 871, 871, 871, 871, 871, 871, 871 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 261, 261, 265, 268, 268, 268, 268, 268, 268, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 472, 472, 472, 472, 472, 472, 472, 472, 472, 472, 472, 472, 472, 472, 472, 472, 472, 472, 472, 472, 472, 472, 472, 472, 472 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 129, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 129, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 129, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 412, 412, -1, -1, -1, -1, -1, -1, 412, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 695, -1, -1, 414, -1, 709, -1, -1, 19, 697, -1, -1, 415, 699, 376, -1, -1, -1, 762, -1, -1, 51, -1, 860, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 474, 474, 474, 474, 474, 474, 475, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 426, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 430, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 477, -1, 418, 478, 478, 478, 478, 478, 478, 478, 478, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 477, -1, 478, 478, 478, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 477, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 479, -1, -1, -1, -1, 480, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 483, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 484, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 486, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 489, 489, 489, 489, 489, 489, 489, 489, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 489, 489, 489, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 89, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 104, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 119, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 130, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 646, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 141, -1, 152, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 163, -1, -1, -1, -1, -1, -1, 71 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 119, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 173, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 117, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 183, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 26, 26, 26, 26, 26, 26, 26, 26, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 26, 26, 26, 26, 26, 26, 26, 26, 26, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 26, 26, 26, 26, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 64, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 490, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 491, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 161 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 16, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 493, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 16, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 495, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 496, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 86, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 23, 23, 23, 23, 23, 23, 23, 23, 23, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 498, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 694, -1, -1, -1, -1, -1 },
			{ -1, 453, -1, -1, -1, -1, 455, -1, 456, 456, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 90, 90, 90, 90, 90, 90, 105, 120, 120, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 639, 639, 639, 639, 639, 639, 639, 639, 639, 639, 639, 639, 639, 639, 639, 639, 639, 639, 639, 639, 639, 639, 639, 639, 639 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 131, 131, 654, 742, 742, 742, 742, 742, 742, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 164, 164, 164, 164, 164, 164, 174, 184, 184, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 717, -1, -1, 501, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 503, -1, -1, -1, -1, -1, 504, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 505, -1, -1, -1, 506, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 507, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 508, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 509, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 27 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 399, 399, 399, 399, 399, 399, 399, 399, 56, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 856, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 633, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 861, 399, 399, 399, 399, 399 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 399, 399, 887, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 949, 399, 399, 399, 399 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 399, 399, 399, 399, 888, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 399, 399, 399, 956, 513, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 278, 278, 278, 278, 278, 278, 278, 278, 278, 278, 278, 278, 278, 278, 278, 278, 278, 278, 278, 278, 278, 278, 278, 278, 278 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 107, 107, 107, 107, 107, 107, 107, 107, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 107, 107, 107, 107, 107, 107, 107, 107, 107, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 107, 107, 107, 107, 107, 107, 107, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 183, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 140, 140, 140, 151, 650, 650, 650, 650, 650, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 520, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 192, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 198, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 204, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 210, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 664, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 216, -1, 222, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 228, -1, -1, -1, -1, -1, -1, 234 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 204, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 240, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 245, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 719, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 492, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 525, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 64 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 64, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 444, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 526, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 494, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 497, -1, -1, -1, -1, 406, -1, 407, 407, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 667, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 667, 215, 215, 215, 215, 215, 215, 215, 215, 215 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 527, 528, 528, 529, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 31, 31, 31, 31, 31, 31, 31, 31, 31, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 75 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 93, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 536, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 108, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 121, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 132, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 143, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 93, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 154, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 165, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 175, 537, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 536, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 27, 399, 399, 399, 399, 399, 399, 399 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 27, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 22, 399, 399, 399, 399, 399 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 32, 399, 399 },
			{ -1, 516, -1, -1, -1, -1, 708, -1, 517, 517, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 869, 869, 869, 869, 869, 869, 869, 869, 869, 869, 869, 869, 869, 869, 869, 869, 869, 869, 869, 869, 869, 869, 869, 869, 869 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 280, 282, 284, 284, 284, 284, 284, 284, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 716, 716, 716, 716, 716, 716, 716, 716, 716, 716, 716, 716, 716, 716, 716, 716, 716, 716, 716, 716, 716, 716, 716, 716, 716 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 408, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 408, 519, 519, 519, 519, 519, 519, 519, 519, 519, 519, 519, 519, 519, 519, 519, 519, 519, 519, 519, 519, 519, 519, 519, 519, 519 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 162, 172, 172, 182, 650, 650, 650, 650, 650, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 71, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 547, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 548, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 71, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 553, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 33, 33, 33, 33, 33, 33, 33, 33, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 33, 33, 33, 33, 33, 33, 33, 33, 33, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 33, 33, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 530, -1, -1, -1, -1, 531, -1, 532, 532, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 868, 868, 868, 868, 868, 868, 868, 868, 868, 868, 868, 868, 868, 868, 868, 868, 868, 868, 868, 868, 868, 868, 868, 868, 868 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 217, 217, 223, 229, 229, 229, 229, 229, 229, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 557, 557, 557, 557, 557, 557, 557, 557, 557, 557, 557, 557, 557, 557, 557, 557, 557, 557, 557, 557, 557, 557, 557, 557, 557 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 558, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 75, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 75, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 399, 399, 399, 399, 399, 399, 399, 27, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 34 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 191, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 191, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 569, 569, 570, 571, 571, 571, 571, 571, 571, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 191, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 58, 58, 58, 58, 58, 58, 58, 58, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 58, 58, 58, 58, 58, 58, 58, 58, 58, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 58, 58, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 572, 572, 573, 574, 574, 574, 574, 574, 574, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 322, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 313, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 71, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 551, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 550, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 575, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 576, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 577, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 16, -1, -1, -1, -1, -1 },
			{ -1, 554, -1, -1, -1, -1, 722, -1, 555, 555, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 246, 246, 250, 254, 254, 254, 254, 254, 254, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 556, -1, -1, -1, -1, 722, -1, 555, 555, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 867, 867, 867, 867, 867, 867, 867, 867, 867, 867, 867, 867, 867, 867, 867, 867, 867, 867, 867, 867, 867, 867, 867, 867, 867 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 262, 262, 262, 262, 262, 262, 262, 262, 262, 262, 262, 262, 262, 262, 262, 262, 262, 262, 262, 262, 262, 262, 262, 262, 262 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 728, -1, -1, -1, -1, -1, 580, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 582, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 75, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 583, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 584, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 981 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 399, 399, 399, 399, 27, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 399, 399, 399, 27, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 518, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 518, 567, 567, 567, 567, 567, 567, 567, 567, 567, 567, 567, 567, 567, 567, 567, 567, 567, 567, 567, 567, 567, 567, 567, 567, 567 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 586, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 571, 571, 571, 571, 571, 571, 571, 571, 571, 813, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 571, 571, 571, 571, 571, -1, -1, -1, -1, 813, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 813, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 589, 589, 589, 589, 589, 589, 574, 574, 574, 590, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 589, 589, 589, 589, 589, 818, -1, -1, -1, 590, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 818, 818, 818, 818, 818, 818, -1, -1, -1, 590, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 71, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 591, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 727, 727, 727, 727, 727, 727, 727, 727, 727, 727, 727, 727, 727, 727, 727, 727, 727, 727, 727, 727, 727, 727, 727, 727, 727 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 533, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 533, 579, 579, 579, 579, 579, 579, 579, 579, 579, 579, 579, 579, 579, 579, 579, 579, 579, 579, 579, 579, 579, 579, 579, 579, 579 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 592, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 185 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 75, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 75, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 581, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 593, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 822, 822, 594, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 569, 569, 570, 571, 571, 571, 571, 571, 571, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 597, 597, 598, 571, 571, 571, 571, 571, 571, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 191, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 826, 826, 826, 826, 826, 826, 599, 599, 599, 590, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 830, 830, 830, 830, 830, 830, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 601, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 603, 603, 603, 603, 603, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 596, 596, 596, 596, 596, 596, 596, 596, 596, 604, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 604, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 605, 605, 605, 605, 605, 605, 605, 605, 605, 813, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 605, 605, 605, 605, 605, -1, -1, -1, -1, 813, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 608, 608, 608, 608, 608, 608, 609, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 578, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 578, 600, 600, 600, 600, 600, 600, 600, 600, 600, 600, 600, 600, 600, 600, 600, 600, 600, 600, 600, 600, 600, 600, 600, 600, 600 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 610, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 27, 399, 399, 399, 399, 399 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 878, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 36, 36, 36, 36, 36, 36, 59, 77, 77, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 611, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 94, 94, 94, 94, 94, 94, 59, 77, 77, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 36, 608, 608, 608, 608, 608, 609, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 75, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 613, 613, 613, 613, 613, 613, 596, 596, 596, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 730, 730, 730, 730, 730, 730, 730, 730, 730, 604, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 615, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 837, 837, 837, 837, 837, 837, 617, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 109, 109, 109, 109, 109, 109, 122, 77, 77, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 618, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 619, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 619, -1, -1, -1, -1, -1, -1, 621, 621, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 144, 144, 144, 144, 144, 144, 144, 144, 144, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 37, 37, 627, 734, 734, 734, 734, 734, 734, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 155, 155, 166, 176, 176, 176, 176, 176, 176, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 81, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97 },
			{ -1, 766, 766, 766, -1, -1, -1, -1, -1, -1, -1, 43, 43, 114, 648, 648, 648, 648, 648, 648, -1, -1, -1, -1, -1, -1, 766, -1, -1, -1, 766, -1, -1, -1, -1, -1, 65, -1, -1, -1, 65, 83, 99, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 46, 46, 46, 46, 46, 46, 46, 46, 46, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 57, 57, 57, 57, 57, 57, 57, 57, 57, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 734, 734, 734, 734, 734, 634, 78, 78, 78, 60, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 275, 275, -1, -1, -1, -1, 81, -1, 275, -1, 6, 6, 6, 624, 732, 732, 732, 732, 732, -1, -1, -1, 97, 97, 97, 97, 97, 97, 97, 97, 865, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97 },
			{ -1, 300, 300, -1, -1, -1, -1, -1, -1, 300, -1, 9, 9, 9, 9, 9, 9, 9, 9, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 64, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 323, 327, -1, -1, -1, 325, -1, 326, 326, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 327, -1, -1, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 71, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 238, 238, 238, 238, 238, 238, 238, 238, 238, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 399, 399, 399, 849, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 538, 399, 399, 399, 399, 399 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 78, 78, 78, 78, 78, 78, 78, 78, 78, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 277, -1, -1, -1, -1, -1, -1, -1, -1, -1, 437, 438, 438, 438, 438, 438, 438, 438, 438, -1, -1, -1, -1, -1, -1, 279, -1, 281, -1, 194, -1, -1, -1, -1, 283, -1, -1, -1, -1, -1, 285, 677, -1, -1, 242, -1, 678 },
			{ -1, 277, -1, -1, -1, -1, -1, -1, -1, -1, -1, 737, 737, 737, 737, 737, 219, 225, 225, 225, 213, -1, -1, -1, -1, -1, 279, -1, 281, -1, 194, -1, -1, -1, -1, 283, -1, -1, -1, -1, -1, 285, 677, -1, -1, 242, -1, 678 },
			{ -1, -1, 47, -1, -1, -1, -1, 190, -1, -1, 518, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 518, 775, 775, 775, 775, 775, 775, 775, 775, 775, 775, 775, 775, 775, 775, 775, 775, 775, 775, 775, 775, 775, 775, 775, 775, 775 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 471, 471, 471, 471, 471, 471, 471, 471, 471, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 409, -1, -1, -1, 409, 410, 411, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 142, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 199, 199, 199, 199, 199, 199, 199, 199, 199, 199, 199, 199, 199, 199, 199, 199, 199, 199, 199, 199, 199, 199, 199, 199, 199 },
			{ -1, 323, 689, -1, -1, -1, 325, -1, 326, 326, -1, 649, 649, 649, 649, 649, 649, 649, 649, 649, 689, -1, -1, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 116, 116, 116, 116, 116, 116, 116, 116, 116, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 247, 251, -1, -1, -1, -1, -1, -1, 110, 123, 255, 255, 255, 255, 255, 269, 269, 269, 269, 259, -1, -1, 263, -1, -1, 177, -1, 187, -1, 194, 4, 200, -1, -1, 206, 212, 218, 266, -1, 224, 230, 236, -1, 42, 242, 858, 678 },
			{ -1, -1, -1, -1, -1, -1, -1, 81, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 231, 829, 829, 829, 829, 829, 829, 829 },
			{ -1, 300, 300, -1, -1, -1, -1, -1, -1, 300, -1, 9, 9, 9, 9, 9, 9, 9, 9, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 757, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 190, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 221, 221, 221, 221, 221, 221, 221, 221, 221, 221, 221, 221, 221, 221, 221, 221, 221, 221, 221, 221, 221, 221, 221, 221, 221 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 721, -1, -1, -1, -1 },
			{ -1, 300, 300, -1, -1, -1, -1, -1, -1, 300, -1, 9, 9, 9, 9, 9, 9, 9, 9, 9, -1, -1, -1, -1, -1, -1, -1, 448, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, 360, -1, -1, -1, -1, -1, -1, -1, -1, 180, 180, 180, 180, 180, 180, 180, 180, 180, 360, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 392, 689, -1, -1, -1, 325, -1, 326, 326, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 689, -1, -1, 203, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 203, 100, 100, 100, 100, 100, 100, 100, 100, 100 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 540, -1, -1, -1, 540, 541, 542, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 552, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 554, 457, -1, -1, -1, 722, -1, 555, 555, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 457, -1, -1, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193 },
			{ -1, 404, 405, -1, -1, -1, 406, -1, 407, 407, -1, 181, 181, 181, 181, 181, 181, 181, 181, 181, -1, -1, -1, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 742, 742, 742, 742, 742, 211, 142, 142, 142, 205, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 404, 405, -1, -1, -1, 406, -1, 407, 407, -1, 249, 23, 23, 23, 23, 23, 23, 23, 23, -1, -1, -1, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215 },
			{ -1, -1, -1, -1, -1, -1, -1, 142, -1, -1, 533, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 533, 235, 235, 235, 235, 235, 235, 235, 235, 235, 235, 235, 235, 235, 235, 235, 235, 235, 235, 235, 235, 235, 235, 235, 235, 235 },
			{ -1, -1, 360, -1, -1, -1, -1, -1, -1, -1, -1, 632, 632, 632, 632, 632, 632, 632, 632, 632, 360, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 744, 744, 744, 744, 744, 233, 190, 190, 190, 227, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 687, -1, -1, -1, -1, 335, -1, -1, -1, -1 },
			{ -1, 404, 405, -1, -1, -1, 406, -1, 407, 407, -1, 253, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 307, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 190, -1, -1, 408, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 408, 272, 272, 272, 272, 272, 272, 272, 272, 272, 272, 272, 272, 272, 272, 272, 272, 272, 272, 272, 272, 272, 272, 272, 272, 272 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 498, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 721, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 142, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 258, 258, 258, 258, 258, 258, 258, 258, 258, 258, 258, 258, 258, 258, 258, 258, 258, 258, 258, 258, 258, 258, 258, 258, 258 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 338, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 339, -1, -1, -1, -1 },
			{ -1, -1, 450, -1, -1, -1, -1, 190, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 746, 746, 746, 746, 746, 746, 746, 746, 746, 746, 746, 746, 637, 746, 746, 746, 746, 746, 746, 746, 746, 746, 746, 746, 746 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 498, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 552, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 81, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 342, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 190, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 276, 276, 276, 276, 276, 276, 276, 276, 276, 276, 276, 276, 276, 276, 276, 276, 276, 276, 276, 276, 276, 276, 276, 276, 276 },
			{ -1, 453, 454, -1, -1, -1, 455, -1, 456, 456, -1, 670, 670, 670, 670, 670, 670, 670, 670, 670, 454, -1, -1, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72 },
			{ -1, 275, 275, -1, -1, -1, -1, 81, -1, 275, 320, 6, 6, 6, 624, 732, 732, 732, 732, 732, -1, -1, 320, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248 },
			{ -1, -1, -1, -1, -1, -1, -1, 142, -1, -1, 578, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 578, 600, 600, 600, 600, 600, 600, 600, 600, 600, 600, 600, 600, 600, 600, 600, 600, 600, 600, 600, 600, 600, 600, 600, 600, 600 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 322, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 313, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 315, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 81, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 400, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399 },
			{ -1, 275, 275, -1, -1, -1, -1, 81, -1, 359, 320, 6, 6, 6, 624, 732, 732, 732, 732, 732, -1, -1, 320, 399, 399, 399, 399, 978, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 578, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 578, 681, 681, 681, 681, 681, 681, 681, 681, 681, 681, 681, 681, 681, 681, 681, 681, 681, 681, 681, 681, 681, 681, 681, 681, 681 },
			{ -1, -1, -1, -1, -1, -1, -1, 190, -1, -1, 518, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 518, 567, 567, 567, 567, 567, 567, 567, 567, 567, 567, 567, 567, 567, 567, 567, 567, 567, 567, 567, 567, 567, 567, 567, 567, 567 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 518, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 518, 683, 683, 683, 683, 683, 683, 683, 683, 683, 683, 683, 683, 683, 683, 683, 683, 683, 683, 683, 683, 683, 683, 683, 683, 683 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 303, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 309, -1, -1, -1, 310, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 311, -1, -1, -1, -1 },
			{ -1, -1, 329, -1, -1, -1, -1, -1, -1, 329, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 361, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 741, 741, 741, 741, 741, 741, 655, 149, 149, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 771, 771, 771, 771, 771, 771, 660, 181, 181, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 451, 451, 451, 451, 451, 451, 451, 451, 451, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 702, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 383, -1, -1, -1, -1, -1 },
			{ -1, -1, 329, -1, -1, -1, -1, -1, -1, 329, -1, 686, 686, 686, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 424, -1, -1, -1, -1, 425, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 703, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 428, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 429, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 431, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 711, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 706, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 711, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 711, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 711, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 711, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 400, 399, 399, 399, 399, 399 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 777, 777, 777, 777, 777, 777, 777, 777, 777, 777, 777, 777, 777, 777, 777, 777, 777, 777, 777, 777, 777, 777, 777, 777, 777 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 427, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 485, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 502, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 510, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 56, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 683, 683, 683, 683, 683, 683, 683, 683, 683, 683, 683, 683, 683, 683, 683, 683, 683, 683, 683, 683, 683, 683, 683, 683, 683 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 535, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 543, 544, 544, 545, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 549, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 747, 747, 747, 747, 747, 747, 747, 747, 747, 747, 747, 747, 747, 747, 747, 747, 747, 747, 747, 747, 747, 747, 747, 747, 747 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 671, 671, 223, 229, 229, 229, 229, 229, 229, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 563, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 585, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 681, 681, 681, 681, 681, 681, 681, 681, 681, 681, 681, 681, 681, 681, 681, 681, 681, 681, 681, 681, 681, 681, 681, 681, 681 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 580, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 612, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 616, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 81, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 97, 97, 97, 97, 769, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97 },
			{ -1, 766, 766, 766, -1, -1, -1, -1, -1, -1, -1, 740, 740, 114, 648, 648, 648, 648, 648, 648, -1, -1, -1, -1, -1, -1, 766, -1, -1, -1, 766, -1, -1, -1, -1, -1, 65, -1, -1, -1, 65, 83, 99, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 625, 625, 625, 625, 625, 625, 625, 625, 625, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 634, 634, 634, 634, 634, 634, 78, 78, 78, 60, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 323, 689, -1, -1, -1, 325, -1, 326, 326, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 689, -1, -1, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100 },
			{ -1, 277, -1, -1, -1, -1, -1, -1, -1, -1, -1, 438, 438, 438, 439, 439, 439, 439, 439, 439, -1, -1, -1, -1, -1, -1, 279, -1, 281, -1, 194, -1, -1, -1, -1, 283, -1, -1, -1, -1, -1, 285, 677, -1, -1, 242, -1, 678 },
			{ -1, 277, -1, -1, -1, -1, -1, -1, -1, -1, -1, 219, 219, 219, 219, 219, 219, 225, 225, 225, 213, -1, -1, -1, -1, -1, 279, -1, 281, -1, 194, -1, -1, -1, -1, 283, -1, -1, -1, -1, -1, 285, 677, -1, -1, 242, -1, 678 },
			{ -1, 247, 251, -1, -1, -1, -1, -1, -1, 110, 123, 269, 269, 271, 271, 271, 271, 271, 271, 271, 259, -1, -1, 263, -1, -1, 177, -1, 187, -1, 194, 4, 200, -1, -1, 206, 212, 218, 266, -1, 224, 230, 236, -1, 42, 242, 858, 678 },
			{ -1, -1, -1, -1, -1, -1, -1, 81, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 829, 829, 829, 829, 829, 829, 872, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829 },
			{ -1, -1, 360, -1, -1, -1, -1, -1, -1, -1, -1, 657, 657, 657, 657, 657, 657, 657, 657, 657, 360, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 404, 405, -1, -1, -1, 406, -1, 407, 407, -1, 249, 249, 249, 249, 249, 249, 249, 249, 249, -1, -1, -1, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 211, 211, 211, 211, 211, 211, 142, 142, 142, 205, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 142, -1, -1, 578, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 578, 773, 773, 773, 773, 773, 773, 773, 773, 773, 773, 773, 773, 773, 773, 773, 773, 773, 773, 773, 773, 773, 773, 773, 773, 773 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 233, 233, 233, 233, 233, 233, 190, 190, 190, 227, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 341, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 190, -1, -1, 518, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 518, 775, 775, 775, 775, 775, 775, 775, 775, 775, 775, 775, 775, 775, 775, 775, 775, 775, 775, 775, 775, 775, 775, 775, 775, 775 },
			{ -1, -1, -1, -1, -1, -1, -1, 142, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 776, 776, 776, 776, 776, 776, 776, 776, 776, 776, 776, 776, 776, 776, 776, 776, 776, 776, 776, 776, 776, 776, 776, 776, 776 },
			{ -1, 453, 457, -1, -1, -1, 455, -1, 456, 456, -1, 670, 670, 670, 670, 670, 670, 670, 670, 670, 457, -1, -1, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72 },
			{ -1, 275, 275, -1, -1, -1, -1, 81, -1, 359, 320, 6, 6, 6, 624, 732, 732, 732, 732, 732, -1, -1, 320, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 252 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 295, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 81, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 401, 399, 399, 399, 399, 399 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 340, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 398, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 759, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 487, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 443, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 399, 399, 399, 399, 399, 399, 399, 401, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 476, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 500, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 511, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 81, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 97, 97, 97, 97, 787, 97, 97, 97, 890, 97, 97, 97, 97, 97, 921, 97, 97, 124, 97, 97, 97, 97, 97, 97, 97 },
			{ -1, 277, -1, -1, -1, -1, -1, -1, -1, -1, -1, 439, 439, 439, 439, 439, 439, 439, 439, 439, -1, -1, -1, -1, -1, -1, 279, -1, 281, -1, 194, -1, -1, -1, -1, 283, -1, -1, -1, -1, -1, 285, 677, -1, -1, 242, -1, 678 },
			{ -1, 247, 251, -1, -1, -1, -1, -1, -1, 110, 123, 271, 271, 271, 271, 271, 271, 271, 271, 271, 259, -1, -1, 263, -1, -1, 177, -1, 187, -1, 194, 4, 200, -1, -1, 206, 212, 218, 266, -1, 224, 230, 236, -1, 42, 242, 858, 678 },
			{ -1, -1, -1, -1, -1, -1, -1, 81, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 829, 829, 892, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829 },
			{ -1, -1, 360, -1, -1, -1, -1, -1, -1, -1, -1, 208, 208, 208, 208, 208, 208, 208, 208, 208, 360, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 404, 405, -1, -1, -1, 406, -1, 407, 407, -1, 253, 253, 253, 253, 253, 253, 253, 253, 253, -1, -1, -1, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215 },
			{ -1, -1, -1, -1, -1, -1, -1, 142, -1, -1, 578, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 578, 676, 676, 676, 676, 676, 676, 676, 676, 676, 676, 676, 676, 676, 676, 676, 676, 676, 676, 676, 676, 676, 676, 676, 676, 676 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 801, 801, 801, 801, 801, 239, 244, 244, 244, 227, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 190, -1, -1, 518, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 518, 682, 682, 682, 682, 682, 682, 682, 682, 682, 682, 682, 682, 682, 682, 682, 682, 682, 682, 682, 682, 682, 682, 682, 682, 682 },
			{ -1, -1, -1, -1, -1, -1, -1, 190, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 748, 748, 748, 748, 748, 748, 748, 748, 748, 748, 748, 748, 748, 748, 748, 748, 748, 748, 748, 748, 748, 748, 748, 748, 748 },
			{ -1, 275, 275, -1, -1, -1, -1, 81, -1, 359, 320, 6, 6, 6, 624, 732, 732, 732, 732, 732, -1, -1, 320, 248, 248, 248, 248, 252, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 296, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 81, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 399, 399, 399, 399, 399, 399, 399, 401, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 473, 474, 474, 474, 474, 474, 474, 474, 474, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 449, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 462, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 481, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 81, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 799, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 809, 97, 97, 97, 97 },
			{ -1, 277, -1, -1, -1, -1, -1, -1, -1, -1, -1, 439, 439, 439, 439, 439, 439, 440, 61, 61, -1, -1, -1, -1, -1, -1, 279, -1, 281, -1, 194, -1, -1, -1, -1, 283, -1, -1, -1, -1, -1, 285, 677, -1, -1, 242, -1, 678 },
			{ -1, -1, -1, -1, -1, -1, -1, 81, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 829, 980, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829 },
			{ -1, -1, 360, -1, -1, -1, -1, -1, -1, -1, -1, 208, 208, 208, 208, 208, 214, 214, 214, 214, 360, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 239, 239, 239, 239, 239, 239, 244, 244, 244, 227, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 275, 275, -1, -1, -1, -1, 81, -1, 359, 320, 6, 6, 6, 624, 732, 732, 732, 732, 732, -1, -1, 320, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 680, 248, 248, 248, 248, 248 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 328, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 81, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 402, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 474, 474, 474, 474, 474, 474, 474, 474, 474, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 1000, 399, 399, 463, 399, 399, 399, 399, 399, 399, 399 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 482, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 81, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 816, 97, 97, 97, 97, 97, 97, 97, 973, 97, 97, 97, 97, 97, 157, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97 },
			{ -1, -1, -1, -1, -1, -1, -1, 81, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 950, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829 },
			{ -1, -1, 360, -1, -1, -1, -1, -1, -1, -1, -1, 214, 214, 214, 214, 214, 214, 214, 214, 214, 360, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 233, 233, 233, 233, 233, 233, 270, 270, 270, 227, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 275, 275, -1, -1, -1, -1, 81, -1, 275, 320, 6, 6, 6, 624, 732, 732, 732, 732, 732, -1, -1, 320, 248, 248, 248, 248, 248, 248, 248, 248, 252, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 336, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 81, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 399, 399, 399, 399, 399, 399, 399, 400, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 568, 568, 568, 568, 568, 568, 568, 568, 568, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 399, 399, 399, 399, 399, 399, 399, 399, 464, 399, 399, 399, 399, 399, 465, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 488, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 81, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 97, 97, 97, 97, 905, 97, 97, 97, 908, 97, 97, 97, 97, 97, 168, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97 },
			{ -1, -1, -1, -1, -1, -1, -1, 81, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 750, 829, 778, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 337, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 81, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 399, 399, 399, 401, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 595, 595, 595, 595, 595, 595, 596, 596, 596, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 466, 399, 399, 399, 467, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 715, 399, 399, 399, 399 },
			{ -1, -1, -1, -1, -1, -1, -1, 81, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 97, 97, 821, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97 },
			{ -1, -1, -1, -1, -1, -1, -1, 81, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 900, 829, 829, 829, 829, 829, 829, 986 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 343, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 599, 599, 599, 599, 599, 599, 599, 599, 599, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 399, 399, 399, 399, 399, 399, 399, 468, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 469, 399, 399, 399, 399 },
			{ -1, -1, -1, -1, -1, -1, -1, 81, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 178, 97, 97, 97, 825, 97, 97, 97, 910, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 157, 97, 97, 97, 97 },
			{ -1, -1, -1, -1, -1, -1, -1, 81, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 957, 829, 829, 829, 829, 829 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 603, 603, 603, 603, 603, 603, 603, 603, 603, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 399, 399, 399, 399, 470, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399 },
			{ -1, -1, -1, -1, -1, -1, -1, 81, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 97, 97, 97, 97, 908, 97, 97, 188, 97, 97, 97, 97, 97, 97, 912, 97, 97, 97, 97, 97, 195, 97, 982, 97, 97 },
			{ -1, -1, -1, -1, -1, -1, -1, 81, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 829, 829, 897, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 792, 829, 829, 829, 829, 829, 954, 829, 829, 829 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 606, 606, 606, 606, 606, 606, 607, 599, 599, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 399, 399, 399, 401, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399 },
			{ -1, -1, -1, -1, -1, -1, -1, 81, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 97, 97, 97, 97, 201, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 729, 729, 729, 729, 729, 729, 729, 729, 729, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 402, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399 },
			{ -1, -1, -1, -1, -1, -1, -1, 81, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 207, 207, 207, 207, 207, 207, 207, 207, 207, 207, 207, 207, 207, 207, 207, 207, 207, 207, 207, 207, 207, 207, 207, 207, 207 },
			{ -1, -1, -1, -1, -1, -1, -1, 81, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 248, 248, 248, 248, 248, 248, 248, 752, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 614, 614, 614, 614, 614, 614, 614, 614, 614, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 400 },
			{ -1, -1, -1, -1, -1, -1, -1, 81, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 780, 248, 248, 248, 248, 248 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 618, 618, 618, 618, 618, 618, 618, 618, 618, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 512, 399, 399, 399, 399 },
			{ -1, -1, -1, -1, -1, -1, -1, 81, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 752, 248, 248, 248, 248, 248, 248 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 512, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399 },
			{ -1, -1, -1, -1, -1, -1, -1, 81, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 256, 248, 248, 248, 248, 248 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 399, 399, 399, 399, 399, 399, 399, 514, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399 },
			{ -1, -1, -1, -1, -1, -1, -1, 81, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 812, 256, 248, 248, 248, 248, 248, 248 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 400, 399, 399, 399, 399, 399, 399, 399 },
			{ -1, -1, -1, -1, -1, -1, -1, 81, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 260, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 515, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 401, 399, 399, 399, 399, 399, 399 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 539, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 564, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 565, 399, 399, 399, 399, 399 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 566, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 399, 399, 399, 399, 399, 399, 399, 602, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399 },
			{ -1, 323, 324, -1, -1, -1, 325, -1, 326, 875, -1, 44, 44, 44, 630, 630, 630, 630, 630, 630, 327, -1, -1, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100 },
			{ -1, -1, -1, -1, -1, -1, -1, 81, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 961, 248, 248, 248, 248 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 981, 399, 850, 399, 399, 399, 399 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 626, 626, 626, 626, 626, 626, 626, 626, 626, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 300, 300, -1, -1, -1, -1, -1, -1, 300, -1, 9, 9, 9, 9, 9, 9, 9, 9, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 629, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 323, 688, -1, -1, -1, 325, -1, 326, 875, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 689, -1, -1, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 631, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 969, 399, 399, 399, 399 },
			{ -1, 323, 688, -1, -1, -1, 325, -1, 326, 875, -1, 115, 115, 115, 649, 649, 649, 649, 649, 649, 689, -1, -1, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 641, 641, 641, 641, 641, 641, 641, 641, 641, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 81, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 829, 829, 829, 829, 829, 829, 833, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829 },
			{ -1, 275, 275, -1, -1, -1, -1, 81, -1, 275, 320, 6, 6, 6, 624, 732, 732, 732, 732, 732, -1, -1, 320, 829, 829, 829, 829, 829, 829, 829, 829, 802, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 801, 801, 801, 801, 801, 801, 801, 801, 801, 227, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 142, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 789, 789, 789, 789, 789, 789, 789, 789, 789, 789, 789, 789, 789, 789, 789, 789, 789, 789, 789, 789, 789, 789, 789, 789, 789 },
			{ -1, -1, -1, -1, -1, -1, -1, 190, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 791, 791, 791, 791, 791, 791, 791, 791, 791, 791, 791, 791, 791, 791, 791, 791, 791, 791, 791, 791, 791, 791, 791, 791, 791 },
			{ -1, 453, 454, -1, -1, -1, 455, -1, 456, 456, -1, 674, 674, 674, 674, 674, 749, 749, 749, 749, 454, -1, -1, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72 },
			{ -1, 275, 275, -1, -1, -1, -1, 81, -1, 359, 320, 6, 6, 6, 624, 732, 732, 732, 732, 732, -1, -1, 320, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 873, 248, 248, 248, 248 },
			{ -1, -1, -1, -1, -1, -1, -1, 81, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 707, 399, 399, 399, 399, 399, 399 },
			{ -1, 300, 300, -1, -1, -1, -1, -1, -1, 300, -1, 885, 898, 898, 904, 9, 9, 9, 9, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 866, 866, 774, 790, 790, 790, 790, 790, 790, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 755, 755, 755, 755, 755, 755, 755, 755, 755, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 835, 399, 399, 399, 399, 399, 399, 399 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 834, 834, 834, 834, 834, 834, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 81, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 97, 97, 97, 97, 97, 97, 97, 97, 864, 97, 97, 993, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97 },
			{ -1, 275, 275, -1, -1, -1, -1, 81, -1, 275, -1, 6, 6, 6, 624, 732, 732, 732, 732, 732, -1, -1, -1, 97, 97, 97, 97, 97, 97, 97, 97, 135, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97 },
			{ -1, 300, 300, -1, -1, -1, -1, -1, -1, 874, -1, 9, 9, 9, 9, 9, 9, 9, 9, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 757, -1, -1, -1, -1 },
			{ -1, 300, 300, -1, -1, -1, -1, -1, -1, 874, -1, 9, 9, 9, 9, 9, 9, 9, 9, 9, -1, -1, -1, -1, -1, -1, -1, 448, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 81, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 893, 248, 248, 248, 248, 248, 248, 248 },
			{ -1, 323, 324, -1, -1, -1, 325, -1, 326, 875, -1, 630, 630, 630, 630, 630, 630, 630, 630, 630, 327, -1, -1, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 854, 733, 733, 733, 733, 733, 733, 733, 733, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 81, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 906, 248, 248, 248, 248, 248, 248 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 851, 399, 399, 399, 981, 399, 399, 399, 399, 399, 399 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 917, 399, 399, 399, 399, 399, 399 },
			{ -1, 323, 688, -1, -1, -1, 325, -1, 326, 875, -1, 649, 649, 649, 649, 649, 649, 649, 649, 649, 689, -1, -1, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100 },
			{ -1, -1, -1, -1, -1, -1, -1, 81, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 829, 829, 829, 829, 829, 836, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 839, 829, 829, 829, 829, 829, 829, 829 },
			{ -1, 453, 454, -1, -1, -1, 455, -1, 456, 456, -1, 749, 749, 749, 749, 749, 749, 749, 749, 749, 454, -1, -1, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72, 72 },
			{ -1, 275, 275, -1, -1, -1, -1, 81, -1, 359, 320, 6, 6, 6, 624, 732, 732, 732, 732, 732, -1, -1, 320, 248, 248, 248, 248, 977, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248 },
			{ -1, -1, -1, -1, -1, -1, -1, 81, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 761, 399, 399, 399, 399, 399 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 951, 951, 951, 951, 951, 951, 951, 951, 951, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 838, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399 },
			{ -1, -1, -1, -1, -1, -1, -1, 81, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 899, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97 },
			{ -1, -1, -1, -1, -1, -1, -1, 81, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 901, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 733, 733, 733, 733, 733, 733, 733, 733, 733, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 81, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 841, 829, 829, 829, 829, 829, 829 },
			{ -1, 275, 275, -1, -1, -1, -1, 81, -1, 359, 320, 6, 6, 6, 624, 732, 732, 732, 732, 732, -1, -1, 320, 248, 248, 804, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248 },
			{ -1, -1, -1, -1, -1, -1, -1, 81, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 827, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 805, 805, 805, 805, 805, 805, 805, 805, 805, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 399, 399, 399, 399, 840, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 733, 733, 854, 854, 854, 854, 854, 854, 854, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 81, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 841, 829 },
			{ -1, -1, -1, -1, -1, -1, -1, 81, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 399, 399, 399, 831, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 399, 399, 399, 399, 399, 399, 842, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399 },
			{ -1, -1, -1, -1, -1, -1, -1, 81, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 836, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 399, 399, 399, 399, 844, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399 },
			{ -1, -1, -1, -1, -1, -1, -1, 81, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 836, 829 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 399, 399, 399, 831, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399 },
			{ -1, -1, -1, -1, -1, -1, -1, 81, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 829, 829, 829, 845, 829, 829, 829, 829, 829, 829, 829, 829, 960, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 761, 399, 399, 399, 399, 399 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 846, 399, 399, 399, 399, 399, 399, 399 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 847, 399, 399, 399, 399 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 399, 399, 399, 848, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 399, 399, 399, 399, 399, 399, 852, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399 },
			{ -1, 300, 300, -1, -1, -1, -1, -1, -1, 874, -1, 9, 9, 9, 9, 9, 9, 9, 9, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 300, 300, -1, -1, -1, -1, -1, -1, 874, -1, 9, 9, 9, 9, 9, 9, 9, 9, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 379, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 81, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 883, 829, 829, 829, 829 },
			{ -1, 300, 300, -1, -1, -1, -1, -1, -1, 874, -1, 9, 9, 9, 9, 9, 9, 9, 9, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 380, -1, -1, -1, -1 },
			{ -1, 300, 300, -1, -1, -1, -1, -1, -1, 874, -1, 9, 9, 9, 9, 9, 9, 9, 9, 9, -1, -1, -1, -1, -1, -1, -1, 381, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 300, 300, -1, -1, -1, -1, -1, -1, 874, -1, 9, 9, 9, 9, 9, 9, 9, 9, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 382, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 300, 300, -1, -1, -1, -1, -1, -1, 874, -1, 9, 9, 9, 9, 9, 9, 9, 9, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 64 },
			{ -1, 300, 300, -1, -1, -1, -1, -1, -1, 874, -1, 9, 9, 9, 9, 9, 9, 9, 9, 9, -1, -1, -1, -1, -1, -1, -1, 64, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 300, 300, -1, -1, -1, -1, -1, -1, 874, -1, 9, 9, 9, 9, 9, 9, 9, 9, 9, -1, -1, -1, -1, -1, 384, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 300, 300, -1, -1, -1, -1, -1, -1, 874, -1, 9, 9, 9, 9, 9, 9, 9, 9, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 388, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 300, 300, -1, -1, -1, -1, -1, -1, 874, -1, 9, 9, 9, 9, 9, 9, 9, 9, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 81, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 877, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 920, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 922, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 923, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 924, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 881, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 925, -1, 926, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 927, -1, -1, -1, -1, -1, -1, 919 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 923, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 928, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 929, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 399, 909, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 938, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 937, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 933, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 934, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 940, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 81, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 952, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248 },
			{ -1, -1, -1, -1, -1, -1, -1, 81, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 966, 248, 248, 248, 248, 248, 248, 248 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 971, 399, 399, 399, 399, 399, 399, 399 },
			{ -1, 275, 275, -1, -1, -1, -1, 81, -1, 359, 320, 6, 6, 6, 624, 732, 732, 732, 732, 732, -1, -1, 320, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 930, 248, 248, 248, 248 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 902, 902, 902, 902, 902, 902, 902, 902, 902, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 81, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 399, 399, 399, 399, 399, 399, 399, 399, 907, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 913, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399 },
			{ -1, -1, -1, -1, -1, -1, -1, 81, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 248, 248, 248, 248, 964, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248 },
			{ -1, -1, -1, -1, -1, -1, -1, 81, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 991, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 988, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399 },
			{ -1, 275, 275, -1, -1, -1, -1, 81, -1, 359, 320, 6, 6, 6, 624, 732, 732, 732, 732, 732, -1, -1, 320, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 958, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248 },
			{ -1, -1, -1, -1, -1, -1, -1, 81, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 399, 909, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 877, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399 },
			{ -1, -1, -1, -1, -1, -1, -1, 81, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 968, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248 },
			{ -1, -1, -1, -1, -1, -1, -1, 81, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 911, 399, 399, 399, 399, 399, 399, 399 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 915, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399 },
			{ -1, -1, -1, -1, -1, -1, -1, 81, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 970, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248 },
			{ -1, -1, -1, -1, -1, -1, -1, 81, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 913, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 911, 399, 399, 399, 399, 399, 399 },
			{ -1, -1, -1, -1, -1, -1, -1, 81, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 911, 399, 399, 399, 399, 399, 399 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 916, 399, 399, 399, 399, 399, 399, 399 },
			{ -1, -1, -1, -1, -1, -1, -1, 81, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 914, 399, 399, 399, 399, 399, 399, 399 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 917, 399, 399, 399, 399, 399, 399, 399 },
			{ -1, -1, -1, -1, -1, -1, -1, 81, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 399, 399, 399, 399, 399, 913, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 399, 399, 399, 399, 399, 399, 399, 399, 918, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399 },
			{ -1, -1, -1, -1, -1, -1, -1, 81, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 829, 829, 829, 947, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 931, -1, -1, -1, -1, 932, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 935, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 936, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 939, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 81, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 941, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 941, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399 },
			{ -1, -1, -1, -1, -1, -1, -1, 81, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 983, 248, 248, 248 },
			{ -1, 275, 275, -1, -1, -1, -1, 81, -1, 359, 320, 6, 6, 6, 624, 732, 732, 732, 732, 732, -1, -1, 320, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 987, 248, 248, 248, 248, 248, 248, 248 },
			{ -1, -1, -1, -1, -1, -1, -1, 81, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 829, 829, 829, 829, 963, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829 },
			{ -1, -1, -1, -1, -1, -1, -1, 81, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 399, 399, 399, 399, 953, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 399, 399, 399, 399, 399, 399, 399, 990, 399, 399, 399, 399, 399, 972, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399 },
			{ -1, -1, -1, -1, -1, -1, -1, 81, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 992, 248, 248, 248, 248, 248 },
			{ -1, 275, 275, -1, -1, -1, -1, 81, -1, 359, 320, 6, 6, 6, 624, 732, 732, 732, 732, 732, -1, -1, 320, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248 },
			{ -1, -1, -1, -1, -1, -1, -1, 81, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 959, 399, 399, 399, 399 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 399, 399, 399, 399, 971, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399 },
			{ -1, -1, -1, -1, -1, -1, -1, 81, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 399, 399, 399, 399, 399, 399, 399, 399, 962, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 972, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399 },
			{ -1, -1, -1, -1, -1, -1, -1, 81, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 399, 399, 399, 399, 965, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399 },
			{ -1, -1, -1, -1, -1, -1, -1, 81, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 399, 399, 399, 399, 967, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399 },
			{ -1, -1, -1, -1, -1, -1, -1, 81, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 829, 829, 829, 829, 979, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 984, 399, 399, 399, 399, 399 },
			{ -1, -1, -1, -1, -1, -1, -1, 81, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 997, 97, 97, 97, 97, 97, 97, 97 },
			{ -1, -1, -1, -1, -1, -1, -1, 81, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 989, 248, 248, 248 },
			{ -1, -1, -1, -1, -1, -1, -1, 81, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 829, 829, 829, 829, 996, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829 },
			{ -1, -1, -1, -1, -1, -1, -1, 81, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 97, 97, 97, 97, 999, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97 },
			{ -1, -1, -1, -1, -1, -1, -1, 81, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 829, 985, 829, 829, 829, 829, 829, 829 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 399, 994, 399, 399, 399, 399, 399, 399, 399 }
		};
		
		
		private static int[] yy_state_dtrans = new int[]
		{
			  0
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
							System.Diagnostics.Debug.Assert(last_accept_state >= 1001);
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

