<?php

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

(new X)->foo();

echo "Done.";
