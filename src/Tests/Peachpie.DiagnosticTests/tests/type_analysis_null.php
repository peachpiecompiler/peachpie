<?php

function foo1(): string {
    return "Hello";
}

function foo2_todo(): ?string { // TODO: Nullable<PhpString>
    return null;
}

function foo2(): ?stdClass {
    return null;
}

/*|string|*/$res = foo1();
/*|stdClass|null|*/$res = foo2();

function foo3() { // : stdClass
    return new stdClass;
}

function foo4() { // : ?stdClass
    return rand() ? new stdClass : null;
}

/*|stdClass|*/$res = foo3();
/*|stdClass|null|*/$res = foo4();
