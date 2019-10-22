<?php
namespace operators\ternary_short;

function foo($x)
{
	echo "'$x' ";
	return $x;
}

print_r( foo(0) ?: foo(2) );
print_r( foo(1) ?: foo(2) );
print_r( true ?: false );
print_r( false ?: true );
print_r( false ?: false );
print_r( true ?: true );
print_r( 1 ?: 2 );
print_r( 0 ?: 1 );
print_r( foo(0) ?: foo(0) ?: foo(0) ?: foo(3) );
print_r( foo(0) ?: 0 ?: foo(0) ?: foo(3) );
print_r( foo(0) ?: foo(0) ?: foo(0) ?: 3 );
print_r( 0 ?: 0 ?: 0 ?: foo(3) );
print_r( 0 ?: 1 ?: foo(0) ?: foo(3) );

echo "\nDONE";
