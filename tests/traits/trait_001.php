<?php
namespace traits\trait_001;

// Example #2 Precedence Order Example

class Base {
    public function sayHello() {
        echo 'Hello ';
    }
}

trait SayWorld {
    public function sayHello() {
        parent::sayHello();
        echo 'World!';
    }
}

class MyHelloWorld extends Base {
    use SayWorld;
}

(new MyHelloWorld())->sayHello();

echo "\nDone.";
