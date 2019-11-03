<?php
namespace classes\overloading_012;

interface FieldInterface
{
    public function increment($invert);
}

abstract class AbstractField implements FieldInterface
{
    // we have to synthesize "abstract increment" here
    // which may break how we create class tables
}

class HoursField extends AbstractField
{
    public function increment($invert, $parts = null)   // the method has a different signature than synthesized AbstractField::increment
    {
        echo __METHOD__;
    }
}

(new HoursField)->increment(1, 2);

echo "Done";
