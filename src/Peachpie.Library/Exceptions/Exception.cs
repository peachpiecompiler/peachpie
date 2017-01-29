using System;
using Pchp.Core;

/// <summary>
/// <see cref="Exception"/> is the base class for all Exceptions in PHP 5, and the base class for all user exceptions in PHP 7.
/// </summary>
public class Exception : System.Exception, Throwable
{
    protected string message;
    protected long code;
    protected string file;
    protected int line;

    public Exception() { }

    public Exception(string message = "", long code = 0, Throwable previous = null)
    {
        __construct(message, code, previous);
    }

    public void __construct(string message = "", long code = 0, Throwable previous = null)
    {
        this.message = message;
        this.code = code;
    }

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

    public virtual string getMessage() => this.message;

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