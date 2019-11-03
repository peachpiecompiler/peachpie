<?php
namespace functions\anonymous_006_args;

class QQ
{
    public static function main()
    {
        $q = 123;
        $f = function($param) use($q) {
            print_r(func_get_args());
        };
        $f("asd", "fgh");
    }
}
QQ::main();

echo "Done.";
