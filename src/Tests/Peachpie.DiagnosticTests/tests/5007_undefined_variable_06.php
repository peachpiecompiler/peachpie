<?php

function bar() {
  // Output arguments (known from C#) mustn't be reported
  if (preg_match(".*", "hello", $matches/*!PHP5030!*/)) {
    echo $matches;
  }
}
