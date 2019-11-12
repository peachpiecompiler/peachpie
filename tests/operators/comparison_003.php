<?php
namespace operators\comparison_003;

function bar(int $a) {
  echo $a;
  return $a;
}

function test()
{
  if (bar(42) === "blah") {
    echo "unreachable";
  }

  echo "Done";
}

test();
