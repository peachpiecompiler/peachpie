<?php

function unreachable_while($x) {

  return;

  while ($x == 0/*!PHP5012!*/) { }
}

function unreachable_do_while($x) {

  return;

  do {
    echo "unreachable";/*!PHP5012!*/
  } while ($x == 0);
}

function unreachable_for($x) {

  return;

  for ($i = 0/*!PHP5012!*/; $i < $x; $i++) { }
}

function unreachable_foreach($x) {

  return;

  foreach ($x/*!PHP5012!*/ as $value) { }
}
