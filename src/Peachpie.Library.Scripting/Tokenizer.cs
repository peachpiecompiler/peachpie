using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Devsense.PHP.Syntax;
using Devsense.PHP.Text;
using Pchp.Core;

namespace Peachpie.Library.Scripting
{
    [PhpExtension("tokenizer")]
    public static class Tokenizer
    {
        #region T_* constants

        // Tokens available as  PHP constants
        // see https://www.php.net/manual/en/tokens.php

        ///<summary>abstract Class Abstraction</summary>
        public const int T_ABSTRACT = (int)Tokens.T_ABSTRACT;

        ///<summary>&amp;=	assignment operators</summary>
        public const int T_AND_EQUAL = (int)Tokens.T_AND_EQUAL;

        ///<summary>array() array(), array syntax</summary>
        public const int T_ARRAY = (int)Tokens.T_ARRAY;

        ///<summary>array) type-casting</summary>
        public const int T_ARRAY_CAST = (int)Tokens.T_ARRAY_CAST;

        ///<summary>as	foreach</summary>
        public const int T_AS = (int)Tokens.T_AS;

        // ///<summary>anything below ASCII 32 except \t(0x09), \n(0x0a) and \r(0x0d)</summary>
        // public const int T_BAD_CHARACTER = (int)Tokens.T_BAD_CHARACTER;

        ///<summary>&amp;&amp;	logical operators</summary>
        public const int T_BOOLEAN_AND = (int)Tokens.T_BOOLEAN_AND;

        ///<summary>||	logical operators</summary>
        public const int T_BOOLEAN_OR = (int)Tokens.T_BOOLEAN_OR;

        ///<summary>bool) or(boolean) type-casting</summary>
        public const int T_BOOL_CAST = (int)Tokens.T_BOOL_CAST;

        ///<summary>break	break</summary>
        public const int T_BREAK = (int)Tokens.T_BREAK;

        ///<summary>callable    callable</summary>
        public const int T_CALLABLE = (int)Tokens.T_CALLABLE;

        ///<summary>case	switch</summary>
        public const int T_CASE = (int)Tokens.T_CASE;

        ///<summary>catch	Exceptions</summary>
        public const int T_CATCH = (int)Tokens.T_CATCH;

        // ///<summary>not used anymore</summary>
        // public const int T_CHARACTER = (int)Tokens.T_CHARACTER;

        ///<summary>class classes and objects</summary>
        public const int T_CLASS = (int)Tokens.T_CLASS;

        ///<summary>__CLASS__   magic constants</summary>
        public const int T_CLASS_C = (int)Tokens.T_CLASS_C;

        ///<summary>clone   classes and objects</summary>
        public const int T_CLONE = (int)Tokens.T_CLONE;

        ///<summary>?> or %>	escaping from HTML</summary>
        public const int T_CLOSE_TAG = (int)Tokens.T_CLOSE_TAG;

        ///<summary>??	comparison operators(available since PHP 7.0.0)</summary>
        public const int T_COALESCE = (int)Tokens.T_COALESCE;

        ///<summary>// or #, and /* */	comments</summary>
        public const int T_COMMENT = (int)Tokens.T_COMMENT;

        ///<summary>.=	assignment operators</summary>
        public const int T_CONCAT_EQUAL = (int)Tokens.T_CONCAT_EQUAL;

        ///<summary>const   class constants</summary>
        public const int T_CONST = (int)Tokens.T_CONST;

        ///<summary>"foo" or 'bar'	string syntax</summary>
        public const int T_CONSTANT_ENCAPSED_STRING = (int)Tokens.T_CONSTANT_ENCAPSED_STRING;

        ///<summary>continue	continue</summary>
        public const int T_CONTINUE = (int)Tokens.T_CONTINUE;

        ///<summary>{$	complex variable parsed syntax</summary>
        public const int T_CURLY_OPEN = (int)Tokens.T_CURLY_OPEN;

        ///<summary>--	incrementing/decrementing operators</summary>
        public const int T_DEC = (int)Tokens.T_DEC;

        ///<summary>declare declare</summary>
        public const int T_DECLARE = (int)Tokens.T_DECLARE;

        ///<summary>default	switch</summary>
        public const int T_DEFAULT = (int)Tokens.T_DEFAULT;

        ///<summary>__DIR__ magic constants(available since PHP 5.3.0)</summary>
        public const int T_DIR = (int)Tokens.T_DIR;

