<?php
namespace functions\instance_call_004;

class X {
    private function handleDependencies() : bool {
        echo "1.";
        return false;
    }

    function test() {
        if (!$this instanceof Y)
        {
            // lets confuse type analysis with $this
        }

        $this->handleDependencies(); // call to a private method so $this variable must be resolved to be able to call it in ct
    }
}

class Y extends X {
}

(new X)->test();

echo "Done.";