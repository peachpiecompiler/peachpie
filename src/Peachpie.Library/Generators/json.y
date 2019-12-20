using System.Diagnostics;
using Pchp.Core;
using Pchp.Library.Parsers;

using Pair = System.Collections.Generic.KeyValuePair<string, Pchp.Core.PhpValue>;

%%

%namespace Pchp.Library.Json
%valuetype SemanticValueType
%positiontype Position
%tokentype Tokens
%visibility internal

%union
{
    public object obj;
    public PhpValue value;

    public Parser.Node<Pair> members { get => (Parser.Node<Pair>)obj; set => obj = value; }
    public Parser.Node<PhpValue> elements { get => (Parser.Node<PhpValue>)obj; set => obj = value; }
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
	  value	{ Result = $1.value; }
;

object:
		OBJECT_OPEN members OBJECT_CLOSE
		{
			var arr = new PhpArray(16);
				
			for (var n = $2.members; n != null; n = n.Next)
			{
				arr.Add( Core.Convert.StringToArrayKey(n.Value.Key), n.Value.Value );
			}
					
			$$.value = decodeOptions.Assoc ? PhpValue.Create(arr) : PhpValue.FromClass(arr.ToObject());
		}
	|	OBJECT_OPEN OBJECT_CLOSE
        {
            $$.value = decodeOptions.Assoc ? PhpValue.Create(PhpArray.NewEmpty()) : PhpValue.FromClass(new stdClass());
        }
	;
	
members:
		pair ITEMS_SEPARATOR members
		{
			var node = $1.members;
            node.Next = $3.members;
            $$.members = node;
		}
	|	pair	{ $$.members = $1.members; }
	;
	
pair:
		STRING NAMEVALUE_SEPARATOR value	{ $$.members = new Node<Pair>(new Pair((string)$1.obj, $3.value)); }
	;
	
array:
		ARRAY_OPEN elements ARRAY_CLOSE
		{
			var arr = new PhpArray(16);
			
            for (var n = $2.elements; n != null; n = n.Next)
            {
                arr.Add( n.Value );
            }
				
			$$.value = PhpValue.Create(arr);
		}
	|	ARRAY_OPEN ARRAY_CLOSE	{ $$.value = PhpValue.Create(PhpArray.NewEmpty()); }
	;
	
elements:
		value ITEMS_SEPARATOR elements
		{
			$$.elements = new Node<PhpValue>($1.value, $3.elements);
		}
	|	value { $$.elements = new Node<PhpValue>($1.value); }
	;
	
value:
		STRING	{$$.value = PhpValue.Create((string)$1.obj);}
	|	INTEGER	{$$.value = $1.value;}
	|	DOUBLE	{$$.value = $1.value;}
	|	object	{$$.value = $1.value;}
	|	array	{$$.value = $1.value;}
	|	TRUE	{$$.value = PhpValue.True;}
	|	FALSE	{$$.value = PhpValue.False;}
	|	NULL	{$$.value = PhpValue.Null;}
	;

%%

protected override int EofToken { get { return (int)Tokens.EOF; } }
protected override int ErrorToken { get { return (int)Tokens.ERROR; } }

