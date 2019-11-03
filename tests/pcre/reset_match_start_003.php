<?php
namespace pcre\reset_match_start_003;

function test($pattern, $subject) {
  preg_match($pattern, $subject, $matches);
  print_r($matches);
  preg_match_all($pattern, $subject, $matches);
  print_r($matches);
  print_r(preg_split($pattern, $subject));
  echo preg_replace($pattern, "pub", $subject) ."\n";
}

test('/foo(ba\Kz)|(bar)baz/', "foobarbaz");
test('/foo(ba\Kz|bar)/', "foobar");
test('/foo(ba\Kz|bak|b\Kar)/', "foobar");
test('/foo((ba\Kz|bak)|b\Kar)/', "foobar");
test('/fo+(\Kbaz|b\Kak|bar)/', "foobar");
