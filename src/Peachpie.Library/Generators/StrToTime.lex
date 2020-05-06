
using System;
using System.Collections.Generic;

/*

 Copyright (c) 2005-2006 Tomas Matousek. Based on PHP5 implementation by Derick Rethans <derick@derickrethans.nl>. 

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

%%

%namespace Pchp.Library.DateTime
%type Tokens
%eofval Tokens.EOF
%errorval Tokens.ERROR
%attributes internal
%class Scanner
%function GetNextToken

%{

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

%}

any [\000-\377]

frac "."[0-9]+

az14            [a-z]|[a-z][a-z]|[a-z][a-z][a-z]|[a-z][a-z][a-z][a-z]

ago "ago"

hour24					[01]?[0-9]|"2"[0-4]
hour24lz				[01][0-9]|"2"[0-4]
hour12					"0"?[1-9]|"1"[0-2]
minute					[0-5]?[0-9]
minutelz				[0-5][0-9]
second					{minute}|"60"
secondlz				{minutelz}|"60"
meridian				[ap]"."?m"."?
daysuf          "st"|"nd"|"rd"|"th"

tz							"("?{az14}")"?|[a-z][a-z]+([_/][a-z][a-z]+)+
tzcorrection		[+-]{hour24}":"?{minute}?
zone            {tzcorrection}|{tz}

month						"0"?[0-9]|"1"[0-2]
day							([0-2]?[0-9]|"3"[01]){daysuf}?
year						[0-9]|[0-9][0-9]|[0-9][0-9][0-9]|[0-9][0-9][0-9][0-9]
year2						[0-9][0-9]
year4						[0-9][0-9][0-9][0-9]

dayofyear				"00"[1-9]|"0"[1-9][0-9]|[1-2][0-9][0-9]|"3"[0-5][0-9]|"36"[0-6]
weekofyear			"0"[1-9]|[1-4][0-9]|"5"[0-3]

monthlz					"0"[1-9]|"1"[0-2]
daylz						"0"[1-9]|[1-2][0-9]|"3"[01]

dayfull					"sunday"|"monday"|"tuesday"|"wednesday"|"thursday"|"friday"|"saturday"
dayabbr					"sun"|"mon"|"tue"|"wed"|"thu"|"fri"|"sat"|"sun"
daytext					{dayfull}|{dayabbr}

monthfull				"january"|"february"|"march"|"april"|"may"|"june"|"july"|"august"|"september"|"october"|"november"|"december"

monthabbr				"jan"|"feb"|"mar"|"apr"|"may"|"jun"|"jul"|"aug"|"sep"|"sept"|"oct"|"nov"|"dec"
monthroman			"i"|"ii"|"iii"|"iv"|"v"|"vi"|"vii"|"viii"|"ix"|"x"|"xi"|"xii"
monthtext				{monthfull}|{monthabbr}|{monthroman}

timetiny12      {hour12}[ ]*{meridian}
timeshort12			{hour12}[:.]{minutelz}[ ]*{meridian}
timelong12			{hour12}[:.]{minute}[:.]{secondlz}[ ]*{meridian}

timeshort24			{hour24}[:.]{minute}([ ]*{zone})?
timelong24			{hour24}[:.]{minute}[:.]{second}([ ]*{zone})?
iso8601long			{hour24}[:.]{minute}[:.]{second}{frac}([ ]*{zone})?

iso8601normtz		{hour24}[:.]{minute}[:.]{secondlz}[ ]*{zone}

gnunocolon			{hour24lz}{minutelz}
iso8601nocolon	{hour24lz}{minutelz}{secondlz}

americanshort		{month}"/"{day}
american				{month}"/"{day}"/"{year}
iso8601dateslash	{year4}"/"{monthlz}"/"{daylz}"/"?
pointeddate4		{day}[-.]{month}[-.]{year4}
gnudateshort		{year}"-"{month}"-"{day}
iso8601date			{year4}"-"{monthlz}"-"{daylz}
pointeddate2		{day}"."{month}"."{year2}
datefull				{day}([-. ])*{monthtext}([-. ])*{year}
datenoday				{monthtext}([-. ])*{year4}
datenodayrev		{year4}([-. ])*{monthtext}
datetextual			{monthtext}([-. ])*{day}[,.stndrh ]*{year}
datenoyear			{monthtext}([-. ])*{day}[,.stndrh ]*
datenoyearrev		{day}([-. ])*{monthtext}
datenocolon			{year4}{monthlz}{daylz}

soap						{year4}"-"{monthlz}"-"{daylz}"t"{hour24lz}":"{minutelz}":"{secondlz}{frac}{tzcorrection}?
xmlrpc					{year4}{monthlz}{daylz}"t"{hour24}":"{minutelz}":"{secondlz}
xmlrpcnocolon		{year4}{monthlz}{daylz}"t"{hour24}{minutelz}{secondlz}
wddx						{year4}"-"{month}"-"{day}"t"{hour24}":"{minute}":"{second}
pgydotd					{year4}"."?{dayofyear}
pgtextshort			{monthabbr}"-"{daylz}"-"{year}
pgtextreverse		{year}"-"{monthabbr}"-"{daylz}
isoweekday			{year4}"w"{weekofyear}[0-7]
isoweek					{year4}"w"{weekofyear}

clf							{day}"/"{monthabbr}"/"{year4}":"{hour24lz}":"{minutelz}":"{secondlz}[ ]+{tzcorrection}

timestamp				"@""-"?[1-9][0-9]*

dateshortwithtimeshort	{datenoyear}{timeshort24}

dateshortwithtimelong		{datenoyear}{timelong24}
dateshortwithtimelongtz	{datenoyear}{iso8601normtz}

reltextnumber		"first"|"next"|"second"|"third"|"fourth"|"fifth"|"sixth"|"seventh"|"eight"|"ninth"|"tenth"|"eleventh"|"twelfth"|"last"|"previous"|"this"
reltextunit			(("sec"|"second"|"min"|"minute"|"hour"|"day"|"week"|"fortnight"|"forthnight"|"month"|"year")"s"?)|{daytext}

relnumber						([+-]?[ ]*[0-9]+)
relative						{relnumber}[ ]*{reltextunit}
relativetext				{reltextnumber}[ ]*{reltextunit}

%%

<YYINITIAL>"yesterday" {
	INIT();
	time.HAVE_RELATIVE();
	time.UNHAVE_TIME();

	time.relative.d = -1;
	DEINIT();
	return Tokens.RELATIVE;
}

<YYINITIAL>"now" {
	INIT();

	DEINIT();
	return Tokens.RELATIVE;
}

<YYINITIAL>"noon" {
	INIT();
	time.UNHAVE_TIME();
	time.HAVE_TIME();
	time.h = 12;

	DEINIT();
	return Tokens.RELATIVE;
}

<YYINITIAL>"midnight"|"today" {
	INIT();
	time.UNHAVE_TIME();

	DEINIT();
	return Tokens.RELATIVE;
}

<YYINITIAL>"tomorrow" {
	INIT();
	time.HAVE_RELATIVE();
	time.UNHAVE_TIME();

	time.relative.d = 1;
	DEINIT();
	return Tokens.RELATIVE;
}

<YYINITIAL>{timestamp} {
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

<YYINITIAL>{timetiny12}|{timeshort12}|{timelong12} {
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

<YYINITIAL>{timeshort24}|{timelong24}|{iso8601long} {
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

<YYINITIAL>{gnunocolon} {
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

<YYINITIAL>{iso8601nocolon} {
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

<YYINITIAL>{americanshort}|{american} {
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

<YYINITIAL>{iso8601date}|{iso8601dateslash} {
	INIT();
	if (time.have_date!=0) { return Tokens.ERROR; } 
	time.HAVE_DATE();
	
	time.y = DateInfo.ParseUnsignedInt(str, ref pos, 4);
	time.m = DateInfo.ParseUnsignedInt(str, ref pos, 2);
	time.d = DateInfo.ParseUnsignedInt(str, ref pos, 2);
	
	DEINIT();
	return Tokens.ISO_DATE;
}

<YYINITIAL>{datefull} {
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

<YYINITIAL>{gnudateshort} {
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

<YYINITIAL>{pointeddate4} {
	INIT();
	if (time.have_date!=0) { return Tokens.ERROR;} 
	time.HAVE_DATE();
	
	time.d = DateInfo.ParseUnsignedInt(str, ref pos, 2);
	time.m = DateInfo.ParseUnsignedInt(str, ref pos, 2);
	time.y = DateInfo.ParseUnsignedInt(str, ref pos, 4);
	
	DEINIT();
	return Tokens.DATE_FULL_POINTED;
}

<YYINITIAL>{pointeddate2} {
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

<YYINITIAL>{datenoday} {
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

<YYINITIAL>{datenodayrev} {
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

<YYINITIAL>{datetextual}|{datenoyear}
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

<YYINITIAL>{datenoyearrev} {
	INIT();
	if (time.have_date!=0) { return Tokens.ERROR; } 
	time.HAVE_DATE();
	
	time.d = DateInfo.ParseUnsignedInt(str, ref pos, 2);
	DateInfo.SkipDaySuffix(str, ref pos);
	time.m = DateInfo.ParseMonth(str, ref pos);
	
	DEINIT();
	return Tokens.DATE_TEXT;
}

<YYINITIAL>{datenocolon} {
	INIT();
	if (time.have_date!=0) { return Tokens.ERROR; } 
	time.HAVE_DATE();
	
	time.y = DateInfo.ParseUnsignedInt(str, ref pos, 4);
	time.m = DateInfo.ParseUnsignedInt(str, ref pos, 2);
	time.d = DateInfo.ParseUnsignedInt(str, ref pos, 2);
	
	DEINIT();
	return Tokens.DATE_NOCOLON;
}

<YYINITIAL>{xmlrpc}|{xmlrpcnocolon}|{soap}|{wddx} {
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

<YYINITIAL>{pgydotd} {
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

<YYINITIAL>{isoweekday} {
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

<YYINITIAL>{isoweek} {
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

<YYINITIAL>{pgtextshort} {
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

<YYINITIAL>{pgtextreverse} {
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

<YYINITIAL>{clf} {
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

<YYINITIAL>{year4} {
	INIT();
	
	time.y = DateInfo.ParseUnsignedInt(str, ref pos, 4);
	
	DEINIT();
	return Tokens.CLF;
}

<YYINITIAL>{ago} {
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

<YYINITIAL>{relativetext} {
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

<YYINITIAL>{daytext} {
	INIT();
	time.HAVE_RELATIVE();
	time.HAVE_WEEKDAY_RELATIVE();
	time.UNHAVE_TIME();

	time.SetWeekDay(DateInfo.ReadToSpace(str,ref pos));
  time.relative.weekday_behavior = 1;
  	
	DEINIT();
	return Tokens.WEEKDAY;
}

<YYINITIAL>{tzcorrection}|{tz}
{
	INIT();
	
	errors += time.SetTimeZone(str, ref pos) ? 0 : 1;
	
	DEINIT();
	return Tokens.TIMEZONE;
}

<YYINITIAL>{dateshortwithtimeshort}|{dateshortwithtimelong}|{dateshortwithtimelongtz} {
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

<YYINITIAL>{relative} {
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

<YYINITIAL>[ .,\0\n\r\t] {
  break;
}

<YYINITIAL>{any} {
  return Tokens.ERROR;
}
