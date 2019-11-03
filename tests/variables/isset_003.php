<?php
namespace variables\isset_003;

interface TestInterface extends \ArrayAccess {
}

function testFunc(TestInterface $obj) {
    echo isset($obj["hello_world"]) ? 1 : 0;	// compiler crash https://github.com/peachpiecompiler/peachpie/issues/504 
}

echo "Done.";
