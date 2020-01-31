using System;
using System.Diagnostics;
using Pchp.Core;
using Pchp.Core.Reflection;

public delegate void GeneratorStateMachineDelegate(Context ctx, object @this, PhpArray locals, PhpArray tmpLocals, Generator gen);

[PhpType(PhpTypeAttribute.InheritName), PhpExtension("Core")]
public class Generator : Iterator
{
    #region BoundVariables

    /// <summary>
    /// Context associated in which the generator is run.
    /// </summary>
    readonly internal Context _ctx;

    /// <summary>
    /// Delegate to a static method implementing the state machine itself. 
    /// </summary>
    readonly internal GeneratorStateMachineDelegate _stateMachineMethod;

    readonly internal RuntimeMethodHandle _ownerhandle;

    /// <summary>
    /// INTERNAL: In case Generator is created inside a Closure (anonymous function),
    /// we remember the Closure's scope here.
    /// Otherwise the Generator's scope is resolved statically during the compilation.
    /// </summary>
    internal RuntimeTypeHandle _scope;

    /// <summary>
    /// Lifted local variables from the state machine function.
    /// </summary>
    readonly PhpArray _locals;

    /// <summary>
    /// Temporal locals of the state machine function.
    /// </summary>
    readonly PhpArray _tmpLocals;

    /// <summary>
    /// Bound `$this` for non-static enumerator methods, <c>null</c> for static ones.
    /// </summary>
    internal object _this;

    /// <summary>
    /// Lazy bound <c>static</c>.
    /// Can be <c>null</c> if not used in the generator state function.
    /// </summary>
    internal PhpTypeInfo _static;

    #endregion

    #region StateVariables

    /// <summary>
    /// Current state of the state machine implemented by <see cref="_stateMachineMethod"/>
    /// </summary>
    /// <remarks>
    ///   0: before first yield
    ///  -1: running
    ///  -2: closed
    /// +x: valid state
    /// </remarks>
    internal int _state = 0;

    internal PhpValue _currValue, _currKey, _currSendItem, _returnValue;
    internal Exception _currException;

    /// <summary>
    /// Max numerical key for next auto-incremented yield.
    /// </summary>
    internal long _maxNumericalKey = -1;

    /// <summary>
    /// Helper variables used for <see cref="rewind"/> and <see cref="checkIfRunToFirstYieldIfNotRun"/>
    /// </summary>
    bool _runToFirstYield; // Might get replaced by _state logic
    bool _runAfterFirstYield;

    #endregion

    #region HelperLocalProperties
    bool isInValidState { get => (_state >= 0); }
    #endregion  

    #region Constructors

    internal Generator(Context ctx, PhpArray locals, PhpArray tmpLocals, GeneratorStateMachineDelegate method, RuntimeMethodHandle ownerhandle)
    {
        Debug.Assert(ctx != null);
        Debug.Assert(method != null);

        _ctx = ctx;
        _locals = locals;
        _tmpLocals = tmpLocals;
        _stateMachineMethod = method;
        _ownerhandle = ownerhandle;

        _currValue = PhpValue.Null;
        _currKey = PhpValue.Null;
        _currSendItem = PhpValue.Null;
        _returnValue = PhpValue.Null;
    }

    #endregion

    #region IteratorMethods

    /// <summary>
    /// Rewinds the iterator to the first element.
    /// </summary>
    public void rewind()
    {
        checkIfRunToFirstYieldIfNotRun();
        if (_runAfterFirstYield) { throw new Exception("Cannot rewind a generator that was already run"); }
    }

    /// <summary>
    /// Moves forward to next element.
    /// </summary>
    public void next()
    {
        checkIfRunToFirstYieldIfNotRun();
        moveStateMachine();
    }

    /// <summary>
    /// Checks if there is a current element after calls to <see cref="rewind"/> or <see cref="next"/>.
    /// </summary>
    /// <returns><c>bool</c>.</returns>
    public bool valid()
    {
        checkIfRunToFirstYieldIfNotRun();
        return isInValidState;
    }

    /// <summary>
    /// Returns the key of the current element.
    /// </summary>
    public PhpValue key()
    {
        checkIfRunToFirstYieldIfNotRun();
        return (isInValidState) ? _currKey : PhpValue.Null;
    }

    /// <summary>
    /// Returns the current element (value).
    /// </summary>
    public PhpValue current()
    {
        checkIfRunToFirstYieldIfNotRun();
        return (isInValidState) ? _currValue : PhpValue.Null;
    }

    /// <summary>
    /// Get the return value of a generator
    /// </summary>
    /// <returns>Returns the generator's return value once it has finished executing. </returns>
    public PhpValue getReturn()
    {
        if (_state != -2) { throw new Exception("Cannot get return value of a generator that hasn't returned"); }
        return _returnValue;
    }

    /// <summary>
    /// Sends a <paramref name="value"/> to the generator and forwards to next element.
    /// </summary>
    /// <returns>Returns the yielded value. </returns>
    public PhpValue send(PhpValue value)
    {
        checkIfRunToFirstYieldIfNotRun();

        _currSendItem = value;
        moveStateMachine();
        _currSendItem = PhpValue.Null;

        return current();
    }

    /// <summary>
    /// Throw an exception into the generator
    /// </summary>
    /// <param name="ex">Exception to throw into the generator.</param>
    /// <returns>Returns the yielded value. </returns>
    public PhpValue @throw(Exception ex)
    {
        if (!valid()) { throw ex; }

        _currException = ex;
        moveStateMachine();
        _currException = null;

        return current();
    }

    /// <summary>
    /// Serialize callback.
    /// </summary>
    /// <remarks>
    /// Throws an exception as generators can't be serialized. 
    /// </remarks>
    public void __wakeup()
    {
        throw new Exception("Unserialization of 'Generator' is not allowed");
    }

    #endregion

    #region HelperMethods

    /// <summary>
    /// Moves the state machine to next element.
    /// </summary>
    [DebuggerNonUserCode, DebuggerStepThrough]
    private void moveStateMachine()
    {
        if (!isInValidState) { return; }
        checkIfMovingFromFirstYeild();

        _stateMachineMethod.Invoke(_ctx, _this, _locals, _tmpLocals, gen: this);

        _runToFirstYield = true;
    }

    /// <summary>
    /// Checks if the generator is moving beyond first yield, if so sets proper variable. Important for <see cref="rewind"/>.
    /// </summary>
    [DebuggerNonUserCode, DebuggerStepThrough]
    private void checkIfMovingFromFirstYeild()
    {
        if (_runToFirstYield && !_runAfterFirstYield) { _runAfterFirstYield = true; }
    }

    /// <summary>
    /// Checks if generator already run to the first yield. Runs there if it didn't.
    /// </summary>
    [DebuggerNonUserCode, DebuggerStepThrough]
    private void checkIfRunToFirstYieldIfNotRun()
    {
        if (!_runToFirstYield) { this.moveStateMachine(); }
    }

    #endregion
}

