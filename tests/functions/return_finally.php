<?php
namespace functions\return_finally;

// returning value from "finally"

function f1()
{
    try
    {
        return 111;
    }
    catch (\Exception $ex)
    {
        return 222;
    }
    finally
    {
        return 666;
    }
}

function f2()
{
    try {
        try {
            return 111;
        }
        catch (\Exception $ex) {
            return 222;
        }
        finally {
            return 666;
        }
    }
    catch (\Exception $ex) {
        return 333;
    }
    finally {
        return 777;
    }
}

echo f1();
echo f2();