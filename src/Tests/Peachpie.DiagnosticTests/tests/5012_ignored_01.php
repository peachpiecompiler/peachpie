<?php

function foo(int $bar) {
  declare(ticks = 1);/*!PHP5012!*/

  // Other diagnostics and type analysis must work within the block
  declare(strict_types = 1)/*!PHP5012!*/ {
    echo $uninitialized/*!PHP5007!*/;
    echo /*|integer|*/$bar;

    if (false) {
      echo "unreachable";/*!PHP5011!*/
    }
  }
}

declare(strict_types = 1);/*!PHP5012!*/

declare(strict_types = 1)/*!PHP5012!*/ {
  if (false) {
    echo "unreachable";/*!PHP5011!*/
  }
}
