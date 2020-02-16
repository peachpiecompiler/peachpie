<?php
namespace spl\serialization_001;

function print_it($it) {
  foreach ($it as $key => $value) {
    echo $key .":\n". print_r($value, true) ."\n";
  }
  echo "\n";
}

function test($it) {
  print_it($it);
  print_r($it->__serialize());

  $serialized = serialize($it);
  echo "\n". $serialized ."\n\n";
  $deserialized = unserialize($serialized);
  print_it($deserialized);
}

function test_arrayiterator() {
  $arr = ["foo" => "bar", "bla", ["baz" =>  (object)["foobar" => "barfoo"]]];
  $it = new \ArrayIterator($arr);
  $it->blah = "boo";
  $it->setFlags(\ArrayIterator::ARRAY_AS_PROPS);

  test($it);
}

function test_arrayobject() {
  $arr = ["foo" => "bar", "bla", ["baz" =>  (object)["foobar" => "barfoo"]]];
  $it = new \ArrayObject($arr, \ArrayObject::ARRAY_AS_PROPS);
  $it->blah = "boo";

  test($it);
}

function test_objectstorage() {
  $store = new \SplObjectStorage();

  $store->foo = "bar";
  $store->bla = "baz";

  $store[(object)["p" => "bar"]] = "foo";
  $store->attach((object)["p" => "blah"]);

  test($store);
}

function test_dll() {
  $dll = new \SplDoublyLinkedList();

  $dll->foo = "bar";
  $dll->bla = "baz";

  $dll->push("foo");
  $dll->push((object)["p" => "bar"]);

  test($dll);
}

test_arrayiterator();
test_arrayobject();
test_objectstorage();
test_dll();
