<?php
namespace operators\instanceof_001;

class Dog {
	
}

class BigDog extends Dog {
	
}

class Cat {
	
}

function test($x) {
	echo $x instanceof Dog ? "1" : "0";
}

/** @param object $x */
function test2($x) {
	echo $x instanceof Dog ? "1" : "0";
}

test(new Dog);
test(new BigDog);
test(new Cat);
test2(new Dog);
test2(new Cat);
test(null);
test(123);
test(123.456);
test("Dog");
test([1,2,3]);
test(true);
