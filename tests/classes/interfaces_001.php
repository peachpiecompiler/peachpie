<?php

interface ExceptionMessage
{
    public function getMessage();
}

class MyException extends Exception implements ExceptionMessage
{
}
