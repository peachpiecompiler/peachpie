<?php
namespace classes\overloading_006;

 interface I
 {
     public function foo();
 }

 class A implements I
 {
     public function foo() {
         return __METHOD__;
     }
 }


 class B extends A
 {
     public function foo() {
         return __METHOD__;
     }
 }

 echo (new B)->foo();

 echo "Done.";
