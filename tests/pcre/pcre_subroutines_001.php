<?php
namespace pcre\pcre_subroutines_001;

function test($pattern, $subject) {
  echo preg_match($pattern, $subject, $matches);
  echo "\n";

	print_r($matches);
}

test('/([abc])(?1)(?1)/', 'abcd');
test('/([abc](d))(?1)(?1)/', 'adcdbd');
test('/([abc](d))(?:[abc](?:d))(?:[abc](?:d))/', 'adcdbd');

test('/^(((?=.*(::))(?!.*\3.+\3))\3?|([\dA-F]{1,4}(\3|:\b|$)|\2))(?4){5}((?4){2}|(((2[0-4]|1\d|[1-9])?\d|25[0-5])\.?\b){4})$/i', '2001:0db8:85a3:0000:0000:8a2e:0370:7334');
test('/\A(\((?>[^()]|(?1))*\))\z/', '(((lorem)ipsum()))');
test('/\b(([a-z])(?1)(?2)|[a-z])\b/', 'racecar');
test('/\b(([a-z])(?1)(?2)|[a-z])\b/', 'none');
test('/^Name:\ (.*) Born:\ ((?:3[01]|[12][0-9]|[1-9])-(?:Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)-(?:19|20)[0-9][0-9]) Admitted:\ (?2) Released:\ (?2)$/',
  'Name: John Doe Born: 17-Jan-1964 Admitted: 30-Jul-2013 Released: 3-Aug-2013');
