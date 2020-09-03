<?php
namespace com_dotnet\com_create_guid;

function test() {
    if (!extension_loaded("com_dotnet")) {
        echo "Extension com_dotnet not loaded";
        return;
    }

    echo !empty(com_create_guid()) . PHP_EOL;
}

test();
