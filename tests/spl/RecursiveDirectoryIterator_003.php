<?php
namespace spl\RecursiveDirectoryIterator_003;

class MySplFileInfo extends \SplFileInfo
{
    public function __construct($file_name) {
        parent::__construct($file_name);
    }

    public function foo() {
        echo $this->getPathname() ." foo!\n";
    }
}

class MyRecursiveDirectoryIterator extends \RecursiveDirectoryIterator
{
    public function __construct($dir) {
        parent::__construct($dir);
    }

    public function current() {
        return new MySplFileInfo($this->getPathname());
    }
}

function test() {
    $it = new MyRecursiveDirectoryIterator('subdir');
    $it = new \RecursiveIteratorIterator($it);
    foreach ($it as $file) {
        if ($file->getFilename() == '.' || $file->getFilename() == '..') {
          continue;
        }

        $file->foo();
    }
}

test();
