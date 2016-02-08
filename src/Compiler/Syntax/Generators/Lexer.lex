/*

 Copyright (c) 2004-2006 Tomas Matousek. Based on PHP5 and PHP6 grammar tokens definition. 

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using PHP.Core;

using System.Collections.Generic;

%%

%namespace PHP.Core.Parsers
%type Tokens
%class Lexer
%eofval Tokens.EOF
%errorval Tokens.ERROR
%attributes public partial
%function GetNextToken
%ignorecase
%charmap Map
%char

%x INITIAL
%x ST_IN_SCRIPTING
%x ST_DOUBLE_QUOTES
%x ST_SINGLE_QUOTES
%x ST_BACKQUOTE
%x ST_HEREDOC
%x ST_NEWDOC
%x ST_LOOKING_FOR_PROPERTY
%x ST_LOOKING_FOR_VARNAME
%x ST_DOC_COMMENT
%x ST_COMMENT
%x ST_ONE_LINE_COMMENT

HexDigit                [0-9A-Fa-f]

LNUM                    [0-9]+
DNUM                    ([0-9]*[.][0-9]+)|([0-9]+[.][0-9]*)
EXPONENT_DNUM           (({LNUM}|{DNUM})[eE][+-]?{LNUM})
HNUM                    "0x"{HexDigit}+
BNUM					"0b"[01]+
LABEL                   [a-zA-Z_][a-zA-Z0-9_]*
WHITESPACE              [ \n\r\t]+
TABS_AND_SPACES         [ \t]*
TOKENS                  [;:,.\[\]()|^&+-/*=%!~$<>?@]
ESCAPED_AND_WHITESPACE  [\n\t\r #'.:;,()|^&+-/*=%!~<>?@]+
ANY_CHAR                (.|[\n\r])
NEWLINE                 ("\r"|"\n"|"\r\n")
NS_SEPARATOR			("\\")

SlashedOctalNumber      "\\"[0-7][0-7]?[0-7]?
SlashedHexNumber        "\\"[x]{HexDigit}{HexDigit}?
SlashedUnicodeCharCode  "\\"[uU]{HexDigit}?{HexDigit}?{HexDigit}?{HexDigit}?{HexDigit}?{HexDigit}?
SlashedUnicodeCharName  "\\"[C]("{"[A-Z0-9 -]+"}")?
SlashedSingleQuote      "\\'"
SlashedDoubleQuotes     "\\\""
SlashedBackQuote        "\\`"
SlashedSlash            "\\\\"
SlashedSpecialChar      "\\"[ntr\\$]
SlashedOpenBrace        "\\{"
SlashedAnyChar          "\\"{ANY_CHAR}
NonVariableStart        [^a-zA-Z_{]

%%

<INITIAL>(([^<]|"<"[^?%s<])+)|"<" { 
	return Tokens.T_INLINE_HTML; 
}

<INITIAL>"<?"|"<script"{WHITESPACE}+"language"{WHITESPACE}*"="{WHITESPACE}*("php"|"\"php\""|"\'php\'"){WHITESPACE}*">" {
	if (AllowShortTags || TokenLength>2) 
	{ 
		BEGIN(LexicalStates.ST_IN_SCRIPTING);
		return Tokens.T_OPEN_TAG;
	} 
	else 
	{
		return Tokens.T_INLINE_HTML;
	}
}

<INITIAL>("<s"[^< \n\r\t]*) { 
	return Tokens.T_INLINE_HTML; 
}

<INITIAL>"<%="|"<?=" {
	if (GetTokenChar(1) != '%' || AllowAspTags) 
	{
		BEGIN(LexicalStates.ST_IN_SCRIPTING);
		return Tokens.T_OPEN_TAG_WITH_ECHO;
	} 
	else 
	{
		return Tokens.T_INLINE_HTML;
	}
}

<INITIAL>"<%" { 
	if (AllowAspTags)
	{
		BEGIN(LexicalStates.ST_IN_SCRIPTING);
		return Tokens.T_OPEN_TAG;
	} 
	else 
	{
		return Tokens.T_INLINE_HTML;
	}
}

<INITIAL>"<?php"([ \t]|{NEWLINE}) {
	BEGIN(LexicalStates.ST_IN_SCRIPTING);
	return Tokens.T_OPEN_TAG;
}

<INITIAL>{ANY_CHAR}    { return Tokens.ERROR; }


<ST_IN_SCRIPTING>("?>"|"</script"{WHITESPACE}*">"){NEWLINE}? { 
	BEGIN(LexicalStates.INITIAL); 
	return Tokens.T_CLOSE_TAG; 
}

<ST_IN_SCRIPTING>"exit"       			{ return Tokens.T_EXIT; }
<ST_IN_SCRIPTING>"die"        			{ return Tokens.T_EXIT; }
<ST_IN_SCRIPTING>"function"   			{ return Tokens.T_FUNCTION; }
<ST_IN_SCRIPTING>"const"      			{ return Tokens.T_CONST; }
<ST_IN_SCRIPTING>"return"     			{ return Tokens.T_RETURN; }
<ST_IN_SCRIPTING>"yield"     			{ return Tokens.T_YIELD; }
<ST_IN_SCRIPTING>"if"         			{ return Tokens.T_IF; }
<ST_IN_SCRIPTING>"elseif"     			{ return Tokens.T_ELSEIF; }
<ST_IN_SCRIPTING>"endif"      			{ return Tokens.T_ENDIF; }
<ST_IN_SCRIPTING>"else"       			{ return Tokens.T_ELSE; }
<ST_IN_SCRIPTING>"while"      			{ return Tokens.T_WHILE; }
<ST_IN_SCRIPTING>"endwhile"   			{ return Tokens.T_ENDWHILE; }
<ST_IN_SCRIPTING>"do"         			{ return Tokens.T_DO; }
<ST_IN_SCRIPTING>"for"        			{ return Tokens.T_FOR; }
<ST_IN_SCRIPTING>"endfor"     			{ return Tokens.T_ENDFOR; }
<ST_IN_SCRIPTING>"foreach"    			{ return Tokens.T_FOREACH; }
<ST_IN_SCRIPTING>"endforeach" 			{ return Tokens.T_ENDFOREACH; }
<ST_IN_SCRIPTING>"declare" 			    { return Tokens.T_DECLARE; }
<ST_IN_SCRIPTING>"enddeclare" 			{ return Tokens.T_ENDDECLARE; }
<ST_IN_SCRIPTING>"as"         			{ return Tokens.T_AS; }
<ST_IN_SCRIPTING>"switch"     			{ return Tokens.T_SWITCH; }
<ST_IN_SCRIPTING>"endswitch"  			{ return Tokens.T_ENDSWITCH; }
<ST_IN_SCRIPTING>"case"       			{ return Tokens.T_CASE; }
<ST_IN_SCRIPTING>"default"    			{ return Tokens.T_DEFAULT; }
<ST_IN_SCRIPTING>"break"      			{ return Tokens.T_BREAK; }
<ST_IN_SCRIPTING>"continue"   			{ return Tokens.T_CONTINUE; }
<ST_IN_SCRIPTING>"echo"       			{ return Tokens.T_ECHO; }
<ST_IN_SCRIPTING>"print"      			{ return Tokens.T_PRINT; }
<ST_IN_SCRIPTING>"class"      			{ return Tokens.T_CLASS; }
<ST_IN_SCRIPTING>"trait"      			{ return Tokens.T_TRAIT; }
<ST_IN_SCRIPTING>"insteadof"      		{ return Tokens.T_INSTEADOF; }
<ST_IN_SCRIPTING>"extends"    			{ return Tokens.T_EXTENDS; }
<ST_IN_SCRIPTING>"new"        			{ return Tokens.T_NEW; }
<ST_IN_SCRIPTING>"var"        			{ return Tokens.T_VAR; }
<ST_IN_SCRIPTING>"eval"							{ return Tokens.T_EVAL; }
<ST_IN_SCRIPTING>"include"					{ return Tokens.T_INCLUDE; }
<ST_IN_SCRIPTING>"include_once" 		{ return Tokens.T_INCLUDE_ONCE; }
<ST_IN_SCRIPTING>"require"					{ return Tokens.T_REQUIRE; }
<ST_IN_SCRIPTING>"require_once" 		{ return Tokens.T_REQUIRE_ONCE; }
<ST_IN_SCRIPTING>"global"						{ return Tokens.T_GLOBAL; }
<ST_IN_SCRIPTING>"isset"						{ return Tokens.T_ISSET; }
<ST_IN_SCRIPTING>"empty"						{ return Tokens.T_EMPTY; }
<ST_IN_SCRIPTING>"static"						{ return Tokens.T_STATIC; }
<ST_IN_SCRIPTING>"unset"						{ return Tokens.T_UNSET; }
<ST_IN_SCRIPTING>"or" 							{ return Tokens.T_LOGICAL_OR; }
<ST_IN_SCRIPTING>"and"							{ return Tokens.T_LOGICAL_AND; }
<ST_IN_SCRIPTING>"xor"							{ return Tokens.T_LOGICAL_XOR; }
<ST_IN_SCRIPTING>"list"							{ return Tokens.T_LIST; }
<ST_IN_SCRIPTING>"array"			 			{ return Tokens.T_ARRAY; }
<ST_IN_SCRIPTING>"callable"			 			{ return Tokens.T_CALLABLE; }
<ST_IN_SCRIPTING>"__CLASS__"    		{ return Tokens.T_CLASS_C; }
<ST_IN_SCRIPTING>"__TRAIT__"    		{ return Tokens.T_TRAIT_C; }
<ST_IN_SCRIPTING>"__FUNCTION__" 		{ return Tokens.T_FUNC_C; }
<ST_IN_SCRIPTING>"__METHOD__"   		{ return Tokens.T_METHOD_C; }
<ST_IN_SCRIPTING>"__LINE__"     		{ return Tokens.T_LINE; }
<ST_IN_SCRIPTING>"__FILE__"     		{ return Tokens.T_FILE; } 
<ST_IN_SCRIPTING>"__DIR__"     			{ return Tokens.T_DIR; } 

<ST_IN_SCRIPTING>"try"              { return Tokens.T_TRY; }
<ST_IN_SCRIPTING>"catch"            { return Tokens.T_CATCH; }
<ST_IN_SCRIPTING>"finally"          { return Tokens.T_FINALLY; }
<ST_IN_SCRIPTING>"throw"            { return Tokens.T_THROW; }
<ST_IN_SCRIPTING>"interface"        { return Tokens.T_INTERFACE; } 
<ST_IN_SCRIPTING>"implements"       { return Tokens.T_IMPLEMENTS; }
<ST_IN_SCRIPTING>"clone"            { return Tokens.T_CLONE; } 
<ST_IN_SCRIPTING>"abstract"         { return Tokens.T_ABSTRACT; }
<ST_IN_SCRIPTING>"final"            { return Tokens.T_FINAL; }
<ST_IN_SCRIPTING>"private"          { return Tokens.T_PRIVATE; }
<ST_IN_SCRIPTING>"protected"        { return Tokens.T_PROTECTED; }
<ST_IN_SCRIPTING>"public"           { return Tokens.T_PUBLIC; }
<ST_IN_SCRIPTING>"instanceof"       { return Tokens.T_INSTANCEOF; }

<ST_IN_SCRIPTING>"__NAMESPACE__"    { return Tokens.T_NAMESPACE_C; }
<ST_IN_SCRIPTING>"namespace"        { return Tokens.T_NAMESPACE; }
<ST_IN_SCRIPTING>"use"				{ return Tokens.T_USE; }
<ST_IN_SCRIPTING>"import"			{ return Tokens.T_IMPORT; }
<ST_IN_SCRIPTING>"goto"             { return Tokens.T_GOTO; }

<ST_IN_SCRIPTING>"bool"             { return Tokens.T_BOOL_TYPE; }
<ST_IN_SCRIPTING>"int"              { return Tokens.T_INT_TYPE; }
<ST_IN_SCRIPTING>"int64"            { return Tokens.T_INT64_TYPE; }
<ST_IN_SCRIPTING>"double"           { return Tokens.T_DOUBLE_TYPE; }
<ST_IN_SCRIPTING>"string"           { return Tokens.T_STRING_TYPE; }
<ST_IN_SCRIPTING>"resource"         { return Tokens.T_RESOURCE_TYPE; }
<ST_IN_SCRIPTING>"object"           { return Tokens.T_OBJECT_TYPE; }
<ST_IN_SCRIPTING>"clrtypeof"        { return Tokens.T_TYPEOF; }

<ST_IN_SCRIPTING>"partial"          { return Tokens.T_PARTIAL; }

<ST_IN_SCRIPTING>"<:"           		{ return Tokens.T_LGENERIC; }
<ST_IN_SCRIPTING>":>"           		{ return Tokens.T_RGENERIC; }
                                		         
<ST_IN_SCRIPTING>"__get"        		{ return Tokens.T_GET; }
<ST_IN_SCRIPTING>"__set"        		{ return Tokens.T_SET; }
<ST_IN_SCRIPTING>"__call"       		{ return Tokens.T_CALL; }
<ST_IN_SCRIPTING>"__callStatic"       	{ return Tokens.T_CALLSTATIC; }
<ST_IN_SCRIPTING>"__tostring"   		{ return Tokens.T_TOSTRING; }
<ST_IN_SCRIPTING>"__construct"  		{ return Tokens.T_CONSTRUCT; }
<ST_IN_SCRIPTING>"__destruct"   		{ return Tokens.T_DESTRUCT; }
<ST_IN_SCRIPTING>"__wakeup"     		{ return Tokens.T_WAKEUP; }
<ST_IN_SCRIPTING>"__sleep"      		{ return Tokens.T_SLEEP; }
<ST_IN_SCRIPTING>"parent"       		{ return Tokens.T_PARENT; }
<ST_IN_SCRIPTING>"self"         		{ return Tokens.T_SELF; }
<ST_IN_SCRIPTING>"__autoload"   		{ return Tokens.T_AUTOLOAD; }
<ST_IN_SCRIPTING>"true"         		{ return Tokens.T_TRUE; }
<ST_IN_SCRIPTING>"false"        		{ return Tokens.T_FALSE; }
<ST_IN_SCRIPTING>"null"					{ return Tokens.T_NULL; }

<ST_IN_SCRIPTING>"=>"           		{ return Tokens.T_DOUBLE_ARROW; }
<ST_IN_SCRIPTING>"++"           		{ return Tokens.T_INC; }
<ST_IN_SCRIPTING>"--"           		{ return Tokens.T_DEC; }
<ST_IN_SCRIPTING>"==="          		{ return Tokens.T_IS_IDENTICAL; }
<ST_IN_SCRIPTING>"!=="          		{ return Tokens.T_IS_NOT_IDENTICAL; }
<ST_IN_SCRIPTING>"=="           		{ return Tokens.T_IS_EQUAL; }
<ST_IN_SCRIPTING>"!="|"<>"      		{ return Tokens.T_IS_NOT_EQUAL; }
<ST_IN_SCRIPTING>"<="           		{ return Tokens.T_IS_SMALLER_OR_EQUAL; }
<ST_IN_SCRIPTING>">="           		{ return Tokens.T_IS_GREATER_OR_EQUAL; }
<ST_IN_SCRIPTING>"+="           		{ return Tokens.T_PLUS_EQUAL; }
<ST_IN_SCRIPTING>"-="           		{ return Tokens.T_MINUS_EQUAL; }
<ST_IN_SCRIPTING>"*="           		{ return Tokens.T_MUL_EQUAL; }
<ST_IN_SCRIPTING>"*\*"					{ return Tokens.T_POW; }
<ST_IN_SCRIPTING>"*\*="					{ return Tokens.T_POW_EQUAL; }
<ST_IN_SCRIPTING>"/="           		{ return Tokens.T_DIV_EQUAL; }
<ST_IN_SCRIPTING>".="           		{ return Tokens.T_CONCAT_EQUAL; }
<ST_IN_SCRIPTING>"%="           		{ return Tokens.T_MOD_EQUAL; }
<ST_IN_SCRIPTING>"<<="          		{ return Tokens.T_SL_EQUAL; }
<ST_IN_SCRIPTING>">>="          		{ return Tokens.T_SR_EQUAL; }
<ST_IN_SCRIPTING>"&="           		{ return Tokens.T_AND_EQUAL; }
<ST_IN_SCRIPTING>"|="           		{ return Tokens.T_OR_EQUAL; }
<ST_IN_SCRIPTING>"^="           		{ return Tokens.T_XOR_EQUAL; }
<ST_IN_SCRIPTING>"||"           		{ return Tokens.T_BOOLEAN_OR; }
<ST_IN_SCRIPTING>"&&"           		{ return Tokens.T_BOOLEAN_AND; }
<ST_IN_SCRIPTING>"<<"           		{ return Tokens.T_SL; }
<ST_IN_SCRIPTING>">>"           		{ return Tokens.T_SR; }
<ST_IN_SCRIPTING>"::"           		{ return Tokens.T_DOUBLE_COLON; }

<ST_IN_SCRIPTING>"->"           		{ yy_push_state(LexicalStates.ST_LOOKING_FOR_PROPERTY); return Tokens.T_OBJECT_OPERATOR; }
<ST_IN_SCRIPTING>"$"{LABEL}     		{ return Tokens.T_VARIABLE; }

<ST_IN_SCRIPTING>{TOKENS}          	{ return (Tokens)GetTokenChar(0); }
<ST_IN_SCRIPTING>{LABEL}           	{ return Tokens.T_STRING; }
<ST_IN_SCRIPTING>{NS_SEPARATOR}		{ return Tokens.T_NS_SEPARATOR; }
<ST_IN_SCRIPTING>"..."				{ return Tokens.T_ELLIPSIS; }
<ST_IN_SCRIPTING>{WHITESPACE}      	{ return Tokens.T_WHITESPACE; }
<ST_IN_SCRIPTING>{LNUM}            	{ return Tokens.ParseDecimalNumber; }
<ST_IN_SCRIPTING>{HNUM}            	{ return Tokens.ParseHexadecimalNumber; }
<ST_IN_SCRIPTING>{DNUM}            	{ return Tokens.ParseDouble; }
<ST_IN_SCRIPTING>{EXPONENT_DNUM}   	{ return Tokens.ParseDouble; } 
<ST_IN_SCRIPTING>{BNUM}            	{ return Tokens.ParseBinaryNumber; }

<ST_IN_SCRIPTING>"#pragma"[ \t]+"line"[ \t]+[-]?{LNUM}[ \t]* { BEGIN(LexicalStates.ST_ONE_LINE_COMMENT); return Tokens.T_PRAGMA_LINE; }
<ST_IN_SCRIPTING>"#pragma"[ \t]+"file"[^\n]+                 { BEGIN(LexicalStates.ST_ONE_LINE_COMMENT); return Tokens.T_PRAGMA_FILE; }
<ST_IN_SCRIPTING>"#pragma"[ \t]+"default"[ \t]+"line"[ \t]*  { BEGIN(LexicalStates.ST_ONE_LINE_COMMENT); return Tokens.T_PRAGMA_DEFAULT_LINE; }
<ST_IN_SCRIPTING>"#pragma"[ \t]+"default"[ \t]+"file"[ \t]*  { BEGIN(LexicalStates.ST_ONE_LINE_COMMENT); return Tokens.T_PRAGMA_DEFAULT_FILE; }

<ST_IN_SCRIPTING>"#"               	{ BEGIN(LexicalStates.ST_ONE_LINE_COMMENT); yymore(); break; }
<ST_IN_SCRIPTING>"//"              	{ BEGIN(LexicalStates.ST_ONE_LINE_COMMENT); yymore(); break; }
<ST_IN_SCRIPTING>"/**"{WHITESPACE} 	{ BEGIN(LexicalStates.ST_DOC_COMMENT); yymore(); break; }
<ST_IN_SCRIPTING>"/*"              	{ BEGIN(LexicalStates.ST_COMMENT); yymore(); break; }

<ST_IN_SCRIPTING>"("{TABS_AND_SPACES}"int8"{TABS_AND_SPACES}")"                     { return Tokens.T_INT8_CAST; }
<ST_IN_SCRIPTING>"("{TABS_AND_SPACES}"int16"{TABS_AND_SPACES}")"                    { return Tokens.T_INT16_CAST; }
<ST_IN_SCRIPTING>"("{TABS_AND_SPACES}("int"|"int32"|"integer"){TABS_AND_SPACES}")"  { return Tokens.T_INT32_CAST; }
<ST_IN_SCRIPTING>"("{TABS_AND_SPACES}"int64"{TABS_AND_SPACES}")"                    { return Tokens.T_INT64_CAST; }
<ST_IN_SCRIPTING>"("{TABS_AND_SPACES}"uint8"{TABS_AND_SPACES}")"                    { return Tokens.T_UINT8_CAST; }
<ST_IN_SCRIPTING>"("{TABS_AND_SPACES}"uint16"{TABS_AND_SPACES}")"                   { return Tokens.T_UINT16_CAST; }
<ST_IN_SCRIPTING>"("{TABS_AND_SPACES}("uint"|"uint32"){TABS_AND_SPACES}")"          { return Tokens.T_UINT32_CAST; }
<ST_IN_SCRIPTING>"("{TABS_AND_SPACES}"uint64"{TABS_AND_SPACES}")"                   { return Tokens.T_UINT64_CAST; }
<ST_IN_SCRIPTING>"("{TABS_AND_SPACES}("real"|"double"){TABS_AND_SPACES}")"          { return Tokens.T_DOUBLE_CAST; }
<ST_IN_SCRIPTING>"("{TABS_AND_SPACES}"float"{TABS_AND_SPACES}")"                    { return Tokens.T_FLOAT_CAST; }
<ST_IN_SCRIPTING>"("{TABS_AND_SPACES}"string"{TABS_AND_SPACES}")"                   { return Tokens.T_STRING_CAST; }
<ST_IN_SCRIPTING>"("{TABS_AND_SPACES}"binary"{TABS_AND_SPACES}")"                   { return Tokens.T_BINARY_CAST; }
<ST_IN_SCRIPTING>"("{TABS_AND_SPACES}"unicode"{TABS_AND_SPACES}")"                  { return Tokens.T_UNICODE_CAST; }
<ST_IN_SCRIPTING>"("{TABS_AND_SPACES}"array"{TABS_AND_SPACES}")"                    { return Tokens.T_ARRAY_CAST; }
<ST_IN_SCRIPTING>"("{TABS_AND_SPACES}"object"{TABS_AND_SPACES}")"                   { return Tokens.T_OBJECT_CAST; }
<ST_IN_SCRIPTING>"("{TABS_AND_SPACES}("bool"|"boolean"){TABS_AND_SPACES}")"         { return Tokens.T_BOOL_CAST; }
<ST_IN_SCRIPTING>"("{TABS_AND_SPACES}"unset"{TABS_AND_SPACES}")"                    { return Tokens.T_UNSET_CAST; }

<ST_IN_SCRIPTING>"{"   { yy_push_state(LexicalStates.ST_IN_SCRIPTING); return Tokens.T_LBRACE; }
<ST_IN_SCRIPTING>"}"   { if (!yy_pop_state()) return Tokens.ERROR; return Tokens.T_RBRACE; }

<ST_IN_SCRIPTING>"%>"{NEWLINE}? {
	if (AllowAspTags) 
	{
		BEGIN(LexicalStates.INITIAL);
		return Tokens.T_CLOSE_TAG;
	} 
	else
	{
		yyless(1);
		return Tokens.T_PERCENT;
	}
}

<ST_IN_SCRIPTING>(b?["]([^$"\\]|("\\".)|("\\"{NEWLINE}))*["]) { return Tokens.DoubleQuotedString; }
<ST_IN_SCRIPTING>(b?[']([^'\\]|("\\".)|("\\"{NEWLINE}))*['])  { return Tokens.SingleQuotedString; }
<ST_IN_SCRIPTING>(i[']([^'\\<>`#\r\n]|("\\"[^<>`#\r\n]))+[']) { return Tokens.SingleQuotedIdentifier; }
<ST_IN_SCRIPTING>(i[']([^'\\]|("\\".)|("\\"{NEWLINE}))*['])   { return Tokens.ErrorInvalidIdentifier; }

<ST_IN_SCRIPTING>b?["]  
{ 
	BEGIN(LexicalStates.ST_DOUBLE_QUOTES); 
	return (GetTokenChar(0) != '"') ? Tokens.T_BINARY_DOUBLE : Tokens.T_DOUBLE_QUOTES; 
}

<ST_IN_SCRIPTING>b?['] 
{ 
	// Gets here only in the case of unterminated singly-quoted string. That leads usually to an error token,
	// however when the source code is parsed per-line (as in Visual Studio colorizer) it is important to remember
	// that we are in the singly-quoted string at the end of the line.
	BEGIN(LexicalStates.ST_SINGLE_QUOTES); 
	yymore(); 
	break; 
}

<ST_IN_SCRIPTING>[`]
{ 
	BEGIN(LexicalStates.ST_BACKQUOTE); 
	return Tokens.T_BACKQUOTE; 
}

<ST_IN_SCRIPTING>b?"<<<"{TABS_AND_SPACES}({LABEL}|([']{LABEL}['])|(["]{LABEL}["])){NEWLINE} {
	bool is_binary = GetTokenChar(0) != '<';
	hereDocLabel = GetTokenSubstring(is_binary ? 4 : 3).Trim();
	var newstate = LexicalStates.ST_HEREDOC;
	if (hereDocLabel[0] == '"' || hereDocLabel[0] == '\'')
	{
		if (hereDocLabel[0] == '\'') newstate = LexicalStates.ST_NEWDOC;	// newdoc syntax, continue in ST_NEWDOC lexical state
		hereDocLabel = hereDocLabel.Substring(1, hereDocLabel.Length - 2);	// trim quote characters around
	}
	BEGIN(newstate);
	return is_binary ? Tokens.T_BINARY_HEREDOC : Tokens.T_START_HEREDOC;
}

<ST_IN_SCRIPTING>{ANY_CHAR} { return Tokens.ERROR; }





<ST_LOOKING_FOR_PROPERTY>{LABEL} {
	if (!yy_pop_state()) return Tokens.ERROR;
	inString = (CurrentLexicalState != LexicalStates.ST_IN_SCRIPTING); 
	isCode = true;
	return Tokens.T_STRING;
}

<ST_LOOKING_FOR_PROPERTY>{ANY_CHAR} {
	yyless(0);
	if (!yy_pop_state()) return Tokens.ERROR;
	break;
}



<ST_LOOKING_FOR_VARNAME>{LABEL} {
	if (!yy_pop_state()) return Tokens.ERROR;
	yy_push_state(LexicalStates.ST_IN_SCRIPTING);
	return Tokens.T_STRING_VARNAME;
}

<ST_LOOKING_FOR_VARNAME>{ANY_CHAR} {
	yyless(0);
	if (!yy_pop_state()) return Tokens.ERROR;
	yy_push_state(LexicalStates.ST_IN_SCRIPTING);
	break;
}



<ST_ONE_LINE_COMMENT>"?"|"%"|">" { yymore(); break; }
<ST_ONE_LINE_COMMENT>[^\n\r?%>]+ { yymore(); break; }
<ST_ONE_LINE_COMMENT>{NEWLINE}   { BEGIN(LexicalStates.ST_IN_SCRIPTING); return Tokens.T_LINE_COMMENT; }

<ST_ONE_LINE_COMMENT>"?>"|"%>"   { 
  if (AllowAspTags || GetTokenChar(TokenLength - 2) != '%') 
  { 
		yyless(0);
		BEGIN(LexicalStates.ST_IN_SCRIPTING);
		return Tokens.T_LINE_COMMENT;
	} 
	else 
	{
		yymore();
		break;
	}
}



<ST_COMMENT>[^*]+       { yymore(); break; }
<ST_COMMENT>"*/"        { BEGIN(LexicalStates.ST_IN_SCRIPTING); return Tokens.T_COMMENT; }
<ST_COMMENT>"*"         { yymore(); break; }



