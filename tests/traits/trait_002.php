<?php
namespace traits\trait_002;

// Example #3 Alternate Precedence Order Example

trait HelloWorld {
    public function sayHello() {
        echo 'Hello World!';
    }
}

class TheWorldIsNotEnough {
    use HelloWorld;
    public function sayHello() {
        echo 'Hello Universe!';
    }
}

(new TheWorldIsNotEnough())->sayHello();
echo "\nDone.";