        ///<summary>/=	assignment operators</summary>
        public const int T_DIV_EQUAL = (int)Tokens.T_DIV_EQUAL;

        ///<summary>0.12, etc.floating point numbers</summary>
        public const int T_DNUMBER = (int)Tokens.T_DNUMBER;

        ///<summary>/** */	PHPDoc style comments</summary>
        public const int T_DOC_COMMENT = (int)Tokens.T_DOC_COMMENT;

        ///<summary>do	do..while</summary>
        public const int T_DO = (int)Tokens.T_DO;

        ///<summary>${	complex variable parsed syntax</summary>
        public const int T_DOLLAR_OPEN_CURLY_BRACES = (int)Tokens.T_DOLLAR_OPEN_CURLY_BRACES;

        ///<summary>=>	array syntax</summary>
        public const int T_DOUBLE_ARROW = (int)Tokens.T_DOUBLE_ARROW;

        ///<summary>real), (double) or(float) type-casting</summary>
        public const int T_DOUBLE_CAST = (int)Tokens.T_DOUBLE_CAST;

        ///<summary>::	see T_PAAMAYIM_NEKUDOTAYIM below</summary>
        public const int T_DOUBLE_COLON = (int)Tokens.T_DOUBLE_COLON;

        ///<summary>echo    echo</summary>
        public const int T_ECHO = (int)Tokens.T_ECHO;

        ///<summary>...	function arguments (available since PHP 5.6.0)</summary>
        public const int T_ELLIPSIS = (int)Tokens.T_ELLIPSIS;

        ///<summary>else	else</summary>
        public const int T_ELSE = (int)Tokens.T_ELSE;

        ///<summary>elseif  elseif</summary>
        public const int T_ELSEIF = (int)Tokens.T_ELSEIF;

        ///<summary>empty empty()</summary>
        public const int T_EMPTY = (int)Tokens.T_EMPTY;

        ///<summary>" $a"	constant part of string with variables</summary>
        public const int T_ENCAPSED_AND_WHITESPACE = (int)Tokens.T_ENCAPSED_AND_WHITESPACE;

        ///<summary>enddeclare declare, alternative syntax</summary>
        public const int T_ENDDECLARE = (int)Tokens.T_ENDDECLARE;

        ///<summary>endfor	for, alternative syntax</summary>
        public const int T_ENDFOR = (int)Tokens.T_ENDFOR;

        ///<summary>endforeach	foreach, alternative syntax</summary>
        public const int T_ENDFOREACH = (int)Tokens.T_ENDFOREACH;

        ///<summary>endif	if, alternative syntax</summary>
        public const int T_ENDIF = (int)Tokens.T_ENDIF;

        ///<summary>endswitch	switch, alternative syntax</summary>
        public const int T_ENDSWITCH = (int)Tokens.T_ENDSWITCH;

        ///<summary>endwhile	while, alternative syntax</summary>
        public const int T_ENDWHILE = (int)Tokens.T_ENDWHILE;

        ///<summary>heredoc syntax</summary>
        public const int T_END_HEREDOC = (int)Tokens.T_END_HEREDOC;

        ///<summary>eval()  eval()</summary>
        public const int T_EVAL = (int)Tokens.T_EVAL;

        ///<summary>exit or die exit(), die()</summary>
        public const int T_EXIT = (int)Tokens.T_EXIT;

        ///<summary>extends extends, classes and objects</summary>
        public const int T_EXTENDS = (int)Tokens.T_EXTENDS;

        ///<summary>__FILE__ magic constants</summary>
        public const int T_FILE = (int)Tokens.T_FILE;

        ///<summary>final Final Keyword</summary>
        public const int T_FINAL = (int)Tokens.T_FINAL;

        ///<summary>finally	Exceptions(available since PHP 5.5.0)</summary>
        public const int T_FINALLY = (int)Tokens.T_FINALLY;

        ///<summary>for	for</summary>
        public const int T_FOR = (int)Tokens.T_FOR;

        ///<summary>foreach	foreach</summary>
        public const int T_FOREACH = (int)Tokens.T_FOREACH;

        ///<summary>function</summary>
        public const int T_FUNCTION = (int)Tokens.T_FUNCTION;

        /// <summary>"fn" keyword</summary>
        public const int T_FN = (int)Tokens.T_FN;

        ///<summary>__FUNCTION__ magic constants</summary>
        public const int T_FUNC_C = (int)Tokens.T_FUNC_C;

