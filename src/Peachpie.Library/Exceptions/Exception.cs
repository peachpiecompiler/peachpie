using System;
using Pchp.Core;

/// <summary>
/// <see cref="Exception"/> is the base class for all Exceptions in PHP 5, and the base class for all user exceptions in PHP 7.
/// </summary>
public class Exception : System.Exception, Throwable
{
    public virtual int getCode()
    {
        throw new NotImplementedException();
    }

    public virtual string getFile()
    {
        throw new NotImplementedException();
    }

    public virtual int getLine()
    {
        throw new NotImplementedException();
    }

    public virtual string getMessage()
    {
        throw new NotImplementedException();
    }

    public virtual Throwable getPrevious()
    {
        throw new NotImplementedException();
    }

    public virtual PhpArray getTrace()
    {
        throw new NotImplementedException();
    }

    public virtual string getTraceAsString()
    {
        throw new NotImplementedException();
    }

    public virtual string __toString()
    {
        throw new NotImplementedException();
    }
}