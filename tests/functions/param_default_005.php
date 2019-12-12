<?php
namespace functions\param_default_005;

function test(&$arr = [])
{
    print_r($arr);
    $arr[] = 666;
}

test();
test();
test();         // repetitious use of default argument passed by ref has not been changed

print_r([]);    //  internal PhpArray.Empty has not been chaned

echo "Done";
