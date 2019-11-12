<?php
namespace arrays\array_diff_intersect;

$array1 = array("a" => "green", "red", "red", "blue", "blue", "blue"); 
$array2 = array("b" => "green", "yellow", "q" => "red", "red", "blue", "blue"); 
$array3 = array("b" => "green", "yellow", "yellow","blue", "blue","r" => "blue", "blue","blue", "blue"); 
$array4 = array("b" => "green", "yellow", "red", "hello", "blue"); 
print_r(array_diff($array1, $array2, $array3, $array4)); 
print_r(array_intersect($array1, $array2, $array3, $array4)); 
print_r(array_diff($array4, $array3, $array2, $array1)); 
print_r(array_intersect($array4, $array3, $array2, $array1)); 
print_r(array_diff_assoc($array1, $array2, $array3, $array4)); 
print_r(array_intersect_assoc($array1, $array2, $array3, $array4)); 
print_r(array_diff_assoc($array4, $array3, $array2, $array1)); 
print_r(array_intersect_assoc($array4, $array3, $array2, $array1)); 


$a = array(1,2,3);
print_r(array_intersect($a, $a, $a)); 
print_r(array_diff($a, $a, $a)); 

$array1 = array(1,2,3);
$array2 = array(1,1,1,1,1);
$array3 = array(2,2,2,2,2);
print_r(array_diff($array1, $array2, $array3));  
print_r(array_intersect($array1, $array2, $array3));  
