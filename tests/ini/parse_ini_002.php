<?php

namespace ini\parse_ini_002;

function test() {
    $ini_content = <<<INI

;no sections
# comments

foo=bar
1 = intkey
constant = PHP_CONST_DEFINED_PREVIOUSLY
result = PHP_CONST_1 | PHP_CONST_2
testempty=   
testnull=null
testwithcomments1=bar ;test
testwithcomments2=bar #test

[SECTION_TEST_STRING]
foo=also bar
normal=Hello   World   f
quoted="Hello World"

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

INI;

    define('PHP_CONST_DEFINED_PREVIOUSLY', 'FOOBAR'); 
    define('PHP_CONST_1', '1'); 
    define('PHP_CONST_2', '2'); 
    define('PHP_CONST_4', '4'); 

    echo "Test RAW".PHP_EOL;
    $result = parse_ini_string($ini_content, false, INI_SCANNER_RAW);
    if (empty($result)) throw new \Exception("Should not be empty");
    print_r($result);

    echo "Test RAW with section".PHP_EOL;
    $result = parse_ini_string($ini_content, true, INI_SCANNER_RAW);
    if (empty($result)) throw new \Exception("Should not be empty");
    print_r($result);
}

test();
