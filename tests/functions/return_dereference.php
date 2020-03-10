<?php
namespace functions\return_dereference;

function wp_parse_args( $args ) {
	if ( is_array( $args ) ) {
		$parsed_args =& $args;
	}

	return $parsed_args; // value MUST be dereferenced
}

function test()
{
    global $a; // $a can be aliased
    $a = wp_parse_args([]); // Debug.Assert // https://github.com/iolevel/wpdotnet-sdk/issues/65 - only when compiling in Release
    print_r($a);
}

test();
