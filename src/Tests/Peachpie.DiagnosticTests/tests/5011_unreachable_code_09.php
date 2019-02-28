<?php

function unreachable_type_intersections(bool $b, int $i, float $f, string $s, object $o, $x) {
  if ($b === $i) {
    echo "unreachable";/*!PHP5011!*/
  }

  if ($b !== $i) {
    echo "reachable";
  } else {
    echo "unreachable";/*!PHP5011!*/
  }

  if ($s === $o) {
    echo "unreachable";/*!PHP5011!*/
  } else {
    echo "reachable";
  }

  // Any type
  if ($x === $b) {
    echo "reachable";
  } else {
    echo "reachable";
  }

  // Reference
  $rf = $f;
  $someRef =& $rf;
  if ($rf === $o) {
    echo "reachable";
  } else {
    echo "reachable";
  }

  // Left of instance of must be an object ($s denotes any class name)
  if ($b instanceof $s) {
    echo "unreachable";/*!PHP5011!*/
  } else if ($b instanceof $s) {
    echo "unreachable";/*!PHP5011!*/
  } else if ($i instanceof $s) {
    echo "unreachable";/*!PHP5011!*/
  } else if ($f instanceof $s) {
    echo "unreachable";/*!PHP5011!*/
  } else if ($s instanceof $s) {
    echo "unreachable";/*!PHP5011!*/
  } else if ($o instanceof $s) {
    echo "reachable";
  } else if ($x instanceof $s) {
    echo "reachable";
  } else if ($rf instanceof $s) {
    echo "reachable";
  } else {
    echo "reachable";
  }
}
