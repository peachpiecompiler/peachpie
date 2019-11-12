<?php
namespace pcre\reset_match_start_001;

function test($pattern, $subject) {
  preg_match($pattern, $subject, $matches);
  print_r($matches);
  preg_match_all($pattern, $subject, $matches);
  print_r($matches);
  print_r(preg_split($pattern, $subject));
  echo preg_replace($pattern, "pub", $subject) ."\n";
}

test('/foo\Kbar/', "foobar");
test('/foo\K(bar)/', "foobar");
test('/(foo)\Kbar/', "foobar");
test('/foo\Kbar/', "hafoobar");
test('/foo\K(bar)/', "hafoobar");
test('/(foo)\Kbar/', "hafoobar");

test('/(foo\Kbar)/', "hafoobar");
test('/((foo\Kb)ar)/', "hafoobar");
test('/(fo(o\Kbar))/', "hafoobar");
