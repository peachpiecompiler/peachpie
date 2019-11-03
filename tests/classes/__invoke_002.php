<?php
namespace classes\__invoke_002;

interface I {
	function __invoke();
}

class A {
    public function __invoke(...$args) {
        print_r( $args );
    }
}

class B {
    public function __invoke() {
        print_r( func_get_args() );
    }
}

class C {
    public function __invoke($a, $b, ...$args) {
        print_r( $args );
    }
}

class D {
    public function __invoke($a, $b) {
        print_r( func_get_args() );
    }
}


class E implements I {
    public function __invoke() {
        print_r( func_get_args() );
    }
}

(new A)(1, 2, 3);
(new B)(1, 2, 3);
(new C)(1, 2, 3);
(new D)(1, 2, 3);
(new E)(1, 2, 3);
