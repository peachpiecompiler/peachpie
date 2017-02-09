using System;
using System.Text;
using System.Collections.Generic;

%%

%namespace Pchp.Library.Json
%type Tokens
%class Lexer
%eofval Tokens.EOF
%errorval Tokens.ERROR
%attributes internal partial
%charmap Map
%function GetNextToken
%ignorecase

%{

// content of the STRING literal text
protected string yytext()
{
	return new String(buffer, token_start, token_end - token_start);
}

private StringBuilder str = null;
protected string QuotedStringContent{get{return str.ToString();}}

%}

e			e[\+|\-]?
hex			[0-9a-fA-F]
digit		[0-9]
digit1		[1-9]
digits		{digit}+
exp			{e}{digits}
frac		"."{digits}
int			-?{digits}
double		{int}({frac}{exp}|{frac}|{exp})
integer		{int}

quote		\"
charUnicode	\\u{hex}{hex}{hex}{hex}
charNormal	[^\"\\]
charEscaped	[\\f|\\b|\\r|\\n|\\t|\\\\|\\/|\\\"]

whitespace	[\r|\n|\t| ]	// whitespaces allowed between tokens


%x INITIAL
%x IN_STRING

%%

<INITIAL>"["		{return Tokens.ARRAY_OPEN;}
<INITIAL>"]"		{return Tokens.ARRAY_CLOSE;}
<INITIAL>","		{return Tokens.ITEMS_SEPARATOR;}
<INITIAL>":"		{return Tokens.NAMEVALUE_SEPARATOR;}
<INITIAL>"{"		{return Tokens.OBJECT_OPEN;}
<INITIAL>"}"		{return Tokens.OBJECT_CLOSE;}
<INITIAL>"true"		{return Tokens.TRUE;}
<INITIAL>"false"	{return Tokens.FALSE;}
<INITIAL>"null"		{return Tokens.NULL;}
<INITIAL>{double}	{return Tokens.DOUBLE;}
<INITIAL>{integer}	{return Tokens.INTEGER;}
<INITIAL>{whitespace}	{}

<INITIAL>{quote}			{BEGIN(LexicalStates.IN_STRING); str = new StringBuilder(); return Tokens.STRING_BEGIN;}
<IN_STRING>{charNormal}+	{str.Append(yytext()); return Tokens.CHARS;}
<IN_STRING>{charUnicode}	{str.Append((char)int.Parse(yytext().Substring(2), System.Globalization.NumberStyles.HexNumber)); return Tokens.UNICODECHAR;}
<IN_STRING>"\\f"			{str.Append('\f'); return Tokens.ESCAPEDCHAR;}
<IN_STRING>"\\b"			{str.Append('\b'); return Tokens.ESCAPEDCHAR;}
<IN_STRING>"\\r"			{str.Append('\r'); return Tokens.ESCAPEDCHAR;}
<IN_STRING>"\\n"			{str.Append('\n'); return Tokens.ESCAPEDCHAR;}
<IN_STRING>"\\t"			{str.Append('\t'); return Tokens.ESCAPEDCHAR;}
<IN_STRING>"\\\\"			{str.Append('\\'); return Tokens.ESCAPEDCHAR;}
<IN_STRING>"\\/"			{str.Append('/'); return Tokens.ESCAPEDCHAR;}
<IN_STRING>"\\\""			{str.Append('"'); return Tokens.ESCAPEDCHAR;}
<IN_STRING>{quote}			{BEGIN(LexicalStates.INITIAL); return Tokens.STRING_END;}
