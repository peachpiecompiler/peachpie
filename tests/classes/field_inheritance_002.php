<?php
namespace classes\field_inheritance_002;

class A {
  protected $x = -1;
  
  public function Aget() {
    return $this->x;
  }
  
  public function Aset($v) {
    $this->x = $v;
  }
}

class B extends A {
  public $x;
  
  public function Bget() {
    return $this->x;
  }
  
  public function Bset($v) {
    $this->x = $v;
  }
}

function test() {
	// in local:
	$b = new B;
	$b->x = 123;
	echo $b->Aget() ."\n";

	$b = new B;
	$b->Bset(123);
	echo $b->Aget() ."\n";
	echo $b->Bget() ."\n";

	$b = new B;
	$b->Aset(123);
	echo $b->Aget() ."\n";
	echo $b->Bget() ."\n";
}

// in global:
$b = new B;
$b->x = 123;
echo $b->Aget() ."\n";

$b = new B;
$b->Bset(123);
echo $b->Aget() ."\n";
echo $b->Bget() ."\n";

$b = new B;
$b->Aset(123);
echo $b->Aget() ."\n";
echo $b->Bget() ."\n";

//
test();
