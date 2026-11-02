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
	
	
	internal partial class Scanner
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
		
		#pragma warning disable 162
		
		
		Tokens Accept0(int state,out bool accepted)
		{
			accepted = true;
			
			switch(state)
			{
				case 1:
					// #line 670
					{
						INIT();
						errors += time.SetTimeZone(str, ref pos) ? 0 : 1;
						DEINIT();
						return Tokens.TIMEZONE;
					}
					break;
					
				case 2:
					// #line 724
					{
					  break;
					}
					break;
					
				case 3:
					// #line 755
					{
					  errors++; return Tokens.ERROR;
					}
					break;
					
				case 4:
					// #line 441
					{
						INIT();
						if (time.have_date!=0) { errors++; return Tokens.ERROR; } 
						time.HAVE_DATE();
						time.d = DateInfo.ParseUnsignedInt(str, ref pos, 2);
						DateInfo.SkipDaySuffix(str, ref pos);
						time.m = DateInfo.ParseMonth(str, ref pos);
						DEINIT();
						return Tokens.DATE_TEXT;
					}
					break;
					
				case 5:
					// #line 204
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
					// #line 427
					{
						INIT();
						if (time.have_date!=0) { errors++; return Tokens.ERROR; } 
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
					// #line 660
					{
						INIT();
						if (time.have_date != 0) { errors++; return Tokens.ERROR; }
						time.HAVE_DATE();
						time.m = DateInfo.ParseMonth(str, ref pos);
						DEINIT();
						return Tokens.DATE_TEXT;
					}
					break;
					
				case 8:
					// #line 647
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
					
				case 9:
					// #line 249
					{
						INIT();
						if (time.have_time!=0) { errors++; return Tokens.ERROR; }
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
					
				case 10:
					// #line 312
					{
						INIT();
						if (time.have_date!=0) { errors++; return Tokens.ERROR; } 
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
					
				case 11:
					// #line 342
					{
						INIT();
						if (time.have_date!=0) { errors++; return Tokens.ERROR;} 
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
					
				case 12:
					// #line 225
					{
						INIT();
						if (time.have_time!=0) { errors++; return Tokens.ERROR; }
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
							errors++; return Tokens.ERROR; 
						}	
						DEINIT();
						return Tokens.TIME12;
					}
					break;
					
				case 13:
					// #line 614
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
					
				case 14:
					// #line 169
					{
						INIT();
						DEINIT();
						return Tokens.RELATIVE;
					}
					break;
					
				case 15:
					// #line 708
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
					
				case 16:
					// #line 272
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
								errors++; return Tokens.ERROR;
					  }
						time.have_time++;
						DEINIT();
						return Tokens.GNU_NOCOLON;
					}
					break;
					
				case 17:
					// #line 605
					{
						INIT();
						time.y = DateInfo.ParseUnsignedInt(str, ref pos, 4);
						DEINIT();
						return Tokens.CLF;
					}
					break;
					
				case 18:
					// #line 176
					{
						INIT();
						time.UNHAVE_TIME();
						time.HAVE_TIME();
						time.h = 12;
						DEINIT();
						return Tokens.RELATIVE;
					}
					break;
					
				case 19:
					// #line 357
					{
						INIT();
						if (time.have_date!=0) { errors++; return Tokens.ERROR; } 
						time.HAVE_DATE();
						time.y = DateInfo.ParseUnsignedInt(str, ref pos, 4);
						time.m = DateInfo.ParseUnsignedInt(str, ref pos, 2);
						time.d = DateInfo.ParseUnsignedInt(str, ref pos, 2);
						time.y = DateInfo.ProcessYear(time.y);
						DEINIT();
						return Tokens.ISO_DATE;
					}
					break;
					
				case 20:
					// #line 412
					{
						INIT();
						if (time.have_date!=0) { errors++; return Tokens.ERROR; } 
						time.HAVE_DATE();
						time.y = DateInfo.ParseUnsignedInt(str, ref pos, 4);
						time.m = DateInfo.ParseMonth(str, ref pos);
						time.d = 1;
						time.y = DateInfo.ProcessYear(time.y);
						DEINIT();
						return Tokens.DATE_NO_DAY;
					}
					break;
					
				case 21:
					// #line 398
					{
						INIT();
						if (time.have_date!=0) { errors++; return Tokens.ERROR; } 
						time.HAVE_DATE();
						time.m = DateInfo.ParseMonth(str, ref pos);
						time.y = DateInfo.ParseUnsignedInt(str, ref pos, 4);
						time.d = 1;
						time.y = DateInfo.ProcessYear(time.y);
						DEINIT();
						return Tokens.DATE_NO_DAY;
					}
					break;
					
				case 22:
					// #line 679
					{
						INIT();
						if (time.have_date!=0) { errors++; return Tokens.ERROR; } 
						time.HAVE_DATE();
						time.m = DateInfo.ParseMonth(str, ref pos);
						time.d = DateInfo.ParseUnsignedInt(str, ref pos, 2);
						if (time.have_time!=0) { errors++; return Tokens.ERROR; }
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
					
				case 23:
					// #line 186
					{
						INIT();
						time.UNHAVE_TIME();
						DEINIT();
						return Tokens.RELATIVE;
					}
					break;
					
				case 24:
					// #line 384
					{
						INIT();
						if (time.have_date!=0) { errors++; return Tokens.ERROR;} 
						time.HAVE_DATE();
						time.d = DateInfo.ParseUnsignedInt(str, ref pos, 2);
						time.m = DateInfo.ParseUnsignedInt(str, ref pos, 2);
						time.y = DateInfo.ParseUnsignedInt(str, ref pos, 2);
						time.y = DateInfo.ProcessYear(time.y);
						DEINIT();
						return Tokens.DATE_FULL_POINTED;
					}
					break;
					
				case 25:
					// #line 594
					{
						INIT();
						if (time.have_date != 0) { errors++; return Tokens.ERROR; }
						time.HAVE_DATE();
						time.y = DateInfo.ProcessYear(DateInfo.ParseUnsignedInt(str, ref pos, 4));
						time.m = DateInfo.ParseUnsignedInt(str, ref pos, 2);
						time.d = 1;
						DEINIT();
						return Tokens.ISO_DATE;
					}
					break;
					
				case 26:
					// #line 296
					{
						INIT();
						if (time.have_time!=0) { errors++; return Tokens.ERROR; }
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
					
				case 27:
					// #line 492
					{
						INIT();
						if (time.have_date!=0) { errors++; return Tokens.ERROR; } 
						time.HAVE_DATE();
						time.y = DateInfo.ParseUnsignedInt(str, ref pos, 4);
						time.d = DateInfo.ParseUnsignedInt(str, ref pos, 3);
						time.m = 1;
						time.y = DateInfo.ProcessYear(time.y);
						DEINIT();
						return Tokens.PG_YEARDAY;
					}
					break;
					
				case 28:
					// #line 525
					{
						{
							int w, d;
							INIT();
							if (time.have_date!=0) { errors++; return Tokens.ERROR; } 
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
					
				case 29:
					// #line 629
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
					
				case 30:
					// #line 546
					{
						INIT();
						if (time.have_date!=0) { errors++; return Tokens.ERROR; } 
						time.HAVE_DATE();
						time.m = DateInfo.ParseMonth(str, ref pos);
						time.d = DateInfo.ParseUnsignedInt(str, ref pos, 2);
						time.y = DateInfo.ParseUnsignedInt(str, ref pos, 4);
						time.y = DateInfo.ProcessYear(time.y);
						DEINIT();
						return Tokens.PG_TEXT;
					}
					break;
					
				case 31:
					// #line 371
					{
						INIT();
						if (time.have_date!=0) { errors++; return Tokens.ERROR;} 
						time.HAVE_DATE();
						time.d = DateInfo.ParseUnsignedInt(str, ref pos, 2);
						time.m = DateInfo.ParseUnsignedInt(str, ref pos, 2);
						time.y = DateInfo.ParseUnsignedInt(str, ref pos, 4);
						DEINIT();
						return Tokens.DATE_FULL_POINTED;
					}
					break;
					
				case 32:
					// #line 454
					{
						INIT();
						if (time.have_date!=0) { errors++; return Tokens.ERROR; } 
						time.HAVE_DATE();
						time.y = DateInfo.ParseUnsignedInt(str, ref pos, 4);
						time.m = DateInfo.ParseUnsignedInt(str, ref pos, 2);
						time.d = DateInfo.ParseUnsignedInt(str, ref pos, 2);
						DEINIT();
						return Tokens.DATE_NOCOLON;
					}
					break;
					
				case 33:
					// #line 506
					{
						int week, day;
						INIT();
						if (time.have_date!=0) { errors++; return Tokens.ERROR; } 
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
					
				case 34:
					// #line 194
					{
						INIT();
						time.HAVE_RELATIVE();
						time.UNHAVE_TIME();
						time.relative.d = 1;
						DEINIT();
						return Tokens.RELATIVE;
					}
					break;
					
				case 35:
					// #line 560
					{
						INIT();
						if (time.have_date!=0) { errors++; return Tokens.ERROR; } 
						time.HAVE_DATE();
						time.y = DateInfo.ParseUnsignedInt(str, ref pos, 4);
						time.m = DateInfo.ParseMonth(str, ref pos);
						time.d = DateInfo.ParseUnsignedInt(str, ref pos, 2);
						time.y = DateInfo.ProcessYear(time.y);
						DEINIT();
						return Tokens.PG_TEXT;
					}
					break;
					
				case 36:
					// #line 159
					{
						INIT();
						time.HAVE_RELATIVE();
						time.UNHAVE_TIME();
						time.relative.d = -1;
						DEINIT();
						return Tokens.RELATIVE;
					}
					break;
					
				case 37:
					// #line 329
					{
						INIT();
						if (time.have_date!=0) { errors++; return Tokens.ERROR; } 
						time.HAVE_DATE();
						time.y = DateInfo.ParseUnsignedInt(str, ref pos, 4);
						time.m = DateInfo.ParseUnsignedInt(str, ref pos, 2);
						time.d = DateInfo.ParseUnsignedInt(str, ref pos, 2);
						DEINIT();
						return Tokens.ISO_DATE;
					}
					break;
					
				case 38:
					// #line 738
					{
						INIT();
						time.HAVE_RELATIVE();
						int behavior;
						int amount = DateInfo.ParseRelativeText(str, ref pos, out behavior);
						while (pos < str.Length && (str[pos] == ' ' || str[pos] == '\t')) pos++;
						int beh = amount > 0 ? 1 : behavior;
						time.relative.special = amount > 0
							? DateInfo.SpecialDayOfWeekInMonth
							: DateInfo.SpecialLastDayOfWeekInMonth;
						int start = pos;
						while (pos < str.Length && str[pos] != ' ' && str[pos] != '\t') pos++;
						time.SetRelative(str.Substring(start, pos - start), amount, beh);
						DEINIT();
						return Tokens.RELATIVE;
					}
					break;
					
				case 39:
					// #line 728
					{
						INIT();
						time.HAVE_RELATIVE();
						time.relative.first_last_day_of = (str.Length > 0 && str[0] == 'l')
							? DateInfo.LastDayOfMonth
							: DateInfo.FirstDayOfMonth;
						DEINIT();
						return Tokens.RELATIVE;
					}
					break;
					
				case 40:
					// #line 467
					{
						INIT();
						if (time.have_time!=0) { errors++; return Tokens.ERROR; }
						time.HAVE_TIME();
						if (time.have_date!=0) { errors++; return Tokens.ERROR; } 
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
					
				case 41:
					// #line 574
					{
						INIT();
						if (time.have_time!=0) { errors++; return Tokens.ERROR; }
						time.HAVE_TIME();
						if (time.have_date!=0) { errors++; return Tokens.ERROR; } 
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
					
				case 43: goto case 1;
				case 44: goto case 2;
				case 45: goto case 3;
				case 46: goto case 4;
				case 47: goto case 6;
				case 48: goto case 7;
				case 49: goto case 8;
				case 50: goto case 9;
				case 51: goto case 10;
				case 52: goto case 11;
				case 53: goto case 12;
				case 54: goto case 15;
				case 55: goto case 16;
				case 56: goto case 19;
				case 57: goto case 20;
				case 58: goto case 21;
				case 59: goto case 22;
				case 60: goto case 25;
				case 61: goto case 26;
				case 62: goto case 27;
				case 63: goto case 29;
				case 64: goto case 30;
				case 65: goto case 37;
				case 66: goto case 40;
				case 67: goto case 41;
				case 69: goto case 1;
				case 70: goto case 3;
				case 71: goto case 4;
				case 72: goto case 6;
				case 73: goto case 7;
				case 74: goto case 9;
				case 75: goto case 10;
				case 76: goto case 12;
				case 77: goto case 15;
				case 78: goto case 19;
				case 79: goto case 20;
				case 80: goto case 22;
				case 81: goto case 25;
				case 82: goto case 26;
				case 83: goto case 27;
				case 84: goto case 29;
				case 85: goto case 37;
				case 86: goto case 40;
				case 87: goto case 41;
				case 89: goto case 1;
				case 90: goto case 3;
				case 91: goto case 4;
				case 92: goto case 6;
				case 93: goto case 7;
				case 94: goto case 9;
				case 95: goto case 10;
				case 96: goto case 12;
				case 97: goto case 15;
				case 98: goto case 19;
				case 99: goto case 20;
				case 100: goto case 22;
				case 101: goto case 25;
				case 102: goto case 26;
				case 103: goto case 27;
				case 104: goto case 29;
				case 105: goto case 40;
				case 107: goto case 1;
				case 108: goto case 3;
				case 109: goto case 4;
				case 110: goto case 6;
				case 111: goto case 7;
				case 112: goto case 9;
				case 113: goto case 10;
				case 114: goto case 15;
				case 115: goto case 19;
				case 116: goto case 20;
				case 117: goto case 22;
				case 118: goto case 25;
				case 119: goto case 26;
				case 120: goto case 27;
				case 121: goto case 29;
				case 122: goto case 40;
				case 124: goto case 1;
				case 125: goto case 3;
				case 126: goto case 4;
				case 127: goto case 6;
				case 128: goto case 7;
				case 129: goto case 9;
				case 130: goto case 10;
				case 131: goto case 15;
				case 132: goto case 19;
				case 133: goto case 20;
				case 134: goto case 22;
				case 135: goto case 25;
				case 136: goto case 26;
				case 137: goto case 29;
				case 138: goto case 40;
				case 140: goto case 1;
				case 141: goto case 4;
				case 142: goto case 6;
				case 143: goto case 9;
				case 144: goto case 15;
				case 145: goto case 19;
				case 146: goto case 20;
				case 147: goto case 22;
				case 148: goto case 25;
				case 149: goto case 29;
				case 150: goto case 40;
				case 152: goto case 1;
				case 153: goto case 4;
				case 154: goto case 6;
				case 155: goto case 9;
				case 156: goto case 15;
				case 157: goto case 19;
				case 158: goto case 20;
				case 159: goto case 22;
				case 160: goto case 29;
				case 161: goto case 40;
				case 163: goto case 1;
				case 164: goto case 4;
				case 165: goto case 6;
				case 166: goto case 9;
				case 167: goto case 15;
				case 168: goto case 19;
				case 169: goto case 20;
				case 170: goto case 22;
				case 171: goto case 29;
				case 172: goto case 40;
				case 174: goto case 1;
				case 175: goto case 4;
				case 176: goto case 6;
				case 177: goto case 9;
				case 178: goto case 15;
				case 179: goto case 19;
				case 180: goto case 20;
				case 181: goto case 22;
				case 182: goto case 29;
				case 183: goto case 40;
				case 185: goto case 1;
				case 186: goto case 4;
				case 187: goto case 6;
				case 188: goto case 9;
				case 189: goto case 19;
				case 190: goto case 20;
				case 191: goto case 22;
				case 192: goto case 29;
				case 193: goto case 40;
				case 195: goto case 1;
				case 196: goto case 4;
				case 197: goto case 6;
				case 198: goto case 9;
				case 199: goto case 19;
				case 200: goto case 20;
				case 201: goto case 22;
				case 202: goto case 29;
				case 203: goto case 40;
				case 205: goto case 1;
				case 206: goto case 6;
				case 207: goto case 9;
				case 208: goto case 19;
				case 209: goto case 20;
				case 210: goto case 22;
				case 211: goto case 29;
				case 213: goto case 1;
				case 214: goto case 6;
				case 215: goto case 9;
				case 216: goto case 20;
				case 217: goto case 22;
				case 218: goto case 29;
				case 220: goto case 1;
				case 221: goto case 6;
				case 222: goto case 9;
				case 223: goto case 20;
				case 224: goto case 22;
				case 226: goto case 1;
				case 227: goto case 6;
				case 228: goto case 9;
				case 229: goto case 20;
				case 230: goto case 22;
				case 232: goto case 1;
				case 233: goto case 6;
				case 234: goto case 9;
				case 235: goto case 20;
				case 236: goto case 22;
				case 238: goto case 1;
				case 239: goto case 6;
				case 240: goto case 9;
				case 241: goto case 20;
				case 242: goto case 22;
				case 244: goto case 1;
				case 245: goto case 6;
				case 246: goto case 9;
				case 247: goto case 20;
				case 248: goto case 22;
				case 250: goto case 1;
				case 251: goto case 6;
				case 252: goto case 9;
				case 253: goto case 20;
				case 254: goto case 22;
				case 256: goto case 1;
				case 257: goto case 9;
				case 258: goto case 20;
				case 259: goto case 22;
				case 261: goto case 1;
				case 262: goto case 9;
				case 263: goto case 20;
				case 264: goto case 22;
				case 266: goto case 1;
				case 267: goto case 9;
				case 268: goto case 22;
				case 270: goto case 1;
				case 271: goto case 9;
				case 272: goto case 22;
				case 274: goto case 1;
				case 275: goto case 9;
				case 276: goto case 22;
				case 278: goto case 1;
				case 279: goto case 9;
				case 280: goto case 22;
				case 282: goto case 1;
				case 283: goto case 9;
				case 284: goto case 22;
				case 286: goto case 1;
				case 287: goto case 9;
				case 289: goto case 1;
				case 290: goto case 9;
				case 292: goto case 1;
				case 293: goto case 9;
				case 295: goto case 1;
				case 296: goto case 9;
				case 298: goto case 1;
				case 299: goto case 9;
				case 301: goto case 1;
				case 302: goto case 9;
				case 304: goto case 1;
				case 305: goto case 9;
				case 307: goto case 1;
				case 308: goto case 9;
				case 310: goto case 1;
				case 311: goto case 9;
				case 313: goto case 1;
				case 314: goto case 9;
				case 316: goto case 1;
				case 317: goto case 9;
				case 319: goto case 1;
				case 320: goto case 9;
				case 322: goto case 1;
				case 686: goto case 1;
				case 687: goto case 6;
				case 688: goto case 7;
				case 689: goto case 11;
				case 690: goto case 29;
				case 691: goto case 30;
				case 692: goto case 41;
				case 693: goto case 1;
				case 694: goto case 4;
				case 695: goto case 9;
				case 696: goto case 20;
				case 697: goto case 21;
				case 698: goto case 41;
				case 700: goto case 12;
				case 701: goto case 19;
				case 702: goto case 1;
				case 703: goto case 9;
				case 704: goto case 10;
				case 705: goto case 22;
				case 706: goto case 3;
				case 707: goto case 7;
				case 708: goto case 4;
				case 709: goto case 6;
				case 710: goto case 9;
				case 711: goto case 19;
				case 712: goto case 20;
				case 713: goto case 22;
				case 714: goto case 4;
				case 715: goto case 9;
				case 716: goto case 20;
				case 717: goto case 1;
				case 718: goto case 22;
				case 719: goto case 1;
				case 720: goto case 9;
				case 721: goto case 6;
				case 722: goto case 9;
				case 723: goto case 9;
				case 724: goto case 22;
				case 725: goto case 9;
				case 727: goto case 9;
				case 728: goto case 22;
				case 730: goto case 9;
				case 731: goto case 20;
				case 733: goto case 6;
				case 734: goto case 20;
				case 735: goto case 22;
				case 736: goto case 6;
				case 738: goto case 22;
				case 740: goto case 22;
				case 742: goto case 9;
				case 743: goto case 1;
				case 744: goto case 1;
				case 745: goto case 9;
				case 794: goto case 1;
				case 795: goto case 6;
				case 796: goto case 7;
				case 797: goto case 11;
				case 798: goto case 41;
				case 799: goto case 9;
				case 801: goto case 1;
				case 802: goto case 22;
				case 803: goto case 3;
				case 804: goto case 6;
				case 805: goto case 22;
				case 806: goto case 9;
				case 807: goto case 1;
				case 808: goto case 9;
				case 809: goto case 9;
				case 810: goto case 9;
				case 812: goto case 6;
				case 814: goto case 1;
				case 832: goto case 1;
				case 833: goto case 6;
				case 835: goto case 1;
				case 836: goto case 3;
				case 837: goto case 6;
				case 838: goto case 9;
				case 839: goto case 9;
				case 841: goto case 1;
				case 848: goto case 1;
				case 850: goto case 1;
				case 851: goto case 6;
				case 853: goto case 1;
				case 858: goto case 1;
				case 859: goto case 1;
				case 860: goto case 6;
				case 862: goto case 1;
				case 866: goto case 1;
				case 867: goto case 1;
				case 871: goto case 1;
				case 872: goto case 1;
				case 876: goto case 1;
				case 880: goto case 1;
				case 898: goto case 7;
				case 899: goto case 8;
				case 900: goto case 9;
				case 901: goto case 11;
				case 902: goto case 29;
				case 903: goto case 30;
				case 904: goto case 4;
				case 905: goto case 9;
				case 906: goto case 20;
				case 907: goto case 29;
				case 908: goto case 1;
				case 909: goto case 9;
				case 910: goto case 10;
				case 911: goto case 22;
				case 912: goto case 1;
				case 913: goto case 9;
				case 914: goto case 1;
				case 915: goto case 6;
				case 916: goto case 1;
				case 925: goto case 1;
				case 926: goto case 1;
				case 927: goto case 4;
				case 928: goto case 4;
				case 929: goto case 1;
				case 930: goto case 8;
				case 931: goto case 9;
				case 932: goto case 11;
				case 933: goto case 29;
				case 934: goto case 29;
				case 935: goto case 1;
				case 936: goto case 9;
				case 937: goto case 22;
				case 938: goto case 9;
				case 939: goto case 6;
				case 940: goto case 1;
				case 943: goto case 1;
				case 944: goto case 1;
				case 945: goto case 11;
				case 946: goto case 1;
				case 947: goto case 1;
				case 950: goto case 11;
				case 951: goto case 1;
				case 952: goto case 1;
				case 954: goto case 1;
				case 955: goto case 1;
				case 957: goto case 1;
				case 959: goto case 1;
				case 966: goto case 7;
				case 967: goto case 4;
				case 968: goto case 1;
				case 969: goto case 4;
				case 970: goto case 4;
				case 971: goto case 4;
				case 972: goto case 4;
				case 973: goto case 4;
				case 974: goto case 4;
				case 975: goto case 4;
				case 976: goto case 4;
				case 977: goto case 4;
				case 978: goto case 1;
				case 979: goto case 1;
				case 992: goto case 1;
				case 996: goto case 1;
				case 998: goto case 1;
				case 999: goto case 8;
				case 1000: goto case 29;
				case 1002: goto case 7;
				case 1003: goto case 1;
				case 1005: goto case 1;
				case 1006: goto case 8;
				case 1007: goto case 29;
				case 1008: goto case 1;
				case 1010: goto case 1;
				case 1011: goto case 1;
				case 1013: goto case 1;
				case 1014: goto case 1;
				case 1016: goto case 1;
				case 1018: goto case 1;
				case 1020: goto case 1;
				case 1022: goto case 7;
				case 1023: goto case 1;
				case 1027: goto case 1;
				case 1029: goto case 1;
				case 1030: goto case 29;
				case 1031: goto case 7;
				case 1032: goto case 1;
				case 1033: goto case 1;
				case 1035: goto case 1;
				case 1036: goto case 1;
				case 1038: goto case 1;
				case 1039: goto case 1;
				case 1040: goto case 1;
				case 1042: goto case 1;
				case 1043: goto case 1;
				case 1044: goto case 1;
				case 1045: goto case 1;
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
			AcceptConditions.Accept, // 39
			AcceptConditions.Accept, // 40
			AcceptConditions.Accept, // 41
			AcceptConditions.NotAccept, // 42
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
			AcceptConditions.Accept, // 63
			AcceptConditions.Accept, // 64
			AcceptConditions.Accept, // 65
			AcceptConditions.Accept, // 66
			AcceptConditions.Accept, // 67
			AcceptConditions.NotAccept, // 68
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
			AcceptConditions.Accept, // 81
			AcceptConditions.Accept, // 82
			AcceptConditions.Accept, // 83
			AcceptConditions.Accept, // 84
			AcceptConditions.Accept, // 85
			AcceptConditions.Accept, // 86
			AcceptConditions.Accept, // 87
			AcceptConditions.NotAccept, // 88
			AcceptConditions.Accept, // 89
			AcceptConditions.Accept, // 90
			AcceptConditions.Accept, // 91
			AcceptConditions.Accept, // 92
			AcceptConditions.Accept, // 93
			AcceptConditions.Accept, // 94
			AcceptConditions.Accept, // 95
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
			AcceptConditions.NotAccept, // 106
			AcceptConditions.Accept, // 107
			AcceptConditions.Accept, // 108
			AcceptConditions.Accept, // 109
			AcceptConditions.Accept, // 110
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
			AcceptConditions.Accept, // 134
			AcceptConditions.Accept, // 135
			AcceptConditions.Accept, // 136
			AcceptConditions.Accept, // 137
			AcceptConditions.Accept, // 138
			AcceptConditions.NotAccept, // 139
			AcceptConditions.Accept, // 140
			AcceptConditions.Accept, // 141
			AcceptConditions.Accept, // 142
			AcceptConditions.Accept, // 143
			AcceptConditions.Accept, // 144
			AcceptConditions.Accept, // 145
			AcceptConditions.Accept, // 146
			AcceptConditions.Accept, // 147
			AcceptConditions.Accept, // 148
			AcceptConditions.Accept, // 149
			AcceptConditions.Accept, // 150
			AcceptConditions.NotAccept, // 151
			AcceptConditions.Accept, // 152
			AcceptConditions.Accept, // 153
			AcceptConditions.Accept, // 154
			AcceptConditions.Accept, // 155
			AcceptConditions.Accept, // 156
			AcceptConditions.Accept, // 157
			AcceptConditions.Accept, // 158
			AcceptConditions.Accept, // 159
			AcceptConditions.Accept, // 160
			AcceptConditions.Accept, // 161
			AcceptConditions.NotAccept, // 162
			AcceptConditions.Accept, // 163
			AcceptConditions.Accept, // 164
			AcceptConditions.Accept, // 165
			AcceptConditions.Accept, // 166
			AcceptConditions.Accept, // 167
			AcceptConditions.Accept, // 168
			AcceptConditions.Accept, // 169
			AcceptConditions.Accept, // 170
			AcceptConditions.Accept, // 171
			AcceptConditions.Accept, // 172
			AcceptConditions.NotAccept, // 173
			AcceptConditions.Accept, // 174
			AcceptConditions.Accept, // 175
			AcceptConditions.Accept, // 176
			AcceptConditions.Accept, // 177
			AcceptConditions.Accept, // 178
			AcceptConditions.Accept, // 179
			AcceptConditions.Accept, // 180
			AcceptConditions.Accept, // 181
			AcceptConditions.Accept, // 182
			AcceptConditions.Accept, // 183
			AcceptConditions.NotAccept, // 184
			AcceptConditions.Accept, // 185
			AcceptConditions.Accept, // 186
			AcceptConditions.Accept, // 187
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
			AcceptConditions.Accept, // 200
			AcceptConditions.Accept, // 201
			AcceptConditions.Accept, // 202
			AcceptConditions.Accept, // 203
			AcceptConditions.NotAccept, // 204
			AcceptConditions.Accept, // 205
			AcceptConditions.Accept, // 206
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
			AcceptConditions.Accept, // 218
			AcceptConditions.NotAccept, // 219
			AcceptConditions.Accept, // 220
			AcceptConditions.Accept, // 221
			AcceptConditions.Accept, // 222
			AcceptConditions.Accept, // 223
			AcceptConditions.Accept, // 224
			AcceptConditions.NotAccept, // 225
			AcceptConditions.Accept, // 226
			AcceptConditions.Accept, // 227
			AcceptConditions.Accept, // 228
			AcceptConditions.Accept, // 229
			AcceptConditions.Accept, // 230
			AcceptConditions.NotAccept, // 231
			AcceptConditions.Accept, // 232
			AcceptConditions.Accept, // 233
			AcceptConditions.Accept, // 234
			AcceptConditions.Accept, // 235
			AcceptConditions.Accept, // 236
			AcceptConditions.NotAccept, // 237
			AcceptConditions.Accept, // 238
			AcceptConditions.Accept, // 239
			AcceptConditions.Accept, // 240
			AcceptConditions.Accept, // 241
			AcceptConditions.Accept, // 242
			AcceptConditions.NotAccept, // 243
			AcceptConditions.Accept, // 244
			AcceptConditions.Accept, // 245
			AcceptConditions.Accept, // 246
			AcceptConditions.Accept, // 247
			AcceptConditions.Accept, // 248
			AcceptConditions.NotAccept, // 249
			AcceptConditions.Accept, // 250
			AcceptConditions.Accept, // 251
			AcceptConditions.Accept, // 252
			AcceptConditions.Accept, // 253
			AcceptConditions.Accept, // 254
			AcceptConditions.NotAccept, // 255
			AcceptConditions.Accept, // 256
			AcceptConditions.Accept, // 257
			AcceptConditions.Accept, // 258
			AcceptConditions.Accept, // 259
			AcceptConditions.NotAccept, // 260
			AcceptConditions.Accept, // 261
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
			AcceptConditions.Accept, // 272
			AcceptConditions.NotAccept, // 273
			AcceptConditions.Accept, // 274
			AcceptConditions.Accept, // 275
			AcceptConditions.Accept, // 276
			AcceptConditions.NotAccept, // 277
			AcceptConditions.Accept, // 278
			AcceptConditions.Accept, // 279
			AcceptConditions.Accept, // 280
			AcceptConditions.NotAccept, // 281
			AcceptConditions.Accept, // 282
			AcceptConditions.Accept, // 283
			AcceptConditions.Accept, // 284
			AcceptConditions.NotAccept, // 285
			AcceptConditions.Accept, // 286
			AcceptConditions.Accept, // 287
			AcceptConditions.NotAccept, // 288
			AcceptConditions.Accept, // 289
			AcceptConditions.Accept, // 290
			AcceptConditions.NotAccept, // 291
			AcceptConditions.Accept, // 292
			AcceptConditions.Accept, // 293
			AcceptConditions.NotAccept, // 294
			AcceptConditions.Accept, // 295
			AcceptConditions.Accept, // 296
			AcceptConditions.NotAccept, // 297
			AcceptConditions.Accept, // 298
			AcceptConditions.Accept, // 299
			AcceptConditions.NotAccept, // 300
			AcceptConditions.Accept, // 301
			AcceptConditions.Accept, // 302
			AcceptConditions.NotAccept, // 303
			AcceptConditions.Accept, // 304
			AcceptConditions.Accept, // 305
			AcceptConditions.NotAccept, // 306
			AcceptConditions.Accept, // 307
			AcceptConditions.Accept, // 308
			AcceptConditions.NotAccept, // 309
			AcceptConditions.Accept, // 310
			AcceptConditions.Accept, // 311
			AcceptConditions.NotAccept, // 312
			AcceptConditions.Accept, // 313
			AcceptConditions.Accept, // 314
			AcceptConditions.NotAccept, // 315
			AcceptConditions.Accept, // 316
			AcceptConditions.Accept, // 317
			AcceptConditions.NotAccept, // 318
			AcceptConditions.Accept, // 319
			AcceptConditions.Accept, // 320
			AcceptConditions.NotAccept, // 321
			AcceptConditions.Accept, // 322
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
			AcceptConditions.NotAccept, // 629
			AcceptConditions.NotAccept, // 630
			AcceptConditions.NotAccept, // 631
			AcceptConditions.NotAccept, // 632
			AcceptConditions.NotAccept, // 633
			AcceptConditions.NotAccept, // 634
			AcceptConditions.NotAccept, // 635
			AcceptConditions.NotAccept, // 636
			AcceptConditions.NotAccept, // 637
			AcceptConditions.NotAccept, // 638
			AcceptConditions.NotAccept, // 639
			AcceptConditions.NotAccept, // 640
			AcceptConditions.NotAccept, // 641
			AcceptConditions.NotAccept, // 642
			AcceptConditions.NotAccept, // 643
			AcceptConditions.NotAccept, // 644
			AcceptConditions.NotAccept, // 645
			AcceptConditions.NotAccept, // 646
			AcceptConditions.NotAccept, // 647
			AcceptConditions.NotAccept, // 648
			AcceptConditions.NotAccept, // 649
			AcceptConditions.NotAccept, // 650
			AcceptConditions.NotAccept, // 651
			AcceptConditions.NotAccept, // 652
			AcceptConditions.NotAccept, // 653
			AcceptConditions.NotAccept, // 654
			AcceptConditions.NotAccept, // 655
			AcceptConditions.NotAccept, // 656
			AcceptConditions.NotAccept, // 657
			AcceptConditions.NotAccept, // 658
			AcceptConditions.NotAccept, // 659
			AcceptConditions.NotAccept, // 660
			AcceptConditions.NotAccept, // 661
			AcceptConditions.NotAccept, // 662
			AcceptConditions.NotAccept, // 663
			AcceptConditions.NotAccept, // 664
			AcceptConditions.NotAccept, // 665
			AcceptConditions.NotAccept, // 666
			AcceptConditions.NotAccept, // 667
			AcceptConditions.NotAccept, // 668
			AcceptConditions.NotAccept, // 669
			AcceptConditions.NotAccept, // 670
			AcceptConditions.NotAccept, // 671
			AcceptConditions.NotAccept, // 672
			AcceptConditions.NotAccept, // 673
			AcceptConditions.NotAccept, // 674
			AcceptConditions.NotAccept, // 675
			AcceptConditions.NotAccept, // 676
			AcceptConditions.NotAccept, // 677
			AcceptConditions.NotAccept, // 678
			AcceptConditions.NotAccept, // 679
			AcceptConditions.NotAccept, // 680
			AcceptConditions.NotAccept, // 681
			AcceptConditions.NotAccept, // 682
			AcceptConditions.NotAccept, // 683
			AcceptConditions.NotAccept, // 684
			AcceptConditions.NotAccept, // 685
			AcceptConditions.Accept, // 686
			AcceptConditions.Accept, // 687
			AcceptConditions.Accept, // 688
			AcceptConditions.Accept, // 689
			AcceptConditions.Accept, // 690
			AcceptConditions.Accept, // 691
			AcceptConditions.Accept, // 692
			AcceptConditions.Accept, // 693
			AcceptConditions.Accept, // 694
			AcceptConditions.Accept, // 695
			AcceptConditions.Accept, // 696
			AcceptConditions.Accept, // 697
			AcceptConditions.Accept, // 698
			AcceptConditions.NotAccept, // 699
			AcceptConditions.Accept, // 700
			AcceptConditions.Accept, // 701
			AcceptConditions.Accept, // 702
			AcceptConditions.Accept, // 703
			AcceptConditions.Accept, // 704
			AcceptConditions.Accept, // 705
			AcceptConditions.Accept, // 706
			AcceptConditions.Accept, // 707
			AcceptConditions.Accept, // 708
			AcceptConditions.Accept, // 709
			AcceptConditions.Accept, // 710
			AcceptConditions.Accept, // 711
			AcceptConditions.Accept, // 712
			AcceptConditions.Accept, // 713
			AcceptConditions.Accept, // 714
			AcceptConditions.Accept, // 715
			AcceptConditions.Accept, // 716
			AcceptConditions.Accept, // 717
			AcceptConditions.Accept, // 718
			AcceptConditions.Accept, // 719
			AcceptConditions.Accept, // 720
			AcceptConditions.Accept, // 721
			AcceptConditions.Accept, // 722
			AcceptConditions.Accept, // 723
			AcceptConditions.Accept, // 724
			AcceptConditions.Accept, // 725
			AcceptConditions.NotAccept, // 726
			AcceptConditions.Accept, // 727
			AcceptConditions.Accept, // 728
			AcceptConditions.NotAccept, // 729
			AcceptConditions.Accept, // 730
			AcceptConditions.Accept, // 731
			AcceptConditions.NotAccept, // 732
			AcceptConditions.Accept, // 733
			AcceptConditions.Accept, // 734
			AcceptConditions.Accept, // 735
			AcceptConditions.Accept, // 736
			AcceptConditions.NotAccept, // 737
			AcceptConditions.Accept, // 738
			AcceptConditions.NotAccept, // 739
			AcceptConditions.Accept, // 740
			AcceptConditions.NotAccept, // 741
			AcceptConditions.Accept, // 742
			AcceptConditions.Accept, // 743
			AcceptConditions.Accept, // 744
			AcceptConditions.Accept, // 745
			AcceptConditions.NotAccept, // 746
			AcceptConditions.NotAccept, // 747
			AcceptConditions.NotAccept, // 748
			AcceptConditions.NotAccept, // 749
			AcceptConditions.NotAccept, // 750
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
			AcceptConditions.NotAccept, // 763
			AcceptConditions.NotAccept, // 764
			AcceptConditions.NotAccept, // 765
			AcceptConditions.NotAccept, // 766
			AcceptConditions.NotAccept, // 767
			AcceptConditions.NotAccept, // 768
			AcceptConditions.NotAccept, // 769
			AcceptConditions.NotAccept, // 770
			AcceptConditions.NotAccept, // 771
			AcceptConditions.NotAccept, // 772
			AcceptConditions.NotAccept, // 773
			AcceptConditions.NotAccept, // 774
			AcceptConditions.NotAccept, // 775
			AcceptConditions.NotAccept, // 776
			AcceptConditions.NotAccept, // 777
			AcceptConditions.NotAccept, // 778
			AcceptConditions.NotAccept, // 779
			AcceptConditions.NotAccept, // 780
			AcceptConditions.NotAccept, // 781
			AcceptConditions.NotAccept, // 782
			AcceptConditions.NotAccept, // 783
			AcceptConditions.NotAccept, // 784
			AcceptConditions.NotAccept, // 785
			AcceptConditions.NotAccept, // 786
			AcceptConditions.NotAccept, // 787
			AcceptConditions.NotAccept, // 788
			AcceptConditions.NotAccept, // 789
			AcceptConditions.NotAccept, // 790
			AcceptConditions.NotAccept, // 791
			AcceptConditions.NotAccept, // 792
			AcceptConditions.NotAccept, // 793
			AcceptConditions.Accept, // 794
			AcceptConditions.Accept, // 795
			AcceptConditions.Accept, // 796
			AcceptConditions.Accept, // 797
			AcceptConditions.Accept, // 798
			AcceptConditions.Accept, // 799
			AcceptConditions.NotAccept, // 800
			AcceptConditions.Accept, // 801
			AcceptConditions.Accept, // 802
			AcceptConditions.Accept, // 803
			AcceptConditions.Accept, // 804
			AcceptConditions.Accept, // 805
			AcceptConditions.Accept, // 806
			AcceptConditions.Accept, // 807
			AcceptConditions.Accept, // 808
			AcceptConditions.Accept, // 809
			AcceptConditions.Accept, // 810
			AcceptConditions.NotAccept, // 811
			AcceptConditions.Accept, // 812
			AcceptConditions.NotAccept, // 813
			AcceptConditions.Accept, // 814
			AcceptConditions.NotAccept, // 815
			AcceptConditions.NotAccept, // 816
			AcceptConditions.NotAccept, // 817
			AcceptConditions.NotAccept, // 818
			AcceptConditions.NotAccept, // 819
			AcceptConditions.NotAccept, // 820
			AcceptConditions.NotAccept, // 821
			AcceptConditions.NotAccept, // 822
			AcceptConditions.NotAccept, // 823
			AcceptConditions.NotAccept, // 824
			AcceptConditions.NotAccept, // 825
			AcceptConditions.NotAccept, // 826
			AcceptConditions.NotAccept, // 827
			AcceptConditions.NotAccept, // 828
			AcceptConditions.NotAccept, // 829
			AcceptConditions.NotAccept, // 830
			AcceptConditions.NotAccept, // 831
			AcceptConditions.Accept, // 832
			AcceptConditions.Accept, // 833
			AcceptConditions.NotAccept, // 834
			AcceptConditions.Accept, // 835
			AcceptConditions.Accept, // 836
			AcceptConditions.Accept, // 837
			AcceptConditions.Accept, // 838
			AcceptConditions.Accept, // 839
			AcceptConditions.NotAccept, // 840
			AcceptConditions.Accept, // 841
			AcceptConditions.NotAccept, // 842
			AcceptConditions.NotAccept, // 843
			AcceptConditions.NotAccept, // 844
			AcceptConditions.NotAccept, // 845
			AcceptConditions.NotAccept, // 846
			AcceptConditions.NotAccept, // 847
			AcceptConditions.Accept, // 848
			AcceptConditions.NotAccept, // 849
			AcceptConditions.Accept, // 850
			AcceptConditions.Accept, // 851
			AcceptConditions.NotAccept, // 852
			AcceptConditions.Accept, // 853
			AcceptConditions.NotAccept, // 854
			AcceptConditions.NotAccept, // 855
			AcceptConditions.NotAccept, // 856
			AcceptConditions.NotAccept, // 857
			AcceptConditions.Accept, // 858
			AcceptConditions.Accept, // 859
			AcceptConditions.Accept, // 860
			AcceptConditions.NotAccept, // 861
			AcceptConditions.Accept, // 862
			AcceptConditions.NotAccept, // 863
			AcceptConditions.NotAccept, // 864
			AcceptConditions.NotAccept, // 865
			AcceptConditions.Accept, // 866
			AcceptConditions.Accept, // 867
			AcceptConditions.NotAccept, // 868
			AcceptConditions.NotAccept, // 869
			AcceptConditions.NotAccept, // 870
			AcceptConditions.Accept, // 871
			AcceptConditions.Accept, // 872
			AcceptConditions.NotAccept, // 873
			AcceptConditions.NotAccept, // 874
			AcceptConditions.NotAccept, // 875
			AcceptConditions.Accept, // 876
			AcceptConditions.NotAccept, // 877
			AcceptConditions.NotAccept, // 878
			AcceptConditions.NotAccept, // 879
			AcceptConditions.Accept, // 880
			AcceptConditions.NotAccept, // 881
			AcceptConditions.NotAccept, // 882
			AcceptConditions.NotAccept, // 883
			AcceptConditions.NotAccept, // 884
			AcceptConditions.NotAccept, // 885
			AcceptConditions.NotAccept, // 886
			AcceptConditions.NotAccept, // 887
			AcceptConditions.NotAccept, // 888
			AcceptConditions.NotAccept, // 889
			AcceptConditions.NotAccept, // 890
			AcceptConditions.NotAccept, // 891
			AcceptConditions.NotAccept, // 892
			AcceptConditions.NotAccept, // 893
			AcceptConditions.NotAccept, // 894
			AcceptConditions.NotAccept, // 895
			AcceptConditions.NotAccept, // 896
			AcceptConditions.NotAccept, // 897
			AcceptConditions.Accept, // 898
			AcceptConditions.Accept, // 899
			AcceptConditions.Accept, // 900
			AcceptConditions.Accept, // 901
			AcceptConditions.Accept, // 902
			AcceptConditions.Accept, // 903
			AcceptConditions.Accept, // 904
			AcceptConditions.Accept, // 905
			AcceptConditions.Accept, // 906
			AcceptConditions.Accept, // 907
			AcceptConditions.Accept, // 908
			AcceptConditions.Accept, // 909
			AcceptConditions.Accept, // 910
			AcceptConditions.Accept, // 911
			AcceptConditions.Accept, // 912
			AcceptConditions.Accept, // 913
			AcceptConditions.Accept, // 914
			AcceptConditions.Accept, // 915
			AcceptConditions.Accept, // 916
			AcceptConditions.NotAccept, // 917
			AcceptConditions.NotAccept, // 918
			AcceptConditions.NotAccept, // 919
			AcceptConditions.NotAccept, // 920
			AcceptConditions.NotAccept, // 921
			AcceptConditions.NotAccept, // 922
			AcceptConditions.NotAccept, // 923
			AcceptConditions.NotAccept, // 924
			AcceptConditions.Accept, // 925
			AcceptConditions.Accept, // 926
			AcceptConditions.Accept, // 927
			AcceptConditions.Accept, // 928
			AcceptConditions.Accept, // 929
			AcceptConditions.Accept, // 930
			AcceptConditions.Accept, // 931
			AcceptConditions.Accept, // 932
			AcceptConditions.Accept, // 933
			AcceptConditions.Accept, // 934
			AcceptConditions.Accept, // 935
			AcceptConditions.Accept, // 936
			AcceptConditions.Accept, // 937
			AcceptConditions.Accept, // 938
			AcceptConditions.Accept, // 939
			AcceptConditions.Accept, // 940
			AcceptConditions.NotAccept, // 941
			AcceptConditions.NotAccept, // 942
			AcceptConditions.Accept, // 943
			AcceptConditions.Accept, // 944
			AcceptConditions.Accept, // 945
			AcceptConditions.Accept, // 946
			AcceptConditions.Accept, // 947
			AcceptConditions.NotAccept, // 948
			AcceptConditions.NotAccept, // 949
			AcceptConditions.Accept, // 950
			AcceptConditions.Accept, // 951
			AcceptConditions.Accept, // 952
			AcceptConditions.NotAccept, // 953
			AcceptConditions.Accept, // 954
			AcceptConditions.Accept, // 955
			AcceptConditions.NotAccept, // 956
			AcceptConditions.Accept, // 957
			AcceptConditions.NotAccept, // 958
			AcceptConditions.Accept, // 959
			AcceptConditions.NotAccept, // 960
			AcceptConditions.NotAccept, // 961
			AcceptConditions.NotAccept, // 962
			AcceptConditions.NotAccept, // 963
			AcceptConditions.NotAccept, // 964
			AcceptConditions.NotAccept, // 965
			AcceptConditions.Accept, // 966
			AcceptConditions.Accept, // 967
			AcceptConditions.Accept, // 968
			AcceptConditions.Accept, // 969
			AcceptConditions.Accept, // 970
			AcceptConditions.Accept, // 971
			AcceptConditions.Accept, // 972
			AcceptConditions.Accept, // 973
			AcceptConditions.Accept, // 974
			AcceptConditions.Accept, // 975
			AcceptConditions.Accept, // 976
			AcceptConditions.Accept, // 977
			AcceptConditions.Accept, // 978
			AcceptConditions.Accept, // 979
			AcceptConditions.NotAccept, // 980
			AcceptConditions.NotAccept, // 981
			AcceptConditions.NotAccept, // 982
			AcceptConditions.NotAccept, // 983
			AcceptConditions.NotAccept, // 984
			AcceptConditions.NotAccept, // 985
			AcceptConditions.NotAccept, // 986
			AcceptConditions.NotAccept, // 987
			AcceptConditions.NotAccept, // 988
			AcceptConditions.NotAccept, // 989
			AcceptConditions.NotAccept, // 990
			AcceptConditions.NotAccept, // 991
			AcceptConditions.Accept, // 992
			AcceptConditions.NotAccept, // 993
			AcceptConditions.NotAccept, // 994
			AcceptConditions.NotAccept, // 995
			AcceptConditions.Accept, // 996
			AcceptConditions.NotAccept, // 997
			AcceptConditions.Accept, // 998
			AcceptConditions.Accept, // 999
			AcceptConditions.Accept, // 1000
			AcceptConditions.NotAccept, // 1001
			AcceptConditions.Accept, // 1002
			AcceptConditions.Accept, // 1003
			AcceptConditions.NotAccept, // 1004
			AcceptConditions.Accept, // 1005
			AcceptConditions.Accept, // 1006
			AcceptConditions.Accept, // 1007
			AcceptConditions.Accept, // 1008
			AcceptConditions.NotAccept, // 1009
			AcceptConditions.Accept, // 1010
			AcceptConditions.Accept, // 1011
			AcceptConditions.NotAccept, // 1012
			AcceptConditions.Accept, // 1013
			AcceptConditions.Accept, // 1014
			AcceptConditions.NotAccept, // 1015
			AcceptConditions.Accept, // 1016
			AcceptConditions.NotAccept, // 1017
			AcceptConditions.Accept, // 1018
			AcceptConditions.NotAccept, // 1019
			AcceptConditions.Accept, // 1020
			AcceptConditions.NotAccept, // 1021
			AcceptConditions.Accept, // 1022
			AcceptConditions.Accept, // 1023
			AcceptConditions.NotAccept, // 1024
			AcceptConditions.NotAccept, // 1025
			AcceptConditions.NotAccept, // 1026
			AcceptConditions.Accept, // 1027
			AcceptConditions.NotAccept, // 1028
			AcceptConditions.Accept, // 1029
			AcceptConditions.Accept, // 1030
			AcceptConditions.Accept, // 1031
			AcceptConditions.Accept, // 1032
			AcceptConditions.Accept, // 1033
			AcceptConditions.NotAccept, // 1034
			AcceptConditions.Accept, // 1035
			AcceptConditions.Accept, // 1036
			AcceptConditions.NotAccept, // 1037
			AcceptConditions.Accept, // 1038
			AcceptConditions.Accept, // 1039
			AcceptConditions.Accept, // 1040
			AcceptConditions.NotAccept, // 1041
			AcceptConditions.Accept, // 1042
			AcceptConditions.Accept, // 1043
			AcceptConditions.Accept, // 1044
			AcceptConditions.Accept, // 1045
			AcceptConditions.NotAccept, // 1046
		};
		
		private static int[] colMap = new int[]
		{
			2, 3, 3, 3, 3, 3, 3, 3, 3, 4, 2, 3, 3, 2, 3, 3, 
			3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 
			5, 3, 3, 3, 3, 3, 3, 3, 6, 7, 3, 8, 9, 10, 11, 12, 
			13, 14, 15, 16, 17, 18, 19, 20, 21, 21, 22, 3, 3, 3, 3, 3, 
			23, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 
			3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 24, 
			3, 25, 26, 27, 28, 29, 1, 30, 31, 32, 33, 34, 35, 36, 37, 38, 
			39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 40, 3, 3, 3, 3, 3, 
			3, 3
		};
		
		private static int[] rowMap = new int[]
		{
			0, 1, 2, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 12, 13, 
			14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 2, 
			29, 2, 21, 2, 21, 30, 2, 2, 31, 32, 33, 34, 33, 35, 36, 37, 
			38, 21, 39, 40, 2, 2, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 
			2, 51, 52, 53, 25, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 2, 
			64, 65, 66, 67, 68, 2, 2, 2, 69, 70, 71, 72, 73, 74, 75, 76, 
			11, 77, 78, 79, 80, 81, 82, 83, 84, 31, 85, 2, 86, 87, 88, 89, 
			90, 91, 92, 93, 94, 2, 95, 25, 2, 96, 97, 98, 99, 100, 101, 102, 
			103, 104, 2, 105, 106, 107, 108, 109, 2, 110, 111, 112, 113, 114, 115, 116, 
			117, 2, 118, 119, 120, 121, 122, 123, 124, 125, 126, 2, 127, 128, 129, 130, 
			131, 132, 133, 134, 135, 136, 137, 138, 139, 140, 141, 142, 143, 144, 145, 146, 
			147, 148, 2, 149, 150, 151, 152, 153, 154, 155, 156, 157, 158, 159, 160, 161, 
			162, 163, 164, 165, 166, 167, 168, 169, 170, 171, 172, 173, 174, 175, 176, 177, 
			178, 179, 180, 2, 181, 182, 183, 184, 185, 186, 187, 188, 189, 19, 190, 191, 
			192, 193, 194, 45, 195, 196, 197, 198, 199, 200, 201, 202, 203, 204, 205, 206, 
			207, 208, 209, 210, 211, 212, 213, 214, 215, 216, 217, 2, 218, 219, 220, 221, 
			222, 223, 224, 225, 226, 227, 22, 228, 229, 230, 231, 232, 233, 234, 235, 236, 
			192, 237, 238, 239, 240, 241, 242, 243, 220, 244, 245, 246, 225, 247, 248, 249, 
			250, 25, 251, 252, 253, 254, 255, 256, 257, 258, 16, 259, 260, 261, 262, 263, 
			264, 265, 266, 267, 268, 269, 270, 207, 271, 272, 273, 274, 89, 254, 275, 276, 
			257, 277, 278, 279, 280, 281, 282, 283, 284, 285, 286, 287, 288, 289, 290, 291, 
			292, 293, 294, 56, 295, 296, 297, 298, 299, 300, 301, 302, 303, 304, 305, 306, 
			307, 308, 309, 310, 311, 312, 313, 314, 315, 316, 317, 318, 319, 320, 321, 322, 
			323, 324, 325, 326, 327, 328, 329, 330, 331, 332, 333, 334, 335, 336, 337, 338, 
			339, 340, 341, 342, 343, 344, 345, 346, 347, 348, 349, 350, 351, 352, 353, 354, 
			355, 356, 357, 21, 358, 359, 360, 361, 362, 363, 364, 365, 366, 367, 368, 369, 
			370, 371, 372, 373, 374, 375, 376, 377, 378, 379, 380, 381, 382, 383, 384, 385, 
			386, 387, 388, 389, 390, 391, 392, 393, 394, 395, 396, 397, 398, 399, 400, 401, 
			402, 403, 404, 405, 406, 407, 408, 409, 410, 411, 412, 413, 414, 415, 416, 417, 
			418, 419, 420, 421, 422, 423, 424, 425, 426, 427, 428, 429, 430, 431, 24, 48, 
			66, 82, 432, 433, 434, 435, 436, 437, 438, 439, 219, 440, 441, 442, 443, 444, 
			445, 446, 447, 448, 449, 450, 451, 452, 453, 454, 455, 456, 457, 458, 459, 460, 
			461, 462, 463, 464, 465, 246, 466, 467, 468, 469, 470, 471, 472, 473, 474, 475, 
			476, 477, 478, 479, 480, 481, 482, 483, 484, 485, 486, 487, 488, 489, 490, 491, 
			492, 493, 494, 495, 496, 497, 498, 499, 500, 501, 502, 503, 504, 505, 506, 507, 
			508, 509, 510, 511, 512, 513, 514, 515, 516, 517, 518, 519, 520, 521, 522, 434, 
			523, 524, 525, 526, 527, 528, 186, 529, 530, 531, 532, 533, 534, 535, 536, 537, 
			538, 539, 540, 541, 542, 543, 544, 545, 546, 547, 548, 549, 550, 551, 552, 553, 
			554, 555, 556, 557, 558, 559, 560, 561, 562, 563, 564, 565, 566, 567, 568, 569, 
			570, 571, 572, 573, 574, 575, 129, 576, 577, 578, 579, 580, 581, 582, 583, 584, 
			585, 586, 587, 588, 589, 590, 591, 592, 593, 594, 551, 187, 581, 595, 596, 588, 
			597, 598, 599, 600, 601, 602, 603, 604, 582, 605, 606, 607, 608, 609, 610, 31, 
			52, 611, 612, 602, 613, 614, 615, 616, 617, 618, 619, 620, 621, 622, 623, 624, 
			625, 626, 627, 628, 629, 630, 631, 632, 633, 634, 635, 636, 637, 638, 12, 639, 
			640, 641, 642, 643, 644, 645, 646, 647, 648, 649, 650, 651, 652, 653, 654, 655, 
			656, 657, 658, 659, 660, 661, 662, 663, 556, 664, 665, 666, 667, 634, 668, 669, 
			670, 671, 240, 672, 673, 674, 675, 676, 677, 678, 679, 680, 681, 682, 683, 684, 
			685, 686, 687, 688, 219, 689, 690, 485, 691, 692, 693, 694, 695, 696, 697, 698, 
			487, 699, 700, 701, 702, 703, 704, 705, 706, 707, 549, 708, 709, 540, 710, 711, 
			712, 713, 714, 715, 716, 596, 717, 718, 719, 720, 721, 722, 723, 724, 725, 726, 
			727, 728, 729, 730, 731, 732, 733, 734, 735, 736, 737, 738, 739, 740, 741, 742, 
			347, 22, 743, 744, 745, 746, 704, 747, 748, 749, 750, 751, 752, 596, 753, 754, 
			755, 73, 756, 757, 758, 759, 760, 761, 762, 763, 764, 765, 766, 485, 767, 768, 
			769, 770, 771, 772, 773, 774, 775, 776, 777, 778, 779, 780, 781, 782, 783, 784, 
			785, 786, 787, 788, 789, 790, 791, 792, 793, 794, 795, 796, 797, 798, 799, 800, 
			801, 802, 803, 804, 805, 806, 807, 808, 809, 810, 811, 812, 813, 814, 815, 816, 
			817, 818, 819, 820, 821, 724, 822, 823, 824, 825, 826, 827, 828, 829, 830, 831, 
			832, 833, 834, 835, 836, 837, 838, 839, 840, 841, 842, 843, 844, 845, 846, 847, 
			848, 849, 850, 851, 852, 853, 854, 855, 856, 108, 137, 857, 858, 859, 860, 861, 
			862, 863, 864, 865, 866, 867, 868, 869, 870, 871, 872, 873, 874, 875, 876, 877, 
			878, 879, 880, 881, 27, 882, 883, 884, 885, 886, 887, 888, 889, 890, 891, 892, 
			893, 894, 895, 896, 897, 898, 899, 900, 901, 902, 903, 904, 905, 906, 907, 908, 
			909, 910, 911, 912, 913, 914, 915, 916, 917, 918, 919, 920, 921, 922, 923, 924, 
			925, 926, 927, 928, 929, 930, 931, 932, 933, 934, 935, 853, 936, 937, 938, 939, 
			940, 941, 942, 943, 944, 945, 21, 946, 947, 948, 949, 950, 951, 952, 953, 954, 
			955, 956, 957, 958, 959, 960, 961
		};
		
		private static readonly int[,] nextState = new int[,]
		{
			{ -1, 1, 2, 3, 2, 44, 45, 3, 70, 2, 70, 2, 3, 90, 108, 706, 803, 836, 836, 836, 836, 836, 3, 125, 3, 686, 794, 794, 832, 929, 794, 794, 43, 848, 794, 944, 858, 866, 871, 1042, 794, 794, 876, 69, 794, 693, 880, 925, 1044 },
			{ -1, 89, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 89, 89, 89, 89, 124, 89, 89, 908, 89, 89, 89, 89, 89, 968, 89, 89, 140, 89, 89, 89, 89, 89, 89, 89 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 339, 339, -1, -1, -1, -1, 339, 339, -1, 11, 11, 11, 11, 11, 11, 11, 11, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 694, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 71, -1, 71, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 5, 5, 5, 5, 5, 5, 5, 5, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 833, -1, -1, -1, 833, -1, 833, -1, 47, 47, 47, 47, 47, 47, 47, 47, 47, -1, -1, -1, -1, -1, -1, 833, -1, -1, 833, -1, -1, -1, -1, -1, 72, -1, -1, -1, 72, 92, 110, -1, -1, -1, -1, -1 },
			{ -1, 298, -1, -1, 300, 300, -1, 107, -1, -1, 361, 300, 312, 6, 6, 6, 687, 795, 795, 795, 795, 795, -1, -1, 312, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 978, 298, 298, 298, 298, 298, 298, 298 },
			{ -1, 298, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 298, 298, 298, 841, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298 },
			{ -1, 112, -1, -1, -1, 368, 369, -1, 370, -1, 371, 372, -1, 50, 50, 50, 50, 50, 50, 50, 50, 50, 373, -1, -1, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 375, 75, 75, 75, 75, 75, 75, 75, 75, 75, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 376, -1, -1, -1, 376, 377, 378, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 901, 901, 901, 901, 901, 901, 901, 901, 901, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 53, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 298, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 759, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 410, -1, -1, 411, 412, -1, -1, -1, -1, 413, 414, 415, 416, 417, 418, 419, 420, 420, 421, 68, 68, -1, -1, -1, 757, -1, -1, 422, -1, -1, 219, 20, 758, -1, -1, 423, 760, 424, -1, -1, -1, 425, 739, -1, 57, 426, 906, 741 },
			{ -1, 410, -1, -1, 411, 412, -1, -1, -1, -1, 413, 414, 415, 699, 800, 834, 849, 68, 68, 68, 68, 68, -1, -1, -1, 757, -1, -1, 422, -1, -1, 219, 20, 758, -1, -1, 423, 760, 424, -1, -1, -1, 425, 739, -1, 57, 426, 906, 741 },
			{ -1, 403, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 701, 701, 701, 701, 701, 701, 701, 701, 701, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 451, -1, -1, -1, 451, 452, 453, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 696, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 79, -1, 79, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 401, -1, 733, 733, 733, 733, 733, 733, 733, 733, 733, 401, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 80, -1, -1, -1, 492, 493, -1, 494, -1, 494, 496, -1, 59, 59, 59, 59, 59, 59, 59, 59, 59, 496, -1, -1, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80 },
			{ -1, 403, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 517, 517, 517, 517, 517, 517, 517, 517, 517, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 521, -1, -1, 135, 148, 148, 148, 148, 148, 148, 148, 148, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 315, -1, -1, -1, 318, -1, -1, -1, -1, -1, -1, -1, 68, 27, 27, 27, 27, 27, 27, 27, 27, -1, -1, -1, -1, -1, -1, 321, -1, -1, 219, -1, -1, -1, -1, 323, -1, -1, -1, -1, -1, 324, 739, -1, -1, 265, -1, 741 },
			{ -1, 315, -1, -1, -1, 318, -1, -1, -1, -1, -1, -1, -1, 68, 68, 68, 68, 68, 68, 68, 68, 68, -1, -1, -1, -1, -1, -1, 321, -1, -1, 219, -1, -1, -1, -1, 323, -1, -1, -1, -1, -1, 324, 739, -1, -1, 265, -1, 741 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 33, 33, 33, 33, 33, 33, 33, 33, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 403, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 403, 403, 403, 894, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 903, 903, 903, 903, 903, 903, 903, 903, 903, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 315, -1, -1, -1, 318, -1, -1, -1, -1, -1, -1, -1, 68, 68, 68, 68, 68, 68, 68, 68, 68, -1, -1, -1, -1, -1, -1, 321, -1, -1, 219, -1, -1, -1, -1, 323, -1, -1, -1, -1, -1, 324, 601, -1, -1, 265, -1, 741 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 595, -1, -1, -1, 595, 596, 647, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 86, 86, 86, 86, 86, 86, 86, 86, 86, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 798, 798, 798, 798, 798, 798, 798, 798, 798, 67, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 42, -1, -1, -1, -1, -1, -1, -1, 68, 68, 68, 68, 68, 68, 68, 68, 68, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 89, -1, -1, 300, 300, -1, 107, -1, -1, 300, 300, -1, 6, 6, 6, 687, 795, 795, 795, 795, 795, -1, -1, -1, 89, 89, 89, 89, 89, 89, 89, 213, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 220, 89, 220, 89 },
			{ -1, 152, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 152, 152, 152, 152, 152, 152, 152, 152, 152, 152, 152, 152, 152, 152, 152, 152, 152, 152, 152, 152, 152, 152, 152, 152 },
			{ -1, -1, -1, -1, 339, 339, -1, -1, -1, -1, 339, 339, -1, 11, 11, 11, 11, 11, 11, 11, 11, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 904, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 833, -1, -1, -1, 833, -1, 142, -1, 154, 154, 165, 176, 176, 176, 176, 176, 176, 401, -1, -1, -1, -1, -1, 833, -1, -1, 833, -1, -1, -1, -1, -1, 72, -1, -1, -1, 72, 92, 110, -1, -1, -1, -1, -1 },
			{ -1, 298, -1, -1, 300, 300, -1, 107, -1, -1, 361, 300, 312, 6, 6, 6, 687, 795, 795, 795, 795, 795, -1, -1, 312, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 111 },
			{ -1, 112, -1, -1, -1, 368, 369, -1, 370, -1, 371, 372, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 373, -1, -1, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 375, 75, 75, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 376, -1, -1, -1, 376, 377, 378, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 178, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 513, 513, 513, 513, 513, 513, 514, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 701, 701, 817, 817, 817, 817, 817, 817, 817, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 451, -1, -1, -1, 451, 452, 453, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 906, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 733, 733, 733, 733, 733, 733, 733, 733, 733, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 80, -1, -1, -1, 492, 493, -1, 494, -1, 494, 496, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 496, -1, -1, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 521, -1, -1, 148, 148, 148, 522, 522, 522, 522, 522, 522, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 315, -1, -1, -1, 318, -1, -1, -1, -1, -1, -1, -1, 62, 83, 83, 103, 27, 27, 27, 27, 27, -1, -1, -1, -1, -1, -1, 321, -1, -1, 219, -1, -1, -1, -1, 323, -1, -1, -1, -1, -1, 324, 739, -1, -1, 265, -1, 741 },
			{ -1, 315, -1, -1, -1, 318, -1, -1, -1, -1, -1, -1, -1, 68, 32, 32, 32, 32, 32, 32, 32, 32, -1, -1, -1, -1, -1, -1, 321, -1, -1, 219, -1, -1, -1, -1, 323, -1, -1, -1, -1, -1, 324, 739, -1, -1, 265, -1, 741 },
			{ -1, 403, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 1030, 403, 403, 403, 403, 403, 403 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 85, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 86, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 698, 698, 698, 698, 698, 698, 87, 87, 87, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 89, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, -1, 303, 303, 306, 309, 309, 309, 309, 309, 309, -1, -1, -1, 89, 89, 89, 89, 954, 89, 270, 89, 89, 89, 89, 89, 89, 959, 89, 89, 89, 89, 89, 274, 89, 1032, 89, 89 },
			{ -1, -1, -1, -1, -1, 42, -1, -1, -1, -1, -1, -1, -1, 163, 163, 719, 807, 807, 807, 807, 807, 807, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 339, 339, -1, -1, -1, -1, 339, 339, -1, 11, 11, 11, 11, 11, 11, 11, 11, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 833, -1, -1, -1, 833, -1, 833, -1, 837, 837, 851, 860, 860, 860, 860, 860, 860, -1, -1, -1, -1, -1, -1, 92, -1, -1, 833, -1, -1, -1, -1, -1, 833, -1, -1, -1, 833, 833, 92, -1, -1, -1, -1, -1 },
			{ -1, 298, -1, -1, 300, 300, -1, 107, -1, -1, 361, 300, 312, 6, 6, 6, 687, 795, 795, 795, 795, 795, -1, -1, 312, 298, 298, 298, 298, 111, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298 },
			{ -1, 112, -1, -1, -1, 368, 369, -1, 370, -1, 370, 373, -1, 695, 695, 695, 695, 695, 695, 695, 695, 695, 373, -1, -1, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 375, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 376, -1, -1, -1, 376, 377, 378, -1, -1, -1, -1, -1 },
			{ -1, 715, -1, -1, -1, -1, -1, 155, -1, -1, -1, 53, 454, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 454, 715, 715, 715, 715, 715, 715, 715, 715, 715, 715, 715, 715, 715, 715, 715, 715, 715, 715, 715, 715, 715, 715, 715, 715 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 178, -1, 431, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 817, 817, 817, 817, 817, 817, 817, 817, 817, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 451, -1, -1, -1, 451, 452, 453, -1, -1, -1, -1, -1 },
			{ -1, 100, -1, -1, -1, -1, -1, 117, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 521, -1, -1, 522, 522, 522, 522, 522, 522, 522, 522, 522, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 315, -1, -1, -1, 318, -1, -1, -1, -1, -1, -1, -1, 27, 27, 27, 27, 27, 27, 27, 27, 27, -1, -1, -1, -1, -1, -1, 321, -1, -1, 219, -1, -1, -1, -1, 323, -1, -1, -1, -1, -1, 324, 739, -1, -1, 265, -1, 741 },
			{ -1, 315, -1, -1, -1, 318, -1, -1, -1, -1, -1, -1, -1, 32, 32, 32, 32, 32, 32, 32, 32, 32, -1, -1, -1, -1, -1, -1, 321, -1, -1, 219, -1, -1, -1, -1, 323, -1, -1, -1, -1, -1, 324, 739, -1, -1, 265, -1, 741 },
			{ -1, -1, -1, -1, 612, 612, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 829, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 325, -1, -1, -1, -1, -1, -1, -1, -1, 326, -1, -1, 327, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 702, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702 },
			{ -1, 88, -1, -1, 106, 123, -1, -1, -1, -1, 139, 151, 162, 173, 184, 184, 184, 184, 184, 184, 184, 184, 194, -1, -1, 204, -1, -1, 212, -1, -1, 219, 4, 225, -1, -1, 231, 237, 243, -1, -1, 249, 255, 260, -1, 46, 265, 904, 741 },
			{ -1, -1, -1, -1, 339, 339, -1, -1, -1, -1, 339, 339, -1, 11, 11, 11, 11, 11, 11, 11, 11, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 406, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 833, -1, -1, -1, 833, -1, 833, -1, 837, 837, 851, 860, 860, 860, 860, 860, 860, -1, -1, -1, -1, -1, -1, 833, -1, -1, 833, -1, -1, -1, -1, -1, 833, -1, -1, -1, 833, 833, 92, -1, -1, -1, -1, -1 },
			{ -1, 298, -1, -1, 300, 300, -1, 107, -1, -1, 361, 300, 312, 6, 6, 6, 687, 795, 795, 795, 795, 795, -1, -1, 312, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 707, 298, 298, 298, 298, 298 },
			{ -1, 112, -1, -1, -1, 368, 369, -1, 370, -1, 371, 751, -1, 129, 129, 129, 129, 129, 129, 129, 129, 129, 752, -1, -1, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 910, 910, 910, 910, 910, 910, 910, 910, 910, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 759, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 432, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 132, 132, 132, 132, 132, 132, 132, 132, 132, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 451, -1, -1, -1, 451, 452, 453, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 828, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 705, -1, -1, -1, -1, -1, 117, -1, -1, -1, -1, 579, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 579, 705, 705, 705, 705, 705, 705, 705, 705, 705, 705, 705, 705, 705, 705, 705, 705, 705, 705, 705, 705, 705, 705, 705, 705 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 521, -1, -1, 522, 522, 522, 522, 522, 522, 523, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 315, -1, -1, -1, 318, -1, -1, -1, -1, -1, -1, -1, 27, 27, 27, 27, 27, 27, 27, 68, 68, -1, -1, -1, -1, -1, -1, 321, -1, -1, 219, -1, -1, -1, -1, 323, -1, -1, -1, -1, -1, 324, 739, -1, -1, 265, -1, 741 },
			{ -1, 315, -1, -1, -1, 318, -1, -1, -1, -1, -1, -1, -1, 32, 32, 68, 68, 68, 68, 68, 68, 68, -1, -1, -1, -1, -1, -1, 321, -1, -1, 219, -1, -1, -1, -1, 323, -1, -1, -1, -1, -1, 324, 739, -1, -1, 265, -1, 741 },
			{ -1, -1, -1, -1, -1, 616, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 211, -1, -1, -1, -1, -1, -1 },
			{ -1, 328, -1, -1, 106, 106, -1, -1, -1, -1, 106, 106, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 204, -1, -1, 813, -1, -1, -1, 4, 225, -1, -1, 746, 729, 243, -1, -1, -1, 840, -1, -1, 46, -1, 904, -1 },
			{ -1, 88, -1, -1, 106, 269, -1, -1, -1, -1, 139, 273, 162, 184, 184, 184, 277, 277, 277, 277, 277, 277, 281, -1, -1, 285, -1, -1, 212, -1, -1, 219, 4, 225, -1, -1, 231, 237, 243, 288, -1, 249, 255, 260, -1, 46, 265, 904, 741 },
			{ -1, -1, -1, -1, 339, 339, -1, -1, -1, -1, 339, 339, -1, 11, 11, 11, 11, 11, 11, 11, 11, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 427, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 833, -1, -1, -1, 833, -1, 833, -1, 837, 837, 851, 860, 860, 860, 860, 860, 860, -1, -1, -1, -1, -1, -1, 833, -1, -1, 92, -1, -1, -1, -1, -1, 833, -1, -1, -1, 833, 833, 92, -1, -1, -1, -1, -1 },
			{ -1, 403, -1, -1, 300, 300, -1, 107, -1, -1, 300, 300, 312, 6, 6, 6, 687, 795, 795, 795, 795, 795, -1, -1, 312, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403 },
			{ -1, 143, -1, -1, -1, -1, -1, 155, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 143, 143, 143, 143, 143, 143, 143, 143, 143, 143, 143, 143, 143, 143, 143, 143, 143, 143, 143, 143, 143, 143, 143, 143 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 375, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 435, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 132, 132, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 451, -1, -1, -1, 451, 452, 453, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 571, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 521, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 211, -1, 617, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 150, 150, 150, 150, 150, 150, 150, 150, 150, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 88, -1, -1, 106, 123, -1, -1, -1, -1, 106, 106, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 204, -1, -1, 212, -1, -1, 219, 4, 225, -1, -1, 231, 729, 243, -1, -1, -1, 747, 739, -1, 46, 265, 904, 741 },
			{ -1, 702, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 702, 7, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 297, -1, -1, -1, 5, 5, 5, 5, 5, 5, 5, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 339, 339, -1, -1, -1, -1, 339, 339, -1, 11, 11, 11, 11, 11, 11, 11, 11, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 428, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 401, -1, 721, 721, 721, 721, 721, 187, 187, 187, 187, 401, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 403, -1, -1, 300, 300, -1, -1, -1, -1, 300, 300, 312, 6, 6, 6, 687, 795, 795, 795, 795, 795, -1, -1, 312, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403 },
			{ -1, 112, -1, -1, -1, 438, 369, -1, 370, -1, 371, 751, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 752, -1, -1, 234, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 234, 112, 112, 112, 112, 112, 112, 112, 112, 112 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 436, -1, -1, -1, 178, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 451, -1, -1, -1, 451, 452, 453, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 572, -1, -1, -1, -1 },
			{ -1, 911, -1, -1, -1, -1, -1, 117, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 911, 911, 911, 911, 911, 911, 911, 911, 911, 911, 911, 911, 911, 911, 911, 911, 911, 911, 911, 911, 911, 911, 911, 911 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 521, -1, -1, -1, 120, 120, 120, 120, 120, 120, 120, 120, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 612, 612, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 829, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 618, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 150, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 852, -1, -1, 106, 106, -1, -1, -1, -1, 106, 106, -1, 329, 330, 331, 331, 331, 331, 331, 331, 331, -1, -1, -1, 1024, -1, -1, 995, -1, -1, -1, 4, 1025, -1, -1, 994, 993, 1026, -1, -1, -1, 997, -1, -1, 46, -1, 904, -1 },
			{ -1, 702, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 702, 702, 702, 702, 702, 702, 702, 8, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702 },
			{ -1, -1, -1, -1, 339, 339, -1, -1, -1, -1, 339, 339, -1, 11, 11, 11, 11, 11, 11, 11, 11, 11, -1, -1, -1, -1, -1, -1, -1, 429, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 833, -1, -1, -1, 833, -1, 833, -1, 233, 233, 915, 939, 939, 939, 239, 239, 239, -1, -1, -1, -1, -1, -1, 833, -1, -1, 833, -1, -1, -1, -1, -1, 833, -1, -1, -1, 833, 833, 92, -1, -1, -1, -1, -1 },
			{ -1, 715, -1, -1, -1, -1, -1, 155, -1, -1, -1, -1, 454, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 454, 715, 715, 715, 715, 715, 715, 715, 715, 715, 715, 715, 715, 715, 715, 715, 715, 715, 715, 715, 715, 715, 715, 715, 715 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 763, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 573, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 718, 718, 718, 718, 718, 718, 718, 718, 718, 224, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 570, -1, -1, 120, 120, 120, 120, 120, 120, 120, 120, 120, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 612, 612, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 830, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 683, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 328, -1, -1, 106, 106, -1, -1, -1, -1, 106, 106, -1, 9, 900, 931, 931, 931, 931, 50, 50, 50, -1, -1, -1, 204, -1, -1, 813, -1, -1, -1, 4, 225, -1, -1, 746, 729, 243, -1, -1, -1, 840, -1, -1, 46, -1, 904, -1 },
			{ -1, 912, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 912, 912, 912, 912, 912, 912, 912, 912, 912, 912, 912, 912, 912, 912, 912, 912, 912, 912, 912, 912, 912, 912, 912, 912 },
			{ -1, -1, -1, -1, 339, 339, -1, -1, -1, -1, 339, 339, -1, 11, 11, 11, 11, 11, 11, 11, 11, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 71 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 401, -1, 21, 21, 21, 21, 21, 21, 21, 21, 21, 401, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 437, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 711, 711, 711, 711, 711, 711, 711, 711, 711, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 595, -1, -1, -1, 595, 596, 597, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 79 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 718, 718, 718, 718, 718, 230, 117, 117, 117, 224, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 619, -1, -1, -1, 211, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, 685, -1, 685, -1, -1, 161, 161, 161, 161, 161, 161, 161, 161, 161, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 861, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 10, 10, 10, 51, 75, 75, 75, 75, 75, -1, -1, -1, 726, -1, -1, 868, -1, -1, -1, -1, 732, -1, -1, 815, 811, 737, -1, -1, -1, 873, -1, -1, -1, -1, -1, -1 },
			{ -1, 315, -1, -1, -1, 318, -1, -1, -1, -1, -1, -1, -1, 807, 807, 807, 807, 807, 807, 807, 807, 807, 282, -1, -1, -1, -1, -1, 321, -1, -1, 219, -1, -1, -1, -1, 323, -1, -1, -1, -1, -1, 324, 739, -1, -1, 265, -1, 741 },
			{ -1, -1, -1, -1, 339, 339, -1, -1, -1, -1, 339, 339, -1, 11, 11, 11, 11, 11, 11, 11, 11, 11, -1, -1, -1, -1, -1, -1, -1, 71, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 401, -1, 21, 21, 21, 21, 21, 58, 58, 58, 58, 401, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 913, -1, -1, -1, -1, -1, 155, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 913, 913, 913, 913, 913, 913, 913, 913, 913, 913, 913, 913, 913, 913, 913, 913, 913, 913, 913, 913, 913, 913, 913, 913 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 843, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 711, 711, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 595, -1, -1, -1, 595, 596, 597, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 713, -1, -1, -1, 580, 922, -1, 581, -1, 581, 496, -1, 724, 724, 724, 724, 724, 724, 724, 724, 724, 496, -1, -1, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713 },
			{ -1, -1, -1, -1, 612, 612, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 787, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 193, 193, 193, 193, 193, 193, 193, 193, 193, 203, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 88, -1, -1, 106, 123, -1, -1, -1, -1, 139, 151, 162, 332, 332, 332, 332, 332, 332, 333, 333, 333, 194, -1, -1, 204, -1, -1, 212, -1, -1, 219, 4, 225, -1, -1, 231, 237, 243, -1, -1, 249, 255, 260, -1, 46, 265, 904, 741 },
			{ -1, 702, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 13, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702 },
			{ -1, -1, -1, -1, 339, 339, -1, -1, -1, -1, 339, 339, -1, 11, 11, 11, 11, 11, 11, 11, 11, 11, -1, -1, -1, -1, -1, 430, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 401, -1, 58, 58, 58, 58, 58, 58, 58, 58, 58, 401, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 723, 723, 723, 723, 723, 723, 723, 723, 723, 240, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 711, 37, 37, 37, 37, 37, 37, 37, 37, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 595, -1, -1, -1, 595, 596, 597, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 574, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 713, -1, -1, -1, 580, 922, -1, 581, -1, 581, 496, -1, 728, -1, -1, -1, -1, -1, -1, -1, -1, 496, -1, -1, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713 },
			{ -1, -1, -1, -1, 612, 612, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 923, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 193, 193, 193, 193, 193, 105, 86, 86, 86, 203, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 88, -1, -1, 106, 269, -1, -1, -1, -1, 139, 273, 162, 332, 332, 332, 332, 332, 332, 333, 333, 333, 281, -1, -1, 285, -1, -1, 212, -1, -1, 219, 4, 225, -1, -1, 231, 237, 243, 288, -1, 249, 255, 260, -1, 46, 265, 904, 741 },
			{ -1, 702, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 688, 702, 702, 702, 702, 702, 702, 702 },
			{ -1, -1, -1, -1, 339, 339, -1, -1, -1, -1, 339, 339, -1, 11, 11, 11, 11, 11, 11, 11, 11, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 434, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 697, 697, 697, 697, 697, 697, 697, 697, 697, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 723, 723, 723, 723, 723, 246, 155, 155, 155, 240, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 37, 37, 37, 37, 37, 37, 37, 37, 37, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 595, -1, -1, -1, 595, 596, 597, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 778, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 713, -1, -1, -1, 580, 922, -1, 581, -1, 581, 496, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 496, -1, -1, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713 },
			{ -1, -1, -1, -1, 612, 612, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 831, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 105, 105, 105, 105, 105, 105, 86, 86, 86, 203, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 74, 74, 74, 74, 74, 74, 695, 695, 695, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 702, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 702, 702, 702, 702, 702, 898, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702 },
			{ -1, -1, -1, -1, 339, 339, -1, -1, -1, -1, 339, 339, -1, 11, 11, 11, 11, 11, 11, 11, 11, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 714, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 833, -1, -1, -1, 833, -1, 833, -1, 47, 245, 245, 245, 245, 245, 245, 245, 245, -1, -1, -1, -1, -1, -1, 833, -1, -1, 833, -1, -1, -1, -1, -1, 72, -1, -1, -1, 72, 92, 110, -1, -1, -1, -1, -1 },
			{ -1, 720, -1, -1, -1, 455, 919, -1, 456, -1, 456, 457, -1, 267, 267, 267, 267, 267, 267, 267, 267, 267, -1, -1, -1, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 37, 37, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 595, -1, -1, -1, 595, 596, 597, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 716, -1, -1, -1, -1, -1 },
			{ -1, 713, -1, -1, -1, 582, 922, -1, 782, -1, 782, -1, -1, 728, 728, 728, 728, 728, 728, 728, 728, 728, -1, -1, -1, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 211, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 105, 105, 105, 105, 105, 105, 86, 86, 86, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 334, -1, -1, -1, -1, 335, -1, -1, -1, -1 },
			{ -1, 702, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 702, 702, 1022, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702 },
			{ -1, -1, -1, -1, -1, 833, -1, -1, -1, 833, -1, 833, -1, 245, 245, 245, 245, 245, 245, 245, 245, 245, -1, -1, -1, -1, -1, -1, 833, -1, -1, 833, -1, -1, -1, -1, -1, 72, -1, -1, -1, 72, 92, 110, -1, -1, -1, -1, -1 },
			{ -1, 720, -1, -1, -1, 455, 919, -1, 456, -1, 456, 457, -1, 267, 24, 24, 24, 24, 24, 24, 24, 24, -1, -1, -1, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 646, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 541, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 828, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 713, -1, -1, -1, 582, 922, -1, 782, -1, 782, -1, -1, 728, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 336, -1, -1, -1, 337, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 702, -1, -1, 300, 300, -1, 107, -1, -1, 300, 300, 312, 6, 6, 6, 687, 795, 795, 795, 795, 795, -1, -1, 312, 702, 702, 702, 702, 702, 702, 702, 292, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702 },
			{ -1, -1, -1, -1, -1, 833, -1, -1, -1, 833, -1, 833, -1, 245, 245, 127, 709, 709, 709, 709, 709, 709, -1, -1, -1, -1, -1, -1, 833, -1, -1, 833, -1, -1, -1, -1, -1, 72, -1, -1, -1, 72, 92, 110, -1, -1, -1, -1, -1 },
			{ -1, 720, -1, -1, -1, 455, 919, -1, 456, -1, 456, 457, -1, 24, 24, 24, 24, 24, 24, 24, 24, 24, -1, -1, -1, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 541, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 571, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 713, -1, -1, -1, 582, 922, -1, 782, -1, 782, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713 },
			{ -1, -1, -1, -1, 612, 612, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 338, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 702, -1, -1, 300, 300, -1, 107, -1, -1, 300, 300, 312, 6, 6, 6, 687, 795, 795, 795, 795, 795, -1, -1, 312, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702 },
			{ -1, 720, -1, -1, -1, 455, 919, -1, 456, -1, 456, 457, -1, 228, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 541, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 572, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 230, 230, 230, 230, 230, 230, 117, 117, 117, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 340, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 341, -1, -1, -1, -1 },
			{ -1, 702, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 966, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702 },
			{ -1, 720, -1, -1, -1, 455, 919, -1, 456, -1, 456, 457, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 541, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 573, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 117, 117, 117, 117, 117, 117, 117, 117, 117, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 342, -1, -1, -1, -1, -1, -1, 343, -1, -1, -1, -1, -1, 344, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 702, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 48, 702, 73, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702 },
			{ -1, 80, -1, -1, -1, 492, 493, -1, 494, -1, 494, 495, -1, 736, 736, 736, 736, 736, 736, 736, 736, 736, 495, -1, -1, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80 },
			{ -1, 143, -1, -1, -1, -1, -1, 155, -1, -1, -1, 488, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 143, 143, 143, 143, 143, 143, 143, 143, 143, 143, 143, 76, 143, 143, 143, 143, 143, 143, 143, 143, 143, 143, 143, 143 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 541, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 79 },
			{ -1, 633, -1, -1, -1, -1, -1, 117, -1, -1, -1, -1, 579, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 579, 633, 633, 633, 633, 633, 633, 633, 633, 633, 633, 633, 633, 633, 633, 633, 633, 633, 633, 633, 633, 633, 633, 633, 633 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 345, -1, -1, -1, -1, -1, -1, -1, -1, -1, 346, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 702, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 796, 702, 702, 702, 702, 702, 702, 1031 },
			{ -1, 80, -1, -1, -1, 492, 493, -1, 494, -1, 494, 495, -1, 227, 227, 227, 227, 227, 227, 227, 227, 227, 495, -1, -1, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 246, 246, 246, 246, 246, 246, 155, 155, 155, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 541, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 802, -1, -1, -1, -1, -1, 117, -1, -1, -1, -1, 634, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 634, 802, 802, 802, 802, 802, 802, 802, 802, 802, 802, 802, 802, 802, 802, 802, 802, 802, 802, 802, 802, 802, 802, 802, 802 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 347, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 702, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 8, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702 },
			{ -1, -1, -1, -1, -1, 833, -1, -1, -1, 833, 515, 142, -1, 154, 154, 165, 176, 176, 176, 176, 176, 176, 401, -1, -1, -1, -1, -1, 833, -1, -1, 833, -1, -1, -1, -1, -1, 72, -1, -1, -1, 72, 92, 110, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 155, 155, 155, 155, 155, 155, 155, 155, 155, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 541, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 574, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 259, 259, 259, 259, 259, 259, 259, 259, 259, 272, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 345, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 702, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 295, 702, 702, 702, 702, 702, 702, 1022, 14, 702, 702 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 246, 246, 246, 246, 246, 246, 283, 283, 283, 240, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 541, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 259, 259, 259, 259, 259, 230, 117, 117, 117, 272, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 348, -1, -1, -1, 349, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 345, 350, -1, -1, -1, -1 },
			{ -1, 702, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 1002, 702, 702, 702, 702, 702 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 283, 283, 283, 283, 283, 283, 283, 283, 283, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 541, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 778, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 230, 230, 230, 230, 230, 230, 117, 117, 117, 272, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 351, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 352, -1, -1, -1, -1 },
			{ -1, 702, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 899, 702, 702, 702, 702, 702 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 541, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 734, -1, -1, -1, -1, -1 },
			{ -1, 264, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 579, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 579, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 353, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 702, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 702, 702, 943, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 93, 702, 702, 702, 702, 702, 1005, 702, 702, 702 },
			{ -1, 720, -1, -1, -1, 455, 919, -1, 456, -1, 456, 457, -1, 517, 517, 517, 517, 517, 517, 517, 517, 517, -1, -1, -1, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720 },
			{ -1, -1, -1, -1, -1, -1, -1, 117, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 88, -1, -1, 106, 269, -1, -1, -1, -1, 106, 106, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 285, -1, -1, 212, -1, -1, 219, 4, 225, -1, -1, 231, 729, 243, 288, -1, -1, 747, 739, -1, 46, 265, 904, 741 },
			{ -1, 702, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 702, 702, 702, 702, 702, 702, 702, 867, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 999, 702, 702, 702, 702 },
			{ -1, 720, -1, -1, -1, 540, 919, -1, 456, -1, 456, 457, -1, 517, 517, 517, 517, 517, 517, 517, 517, 517, -1, -1, -1, 302, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 302, 720, 720, 720, 720, 720, 720, 720, 720, 720 },
			{ -1, 328, -1, -1, 106, 106, -1, -1, -1, -1, 106, 106, -1, 94, 909, 936, 936, 936, 936, 905, 905, 905, -1, -1, -1, 204, -1, -1, 813, -1, -1, -1, 4, 225, -1, -1, 746, 729, 243, -1, -1, -1, 840, -1, -1, 46, -1, 904, -1 },
			{ -1, 702, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 702, 702, 702, 702, 930, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702 },
			{ -1, 720, -1, -1, -1, 540, 919, -1, 456, -1, 456, 457, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 302, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 302, 720, 720, 720, 720, 720, 720, 720, 720, 720 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 284, 284, 284, 284, 284, 284, 284, 284, 284, 272, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 88, -1, -1, 106, 123, -1, -1, -1, -1, 139, 151, 355, 332, 332, 332, 332, 332, 332, 333, 333, 333, 194, -1, -1, 204, -1, -1, 212, -1, -1, 219, 4, 225, -1, -1, 231, 237, 243, -1, -1, 249, 255, 260, -1, 46, 265, 904, 741 },
			{ -1, 702, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 702, 702, 702, 1006, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702 },
			{ -1, 566, -1, -1, -1, -1, -1, 155, -1, -1, -1, -1, 454, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 454, 566, 566, 566, 566, 566, 566, 566, 566, 566, 566, 566, 566, 566, 566, 566, 566, 566, 566, 566, 566, 566, 566, 566, 566 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 703, 703, 703, 703, 703, 703, 799, 799, 799, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 301, 301, 301, 301, 301, 301, 107, 107, 107, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 31, 31, 31, 31, 31, 31, 31, 31, 31, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 356, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 12, -1, -1, 334, -1, -1, -1, -1, 335, -1, -1, -1, -1 },
			{ -1, 315, -1, -1, -1, 318, -1, -1, -1, -1, -1, -1, -1, 289, 289, 289, 289, 289, 289, 289, 289, 289, -1, -1, -1, -1, -1, -1, 321, -1, -1, 219, -1, -1, -1, -1, 323, -1, -1, -1, -1, -1, 324, 739, -1, -1, 265, -1, 741 },
			{ -1, 806, -1, -1, -1, -1, -1, 155, -1, -1, -1, -1, 567, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 567, 806, 806, 806, 806, 806, 806, 806, 806, 806, 806, 806, 806, 806, 806, 806, 806, 806, 806, 806, 806, 806, 806, 806, 806 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 356, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 12, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 296, 296, 296, 296, 296, 296, 296, 296, 296, 311, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 88, -1, -1, 106, 123, -1, -1, -1, -1, 139, 357, 355, 333, 333, 333, 333, 333, 333, 333, 333, 333, -1, -1, -1, 204, -1, -1, 212, -1, -1, 219, 4, 225, -1, -1, 231, 237, 243, -1, -1, 249, 255, 260, -1, 46, 265, 904, 741 },
			{ -1, 298, -1, -1, 300, 300, -1, 107, -1, -1, 300, 300, 312, 6, 6, 6, 687, 795, 795, 795, 795, 795, -1, -1, 312, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 296, 296, 296, 296, 296, 246, 155, 155, 155, 311, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 315, -1, -1, -1, 318, -1, -1, -1, -1, 358, -1, -1, 333, 333, 333, 333, 333, 333, 333, 333, 333, -1, -1, -1, -1, -1, -1, 321, -1, -1, 219, -1, -1, -1, -1, 323, -1, -1, -1, -1, -1, 324, 739, -1, -1, 265, -1, 741 },
			{ -1, 298, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 18, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 246, 246, 246, 246, 246, 246, 155, 155, 155, 311, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 5, 5, 5, 5, 5, 5, 5, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 720, -1, -1, -1, 568, 919, -1, 569, -1, 569, -1, -1, 299, 299, 299, 299, 299, 299, 299, 299, 299, -1, -1, -1, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720 },
			{ -1, -1, -1, -1, 300, 300, -1, -1, -1, -1, 300, 300, -1, 6, 6, 6, 687, 795, 795, 795, 795, 795, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 107, 107, 107, 107, 107, 107, 107, 107, 107, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 287, -1, -1, -1, -1, -1, 155, -1, -1, -1, 488, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 287, 287, 287, 287, 287, 287, 287, 287, 287, 287, 287, 700, 287, 287, 287, 287, 287, 287, 287, 287, 287, 287, 287, 287 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 194, -1, 359, 359, 359, 359, 359, 359, 359, 359, 359, 194, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 403, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 128, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403 },
			{ -1, 305, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 454, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 454, 305, 305, 305, 305, 305, 305, 305, 305, 305, 305, 305, 305, 305, 305, 305, 305, 305, 305, 305, 305, 305, 305, 305, 305 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 194, -1, 359, 359, 359, 359, 359, -1, -1, -1, -1, 194, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 844, -1, -1, 446, 447, -1, 107, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 403, 403, 403, 855, 403, 403, 949, 403, 403, 403, 403, 864, 403, 403, 403, 403, 403, 870, 875, 403, 403, 879, 403, 953 },
			{ -1, -1, -1, -1, -1, -1, -1, 155, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 194, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 194, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 403, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 403, 403, 403, 403, 403, 403, 128, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403 },
			{ -1, 360, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 360, 360, 360, 360, 360, 360, 360, 360, 360, 360, 360, 360, 360, 360, 360, 360, 360, 360, 360, 360, 360, 360, 360, 360 },
			{ -1, 403, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 23 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 320, 320, 320, 320, 320, 320, 320, 320, 320, 311, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 326, -1, -1, 327, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 315, -1, -1, -1, 318, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 321, -1, -1, 219, -1, -1, -1, -1, 323, -1, -1, -1, -1, -1, 324, 739, -1, -1, 265, -1, 741 },
			{ -1, 319, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 319, 319, 319, 319, 319, 319, 319, 319, 319, 319, 319, 319, 319, 319, 319, 319, 319, 319, 319, 319, 319, 319, 319, 319 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 336, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 343, -1, -1, -1, -1, -1, 344, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 348, -1, -1, -1, 362, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 350, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 91, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 364, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 15, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 325, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 366, 367, -1, 331, 331, 331, 331, 331, 331, 331, 331, 331, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 366, 367, -1, 331, 331, 331, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 366, 367, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 315, -1, -1, -1, 318, -1, -1, -1, -1, 358, -1, -1, 16, 16, 16, 16, 16, 16, 16, 16, 16, -1, -1, -1, -1, -1, -1, 321, -1, -1, 219, -1, -1, -1, -1, 323, -1, -1, -1, -1, -1, 324, 739, -1, -1, 265, -1, 741 },
			{ -1, 315, -1, -1, -1, 318, -1, -1, -1, -1, 358, -1, -1, 17, 17, 17, 17, 17, 17, 17, 17, 17, -1, -1, -1, -1, -1, -1, 321, -1, -1, 219, -1, -1, -1, -1, 323, -1, -1, -1, -1, -1, 324, 739, -1, -1, 265, -1, 741 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 109, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 126, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 54 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 141, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 387, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 708, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 153, -1, 164, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 175, -1, -1, -1, -1, -1, -1, 71 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 77, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 97, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 328, -1, -1, 106, 106, -1, -1, -1, -1, 388, 357, 355, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 204, -1, -1, 813, -1, -1, -1, 4, 225, -1, -1, 746, 729, 243, -1, -1, -1, 840, -1, -1, 46, -1, 904, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 141, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 186, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 114, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 131, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 196, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 15, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 328, -1, -1, 106, 106, -1, -1, -1, -1, 388, 357, 355, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 204, -1, -1, 813, -1, -1, -1, 4, 225, -1, -1, 746, 729, 243, -1, -1, -1, 840, -1, 144, 46, -1, 904, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 156, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 167, 389, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 387, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 861, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 726, -1, -1, 868, -1, -1, -1, -1, 732, -1, -1, 815, 811, 737, -1, -1, -1, 873, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 12, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 328, -1, -1, 106, 106, -1, -1, -1, -1, 106, 106, -1, 390, 391, 392, 392, 392, 392, 392, 392, 392, -1, -1, -1, 204, -1, -1, 813, -1, -1, -1, 4, 225, -1, -1, 746, 729, 243, -1, -1, -1, 840, -1, -1, 46, -1, 904, -1 },
			{ -1, 393, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 394, 395, 396, 396, 396, 396, 396, 396, 396, -1, -1, -1, 397, -1, -1, 755, -1, -1, -1, -1, 398, -1, -1, 399, 400, 753, -1, -1, -1, 818, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 194, -1, 402, 402, 402, 402, 402, 402, -1, -1, -1, 194, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 319, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 319, 319, 319, 319, 319, 319, 319, 319, 319, 319, 319, 319, 319, 319, 319, 319, 319, 319, 319, 319, 319, 319, 319, 319 },
			{ -1, -1, -1, -1, 300, 300, -1, -1, -1, -1, 300, 300, -1, 197, 206, 206, 214, 795, 795, 795, 795, 795, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 131, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 144, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 407, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 196, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 19, 19, 19, 56, 78, 78, 78, 78, 78, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 918, 918, 918, 918, 918, 918, 918, 918, 918, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 112, -1, -1, -1, 368, 369, -1, 370, -1, 370, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112 },
			{ -1, 166, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 166, 166, 166, 166, 166, 166, 166, 166, 166, 166, 166, 166, 166, 166, 166, 166, 166, 166, 166, 166, 166, 166, 166, 166 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 177, 177, 188, 723, 723, 723, 723, 723, 723, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 722, 722, 809, 838, 838, 838, 838, 838, 838, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 198, 198, 198, 198, 198, 198, 207, 215, 215, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 725, 725, 725, 725, 725, 725, 222, 228, 228, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 408, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 95, 95, 95, 95, 95, 95, 95, 95, 95, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 113, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 113, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 113, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 408, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 408, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 408, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 408, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 408, -1, 408, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 408, -1, -1, -1, -1, -1, -1, 408 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 408, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 409, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 54, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 328, -1, -1, 106, 106, -1, -1, -1, -1, 106, 106, -1, 433, 754, 748, 748, 748, 748, 748, 748, 748, -1, -1, -1, 204, -1, -1, 813, -1, -1, -1, 4, 225, -1, -1, 746, 729, 243, -1, -1, -1, 840, -1, -1, 46, -1, 904, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 54, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 367, 750, -1, 392, 392, 392, 392, 392, 392, 392, 392, 392, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 367, 750, -1, 816, 816, 816, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 367, 750, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 765, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 439, -1, -1, 396, 396, 396, 396, 396, 396, 396, 396, 396, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 439, -1, -1, 396, 396, 396, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 439, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 766, -1, -1, -1, -1, 440, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 767, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 441, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 442, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 443, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 22, 22, 22, 22, 22, 22, 59, 59, 59, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 55, 55, 55, 55, 55, 55, 55, 55, 55, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 844, -1, -1, 446, 921, -1, -1, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 403, 403, 403, 855, 403, 403, 949, 403, 403, 403, 403, 864, 403, 403, 403, 403, 403, 870, 875, 403, 403, 879, 403, 953 },
			{ -1, 403, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 49 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 448, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 823, -1, -1, -1, -1, -1, 449, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 941, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 941, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 408, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 458, -1, -1, -1, -1, -1, -1, -1, -1, 326, -1, -1, 327, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 877, -1, -1, 411, 411, -1, -1, -1, -1, 411, 411, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 757, -1, -1, 459, -1, -1, -1, 20, 758, -1, -1, 769, 760, 424, -1, -1, -1, 770, -1, -1, 57, -1, 906, -1 },
			{ -1, 410, -1, -1, 411, 412, -1, -1, -1, -1, 411, 411, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 757, -1, -1, 422, -1, -1, 219, 20, 758, -1, -1, 423, 760, 424, -1, -1, -1, 425, 739, -1, 57, 265, 906, 741 },
			{ -1, 825, -1, -1, 411, 411, -1, -1, -1, -1, 411, 411, -1, 25, 60, 81, 101, 118, 118, 118, 118, 118, -1, -1, -1, 460, -1, -1, 846, -1, -1, -1, 20, 461, -1, -1, 824, 462, 821, -1, -1, -1, 857, -1, -1, 57, -1, 906, -1 },
			{ -1, 877, -1, -1, 411, 411, -1, -1, -1, -1, 411, 411, -1, 842, 854, 854, 463, -1, -1, -1, -1, -1, -1, -1, -1, 757, -1, -1, 459, -1, -1, -1, 20, 758, -1, -1, 769, 760, 424, -1, -1, -1, 770, -1, -1, 57, -1, 906, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 464, 465, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 315, -1, -1, -1, 318, -1, -1, -1, -1, -1, -1, -1, 26, 61, 61, 61, 61, 61, 61, 61, 61, -1, -1, -1, -1, -1, -1, 321, -1, -1, 219, -1, -1, -1, -1, 323, -1, -1, -1, -1, -1, 324, 739, -1, -1, 265, -1, 741 },
			{ -1, 315, -1, -1, -1, 318, -1, -1, -1, -1, -1, -1, -1, 61, 61, 61, 82, 82, 82, 82, 82, 82, -1, -1, -1, -1, -1, -1, 321, -1, -1, 219, -1, -1, -1, -1, 323, -1, -1, -1, -1, -1, 324, 739, -1, -1, 265, -1, 741 },
			{ -1, 315, -1, -1, -1, 318, -1, -1, -1, -1, -1, -1, -1, 82, 82, 82, 82, 82, 82, 82, 82, 82, -1, -1, -1, -1, -1, -1, 321, -1, -1, 219, -1, -1, -1, -1, 323, -1, -1, -1, -1, -1, 324, 739, -1, -1, 265, -1, 741 },
			{ -1, 315, -1, -1, -1, 318, -1, -1, -1, -1, -1, -1, -1, 82, 82, 82, 82, 82, 82, 102, 119, 119, -1, -1, -1, -1, -1, -1, 321, -1, -1, 219, -1, -1, -1, -1, 323, -1, -1, -1, -1, -1, 324, 739, -1, -1, 265, -1, 741 },
			{ -1, 315, -1, -1, -1, 318, -1, -1, -1, -1, -1, -1, -1, 119, 119, 119, 119, 119, 119, 119, 119, 119, -1, -1, -1, -1, -1, -1, 321, -1, -1, 219, -1, -1, -1, -1, 323, -1, -1, -1, -1, -1, 324, 739, -1, -1, 265, -1, 741 },
			{ -1, 315, -1, -1, -1, 318, -1, -1, -1, -1, -1, -1, -1, 119, 68, 68, 68, 68, 68, 68, 68, 68, -1, -1, -1, -1, -1, -1, 321, -1, -1, 219, -1, -1, -1, -1, 323, -1, -1, -1, -1, -1, 324, 739, -1, -1, 265, -1, 741 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 336, -1, -1, -1, 468, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 471, -1, -1, -1, -1, -1, -1, 343, -1, -1, -1, -1, -1, 344, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 473, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 348, -1, -1, -1, 474, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 350, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 475, 476, 476, 476, 476, 477, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 353, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 71, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 482, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 434, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 71, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 483, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 54, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 367, 367, -1, 748, 748, 748, 748, 748, 748, 748, 748, 748, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 484, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 768, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 485, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 845, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 112, -1, -1, -1, 438, 369, -1, 370, -1, 370, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 234, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 234, 112, 112, 112, 112, 112, 112, 112, 112, 112 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 98, 98, 98, 115, 132, 132, 132, 132, 132, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 490, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 490, -1, 490, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 490, -1, -1, -1, -1, -1, -1, 490 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 490, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 491, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 403, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 128, 403, 403, 403, 403, 403 },
			{ -1, 499, -1, -1, 446, 446, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 772, -1, -1, -1, -1, -1, 771, 500, -1, -1, 773, -1, -1 },
			{ -1, 501, -1, -1, 446, 921, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 856, -1, -1, 826, -1, -1, -1, -1, 502, -1, -1, -1, -1, -1, 503, 775, -1, -1, 827, -1, 847 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 774, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 516, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 178 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 145, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 145, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 145, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 518, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 518, 518, 518, 518, 518, 518, 518, 518, 518, 518, 518, 518, 518, 518, 518, 518, 518, 518, 518, 518, 518, 518, 518, 518 },
			{ -1, 720, -1, -1, -1, 455, 919, -1, 456, -1, 456, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 290, 290, 293, 296, 296, 296, 296, 296, 296, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 299, 299, 299, 299, 299, 299, 299, 299, 299, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 99, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 468, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 524, -1, -1, -1, -1, 525, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 527, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 528, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 530, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 522, 522, 522, 522, 522, 522, 523, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 534, 534, 534, 534, 534, 534, 534, 534, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 534, 534, 534, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 116, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 133, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 146, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 712, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 158, -1, 169, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 180, -1, -1, -1, -1, -1, -1, 79 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 146, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 190, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 131, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 200, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 28, 28, 28, 28, 28, 28, 28, 28, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 28, 28, 28, 28, 28, 28, 28, 28, 28, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 28, 28, 28, 28, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 71, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 54, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 536, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 54, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 538, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 539, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 96, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 24, 24, 24, 24, 24, 24, 24, 24, 24, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 541, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 756, -1, -1, -1, -1, -1 },
			{ -1, 80, -1, -1, -1, 492, 493, -1, 494, -1, 494, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80 },
			{ -1, 134, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 134, 134, 134, 134, 134, 134, 134, 134, 134, 134, 134, 134, 134, 134, 134, 134, 134, 134, 134, 134, 134, 134, 134, 134 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 147, 147, 159, 718, 718, 718, 718, 718, 718, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 170, 170, 170, 170, 170, 170, 181, 191, 191, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 201, 201, 201, 201, 201, 201, 210, 217, 217, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 403, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 128 },
			{ -1, 403, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 403, 403, 403, 403, 403, 403, 403, 29, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 542, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 545, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 546, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 548, -1, -1, 549, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 551, -1, -1, -1, -1, -1, 552, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 553, -1, -1, -1, 554, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 555, -1, -1, -1, -1 },
			{ -1, 403, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 63 },
			{ -1, 403, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 907, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403 },
			{ -1, 403, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 690, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403 },
			{ -1, 403, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 902, 403, 403, 403, 403, 403 },
			{ -1, 403, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 403, 403, 934, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403 },
			{ -1, 403, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 1000, 403, 403, 403, 403 },
			{ -1, 403, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 403, 403, 403, 403, 933, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403 },
			{ -1, 403, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 403, 403, 403, 1007, 561, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403 },
			{ -1, 403, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 128, 403, 403, 403, 403, 403, 403, 403 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 136, 136, 136, 136, 136, 136, 136, 136, 136, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 136, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 30, 30, 30, 30, 30, 30, 30, 30, 30, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 565, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 305, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 305, 305, 305, 305, 305, 305, 305, 305, 305, 305, 305, 305, 305, 305, 305, 305, 305, 305, 305, 305, 305, 305, 305, 305 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 200, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 209, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 157, 157, 157, 168, 711, 711, 711, 711, 711, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 120, 120, 120, 120, 120, 120, 120, 120, 120, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 120, 120, 120, 120, 120, 120, 120, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 216, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 223, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 229, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 731, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 235, -1, 241, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 247, -1, -1, -1, -1, -1, -1, 253 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 229, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 258, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 263, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 120, 120, 120, 120, 120, 120, 120, 120, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 779, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 71 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 71, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 450, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 575, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 537, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 720, -1, -1, -1, 540, 919, -1, 456, -1, 456, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 302, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 302, 720, 720, 720, 720, 720, 720, 720, 720, 720 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 576, 577, 577, 578, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 583, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 583, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 584, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 585, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 586, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 587, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 588, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 84, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 104 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 121, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 137, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 149, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 160, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 84, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 171, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 182, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 192, 590, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 589, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 403, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 63, 403, 403, 403, 403, 403, 403, 403 },
			{ -1, 403, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 403, 403, 403, 403, 403, 403, 403, 403, 403, 63, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403 },
			{ -1, 403, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 23, 403, 403, 403, 403, 403 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 202 },
			{ -1, 403, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 34, 403, 403 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 593, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 566, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 454, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 454, 566, 566, 566, 566, 566, 566, 566, 566, 566, 566, 566, 566, 566, 566, 566, 566, 566, 566, 566, 566, 566, 566, 566, 566 },
			{ -1, 777, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 777, 777, 777, 777, 777, 777, 777, 777, 777, 777, 777, 777, 777, 777, 777, 777, 777, 777, 777, 777, 777, 777, 777, 777 },
			{ -1, 720, -1, -1, -1, 568, 919, -1, 569, -1, 569, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 314, 314, 317, 320, 320, 320, 320, 320, 320, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 179, 189, 189, 199, 711, 711, 711, 711, 711, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 602, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 603, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 35, 35, 35, 35, 35, 35, 35, 35, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 35, 35, 35, 35, 35, 35, 35, 35, 35, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 35, 35, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 607, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 607, 607, 607, 607, 607, 607, 607, 607, 607, 607, 607, 607, 607, 607, 607, 607, 607, 607, 607, 607, 607, 607, 607, 607 },
			{ -1, 713, -1, -1, -1, 580, 922, -1, 581, -1, 581, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 248, 248, 254, 259, 259, 259, 259, 259, 259, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 612, 612, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 783, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 612, 612, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 784, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 612, 612, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 613, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 612, 612, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 614, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 612, 612, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 785, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 615, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 202, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 202, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 403, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 403, 403, 403, 403, 403, 403, 63, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403 },
			{ -1, 403, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 36 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 54, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 604, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 208, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 208, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 624, 624, 625, 626, 626, 626, 626, 626, 626, -1, -1, -1, -1, -1, -1, -1, -1, -1, 208, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 65, 65, 65, 65, 65, 65, 65, 65, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 65, 65, 65, 65, 65, 65, 65, 65, 65, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 65, 65, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 627, 627, 628, 629, 629, 629, 629, 629, 629, -1, -1, -1, -1, -1, -1, -1, -1, -1, 363, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 352, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 605, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 630, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 631, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 632, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 264, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264, 264 },
			{ -1, 713, -1, -1, -1, 608, 922, -1, 609, -1, 609, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 276, 276, 280, 284, 284, 284, 284, 284, 284, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 713, -1, -1, -1, 610, 922, -1, 609, -1, 609, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713 },
			{ -1, 403, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 1030 },
			{ -1, -1, -1, -1, 612, 612, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 635, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 614, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 637, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 788, -1, -1, -1, -1, -1, 639, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 641, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 642, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 202, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 644, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 403, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 403, 403, 403, 403, 63, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403 },
			{ -1, 403, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 403, 403, 403, 63, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403 },
			{ -1, 622, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 567, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 567, 622, 622, 622, 622, 622, 622, 622, 622, 622, 622, 622, 622, 622, 622, 622, 622, 622, 622, 622, 622, 622, 622, 622, 622 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 645, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 626, 626, 626, 626, 626, 626, 626, 626, 626, 869, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 626, 626, 626, 626, 626, -1, -1, -1, -1, 869, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 869, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 648, 648, 648, 648, 648, 648, 629, 629, 629, 649, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 648, 648, 648, 648, 648, 874, -1, -1, -1, 649, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 874, 874, 874, 874, 874, 874, -1, -1, -1, 649, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 650, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 633, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 579, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 579, 633, 633, 633, 633, 633, 633, 633, 633, 633, 633, 633, 633, 633, 633, 633, 633, 633, 633, 633, 633, 633, 633, 633, 633 },
			{ -1, 786, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 786, 786, 786, 786, 786, 786, 786, 786, 786, 786, 786, 786, 786, 786, 786, 786, 786, 786, 786, 786, 786, 786, 786, 786 },
			{ -1, 38, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 651 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 652, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 791, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 653, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 218 },
			{ -1, 39, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 202, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 654, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 202, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 878, 878, 656, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 624, 624, 625, 626, 626, 626, 626, 626, 626, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 659, 659, 660, 626, 626, 626, 626, 626, 626, -1, -1, -1, -1, -1, -1, -1, -1, -1, 208, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 881, 881, 881, 881, 881, 881, 661, 661, 661, 649, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 883, 883, 883, 883, 883, 883, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 665, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 640, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 666, 666, 666, 666, 666, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 658, 658, 658, 658, 658, 658, 658, 658, 658, 667, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 667, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 668, 668, 668, 668, 668, 668, 668, 668, 668, 869, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 668, 668, 668, 668, 668, -1, -1, -1, -1, 869, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 671, 671, 671, 671, 671, 671, 672, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 662, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 634, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 634, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662 },
			{ -1, 403, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 63, 403, 403, 403, 403, 403 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 673, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 924, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 40, 40, 40, 40, 40, 40, 66, 86, 86, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 674, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 105, 105, 105, 105, 105, 105, 66, 86, 86, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 40, 671, 671, 671, 671, 671, 672, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 202, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 676, 676, 676, 676, 676, 676, 658, 658, 658, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 793, 793, 793, 793, 793, 793, 793, 793, 793, 667, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 678, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 887, 887, 887, 887, 887, 887, 680, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 122, 122, 122, 122, 122, 122, 138, 86, 86, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 681, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 682, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, 682, -1, -1, 684, -1, 684, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 161, 161, 161, 161, 161, 161, 161, 161, 161, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 41, 41, 692, 798, 798, 798, 798, 798, 798, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 172, 172, 183, 193, 193, 193, 193, 193, 193, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 89, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 89, 89, 89, 89, 89, 174, 89, 89, 89, 89, 89, 89, 89, 89, 185, 89, 89, 89, 89, 195, 89, 89, 89, 89 },
			{ -1, -1, -1, -1, -1, 833, -1, -1, -1, 833, -1, 833, -1, 47, 47, 127, 709, 709, 709, 709, 709, 709, -1, -1, -1, -1, -1, -1, 833, -1, -1, 833, -1, -1, -1, -1, -1, 72, -1, -1, -1, 72, 92, 110, -1, -1, -1, -1, -1 },
			{ -1, 298, -1, -1, 300, 300, -1, 107, -1, -1, 361, 300, 312, 6, 6, 6, 687, 795, 795, 795, 795, 795, -1, -1, 312, 298, 298, 298, 298, 298, 298, 298, 304, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 52, 52, 52, 52, 52, 52, 52, 52, 52, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 403, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 403, 403, 403, 894, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 591, 403, 403, 403, 403, 403 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 64, 64, 64, 64, 64, 64, 64, 64, 64, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 798, 798, 798, 798, 798, 698, 87, 87, 87, 67, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 89, -1, -1, 300, 300, -1, 107, -1, -1, 300, 300, -1, 6, 6, 6, 687, 795, 795, 795, 795, 795, -1, -1, -1, 89, 89, 89, 89, 89, 89, 89, 914, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89 },
			{ -1, -1, -1, -1, 339, 339, -1, -1, -1, -1, 339, 339, -1, 11, 11, 11, 11, 11, 11, 11, 11, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 71, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 112, -1, -1, -1, 368, 369, -1, 370, -1, 370, 373, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 373, -1, -1, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 79, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 251, 251, 251, 251, 251, 251, 251, 251, 251, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 87, 87, 87, 87, 87, 87, 87, 87, 87, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 315, -1, -1, -1, 318, -1, -1, -1, -1, -1, -1, -1, 478, 479, 479, 479, 479, 479, 479, 479, 479, -1, -1, -1, -1, -1, -1, 321, -1, -1, 219, -1, -1, -1, -1, 323, -1, -1, -1, -1, -1, 324, 739, -1, -1, 265, -1, 741 },
			{ -1, 806, -1, -1, -1, -1, -1, 155, -1, -1, -1, 53, 567, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 567, 806, 806, 806, 806, 806, 806, 806, 806, 806, 806, 806, 806, 806, 806, 806, 806, 806, 806, 806, 806, 806, 806, 806, 806 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 517, 517, 517, 517, 517, 517, 517, 517, 517, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 451, -1, -1, -1, 451, 452, 453, -1, -1, -1, -1, -1 },
			{ -1, 112, -1, -1, -1, 368, 369, -1, 370, -1, 370, 752, -1, 710, 710, 710, 710, 710, 710, 710, 710, 710, 752, -1, -1, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 130, 130, 130, 130, 130, 130, 130, 130, 130, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 236, -1, -1, -1, -1, -1, 117, -1, -1, -1, -1, 579, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 579, 236, 236, 236, 236, 236, 236, 236, 236, 236, 236, 236, 236, 236, 236, 236, 236, 236, 236, 236, 236, 236, 236, 236, 236 },
			{ -1, 88, -1, -1, 106, 269, -1, -1, -1, -1, 139, 273, 162, 277, 277, 277, 277, 277, 291, 291, 291, 291, 281, -1, -1, 285, -1, -1, 212, -1, -1, 219, 4, 225, -1, -1, 231, 237, 243, 288, -1, 249, 255, 260, -1, 46, 265, 904, 741 },
			{ -1, 403, -1, -1, 300, 300, -1, 107, -1, -1, 361, 300, 312, 6, 6, 6, 687, 795, 795, 795, 795, 795, -1, -1, 312, 403, 403, 403, 403, 1009, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403 },
			{ -1, -1, -1, -1, 339, 339, -1, -1, -1, -1, 339, 339, -1, 11, 11, 11, 11, 11, 11, 11, 11, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 819, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 401, -1, 187, 187, 187, 187, 187, 187, 187, 187, 187, 401, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 112, -1, -1, -1, 438, 369, -1, 370, -1, 370, 752, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 752, -1, -1, 234, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 234, 112, 112, 112, 112, 112, 112, 112, 112, 112 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 595, -1, -1, -1, 595, 596, 597, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 781, -1, -1, -1, -1 },
			{ -1, 242, -1, -1, -1, -1, -1, 117, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 242, 242, 242, 242, 242, 242, 242, 242, 242, 242, 242, 242, 242, 242, 242, 242, 242, 242, 242, 242, 242, 242, 242, 242 },
			{ -1, -1, -1, -1, 339, 339, -1, -1, -1, -1, 339, 339, -1, 11, 11, 11, 11, 11, 11, 11, 11, 11, -1, -1, -1, -1, -1, -1, -1, 486, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 279, -1, -1, -1, -1, -1, 155, -1, -1, -1, -1, 454, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 454, 279, 279, 279, 279, 279, 279, 279, 279, 279, 279, 279, 279, 279, 279, 279, 279, 279, 279, 279, 279, 279, 279, 279, 279 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 606, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 322, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 322, 322, 322, 322, 322, 322, 322, 322, 322, 322, 322, 322, 322, 322, 322, 322, 322, 322, 322, 322, 322, 322, 322, 322 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 230, 230, 230, 230, 230, 230, 117, 117, 117, 224, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 315, -1, -1, -1, 318, -1, -1, -1, -1, -1, -1, -1, 807, 807, 807, 807, 807, 286, 289, 289, 289, 282, -1, -1, -1, -1, -1, 321, -1, -1, 219, -1, -1, -1, -1, 323, -1, -1, -1, -1, -1, 324, 739, -1, -1, 265, -1, 741 },
			{ -1, 287, -1, -1, -1, -1, -1, 155, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 287, 287, 287, 287, 287, 287, 287, 287, 287, 287, 287, 287, 287, 287, 287, 287, 287, 287, 287, 287, 287, 287, 287, 287 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 401, -1, 697, 697, 697, 697, 697, 697, 697, 697, 697, 401, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 252, 252, 252, 252, 252, 252, 252, 252, 252, 240, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 246, 246, 246, 246, 246, 246, 155, 155, 155, 240, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 713, -1, -1, -1, 608, 922, -1, 609, -1, 609, 496, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 496, -1, -1, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713, 713 },
			{ -1, 720, -1, -1, -1, 455, 919, -1, 456, -1, 456, 457, -1, 228, 228, 228, 228, 228, 228, 228, 228, 228, -1, -1, -1, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 379, -1, -1, -1, -1, 380, -1, -1, -1, -1 },
			{ -1, 720, -1, -1, -1, 455, 919, -1, 456, -1, 456, 457, -1, 271, 24, 24, 24, 24, 24, 24, 24, 24, -1, -1, -1, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 346, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 720, -1, -1, -1, 455, 919, -1, 456, -1, 456, 457, -1, 275, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 541, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 781, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 382, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 383, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 541, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 606, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 662, -1, -1, -1, -1, -1, 117, -1, -1, -1, -1, 634, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 634, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662, 662 },
			{ -1, 80, -1, -1, -1, 492, 493, -1, 494, -1, 494, 495, -1, 733, 733, 733, 733, 733, 733, 733, 733, 733, 495, -1, -1, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 749, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 363, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 352, -1, -1, -1, -1 },
			{ -1, 740, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 634, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 634, 740, 740, 740, 740, 740, 740, 740, 740, 740, 740, 740, 740, 740, 740, 740, 740, 740, 740, 740, 740, 740, 740, 740, 740 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 354, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 622, -1, -1, -1, -1, -1, 155, -1, -1, -1, -1, 567, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 567, 622, 622, 622, 622, 622, 622, 622, 622, 622, 622, 622, 622, 622, 622, 622, 622, 622, 622, 622, 622, 622, 622, 622, 622 },
			{ -1, 298, -1, -1, 300, 300, -1, 107, -1, -1, 300, 300, 312, 6, 6, 6, 687, 795, 795, 795, 795, 795, -1, -1, 312, 298, 298, 298, 298, 298, 298, 298, 316, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298 },
			{ -1, 403, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 403, 403, 403, 403, 403, 403, 404, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403 },
			{ -1, 745, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 567, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 567, 745, 745, 745, 745, 745, 745, 745, 745, 745, 745, 745, 745, 745, 745, 745, 745, 745, 745, 745, 745, 745, 745, 745, 745 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 342, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 348, -1, -1, -1, 349, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 350, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 367, 367, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 408, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 489, 489, 489, 489, 489, 489, 489, 489, 489, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 810, 810, 810, 810, 810, 810, 727, 215, 215, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 839, 839, 839, 839, 839, 839, 730, 228, 228, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 764, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 367, 367, -1, 748, 748, 748, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 762, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 466, -1, -1, -1, -1, 467, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 469, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 470, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 472, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 403, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 403, 403, 403, 403, 403, 403, 404, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 490, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 768, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 490, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 490, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 490, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 490, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 471, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 519, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 544, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 543, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 543, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 547, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 535, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 556, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 557, -1, -1, -1, -1 },
			{ -1, 403, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 29, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403 },
			{ -1, 745, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 745, 745, 745, 745, 745, 745, 745, 745, 745, 745, 745, 745, 745, 745, 745, 745, 745, 745, 745, 745, 745, 745, 745, 745 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 598, 599, 599, 600, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 589, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 738, 738, 254, 259, 259, 259, 259, 259, 259, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 636, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 614, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 638, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 740, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 740, 740, 740, 740, 740, 740, 740, 740, 740, 740, 740, 740, 740, 740, 740, 740, 740, 740, 740, 740, 740, 740, 740, 740 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 643, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 639, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 655, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 664, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 675, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 679, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 89, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89 },
			{ -1, -1, -1, -1, -1, 833, -1, -1, -1, 833, -1, 833, -1, 804, 804, 127, 709, 709, 709, 709, 709, 709, -1, -1, -1, -1, -1, -1, 833, -1, -1, 833, -1, -1, -1, -1, -1, 72, -1, -1, -1, 72, 92, 110, -1, -1, -1, -1, -1 },
			{ -1, 298, -1, -1, 300, 300, -1, 107, -1, -1, 361, 300, 312, 6, 6, 6, 687, 795, 795, 795, 795, 795, -1, -1, 312, 298, 298, 310, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 689, 689, 689, 689, 689, 689, 689, 689, 689, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 698, 698, 698, 698, 698, 698, 87, 87, 87, 67, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 112, -1, -1, -1, 368, 369, -1, 370, -1, 370, 752, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 752, -1, -1, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112 },
			{ -1, 315, -1, -1, -1, 318, -1, -1, -1, -1, -1, -1, -1, 479, 479, 479, 480, 480, 480, 480, 480, 480, -1, -1, -1, -1, -1, -1, 321, -1, -1, 219, -1, -1, -1, -1, 323, -1, -1, -1, -1, -1, 324, 739, -1, -1, 265, -1, 741 },
			{ -1, 298, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 744, 298, 298, 298, 298, 298 },
			{ -1, 735, -1, -1, -1, -1, -1, 117, -1, -1, -1, -1, 634, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 634, 735, 735, 735, 735, 735, 735, 735, 735, 735, 735, 735, 735, 735, 735, 735, 735, 735, 735, 735, 735, 735, 735, 735, 735 },
			{ -1, 88, -1, -1, 106, 269, -1, -1, -1, -1, 139, 273, 162, 291, 291, 294, 294, 294, 294, 294, 294, 294, 281, -1, -1, 285, -1, -1, 212, -1, -1, 219, 4, 225, -1, -1, 231, 237, 243, 288, -1, 249, 255, 260, -1, 46, 265, 904, 741 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 401, -1, 721, 721, 721, 721, 721, 721, 721, 721, 721, 401, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 268, -1, -1, -1, -1, -1, 117, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 268, 268, 268, 268, 268, 268, 268, 268, 268, 268, 268, 268, 268, 268, 268, 268, 268, 268, 268, 268, 268, 268, 268, 268 },
			{ -1, 742, -1, -1, -1, -1, -1, 155, -1, -1, -1, -1, 567, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 567, 742, 742, 742, 742, 742, 742, 742, 742, 742, 742, 742, 742, 742, 742, 742, 742, 742, 742, 742, 742, 742, 742, 742, 742 },
			{ -1, 315, -1, -1, -1, 318, -1, -1, -1, -1, -1, -1, -1, 286, 286, 286, 286, 286, 286, 289, 289, 289, 282, -1, -1, -1, -1, -1, 321, -1, -1, 219, -1, -1, -1, -1, 323, -1, -1, -1, -1, -1, 324, 739, -1, -1, 265, -1, 741 },
			{ -1, 308, -1, -1, -1, -1, -1, 155, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 308, 308, 308, 308, 308, 308, 308, 308, 308, 308, 308, 308, 308, 308, 308, 308, 308, 308, 308, 308, 308, 308, 308, 308 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 252, 252, 252, 252, 252, 257, 262, 262, 262, 240, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 720, -1, -1, -1, 455, 919, -1, 456, -1, 456, 457, -1, 271, 271, 271, 271, 271, 271, 271, 271, 271, -1, -1, -1, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 385, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 80, -1, -1, -1, 492, 493, -1, 494, -1, 494, 496, -1, 733, 733, 733, 733, 733, 733, 733, 733, 733, 496, -1, -1, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 337, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 403, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 991, 403, 403, 403, 403, 403 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 384, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 444, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 822, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 403, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 497, 403, 403, 403, 403, 403, 403, 403 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 531, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 449, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 529, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 520, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 780, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 558, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 594, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 643, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 790, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 89, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 89, 89, 89, 89, 205, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89 },
			{ -1, 315, -1, -1, -1, 318, -1, -1, -1, -1, -1, -1, -1, 480, 480, 480, 480, 480, 480, 480, 480, 480, -1, -1, -1, -1, -1, -1, 321, -1, -1, 219, -1, -1, -1, -1, 323, -1, -1, -1, -1, -1, 324, 739, -1, -1, 265, -1, 741 },
			{ -1, 298, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 814, 298, 298, 298, 298, 298, 298 },
			{ -1, 88, -1, -1, 106, 269, -1, -1, -1, -1, 139, 273, 162, 294, 294, 294, 294, 294, 294, 294, 294, 294, 281, -1, -1, 285, -1, -1, 212, -1, -1, 219, 4, 225, -1, -1, 231, 237, 243, 288, -1, 249, 255, 260, -1, 46, 265, 904, 741 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 401, -1, 221, 221, 221, 221, 221, 221, 221, 221, 221, 401, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 257, 257, 257, 257, 257, 257, 262, 262, 262, 240, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 720, -1, -1, -1, 455, 919, -1, 456, -1, 456, 457, -1, 275, 275, 275, 275, 275, 275, 275, 275, 275, -1, -1, -1, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720, 720 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 365, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 403, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 405, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 533, 522, 522, 522, 522, 522, 522, 522, 522, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 487, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 403, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 1046, 403, 403, 498, 403, 403, 403, 403, 403, 403, 403 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 526, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 559, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 89, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 226, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 232, 89, 89, 89, 89 },
			{ -1, 315, -1, -1, -1, 318, -1, -1, -1, -1, -1, -1, -1, 480, 480, 480, 480, 480, 480, 481, 68, 68, -1, -1, -1, -1, -1, -1, 321, -1, -1, 219, -1, -1, -1, -1, 323, -1, -1, -1, -1, -1, 324, 739, -1, -1, 265, -1, 741 },
			{ -1, 298, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 298, 298, 298, 298, 298, 298, 996, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 401, -1, 221, 221, 221, 221, 221, 227, 227, 227, 227, 401, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 980, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 403, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 445, 403, 403, 403, 403, 403, 403 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 522, 522, 522, 522, 522, 522, 522, 522, 522, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 403, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 504, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 550, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 532, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 89, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 238, 89, 89, 89, 89, 89, 89, 1023, 89, 89, 89, 89, 89, 244, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89 },
			{ -1, 298, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 307, 298, 298, 298, 298, 298 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 401, -1, 227, 227, 227, 227, 227, 227, 227, 227, 227, 401, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 374, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 403, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 403, 403, 403, 404, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 623, 623, 623, 623, 623, 623, 623, 623, 623, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 403, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 403, 403, 403, 403, 403, 403, 403, 505, 403, 403, 403, 403, 403, 506, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 563, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 89, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 89, 89, 89, 89, 951, 89, 89, 954, 89, 89, 89, 89, 89, 250, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89 },
			{ -1, 298, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 862, 979, 298, 298, 298, 298, 298, 298 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 381, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 657, 657, 657, 657, 657, 657, 658, 658, 658, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 403, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 507, 403, 403, 403, 508, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 776, 403, 403, 403, 403 },
			{ -1, 89, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 89, 89, 256, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89 },
			{ -1, 298, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 313, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 386, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 661, 661, 661, 661, 661, 661, 661, 661, 661, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 403, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 403, 403, 403, 403, 403, 403, 509, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 510, 403, 403, 403, 403 },
			{ -1, 89, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 261, 89, 89, 89, 266, 89, 89, 957, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 244, 89, 89, 89, 89 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 458, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 666, 666, 666, 666, 666, 666, 666, 666, 666, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 403, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 403, 403, 403, 403, 511, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403 },
			{ -1, 89, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 89, 89, 89, 89, 278, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 669, 669, 669, 669, 669, 669, 670, 661, 661, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 403, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 403, 403, 403, 403, 512, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 792, 792, 792, 792, 792, 792, 792, 792, 792, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 403, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 403, 403, 403, 404, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 677, 677, 677, 677, 677, 677, 677, 677, 677, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 403, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 405, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 681, 681, 681, 681, 681, 681, 681, 681, 681, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 403, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 560, 403, 403, 403, 403 },
			{ -1, 403, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 560, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403 },
			{ -1, 403, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 403, 403, 403, 403, 403, 403, 562, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403 },
			{ -1, 403, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 564, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403 },
			{ -1, 403, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 404, 403, 403, 403, 403, 403, 403 },
			{ -1, 403, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 592, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403 },
			{ -1, 403, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 611, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403 },
			{ -1, 403, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 620, 403, 403, 403, 403, 403 },
			{ -1, 403, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 621, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403 },
			{ -1, 403, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 403, 403, 403, 403, 403, 403, 663, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403 },
			{ -1, 298, -1, -1, 300, 300, -1, 107, -1, -1, 361, 300, 312, 6, 6, 6, 687, 795, 795, 795, 795, 795, -1, -1, 312, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 853, 298, 298, 298, 298 },
			{ -1, 298, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 1011, 298, 298, 298, 298 },
			{ -1, 112, -1, -1, -1, 368, 369, -1, 370, -1, 371, 372, -1, 50, 50, 50, 695, 695, 695, 695, 695, 695, 373, -1, -1, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112 },
			{ -1, 403, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 1017, 403, 403, 403, 403 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 691, 691, 691, 691, 691, 691, 691, 691, 691, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 339, 339, -1, -1, -1, -1, 339, 339, -1, 11, 11, 11, 11, 11, 11, 11, 11, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 694, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 112, -1, -1, -1, 368, 369, -1, 370, -1, 371, 751, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 752, -1, -1, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 696, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 403, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 1030, 403, 895, 403, 403, 403, 403 },
			{ -1, 801, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 835, 702, 702, 702, 702, 702, 702, 702 },
			{ -1, 112, -1, -1, -1, 368, 369, -1, 370, -1, 371, 751, -1, 129, 129, 129, 710, 710, 710, 710, 710, 710, 752, -1, -1, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 704, 704, 704, 704, 704, 704, 704, 704, 704, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 805, -1, -1, -1, -1, -1, 117, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 805, 805, 805, 805, 805, 805, 805, 805, 805, 805, 805, 805, 805, 805, 805, 805, 805, 805, 805, 805, 805, 805, 805, 805 },
			{ -1, 717, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 717, 717, 717, 717, 717, 717, 717, 717, 717, 717, 717, 717, 717, 717, 717, 717, 717, 717, 717, 717, 717, 717, 717, 717 },
			{ -1, 808, -1, -1, -1, -1, -1, 155, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 808, 808, 808, 808, 808, 808, 808, 808, 808, 808, 808, 808, 808, 808, 808, 808, 808, 808, 808, 808, 808, 808, 808, 808 },
			{ -1, 702, -1, -1, 300, 300, -1, 107, -1, -1, 300, 300, 312, 6, 6, 6, 687, 795, 795, 795, 795, 795, -1, -1, 312, 702, 702, 702, 702, 702, 702, 702, 743, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702 },
			{ -1, 80, -1, -1, -1, 492, 493, -1, 494, -1, 494, 495, -1, 736, 736, 736, 736, 736, 812, 812, 812, 812, 495, -1, -1, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80 },
			{ -1, 403, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 761, 403, 403, 403, 403, 403 },
			{ -1, -1, -1, -1, 339, 339, -1, -1, -1, -1, 339, 339, -1, 932, 945, 945, 950, 11, 11, 11, 11, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 817, 817, 817, 817, 817, 817, 817, 817, 817, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 938, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 938, 938, 938, 938, 938, 938, 938, 938, 938, 938, 938, 938, 938, 938, 938, 938, 938, 938, 938, 938, 938, 938, 938, 938 },
			{ -1, 403, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 820, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403 },
			{ -1, 501, -1, -1, 446, 921, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 865, -1, -1, 826, -1, -1, -1, -1, 502, -1, -1, -1, -1, -1, 503, 775, -1, -1, 827, -1, 847 },
			{ -1, 937, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 937, 937, 937, 937, 937, 937, 937, 937, 937, 937, 937, 937, 937, 937, 937, 937, 937, 937, 937, 937, 937, 937, 937, 937 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 789, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 885, 885, 885, 885, 885, 885, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 89, -1, -1, 300, 300, -1, 107, -1, -1, 300, 300, -1, 6, 6, 6, 687, 795, 795, 795, 795, 795, -1, -1, -1, 89, 89, 89, 89, 89, 89, 89, 213, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89 },
			{ -1, 298, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 916, 298, 298, 298, 298, 298, 298, 298 },
			{ -1, -1, -1, -1, 339, 339, -1, -1, -1, -1, 917, 339, -1, 11, 11, 11, 11, 11, 11, 11, 11, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 819, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 339, 339, -1, -1, -1, -1, 917, 339, -1, 11, 11, 11, 11, 11, 11, 11, 11, 11, -1, -1, -1, -1, -1, -1, -1, 486, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 89, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 89, 89, 89, 89, 89, 89, 89, 935, 89, 89, 1040, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89 },
			{ -1, 298, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 955, 298, 298, 298, 298, 298, 298 },
			{ -1, 112, -1, -1, -1, 368, 369, -1, 370, -1, 371, 372, -1, 695, 695, 695, 695, 695, 695, 695, 695, 695, 373, -1, -1, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 901, 797, 797, 797, 797, 797, 797, 797, 797, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 403, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 964, 403, 403, 403, 403, 403, 403 },
			{ -1, 403, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 896, 403, 403, 403, 1030, 403, 403, 403, 403, 403, 403 },
			{ -1, 702, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 702, 702, 702, 702, 702, 850, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702 },
			{ -1, 112, -1, -1, -1, 368, 369, -1, 370, -1, 371, 751, -1, 710, 710, 710, 710, 710, 710, 710, 710, 710, 752, -1, -1, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112, 112 },
			{ -1, 80, -1, -1, -1, 492, 493, -1, 494, -1, 494, 495, -1, 812, 812, 812, 812, 812, 812, 812, 812, 812, 495, -1, -1, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80 },
			{ -1, 403, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 820, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 1001, 1001, 1001, 1001, 1001, 1001, 1001, 1001, 1001, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 403, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 403, 882, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403 },
			{ -1, 298, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 952, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298 },
			{ -1, 89, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 946, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 797, 797, 797, 797, 797, 797, 797, 797, 797, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 702, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 859, 702, 702, 702, 702, 702, 702 },
			{ -1, 403, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 403, 882, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 863, 863, 863, 863, 863, 863, 863, 863, 863, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 403, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 888, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 797, 797, 901, 901, 901, 901, 901, 901, 901, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 702, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 998, 702 },
			{ -1, 403, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 884, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403 },
			{ -1, 403, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 403, 403, 403, 403, 889, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403 },
			{ -1, 702, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 801, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702 },
			{ -1, 403, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 403, 403, 403, 886, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403 },
			{ -1, 403, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 403, 403, 403, 403, 403, 890, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403 },
			{ -1, 702, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 801, 702 },
			{ -1, 403, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 403, 403, 403, 886, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403 },
			{ -1, 702, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 702, 702, 702, 872, 702, 702, 702, 702, 702, 702, 702, 1010, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702 },
			{ -1, 403, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 761, 403, 403, 403, 403, 403 },
			{ -1, 403, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 891, 403, 403, 403, 403, 403, 403, 403 },
			{ -1, 403, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 892, 403, 403, 403, 403 },
			{ -1, 403, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 403, 403, 403, 893, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403 },
			{ -1, 403, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 403, 403, 403, 403, 403, 897, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403 },
			{ -1, 298, -1, -1, 300, 300, -1, 107, -1, -1, 361, 300, 312, 6, 6, 6, 687, 795, 795, 795, 795, 795, -1, -1, 312, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 940, 298, 298, 298, 298 },
			{ -1, -1, -1, -1, 339, 339, -1, -1, -1, -1, 917, 339, -1, 11, 11, 11, 11, 11, 11, 11, 11, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 702, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 926, 702, 702, 702, 702 },
			{ -1, -1, -1, -1, 339, 339, -1, -1, -1, -1, 917, 339, -1, 11, 11, 11, 11, 11, 11, 11, 11, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 406, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 339, 339, -1, -1, -1, -1, 917, 339, -1, 11, 11, 11, 11, 11, 11, 11, 11, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 427, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 339, 339, -1, -1, -1, -1, 917, 339, -1, 11, 11, 11, 11, 11, 11, 11, 11, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 428, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 339, 339, -1, -1, -1, -1, 917, 339, -1, 11, 11, 11, 11, 11, 11, 11, 11, 11, -1, -1, -1, -1, -1, -1, -1, 429, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 339, 339, -1, -1, -1, -1, 917, 339, -1, 11, 11, 11, 11, 11, 11, 11, 11, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 71 },
			{ -1, -1, -1, -1, 339, 339, -1, -1, -1, -1, 917, 339, -1, 11, 11, 11, 11, 11, 11, 11, 11, 11, -1, -1, -1, -1, -1, -1, -1, 71, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 339, 339, -1, -1, -1, -1, 917, 339, -1, 11, 11, 11, 11, 11, 11, 11, 11, 11, -1, -1, -1, -1, -1, 430, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 339, 339, -1, -1, -1, -1, 917, 339, -1, 11, 11, 11, 11, 11, 11, 11, 11, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 434, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 339, 339, -1, -1, -1, -1, 917, 339, -1, 11, 11, 11, 11, 11, 11, 11, 11, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 928, -1, -1, -1, -1, -1 },
			{ -1, 403, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 920, 403, 403, 403, 403 },
			{ -1, 844, -1, -1, 446, 921, -1, 107, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 403, 403, 403, 855, 403, 403, 949, 403, 403, 403, 403, 864, 403, 403, 403, 403, 403, 870, 875, 403, 403, 879, 403, 953 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 969, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 970, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 971, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 972, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 927, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 973, -1, 974, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 975, -1, -1, -1, -1, -1, -1, 967 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 972, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 976, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 977, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 403, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 960, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403 },
			{ -1, 844, -1, -1, 446, 447, -1, -1, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 403, 403, 403, 855, 403, 403, 949, 403, 403, 403, 403, 864, 403, 403, 403, 403, 403, 870, 875, 403, 403, 879, 403, 953 },
			{ -1, 298, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 1008, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 987, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 986, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 983, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 403, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 404, 403, 403, 403, 403, 403 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 989, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 298, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 979, 298, 298, 298, 298, 298 },
			{ -1, 298, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 1016, 298, 298, 298, 298, 298, 298, 298 },
			{ -1, 403, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 1019, 403, 403, 403, 403, 403, 403, 403 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 948, 948, 948, 948, 948, 948, 948, 948, 948, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 298, -1, -1, 300, 300, -1, 107, -1, -1, 361, 300, 312, 6, 6, 6, 687, 795, 795, 795, 795, 795, -1, -1, 312, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 947, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298 },
			{ -1, 403, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 942, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403 },
			{ -1, 403, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 962, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403 },
			{ -1, 298, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 298, 298, 298, 298, 1014, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298 },
			{ -1, 298, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 1036, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298 },
			{ -1, 403, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 1034, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403 },
			{ -1, 403, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 403, 403, 403, 403, 403, 403, 403, 956, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403 },
			{ -1, 403, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 942, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403 },
			{ -1, 298, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 1018, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298 },
			{ -1, 403, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 958, 403, 403, 403, 403, 403, 403, 403 },
			{ -1, 403, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 958, 403, 403, 403, 403, 403, 403 },
			{ -1, 298, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 1020, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298 },
			{ -1, 403, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 960, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403 },
			{ -1, 403, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 963, 403, 403, 403, 403, 403, 403, 403 },
			{ -1, 403, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 958, 403, 403, 403, 403, 403, 403 },
			{ -1, 403, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 964, 403, 403, 403, 403, 403, 403, 403 },
			{ -1, 403, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 961, 403, 403, 403, 403, 403, 403, 403 },
			{ -1, 960, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403 },
			{ -1, 403, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 403, 403, 403, 403, 403, 403, 403, 965, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403 },
			{ -1, 298, -1, -1, 300, 300, -1, 107, -1, -1, 361, 300, 312, 6, 6, 6, 687, 795, 795, 795, 795, 795, -1, -1, 312, 298, 298, 298, 298, 1003, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298 },
			{ -1, 702, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 702, 702, 702, 992, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 981, -1, -1, -1, -1, 982, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 984, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 985, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 988, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 403, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 403, 403, 403, 403, 990, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403 },
			{ -1, 403, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 403, 403, 403, 403, 403, 403, 1037, 403, 403, 403, 403, 403, 1021, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403 },
			{ -1, 298, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 1027, 298, 298, 298 },
			{ -1, 298, -1, -1, 300, 300, -1, 107, -1, -1, 361, 300, 312, 6, 6, 6, 687, 795, 795, 795, 795, 795, -1, -1, 312, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298 },
			{ -1, 702, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 702, 702, 702, 702, 1013, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702 },
			{ -1, 403, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 403, 403, 403, 403, 403, 403, 403, 1004, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403 },
			{ -1, 403, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 403, 403, 403, 403, 1019, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403 },
			{ -1, 298, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 1033, 298, 298, 298 },
			{ -1, 403, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 403, 403, 403, 403, 1012, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403 },
			{ -1, 403, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 1021, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403 },
			{ -1, 298, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 298, 1039, 298, 298, 298, 298, 298 },
			{ -1, 403, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 403, 403, 403, 403, 1015, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403 },
			{ -1, 702, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 702, 702, 702, 702, 1029, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702 },
			{ -1, 403, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 1028, 403, 403, 403, 403, 403 },
			{ -1, 89, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 1043, 89, 89, 89, 89, 89, 89, 89 },
			{ -1, 702, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 702, 702, 702, 702, 1035, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702 },
			{ -1, 89, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 89, 89, 89, 89, 1045, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89, 89 },
			{ -1, 702, -1, -1, -1, -1, -1, 107, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 702, 1038, 702, 702, 702, 702, 702, 702 },
			{ -1, 403, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 403, 1041, 403, 403, 403, 403, 403, 403, 403 }
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
							System.Diagnostics.Debug.Assert(last_accept_state >= 1047);
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

