<?php

function unreachable_while($x) {

  return;

  while ($x == 0/*!PHP3012!*/) { }
}

function unreachable_do_while($x) {

  return;

  do {
    echo "unreachable";/*!PHP3012!*/
  } while ($x == 0);
}

function unreachable_for($x) {

  return;

  for ($i = 0/*!PHP3012!*/; $i < $x; $i++) { }
}

function unreachable_foreach($x) {

  return;

  foreach ($x/*!PHP3012!*/ as $value) { }
}
