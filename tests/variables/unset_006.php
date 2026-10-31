<?php
namespace variables\unset_006;

$g = 123;
unset($g, ); // null item // https://github.com/peachpiecompiler/peachpie/issues/1146

