<?php
namespace classes\static_006;

// See https://www.php.net/manual/en/language.oop5.late-static-bindings.php#example-243

// forwards the late static type through several self:: calls

class X
{
    static function a()
    {
        echo static::class;
    }

    static function b()
    {
        self::a();
    }

    static function c()
    {
        self::b();
    }
}

X::c();

echo "Done.";
