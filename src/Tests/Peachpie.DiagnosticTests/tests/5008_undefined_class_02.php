<?php

$b = UndefinedClass/*!PHP5008!*/::Constant;
$b = UndefinedClass/*!PHP5008!*/::$staticProperty;
$b = UndefinedClass/*!PHP5008!*/::staticMethod();
$b = UndefinedClass/*!PHP5008!*/::undefinedStaticMethod();

$badInstance = new UndefinedClass/*!PHP5008!*/();

if ($badInstance instanceof UndefinedClass/*!PHP5008!*/) {}
