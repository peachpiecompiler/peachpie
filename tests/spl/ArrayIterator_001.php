<?php
namespace spl\ArrayIterator_001;

function test($subj, $flags) {
  $it = new \ArrayIterator($subj);

  $it->setFlags($flags);
  $it->foo = 12;
  $it->baz = 666;

  foreach ($it as $key => $val) {
    echo "{$key}: {$val}\n";
  }

  echo $it->baz ."\n";
  echo is_null($it->nonexisting) ."\n";

  echo "\n";
}

test(["foo" => 43, "bar" => 64], \ArrayIterator::ARRAY_AS_PROPS);
test(["foo" => 42, "bar" => 64], 0);
test((object)["foo" => 42, "bar" => 64], 0);
