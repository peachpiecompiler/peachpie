<?php
namespace strings\vsprintf_001;

function test() {
  // https://www.php.net/manual/en/function.vsprintf.php
  echo vsprintf("%04d-%02d-%02d", explode('-', '1988-8-1')) ."\n";

  // Not enough arguments
  echo vsprintf("%04d-%02d-%02d", explode('-', '1988-8')) ."\n";
}

test();
