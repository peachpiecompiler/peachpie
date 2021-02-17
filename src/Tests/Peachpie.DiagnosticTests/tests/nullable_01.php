<?php

function test(string $s) {
  $not_null = _($s);
  return /*|string|*/$not_null;
}
