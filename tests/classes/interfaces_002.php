<?php

interface IA {
  public function foo();
}

interface IB extends IA {
}

class A implements IA, IB
{
  public function foo($p) {
	  echo $p;
  }
}
(new A)->foo(666);

echo "Done";
