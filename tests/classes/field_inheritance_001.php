<?php
namespace classes\field_inheritance_001;

class A {
	var $fld = 1;
	
	function f() {
		echo $this->fld;
	}
}

class B extends A {
	var $fld;
}

class C extends B {
	var $FLD = "FLD";
}

class D extends B {
	var $fld = 4;
}

(new A)->f();
(new B)->f();
(new C)->f();
(new D)->f();
