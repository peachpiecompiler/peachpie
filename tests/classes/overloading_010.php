<?php
namespace classes\overloading_010;

interface SessionBagInterface {
    public function getName();
}

final class SessionBagProxy implements SessionBagInterface {
    public function getName() {
        return __METHOD__;
    }
}

class C {

}

interface I {
    public function getPropertyValue($containingValue);
}

abstract class B extends C implements I {

}

class A extends B {
    public function getPropertyValue($object) { return __METHOD__; }
}

echo (new SessionBagProxy)->getName();
echo (new A)->getPropertyValue(NULL);

echo "Done.";
