<?php
namespace operators\cast_003;

function foo()
{
    // returns System.Void
}

print_r((int)foo()); // 0
print_r((string)foo()); // ""

echo "\nDone.";
