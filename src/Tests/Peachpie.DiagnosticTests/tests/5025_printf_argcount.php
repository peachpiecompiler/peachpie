<?php

function foo($a, $b, $c, $d) {
  printf("%d %f %'.9d", $a, $b, $c);
  printf("%d %f %'.9d"/*!PHP5025!*/, $a, $b);
  printf("%d %f %'.9d"/*!PHP5025!*/, $a, $b, $c, $d);
  printf('%1$d %d %2$f'/*!PHP5025!*/, $a, $b, $c);
  printf('%1$d %d %1$f'/*!PHP5025!*/, $a, $b);
  printf('%1$d %1$d', $a);

  echo sprintf("%%%d%%%%%f", $a, $b);
  echo sprintf("%%%d%%%%%f"/*!PHP5025!*/, $a);
  echo sprintf("%%%d%%%%%f"/*!PHP5025!*/, $a, $b, $c);
}
