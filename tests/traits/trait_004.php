<?php

// Example #10 Static Methods

trait StaticExample {
    public static function doSomething() {
        return 'Doing something';
    }
}

class Example {
    use StaticExample;
}

echo Example::doSomething();
