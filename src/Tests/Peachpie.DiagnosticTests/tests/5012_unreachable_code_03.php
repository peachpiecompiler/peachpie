<?php

function unreachable_goto($x) {
  start:

  goto end;

  goto start;/*!PHP5012!*/

  end:

  goto start;

  echo "unreachable";/*!PHP5012!*/  
}

// Analysis mustn't break in case of such loop in unreachable code
function infinite_goto($x) {

  return;

  start:

  goto end;/*!PHP5012!*/

  end:

  goto start;/*!PHP5012!*/
}
