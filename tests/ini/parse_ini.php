<?php

namespace ini\parse_ini;

function test() {
    $ini_content = <<<INI

;no sections
# comments

foo=bar
1 = intkey
constant = PHP_CONST_DEFINED_PREVIOUSLY
result = PHP_CONST_1 | PHP_CONST_2 | PHP_CONST_4
result2 = 15 & PHP_CONST_2
result3 = ~PHP_CONST_1 ^ PHP_CONST_4
result4 = (PHP_CONST_1 | PHP_CONST_2) & ~PHP_CONST_4
testempty=   
testnull=null
testwithcomments1=bar ;test
testwithcomments2=bar #test
key with spaces=bar

[SECTION_TEST_STRING]
foo=also bar
normal=Hello World
quoted="Hello World"
apostrophed = 'Hello World'
quoted_escaped = "it work's \"fine\"!"
quoted_multiline = "line1
line2
line3"

[SECTION_TEST_NUMBER]
testnum=42
testfloat1=+1.4E-3
testfloat2=0.005

[SECTION_TEST_TRUTHY]
booltrue1=true
booltrue2=on
booltrue3=yes

[SECTION_TEST_FALSY]
boolfalse1=false
boolfalse2=off
boolfalse3=no
boolfalse4=none

[SECTION_ARRAY]
arr[] = a
arr[] = b
arr[] = c

INI;

    define('PHP_CONST_DEFINED_PREVIOUSLY', 'FOOBAR'); 
    define('PHP_CONST_1', '1'); 
    define('PHP_CONST_2', '2'); 
    define('PHP_CONST_4', '4'); 

    echo "Test default".PHP_EOL;
    print_r(parse_ini_string($ini_content));

    echo "Test default with section".PHP_EOL;
    print_r(parse_ini_string($ini_content, true));
}

test();
