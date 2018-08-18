<?php

function unreachable_while($x) {

  return;

  while ($x == 0/*!PHP5011!*/) { }
}

function unreachable_do_while($x) {

  return;

  do {
    echo "unreachable";/*!PHP5011!*/
  } while ($x == 0);
}

function unreachable_for($x) {

  return;

  for ($i = 0/*!PHP5011!*/; $i < $x; $i++) { }
}

function unreachable_foreach($x) {

  return;

  foreach ($x/*!PHP5011!*/ as $value) { }
}
