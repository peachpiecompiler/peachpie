<?php
namespace functions\instance_call_002;

interface IA {
  public function foo();
}

class A implements IA {
  public function foo() {
    print_r( func_get_args() );
  }
}

function test(IA $ia) {
  $ia->foo("lorem", "ipsum");	// be careful, do not call IA::foo() directly
}

test(new A);

echo "Done.";
