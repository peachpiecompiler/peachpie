<?php
namespace strings\wordwrap;

echo(wordwrap("123  123ab123", 3, "|")), PHP_EOL;
echo(wordwrap("123   123ab123", 3, "|")), PHP_EOL;
echo(wordwrap("123    123ab123", 3, "|")), PHP_EOL;
echo(wordwrap("123     123ab123", 3, "|")), PHP_EOL;
echo(wordwrap("123      123ab123", 3, "|")), PHP_EOL;

echo(wordwrap("123  123ab123", 3, "|", true)), PHP_EOL;
echo(wordwrap("123   123ab123", 3, "|", true)), PHP_EOL;
echo(wordwrap("123    123ab123", 3, "|", true)), PHP_EOL;
echo(wordwrap("123     123ab123", 3, "|", true)), PHP_EOL;
echo(wordwrap("123      123ab123", 3, "|", true)), PHP_EOL;

// handling newline:
echo wordwrap("ab a\nbbbb\nccccc\nddd\neeee", 5), PHP_EOL;

//
echo "Done.";