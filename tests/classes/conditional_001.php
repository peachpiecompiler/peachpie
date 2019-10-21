<?php
namespace classes\conditional_001;

if (true) {
	class X {
		function foo(){ echo "one"; }
	}
}
else {
	class X {
		function foo(){ echo "two"; }
	}
}

class Y extends X {
	function bar() { parent::foo(); }
}

(new X)->foo();
(new Y)->bar();

echo "Done.";
