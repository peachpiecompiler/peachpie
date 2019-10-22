<?php
namespace classes\__get_002;

class TestClass {
    function __get($name) {
        if($name[0] == 'a')
            return $this->bc;
        else
            return $name;
    }
}

print_r((new TestClass)->abc);

echo "Done.";