        ///<summary>global variable scope</summary>
        public const int T_GLOBAL = (int)Tokens.T_GLOBAL;

        ///<summary>goto	goto (available since PHP 5.3.0)</summary>
        public const int T_GOTO = (int)Tokens.T_GOTO;

        ///<summary>__halt_compiler()   __halt_compiler(available since PHP 5.1.0)</summary>
        public const int T_HALT_COMPILER = (int)Tokens.T_HALT_COMPILER;

        ///<summary>if	if</summary>
        public const int T_IF = (int)Tokens.T_IF;

        ///<summary>implements  Object Interfaces</summary>
        public const int T_IMPLEMENTS = (int)Tokens.T_IMPLEMENTS;

        ///<summary>++	incrementing/decrementing operators</summary>
        public const int T_INC = (int)Tokens.T_INC;

        ///<summary>include()   include</summary>
        public const int T_INCLUDE = (int)Tokens.T_INCLUDE;

        ///<summary>include_once()  include_once</summary>
        public const int T_INCLUDE_ONCE = (int)Tokens.T_INCLUDE_ONCE;

        ///<summary>text outside PHP</summary>
        public const int T_INLINE_HTML = (int)Tokens.T_INLINE_HTML;

        ///<summary>instanceof type operators</summary>
        public const int T_INSTANCEOF = (int)Tokens.T_INSTANCEOF;

        ///<summary>insteadof Traits(available since PHP 5.4.0)</summary>
        public const int T_INSTEADOF = (int)Tokens.T_INSTEADOF;

        ///<summary>int) or(integer)  type-casting</summary>
        public const int T_INT_CAST = (int)Tokens.T_INT_CAST;

        ///<summary>interface Object Interfaces</summary>
        public const int T_INTERFACE = (int)Tokens.T_INTERFACE;

        ///<summary>isset() isset()</summary>
        public const int T_ISSET = (int)Tokens.T_ISSET;

        ///<summary>==	comparison operators</summary>
        public const int T_IS_EQUAL = (int)Tokens.T_IS_EQUAL;

        ///<summary>>=	comparison operators</summary>
        public const int T_IS_GREATER_OR_EQUAL = (int)Tokens.T_IS_GREATER_OR_EQUAL;

        ///<summary>===	comparison operators</summary>
        public const int T_IS_IDENTICAL = (int)Tokens.T_IS_IDENTICAL;

        ///<summary>!= or&lt;&gt; comparison operators</summary>
        public const int T_IS_NOT_EQUAL = (int)Tokens.T_IS_NOT_EQUAL;

        ///<summary>!==	comparison operators</summary>
        public const int T_IS_NOT_IDENTICAL = (int)Tokens.T_IS_NOT_IDENTICAL;

        ///<summary>&lt;=	comparison operators</summary>
        public const int T_IS_SMALLER_OR_EQUAL = (int)Tokens.T_IS_SMALLER_OR_EQUAL;

        ///<summary>&lt;=&gt;	comparison operators(available since PHP 7.0.0)</summary>
        public const int T_SPACESHIP = (int)Tokens.T_SPACESHIP;

        ///<summary>__LINE__    magic constants</summary>
        public const int T_LINE = (int)Tokens.T_LINE;

        ///<summary>list()  list()</summary>
        public const int T_LIST = (int)Tokens.T_LIST;

        ///<summary>123, 012, 0x1ac, etc.integers</summary>
        public const int T_LNUMBER = (int)Tokens.T_LNUMBER;

        ///<summary>and logical operators</summary>
        public const int T_LOGICAL_AND = (int)Tokens.T_LOGICAL_AND;

        ///<summary>or logical operators</summary>
        public const int T_LOGICAL_OR = (int)Tokens.T_LOGICAL_OR;

        ///<summary>xor logical operators</summary>
        public const int T_LOGICAL_XOR = (int)Tokens.T_LOGICAL_XOR;

        ///<summary>__METHOD__ magic constants</summary>
        public const int T_METHOD_C = (int)Tokens.T_METHOD_C;

        ///<summary>-=	assignment operators</summary>
        public const int T_MINUS_EQUAL = (int)Tokens.T_MINUS_EQUAL;

        ///<summary>%=	assignment operators</summary>
        public const int T_MOD_EQUAL = (int)Tokens.T_MOD_EQUAL;

