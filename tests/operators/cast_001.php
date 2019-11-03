<?php
namespace operators\cast_001;

// $x = (object)1;
// $x = (object)1.2;
// $x = (object)"Helo";
// $x = (object)true;
$x = (object)[1, 2, "prop" => "value"];