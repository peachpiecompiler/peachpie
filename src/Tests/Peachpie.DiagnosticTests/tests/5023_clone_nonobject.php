<?php

class MyClass {}

function foo(MyClass $mc, int $i, string $s, float $f, bool $b, $any, $hlp) {
  clone $mc;
  clone $i/*!PHP5023!*/;
  clone $s/*!PHP5023!*/;
  clone $f/*!PHP5023!*/;
  clone $b/*!PHP5023!*/;
  clone $any;

  $is = $hlp ? $i : $s;
  $mci = $hlp ? $mc : $i;
  clone $is/*!PHP5023!*/;
  clone $mci/*!PHP5023!*/;
}
