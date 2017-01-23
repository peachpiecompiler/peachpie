<?php

function foo($x)
{
	echo "'$x' ";
	return $x;
}

var_dump( foo(0) ?: foo(2) );
var_dump( foo(1) ?: foo(2) );
var_dump( true ?: false );
var_dump( false ?: true );
var_dump( false ?: false );
var_dump( true ?: true );
var_dump( 1 ?: 2 );
var_dump( 0 ?: 1 );
var_dump( foo(0) ?: foo(0) ?: foo(0) ?: foo(3) );
var_dump( foo(0) ?: 0 ?: foo(0) ?: foo(3) );
var_dump( foo(0) ?: foo(0) ?: foo(0) ?: 3 );
var_dump( 0 ?: 0 ?: 0 ?: foo(3) );
var_dump( 0 ?: 1 ?: foo(0) ?: foo(3) );

echo "\nDONE";
