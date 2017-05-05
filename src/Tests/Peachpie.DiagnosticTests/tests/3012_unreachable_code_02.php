<?php

function unreachable_for($x) {

  return;

  for ($i = 0/*!PHP3012!*/; $i < $x; $i++) { }
}

function unreachable_foreach($x) {

  return;

  foreach ($x/*!PHP3012!*/ as $value) { }
}
