<?php

function foo(bool $b, int $i, float $f, string $s, array $a, object $o) {
  echo (bool)$b/*!PHP6002!*/;
  echo (int)$i/*!PHP6002!*/;
  echo (float)$f/*!PHP6002!*/;
  echo (string)$s/*!PHP6002!*/;
  echo (array)$a/*!PHP6002!*/;
  echo (object)$o/*!PHP6002!*/;
}
