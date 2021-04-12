<?php

function foo1(): string {
    return "Hello";
}

function foo2(): ?string { // TODO: Nullable<PhpString>
    return null;
}

/*|string|*/$res = foo1();
/*|string|null|*/$res = foo2();

function foo3() { // : stdClass
    return new stdClass;
}

function foo4() { // : ?stdClass
    return rand() ? new stdClass : null;
}

function foo5(): ?stdClass {
    return null;
}

/*|stdClass|*/$res = foo3();
/*|stdClass|null|*/$res = foo4();
/*|stdClass|null|*/$res = foo5();
