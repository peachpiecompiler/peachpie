<?php
namespace classes\interfaces_001;

interface ExceptionMessage
{
    public function getMessage();
}

class MyException extends \Exception implements ExceptionMessage
{
}
