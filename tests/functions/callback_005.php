<?php
namespace functions\callback_005;

class A {}

function test() {
  try
  {
    echo array_map("invalid_routine", [0]);
  	echo "OK ";
    echo call_user_func("invalid_routine");
  	echo "OK ";
    echo call_user_func_array("invalid_routine", []);
  	echo "OK ";
    echo call_user_method("invalid_routine", new A);
  	echo "OK ";
  }
  catch (\Throwable $e)
  {
    echo get_class($e);
  }

}

test();
