<?php

$b = UndefinedClass/*!PHP3008!*/::Constant;
$b = UndefinedClass/*!PHP3008!*/::$staticProperty;
$b = UndefinedClass/*!PHP3008!*/::staticMethod();
$b = UndefinedClass/*!PHP3008!*/::undefinedStaticMethod();

$badInstance = new UndefinedClass/*!PHP3008!*/();

if ($badInstance instanceof UndefinedClass/*!PHP3008!*/) {}
