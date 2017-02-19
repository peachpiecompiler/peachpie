using System.Diagnostics;
using Pchp.Core;
using Pchp.Library.Parsers;
%%

%namespace Pchp.Library.Json
%valuetype SemanticValueType
%positiontype Position
%tokentype Tokens
%visibility internal

%union
{
	public object obj; 
}

%token ARRAY_OPEN
%token ARRAY_CLOSE
%token ITEMS_SEPARATOR
%token NAMEVALUE_SEPARATOR
%token OBJECT_OPEN
%token OBJECT_CLOSE
%token TRUE
%token FALSE
%token NULL
%token INTEGER
%token DOUBLE
%token STRING

%token STRING_BEGIN
%token CHARS
%token UNICODECHAR
%token ESCAPEDCHAR
%token STRING_END
	   
%% /* Productions */

start:
	  value	{ Result = (PhpValue)$1.obj; }
;

object:
		OBJECT_OPEN members OBJECT_CLOSE
		{
			var elements = (List<KeyValuePair<string, PhpValue>>)$2.obj;				
			var arr = new PhpArray(elements.Count);
				
			foreach (var item in elements)
			{
				arr.Add( Core.Convert.StringToArrayKey(item.Key), item.Value );
			}
					
			if (decodeOptions.Assoc)
			{
				$$.obj = PhpValue.Create(arr);
			}
			else
			{
				$$.obj = PhpValue.FromClass(arr.ToClass());
			}
		}
	|	OBJECT_OPEN OBJECT_CLOSE	{ $$.obj = PhpValue.FromClass(new stdClass()); }
	;
	
members:
		pair ITEMS_SEPARATOR members
		{
			var elements = (List<KeyValuePair<string, PhpValue>>)$3.obj;
			var result = new List<KeyValuePair<string, PhpValue>>( elements.Count + 1 ){ (KeyValuePair<string, PhpValue>)$1.obj };
			result.AddRange(elements);			
			$$.obj = result;
		}
	|	pair	{ $$.obj = new List<KeyValuePair<string, PhpValue>>(){ (KeyValuePair<string,PhpValue>)$1.obj }; }
	;
	
pair:
		STRING NAMEVALUE_SEPARATOR value	{ $$.obj = new KeyValuePair<string,PhpValue>((string)$1.obj, (PhpValue)$3.obj); }
	;
	
array:
		ARRAY_OPEN elements ARRAY_CLOSE
		{
			var elements = (List<PhpValue>)$2.obj;
			var arr = new PhpArray( elements.Count );
			
			foreach (var item in elements)
				arr.Add( item );
				
			$$.obj = arr;
		}
	|	ARRAY_OPEN ARRAY_CLOSE	{ $$.obj = PhpArray.NewEmpty(); }
	;
	
elements:
		value ITEMS_SEPARATOR elements
		{
			var elements = (List<PhpValue>)$3.obj;
			var result = new List<PhpValue>( elements.Count + 1 ){ (PhpValue)$1.obj };
			result.AddRange(elements);
			$$.obj = result;
		}
	|	value { $$.obj = new List<PhpValue>(){ (PhpValue)$1.obj }; }
	;
	
value:
		STRING	{$$.obj = PhpValue.Create((string)$1.obj);}
	|	INTEGER	{$$.obj = PhpValue.FromClr($1.obj);}
	|	DOUBLE	{$$.obj = PhpValue.FromClr($1.obj);}
	|	object	{$$.obj = (PhpValue)$1.obj;}
	|	array	{$$.obj = PhpValue.Create((PhpArray)$1.obj);}
	|	TRUE	{$$.obj = PhpValue.True;}
	|	FALSE	{$$.obj = PhpValue.False;}
	|	NULL	{$$.obj = PhpValue.Null;}
	;

%%

protected override int EofToken { get { return (int)Tokens.EOF; } }
protected override int ErrorToken { get { return (int)Tokens.ERROR; } }

readonly PhpSerialization.JsonSerializer.DecodeOptions/*!*/decodeOptions;

internal Parser(PhpSerialization.JsonSerializer.DecodeOptions/*!*/decodeOptions)
{
	Debug.Assert(decodeOptions != null);
	
	this.decodeOptions = decodeOptions;
}

public PhpValue Result { get; private set; }