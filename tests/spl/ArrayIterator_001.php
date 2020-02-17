<?php
namespace spl\ArrayIterator_001;

function test($it) {
  $it->foo = 12;
  $it->baz = 666;

  foreach ($it as $key => $val) {
    echo "{$key}: {$val}\n";
  }

  echo $it->baz ."\n";
  echo is_null($it->nonexisting) ."\n";

  echo "\n";
}

test(new \ArrayIterator(["foo" => 43, "bar" => 64], \ArrayIterator::ARRAY_AS_PROPS));
test(new \ArrayIterator(["foo" => 42, "bar" => 64]));
test(new \ArrayIterator((object)["foo" => 42, "bar" => 64]));
test(new \ArrayObject(["foo" => 43, "bar" => 64], \ArrayObject::ARRAY_AS_PROPS));
test(new \ArrayObject(["foo" => 43, "bar" => 64]));
test(new \ArrayObject((object)["foo" => 42, "bar" => 64]));
