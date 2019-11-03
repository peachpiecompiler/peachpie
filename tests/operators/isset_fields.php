<?php
namespace operators\isset_fields;

class X {
    function __get($key) { return ''; }
    var $defined = '';
    var $d2 = '';
	var $dnull = '';
}

function o( $b )
{
	echo $b ? "true" : "false";
	echo "\n";
}

function test($x) {
    $x->dynamic = '';

    $a = $x->something;

    o( isset($a) );
    o( isset($x->something) ); // in case of __get, the value is converted to boolean instead
    o( isset($x->defined) );
    o( isset($x->dynamic) );
    o( isset($x->d2) );
	o( isset($x->dnull) );
}

test(new X);
