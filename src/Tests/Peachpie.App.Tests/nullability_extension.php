<?php

function nullable(Nullability $x) {
  return $x->returnNull($x);
}

function non_nullable(Nullability $x) {
  return $x->noNull($x);
}
