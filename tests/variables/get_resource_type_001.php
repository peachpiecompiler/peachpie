<?php
namespace variables\get_resource_type_001;

class A {}

function test($x)
{
  $res = get_resource_type($x);

  echo (int)is_null($res);
  echo (int)is_bool($res);
  echo (int)$res;
}

test(new A);
