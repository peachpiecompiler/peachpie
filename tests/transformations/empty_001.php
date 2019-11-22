<?php
namespace transformations\empty_001;

function simple() {
  echo (int)empty($x);
}

simple();

function flow_sensitive($b) {
  if ($b) {
    $x = $b;
    echo (int)empty($x);
  } else {
    echo (int)empty($x);
  }
}

flow_sensitive(true);
flow_sensitive(false);

function modif_global() {
  global $some_global_variable;
  $some_global_variable = 42;
}

modif_global();

echo (int)empty($some_global_variable);

function with_eval() {
  eval('$x = 42;');
  echo (int)empty($x);
}

with_eval();
