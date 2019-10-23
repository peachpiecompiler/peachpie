<?php
namespace variables\valueref_004;

class MyException extends \RuntimeException {

    function foo() {
        $x = new class($this->message) {    // https://github.com/peachpiecompiler/peachpie/issues/564 // we have to be able to make PhpAlias from Exception::$message
           public function __construct(&$message) { }
        };
    }
}

echo "Done.";
