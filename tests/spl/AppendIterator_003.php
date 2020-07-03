<?php
namespace spl\AppendIterator_003;

class MyIterator extends \FilterIterator
{
  private $name;

  public function __construct(string $name, \Iterator $iterator)
  {
    $this->name = $name;
    parent::__construct($iterator);
  }
  public function accept() : bool
  {
    return true;
  }

  public function rewind() : void
  {
    echo "{$this->name} rewind\n";
    parent::rewind();
  }

  public function next()
  {
    echo "{$this->name} next\n";
    parent::next();
  }
}

function test() {
  $it1 = new MyIterator("it1", new \ArrayIterator(["foo", "bar"]));
  $it2 = new MyIterator("it2", new \ArrayIterator(["baz", "boo"]));

  $appendIt = new \AppendIterator();
  echo "append it1\n";
  $appendIt->append($it1);
  echo "append it2\n";
  $appendIt->append($it2);

  echo "iterate\n";
  foreach ($appendIt as $value)
  {
  	echo $value ."\n";
  }

  echo "iterate\n";
  foreach ($appendIt as $value)
  {
  	echo $value ."\n";
  }
}

test();
