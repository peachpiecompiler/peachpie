<?php
namespace classes\properties_001;

class Y {
	function foo() {
	}
}

class X {

	static $id = 0;

	function __construct () {
		@$this->id = 10;	// Notice: Accessing static property X::$id as non static // PHP creates runtime field "id" different to self::$id
		self::$id = 11;
		
		echo self::$id, ' ', @$this->id, ' ';

		$a = new Y();
		$a->fld = $this;
		@$a->fld->id = 4;

		$v = "fld";
		$this->$v = 5;
	}
}

(new X);

echo "Done.";
