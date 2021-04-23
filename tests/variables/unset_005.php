<?php
namespace variables\unset_005;

function test() {
    $array1 = [1, 2];
    $x = &$array1[1];  // $x <=> $array1[1] form a "reference/alias set"
    unset($x); // The set has been broken, `$array1[1]` should not be treated as an alias anymore
    $array2 = $array1; // hence the copying semantics should be to deep copy the value
    $array2[1] = 22; // this should not affect `$array1[1]`
    print_r($array1);
}

test();

$array1 = [1, 2];
$x = &$array1[1];  // $x <=> $array1[1] form a "reference/alias set"
unset($x); // The set has been broken, `$array1[1]` should not be treated as an alias anymore
$array2 = $array1; // hence the copying semantics should be to deep copy the value
$array2[1] = 22; // this should not affect `$array1[1]`
print_r($array1);

echo "Done.";
