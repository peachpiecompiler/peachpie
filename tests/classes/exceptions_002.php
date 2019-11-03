<?php
namespace classes\exceptions_002;

class A extends \Exception
{}

class B extends \Exception
{}

try
{
    throw new B;
}
catch (A|B|C $ex)
{
    echo "A|B|C hit\n";
}
finally
{
    echo "finally\n";
}

echo "Done.";
