<?php

function foo(int $bar) {
  declare(ticks = 1);/*!PHP5013!*/

  // Other diagnostics and type analysis must work within the block
  declare(strict_types = 1)/*!PHP5013!*/ {
    echo $uninitialized/*!PHP5007!*/;
    echo /*|integer|*/$bar;

    if (false) {
      echo "unreachable";/*!PHP5012!*/
    }
  }
}

declare(strict_types = 1);/*!PHP5013!*/

declare(strict_types = 1)/*!PHP5013!*/ {
  if (false) {
    echo "unreachable";/*!PHP5012!*/
  }
}
