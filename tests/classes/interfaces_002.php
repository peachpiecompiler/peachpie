<?php
namespace classes\interfaces_002;

interface IA {
  public function foo();
}

interface IB extends IA {
}

class A implements IA, IB
{
  public function foo() {
	  print_r( func_get_args() ); // implicit parameter is created, causes new implicit overload
  }
}
(new A)->foo(666);

echo "Done";
