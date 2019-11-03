<?php
namespace classes\__get_001;

class X {
	protected $p = '$p';
	function __get($name) {
		switch ($name) {
			case 'p': return "[$this->p]";
			default: return "(default)";
		}
	}
}

function f() {
	echo (new X)->p;
	echo (new X)->nonexisting;
}

f();
