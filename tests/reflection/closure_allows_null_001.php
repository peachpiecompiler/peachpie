<?php
namespace reflection\closure_allows_null_001;

$tmp1 = function (\stdClass $user) {
    return true;
};

$tmp2 = function (?\stdClass $user) {
    return true;
};

$param1 = (new \ReflectionFunction($tmp1))->getParameters()[0];
$param2 = (new \ReflectionFunction($tmp2))->getParameters()[0];

echo $param1->allowsNull() ? "true" : "false";
echo $param2->allowsNull() ? "true" : "false";