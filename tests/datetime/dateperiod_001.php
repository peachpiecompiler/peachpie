<?php
namespace date\dateperiod_001;

function test(\DatePeriod $p) {
  foreach ($p as $v) {
    echo "{$v->format('Y-m-d')}\n";
  }

  echo "\n";

  foreach ($p as $k => $v) {
    echo "{$k}: {$v->format('Y-m-d')}\n";
  }
}

test(new \DatePeriod(new \DateTime('2020-02-02'), new \DateInterval('P2D'), 2));
