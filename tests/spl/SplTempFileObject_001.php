<?php
namespace spl\SplTempFileObject_001;

function print_val($v) {
    if (is_string($v)) {
        echo "\"{$v}\"";
    } else if (is_bool($v)) {
        echo $v ? "true" : "false";
    } else {
        echo $v;
    }
    echo "\n";
}

function testObj($obj) {
    print_val($obj->getBasename());
    print_val($obj->getFileInfo());
    print_val($obj->getPathname());
    print_val($obj->getPath());
    print_val($obj->getExtension());
    print_val($obj->getFilename());
    print_val($obj->getRealPath());
    print_val($obj->isDir());
    print_val($obj->isFile());

    $obj->fwrite("Lorem Ipsum dolor sit amet");
    $obj->fseek(2);
    echo $obj->fread(10);
    
    echo "\n\n";
}

function test() {
    testObj(new \SplTempFileObject(-1));
    testObj(new \SplTempFileObject(0));
    testObj(new \SplTempFileObject(5));
}

test();