<ST_DOC_COMMENT>[^*]+   { yymore(); break; }
<ST_DOC_COMMENT>"*/"    { BEGIN(LexicalStates.ST_IN_SCRIPTING); return Tokens.T_DOC_COMMENT; }
<ST_DOC_COMMENT>"*"     { yymore(); break; }



<ST_SINGLE_QUOTES>([^'\\]|("\\".)|("\\"{NEWLINE}))+ { yymore(); break; }
<ST_SINGLE_QUOTES>"'"                               { BEGIN(LexicalStates.ST_IN_SCRIPTING); return Tokens.SingleQuotedString; }



<ST_HEREDOC,ST_NEWDOC>^{LABEL}(";")?{NEWLINE} {
	if (IsCurrentHeredocEnd(0))
	{
	  yyless(hereDocLabel.Length);
	  hereDocLabel = null;
	  BEGIN(LexicalStates.ST_IN_SCRIPTING);
		return Tokens.T_END_HEREDOC;
	}
	else 
	{
		inString = true;
		return Tokens.T_STRING;
	}
}

<ST_HEREDOC>{LNUM}|{HNUM}                  { return Tokens.T_NUM_STRING; }
<ST_HEREDOC>{LABEL}                        { inString = true; return Tokens.T_STRING; }
<ST_HEREDOC>{SlashedOctalNumber}           { return Tokens.OctalCharCode; }
<ST_HEREDOC>{SlashedHexNumber}             { return Tokens.HexCharCode; }
<ST_HEREDOC>{SlashedUnicodeCharCode}       { inString = true; return (inUnicodeString) ? Tokens.UnicodeCharCode : Tokens.T_STRING; }
<ST_HEREDOC>{SlashedUnicodeCharName}       { inString = true; return (inUnicodeString) ? Tokens.UnicodeCharName : Tokens.T_STRING; }
<ST_HEREDOC>{SlashedSpecialChar}           { return Tokens.EscapedCharacter; }
<ST_HEREDOC>{SlashedOpenBrace}             { inString = true; return Tokens.T_STRING; }
<ST_HEREDOC>{SlashedAnyChar}               { return Tokens.T_BAD_CHARACTER; }
<ST_HEREDOC>["'`]+                         { return Tokens.T_ENCAPSED_AND_WHITESPACE; }
<ST_HEREDOC>"$"{LABEL}                     { inString = true; return Tokens.T_VARIABLE; }
<ST_HEREDOC>"${"                           { yy_push_state(LexicalStates.ST_LOOKING_FOR_VARNAME); return Tokens.T_DOLLAR_OPEN_CURLY_BRACES; }
<ST_HEREDOC>"$"{NonVariableStart}          { yyless(1); return Tokens.T_CHARACTER; }
<ST_HEREDOC>"->"                           { yy_push_state(LexicalStates.ST_LOOKING_FOR_PROPERTY); inString = true; return Tokens.T_OBJECT_OPERATOR; }
<ST_HEREDOC>("["|"]"|"{"|"}"|"$")          { inString = true; return (Tokens)GetTokenChar(0); }
<ST_HEREDOC>"{$"                           { yy_push_state(LexicalStates.ST_IN_SCRIPTING); yyless(1); return Tokens.T_CURLY_OPEN; }
<ST_HEREDOC>{ESCAPED_AND_WHITESPACE}       { return Tokens.T_ENCAPSED_AND_WHITESPACE; }
<ST_HEREDOC,ST_NEWDOC>{ANY_CHAR}                     { return Tokens.T_CHARACTER; }


<ST_DOUBLE_QUOTES>{LNUM}|{HNUM}            { return Tokens.T_NUM_STRING; }
<ST_DOUBLE_QUOTES>{LABEL}                  { inString = true; return Tokens.T_STRING; }
<ST_DOUBLE_QUOTES>{SlashedOctalNumber}     { return Tokens.OctalCharCode; }
<ST_DOUBLE_QUOTES>{SlashedHexNumber}       { return Tokens.HexCharCode; }
<ST_DOUBLE_QUOTES>{SlashedUnicodeCharCode} { inString = true; return (inUnicodeString) ? Tokens.UnicodeCharCode : Tokens.T_STRING; }
<ST_DOUBLE_QUOTES>{SlashedUnicodeCharName} { inString = true; return (inUnicodeString) ? Tokens.UnicodeCharName : Tokens.T_STRING; }
<ST_DOUBLE_QUOTES>{SlashedDoubleQuotes}    { return Tokens.EscapedCharacter; }
<ST_DOUBLE_QUOTES>{SlashedSpecialChar}     { return Tokens.EscapedCharacter; }
<ST_DOUBLE_QUOTES>{SlashedOpenBrace}       { inString = true; return Tokens.T_STRING; }
<ST_DOUBLE_QUOTES>{SlashedAnyChar}         { return Tokens.T_BAD_CHARACTER; }
<ST_DOUBLE_QUOTES>[`]+                     { return Tokens.T_ENCAPSED_AND_WHITESPACE; }
<ST_DOUBLE_QUOTES>["]                      { BEGIN(LexicalStates.ST_IN_SCRIPTING); return Tokens.T_DOUBLE_QUOTES; }
<ST_DOUBLE_QUOTES>"$"{LABEL}               { inString = true; return Tokens.T_VARIABLE; }
<ST_DOUBLE_QUOTES>"${"                     { yy_push_state(LexicalStates.ST_LOOKING_FOR_VARNAME); return Tokens.T_DOLLAR_OPEN_CURLY_BRACES; }
<ST_DOUBLE_QUOTES>"$"{NonVariableStart}    { yyless(1); return Tokens.T_CHARACTER; }
<ST_DOUBLE_QUOTES>"->"                     { yy_push_state(LexicalStates.ST_LOOKING_FOR_PROPERTY); inString = true; return Tokens.T_OBJECT_OPERATOR; }
<ST_DOUBLE_QUOTES>"{$"                     { yy_push_state(LexicalStates.ST_IN_SCRIPTING); yyless(1); return Tokens.T_CURLY_OPEN; }
<ST_DOUBLE_QUOTES>("["|"]"|"{"|"}"|"$")    { inString = true; return (Tokens)GetTokenChar(0); }
<ST_DOUBLE_QUOTES>{ESCAPED_AND_WHITESPACE} { return Tokens.T_ENCAPSED_AND_WHITESPACE; }
<ST_DOUBLE_QUOTES>{ANY_CHAR}               { return Tokens.T_CHARACTER; }



<ST_BACKQUOTE>{LNUM}|{HNUM}                { return Tokens.T_NUM_STRING; }
<ST_BACKQUOTE>{LABEL}                      { inString = true; return Tokens.T_STRING; }
<ST_BACKQUOTE>{SlashedOctalNumber}         { return Tokens.OctalCharCode; }
<ST_BACKQUOTE>{SlashedHexNumber}           { return Tokens.HexCharCode; }
<ST_BACKQUOTE>{SlashedUnicodeCharCode}     { inString = true; return (inUnicodeString) ? Tokens.UnicodeCharCode : Tokens.T_STRING; }
<ST_BACKQUOTE>{SlashedUnicodeCharName}     { inString = true; return (inUnicodeString) ? Tokens.UnicodeCharName : Tokens.T_STRING; }
<ST_BACKQUOTE>{SlashedBackQuote}           { return Tokens.EscapedCharacter; }
<ST_BACKQUOTE>{SlashedSpecialChar}         { return Tokens.EscapedCharacter; }
<ST_BACKQUOTE>{SlashedOpenBrace}           { inString = true; return Tokens.T_STRING; }
<ST_BACKQUOTE>{SlashedAnyChar}             { return Tokens.T_BAD_CHARACTER; }
<ST_BACKQUOTE>["]+                         { return Tokens.T_ENCAPSED_AND_WHITESPACE; }
<ST_BACKQUOTE>[`]                          { BEGIN(LexicalStates.ST_IN_SCRIPTING); return Tokens.T_BACKQUOTE; }
<ST_BACKQUOTE>"$"{LABEL}                   { inString = true; return Tokens.T_VARIABLE; }
<ST_BACKQUOTE>"${"                         { yy_push_state(LexicalStates.ST_LOOKING_FOR_VARNAME); return Tokens.T_DOLLAR_OPEN_CURLY_BRACES; }
<ST_BACKQUOTE>"$"{NonVariableStart}        { yyless(1); return Tokens.T_CHARACTER; }
<ST_BACKQUOTE>"->"                         { yy_push_state(LexicalStates.ST_LOOKING_FOR_PROPERTY); inString = true; return Tokens.T_OBJECT_OPERATOR; }
<ST_BACKQUOTE>("["|"]"|"{"|"}"|"$")        { inString = true; return (Tokens)GetTokenChar(0); }
<ST_BACKQUOTE>"{$"                         { yy_push_state(LexicalStates.ST_IN_SCRIPTING); yyless(1); return Tokens.T_CURLY_OPEN; }
<ST_BACKQUOTE>{ESCAPED_AND_WHITESPACE}     { return Tokens.T_ENCAPSED_AND_WHITESPACE; }
<ST_BACKQUOTE>{ANY_CHAR}                   { return Tokens.T_CHARACTER; }


