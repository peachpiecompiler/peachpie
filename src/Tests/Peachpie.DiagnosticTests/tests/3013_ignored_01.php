<?php

function foo(int $bar) {
  declare(ticks = 1);/*!PHP3013!*/

  // Other diagnostics and type analysis must work within the block
  declare(strict_types = 1)/*!PHP3013!*/ {
    echo $uninitialized/*!PHP3007!*/;
    echo /*|integer|*/$bar;

    if (false) {
      echo "unreachable";/*!PHP3012!*/
    }
  }
}

declare(strict_types = 1);/*!PHP3013!*/

declare(strict_types = 1)/*!PHP3013!*/ {
  if (false) {
    echo "unreachable";/*!PHP3012!*/
  }
}
