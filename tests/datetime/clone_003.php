<?php
namespace datetime\clone_003;

class TestClass extends \DateTime
{
}

$obj = new TestClass();

echo get_class($obj), PHP_EOL;
echo get_class(clone $obj), PHP_EOL;

echo "Done.";
