<?php

class C {}

function foo(array $a, C $c, ?C $cn1, C $cn2 = null) {}

function bar($a, $c, $cn1, $cn2) {
  foo(/*|mixed|*/$a, /*|mixed|*/$c, /*|mixed|*/$cn1, /*|mixed|*/$cn2);
  foo(/*|array|*/$a, /*|C|*/$c, /*|C|null|*/$cn1, /*|C|null|*/$cn2);
}
