<?php
namespace strings\wordwrap;

echo(wordwrap("123  123ab123", 3, "|")),"\n";
echo(wordwrap("123   123ab123", 3, "|")),"\n";
echo(wordwrap("123    123ab123", 3, "|")),"\n";
echo(wordwrap("123     123ab123", 3, "|")),"\n";
echo(wordwrap("123      123ab123", 3, "|")),"\n";

echo(wordwrap("123  123ab123", 3, "|", true)),"\n";
echo(wordwrap("123   123ab123", 3, "|", true)),"\n";
echo(wordwrap("123    123ab123", 3, "|", true)),"\n";
echo(wordwrap("123     123ab123", 3, "|", true)),"\n";
echo(wordwrap("123      123ab123", 3, "|", true)),"\n";