        ///<summary>*=	assignment operators</summary>
        public const int T_MUL_EQUAL = (int)Tokens.T_MUL_EQUAL;

        ///<summary>namespace namespaces (available since PHP 5.3.0)</summary>
        public const int T_NAMESPACE = (int)Tokens.T_NAMESPACE;

        ///<summary>__NAMESPACE__   namespaces(available since PHP 5.3.0)</summary>
        public const int T_NS_C = (int)Tokens.T_NS_C;

        ///<summary>\	namespaces(available since PHP 5.3.0)</summary>
        public const int T_NS_SEPARATOR = (int)Tokens.T_NS_SEPARATOR;

        ///<summary>new classes and objects</summary>
        public const int T_NEW = (int)Tokens.T_NEW;

        ///<summary>"$a[0]"	numeric array index inside string</summary>
        public const int T_NUM_STRING = (int)Tokens.T_NUM_STRING;

        ///<summary>object)    type-casting</summary>
        public const int T_OBJECT_CAST = (int)Tokens.T_OBJECT_CAST;

        ///<summary>-&gt;	classes and objects</summary>
        public const int T_OBJECT_OPERATOR = (int)Tokens.T_OBJECT_OPERATOR;

        ///<summary>&lt;?php, &lt;? or &lt;%	escaping from HTML</summary>
        public const int T_OPEN_TAG = (int)Tokens.T_OPEN_TAG;

        ///<summary>&lt;?= or &lt;%=	escaping from HTML</summary>
        public const int T_OPEN_TAG_WITH_ECHO = (int)Tokens.T_OPEN_TAG_WITH_ECHO;

        ///<summary>|=	assignment operators</summary>
        public const int T_OR_EQUAL = (int)Tokens.T_OR_EQUAL;

        ///<summary>::	::. Also defined as T_DOUBLE_COLON.</summary>
        public const int T_PAAMAYIM_NEKUDOTAYIM = (int)Tokens.T_DOUBLE_COLON;

        ///<summary>+=	assignment operators</summary>
        public const int T_PLUS_EQUAL = (int)Tokens.T_PLUS_EQUAL;

        ///<summary>**	arithmetic operators (available since PHP 5.6.0)</summary>
        public const int T_POW = (int)Tokens.T_POW;

        ///<summary>**=	assignment operators(available since PHP 5.6.0)</summary>
        public const int T_POW_EQUAL = (int)Tokens.T_POW_EQUAL;

        /// <summary>'??=' operator (PHP 7.4)</summary>
        public const int T_COALESCE_EQUAL = (int)Tokens.T_COALESCE_EQUAL;

        ///<summary>print() print</summary>
        public const int T_PRINT = (int)Tokens.T_PRINT;

        ///<summary>private classes and objects</summary>
        public const int T_PRIVATE = (int)Tokens.T_PRIVATE;

        ///<summary>public classes and objects</summary>
        public const int T_PUBLIC = (int)Tokens.T_PUBLIC;

        ///<summary>protected classes and objects</summary>
        public const int T_PROTECTED = (int)Tokens.T_PROTECTED;

        ///<summary>require()   require</summary>
        public const int T_REQUIRE = (int)Tokens.T_REQUIRE;

        ///<summary>require_once()  require_once</summary>
        public const int T_REQUIRE_ONCE = (int)Tokens.T_REQUIRE_ONCE;

        ///<summary>return	returning values</summary>
        public const int T_RETURN = (int)Tokens.T_RETURN;

        ///<summary>&lt;&lt;	bitwise operators</summary>
        public const int T_SL = (int)Tokens.T_SL;

        ///<summary>&lt;&lt;=	assignment operators</summary>
        public const int T_SL_EQUAL = (int)Tokens.T_SL_EQUAL;

        ///<summary>>>	bitwise operators</summary>
        public const int T_SR = (int)Tokens.T_SR;

        ///<summary>>>=	assignment operators</summary>
        public const int T_SR_EQUAL = (int)Tokens.T_SR_EQUAL;

        ///<summary>&lt;&lt;&lt;	heredoc syntax</summary>
        public const int T_START_HEREDOC = (int)Tokens.T_START_HEREDOC;

        ///<summary>static variable scope</summary>
        public const int T_STATIC = (int)Tokens.T_STATIC;

        ///<summary>parent, self, etc.	identifiers, e.g.keywords like parent and self, function names, class names and more are matched.See also T_CONSTANT_ENCAPSED_STRING.</summary>
        public const int T_STRING = (int)Tokens.T_STRING;

