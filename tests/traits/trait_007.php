<?php
namespace traits\trait_007;

// trait implementing an interface

interface IA {
	function foo( $x, $y );
}

trait TA {

	function foo( $x, $y ) {
		echo __TRAIT__, $x, $y;
	}
}

class A implements IA {
	use TA;
}

(new A)->foo( 1, 2 );
