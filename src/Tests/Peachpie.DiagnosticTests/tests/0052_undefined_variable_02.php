<?php

function bar() {
  $ref = &$ensuredRef;
  echo $ensuredRef;

  $ensuredArray[] = 0;
}
