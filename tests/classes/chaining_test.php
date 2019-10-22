<?php
namespace classes\chaining_test;

class Class1
{
	public static $x;
	public static $y;
};

Class1::$x->a->b->c[0][1][2]->x["A"]->y["B"]->z["C"] = "Hujer";
Class1::$y->a->b->c[0][1][2]->x["A"]->y["B"]->z["C"] =& Class1::$x->a->b->c;

echo Class1::$y->a->b->c[0][1][2]->x["A"]->y["B"]->z["C"][0][1][2]->x["A"]->y["B"]->z["C"];
