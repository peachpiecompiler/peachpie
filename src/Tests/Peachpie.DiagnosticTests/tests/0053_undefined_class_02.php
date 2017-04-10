<?php

$b = UndefinedClass/*!PHP0053!*/::Constant;
$b = UndefinedClass/*!PHP0053!*/::$staticProperty;
$b = UndefinedClass/*!PHP0053!*/::staticMethod();
$b = UndefinedClass/*!PHP0053!*/::undefinedStaticMethod();

$badInstance = new UndefinedClass/*!PHP0053!*/();

if ($badInstance instanceof UndefinedClass/*!PHP0053!*/) {}
