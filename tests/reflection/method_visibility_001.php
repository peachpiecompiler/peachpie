<?php
namespace reflection\method_visibility_001;

class TestClass1
{
    public function func1()
    {
    }

    protected function func2()
    {
    }

    private function func3()
    {
    }
}

$tmp1 = new \ReflectionClass(TestClass1::class);
$method1 = $tmp1->getMethod("func1");
$method2 = $tmp1->getMethod("func2");
$method3 = $tmp1->getMethod("func3");

echo $method1->isPublic() ? "true" : "false";
echo $method1->isProtected() ? "true" : "false";
echo $method1->isPrivate() ? "true" : "false";
echo $method2->isPublic() ? "true" : "false";
echo $method2->isProtected() ? "true" : "false";
echo $method2->isPrivate() ? "true" : "false";
echo $method3->isPublic() ? "true" : "false";
echo $method3->isProtected() ? "true" : "false";
echo $method3->isPrivate() ? "true" : "false";