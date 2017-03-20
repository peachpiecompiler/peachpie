using Pchp.Core.Reflection;
using System;

namespace Pchp.Core.std
{
    internal delegate void GeneratorStateMachine(Context ctx, Generator gen);

    [PhpType("Generator")]
    public class Generator : Iterator
    {
        /// <summary>
        /// Context associated in which the generator is run.
        /// </summary>
        readonly protected Context _ctx;

        /// <summary>
        /// Lifted local variables from the state machine function.
        /// </summary>
        readonly internal PhpArray _local;

        /// <summary>
        /// Delegate to a static method implementing the state machine itself. 
        /// </summary>
        readonly internal GeneratorStateMachine _stateMachineMethod;
      
        /// <summary>
        /// Helper variables used for <see cref="rewind"/> and <see cref="checkIfRunToFirstYieldIfNotRun"/>
        /// </summary>
        bool _runToFirstYield = false; //Might get replaced by _state logic
        bool _runAfterFirstYield = false;

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

        /// <summary>
        /// Did last yield returned user-specified key.
        /// </summary>
        internal bool _userKeyReturned = false;
        /// <summary>
        /// Automatic numerical key for next yield.
        /// </summary>
        long _nextNumericalKey = 0;

        internal PhpValue _currValue, _currKey, _currSendItem, _returnValue;
        internal Exception _currException;

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
            if(!valid()) { return; }

            checkIfMovingFromFirstYeild();
            checkIfRunToFirstYieldIfNotRun();

            _stateMachineMethod.Invoke(_ctx, this);

            if (!_userKeyReturned) { _currKey = PhpValue.Create(_nextNumericalKey); }
            if(_currKey.IsInteger()) { _nextNumericalKey = (_currKey.ToLong() + 1); } 
        }

        /// <summary>
        /// Checks if there is a current element after calls to <see cref="rewind"/> or <see cref="next"/>.
        /// </summary>
        /// <returns><c>bool</c>.</returns>
        public bool valid()
        {
            checkIfRunToFirstYieldIfNotRun();
            return (_state >= 0);
        }

        /// <summary>
        /// Returns the key of the current element.
        /// </summary>
        public PhpValue key()
        {
            checkIfRunToFirstYieldIfNotRun();
            return _currKey;
        }

        /// <summary>
        /// Returns the current element (value).
        /// </summary>
        public PhpValue current()
        {
            checkIfRunToFirstYieldIfNotRun();
            return _currValue;
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
            next();
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
            if(!valid()) { throw ex; }

            _currException = ex;
            next();
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

        /// <summary>
        /// Checks if the generator is moving beyond first yield, if so sets proper variable. Important for <see cref="rewind"/>.
        /// </summary>
        private void checkIfMovingFromFirstYeild()
        {
            if (_runToFirstYield && !_runAfterFirstYield) { _runAfterFirstYield = true; }
        }

        /// <summary>
        /// Checks if generator already run to the first yield. Runs there if it didn't.
        /// </summary>
        private void checkIfRunToFirstYieldIfNotRun()
        {
            if(!_runToFirstYield) { _runToFirstYield = true; this.next(); }           
        }

    }
}
