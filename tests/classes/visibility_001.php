<?php
namespace classes\visibility_001;

// https://github.com/peachpiecompiler/peachpie/issues/225

class A  {
    public function getSomeText() {
        return $this->_privateMember.'-DEBUG';
    }
}
class B extends A {
    protected $_privateMember = "Hi there!";
}

echo (new B())->getSomeText();

echo "Done.";
