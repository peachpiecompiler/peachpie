<?php
namespace operators\coalesce;

function test($x) {
	print_r( $x ?? "null" );
}

echo "Value:\n";

test(null);
test(0);
test(1);
test(false);
test("Hello");
test("0");
test(new \stdClass);
test([]);
test([1, 2, 3]);

echo "\nTyped:\n";

print_r( null ?? "null" );
print_r( 0 ?? "null" );
print_r( 1 ?? "null" );
print_r( false ?? "null" );
print_r( "Hello" ?? "null" );
print_r( "0" ?? "null" );
print_r( new \stdClass ?? "null" );
print_r( [] ?? "null" );

echo "Done";
