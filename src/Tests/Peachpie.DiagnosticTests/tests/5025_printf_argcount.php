<?php

function foo($a, $b, $c, $d) {
  printf("%d %f %'.9d", $a, $b, $c);
  printf("%d %f %'.9d", $a, $b)/*!PHP5025!*/;
  printf("%d %f %'.9d", $a, $b, $c, $d)/*!PHP5025!*/;
  printf('%1$d %d %2$f', $a, $b, $c)/*!PHP5025!*/;
  printf('%1$d %d %1$f', $a, $b)/*!PHP5025!*/;
  printf('%1$d %1$d', $a);

  echo sprintf("%%%d%%%%%f", $a, $b);
  echo sprintf("%%%d%%%%%f", $a)/*!PHP5025!*/;
  echo sprintf("%%%d%%%%%f", $a, $b, $c)/*!PHP5025!*/;
}
