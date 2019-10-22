<?php
namespace constructs\switch_004;

function f($a)
{
  $cnd = false;
  switch ($a)
  {
    default:
      echo "default\n";

    case "submit":
      echo "submit\n";
      if ($cnd)
      {
        echo "if\n";
      }

    case "edit":
      echo "edit\n";
      break;
  }
}

f("submit");
f("edit");
f("x");
