<?php
namespace variables\chains_001;

class X {
	var $data;

	/** @var string */
	var $name;
	
	function bar($a, $b){
		echo "$a$b;";
	}
}

function newX() {
	return true ? new X : null;	// just to confuse type analysis
}

function test() {
	$x = newX();
	$y = $x;
	if ($y instanceof X) {
		echo $y->bar(0,1);
	}

	$x->runtime_field = "Hello";
	echo $x->runtime_field;
	
	$y->name = "Whof";
	echo $y->name;

	$x->data = 123;
	
	$s = new \stdClass();
	$s->rrr = null;
	@$s->rrr->qqq = "woaaaa";
	echo $s->rrr->qqq;

	$x->data = new X;
	$x->data->name = "Hello";
	$x->data = null;
	@$x->data->name = "Hola";

	echo $x->data->name;
}

test();