        ///<summary>(string) type-casting</summary>
        public const int T_STRING_CAST = (int)Tokens.T_STRING_CAST;

        ///<summary>"${a	complex variable parsed syntax</summary>
        public const int T_STRING_VARNAME = (int)Tokens.T_STRING_VARNAME;

        ///<summary>switch	switch</summary>
        public const int T_SWITCH = (int)Tokens.T_SWITCH;

        ///<summary>throw	Exceptions</summary>
        public const int T_THROW = (int)Tokens.T_THROW;

        ///<summary>trait Traits (available since PHP 5.4.0)</summary>
        public const int T_TRAIT = (int)Tokens.T_TRAIT;

        ///<summary>__TRAIT__   __TRAIT__(available since PHP 5.4.0)</summary>
        public const int T_TRAIT_C = (int)Tokens.T_TRAIT_C;

        ///<summary>try	Exceptions</summary>
        public const int T_TRY = (int)Tokens.T_TRY;

        ///<summary>unset() unset()</summary>
        public const int T_UNSET = (int)Tokens.T_UNSET;

        ///<summary>unset) type-casting</summary>
        public const int T_UNSET_CAST = (int)Tokens.T_UNSET_CAST;

        ///<summary>use namespaces(available since PHP 5.3.0)</summary>
        public const int T_USE = (int)Tokens.T_USE;

        ///<summary>var classes and objects</summary>
        public const int T_VAR = (int)Tokens.T_VAR;

        ///<summary>$foo variables</summary>
        public const int T_VARIABLE = (int)Tokens.T_VARIABLE;

        ///<summary>while	while, do..while</summary>
        public const int T_WHILE = (int)Tokens.T_WHILE;

        ///<summary>\t \r\n</summary>
        public const int T_WHITESPACE = (int)Tokens.T_WHITESPACE;

        ///<summary>^=	assignment operators</summary>
        public const int T_XOR_EQUAL = (int)Tokens.T_XOR_EQUAL;

        ///<summary>yield   generators(available since PHP 5.5.0)</summary>
        public const int T_YIELD = (int)Tokens.T_YIELD;

        ///<summary>yield from generators(available since PHP 7.0.0)</summary>
        public const int T_YIELD_FROM = (int)Tokens.T_YIELD_FROM;

        /// <summary>
        /// Not used.
        /// Our lexer does not report T_BAD_CHARACTER on long script (&gt; Int32.MaxValue characters)
        /// </summary>
        public const int T_BAD_CHARACTER = 1024;

        #endregion

        /// <summary>
        /// Recognises the ability to use reserved words in specific contexts.
        /// </summary>
        public const int TOKEN_PARSE = 1;

        /// <summary>
        /// Get the symbolic name of a given PHP token.
        /// </summary>
        [return: NotNull]
        public static string token_name(int token)
        {
            return ((Tokens)token).ToString();
        }

        /// <summary>
        /// Split given source into PHP tokens.
        /// </summary>
        /// <param name="source">The PHP source to parse.</param>
        /// <param name="flags"></param>
        /// <returns>
        /// An array of token identifiers.
        /// Each individual token identifier is either a single character (i.e.: ;, ., >, !, etc...),
        /// or a three element array containing the token index in element 0, the string content of the original token in element 1 and the line number in element 2.
        /// </returns>
        [return: NotNull]
        public static PhpArray/*!*/token_get_all(string source, int flags = 0)
        {
            var tokens = new PhpArray();

            if (string.IsNullOrEmpty(source))
            {
                return tokens;
            }

            Tokens t;
            var lines = LineBreaks.Create(source);
            using (var tokenizer = new Lexer(new StringReader(source), Encoding.UTF8))
            {
                while ((t = tokenizer.GetNextToken()) != Tokens.EOF)
                {
                    if (tokenizer.TokenSpan.Length == 1 && (int)t == tokenizer.TokenText[0])
                    {
                        // single char token
                        tokens.Add(tokenizer.TokenText);
                    }
                    else
                    {
                        // other
                        tokens.Add(new PhpArray(3)
                    {
                        (int)t,
                        tokenizer.TokenText,
                        lines.GetLineFromPosition(tokenizer.TokenSpan.Start) + 1,
                    });
                    }

                    //

                    if (t == Tokens.T_ERROR)
                    {
                        break;
                    }
                }
            }

            return tokens;
        }
    }
}
