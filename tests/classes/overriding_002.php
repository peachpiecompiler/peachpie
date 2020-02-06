<?php
namespace classes\overriding_002;

interface SelfDescribing
{
    public function toString(): string;
}

final class Warning extends \Exception implements SelfDescribing
{
    public function toString(): string  // even sealed method (it's sealed because in sealed class) must be marked as virtual if implements an interface
    {
        return $this->getMessage();
    }
}


echo "Done.";
