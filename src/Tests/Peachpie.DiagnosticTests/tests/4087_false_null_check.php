<?php

class X {
    // https://github.com/peachpiecompiler/peachpie/issues/1111#issuecomment-1537665747
    // nette\schema\src\Schema\Expect.php	89
    public static function is(mixed $value) {
        return true;
    }
}

X::is(null);

echo "ok";
