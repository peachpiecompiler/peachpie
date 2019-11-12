<?php
namespace variables\valueref_003;

$a = [0,0,0];

foreach($a as &$value) { }    // upgrades values to PhpAlias

$b = $a;	// pass array by value, lazy copy
$b[0] = 1; 	// values must not be references

print_r( $a ); // expected: [0,0,0]