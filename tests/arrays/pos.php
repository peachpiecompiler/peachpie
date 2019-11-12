<?php
namespace arrays\pos;

function f($x)
{
  $x = (($x===null) ? "NULL" : (($x==="") ? "''" : (($x===false) ? "FALSE" : $x)));
  return "$x\n";
}

$a = array(1,0,"",null,false,10);

echo f(current($a));
echo f(key($a));
echo f(next($a));
echo f(current($a));
echo f(key($a));
echo f(next($a));
echo f(current($a));
echo f(key($a));
echo f(next($a));
echo f(current($a));
echo f(key($a));
echo f(next($a));
echo f(current($a));
echo f(key($a));
echo f(next($a));
echo f(current($a));
echo f(key($a));
echo f(next($a));
echo f(current($a));
echo f(key($a));
