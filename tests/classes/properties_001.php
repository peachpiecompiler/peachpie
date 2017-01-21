<?php

class Y {
	function foo() {
	}
}

class X {

	static $id = 0;

	function X () {
		$this->id = 10;	// accessing static field with instance

		$a = new Y();
		$a->fld = $this;
		$a->fld->id = 4;


		$v = "fld";
		$this->$v = 5;
	}
}

(new X);
