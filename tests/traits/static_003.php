<?php
namespace traits\static_003;

trait T
{
  public static function foo() {
    // https://github.com/peachpiecompiler/peachpie/issues/707
    parent::bar();
  }
}

class C {
  use T; // TODO: in future, we might report diagnostic here, invalid use of "parent"
}

echo "Done.";
