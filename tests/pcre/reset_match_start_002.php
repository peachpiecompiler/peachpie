<?php
namespace pcre\reset_match_start_002;

function test() {
  preg_match_all('/a?/', "foo", $matches);
  print_r($matches);

  preg_match_all('/a?\K/', "foo", $matches);
  print_r($matches);

  // http://www.phpfreaks.com/blog/pcre-regex-spotlight-k
  echo preg_replace('#foo\Kbar#', 'pub', "It\'s foobar time! Free drinks at the bar!");
  echo preg_replace('#fo{3,}\Kbar#', 'pub', "It's foobar time! Free drinks at the fooooobar bar!");
  echo preg_replace('#\d+-\d+\K-#', ' ', "346-5654-78-90-3-116");

  // https://bugs.php.net/bug.php?id=70232
  $pattern = '/(?: |\G)\d\B\K/';
  $subject = "123 a123 1234567 b123 123";
  preg_match_all($pattern, $subject, $matches);
  print_r($matches);
  echo preg_replace($pattern, "*", $subject);
}

test();
