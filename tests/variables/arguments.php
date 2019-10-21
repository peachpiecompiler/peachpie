<?php
namespace variables\arguments;

  function A() 
  {
    $numargs = func_num_args(); 
    echo "Number of arguments: $numargs<br>\n"; 
    if ($numargs >= 2) 
    { 
      echo "Second argument is: " . func_get_arg (1) . "<br>\n"; 
    } 
    $arg_list = func_get_args(); 
    
    for ($i = 0; $i < $numargs; $i++) 
      echo "Argument $i is: " . $arg_list[$i] . "<br>\n"; 
  }

  A(1,2,3,4,5,6);
